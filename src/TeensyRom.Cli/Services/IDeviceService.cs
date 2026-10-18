namespace TeensyRom.Cli.Services;

/// <summary>
/// The TeensyROM units tr-web knows, and the one this CLI session works with.
/// </summary>
internal interface IDeviceService
{
    DeviceSummary? SelectedDevice { get; }

    /// <summary>Asks tr-web which version it runs and compares it with the pinned one.</summary>
    Task<VersionCheck> CheckVersionAsync(CancellationToken ct = default);

    /// <summary>Lists the devices tr-web already knows, online first, without scanning for new ones.</summary>
    /// <exception cref="TeensyApiException">tr-web could not list its devices.</exception>
    Task<IReadOnlyList<DeviceSummary>> GetDevicesAsync(CancellationToken ct = default);

    void Select(DeviceSummary device);

    /// <summary>The selected device's id.</summary>
    /// <exception cref="TeensyApiException">No device has been selected.</exception>
    string RequireDeviceId();
}

internal sealed record DeviceSummary(string Id, string Name, bool IsOnline, string Connection, string State);

internal enum VersionOutcome { Match, Mismatch, Unreachable }

internal sealed record VersionCheck(VersionOutcome Outcome, string? RunningVersion);
