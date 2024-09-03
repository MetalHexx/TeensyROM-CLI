using MediatR;
using Spectre.Console;
using System.Reactive.Linq;
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
        PlayerSettings GetPlayerSettings();
        Task LaunchItem(TeensyStorageType storageType, ILaunchableItem item);
        Task LaunchItem(TeensyStorageType storageType, string path);        
        Task PlayNext();
        Task PlayRandom(TeensyStorageType storageType, string scopePath, TeensyFilterType filterType);
        void SetFilter(TeensyFilterType filterType);
        void SetPlayMode(PlayMode playMode);
        void SetStreamTime(TimeSpan? timespan);
        void SetSidTimer(SidTimer value);
        void StopStream();
        void SetScope(string path);
    }

    internal class PlayerService : IPlayerService
    {
        private TeensyStorageType _selectedStorage = TeensyStorageType.SD;
        private StorageScope _selectedScope = StorageScope.DirDeep;
        private string _scopeDirectory = "/";
        private string _path = "/";

        private ILaunchableItem? _currentFile = null;
        private PlayState _playState = PlayState.Stopped;
        private PlayMode _playMode = PlayMode.Random;
        private TeensyFilterType _filterType = TeensyFilterType.All;
        private TimeSpan? _streamTimeSpan = null;
        private SidTimer _sidTimer = SidTimer.SongLength;

        private IDisposable? _progressSubscription;
        private readonly IMediator mediator;
        private readonly ICachedStorageService storage;
        private readonly IProgressTimer progressTimer;
        private readonly ISettingsService settingsService;
        private readonly ISerialStateContext serial;

        public PlayerService(IMediator mediator, ICachedStorageService storage, IProgressTimer progressTimer, ISettingsService settingsService, ISerialStateContext serial)
        {
            this.mediator = mediator;
            this.storage = storage;
            this.progressTimer = progressTimer;
            this.settingsService = settingsService;
            this.serial = serial;

            serial.CurrentState
                .Where(state => state is SerialConnectionLostState && _playState is PlayState.Playing)
                .Subscribe(_ => StopStream());
        }

        public async Task LaunchItem(TeensyStorageType storageType, string path) 
        {
            _playMode = PlayMode.CurrentDirectory;

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
                MaybeStartStream(launchItem);
                return;
            }
            RadHelper.WriteError("File is not launchable.");
            AnsiConsole.WriteLine();
            return;
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
                _currentFile = item;
            }
            else
            {
                RadHelper.WriteError($"Error Launching: { item.Path.EscapeBrackets() }");
                AnsiConsole.WriteLine(RadHelper.ClearHack);

                await PlayRandom(_selectedStorage, _scopeDirectory, _filterType);
            }
            AnsiConsole.WriteLine(RadHelper.ClearHack);
        }

        public async Task PlayRandom(TeensyStorageType storageType, string scopePath, TeensyFilterType filterType)
        {
            _playMode = PlayMode.Random;

            var trSettings = await settingsService.Settings.FirstAsync();
            _filterType = filterType;
            _scopeDirectory = scopePath;

            var fileTypes = trSettings.GetFileTypes(_filterType);

            var launchItem = storage.GetRandomFilePath(StorageScope.DirDeep, _scopeDirectory, fileTypes);

            var item = storage.GetRandomFile(_selectedScope, _scopeDirectory, fileTypes);

            if (item is null) return;            

            await LaunchItem(storageType, item);

            MaybeStartStream(item);
        }

        private void MaybeStartStream(ILaunchableItem fileItem)
        {
            if (fileItem is SongItem songItem && _sidTimer is SidTimer.SongLength)
            {   
                StartStream(songItem.PlayLength);
                return;
            }
            if (_streamTimeSpan is not null)
            {
                StartStream(_streamTimeSpan.Value);
            }
        }

        private void StartStream(TimeSpan length)
        {
            _playState = PlayState.Playing;
            _progressSubscription?.Dispose();

            progressTimer.StartNewTimer(length);

            
            _progressSubscription = progressTimer.TimerComplete.Subscribe(async _ =>
            {
                
                await PlayNext();
            });            
        }

        public async Task PlayNext() 
        {
            if (_playMode is PlayMode.Random)
            {
                await PlayRandom(_selectedStorage, _scopeDirectory, _filterType);
                return;
            }

            if (_currentFile is null)
            {
                await PlayRandom(_selectedStorage, _scopeDirectory, _filterType);
                return;
            }
            var currentPath = _currentFile!.Path.GetUnixParentPath();
            var currentDirectory = await storage.GetDirectory(currentPath);

            if (currentDirectory is null)
            {
                RadHelper.WriteError($"Could not find directory {currentPath}. Launching random.");
                AnsiConsole.WriteLine(RadHelper.ClearHack);
                await PlayRandom(_selectedStorage, _scopeDirectory, _filterType);
                return;
            }
            var currentFile = currentDirectory.Files?.FirstOrDefault(f => f.Id == _currentFile.Id);

            if (currentFile is null)
            {
                RadHelper.WriteError($"Could not find current file. Launching random.");
                AnsiConsole.WriteLine(RadHelper.ClearHack);
                await PlayRandom(_selectedStorage, _scopeDirectory, _filterType);
                return;
            }
            var currentIndex = currentDirectory.Files!.IndexOf(currentFile);

            var newIndex = currentIndex < currentDirectory.Files.Count - 1
                ? currentIndex + 1
                : 0;

            var fileItem = currentDirectory.Files[newIndex];

            if (fileItem is ILaunchableItem launchItem)
            {
                await LaunchItem(_selectedStorage, launchItem);
                return;
            }
            RadHelper.WriteError($"{fileItem.Path} is not launchable. Launching random.");
            AnsiConsole.WriteLine(RadHelper.ClearHack);
            await PlayRandom(_selectedStorage, _scopeDirectory, _filterType);
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

        public PlayerSettings GetPlayerSettings() => new PlayerSettings
        {
            PlayState = _playState,
            PlayMode = _playMode,
            FilterType = _filterType,
            ScopePath = _scopeDirectory,
            PlayTimer = _streamTimeSpan,
            SidTimer = _sidTimer,
            CurrentItem = _currentFile,
            ScopeDirectory = _scopeDirectory
        };

        public void SetPlayMode(PlayMode playMode) => _playMode = playMode;
        public void SetFilter(TeensyFilterType filterType) => _filterType = filterType;
        public void SetScope(string path) => _scopeDirectory = path;
        public void SetStreamTime(TimeSpan? timespan)
        {
            _streamTimeSpan = timespan;

            if (_currentFile is SongItem && _sidTimer is SidTimer.SongLength) 
            {
                return;
            }

            if (_streamTimeSpan is not null)
            {
                StopStream();
                StartStream(_streamTimeSpan.Value);
            }
        }
        public void SetSidTimer(SidTimer value) => _sidTimer = value;
    }
}
