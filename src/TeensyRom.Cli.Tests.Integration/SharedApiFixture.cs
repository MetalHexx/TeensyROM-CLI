// Runs the real CLI in-process against the one shared test API (scripts/test-api.ps1) - no hardware needed.
//
//   dotnet test src/TeensyRom.Cli.Tests.Integration --filter "Category!=Hardware"
//
// The fixture starts or reuses the API itself. When tr-web is not the sibling "teensyrom-web" folder of
// this repo, set TEENSYROM_WEB_DIR to the checkout; it reaches test-api.ps1 through the environment.

using System.Diagnostics;
using TeensyRom.Cli.Api;
using TeensyRom.Cli.Api.Generated;

namespace TeensyRom.Cli.Tests.Integration;

/// <summary>
/// Ensures the shared test API once per run and reads its device list once, so scenarios never
/// race the <c>/api/devices</c> rate limit (one request per 5 s, one queued).
/// </summary>
public sealed class SharedApiFixture : IAsyncLifetime
{
    private static readonly TimeSpan EnsureTimeout = TimeSpan.FromMinutes(15);

    private HttpClient? _http;

    /// <summary>A direct client for cross-checking what the CLI shows against what the API reports.</summary>
    public ITeensyRomApiClient Api { get; private set; } = null!;

    /// <summary>Every device the API knew when the run started, read once.</summary>
    public IReadOnlyList<CartDto> Devices { get; private set; } = [];

    public IReadOnlyList<CartDto> OnlineDevices => Devices.Where(d => d.DeviceState != DeviceState.ConnectionLost).ToList();

    public async Task InitializeAsync()
    {
        await EnsureApiAsync();

        _http = new HttpClient { BaseAddress = new Uri(ApiPin.BaseUrl) };
        Api = new TeensyRomApiClient(_http);
        Devices = await ReadDevicesAsync();
    }

    public Task DisposeAsync()
    {
        _http?.Dispose();
        return Task.CompletedTask;
    }

    private async Task<IReadOnlyList<CartDto>> ReadDevicesAsync()
    {
        try
        {
            var response = await Api.FindDevicesAsync(fullScan: false);
            return response.Devices.ToList();
        }
        catch (ApiException ex) when (ex.StatusCode == 404)
        {
            return [];
        }
    }

    private static async Task EnsureApiAsync()
    {
        var script = Path.Combine(FindRepoRoot(), "scripts", "test-api.ps1");
        var start = new ProcessStartInfo("pwsh")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        // Under `dotnet test`, the reusable MSBuild nodes left by the script's build keep it from returning until they idle out.
        start.Environment["MSBUILDDISABLENODEREUSE"] = "1";
        start.ArgumentList.Add("-NoProfile");
        start.ArgumentList.Add("-File");
        start.ArgumentList.Add(script);
        start.ArgumentList.Add("ensure");

        using var process = Process.Start(start)
            ?? throw new InvalidOperationException($"Could not start pwsh to run {script}.");
        using var timeout = new CancellationTokenSource(EnsureTimeout);

        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();

        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new InvalidOperationException($"test-api.ps1 ensure did not finish within {EnsureTimeout.TotalMinutes:0} minutes.");
        }

        var stdout = await output;
        var stderr = await errors;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(LastLine(stderr) ?? LastLine(stdout) ?? $"test-api.ps1 ensure exited with {process.ExitCode}.");
        }
    }

    private static string FindRepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "scripts", "test-api.ps1"))) return dir.FullName;
        }
        throw new InvalidOperationException($"scripts/test-api.ps1 not found above {AppContext.BaseDirectory}.");
    }

    private static string? LastLine(string text) =>
        text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault();
}
