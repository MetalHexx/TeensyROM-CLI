using Spectre.Console;
using Spectre.Console.Cli;
using System.Reactive.Linq;
using TeensyRom.Cli.Commands.Common;
using TeensyRom.Cli.Commands.TeensyRom.Services;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Player;
using TeensyRom.Core.Serial.State;
using TeensyRom.Core.Settings;
using TeensyRom.Core.Storage.Entities;
using TeensyRom.Core.Storage.Services;

namespace TeensyRom.Cli.Commands.TeensyRom
{
    internal class SearchFilesCommand(ISerialStateContext serial, ICachedStorageService storage, ITypeResolver resolver, IPlayerService player, ISettingsService settingsService) : AsyncCommand<SearchFilesCommandSettings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, SearchFilesCommandSettings settings)
        {
            var playerCommand = resolver.Resolve(typeof(PlayerCommand)) as PlayerCommand;

            player.StopStream();

            RadHelper.WriteMenu("Search Files", "Searches your storage devices.  Search will only include files that have been cached.", 
            [

                "Cache files to fatten your search. ;)",
            ]);
            RadHelper.WriteHelpTable(("SearchExample", "Description"), 
            [
                ("iron maiden aces high", "Searches for any term individually."),
                ("iron maiden \"aces high\"", "Search will consider phrases between quotes as an individual search term."),
                ("+iron maiden aces high", "\"iron\" must have a match in every search result"),
                ("\"aces high\" +\"iron maiden\"", "\"iron maiden\" must have a match in every search result"),
            ]);

            var globalSettings = settingsService.GetSettings();

            settings.StorageDevice = string.IsNullOrWhiteSpace(settings.StorageDevice)
                ? globalSettings.StorageType.ToString()
                : settings.StorageDevice;

            var storageType = CommandHelper.PromptForStorageType(settings.StorageDevice, promptAlways: globalSettings.AlwaysPromptStorage);

            if (globalSettings.AlwaysPromptStorage) 
            {
                storage.SwitchStorage(storageType);
            }

            if (string.IsNullOrWhiteSpace(settings.Query))
            {
                settings.Query = PromptHelper.DefaultValueTextPrompt("Search Terms:", 2, "iron maiden aces high");
                RadHelper.WriteLine();
            }

            var validation = settings.Validate();

            if (!validation.Successful)
            {
                RadHelper.WriteError(validation?.Message ?? "Validation error");
                return 0;
            }

            var searchResults = storage.Search(settings.Query, []);

            if (!searchResults.Any()) 
            {
                RadHelper.WriteError("No files found with the specified search criteria");
                AnsiConsole.WriteLine();
            }

            var fileName = PromptHelper.FilePrompt("Select File", searchResults
                .Select(f => f.Name)
                .ToList());

            AnsiConsole.WriteLine();

            var file = searchResults.First(f => f.Name == fileName);
            player.SetSearchMode(settings.Query);
            await player.LaunchItem(storageType, file.Path);

            if (playerCommand is not null)
            {
                await playerCommand.ExecuteAsync(context, new());
            }
            return 0;
        }
    }
}