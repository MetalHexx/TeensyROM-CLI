using Spectre.Console;
using Spectre.Console.Cli;
using TeensyRom.Cli.Commands.Chipsynth;
using TeensyRom.Cli.Fonts;
using TeensyRom.Cli.Helpers;

AnsiConsole.WriteLine();
RadHelper.RenderLogo("TeensyROM", FontConstants.FontPath);

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("TeensyROM.Cli");
    config.SetApplicationVersion("1.0.0");
    config.AddExample(["chipsynth"]);
    config.AddExample(["cs"]);
    config.AddCommand<GeneratePresetsCommand>("chipsynth")
            .WithAlias("cs")
            .WithDescription("Generate ASID friendly Chipsynth ASID presets.")
            .WithExample(["chipsynth"])
            .WithExample(["cs"])
            .WithExample(["cs", "--source c:\\your\\preset\\directory", "--target ASID --clock ntsc"]);
});

app.Run(args);