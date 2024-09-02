using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Diagnostics;
using System.Reflection;
using TeensyRom.Cli.Commands.Chipsynth;
using TeensyRom.Cli.Commands.Common;
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
        logService.Enabled = false;
        var serial = new ObservableSerialPort(logService);
        var serialState = new SerialStateContext(serial);
        var settings = new SettingsService();
        var alertService = new AlertService();
        var gameService = new GameMetadataService(logService);
        var sidService = new SidMetadataService(settings);        

        UnpackAssets();

        services.AddSingleton<IObservableSerialPort>(serial);
        services.AddSingleton<ISerialStateContext>(serialState);
        services.AddSingleton<ILoggingService>(logService);
        services.AddSingleton<IAlertService>(alertService);
        services.AddSingleton<ISettingsService>(settings);
        services.AddSingleton<IGameMetadataService>(gameService);
        services.AddSingleton<ISidMetadataService>(sidService);
        services.AddSingleton<ICachedStorageService, CachedStorageService>();
        services.AddSingleton<IPlayerService, PlayerService>();
        services.AddSingleton<ITypeResolver, TypeResolver>();
        services.AddSingleton<IProgressTimer, ProgressTimer>();
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
            config.AddExample("random");
            config.AddExample("random -s -f=all");
            config.AddExample("launch");
            config.AddExample("launch -s sd -p /music/MUSICIANS/T/Tjelta_Geir/Artillery.sid");
            config.AddExample("navigate");           
            config.AddExample("navigate -s sd -p /music/MUSICIANS/T/Tjelta_Geir");
            config.AddExample("search");
            config.AddExample("search -s sd -t \"iron maiden aces high\"");
            config.AddExample("cache");
            config.AddExample("cache -s sd -p /music");
            config.AddExample("ports");
            config.AddExample(["chipsynth"]);
            config.AddExample(["cs"]);


            config.AddCommand<RandomStreamCommand>("random")
                    .WithAlias("r")
                    .WithDescription("Stream random files by file type and directory.")
                    .WithExample("random")
                    .WithExample("random -s -f=all -s=/games");

            config.AddCommand<LaunchFileConsoleCommand>("launch")
                    .WithAlias("l")
                    .WithDescription("Launch a file on TeensyROM")
                    .WithExample(["launch"]);

            config.AddCommand<NavigateStorageCommand>("navigate")
                    .WithAlias("n")
                    .WithDescription("Navigate through the storage directories and pick a file to launch.")
                    .WithExample(["nav -s SD -p /music/MUSICIANS/T/Tjelta_Geir/"]);

            config.AddCommand<SearchFilesCommand>("search")
                    .WithAlias("s")
                    .WithDescription("Search for launchable files on the TeensyROM.")
                    .WithExample(["search -s SD -t \"Iron Maiden Aces High\""]);

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
        });

        //logService.Logs.Subscribe(log => AnsiConsole.Markup($"{log}\r\n\r\n"));

        if (args.Contains("-h") || args.Contains("--help") || args.Contains("-v") || args.Contains("--version"))
        {
            app.Run(args);
            return -99;
        }

        var resultCode = 0;

        while (true)
        {
            try
            {
                if (args.Length > 0)
                {
                    resultCode = app.Run(args);
                }

                var menuChoice = PromptHelper.ChoicePrompt("Choose wisely", ["Random Stream", "Navigate Storage", "Launch File", "Search Files", "Cache Files", "List Ports", "Generate ChipSynth ASID Patches", "Leave"]);

                AnsiConsole.WriteLine();

                if (menuChoice == "Leave") return 0;

                args = menuChoice switch
                {
                    "Random Stream" => ["random"],
                    "Launch File" => ["launch"],
                    "Navigate Storage" => ["navigate"],
                    "Search Files" => ["search"],
                    "Cache Files" => ["cache"],
                    "List Ports" => ["ports"],
                    "Generate ChipSynth ASID Patches" => ["chipsynth"],
                    _ => []
                };
                app.Run(args);

                args = [];
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