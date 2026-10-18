using System.Text.Json;
using TeensyRom.Cli.Api.Generated;

namespace TeensyRom.Cli.Api;

/// <summary>
/// The one place API failures become plain text.
/// </summary>
internal static class ApiErrors
{
    /// <summary>
    /// Turns a failed call into the sentence the user sees. Never uses NSwag's own wording or a stack trace.
    /// </summary>
    public static string ToMessage(Exception ex) => ex switch
    {
        TeensyApiException known => known.Message,
        HttpRequestException => $"tr-web stopped answering at {ApiPin.DisplayAddress} — start it and try again.",
        ApiException<ProblemDetails> { Result: { } problem } when FirstText(problem.Title, problem.Detail) is { } text => text,
        ApiException api => FromBody(api.Response) ?? $"tr-web couldn't complete that request (HTTP {api.StatusCode}).",
        _ => "tr-web couldn't complete that request."
    };

    /// <summary>
    /// Runs an API call and rethrows its failures as a <see cref="TeensyApiException"/>.
    /// </summary>
    public static async Task<T> Call<T>(Func<Task<T>> call)
    {
        try
        {
            return await call();
        }
        catch (Exception ex) when (IsApiFailure(ex))
        {
            throw Wrap(ex);
        }
    }

    /// <inheritdoc cref="Call{T}(Func{Task{T}})"/>
    public static async Task Call(Func<Task> call)
    {
        try
        {
            await call();
        }
        catch (Exception ex) when (IsApiFailure(ex))
        {
            throw Wrap(ex);
        }
    }

    private static bool IsApiFailure(Exception ex) =>
        ex is HttpRequestException or ApiException or OperationCanceledException;

    private static TeensyApiException Wrap(Exception ex) => new(ToMessage(ex), (ex as ApiException)?.StatusCode, ex);

    private static string? FirstText(params string?[] candidates) =>
        candidates.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c));

    // tr-web answers with either a bare JSON string or a ProblemDetails object.
    private static string? FromBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;

            return root.ValueKind switch
            {
                JsonValueKind.String => FirstText(root.GetString()),
                JsonValueKind.Object => FirstText(StringProperty(root, "title"), StringProperty(root, "detail")),
                _ => null
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? StringProperty(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
