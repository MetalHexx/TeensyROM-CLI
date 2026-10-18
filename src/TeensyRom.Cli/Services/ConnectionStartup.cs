using TeensyRom.Cli.Api;
using TeensyRom.Cli.Helpers;

namespace TeensyRom.Cli.Services;

/// <summary>
/// What every CLI start does before the menu: confirms the right tr-web is running and picks the TeensyROM to work with.
/// </summary>
internal sealed class ConnectionStartup(IDeviceService devices)
{
    /// <summary>
    /// Returns true once a device is selected; false after telling the user what to fix.
    /// </summary>
    public async Task<bool> RunAsync(CancellationToken ct = default)
    {
        var version = await devices.CheckVersionAsync(ct);

        switch (version.Outcome)
        {
            case VersionOutcome.Unreachable:
                RadHelper.WriteError($"tr-web isn't running at {ApiPin.DisplayAddress} — start it and try again.");
                return false;
            case VersionOutcome.Mismatch:
                RadHelper.WriteError($"tr-web {version.RunningVersion} is running at {ApiPin.DisplayAddress}, but this CLI needs tr-web {ApiPin.Version}.");
                return false;
        }

        List<DeviceSummary> online;

        try
        {
            online = (await devices.GetDevicesAsync(ct)).Where(d => d.IsOnline).ToList();
        }
        catch (TeensyApiException ex)
        {
            RadHelper.WriteError(ex.Message);
            return false;
        }

        if (online.Count == 0)
        {
            RadHelper.WriteError("No TeensyROM is online in tr-web — connect one in tr-web and try again.");
            return false;
        }

        var labels = online.Select(Label).ToList();
        var choice = PromptHelper.FilePrompt("TeensyROM", labels);

        devices.Select(online[labels.IndexOf(choice)]);
        return true;
    }

    private static string Label(DeviceSummary device) => $"{device.Name} ({device.Connection})";
}
