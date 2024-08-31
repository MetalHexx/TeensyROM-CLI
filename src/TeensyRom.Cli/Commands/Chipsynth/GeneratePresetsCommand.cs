using System.Xml.Linq;
using Spectre.Console;
using Spectre.Console.Cli;
using TeensyRom.Cli.Commands.TeensyRom.Services;
using TeensyRom.Cli.Helpers;

namespace TeensyRom.Cli.Commands.Chipsynth
{
    internal class GeneratePresetsCommand(IPlayerService player) : Command<GeneratePresetsSettings>
    {
        public override int Execute(CommandContext context, GeneratePresetsSettings s)
        {
            player.StopContinuousPlay();

            RadHelper.WriteTitle("Chipsynth ASID Preset Generator");
            AnsiConsole.WriteLine();

            var proceed = RunWizard(s);

            if (!proceed)
            {
                RadHelper.WriteTitle("Preset Generation Cancelled");
                return 0;
            }
            RadHelper.WriteTitle("Preset Generation Starting");

            TransformPresets(s);

            RadHelper.WriteTitle("Preset Generation Completed");
            AnsiConsole.WriteLine();

            return 0;
        }

        public bool RunWizard(GeneratePresetsSettings s) 
        {
            var reRun = false;
            do
            {
                if (string.IsNullOrWhiteSpace(s.Clock))
                {
                    s.Clock = PromptHelper.ChoicePrompt("SID Clock", ["PAL", "NTSC"]);
                }
                if (string.IsNullOrEmpty(s.SourcePath))
                {
                    s.SourcePath = PromptHelper.DefaultValueTextPrompt("Source Path", 4, Directory.GetCurrentDirectory());
                }
                if (string.IsNullOrEmpty(s.TargetPath))
                {
                    s.TargetPath = PromptHelper.DefaultValueTextPrompt("Target Path", 1, "ASID");
                }
                OutputSettings(s);

                var proceed = PromptHelper.Confirm("Proceed with preset generation?", true);
                AnsiConsole.WriteLine();

                if (!proceed) return false;

                var validationResult = s.Validate();

                if (validationResult.Message is not null) 
                {
                    RadHelper.WriteError(validationResult.Message);
                    AnsiConsole.WriteLine();
                }
                reRun = !s.Validate().Successful || !EnsureTargetPath(s);

                if (reRun)
                {
                    s.SourcePath = string.Empty;
                    s.TargetPath = string.Empty;
                    reRun = true;
                }
            }
            while (reRun);            

            return true;            
        }

        private static bool EnsureTargetPath(GeneratePresetsSettings s)
        {
            var targetFullPath = Path.Combine(s.SourcePath, s.TargetPath);

            if (!Directory.Exists(targetFullPath))
            {
                try
                {
                    Directory.CreateDirectory(targetFullPath);
                }
                catch (Exception)
                {
                    AnsiConsole.WriteLine();
                    RadHelper.WriteError("Unable to create target path.");
                    AnsiConsole.WriteLine();
                    return false;
                }
            }
            return Directory.Exists(targetFullPath);
        }

        private static void OutputSettings(GeneratePresetsSettings s)
        {
            AnsiConsole.WriteLine();

            var table = new Table()
                .AddColumn("Setting")
                .AddColumn("Value")
                .AddRow(
                    RadHelper.AddSecondaryColor($"SID Clock"),
                    RadHelper.AddSecondaryColor(s.Clock.ToUpper()))
                .AddRow(
                    RadHelper.AddSecondaryColor($"Source Path"),
                    RadHelper.AddSecondaryColor(s.SourcePath))
                .AddRow(
                    RadHelper.AddSecondaryColor($"Target Path"),
                    RadHelper.AddSecondaryColor(Path.Combine(s.SourcePath, s.TargetPath)))
                .BorderColor(RadHelper.Theme.Primary.Color)
                .Border(TableBorder.Rounded);

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }

        private int TransformPresets(GeneratePresetsSettings s)
        {
            AnsiConsole.WriteLine();
            List<FileInfo> files = GetExistingPresets(s);

            if(files.Count == 0)
            {
                RadHelper.WriteError("No .fermatax files found.");
                return -1;
            }
            foreach (var file in files)
            {
                WritePreset(file, s);
            }  
            AnsiConsole.WriteLine();            
            return 0;
        }

        private static List<FileInfo> GetExistingPresets(GeneratePresetsSettings s)
        {
            List<FileInfo> files = [];

            var sourceDirectory = new DirectoryInfo(s.SourcePath);

            files.AddRange(sourceDirectory.GetFiles("*.fermatax", SearchOption.AllDirectories));

            return files;
        }

        private void WritePreset(FileInfo file, GeneratePresetsSettings s) 
        {
            var transformer = new PresetTransformer();

            if (file is null)
            {
                RadHelper.WriteError("File was null.");
                return;
            }            
            XDocument xmlDoc = XDocument.Load(file.FullName);

            xmlDoc = transformer.Transform(xmlDoc, s.Clock);

            if (file.DirectoryName is null)
            {
                RadHelper.WriteError("File Directory was null.");
                return;
            }
            var newBasePath = Path.Combine(s.SourcePath, s.TargetPath);
            var newDirectoryPath = file.DirectoryName.Replace(s.SourcePath, newBasePath);

            if (!Directory.Exists(newDirectoryPath))
            {
                Directory.CreateDirectory(newDirectoryPath);
            }
            var newFilePath = Path.Combine(newDirectoryPath, file.Name);

            RadHelper.WriteLine($"Writing {newFilePath}");
            xmlDoc.Save(newFilePath);
        }
    }
}