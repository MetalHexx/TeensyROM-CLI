using System.ComponentModel;
using System.Text.Json.Serialization;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Rendering;

namespace TeensyRom.Cli.Commands.Chipsynth
{
    internal class GeneratePresetsSettings : CommandSettings
    {
        [JsonIgnore]
        [Description("SID Clock that matches your machine.")]
        [CommandOption("-c|--clock")]
        public string Clock { get; set; } = string.Empty;

        [JsonIgnore]
        [Description("Source path of the Chipsynth C64 presets. (Absolute Path)")]
        [CommandOption("-s|--source")]
        public string SourcePath { get; set; } = string.Empty;

        [JsonIgnore]
        [Description("Target path of the Chipsynth C64 presets. (Relative Path)")]
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
                return ValidationResult.Error($"The target path '{TargetPath}' must be a relative path.");
            }
            var validClock = string.IsNullOrWhiteSpace(Clock) || Clock.Equals("PAL", StringComparison.OrdinalIgnoreCase) || Clock.Equals("NTSC", StringComparison.OrdinalIgnoreCase);
            
            if (!validClock)
            {
                return ValidationResult.Error($"The clock '{Clock}' must be 'PAL' or 'NTSC'.");
            }
            return base.Validate();            
        }
    }
}
