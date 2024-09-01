using MediatR;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Reactive.Linq;
using TeensyRom.Cli.Commands.Common;
using TeensyRom.Cli.Commands.TeensyRom.Services;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Logging;
using TeensyRom.Core.Serial.State;
using TeensyRom.Core.Storage.Entities;
using TeensyRom.Core.Storage.Services;

namespace TeensyRom.Cli.Commands.TeensyRom
{
    internal class ListFilesCommand(ISerialStateContext serial, ICachedStorageService storage, ILoggingService logService, ITypeResolver resolver, IPlayerService player) : AsyncCommand<ListFilesCommandSettings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, ListFilesCommandSettings settings)
        {
            player.StopStream();

            RadHelper.WriteMenu("List Directory", "Navigate to a directory and pick a file to launch.",
            [
               "When a SID ends, a random SID stream will begin.",
               "Games will not start a stream (for now)",
               "Directory will be cached after first visit.",
            ]);

            var storageType = CommandHelper.PromptForStorageType(settings.StorageDevice);
            settings.FilePath = CommandHelper.PromptForDirectoryPath(settings.FilePath);

            if (!settings.ValidateSettings()) return -1;

            var cacheItem = await storage.GetDirectory(settings.FilePath);

            if (cacheItem is null || !cacheItem.Files.Any())
            {
                RadHelper.WriteError("Directory or files not found.");
                AnsiConsole.WriteLine();
                return 0;
            }

            var fileName = PromptHelper.FilePrompt("Select File", cacheItem.Files.Select(f => f.Name).ToList());
            var file = cacheItem.Files.First(f => f.Name == fileName);

            await player.LaunchItem(storageType, file.Path);

            return 0;
        }
    }
}