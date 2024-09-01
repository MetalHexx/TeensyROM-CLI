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
        void StopContinuousPlay();
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
                StartContinuousPlay();
                return;
            }
            RadHelper.WriteError("File is not launchable.");
            AnsiConsole.WriteLine();
        }

        public async Task LaunchItem(TeensyStorageType storageType, ILaunchableItem item)
        {
            var trAvailable = await IsTrAvailable();

            if(!trAvailable) return;

            _selectedStorage = storageType;
            _path = item.Path;
            _playState = PlayState.Playing;

            

            if (item is not SongItem) 
            {
                RadHelper.WriteTitle("Stopping Continuous File Launching (SID Only)");
            }
            var result = await mediator.Send(new LaunchFileCommand(storageType, item));

            if (result.IsSuccess)
            {
                RadHelper.WriteFileInfo(item);
            }
            else
            {
                RadHelper.WriteError($"Error Launching: {item.Path.Replace("[", "(").Replace("]", ")")}");
                await PlayRandom(_selectedStorage, _scopePath, _filterType);
            }
            AnsiConsole.WriteLine("                                                                                                             ");
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

            StartContinuousPlay();
        }

        private void StartContinuousPlay()
        {
            _playState = PlayState.Playing;
            _progressSubscription?.Dispose();

            progressTimer.StartNewTimer(TimeSpan.FromSeconds(3));

            _progressSubscription = progressTimer.TimerComplete.Subscribe(async _ =>
            {
                await PlayRandom(_selectedStorage, _scopePath, _filterType);
            });
        }

        public void StopContinuousPlay()
        {
            if (_progressSubscription is not null) 
            {
                RadHelper.WriteTitle("Stopping Continuous File Launching");
                AnsiConsole.WriteLine("                                                                                                       ");
            }            
            _playState = PlayState.Stopped;
            _progressSubscription?.Dispose();
            _progressSubscription = null;
        }

        private async Task<bool> IsTrAvailable()
        {
            var serialState = await serial.CurrentState.FirstAsync();

            if (serialState is SerialConnectedState) 
            {
                return true;
            }
            if (serialState is SerialBusyState) 
            {
                RadHelper.WriteError("Cannot launch files while serial is busy!");
                return false;
            }

            if (serialState is SerialConnectableState or SerialConnectionLostState or SerialStartState)
            {
                RadHelper.WriteError("Cannot launch files while disconnected from TeensyROM!");
                return false;
            }
            RadHelper.WriteError("Cannot launch files while in current state");
            return false;
        }
    }
}
