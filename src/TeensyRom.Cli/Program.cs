using Spectre.Console.Cli;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using TeensyRom.Cli;
using TeensyRom.Cli.Commands.Chipsynth;
using TeensyRom.Cli.Fonts;
using TeensyRom.Cli.Helpers;

RadHelper.RenderLogo("TeensyROM", FontConstants.FontPath);

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("TeensyROM.Cli");
    config.SetApplicationVersion("1.0.0");
    config.AddExample(["chipsynth", "transform"]);
    config.AddExample(["c", "t"]);
    config.AddExample(["c", "t", "-n", "-q"]);
    config.AddExample(["c", "t", "--ntsc", "-q"]);
    config.AddExample(["chipsynth", "transform", "-n"]);
    config.AddExample(["chipsynth", "transform", "-ntsc"]);


    config.AddBranch("chipsynth", transformCommand =>
    {
        transformCommand
            .AddCommand<TransformPatchesCommand>("transform")
            .WithAlias("t")
            .WithDescription("Transform Chipsynth ASID friendly patches.  Make sure to run this from your Chipsynth patch directory.  New transformed patches will be placed in /ASID")
            .WithExample(["transform", "chipsynth"])
            .WithExample(["t", "c"])
            .WithExample(["t", "c", "-n", "-q"])
            .WithExample(["t", "c", "--ntsc", "-q"])
            .WithExample(["transform", "chipsynth", "-n"])
            .WithExample(["transform", "chipsynth", "-ntsc"]);

    })
    .WithAlias("c");
});

app.Run(args);