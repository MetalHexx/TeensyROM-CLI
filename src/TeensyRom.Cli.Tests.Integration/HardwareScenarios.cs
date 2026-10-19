using TeensyRom.Cli.Services;
using TeensyRom.Cli.Storage.Entities;

namespace TeensyRom.Cli.Tests.Integration;

/// <summary>
/// Needs a TeensyROM attached to the test API; excluded from the pipeline run.
/// </summary>
[Collection(RealApiCollection.Name)]
[Trait("Category", "Hardware")]
public sealed class HardwareScenarios(SharedApiFixture api)
{
    /// <summary>
    /// Launches the file named by CLI_BENCH_FILE (for example /music/MUSICIANS/T/Tjelta_Geir/Artillery.sid)
    /// on the first online device. The bench run extends this pattern.
    /// </summary>
    [SkippableFact]
    public async Task LaunchFile_RunsTheBenchFile_AndTheApiReportsItLaunched()
    {
        var path = Environment.GetEnvironmentVariable("CLI_BENCH_FILE") ?? string.Empty;
        Skip.If(string.IsNullOrWhiteSpace(path), "CLI_BENCH_FILE is not set");
        Skip.If(api.OnlineDevices.Count == 0, "no TeensyROM is online in the test API");
        var device = api.OnlineDevices[0];
        var storageType = CliServices.AvailableStorageOrSkip(device);

        using var cli = new CliServices();
        cli.SelectDevice(device);
        var item = FileItem.Create(path).Should().BeAssignableTo<ILaunchableItem>().Subject;

        var outcome = await cli.Get<ILaunchService>().LaunchFile(storageType, item);

        outcome.Status.Should().Be(LaunchStatus.Launched, outcome.Message);
    }
}
