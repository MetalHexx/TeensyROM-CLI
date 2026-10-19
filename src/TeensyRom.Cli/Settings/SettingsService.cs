using Newtonsoft.Json;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using TeensyRom.Cli.Common;
using TeensyRom.Cli.Logging;
using Formatting = Newtonsoft.Json.Formatting;

namespace TeensyRom.Cli.Settings
{
    public class SettingsService : ISettingsService
    {
        public IObservable<TeensySettings> Settings => _settings.AsObservable();

        private BehaviorSubject<TeensySettings> _settings;
        private readonly ILoggingService _logger;

        private string _settingsFilePath => Path.Combine(Assembly.GetExecutingAssembly().GetPath(), SettingsConstants.SettingsPath).GetOsFriendlyPath();

        public SettingsService(ILoggingService logger)
        {
            _settings = new BehaviorSubject<TeensySettings>(GetSettings());
            _logger = logger;
        }

        public TeensySettings GetSettings()
        {
            var settings = _settings?.Value;

            if (settings is not null)
            {
                return settings.GetClone();
            }

            var path = _settingsFilePath;

            if (File.Exists(path))
            {
                using var stream = File.Open(path, FileMode.Open, FileAccess.Read);
                using var reader = new StreamReader(stream);
                settings = Deserialize(reader.ReadToEnd());
            }
            if(settings is null)
            {
                settings = InitDefaultSettings();
                WriteSettings(settings);
            }
            ValidateAndLogSettings(settings);

            return settings;
        }

        /// <summary>
        /// Parses a settings file's content. Members the file carries that <see cref="TeensySettings"/> doesn't
        /// define (for example a COM port or banned lists) are ignored.
        /// </summary>
        public static TeensySettings? Deserialize(string json) =>
            JsonConvert.DeserializeObject<TeensySettings>(json, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.Indented
            });

        private TeensySettings InitDefaultSettings()
        {
            var settings = new TeensySettings();
            settings.InitializeDefaults();            
            return settings;
        }
        public bool SaveSettings(TeensySettings settings)
        {
            _logger.Internal($"Start Save Settings.");
            _logger.Internal($"Settings file path: {_settingsFilePath}");

            if (!ValidateAndLogSettings(settings)) 
            {
                _logger.InternalError($"Settings failed validation.  Please correct the errors and try again.");
                _logger.InternalError("Writing Settings value");
                var settingsString = JsonConvert.SerializeObject(settings, Formatting.Indented, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    Formatting = Formatting.Indented
                });                
                _logger.InternalError(settingsString);
                return false;
            }
            try
            {
                _logger.Internal($"Writing Settings to disk");
                WriteSettings(settings);
                _logger.Internal($"Successfully saved settings to disk");
                _settings.OnNext(settings with { });
            }
            catch (Exception ex)
            {
                _logger.InternalError("An error ocurred writing settings to disk");
                _logger.InternalError(ex.ToString());
            }
            _logger.Internal($"Exiting SaveSettings");
            return true;
        }

        private void WriteSettings(TeensySettings settings)
        {
            if (!Directory.Exists(Path.GetDirectoryName(_settingsFilePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_settingsFilePath)!);
            }
            File.WriteAllText(_settingsFilePath, JsonConvert.SerializeObject(settings, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
                Formatting = Formatting.Indented
            }));
        }

        public bool ValidateAndLogSettings(TeensySettings settings)
        {
            return true;
        }
    }
}