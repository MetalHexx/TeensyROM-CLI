using MediatR;
using Spectre.Console;
using TeensyRom.Cli.Helpers;
using TeensyRom.Core.Commands.File.LaunchFile;
using TeensyRom.Core.Player;
using TeensyRom.Core.Progress;
using TeensyRom.Core.Settings;
using TeensyRom.Core.Storage.Entities;
using TeensyRom.Core.Storage.Services;

namespace TeensyRom.Cli.Commands.TeensyRom.Services
{
    internal interface IPlayerService 
    {
        Task LaunchItem(TeensyStorageType storageType, ILaunchableItem item);
    }


    internal class PlayerService(IMediator mediator, ICachedStorageService storage, IProgressTimer progressTimer) : IPlayerService
    {
        private TeensyStorageType _selectedStorage = TeensyStorageType.SD;
        private StorageScope _selectedScope = StorageScope.DirDeep;
        private string _scopePath = "/";
        private string _path = "/";

        private PlayState _playState = PlayState.Stopped;
        private PlayMode _playMode = PlayMode.Shuffle;
        private TeensyFilterType _filterType = TeensyFilterType.All;

        private IDisposable? _progressSubscription;

        public async Task LaunchItem(TeensyStorageType storageType, ILaunchableItem item)
        {
            _selectedStorage = storageType;
            _path = item.Path;
            _playState = PlayState.Playing;

            _progressSubscription?.Dispose();

            if (item is SongItem songItem)
            {
                progressTimer.StartNewTimer(songItem.PlayLength);

                _progressSubscription = progressTimer.TimerComplete.Subscribe(async _ => 
                {                    
                    await PlayRandom(storageType, "/");
                });
            }            

            var result = await mediator.Send(new LaunchFileCommand(storageType, item));

            if (result.IsSuccess)
            {
                RadHelper.WriteTitle($"Now Playing: {item.Path}");
            }
            else
            {
                RadHelper.WriteError($"Error Launching: {item.Path}");
                await PlayRandom(_selectedStorage, _scopePath);
            }
            AnsiConsole.WriteLine("                                                                                                             ");
        }

        public async Task PlayRandom(TeensyStorageType storageType, string scopePath)
        {
            var item = storage.GetRandomFile(_selectedScope, _scopePath);

            if (item is null) return;

            await LaunchItem(storageType, item);
        }
    }
}
