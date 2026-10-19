using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Testing;
using TeensyRom.Cli.Api.Generated;
using TeensyRom.Cli.Services;
using TeensyStorageType = TeensyRom.Cli.Storage.Entities.TeensyStorageType;

namespace TeensyRom.Cli.Tests.Integration;

/// <summary>
/// The real CLI - composition root, startup and commands - on Spectre's test console.
/// Push keys on <see cref="Console"/>.Input before the prompt that reads them.
/// </summary>
internal sealed class CliHarness : IDisposable
{
    private readonly IAnsiConsole _original = AnsiConsole.Console;
    private CommandApp? _app;

    public TestConsole Console { get; } = NewConsole();

    public CliComposition Host { get; }

    public CliHarness()
    {
        AnsiConsole.Console = Console;
        Host = CliHost.Create();
    }

    /// <summary>Runs what the CLI runs before its menu: the version check and the device prompt.</summary>
    public Task<bool> StartAsync() => Host.Startup.RunAsync();

    /// <summary>Runs a command line; one app serves every call on this harness.</summary>
    public int Run(params string[] args) =>
        (_app ??= CliHost.CreateApp(new TypeRegistrar(Host.Services))).Run(args);

    public string Output => Console.Output;

    public void Dispose() => AnsiConsole.Console = _original;

    public static TestConsole NewConsole()
    {
        var console = new TestConsole().Interactive();
        console.Profile.Width = 300;
        return console;
    }
}

/// <summary>
/// CLI services resolved from a composition that runs no command, for scenarios that call the services directly.
/// </summary>
internal sealed class CliServices : IDisposable
{
    private readonly IAnsiConsole _original = AnsiConsole.Console;
    private readonly ServiceProvider _provider;

    public CliServices()
    {
        AnsiConsole.Console = CliHarness.NewConsole();
        _provider = CliHost.Create().Services.BuildServiceProvider();
    }

    public T Get<T>() where T : notnull => _provider.GetRequiredService<T>();

    /// <summary>Makes the device the one the CLI's services work with, as the startup prompt would.</summary>
    public void SelectDevice(CartDto device) => Get<IDeviceService>().Select(
        new DeviceSummary(device.DeviceId, device.Name, true, device.ConnectionType.ToString(), device.DeviceState.ToString()));

    /// <summary>The SD card when the device reports it, else USB; skips the scenario when it reports neither.</summary>
    public static TeensyStorageType AvailableStorageOrSkip(CartDto device)
    {
        Skip.If(!device.SdStorage.Available && !device.UsbStorage.Available, $"{device.Name} reports no available storage");

        return device.SdStorage.Available ? TeensyStorageType.SD : TeensyStorageType.USB;
    }

    public void Dispose()
    {
        _provider.Dispose();
        AnsiConsole.Console = _original;
    }
}
