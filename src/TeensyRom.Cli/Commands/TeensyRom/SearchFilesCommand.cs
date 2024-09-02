using Spectre.Console;
using Spectre.Console.Cli;
using System.Reactive.Linq;
using TeensyRom.Cli.Commands.Common;
using TeensyRom.Cli.Commands.TeensyRom.Services;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Serial.State;
using TeensyRom.Core.Storage.Entities;
using TeensyRom.Core.Storage.Services;

namespace TeensyRom.Cli.Commands.TeensyRom
{
    internal class SearchFilesCommand(ISerialStateContext serial, ICachedStorageService storage, ITypeResolver resolver, IPlayerService player) : AsyncCommand<SearchFilesCommandSettings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, SearchFilesCommandSettings settings)
        {
            player.StopStream();

            RadHelper.WriteMenu("Search Files", "Searches your storage devices.  Search will only include files that have been cached.", []);

            var table = new Table()
                .BorderColor(RadHelper.Theme.Secondary.Color)
                .Border(TableBorder.Rounded)
                .AddColumn("Search Example")
                .AddColumn("Description")
                .AddRow(
                    RadHelper.AddHighlights($"iron maiden aces high"),
                    RadHelper.AddHighlights($"Searches for any term individually."))                
                .AddRow(
                    RadHelper.AddHighlights($"iron maiden \"aces high\""),
                    RadHelper.AddHighlights($"Search will consider phrases between quotes as an individual search term."))
                .AddRow(
                    RadHelper.AddHighlights($"+iron maiden aces high"),
                    RadHelper.AddHighlights($"\"iron\" must have a match in every search result"))
                .AddRow(
                    RadHelper.AddHighlights($"\"aces high\" +\"iron maiden\""),
                    RadHelper.AddHighlights($"\"iron maiden\" must have a match in every search result"));

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();

            var storageType = CommandHelper.PromptForStorageType(settings.StorageDevice);

            storage.SwitchStorage(storageType);

            if (string.IsNullOrWhiteSpace(settings.Terms))
            {
                settings.Terms = PromptHelper.DefaultValueTextPrompt("Search Terms:", 2, "iron maiden aces high");
                RadHelper.WriteLine();
            }

            var validation = settings.Validate();

            if (!validation.Successful)
            {
                RadHelper.WriteError(validation?.Message ?? "Validation error");
                return 0;
            }

            var searchResults = storage.Search(settings.Terms, []);

            if (!searchResults.Any()) 
            {
                RadHelper.WriteError("No files found with the specified search criteria");
                AnsiConsole.WriteLine();
            }

            var fileName = PromptHelper.FilePrompt("Select File", searchResults.Select(f => f.Name).ToList());
            AnsiConsole.WriteLine();

            var file = searchResults.First(f => f.Name == fileName);

            await player.LaunchItem(storageType, file.Path);

            return 0;
        }
    }
}