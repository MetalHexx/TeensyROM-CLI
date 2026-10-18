using NSubstitute;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Testing;
using System.Reactive.Linq;
using TeensyRom.Cli.Commands.Main.Launcher;
using TeensyRom.Cli.Player;
using TeensyRom.Cli.Services;
using TeensyRom.Cli.Storage.Entities;

namespace TeensyRom.Cli.Tests;

[Collection(ConsoleCollection.Name)]
public sealed class PlayerCommandTests : IDisposable
{
    private readonly IAnsiConsole _originalConsole = AnsiConsole.Console;
    private readonly TestConsole _console = new TestConsole().Interactive();
    private readonly IPlayerService _player = Substitute.For<IPlayerService>();
    private readonly IStorageService _storage = Substitute.For<IStorageService>();
    private readonly PlayerState _state;

    public PlayerCommandTests()
    {
        _console.Profile.Width = 200;
        AnsiConsole.Console = _console;

        _state = new PlayerState { CurrentItem = new SongItem { Name = "Commando.sid", Path = "/music/Commando.sid" } };
        _player.GetState().Returns(_ => _state with { });
        _player.FileLaunched.Returns(Observable.Never<ILaunchableItem>());
    }

    public void Dispose() => AnsiConsole.Console = _originalConsole;

    /// <summary>A player operation that takes real time to finish, the way a launch on tr-web does.</summary>
    private Task SettlesAs(PlayState state) => Task.Run(async () =>
    {
        await Task.Delay(50);
        _state.PlayState = state;
    });

    private void Choose(int downPresses)
    {
        for (var i = 0; i < downPresses; i++) _console.Input.PushKey(ConsoleKey.DownArrow);
        _console.Input.PushKey(ConsoleKey.Enter);
    }

    private const int Back = 7;

    private void Run() =>
        new PlayerCommand(_player, _storage).Execute(new CommandContext([], Substitute.For<IRemainingArguments>(), "player", null), new PlayerSettings());

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Execute_AfterNextOrPreviousFromAPause_OffersStopPauseOnceTheNewTrackPlays(int nextOrPrevious)
    {
        _state.PlayState = PlayState.Paused;
        _player.PlayNext().Returns(_ => SettlesAs(PlayState.Playing));
        _player.PlayPrevious().Returns(_ => SettlesAs(PlayState.Playing));
        Choose(nextOrPrevious);
        Choose(Back);

        Run();

        _console.Output.Should().Contain("Resume", "the player was paused when the first menu was drawn")
            .And.Contain("Stop/Pause", "the second menu is drawn after the new track started playing");
    }

    [Fact]
    public void Execute_AfterStopPause_OffersResume()
    {
        _state.PlayState = PlayState.Playing;
        _player.TogglePlay().Returns(_ => SettlesAs(PlayState.Paused));
        Choose(2);
        Choose(Back);

        Run();

        _console.Output.Should().Contain("Stop/Pause").And.Contain("Resume");
    }

    [Fact]
    public void Execute_AfterBanSkipsToTheNextTrack_KeepsOfferingStopPause()
    {
        _state.PlayState = PlayState.Playing;
        _player.TogglePlay().Returns(_ => SettlesAs(PlayState.Paused));
        _player.PlayNext().Returns(_ => SettlesAs(PlayState.Playing));
        _storage.BanFile(Arg.Any<ILaunchableItem>()).Returns(Task.CompletedTask);
        Choose(4);
        Choose(1);
        Choose(Back);

        Run();

        _console.Output.Should().Contain("Stop/Pause").And.NotContain("Resume");
    }
}
