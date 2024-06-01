using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            s.Clock = PromptHelper.ChoicePrompt("SID Clock", ["PAL", "NTSC"]);

            return TransformPatches(s);
        }

        public int TransformPatches(TransformPatchesSettings s)
        {
            string fullCurrentAssemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            List<FileInfo> files = [];
            string? directoryName = Path.GetDirectoryName(fullCurrentAssemblyPath);

            if (directoryName is null)
            {
                Console.WriteLine("Directory Name was null.");
                return -1;
            }
            var directory = new DirectoryInfo(directoryName);
            files.AddRange(directory.GetFiles("*.fermatax", SearchOption.AllDirectories));

            if (!Directory.Exists("ASID"))
            {
                Directory.CreateDirectory("ASID");
            }
            var newBasePath = Path.Combine(directory.FullName, "ASID");
            bool isNtsc = s.Clock.Equals("NTSC", StringComparison.OrdinalIgnoreCase);
            var transformer = new PatchTransformer();

            foreach (var file in files)
            {
                if (file is null)
                {
                    RadHelper.WriteError("File was null.");
                    continue;
                }
                XDocument xmlDoc = XDocument.Load(file.FullName);
                xmlDoc = transformer.Transform(xmlDoc, isNtsc);

                if (file.DirectoryName is null)
                {
                    RadHelper.WriteError("File Directory was null.");
                    continue;
                }

                var newDirectoryPath = file.DirectoryName.Replace(directory.FullName, newBasePath);

                if (!Directory.Exists(newDirectoryPath))
                {
                    Directory.CreateDirectory(newDirectoryPath);
                }
                var newFilePath = Path.Combine(newDirectoryPath, file.Name);
                RadHelper.WriteLine($"Writing {newFilePath}");
                xmlDoc.Save(newFilePath);
            }
            RadHelper.WriteTitle("Patch Updates Completed.");
            return 0;
        }
    }
}