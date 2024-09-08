using TeensyRom.Cli.Commands.Main.Launcher;
using TeensyRom.Core.Commands.File.LaunchFile;
using TeensyRom.Core.Player;
using TeensyRom.Core.Settings;
using TeensyRom.Core.Storage.Entities;

namespace TeensyRom.Cli.Services
{
    internal interface IPlayerService
    {
        IObservable<ILaunchableItem> FileLaunched { get; }

        PlayerSettings GetPlayerSettings();
        Task<LaunchFileResult> LaunchItem(TeensyStorageType storageType, ILaunchableItem item);
        Task LaunchItem(TeensyStorageType storageType, string path);
        Task PlayNext();
        Task PlayPrevious();
        Task PlayRandom(TeensyStorageType storageType, string scopePath, TeensyFilterType filterType);
        void SetFilter(TeensyFilterType filterType);
        void SetStreamTime(TimeSpan? timespan);
        void SetSidTimer(SidTimer value);
        void StopStream();
        void SetDirectoryScope(string path);
        void SetSearchMode(string query);
        void SetDirectoryMode(string path);
        void SetRandomMode(string path);
    }
}
