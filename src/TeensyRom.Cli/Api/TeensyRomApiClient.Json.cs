using System.Text.Json;
using System.Text.Json.Serialization;

namespace TeensyRom.Cli.Api.Generated;

/// <summary>
/// Hand-written half of the generated client. tr-web's spec declares its enums without a string type,
/// so the generator emits int-backed enums while tr-web sends names on the wire.
/// </summary>
public partial class TeensyRomApiClient
{
    static partial void UpdateJsonSerializerSettings(JsonSerializerOptions settings)
        => settings.Converters.Add(new JsonStringEnumConverter());

    // Keeps every response body on ApiException.Response: tr-web answers some 404s with a bare JSON
    // string, which the generated ProblemDetails read rejects, and the message would otherwise be lost.
    partial void Initialize() => ReadResponseAsString = true;
}
