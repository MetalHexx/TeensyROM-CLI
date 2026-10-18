using NSubstitute;
using Spectre.Console;
using Spectre.Console.Testing;
using TeensyRom.Cli.Api;
using TeensyRom.Cli.Services;

namespace TeensyRom.Cli.Tests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ConsoleCollection
{
    public const string Name = "Static AnsiConsole";
}

[Collection(ConsoleCollection.Name)]
public sealed class ConnectionStartupTests : IDisposable
{
    private readonly IAnsiConsole _originalConsole = AnsiConsole.Console;
    private readonly TestConsole _console = new TestConsole().Interactive();
    private readonly IDeviceService _devices = Substitute.For<IDeviceService>();
    private readonly ConnectionStartup _sut;

    public ConnectionStartupTests()
    {
        _console.Profile.Width = 200;
        AnsiConsole.Console = _console;
        _devices.CheckVersionAsync(Arg.Any<CancellationToken>()).Returns(new VersionCheck(VersionOutcome.Match, "1.0.0-alpha.9"));
        _sut = new ConnectionStartup(_devices);
    }

    public void Dispose() => AnsiConsole.Console = _originalConsole;

    private static DeviceSummary Online(string id, string name, string connection = "USB COM3") =>
        new(id, name, true, connection, "Connected");

    private static DeviceSummary Offline(string id, string name) =>
        new(id, name, false, "USB COM9", "ConnectionLost");

    private void TrWebKnows(params DeviceSummary[] known) =>
        _devices.GetDevicesAsync(Arg.Any<CancellationToken>()).Returns(known);

    [Fact]
    public async Task Run_OffersOnlyOnlineDevices_AndSelectsThePick()
    {
        var alpha = Online("a", "Alpha");
        var bravo = Online("b", "Bravo", "Ethernet 10.0.0.7");
        TrWebKnows(alpha, Offline("c", "Charlie"), bravo);
        _console.Input.PushKey(ConsoleKey.DownArrow);
        _console.Input.PushKey(ConsoleKey.Enter);

        var ready = await _sut.RunAsync();

        ready.Should().BeTrue();
        _devices.Received(1).Select(bravo);
        _console.Output.Should().Contain("Alpha (USB COM3)").And.Contain("Bravo (Ethernet 10.0.0.7)").And.NotContain("Charlie");
    }

    [Fact]
    public async Task Run_WithOneOnlineDevice_StillPrompts()
    {
        var alpha = Online("a", "Alpha");
        TrWebKnows(alpha);
        _console.Input.PushKey(ConsoleKey.Enter);

        var ready = await _sut.RunAsync();

        ready.Should().BeTrue();
        _devices.Received(1).Select(alpha);
        _console.Input.IsKeyAvailable().Should().BeFalse("the prompt consumed the key press");
    }

    [Fact]
    public async Task Run_WithADeviceNameThatLooksLikeMarkup_ShowsItAsTyped()
    {
        var odd = Online("a", "[red]Rig[/] #1");
        TrWebKnows(odd);
        _console.Input.PushKey(ConsoleKey.Enter);

        var ready = await _sut.RunAsync();

        ready.Should().BeTrue();
        _devices.Received(1).Select(odd);
        _console.Output.Should().Contain("[red]Rig[/] #1");
    }

    [Fact]
    public async Task Run_WithNoOnlineDevice_SaysSoAndFails()
    {
        TrWebKnows(Offline("c", "Charlie"));

        var ready = await _sut.RunAsync();

        ready.Should().BeFalse();
        _console.Output.Should().Contain("No TeensyROM is online in tr-web — connect one in tr-web and try again.");
        _devices.DidNotReceive().Select(Arg.Any<DeviceSummary>());
    }

    [Fact]
    public async Task Run_WhenListingDevicesFails_ShowsItsMessageAndFails()
    {
        _devices.GetDevicesAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<DeviceSummary>>(new TeensyApiException("tr-web is busy listing devices — try again in a few seconds.", 503)));

        var ready = await _sut.RunAsync();

        ready.Should().BeFalse();
        _console.Output.Should().Contain("tr-web is busy listing devices — try again in a few seconds.");
    }

    [Fact]
    public async Task Run_WhenTrWebIsNotRunning_SaysSoAndSkipsTheDeviceList()
    {
        _devices.CheckVersionAsync(Arg.Any<CancellationToken>()).Returns(new VersionCheck(VersionOutcome.Unreachable, null));

        var ready = await _sut.RunAsync();

        ready.Should().BeFalse();
        _console.Output.Should().Contain("tr-web isn't running at localhost:10213 — start it and try again.");
        await _devices.DidNotReceive().GetDevicesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_WhenTrWebIsTheWrongVersion_NamesBothVersionsAndSkipsTheDeviceList()
    {
        _devices.CheckVersionAsync(Arg.Any<CancellationToken>()).Returns(new VersionCheck(VersionOutcome.Mismatch, "1.0.0-alpha.10"));

        var ready = await _sut.RunAsync();

        ready.Should().BeFalse();
        _console.Output.Should().Contain($"tr-web 1.0.0-alpha.10 is running at localhost:10213, but this CLI needs tr-web {ApiPin.Version}.");
        await _devices.DidNotReceive().GetDevicesAsync(Arg.Any<CancellationToken>());
    }
}
