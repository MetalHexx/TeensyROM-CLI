using TeensyRom.Cli.Api;
using TeensyRom.Cli.Api.Generated;

namespace TeensyRom.Cli.Services;

internal sealed class DeviceService(ITeensyRomApiClient api) : IDeviceService
{
    private static readonly TimeSpan VersionTimeout = TimeSpan.FromSeconds(5);

    public DeviceSummary? SelectedDevice { get; private set; }

    public async Task<VersionCheck> CheckVersionAsync(CancellationToken ct = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(VersionTimeout);

        try
        {
            var response = await api.GetVersionAsync(timeout.Token);
            var running = response.Version;
            var plus = running.IndexOf('+');
            var comparable = plus < 0 ? running : running[..plus];

            return new VersionCheck(
                string.Equals(comparable, ApiPin.Version, StringComparison.Ordinal) ? VersionOutcome.Match : VersionOutcome.Mismatch,
                running);
        }
        catch (Exception)
        {
            return new VersionCheck(VersionOutcome.Unreachable, null);
        }
    }

    public async Task<IReadOnlyList<DeviceSummary>> GetDevicesAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await ApiErrors.Call(() => api.FindDevicesAsync(fullScan: false, ct));
            return response.Devices.Select(ToSummary).ToList();
        }
        catch (TeensyApiException ex) when (ex.StatusCode == 404)
        {
            return [];
        }
        catch (TeensyApiException ex) when (ex.StatusCode == 503)
        {
            throw new TeensyApiException("tr-web is busy listing devices — try again in a few seconds.", ex.StatusCode, ex);
        }
    }

    public void Select(DeviceSummary device) => SelectedDevice = device;

    public string RequireDeviceId() =>
        SelectedDevice?.Id ?? throw new TeensyApiException("No TeensyROM is selected.");

    private static DeviceSummary ToSummary(CartDto cart) => new(
        cart.DeviceId,
        cart.Name,
        cart.DeviceState != DeviceState.ConnectionLost,
        cart.ConnectionType == ConnectionType.Serial ? $"USB {cart.ComPort}" : $"Ethernet {cart.IpAddress}",
        cart.DeviceState.ToString());
}
