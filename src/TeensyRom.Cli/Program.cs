using Spectre.Console;
using Spectre.Console.Cli;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using TeensyRom.Cli;
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
    config.AddExample(["generate patches"]);
    config.AddExample(["g p"]);

    config.AddBranch("generate", transformCommand =>
    {
        transformCommand
            .AddCommand<GeneratePatchesCommand>("patches")
            .WithAlias("p")
            .WithDescription("Generate Chipsynth ASID friendly patches.")
            .WithExample(["generate", "patches"])
            .WithExample(["g", "p"])
            .WithExample(["g", "p", "--source c:\\patch\\directory", "--target ASID --clock ntsc"]);
    })
    .WithAlias("g");
});

app.Run(args);