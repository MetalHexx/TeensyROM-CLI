using System.ComponentModel;
using System.Text.Json.Serialization;
using Spectre.Console.Cli;

namespace TeensyRom.Cli.Commands.Chipsynth
{
    internal class TransformPatchesSettings : CommandSettings
    {
        public static class Defaults
        {
            public const string Clock = "PAL";
        }


        [JsonIgnore]
        [Description("Set SID clock to NTSC")]
        [CommandOption("-c|--clock")]
        [DefaultValue(Defaults.Clock)]
        public string Clock { get; set; } = string.Empty;
    }
}
