using MediatR;
using Spectre.Console;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Commands;
using TeensyRom.Core.Commands.File.LaunchFile;
using TeensyRom.Core.Common;
using TeensyRom.Core.Logging;
using TeensyRom.Core.Player;
using TeensyRom.Core.Progress;
using TeensyRom.Core.Serial.State;
using TeensyRom.Core.Settings;
using TeensyRom.Core.Storage.Entities;
using TeensyRom.Core.Storage.Services;

namespace TeensyRom.Cli.Services.Player
{
    internal class PlayerService : IPlayerService
    {
        public IObservable<ILaunchableItem> FileLaunched => _fileLaunched.AsObservable();
        private Subject<ILaunchableItem> _fileLaunched = new();
        private PlayerState _state;

        private IDisposable? _timerSubscription;
        private readonly IMediator _mediator;
        private readonly ICachedStorageService _storage;
        private readonly IProgressTimer _timer;
        private readonly ISettingsService _settingsService;
        private readonly ISerialStateContext _serial;
        private readonly IPlayerFileContext _fileContext;
        private readonly IAlertService _alert;
        private readonly ILoggingService _log;

        public PlayerService(IMediator mediator, ICachedStorageService storage, IProgressTimer progressTimer, ISettingsService settingsService, ISerialStateContext serial, IPlayerFileContext fileContext, IAlertService alert, ILoggingService log)
        {
            _mediator = mediator;
            _storage = storage;
            _timer = progressTimer;
            _settingsService = settingsService;
            _serial = serial;
            _fileContext = fileContext;
            _alert = alert;
            _log = log;
            var settings = settingsService.GetSettings();

            _state = new()
            {
                StorageType = settings.StorageType,
                FilterType = settings.StartupFilter,
            };

            serial.CurrentState
                .Where(state => state is SerialConnectionLostState && _state.PlayState is PlayState.Playing)
                .Subscribe(_ => StopStream());

            SetupTimerSubscription();
        }

        public async Task LaunchFile(ILaunchableItem file)
        {
            await LaunchFile(file.Path);
        }

        public async Task LaunchFile(string filePath)
        {
            var fileItem = _fileContext.Find(filePath);            

            if (fileItem is null)
            {
                _alert.PublishError("File not found.");
                return;
            }            

            var launchSuccessful = await ExecuteLaunch(fileItem);

            if (!launchSuccessful)
            {
                await LaunchNext();
            }
            _fileContext.SetCurrentIndex(fileItem);
        }

        public async Task LaunchRandom()
        {
            SetRandomMode(_state.ScopePath);

            var randomItem = _storage.GetRandomFile(_state.Scope, _state.ScopePath, GetFilterFileTypes());

            if (randomItem is null)
            {
                _alert.PublishError($"No files for the filter \"{_state.FilterType}\" were found on the {_state.StorageType} in {_state.ScopePath}.");
                return;
            }
            var launchSuccessful = await ExecuteLaunch(randomItem);

            if (!launchSuccessful)
            {
                await LaunchNext();
                return;
            }
            _fileContext.Add(randomItem);
            return;

        }

        public async Task LaunchPrevious()
        {
            var wrapAround = _state.PlayMode is not PlayMode.Random;

            var fileToPlay = _fileContext.GetPrevious(wrapAround, GetFilterFileTypes());

            if (fileToPlay is null)
            {
                if (_state.CurrentItem is null)
                {
                    return;
                }
                fileToPlay = _state.CurrentItem;
            }
            var launchSuccessful = await ExecuteLaunch(fileToPlay);

            if (_fileContext.HasCompatibleFiles() is false)
            {
                StopStream();
                _alert.PublishError("No compatible files found. Try starting a different stream.");
                return;
            }

            if (!launchSuccessful)
            {
                await LaunchPrevious();
            }
        }

        public async Task LaunchNext()
        {
            var wrapAround = _state.PlayMode is not PlayMode.Random;

            var fileToPlay = _fileContext.GetNext(wrapAround, GetFilterFileTypes());

            if (_state.PlayMode is PlayMode.Random && fileToPlay is null)
            {
                await LaunchRandom();
                return;
            }
            if (_fileContext.HasCompatibleFiles() is false)
            {
                StopStream();
                _alert.PublishError("No compatible files found. Try starting a different stream.");
                return;
            }
            var launchSuccessful = await ExecuteLaunch(fileToPlay!);

            if (!launchSuccessful) await LaunchNext();
        }

        private async Task<bool> ExecuteLaunch(ILaunchableItem item)
        {
            _state = _state with
            {
                CurrentItem = item,
                PlayState = PlayState.Playing
            };
            if (!item.IsCompatible)
            {
                AlertBadFile(item);
                return false;
            }
            var result = await _mediator.Send(new LaunchFileCommand(_state.StorageType, item));

            if (result.IsSuccess)
            {
                if (_state.PlayMode is not PlayMode.Random)
                {
                    _fileContext.SetCurrentIndex(item);
                }
                _fileLaunched.OnNext(item);
                _alert.Publish(RadHelper.ClearHack);
                MaybeStartStream(item);
                return true;
            }
            if (result.LaunchResult is LaunchFileResultType.SidError or LaunchFileResultType.ProgramError)
            {
                _storage.MarkIncompatible(item);
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
                await LaunchNext();
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
            var files = _storage.Search(query, []);
            _fileContext.Load(files.OfType<ILaunchableItem>());
        }

        public async Task SetDirectoryMode(string filePath)
        {
            var directory = await _storage.GetDirectory(filePath);

            if (directory is null)
            {
                _alert.PublishError("Directory not found.");
                return;
            }
            if (directory.Files.Count == 0)
            {
                _alert.PublishError("No files found in directory.");
                return;
            };
            var files = directory.Files.OfType<ILaunchableItem>();

            if (!files.Any())
            {
                _alert.PublishError("No launchable files found in directory.");
                return;
            }
            _fileContext.Load(files);

            _state = _state with
            {
                PlayMode = PlayMode.CurrentDirectory,
                FilterType = TeensyFilterType.All,
                SearchQuery = null
            };

            _alert.Publish("Switching filter to \"All\"");
        }

        public void SetRandomMode(string scopePath)
        {
            if (_state.PlayMode is not PlayMode.Random)
            {
                _fileContext.Clear();
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

        public void TogglePlay()
        {
            if (_state.PlayState is PlayState.Playing)
            {
                PauseStream();

                if (_state.CurrentItem is SongItem)
                {
                    _mediator.Send(new ToggleMusicCommand());
                    _alert.Publish($"{_state.CurrentItem.Name} has been paused.");
                    return;
                }
                _mediator.Send(new ResetCommand());
                _alert.Publish($"{_state.CurrentItem?.Name} has been stopped.");
                return;
            }
            if (_state.CurrentItem is null)
            {
                _alert.PublishError("Hit back and try starting a new stream.");
                return;
            }
            ResumeStream();

            _state = _state with { PlayState = PlayState.Playing };

            if (_state.CurrentItem is SongItem)
            {
                _mediator.Send(new ToggleMusicCommand());
            }
            else
            {
                _mediator.Send(new LaunchFileCommand(_state.StorageType, _state.CurrentItem));
            }
            _alert.Publish($"{_state.CurrentItem.Name} has been resumed.");
        }
    }
}