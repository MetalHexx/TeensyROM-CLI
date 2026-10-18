using NSubstitute;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Testing;
using TeensyRom.Cli.Api;
using TeensyRom.Cli.Commands.Main;
using TeensyRom.Cli.Services;
using TeensyRom.Cli.Settings;

namespace TeensyRom.Cli.Tests;

[Collection(ConsoleCollection.Name)]
public sealed class IndexCommandTests : IDisposable
{
    private readonly IAnsiConsole _originalConsole = AnsiConsole.Console;
    private readonly TestConsole _console = new TestConsole().Interactive();
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly IPlayerService _player = Substitute.For<IPlayerService>();
    private readonly ISettingsService _settings = Substitute.For<ISettingsService>();
    private readonly IDeviceLogStream _deviceLog = Substitute.For<IDeviceLogStream>();
    private readonly List<string> _events = [];
    private Action<string> _onLine = _ => { };

    public IndexCommandTests()
    {
        _console.Profile.Width = 200;
        AnsiConsole.Console = _console;
        _console.Input.PushKey(ConsoleKey.Enter);

        _settings.GetSettings().Returns(_ => new TeensySettings());
        _deviceLog.FollowAsync(Arg.Any<Action<string>>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            _events.Add("follow");
            _onLine = call.Arg<Action<string>>();
            return Task.FromResult<IAsyncDisposable>(new StopSpy(_events));
        });
    }

    public void Dispose() => AnsiConsole.Console = _originalConsole;

    private Task<int> Run() =>
        new IndexCommand(_storage, _player, _settings, _deviceLog)
            .ExecuteAsync(new CommandContext([], Substitute.For<IRemainingArguments>(), "index", null), new IndexSettings { StorageDevice = "sd", Path = "/music" });

    private void IndexingLogs(params string[] lines) =>
        _storage.Index(Arg.Any<string>()).Returns(_ =>
        {
            _events.Add("index");
            foreach (var line in lines) _onLine(line);
            _events.Add(_console.Output.Contains("Indexing completed") ? "completed line already shown" : "completed line not shown");
            return Task.CompletedTask;
        });

    [Fact]
    public async Task Execute_ShowsEachIndexingLineAsTrWebLogsIt_AndStopsTheStreamWhenIndexingEnds()
    {
        IndexingLogs("Resetting TeensyROM", "=> Indexing (SD): /music", "=> Indexing (SD): /music/MUSICIANS", "Indexing completed for SD storage.");

        var result = await Run();

        result.Should().Be(0);
        _console.Output.Should().Contain("=> Indexing (SD): /music").And.Contain("=> Indexing (SD): /music/MUSICIANS").And.Contain("Indexing completed for SD storage.")
            .And.NotContain("Resetting TeensyROM");
        _events.Should().Equal("follow", "index", "completed line already shown", "stop");
    }

    [Fact]
    public async Task Execute_WhenIndexingFails_StillStopsTheStream()
    {
        _storage.Index(Arg.Any<string>()).Returns(_ => Task.FromException(new TeensyApiException("There was an error indexing.")));

        var run = () => Run();

        await run.Should().ThrowAsync<TeensyApiException>().WithMessage("There was an error indexing.");
        _events.Should().Equal("follow", "stop");
    }

    [Fact]
    public async Task Execute_WhenTheStreamCantBeFollowed_SaysSo_AndStillIndexes()
    {
        _deviceLog.FollowAsync(Arg.Any<Action<string>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IAsyncDisposable>(new TeensyApiException("tr-web's log stream is unavailable.")));

        var result = await Run();

        result.Should().Be(0);
        _console.Output.Should().Contain("without showing its progress").And.Contain("tr-web's log stream is unavailable.");
        await _storage.Received(1).Index("/music");
    }

    private sealed class StopSpy(List<string> events) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            events.Add("stop");
            return ValueTask.CompletedTask;
        }
    }
}
