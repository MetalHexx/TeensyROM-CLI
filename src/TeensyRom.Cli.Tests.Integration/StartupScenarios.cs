namespace TeensyRom.Cli.Tests.Integration;

[Collection(RealApiCollection.Name)]
public sealed class StartupScenarios(SharedApiFixture api)
{
    [Fact]
    public async Task Startup_OffersExactlyTheOnlineDevicesTheApiReports()
    {
        using var cli = new CliHarness();
        var online = api.OnlineDevices;
        if (online.Count > 0) cli.Console.Input.PushKey(ConsoleKey.Enter);

        var ready = await cli.StartAsync();

        cli.Output.Should().NotContain("this CLI needs").And.NotContain("isn't running");

        if (online.Count == 0)
        {
            ready.Should().BeFalse();
            cli.Output.Should().Contain("No TeensyROM is online");
            return;
        }

        ready.Should().BeTrue();
        foreach (var device in online)
        {
            cli.Output.Should().Contain(device.Name);
        }

        var offlineOnly = api.Devices.Except(online).Where(d => !online.Any(o => o.Name.Contains(d.Name)));
        foreach (var device in offlineOnly)
        {
            cli.Output.Should().NotContain(device.Name);
        }
    }
}
