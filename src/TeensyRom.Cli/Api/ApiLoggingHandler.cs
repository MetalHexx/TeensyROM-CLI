using System.Diagnostics;
using TeensyRom.Cli.Logging;

namespace TeensyRom.Cli.Api;

/// <summary>
/// Logs every call to tr-web as "METHOD path → status (ms)" and failures that never got an answer as errors.
/// </summary>
internal sealed class ApiLoggingHandler(ILoggingService log) : DelegatingHandler(new HttpClientHandler())
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var call = $"{request.Method} {request.RequestUri?.PathAndQuery}";
        var timer = Stopwatch.StartNew();

        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            log.Internal($"{call} → {(int)response.StatusCode} ({timer.ElapsedMilliseconds} ms)");
            return response;
        }
        catch (Exception ex)
        {
            log.InternalError($"{call} → failed ({timer.ElapsedMilliseconds} ms): {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }
}
