using System.Net;
using System.Text;
using TeensyRom.Cli.Api.Generated;

namespace TeensyRom.Cli.Tests;

/// <summary>
/// A <see cref="TeensyRomApiClient"/> over a stubbed HTTP handler, so tests exercise the real generated request and error handling.
/// </summary>
internal static class ApiTestSupport
{
    public static TeensyRomApiClient ClientAnswering(HttpStatusCode status, string body) =>
        ClientWith(_ => Task.FromResult(Reply(status, body)));

    public static TeensyRomApiClient ClientWith(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) =>
        new(new HttpClient(new StubHandler(handler)) { BaseAddress = new Uri("http://localhost:10213") });

    public static HttpResponseMessage Reply(HttpStatusCode status, string body) => new(status)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    public static ApiException ApiFailure(int status, string? body) =>
        new("NSwag's own wording", status, body, new Dictionary<string, IEnumerable<string>>(), null);

    private sealed class StubHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            handler(request);
    }
}
