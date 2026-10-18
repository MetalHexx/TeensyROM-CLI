namespace TeensyRom.Cli.Tests.Integration;

[Collection(RealApiCollection.Name)]
public sealed class PortsScenarios(SharedApiFixture api)
{
    [Fact]
    public async Task Ports_ListsEveryDeviceTheApiReports()
    {
        using var cli = new CliHarness();

        if (api.OnlineDevices.Count > 0)
        {
            cli.Console.Input.PushKey(ConsoleKey.Enter);
            (await cli.StartAsync()).Should().BeTrue();
        }

        var exitCode = cli.Run("ports");

        exitCode.Should().Be(0);
        foreach (var device in api.Devices)
        {
            cli.Output.Should().Contain(device.Name);
        }
    }
}
