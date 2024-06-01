using System.Xml.Linq;
using Spectre.Console;
using Spectre.Console.Cli;
using TeensyRom.Cli.Helpers;

namespace TeensyRom.Cli.Commands.Chipsynth
{
    internal class TransformPatchesCommand : Command<TransformPatchesSettings>
    {
        public override int Execute(CommandContext context, TransformPatchesSettings s)
        {
            RadHelper.WriteTitle("Chipsynth C64 ASID Patch Generator");
            AnsiConsole.WriteLine();

            var proceed = RunWizard(s);

            if (!proceed)
            {
                RadHelper.WriteTitle("Patch Generation Cancelled");
                return 0;
            }
            RadHelper.WriteTitle("Patch Generation Starting");

            TransformPatches(s);

            RadHelper.WriteTitle("Patch Generation Completed");

            return 0;
        }

        public bool RunWizard(TransformPatchesSettings s) 
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

                var proceed = PromptHelper.Confirm("Proceed with patch generation?", true);
                AnsiConsole.WriteLine();

                if (!proceed) return false;

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

        private static bool EnsureTargetPath(TransformPatchesSettings s)
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

        private static void OutputSettings(TransformPatchesSettings s)
        {
            AnsiConsole.WriteLine();

            var table = new Table()
                .AddColumn("Setting")
                .AddColumn("Value")
                .AddRow(
                    RadHelper.AddHighlights($"SID Clock"),
                    RadHelper.AddHighlights(s.Clock.ToUpper()))
                .AddRow(
                    RadHelper.AddHighlights($"Source Path"),
                    RadHelper.AddHighlights(s.SourcePath))
                .AddRow(
                    RadHelper.AddHighlights($"Target Path"),
                    RadHelper.AddHighlights(Path.Combine(s.SourcePath, s.TargetPath)))
                .BorderColor(RadHelper.Theme.Secondary.Color)
                .Border(TableBorder.Rounded);

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }

        private int TransformPatches(TransformPatchesSettings s)
        {
            List<FileInfo> files = GetExistingPatches(s);

            if(files.Count == 0)
            {
                RadHelper.WriteError("No .fermatax files found.");
                return -1;
            }
            foreach (var file in files)
            {
                WritePatch(file, s);
            }            
            return 0;
        }

        private static List<FileInfo> GetExistingPatches(TransformPatchesSettings s)
        {
            List<FileInfo> files = [];

            var sourceDirectory = new DirectoryInfo(s.SourcePath);

            files.AddRange(sourceDirectory.GetFiles("*.fermatax", SearchOption.AllDirectories));

            return files;
        }

        private void WritePatch(FileInfo file, TransformPatchesSettings s) 
        {
            var transformer = new PatchTransformer();

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