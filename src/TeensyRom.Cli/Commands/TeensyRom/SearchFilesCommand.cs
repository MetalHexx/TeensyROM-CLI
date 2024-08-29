using Spectre.Console;
using Spectre.Console.Cli;
using System.Reactive.Linq;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Serial.State;
using TeensyRom.Core.Storage.Entities;
using TeensyRom.Core.Storage.Services;

namespace TeensyRom.Cli.Commands.TeensyRom
{
    internal class SearchFilesCommand(ISerialStateContext serial, ICachedStorageService storage, ITypeResolver resolver) : AsyncCommand<SearchFilesCommandSettings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, SearchFilesCommandSettings settings)
        {
            var launchFileCommand = resolver.Resolve(typeof(LaunchFileConsoleCommand)) as LaunchFileConsoleCommand;

            if (launchFileCommand is null)
            {
                RadHelper.WriteError("Strange. Launch file command was not found.");
                return -1;
            }

            var connectionState = await serial.CurrentState.FirstAsync();
            
            if (connectionState is not SerialConnectedState)
            {
                RadHelper.WriteLine("Connecting to TeensyROM...");
                AnsiConsole.WriteLine();
                serial.OpenPort();
                RadHelper.WriteLine("Connection Successful!");
                RadHelper.WriteLine();
            }

            RadHelper.WriteHorizonalRule("Search Files", Justify.Left);

            AnsiConsole.MarkupLine($"{RadHelper.AddSecondaryColor("Tips:")}");
            AnsiConsole.MarkupLine($"{RadHelper.AddPrimaryColor("- Search terms match on: file name, file path, title, composer name (SID only) and HVSC STIL (SID Only).")}");
            AnsiConsole.MarkupLine($"{RadHelper.AddPrimaryColor("- Only directories visited will be included in search result.  Cache all files to avoid this.")}");
            AnsiConsole.WriteLine();

            var table = new Table()
                .BorderColor(RadHelper.Theme.Secondary.Color)
                .Border(TableBorder.Rounded)
                .AddColumn("Example")
                .AddColumn("Description")
                .AddRow(
                    RadHelper.AddHighlights($"iron maiden aces high"),
                    RadHelper.AddHighlights($"Searches for any term individually."))                
                .AddRow(
                    RadHelper.AddHighlights($"iron maiden \"aces high\""),
                    RadHelper.AddHighlights($"Search will consider phrases between qoutes as an individual search term."))
                .AddRow(
                    RadHelper.AddHighlights($"+iron maiden aces high"),
                    RadHelper.AddHighlights($"\"iron\" must have a match in every search result"))
                .AddRow(
                    RadHelper.AddHighlights($"\"aces high\" +\"iron maiden\""),
                    RadHelper.AddHighlights($"\"iron maiden\" must have a match in every search result"));

            AnsiConsole.Write(table);

            if (settings.StorageDevice.Equals(string.Empty))
            {
                settings.StorageDevice = PromptHelper.ChoicePrompt("Storage Type", ["SD", "USB"]);
                RadHelper.WriteLine();
            }
            var storageType = settings.StorageDevice.ToUpper() switch
            {
                "SD" => TeensyStorageType.SD,
                "USB" => TeensyStorageType.USB,
                _ => TeensyStorageType.SD, 
            };

            if (string.IsNullOrWhiteSpace(settings.Terms))
            {
                settings.Terms = PromptHelper.DefaultValueTextPrompt("Search Terms:", 2, "iron maiden aces high");
                RadHelper.WriteLine();
            }
            var searchResults = storage.Search(settings.Terms, []);

            if (!searchResults.Any()) 
            {
                RadHelper.WriteError("No files found with the specified search criteria");
                AnsiConsole.WriteLine();
            }

            var fileName = PromptHelper.ChoicePrompt("Select File", searchResults.Select(f => f.Name).ToList());
            var file = searchResults.First(f => f.Name == fileName);

            var launchFileCommandSettings = new LaunchFileCommandSettings
            {
                StorageDevice = settings.StorageDevice,
                FilePath = file.Path
            };

            await launchFileCommand.ExecuteAsync(context, launchFileCommandSettings);

            return 0;
        }
    }
}