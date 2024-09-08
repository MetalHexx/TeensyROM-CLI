using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Help;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using TeensyRom.Cli.Commands.Chipsynth;
using TeensyRom.Cli.Commands.Common;
using TeensyRom.Cli.Commands.Main.Launcher;
using TeensyRom.Cli.Commands.TeensyRom;
using TeensyRom.Cli.Commands.TeensyRom.Services;
using TeensyRom.Cli.Fonts;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core;
using TeensyRom.Core.Assets;
using TeensyRom.Core.Common;
using TeensyRom.Core.Games;
using TeensyRom.Core.Logging;
using TeensyRom.Core.Music;
using TeensyRom.Core.Music.Sid;
using TeensyRom.Core.Progress;
using TeensyRom.Core.Serial;
using TeensyRom.Core.Serial.State;
using TeensyRom.Core.Settings;
using TeensyRom.Core.Storage.Services;
public class Program
{
    private static int Main(string[] args)
    {
        AnsiConsole.WriteLine();
        RadHelper.RenderLogo("TeensyROM", FontConstants.FontPath);

        var services = new ServiceCollection();
        var logService = new CliLoggingService();
        var alertService = new CliAlertService();        
        var serial = new ObservableSerialPort(logService, alertService);
        var serialState = new SerialStateContext(serial);
        var settingsService = new SettingsService();        
        var gameService = new GameMetadataService(logService);
        var sidService = new SidMetadataService(settingsService);        

        var settings = settingsService.GetSettings();
        logService.Enabled = settings.EnableDebugLogs;

        //UnpackAssets();

        services.AddSingleton<IObservableSerialPort>(serial);
        services.AddSingleton<ISerialStateContext>(serialState);
        services.AddSingleton<ILoggingService>(logService);
        services.AddSingleton<ICliLoggingService>(logService);
        services.AddSingleton<IAlertService>(alertService);
        services.AddSingleton<ISettingsService>(settingsService);
        services.AddSingleton<IGameMetadataService>(gameService);
        services.AddSingleton<ISidMetadataService>(sidService);
        services.AddSingleton<ICachedStorageService, CachedStorageService>();
        services.AddSingleton<IPlayerService, PlayerService>();
        services.AddSingleton<ITypeResolver, TypeResolver>();
        services.AddSingleton<IProgressTimer, ProgressTimer>();
        services.AddSingleton<ILaunchHistory, LaunchHistory>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CoreAssemblyMarker>());
        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(ExceptionBehavior<,>));
        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(SerialBehavior<,>));

        var registrar = new TypeRegistrar(services);
        
        var app = new CommandApp(registrar);

        app.Configure(config =>
        {
            config.PropagateExceptions();
            config.SetInterceptor(new CommandInterceptor(serialState));

            config.PropagateExceptions();

            config.SetApplicationName("TeensyROM.Cli");
            config.SetApplicationVersion("1.0.0");

            config.AddBranch<LaunchSettings>("launch", launch =>
            {
                launch.SetDefaultCommand<LaunchCommand>();

                launch.AddCommand<RandomCommand>("random")
                      .WithAlias("r")
                      .WithDescription(RandomSettings.Description)
                      .WithExample(RandomSettings.Example);


                launch.AddCommand<NavigateCommand>("nav")
                      .WithAlias("n")
                      .WithDescription(NavigateSettings.Description)
                      .WithExample(NavigateSettings.Example);

                launch.AddCommand<SearchCommand>("search")
                      .WithAlias("s")
                      .WithDescription(SearchSettings.Description)
                      .WithExample(SearchSettings.Example);

                launch.AddCommand<FileLaunchCommand>("file")
                      .WithAlias("s")
                      .WithDescription(FileLaunchSettings.Description)
                      .WithExample(FileLaunchSettings.Example);



                launch.AddCommand<TeensyRom.Cli.Commands.Main.Launcher.PlayerCommand>("player");
        });

            var help = config.Settings.HelpProviderStyles;
            help!.Arguments!.Header = new Style(foreground: RadHelper.Theme.Secondary.Color);
            help!.Description!.Header = new Style(foreground: RadHelper.Theme.Secondary.Color);
            help!.Usage!.Header = new Style(foreground: RadHelper.Theme.Secondary.Color);
            help!.Options!.Header = new Style(foreground: RadHelper.Theme.Secondary.Color);
            help!.Examples!.Header = new Style(foreground: RadHelper.Theme.Secondary.Color);
            help!.Commands!.Header = new Style(foreground: RadHelper.Theme.Secondary.Color);
            config.Settings.HelpProviderStyles = help;

            config.AddExample(RandomSettings.Example);                        
            config.AddExample(FileLaunchSettings.Example);            
            config.AddExample(NavigateSettings.Example);            
            config.AddExample(SearchSettings.Example);

            //config.AddExample("cache");
            //config.AddExample("cache -s sd -p /music");
            //config.AddExample("ports");
            //config.AddExample(["chipsynth"]);
            //config.AddExample(["cs"]);


            config.AddCommand<CacheCommand>("cache")
                    .WithAlias("c")
                    .WithDescription("Caches all the files on your storage device to enhance search and streaming features.")
                    .WithExample(["cache -s sd -p /music/ "]);

            config.AddCommand<PortListCommand>("ports")
                    .WithAlias("p")
                    .WithDescription("Lists all COM ports for troubleshooting purposes.")
                    .WithExample(["ports"]);

            config.AddCommand<GeneratePresetsCommand>("chipsynth")
                    .WithAlias("cs")
                    .WithDescription("Generate ASID friendly Chipsynth ASID presets.")
                    .WithExample(["chipsynth"])
                    .WithExample(["cs"])
                    .WithExample(["cs", "--source c:\\your\\preset\\directory", "--target ASID --clock ntsc"]);

            config.AddCommand<SettingsCommand>("settings")
                    .WithDescription("Change your global settings.")
                    .WithExample(["settings"]);

        });

        //logService.Logs.Subscribe(log => AnsiConsole.Markup($"{log}\r\n\r\n"));

        var resultCode = 0;

        while (true)
        {
            try
            {
                if (args.Contains("-h") || args.Contains("--help") || args.Contains("-v") || args.Contains("--version"))
                {
                    app.Run(args);
                    return -99;
                }

                if (args.Length > 0)
                {
                    var help = args.ToList();
                    help.Add("-h");
                    AnsiConsole.WriteLine();

                    resultCode = app.Run(help.ToArray());
                    resultCode = app.Run(args);
                    args = [];
                }

                var menuChoice = PromptHelper.ChoicePrompt("Choose wisely", ["Launch", "Settings", "Cache Files", "List Ports", "Generate ASID Patches", "Leave"]);

                AnsiConsole.WriteLine();

                if (menuChoice == "Leave") return 0;

                args = menuChoice switch
                {   
                    "Launch" => ["launch"],    
                    "Cache Files" => ["cache"],
                    "Settings" => ["settings"],
                    "List Ports" => ["ports"],
                    "Generate ChipSynth ASID Patches" => ["chipsynth"],
                    _ => []
                };
                app.Run(args);

                args = [];
            }
            catch(CommandParseException ex)
            {
                RadHelper.WriteError(ex.Message);
                AnsiConsole.WriteLine();
                args = ["-h"];
            }
            catch (TeensyStateException ex)
            {
                RadHelper.WriteError(ex.Message);
                continue;
            }
            catch (TeensyBusyException ex)
            {
                RadHelper.WriteError(ex.Message);
                continue;
            }
            catch (Exception ex)
            {
                AnsiConsole.WriteException(ex, ExceptionFormats.Default);
                LogExceptionToFile(ex);
                continue;
            }
            if (resultCode == -1) return resultCode;
        }
    }

    private static void UnpackAssets()
    {

        AssetHelper.UnpackAssets(GameConstants.Game_Image_Local_Path, "OneLoad64.zip");
        AssetHelper.UnpackAssets(MusicConstants.Musician_Image_Local_Path, "Composers.zip");
        AssetHelper.UnpackAssets(AssetConstants.VicePath, "vice-bins.zip");
    }

    private static readonly object _logFileLock = new object();

    private static void LogExceptionToFile(Exception ex)
    {
        string filePath = Path.Combine(Assembly.GetExecutingAssembly().GetPath(), @"Assets\System\Logs\UnhandledErrorLogs.txt");

        if (!Directory.Exists(Path.GetDirectoryName(filePath)))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        }

        try
        {
            lock (_logFileLock)
            {
                File.AppendAllText(filePath, $"{DateTime.Now}{Environment.NewLine}Exception: {ex}{Environment.NewLine}{Environment.NewLine}");
            }
        }
        catch (Exception logEx)
        {
            Debug.WriteLine("Failed to log exception: " + logEx.Message);
        }
    }
}