using TeensyRom.Cli.Commands.Main.Launcher;
using TeensyRom.Core.Commands.File.LaunchFile;
using TeensyRom.Core.Player;
using TeensyRom.Core.Settings;
using TeensyRom.Core.Storage.Entities;

namespace TeensyRom.Cli.Services.Player
{
    internal interface IPlayerService
    {
        IObservable<ILaunchableItem> FileLaunched { get; }

        PlayerState GetState();
        Task LaunchFile(string filePath);
        Task LaunchFile(ILaunchableItem file);
        Task LaunchNext();
        Task LaunchPrevious();
        Task LaunchRandom();
        void SetFilter(TeensyFilterType filterType);
        void SetStreamTime(TimeSpan? timespan);
        void SetSidTimer(SidTimer value);
        void StopStream();
        void SetDirectoryScope(string path);
        void SetSearchMode(string query);
        Task SetDirectoryMode(string path);
        void SetRandomMode(string path);
        void TogglePlay();
        void SetStorage(TeensyStorageType storageType);
    }
}
