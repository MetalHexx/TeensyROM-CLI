namespace TeensyRom.Cli.Api;

/// <summary>
/// The one place that names the tr-web version this CLI is built against and where it finds it.
/// </summary>
internal static class ApiPin
{
    /// <summary>The tr-web version the committed spec copy and generated client match.</summary>
    public const string Version = "1.0.0-alpha.8";

    public const string BaseUrl = "http://localhost:10213";

    /// <summary>The address as shown in messages.</summary>
    public const string DisplayAddress = "localhost:10213";
}
