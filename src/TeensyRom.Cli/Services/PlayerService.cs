using MediatR;
using Spectre.Console;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Commands.File.LaunchFile;
using TeensyRom.Core.Common;
using TeensyRom.Core.Logging;
using TeensyRom.Core.Player;
using TeensyRom.Core.Progress;
using TeensyRom.Core.Serial.State;
using TeensyRom.Core.Settings;
using TeensyRom.Core.Storage.Entities;
using TeensyRom.Core.Storage.Services;

namespace TeensyRom.Cli.Services
{
    internal class PlayerService : IPlayerService
    {
        public IObservable<ILaunchableItem> FileLaunched => _fileLaunched.AsObservable();
        private Subject<ILaunchableItem> _fileLaunched = new();

        private TeensyStorageType _selectedStorage = TeensyStorageType.SD;
        private StorageScope _selectedScope = StorageScope.DirDeep;
        private string _scopeDirectory = "/";
        private string? _searchQuery;

        private ILaunchableItem? _currentFile = null;
        private PlayState _playState = PlayState.Stopped;
        private PlayMode _playMode = PlayMode.Random;
        private TeensyFilterType _filterType = TeensyFilterType.All;
        private TimeSpan? _streamTimeSpan = null;
        private SidTimer _sidTimer = SidTimer.SongLength;

        private IDisposable? _timerSubscription;
        private readonly IMediator _mediator;
        private readonly ICachedStorageService _storage;
        private readonly IProgressTimer _timer;
        private readonly ISettingsService _settingsService;
        private readonly ISerialStateContext _serial;
        private readonly ILaunchHistory _randomHistory;
        private readonly IAlertService _alert;
        private readonly ILoggingService _log;

        public PlayerService(IMediator mediator, ICachedStorageService storage, IProgressTimer progressTimer, ISettingsService settingsService, ISerialStateContext serial, ILaunchHistory history, IAlertService alert, ILoggingService log)
        {
            _mediator = mediator;
            _storage = storage;
            _timer = progressTimer;
            _settingsService = settingsService;
            _serial = serial;
            _randomHistory = history;
            _alert = alert;
            _log = log;
            var settings = settingsService.GetSettings();
            _selectedStorage = settings.StorageType;
            _filterType = settings.StartupFilter;

            serial.CurrentState
                .Where(state => state is SerialConnectionLostState && _playState is PlayState.Playing)
                .Subscribe(_ => StopStream());

            SetupTimerSubscription();
        }

        public async Task LaunchFromDirectory(TeensyStorageType storageType, string path)
        {
            var directory = await _storage.GetDirectory(path.GetUnixParentPath());

            if (directory is null)
            {
                _alert.PublishError("Directory not found.");
                return;
            }
            var fileItem = directory.Files.FirstOrDefault(f => f.Path.Contains(path));

            if (fileItem is not ILaunchableItem launchItem)
            {
                _alert.PublishError("File is not launchable.");
                return;
            }
            var launchSuccessful = await LaunchItem(storageType, launchItem);

            if (!launchSuccessful) 
            {
                await PlayNext();
            }
        }

        public async Task<bool> LaunchItem(TeensyStorageType storageType, ILaunchableItem item)
        {
            _currentFile = item;
            _selectedStorage = storageType;
            _playState = PlayState.Playing;

            if (!item.IsCompatible) 
            {
                AlertBadFile(item);
                return false;
            }
            var result = await _mediator.Send(new LaunchFileCommand(storageType, item));

            if (result.IsSuccess)
            {
                _fileLaunched.OnNext(item);
                _alert.Publish(RadHelper.ClearHack);
                MaybeStartStream(item);
                return true;
            }          
            if (result.LaunchResult is LaunchFileResultType.SidError or LaunchFileResultType.ProgramError)
            {
                _storage.MarkIncompatible(item);
                _randomHistory.Remove(item);
                AlertBadFile(item);
                return false;
            }
            _alert.PublishError($"Failed to launch {item.Name}.");
            return false;
        }

        private void AlertBadFile(ILaunchableItem item) 
        {
            AnsiConsole.WriteLine();
            _alert.PublishError($"{item.Name} is currently unsupported (see logs).  Skipping file.");
        }

        public async Task PlayRandom(TeensyStorageType storageType, string scopePath, TeensyFilterType filterType)
        {
            _filterType = filterType;
            _scopeDirectory = scopePath;

            if (_playMode is not PlayMode.Random)
            {
                _randomHistory.Clear();
            }
            _playMode = PlayMode.Random;

            var randomItem = _storage.GetRandomFile(_selectedScope, _scopeDirectory, GetFilterFileTypes());

            if (randomItem is null)
            {
                _alert.PublishError($"No files for the filter \"{filterType}\" were found on the {storageType} in {scopePath}.");
                return;
            }
            var launchSuccessful = await LaunchItem(storageType, randomItem);

            if (launchSuccessful) 
            {
                _randomHistory.Add(randomItem);
                return;
            }
            await PlayNext();
        }

        public async Task PlayPrevious()
        {
            ILaunchableItem? fileToPlay = _playMode switch
            {
                PlayMode.Random => _randomHistory.GetPrevious(GetFilterFileTypes()),
                PlayMode.Search => GetPreviousSearchItem(),
                PlayMode.CurrentDirectory => await GetPreviousDirectoryItem(),
                _ => _currentFile
            };
            if(fileToPlay is null)
            {
                if (_currentFile is null) 
                {
                    return;
                }
                fileToPlay = _currentFile;
            }
            var launchSuccessful = await LaunchItem(_selectedStorage, fileToPlay);

            if (!launchSuccessful) 
            {
                await PlayPrevious();
            }
        }

        public ILaunchableItem? GetPreviousSearchItem()
        {
            var list = _storage.Search(_searchQuery!, []).ToList();
            return GetPreviousFromList(list);
        }

        public async Task<ILaunchableItem?> GetPreviousDirectoryItem()
        {
            var currentPath = _currentFile!.Path.GetUnixParentPath();
            var currentDirectory = await _storage.GetDirectory(currentPath);

            if (currentDirectory is null)
            {
                _alert.PublishError($"Couldn't find directory {currentPath}.");
                return null;
            }
            var files = currentDirectory.Files.OfType<ILaunchableItem>().ToList();
            return GetPreviousFromList(files);
        }

        private ILaunchableItem? GetPreviousFromList(List<ILaunchableItem> list)
        {
            var unfilteredFiles = list;

            if (unfilteredFiles.Count == 0)
            {
                _alert.PublishError("Something went wrong.  I couldn't find any files in the target location.");
                return null;
            }
            var currentFile = unfilteredFiles.ToList().FirstOrDefault(f => f.Id == _currentFile?.Id);

            if (currentFile is null)
            {
                _alert.PublishError("Something went wrong.  I couldn't find the current file in the target location.");
                return null;
            }

            var filteredFiles = unfilteredFiles
                .Where(f => GetFilterFileTypes()
                    .Any(t => f.FileType == t))
                .ToList();

            if (filteredFiles.Count() == 0)
            {
                _alert.PublishError("There were no files matching your filter in the target location");
                return null;
            }

            var currentFileUnfilteredIndex = unfilteredFiles.IndexOf(currentFile);

            var currentFileComesAfterLastItemInFilteredList = unfilteredFiles.IndexOf(filteredFiles.Last()) < currentFileUnfilteredIndex;

            if (currentFileComesAfterLastItemInFilteredList)
            {
                return filteredFiles.Last();
            }
            var filteredIndex = filteredFiles.IndexOf(currentFile);

            if (filteredIndex != -1)
            {
                var index = filteredIndex == 0
                    ? filteredFiles.Count - 1
                    : filteredIndex - 1;

                return filteredFiles[index];
            }

            ILaunchableItem? candidate = null;

            for (int x = 0; x < filteredFiles.Count; x++)
            {
                var f = filteredFiles[x];

                var fIndex = unfilteredFiles.IndexOf(f);

                if (fIndex < currentFileUnfilteredIndex)
                {
                    candidate = f;
                    continue;
                }
                else if (fIndex > currentFileUnfilteredIndex)
                {
                    break;
                }
            }
            if (candidate is null)
            {
                return filteredFiles.First();
            }
            return candidate;
        }

        public async Task PlayNext()
        {
            ILaunchableItem? fileToPlay = null;

            switch (_playMode) 
            {
                case PlayMode.Random:
                    fileToPlay = _randomHistory.GetNext(GetFilterFileTypes());

                    if (fileToPlay is null)
                    {
                        await PlayRandom(_selectedStorage, _scopeDirectory, _filterType);
                        return;
                    }
                    break;

                case PlayMode.Search:
                    fileToPlay = GetNextSearchItem();
                    break;

                case PlayMode.CurrentDirectory:
                    fileToPlay = await GetNextDirectoryItem();
                    break;
            }

            if (fileToPlay is null)
            {
                fileToPlay = _currentFile;
            }
            var launchSuccessful = await LaunchItem(_selectedStorage, fileToPlay!);

            if (!launchSuccessful) await PlayNext();
        }

        public ILaunchableItem? GetNextSearchItem()
        {
            var list = _storage.Search(_searchQuery, []).ToList();
            return GetNextListItem(list);
        }

        public async Task<ILaunchableItem?> GetNextDirectoryItem()
        {
            var currentPath = _currentFile!.Path.GetUnixParentPath();
            var currentDirectory = await _storage.GetDirectory(currentPath);

            if (currentDirectory is null) return null;

            var compatibleFiles = currentDirectory.Files.Any(f => f.IsCompatible);

            if (!compatibleFiles) return null;        
            
            var list = currentDirectory.Files.OfType<ILaunchableItem>().ToList();
            return GetNextListItem(list);
        }

        public ILaunchableItem? GetNextListItem(List<ILaunchableItem> list)
        {
            var unfilteredFiles = list;

            if (unfilteredFiles.Count == 0)
            {
                RadHelper.WriteError("Something went wrong.  I coudln't find any files in the target location.");
                return null;
            }

            var currentFile = unfilteredFiles.ToList().FirstOrDefault(f => f.Id == _currentFile?.Id);

            if (currentFile is null)
            {
                RadHelper.WriteError("Something went wrong.  I coudln't find the current file in the target location.");
                return null;
            }

            var unfilteredIndex = unfilteredFiles.IndexOf(currentFile);

            var filteredFiles = unfilteredFiles
                .Where(f => GetFilterFileTypes()
                    .Any(t => f.FileType == t))
                .ToList();

            if (filteredFiles.Count() == 0)
            {
                RadHelper.WriteError("There were no files matching your filter in the target location");
                return null;
            }
            if (unfilteredIndex > filteredFiles.Count - 1)
            {
                return filteredFiles.First();
            }
            var filteredIndex = filteredFiles.IndexOf(currentFile);

            if (filteredIndex != -1)
            {
                var index = filteredIndex < filteredFiles.Count - 1
                ? filteredIndex + 1
                : 0;

                return filteredFiles[index];
            }

            ILaunchableItem? candidate = null;

            for (int x = 0; x < filteredFiles.Count; x++)
            {
                var f = filteredFiles[x];

                var fIndex = unfilteredFiles.IndexOf(f);

                if (fIndex > unfilteredIndex)
                {
                    candidate = f;
                    continue;
                }
                else if (fIndex < unfilteredIndex)
                {
                    break;
                }
            }
            if (candidate is null)
            {
                return filteredFiles.First();
            }
            return candidate;
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
            SetupTimerSubscription();
            _timer.StartNewTimer(length);
        }

        private void SetupTimerSubscription()
        {
            _timerSubscription?.Dispose();

            _timerSubscription = _timer.TimerComplete.Subscribe(async _ =>
            {
                await PlayNext();
            });
        }

        public void StopStream()
        {
            if (_timerSubscription is not null)
            {
                _alert.Publish("Stopping Stream");
            }
            _playState = PlayState.Stopped;
            _timerSubscription?.Dispose();
            _timerSubscription = null;
        }

        public PlayerState GetPlayerSettings()
        {
            var settings = _settingsService.GetSettings();

            return new PlayerState
            {
                StorageType = _selectedStorage,
                PlayState = _playState,
                PlayMode = _playMode,
                FilterType = _filterType,
                ScopePath = _scopeDirectory,
                PlayTimer = _streamTimeSpan,
                SidTimer = _sidTimer,
                CurrentItem = _currentFile,
                SearchQuery = _searchQuery
            };
        }

        public void SetSearchMode(string query)
        {
            _playMode = PlayMode.Search;
            _searchQuery = query;
        }

        public void SetDirectoryMode(string directoryPath)
        {
            _playMode = PlayMode.CurrentDirectory;
            _scopeDirectory = directoryPath;
            _searchQuery = null;
        }

        public void SetRandomMode(string scopePath)
        {
            if (_playMode is not PlayMode.Random)
            {
                _randomHistory.Clear();
            }
            _playMode = PlayMode.Random;
            _scopeDirectory = scopePath;
            _searchQuery = null;
        }

        public void SetFilter(TeensyFilterType filterType) => _filterType = filterType;
        public void SetDirectoryScope(string path) => _scopeDirectory = path;
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

        private TeensyFileType[] GetFilterFileTypes()
        {
            var trSettings = _settingsService.GetSettings();
            return trSettings.GetFileTypes(_filterType);
        }
    }
}