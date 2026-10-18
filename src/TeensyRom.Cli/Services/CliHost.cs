using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Reflection;
using System.Runtime.InteropServices;
using TeensyRom.Cli.Api;
using TeensyRom.Cli.Api.Generated;
using TeensyRom.Cli.Commands.Main;
using TeensyRom.Cli.Commands.Main.Asid;
using TeensyRom.Cli.Commands.Main.Launcher;
using TeensyRom.Cli.Helpers;
using TeensyRom.Cli.Common;
using TeensyRom.Cli.Logging;
using TeensyRom.Cli.Progress;
using TeensyRom.Cli.Settings;
using TeensyRom.Cli.Storage.Services;
using AssemblyExtensions = TeensyRom.Cli.Common.AssemblyExtensions;

namespace TeensyRom.Cli.Services;

/// <summary>
/// The CLI's service registrations and the startup that runs before its first command.
/// </summary>
internal sealed record CliComposition(ServiceCollection Services, ConnectionStartup Startup);

/// <summary>
/// Composition root shared by <c>Program</c> and the end-to-end tests.
/// </summary>
internal static class CliHost
{
    /// <summary>
    /// Builds what startup needs as instances, so the device chosen at startup is the one every command sees.
    /// The returned services are not built into a provider: Spectre registers command and settings types
    /// through the <see cref="ITypeRegistrar"/> during <c>Run</c>, and <see cref="TypeRegistrar"/> ignores
    /// registrations once its provider exists.
    /// </summary>
    public static CliComposition Create()
    {
        var services = new ServiceCollection();
        var logService = new CliLoggingService();
        var alertService = new CliAlertService();
        var settingsService = new SettingsService(logService);

        var settings = settingsService.GetSettings();
        logService.Enabled = settings.EnableDebugLogs;

        var httpClient = new HttpClient(new ApiLoggingHandler(logService))
        {
            BaseAddress = new Uri(ApiPin.BaseUrl),
            Timeout = Timeout.InfiniteTimeSpan
        };
        var apiClient = new TeensyRomApiClient(httpClient);
        var deviceService = new DeviceService(apiClient);
        var startup = new ConnectionStartup(deviceService);

        services.AddSingleton<ITeensyRomApiClient>(apiClient);
        services.AddSingleton<IDeviceService>(deviceService);
        services.AddSingleton(startup);

        services.AddSingleton<ILoggingService>(logService);
        services.AddSingleton<ICliLoggingService>(logService);
        services.AddSingleton<IAlertService>(alertService);
        services.AddSingleton<ISettingsService>(settingsService);
        services.AddSingleton<IDeviceLogStream>(new DeviceLogStream(apiClient, deviceService, logService, httpClient.BaseAddress!));
        services.AddSingleton<IStorageService, StorageService>();
        services.AddSingleton<ILaunchService, LaunchService>();
        services.AddSingleton<IPlayerService, PlayerService>();
        services.AddSingleton<ITypeResolver, TypeResolver>();
        services.AddSingleton<IProgressTimer, ProgressTimer>();
        services.AddSingleton<ILaunchHistory, LaunchHistory>();

        return new CliComposition(services, startup);
    }

    public static CommandApp CreateApp(ITypeRegistrar registrar)
    {
        var app = new CommandApp(registrar);

        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.SetInterceptor(new CommandInterceptor());

            config.PropagateExceptions();

            config.SetApplicationName("TeensyROM.Cli".GetOsFriendlyPath());
            config.SetApplicationVersion($"Version: {AssemblyExtensions.GetVersion(Assembly.GetExecutingAssembly())}\r\nOS: {RuntimeInformation.OSDescription}\r\nArchitecture: { RuntimeInformation.OSArchitecture }");

            config.AddBranch<LaunchSettings>("launch", launch =>
            {
                launch.SetDefaultCommand<LaunchCommand>();
                config.AddExample(LaunchSettings.Example);
                launch.SetDescription(LaunchSettings.Description);
                launch.AddExample(LaunchSettings.Example);

                config.AddExample(RandomSettings.Example);
                launch.AddCommand<RandomCommand>("random")
                      .WithAlias("r")
                      .WithDescription(RandomSettings.Description)
                      .WithExample(RandomSettings.Example);

                config.AddExample(NavigateSettings.Example);
                launch.AddCommand<NavigateCommand>("nav")
                      .WithAlias("n")
                      .WithDescription(NavigateSettings.Description)
                      .WithExample(NavigateSettings.Example);

                config.AddExample(SearchSettings.Example);
                launch.AddCommand<SearchCommand>("search")
                      .WithAlias("s")
                      .WithDescription(SearchSettings.Description)
                      .WithExample(SearchSettings.Example);

                config.AddExample(FileLaunchSettings.Example);
                launch.AddCommand<FileLaunchCommand>("file")
                      .WithAlias("s")
                      .WithDescription(FileLaunchSettings.Description)
                      .WithExample(FileLaunchSettings.Example);

                launch.AddCommand<PlayerCommand>("player")
                      .IsHidden();
            });

            config.AddExample(IndexSettings.Example);
            config.AddCommand<IndexCommand>("index")
                  .WithAlias("i")
                  .WithDescription(IndexSettings.Description)
                  .WithExample(IndexSettings.Example);

            config.AddExample(PortListSettings.Example);
            config.AddCommand<PortListCommand>("ports")
                  .WithAlias("p")
                  .WithDescription(PortListSettings.Description)
                  .WithExample(PortListSettings.Example);

            config.AddExample(GeneratePresetsSettings.Example);
            config.AddCommand<GeneratePresetsCommand>("chipsynth")
                  .WithAlias("cs")
                  .WithDescription(GeneratePresetsSettings.Description)
                  .WithExample(GeneratePresetsSettings.Example);

            config.AddCommand<SettingsCommand>("settings")
                  .IsHidden();

            config.AddExample(CreditsSettings.Example);
            config.AddCommand<CreditsCommand>("credits")
                  .WithAlias("c")
                  .WithDescription(CreditsSettings.Description)
                  .WithExample(CreditsSettings.Example);

            var help = config.Settings.HelpProviderStyles;
            help!.Arguments!.Header = new Style(foreground: RadHelper.Theme.Secondary.Color);
            help!.Description!.Header = new Style(foreground: RadHelper.Theme.Secondary.Color);
            help!.Usage!.Header = new Style(foreground: RadHelper.Theme.Secondary.Color);
            help!.Options!.Header = new Style(foreground: RadHelper.Theme.Secondary.Color);
            help!.Examples!.Header = new Style(foreground: RadHelper.Theme.Secondary.Color);
            help!.Commands!.Header = new Style(foreground: RadHelper.Theme.Secondary.Color);
            config.Settings.HelpProviderStyles = help;
        });

        return app;
    }
}
