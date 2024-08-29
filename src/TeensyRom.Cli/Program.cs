using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;
using TeensyRom.Cli.Commands.Chipsynth;
using TeensyRom.Cli.Commands.TeensyRom;
using TeensyRom.Cli.Fonts;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core;
using TeensyRom.Core.Assets;
using TeensyRom.Core.Common;
using TeensyRom.Core.Games;
using TeensyRom.Core.Logging;
using TeensyRom.Core.Music;
using TeensyRom.Core.Music.Sid;
using TeensyRom.Core.Serial;
using TeensyRom.Core.Serial.State;
using TeensyRom.Core.Settings;
using TeensyRom.Core.Storage.Services;

public class Program
{
    private static void Main(string[] args)
    {
        AnsiConsole.WriteLine();
        RadHelper.RenderLogo("TeensyROM", FontConstants.FontPath);

        var services = new ServiceCollection();
        var loggingStrategy = new CliLogColorStrategy();
        var logService = new LoggingService(loggingStrategy);
        var serial = new ObservableSerialPort(logService);
        var serialState = new SerialStateContext(serial);

        UnpackAssets();

        services.AddSingleton<IObservableSerialPort>(serial);
        services.AddSingleton<ISerialStateContext>(serialState);
        services.AddSingleton<ILogColorStategy>(loggingStrategy);
        services.AddSingleton<ILoggingService>(logService);
        services.AddSingleton<IAlertService, TeensyRom.Cli.Services.AlertService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IGameMetadataService, GameMetadataService>();
        services.AddSingleton<ISidMetadataService, SidMetadataService>();
        services.AddSingleton<ICachedStorageService, CachedStorageService>();
        services.AddSingleton<ITypeResolver, TypeResolver>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<CoreAssemblyMarker>());
        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(ExceptionBehavior<,>));
        services.AddSingleton(typeof(IPipelineBehavior<,>), typeof(SerialBehavior<,>));

        var registrar = new TypeRegistrar(services);

        var app = new CommandApp(registrar);

        app.Configure(config =>
        {
            config.SetApplicationName("TeensyROM.Cli");
            config.SetApplicationVersion("1.0.0");
            config.AddExample(["chipsynth"]);
            config.AddExample(["cs"]);
            config.AddExample("launch");
            config.AddExample("launch -s sd -p /music/MUSICIANS/T/Tjelta_Geir/Artillery.sid");
            config.AddExample("list");           
            config.AddExample("list -s sd -p /music/MUSICIANS/T/Tjelta_Geir");
            config.AddExample("search");
            config.AddExample("search -s sd -t \"iron maiden aces high\"");
            config.AddExample("cache");
            config.AddExample("cache -s sd -p /music");
            config.AddExample("ports");

            config.AddCommand<LaunchFileConsoleCommand>("launch")
                    .WithAlias("l")
                    .WithDescription("Launch a file on TeensyROM")
                    .WithExample(["launch"]);

            config.AddCommand<ListFilesCommand>("list")
                    .WithAlias("f")
                    .WithDescription("List all files available to launch in a directory on the TeensyROM")
                    .WithExample(["list -s SD -p /music/MUSICIANS/T/Tjelta_Geir/"]);

            config.AddCommand<SearchFilesCommand>("search")
                    .WithAlias("s")
                    .WithDescription("Search for launchable files on the TeensyROM.")
                    .WithExample(["search -s SD -t \"Iron Maiden Aces High\""]);

            config.AddCommand<CacheCommand>("cache")
                    .WithAlias("c")
                    .WithDescription("Caches all the files on your storage device for increased performance, search, and continuous \\ random play.")
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

        logService.Logs.Subscribe(log => AnsiConsole.Markup($"{log}\r\n\r\n"));

        if (args.Contains("-h") || args.Contains("--help") || args.Contains("-v") || args.Contains("--version"))
        {
            app.Run(args);
            return;
        }

        while (true) 
        {
            if (args.Length > 0)
            {
                app.Run(args);                
            }

            var menuChoice = PromptHelper.ChoicePrompt("Choose wisely", ["Launch File", "List Files", "Search Files", "Cache Files", "List Ports", "Generate ChipSynth ASID Patches", "Leave"]);

            AnsiConsole.WriteLine();

            if (menuChoice == "Leave") return;

            args = menuChoice switch
            {
                "Launch File" => ["launch"],
                "List Files" => ["list"],
                "Search Files" => ["search"],
                "Cache Files" => ["cache"],
                "List Ports" => ["ports"],
                "Generate ChipSynth ASID Patches" => ["chipsynth"],
                _ => []
            };
            app.Run(args);
            args = [];
        }
    }

    private static void UnpackAssets()
    {
        
        AssetHelper.UnpackAssets(GameConstants.Game_Image_Local_Path, "OneLoad64.zip");
        AssetHelper.UnpackAssets(MusicConstants.Musician_Image_Local_Path, "Composers.zip");
        AssetHelper.UnpackAssets(AssetConstants.VicePath, "vice-bins.zip");
    }

}