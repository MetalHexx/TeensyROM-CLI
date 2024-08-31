using MediatR;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Reactive.Linq;
using TeensyRom.Cli.Commands.Common;
using TeensyRom.Cli.Commands.TeensyRom.Services;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Commands.File.LaunchFile;
using TeensyRom.Core.Logging;
using TeensyRom.Core.Serial.State;
using TeensyRom.Core.Settings;
using TeensyRom.Core.Storage.Entities;
using TeensyRom.Core.Storage.Services;

namespace TeensyRom.Cli.Commands.TeensyRom
{
    internal class RandomAllCommand(ISerialStateContext serial, ICachedStorageService storage, ILoggingService logService, ISettingsService settingsService, IPlayerService musicService) : AsyncCommand<RandomAllCommandSettings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, RandomAllCommandSettings settings)
        {            
            await serial.Connect();

            RadHelper.WriteMenu("Launch Random", "Launch random files from storage and discover something new.",
            [
               "Filter will limit the file types selected.",
               "SIDs will play continuously.",
               "Games will stop continuous play.",
               "For best result, cache your storage."
            ]);

            var trSettings = await settingsService.Settings.FirstAsync();

            var storageType = CommandHelper.PromptForStorageType(settings.StorageDevice);
            settings.Directory = CommandHelper.PromptForDirectoryPath(settings.Directory, "/");
            var filterType = CommandHelper.PromptForFilterType(settings.Filter);
            var fileTypes = trSettings.GetFileTypes(filterType);

            var launchItem = storage.GetRandomFilePath(StorageScope.DirDeep, settings.Directory, fileTypes);

            if(launchItem is null) return 0;

            await musicService.LaunchItem(storageType, launchItem);

            return 0;
        }
    }
}