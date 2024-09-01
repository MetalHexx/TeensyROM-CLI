using MediatR;
using Microsoft.VisualBasic;
using Spectre.Console;
using System.Reactive.Linq;
using System.Runtime;
using TeensyRom.Cli.Commands.Common;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Commands.File.LaunchFile;
using TeensyRom.Core.Common;
using TeensyRom.Core.Player;
using TeensyRom.Core.Progress;
using TeensyRom.Core.Serial.State;
using TeensyRom.Core.Settings;
using TeensyRom.Core.Storage.Entities;
using TeensyRom.Core.Storage.Services;

namespace TeensyRom.Cli.Commands.TeensyRom.Services
{
    internal interface IPlayerService 
    {
        Task LaunchItem(TeensyStorageType storageType, ILaunchableItem item);
        Task LaunchItem(TeensyStorageType storageType, string path);
        Task PlayRandom(TeensyStorageType storageType, string scopePath, TeensyFilterType filterType);
        void StopStream();
    }


    internal class PlayerService(IMediator mediator, ICachedStorageService storage, IProgressTimer progressTimer, ISerialStateContext serial, ISettingsService settingsService) : IPlayerService
    {
        private TeensyStorageType _selectedStorage = TeensyStorageType.SD;
        private StorageScope _selectedScope = StorageScope.DirDeep;
        private string _scopePath = "/";
        private string _path = "/";

        private PlayState _playState = PlayState.Stopped;
        private PlayMode _playMode = PlayMode.Shuffle;
        private TeensyFilterType _filterType = TeensyFilterType.All;

        private IDisposable? _progressSubscription;

        public async Task LaunchItem(TeensyStorageType storageType, string path) 
        {
            _playMode = PlayMode.Normal;

            var directory = await storage.GetDirectory(path.GetUnixParentPath());

            if (directory is null) 
            {
                RadHelper.WriteError("File not found.");
                AnsiConsole.WriteLine();
                return;
            }
            var fileItem = directory.Files.FirstOrDefault(f => f.Path.Contains(path));

            if (fileItem is ILaunchableItem launchItem) 
            {
                await LaunchItem(storageType, launchItem);

                if (launchItem is SongItem songItem) 
                {
                    StartStream(songItem.PlayLength);
                }                
                return;
            }
            RadHelper.WriteError("File is not launchable.");
            AnsiConsole.WriteLine();
        }

        public async Task LaunchItem(TeensyStorageType storageType, ILaunchableItem item)
        {
            _selectedStorage = storageType;
            _path = item.Path;
            _playState = PlayState.Playing;

            var result = await mediator.Send(new LaunchFileCommand(storageType, item));

            if (result.IsSuccess)
            {
                RadHelper.WriteFileInfo(item);
            }
            else
            {
                RadHelper.WriteError($"Error Launching: { item.Path.EscapeBrackets() }");
                AnsiConsole.WriteLine();
                await PlayRandom(_selectedStorage, _scopePath, _filterType);
            }
            AnsiConsole.WriteLine(RadHelper.ClearHack);
        }

        public async Task PlayRandom(TeensyStorageType storageType, string scopePath, TeensyFilterType filterType)
        {
            _playMode = PlayMode.Shuffle;

            var trSettings = await settingsService.Settings.FirstAsync();
            _filterType = filterType;
            _scopePath = scopePath;

            var fileTypes = trSettings.GetFileTypes(_filterType);

            var launchItem = storage.GetRandomFilePath(StorageScope.DirDeep, _scopePath, fileTypes);

            var item = storage.GetRandomFile(_selectedScope, _scopePath, fileTypes);

            if (item is null) return;            

            await LaunchItem(storageType, item);

            if (item is SongItem songItem) 
            {
                StartStream(songItem.PlayLength);
            }
        }

        private void StartStream(TimeSpan length)
        {
            _playState = PlayState.Playing;
            _progressSubscription?.Dispose();

            progressTimer.StartNewTimer(length);

            _progressSubscription = progressTimer.TimerComplete.Subscribe(async _ =>
            {
                await PlayRandom(_selectedStorage, _scopePath, _filterType);
            });
        }

        public void StopStream()
        {
            if (_progressSubscription is not null) 
            {
                RadHelper.WriteTitle("Stopping Stream");
                AnsiConsole.WriteLine(RadHelper.ClearHack);
            }            
            _playState = PlayState.Stopped;
            _progressSubscription?.Dispose();
            _progressSubscription = null;
        }
    }
}
