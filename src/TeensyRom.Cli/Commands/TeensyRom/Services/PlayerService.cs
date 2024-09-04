using MediatR;
using Spectre.Console;
using System.Diagnostics.Contracts;
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

    internal class PlayerService : IPlayerService
    {
        private const string NotApplicable = "---";
        private TeensyStorageType _selectedStorage = TeensyStorageType.SD;
        private StorageScope _selectedScope = StorageScope.DirDeep;
        private string _scopeDirectory = "/";
        private string _currentDirectory = "/";
        private string _searchQuery = NotApplicable;

        private ILaunchableItem? _currentFile = null;
        private PlayState _playState = PlayState.Stopped;
        private PlayMode _playMode = PlayMode.Random;
        private TeensyFilterType _filterType = TeensyFilterType.All;
        private TimeSpan? _streamTimeSpan = null;
        private SidTimer _sidTimer = SidTimer.SongLength;

        private IDisposable? _progressSubscription;
        private readonly IMediator _mediator;
        private readonly ICachedStorageService _storage;
        private readonly IProgressTimer _progressTimer;
        private readonly ISettingsService _settingsService;
        private readonly ISerialStateContext _serial;
        private readonly ILaunchHistory _history;

        public PlayerService(IMediator mediator, ICachedStorageService storage, IProgressTimer progressTimer, ISettingsService settingsService, ISerialStateContext serial, ILaunchHistory history)
        {
            _mediator = mediator;
            _storage = storage;
            _progressTimer = progressTimer;
            _settingsService = settingsService;
            _serial = serial;
            _history = history;
            serial.CurrentState
                .Where(state => state is SerialConnectionLostState && _playState is PlayState.Playing)
                .Subscribe(_ => StopStream());
        }

        public async Task LaunchItem(TeensyStorageType storageType, string path) 
        {
            var directory = await _storage.GetDirectory(path.GetUnixParentPath());

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
                return;
            }
            RadHelper.WriteError("File is not launchable.");
            AnsiConsole.WriteLine();
            return;
        }

        public async Task<LaunchFileResult> LaunchItem(TeensyStorageType storageType, ILaunchableItem item)
        {
            _currentFile = item;
            _selectedStorage = storageType;
            _currentDirectory = _currentFile.Path;
            _playState = PlayState.Playing;

            var result = await _mediator.Send(new LaunchFileCommand(storageType, item));
            
            if (result.IsSuccess)
            {
                RadHelper.WriteFileInfo(item);                
            }
            else
            {
                RadHelper.WriteError($"Error Launching: { item.Path.EscapeBrackets() }");
                AnsiConsole.WriteLine(RadHelper.ClearHack);
                await PlayNext();
            }
            AnsiConsole.WriteLine(RadHelper.ClearHack);
            MaybeStartStream(item);

            return result;
        }

        public async Task PlayRandom(TeensyStorageType storageType, string scopePath, TeensyFilterType filterType)
        {
            if (_playMode is not PlayMode.Random) 
            {
                _history.Clear();
            }
            _playMode = PlayMode.Random;

            var trSettings = await _settingsService.Settings.FirstAsync();
            _filterType = filterType;
            _scopeDirectory = scopePath;

            var fileTypes = trSettings.GetFileTypes(_filterType);

            var randomItem = _storage.GetRandomFile(_selectedScope, _scopeDirectory, fileTypes);

            if (randomItem is null) return;            

            var result = await LaunchItem(storageType, randomItem);

            if (result.IsSuccess) 
            {
                _history.Add(randomItem);
            }
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

            _progressTimer.StartNewTimer(length);
            
            _progressSubscription = _progressTimer.TimerComplete.Subscribe(async _ =>
            {                
                await PlayNext();
            });            
        }

        public async Task PlayPrevious()
        {
            if (_playMode is PlayMode.Random) 
            {
                var previous = _history.GetPrevious();

                if (previous is not null) 
                {
                    await LaunchItem(_selectedStorage, previous);
                    return;
                }
                if (_currentFile is not null) 
                {
                    await LaunchItem(_selectedStorage, _currentFile);
                }                
                return;
            }
            if (_playMode is PlayMode.Search) 
            {
                var searchItem = GetPreviousSearchItem();

                if (searchItem is not null)
                {
                    await LaunchItem(_selectedStorage, searchItem);
                    return;
                }
                if (_currentFile is not null)
                {
                    await LaunchItem(_selectedStorage, _currentFile);
                }
                return;
            }
            var previousItem = await GetPreviousDirectoryItem();

            if (previousItem is not null)
            {
                await LaunchItem(_selectedStorage, previousItem);
                AnsiConsole.WriteLine(RadHelper.ClearHack);
                return;
            }
            if (_currentFile is not null)
            {
                await LaunchItem(_selectedStorage, _currentFile);
            }
            return;
        }

        public ILaunchableItem? GetPreviousSearchItem()
        {
            var list = _storage.Search(_searchQuery, []).ToList();
            return GetPreviousFromList(list);
        }

        public async Task<ILaunchableItem?> GetPreviousDirectoryItem()
        {
            var currentPath = _currentFile!.Path.GetUnixParentPath();
            var currentDirectory = await _storage.GetDirectory(currentPath);

            if (currentDirectory is null)
            {
                RadHelper.WriteError($"Couldn't find directory {currentPath}.");
                return null;
            }
            var items = currentDirectory.Files.OfType<ILaunchableItem>().ToList();
            return GetPreviousFromList(items);
        }

        private ILaunchableItem? GetPreviousFromList(List<ILaunchableItem> list) 
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
                return filteredFiles.Last();
            }
            var filteredIndex = filteredFiles.IndexOf(currentFile);

            if (filteredIndex >= 0)
            {
                var index = filteredIndex < 0
                ? filteredIndex - 1
                : filteredFiles.Count - 1;

                return filteredFiles[index];
            }

            for (int x = 0; x < unfilteredFiles.Count; x++)
            {
                var f = filteredFiles[x];
                var fIndex = unfilteredFiles.IndexOf(f);

                if (fIndex < unfilteredIndex)
                {
                    continue;
                }
                return f;
            }
            return filteredFiles.First();
        }

        public async Task PlayNext()
        {
            if (_playMode is PlayMode.Random)
            {
                var nextHistory = _history.GetNext(GetFilterFileTypes());

                if (nextHistory is not null)
                {
                    await LaunchItem(_selectedStorage, nextHistory);
                    return;
                }
                await PlayRandom(_selectedStorage, _scopeDirectory, _filterType);
                return;
            }
            if (_playMode is PlayMode.Search)
            {
                var searchItem = GetNextSearchItem();

                if (searchItem is not null)
                {
                    await LaunchItem(_selectedStorage, searchItem);
                    return;
                }
                RadHelper.WriteError("Couldn't find search result. Launching random.");
                await PlayRandom(_selectedStorage, _scopeDirectory, _filterType);

                return;
            }
            var nextItem = await GetNextDirectoryItem();

            if (nextItem is not null)
            {
                var result = await LaunchItem(_selectedStorage, nextItem);
                AnsiConsole.WriteLine(RadHelper.ClearHack);
                return;
            }
            await PlayRandom(_selectedStorage, _scopeDirectory, _filterType);
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

            if (currentDirectory is null)
            {
                return null;
            }
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

            if (filteredIndex >= 0)
            {
                var index = filteredIndex < filteredFiles.Count - 1
                ? filteredIndex + 1
                : 0;

                return filteredFiles[index];
            }

            for (int x = 0; x < unfilteredFiles.Count; x++)
            {
                var f = filteredFiles[x];
                var fIndex = unfilteredFiles.IndexOf(f);

                if (fIndex < unfilteredIndex)
                {
                    continue;
                }
                return f;
            }
            return filteredFiles.First();
        }

        private TeensyFileType[] GetFilterFileTypes()
        {
            var trSettings = _settingsService.GetSettings();
            return trSettings.GetFileTypes(_filterType);
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

        public PlayerSettings GetPlayerSettings() => new()
        {
            StorageType = _selectedStorage,
            PlayState = _playState,
            PlayMode = _playMode,
            FilterType = _filterType,
            ScopePath = _scopeDirectory,
            PlayTimer = _streamTimeSpan,
            SidTimer = _sidTimer,
            CurrentItem = _currentFile,
            ScopeDirectory = _scopeDirectory,
            SearchQuery = _searchQuery
        };

        public void SetSearchMode(string query) 
        {
            _playMode = PlayMode.Search;
            _searchQuery = query;
        }

        public void SetDirectoryMode(string directoryPath) 
        {
            _playMode = PlayMode.CurrentDirectory;
            _currentDirectory = directoryPath;
            _searchQuery = NotApplicable;
        }

        public void SetRandomMode(string scopePath) 
        {
            if (_playMode is not PlayMode.Random) 
            {
                _history.Clear();                
            }
            _playMode = PlayMode.Random;
            _scopeDirectory = scopePath;
            _searchQuery = NotApplicable;
        }

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
