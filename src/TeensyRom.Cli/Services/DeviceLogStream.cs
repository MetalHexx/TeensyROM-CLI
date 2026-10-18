using Microsoft.AspNetCore.SignalR.Client;
using System.Diagnostics;
using TeensyRom.Cli.Api;
using TeensyRom.Cli.Api.Generated;
using TeensyRom.Cli.Logging;

namespace TeensyRom.Cli.Services;

/// <summary>
/// Follows tr-web's log over its SignalR hub, which broadcasts every device's lines to every listener.
/// </summary>
internal sealed class DeviceLogStream(ITeensyRomApiClient api, IDeviceService devices, ILoggingService log, Uri apiAddress) : IDeviceLogStream
{
    private const string HubPath = "/api/logHub";
    private const string LogProducedEvent = "LogProduced";

    public async Task<IAsyncDisposable> FollowAsync(Action<string> onLine, CancellationToken ct = default)
    {
        var deviceId = devices.RequireDeviceId();
        var hub = new HubConnectionBuilder().WithUrl(new Uri(apiAddress, HubPath)).Build();
        var following = new Following(api, hub, log);

        hub.On<string>(LogProducedEvent, line =>
        {
            following.NoteLine();

            if (TryReadMessage(line, deviceId, out var message))
            {
                onLine(message);
            }
        });

        try
        {
            await ApiErrors.Call(() => hub.StartAsync(ct));
            await ApiErrors.Call(() => api.StartLogsAsync(ct));
        }
        catch
        {
            await hub.DisposeAsync();
            throw;
        }
        return following;
    }

    /// <summary>
    /// Reads a line tr-web logged for the device, which arrives as "(deviceId) Level: message". Lines for other
    /// devices, or for none, are not the device's.
    /// </summary>
    public static bool TryReadMessage(string line, string deviceId, out string message)
    {
        message = string.Empty;
        var prefix = $"({deviceId}) ";

        if (!line.StartsWith(prefix, StringComparison.Ordinal)) return false;

        var withLevel = line[prefix.Length..];
        var levelEnd = withLevel.IndexOf(": ", StringComparison.Ordinal);

        message = levelEnd < 0 ? withLevel : withLevel[(levelEnd + 2)..];
        return true;
    }

    private sealed class Following(ITeensyRomApiClient api, HubConnection hub, ILoggingService log) : IAsyncDisposable
    {
        // tr-web hands its queued lines to SignalR in batches every 100 ms and throws away whatever is still queued when
        // the stream is stopped, so the lines logged just before stopping need a moment to arrive.
        private static readonly TimeSpan QuietPeriod = TimeSpan.FromMilliseconds(300);
        private static readonly TimeSpan DrainLimit = TimeSpan.FromSeconds(3);

        private long _lastLine = Stopwatch.GetTimestamp();

        public void NoteLine() => Volatile.Write(ref _lastLine, Stopwatch.GetTimestamp());

        public async ValueTask DisposeAsync()
        {
            try
            {
                await WaitForQuietAsync();
                await ApiErrors.Call(() => api.StopLogsAsync());
            }
            catch (TeensyApiException ex)
            {
                log.InternalError($"Couldn't stop tr-web's log stream: {ex.Message}");
            }
            finally
            {
                await hub.DisposeAsync();
            }
        }

        private async Task WaitForQuietAsync()
        {
            var waited = Stopwatch.StartNew();

            while (waited.Elapsed < DrainLimit && Stopwatch.GetElapsedTime(Volatile.Read(ref _lastLine)) < QuietPeriod)
            {
                await Task.Delay(25);
            }
        }
    }
}
