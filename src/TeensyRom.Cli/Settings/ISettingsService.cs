using System.Reactive;

namespace TeensyRom.Cli.Settings
{
    public interface ISettingsService
    {
        IObservable<TeensySettings> Settings { get; }
        bool SaveSettings(TeensySettings settings);
        TeensySettings GetSettings();
    }
}