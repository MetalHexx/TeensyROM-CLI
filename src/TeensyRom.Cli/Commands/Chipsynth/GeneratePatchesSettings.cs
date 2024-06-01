using System.ComponentModel;
using System.Text.Json.Serialization;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;

namespace TeensyRom.Cli.Commands.Chipsynth
{
    internal class GeneratePatchesSettings : CommandSettings
    {
        [JsonIgnore]
        [Description("The SID clock to generate.")]
        [CommandOption("-c|--clock")]
        public string Clock { get; set; } = string.Empty;

        [JsonIgnore]
        [Description("The source path of the Chipsynth C64 patches.  Must be an absolute path.")]
        [CommandOption("-s|--source")]
        public string SourcePath { get; set; } = string.Empty;

        [JsonIgnore]
        [Description("The target path of the Chipsynth C64 patches.  This will be relative to the source path.")]
        [CommandOption("-t|--target")]
        public string TargetPath { get; set; } = string.Empty;

        public bool RunWizard { get; set; } = false;

        public override ValidationResult Validate()
        {
            var validSource = string.IsNullOrWhiteSpace(SourcePath) || Directory.Exists(SourcePath);

            if (!validSource)
            {
                return ValidationResult.Error($"The source path '{SourcePath}' does not exist.");
            }
            var validTarget = string.IsNullOrWhiteSpace(TargetPath) || !Path.IsPathRooted(TargetPath);

            if (!validTarget)
            {
                return ValidationResult.Error($"The target path '{TargetPath}' is not a relative path.");
            }
            var validClock = string.IsNullOrWhiteSpace(Clock) || Clock.Equals("PAL", StringComparison.OrdinalIgnoreCase) || Clock.Equals("NTSC", StringComparison.OrdinalIgnoreCase);
            
            if (!validClock)
            {
                return ValidationResult.Error($"The clock '{Clock}' is not valid.  Must be 'PAL' or 'NTSC'.");
            }
            return base.Validate();            
        }
    }
}
