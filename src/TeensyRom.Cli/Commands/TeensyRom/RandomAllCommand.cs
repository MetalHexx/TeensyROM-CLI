using MediatR;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Reactive.Linq;
using TeensyRom.Cli.Commands.Common;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Commands.File.LaunchFile;
using TeensyRom.Core.Logging;
using TeensyRom.Core.Serial.State;
using TeensyRom.Core.Settings;
using TeensyRom.Core.Storage.Entities;
using TeensyRom.Core.Storage.Services;

namespace TeensyRom.Cli.Commands.TeensyRom
{
    internal class RandomAllCommand(ISerialStateContext serial, ICachedStorageService storage, ILoggingService logService, ISettingsService settingsService, IMediator mediator) : AsyncCommand<RandomAllCommandSettings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, RandomAllCommandSettings settings)
        {            
            await serial.Connect();

            CommandHelper.DisplayCommandTitle("Play Random");

            var trSettings = await settingsService.Settings.FirstAsync();

            var storageType = CommandHelper.PromptForStorageType(settings.StorageDevice);
            settings.Directory = CommandHelper.PromptForDirectoryPath(settings.Directory, "/");
            var filterType = CommandHelper.PromptForFilterType(settings.Filter);
            var fileTypes = trSettings.GetFileTypes(filterType);

            var launchItem = storage.GetRandomFilePath(StorageScope.DirDeep, settings.Directory, fileTypes);

            if(launchItem is null) return 0;

            await CommandHelper.LaunchItem(mediator, storageType, launchItem);

            return 0;
        }
    }
}