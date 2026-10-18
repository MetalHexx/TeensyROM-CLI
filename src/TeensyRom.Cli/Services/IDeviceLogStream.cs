using TeensyRom.Cli.Api;

namespace TeensyRom.Cli.Services;

/// <summary>
/// tr-web's live log for the selected device. It is the only place tr-web reports what it is doing while it works,
/// indexing for one.
/// </summary>
internal interface IDeviceLogStream
{
    /// <summary>
    /// Starts tr-web's log stream and hands <paramref name="onLine"/> every line it logs for the selected device, as
    /// it is logged and without the device and level prefix. Disposing the returned handle stops the stream.
    /// </summary>
    /// <remarks><paramref name="onLine"/> runs on a background thread.</remarks>
    /// <exception cref="TeensyApiException">No device is selected, or tr-web's log stream couldn't be started.</exception>
    Task<IAsyncDisposable> FollowAsync(Action<string> onLine, CancellationToken ct = default);
}
