using MediatR;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Collections.Generic;
using System.Reactive.Linq;
using TeensyRom.Cli.Commands.Common;
using TeensyRom.Cli.Commands.TeensyRom.Services;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Logging;
using TeensyRom.Core.Serial.State;
using TeensyRom.Core.Settings;
using TeensyRom.Core.Storage.Entities;
using TeensyRom.Core.Storage.Services;

namespace TeensyRom.Cli.Commands.TeensyRom
{
    internal class NavigateStorageCommand(ISerialStateContext serial, ICachedStorageService storage, ILoggingService logService, ITypeResolver resolver, IPlayerService player, ISettingsService settingsService) : AsyncCommand<NavigateStorageCommandSettings>
    {
        public override async Task<int> ExecuteAsync(CommandContext context, NavigateStorageCommandSettings settings)
        {
            player.StopStream();

            RadHelper.WriteMenu("Navigate Storage", "Navigate through the storage directories and pick a file to launch.",
            [
               "When launching a SID, or a timer is enabled, the next file in the directory will automatically play.",
               "Directory will be cached after first visit.",
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
            settings.StartingPath = CommandHelper.PromptForDirectoryPath(settings.StartingPath);

            var cacheItem = await storage.GetDirectory(settings.StartingPath);

            if (cacheItem is null || (!cacheItem.Files.Any() && !cacheItem.Directories.Any()))
            {
                RadHelper.WriteError($"No directories or files found in {storageType.ToString()} at path {settings.StartingPath}");
                AnsiConsole.WriteLine();
                return 0;
            }

            var directories = PrepareDirectories(cacheItem.Directories);

            IEnumerable<StorageItem> storageItems = directories.Concat(cacheItem.Files.Cast<StorageItem>());

            var selectedFile = await TraverseStorage(storageItems, settings.StartingPath);

            if (selectedFile is null) 
            {
                RadHelper.WriteError("Directory or files not found.");
                AnsiConsole.WriteLine();
                return 0;
            }
            AnsiConsole.WriteLine();

            await player.LaunchItem(storageType, selectedFile.Path);

            return 0;
        }

        public async Task<FileItem?> TraverseStorage(IEnumerable<StorageItem> items, string targetDirectory)
        {
            var storageItemName = PromptHelper.FilePrompt(targetDirectory, items.Select(s => s.Name).ToList());

            var selectedStorageItem = items.First(s => s.Name == storageItemName);

            if (selectedStorageItem is FileItem file) 
            {
                return file;
            }
            var nextDirectory = selectedStorageItem.Path;

            var cacheItem = await storage.GetDirectory(nextDirectory);

            if (cacheItem is null) 
            {
                RadHelper.WriteError("Directory not found.");
                AnsiConsole.WriteLine();
                return null;
            }
            IEnumerable<StorageItem> directories = PrepareDirectories(cacheItem.Directories);

            IEnumerable<StorageItem> storageItems = directories.Concat(cacheItem.Files.Cast<StorageItem>());

            return await TraverseStorage(storageItems, nextDirectory);
        }

        public List<DirectoryItem> PrepareDirectories(IEnumerable<DirectoryItem> directories) 
        {
            return directories
                .Select(d => d.Clone())
                .Select(d => 
                {
                    d.Name = $"/{d.Name}";
                    return d;
                })
                .ToList();
        }
    }
}