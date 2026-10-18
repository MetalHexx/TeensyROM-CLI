using Spectre.Console;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using TeensyRom.Cli.Api;
using TeensyRom.Cli.Helpers;
using TeensyRom.Cli.Common;
using TeensyRom.Cli.Logging;
using TeensyRom.Cli.Player;
using TeensyRom.Cli.Progress;
using TeensyRom.Cli.Settings;
using TeensyRom.Cli.Storage.Entities;
using TeensyRom.Cli.Storage.Services;

namespace TeensyRom.Cli.Services
{
    /// <summary>
    /// Sequences what plays next, previous or at random, and keeps the play timer. Failures talking to tr-web never
    /// escape its operations: they are published as alerts and the stream stops.
    /// </summary>
    internal class PlayerService : IPlayerService
    {
        public IObservable<ILaunchableItem> FileLaunched => _fileLaunched.AsObservable();
        private Subject<ILaunchableItem> _fileLaunched = new();
        private PlayerState _state;

        /// <summary>What a launch attempt means for the next move: carry on, skip to another file, or stop where we are.</summary>
        private enum Step { Launched, Skipped, Stopped }

        private IDisposable? _timerSubscription;
        private readonly IStorageService _storage;
        private readonly ILaunchService _launch;
        private readonly IDeviceService _devices;
        private readonly IProgressTimer _timer;
        private readonly ISettingsService _settingsService;
        private readonly ILaunchHistory _randomHistory;
        private readonly IAlertService _alert;
        private readonly ILoggingService _log;

        public PlayerService(IStorageService storage, ILaunchService launch, IDeviceService devices, IProgressTimer progressTimer, ISettingsService settingsService, ILaunchHistory history, IAlertService alert, ILoggingService log)
        {
            _storage = storage;
            _launch = launch;
            _devices = devices;
            _timer = progressTimer;
            _settingsService = settingsService;
            _randomHistory = history;
            _alert = alert;
            _log = log;
            var settings = settingsService.GetSettings();

            _state = new()
            {
                StorageType = settings.StorageType,
                FilterType = settings.StartupFilter,
            };

            SetupTimerSubscription();
        }

        public Task LaunchFromDirectory(string path) => Guarded(() => LaunchFromDirectoryCore(path));

        private async Task LaunchFromDirectoryCore(string path)
        {
            var directory = await _storage.GetDirectory(path.GetUnixParentPath());
            var fileItem = directory.Files.FirstOrDefault(f => f.Path.Contains(path));

            if (fileItem is not ILaunchableItem launchItem)
            {
                _alert.PublishError("File is not launchable.");
                return;
            }
            if (await Launch(launchItem) is Step.Skipped)
            {
                await PlayNextCore();
            }
        }

        public async Task<bool> LaunchItem(ILaunchableItem item)
        {
            var launched = false;
            await Guarded(async () => launched = await Launch(item) is Step.Launched);
            return launched;
        }

        private async Task<Step> Launch(ILaunchableItem item)
        {
            _state = _state with
            {
                CurrentItem = item,
                PlayState = PlayState.Playing
            };
            if (!item.IsCompatible)
            {
                AlertBadFile(item);
                return Step.Skipped;
            }
            var outcome = await _launch.LaunchFile(_state.StorageType, item);

            return outcome.Status switch
            {
                LaunchStatus.Launched => OnLaunched(item),
                LaunchStatus.Incompatible => SkipIncompatible(item),
                _ => Refused(outcome.Message, $"Failed to launch {item.Name}.")
            };
        }

        private Step OnLaunched(ILaunchableItem item)
        {
            _fileLaunched.OnNext(item);
            _alert.Publish(RadHelper.ClearHack);
            MaybeStartStream(item);
            return Step.Launched;
        }

        private Step SkipIncompatible(ILaunchableItem item)
        {
            item.IsCompatible = false;
            _randomHistory.Remove(item);
            AlertBadFile(item);
            return Step.Skipped;
        }

        /// <summary>
        /// tr-web said it can't launch the file. Moving on to another file would only repeat the request, so the player stops here.
        /// </summary>
        private Step Refused(string? message, string fallback)
        {
            _alert.PublishError(string.IsNullOrWhiteSpace(message) ? fallback : message);
            _state = _state with { PlayState = PlayState.Stopped };
            return Step.Stopped;
        }

        private void AlertBadFile(ILaunchableItem item)
        {
            AnsiConsole.WriteLine();
            _alert.PublishError($"{item.Name} is currently unsupported (see logs).  Skipping file.");
        }

        private async Task Guarded(Func<Task> operation)
        {
            try
            {
                await operation();
            }
            catch (TeensyApiException ex)
            {
                _alert.PublishError(ex.Message);
                StopStream();
            }
        }

        public Task PlayRandom() => Guarded(PlayRandomCore);

        private async Task PlayRandomCore()
        {
            if (_state.PlayMode is not PlayMode.Random)
            {
                _randomHistory.Clear();
            }
            _state.PlayMode = PlayMode.Random;

            var outcome = await _launch.LaunchRandom(_state.StorageType, _state.Scope, _state.ScopePath, _state.FilterType);

            switch (outcome)
            {
                case { Status: LaunchStatus.Launched, Item: { } randomItem }:
                    _state = _state with { CurrentItem = randomItem, PlayState = PlayState.Playing };
                    OnLaunched(randomItem);
                    _randomHistory.Add(randomItem);
                    return;

                case { Status: LaunchStatus.Incompatible, Item: { } badItem }:
                    _state = _state with { CurrentItem = badItem, PlayState = PlayState.Playing };
                    SkipIncompatible(badItem);
                    await PlayNextCore();
                    return;

                case { Status: LaunchStatus.NotFound, Message: LaunchOutcome.NoFilesFoundMessage }:
                    _alert.PublishError($"No files for the filter \"{_state.FilterType}\" were found on the {_state.StorageType} in {_state.ScopePath}.");
                    return;

                default:
                    Refused(outcome.Message, "Failed to launch a random file.");
                    return;
            }
        }

        public Task PlayPrevious() => Guarded(PlayPreviousCore);

        private async Task PlayPreviousCore()
        {
            ILaunchableItem? fileToPlay = _state.PlayMode switch
            {
                PlayMode.Random => _randomHistory.GetPrevious(GetFilterFileTypes()),
                PlayMode.Search => await GetPreviousSearchItem(),
                PlayMode.CurrentDirectory => await GetPreviousDirectoryItem(),
                _ => _state.CurrentItem
            };
            if(fileToPlay is null)
            {
                if (_state.CurrentItem is null)
                {
                    return;
                }
                fileToPlay = _state.CurrentItem;
            }
            if (await Launch(fileToPlay) is Step.Skipped)
            {
                await PlayPreviousCore();
            }
        }

        private async Task<ILaunchableItem?> GetPreviousSearchItem()
        {
            var list = (await _storage.Search(_state.SearchQuery!, TeensyFilterType.All)).ToList();
            return GetPreviousFromList(list);
        }

        private async Task<ILaunchableItem?> GetPreviousDirectoryItem()
        {
            var currentPath = _state.CurrentItem!.Path.GetUnixParentPath();
            var currentDirectory = await _storage.GetDirectory(currentPath);
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
            var currentFile = unfilteredFiles.ToList().FirstOrDefault(f => f.Id == _state.CurrentItem?.Id);

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

        public Task PlayNext() => Guarded(PlayNextCore);

        private async Task PlayNextCore()
        {
            ILaunchableItem? fileToPlay = null;

            switch (_state.PlayMode)
            {
                case PlayMode.Random:
                    fileToPlay = _randomHistory.GetNext(GetFilterFileTypes());

                    if (fileToPlay is null)
                    {
                        await PlayRandomCore();
                        return;
                    }
                    break;

                case PlayMode.Search:
                    fileToPlay = await GetNextSearchItem();
                    break;

                case PlayMode.CurrentDirectory:
                    fileToPlay = await GetNextDirectoryItem();
                    break;
            }

            if (fileToPlay is null)
            {
                fileToPlay = _state.CurrentItem;
            }
            if (fileToPlay is null) return;

            if (await Launch(fileToPlay) is Step.Skipped) await PlayNextCore();
        }

        private async Task<ILaunchableItem?> GetNextSearchItem()
        {
            if (_state.SearchQuery is null) return null;

            var list = (await _storage.Search(_state.SearchQuery, TeensyFilterType.All)).ToList();

            return GetNextListItem(list);
        }

        private async Task<ILaunchableItem?> GetNextDirectoryItem()
        {
            var currentPath = _state.CurrentItem!.Path.GetUnixParentPath();
            var currentDirectory = await _storage.GetDirectory(currentPath);

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

            var currentFile = unfilteredFiles.ToList().FirstOrDefault(f => f.Id == _state.CurrentItem?.Id);

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
            if (fileItem is SongItem songItem && _state.SidTimer is SidTimer.SongLength)
            {
                StartStream(songItem.PlayLength);
                return;
            }
            if (_state.PlayTimer is not null)
            {
                StartStream(_state.PlayTimer.Value);
            }
        }

        private void StartStream(TimeSpan length)
        {
            _state.PlayState = PlayState.Playing;
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
            _state.PlayState = PlayState.Stopped;
            _timerSubscription?.Dispose();
            _timerSubscription = null;
        }

        public void PauseStream() 
        {
            _state.PlayState = PlayState.Paused;
            _timer.PauseTimer();
        }
        public void ResumeStream() 
        {
            _state.PlayState = PlayState.Playing;
            _timer.ResumeTimer();
        }

        public PlayerState GetState() => _state with { };

        public void SetSearchMode(string query)
        {
            _state = _state with
            {
                PlayMode = PlayMode.Search,
                SearchQuery = query
            };
        }

        public void SetDirectoryMode(string directoryPath)
        {
            _state = _state with 
            {
                PlayMode = PlayMode.CurrentDirectory,
                FilterType = TeensyFilterType.All,
                ScopePath = directoryPath,
                SearchQuery = null
            };
            _alert.Publish("Switching filter to \"All\"");
        }

        public void SetRandomMode(string scopePath)
        {
            if (_state.PlayMode is not PlayMode.Random)
            {
                _randomHistory.Clear();
            }
            _state = _state with            
            {
                PlayMode = PlayMode.Random,
                ScopePath = scopePath,
                SearchQuery = null
            };
        }

        public void SetFilter(TeensyFilterType filterType) => _state = _state with { FilterType = filterType };
        public void SetDirectoryScope(string path) => _state = _state with { ScopePath = path };
        public void SetStreamTime(TimeSpan? timespan)
        {
            _state = _state with { PlayTimer = timespan };

            if (_state.CurrentItem is SongItem && _state.SidTimer is SidTimer.SongLength)
            {
                return;
            }

            if (_state.PlayTimer is not null)
            {
                StopStream();
                StartStream(_state.PlayTimer.Value);
            }
        }
        public void SetSidTimer(SidTimer value) => _state = _state with { SidTimer = value };

        public void SetStorage(TeensyStorageType storageType)
        {
            _state = _state with { StorageType = storageType };
            _storage.SwitchStorage(storageType);
        }

        private TeensyFileType[] GetFilterFileTypes()
        {
            var trSettings = _settingsService.GetSettings();
            return trSettings.GetFileTypes(_state.FilterType);
        }

        public Task TogglePlay() => Guarded(TogglePlayCore);

        private async Task TogglePlayCore()
        {
            var current = _state.CurrentItem;

            if(_state.PlayState is PlayState.Playing)
            {
                PauseStream();

                if(current is SongItem)
                {
                    await _devices.ToggleMusicAsync();
                    _alert.Publish($"{current.Name} has been paused.");
                    return;
                }
                await _devices.ResetAsync();
                _alert.Publish($"{current?.Name} has been stopped.");
                return;
            }
            if(current is null)
            {
                _alert.PublishError("Hit back and try starting a new stream.");
                return;
            }
            ResumeStream();

            _state = _state with { PlayState = PlayState.Playing };

            if (current is SongItem)
            {
                await _devices.ToggleMusicAsync();
            }
            else
            {
                var outcome = await _launch.LaunchFile(_state.StorageType, current);

                if (outcome.Status is not LaunchStatus.Launched)
                {
                    Refused(outcome.Message, $"Failed to launch {current.Name}.");
                    return;
                }
            }
            _alert.Publish($"{current.Name} has been resumed.");
        }
    }
}