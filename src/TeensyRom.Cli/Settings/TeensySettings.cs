using TeensyRom.Cli.Storage.Entities;

namespace TeensyRom.Cli.Settings
{
    public enum TeensyFilterType
    {
        All,
        Games,
        Music,
        Hex,
        Images
    }

    /// <summary>
    /// The CLI's own preferences, persisted to <c>Settings.json</c>. Members a file carries that this
    /// class doesn't define are ignored on load and dropped on the next save.
    /// </summary>
    public record TeensySettings
    {
        public TeensyStorageType StorageType { get; set; } = TeensyStorageType.SD;
        public bool AlwaysPromptStorage { get; set; } = true;
        public List<TeensyTarget> FileTargets { get; set; } = [];
        public TeensyFilterType StartupFilter { get; set; } = TeensyFilterType.All;
        public bool EnableDebugLogs { get; set; } = false;
        public bool HasIndexed { get; set; } = false;

        public void InitializeDefaults()
        {
            FileTargets.AddRange(new List<TeensyTarget>
            {
                new TeensyTarget
                {
                    Type = TeensyFileType.Sid,
                    FilterType = TeensyFilterType.Music,
                    DisplayName = "SID",
                    Extension = ".sid"
                },
                new TeensyTarget
                {
                    Type = TeensyFileType.Prg,
                    FilterType = TeensyFilterType.Games,
                    DisplayName = "PRG",
                    Extension = ".prg"
                },
                new TeensyTarget
                {
                    Type = TeensyFileType.P00,
                    FilterType = TeensyFilterType.Games,
                    DisplayName = "P00",
                    Extension = ".p00"
                },
                new TeensyTarget
                {
                    Type = TeensyFileType.Crt,
                    FilterType = TeensyFilterType.Games,
                    DisplayName = "CRT",
                    Extension = ".crt"
                },
                new TeensyTarget
                {
                    Type = TeensyFileType.Hex,
                    FilterType = TeensyFilterType.Hex,
                    DisplayName = "HEX",
                    Extension = ".hex"
                },
                new TeensyTarget
                {
                    Type = TeensyFileType.Kla,
                    FilterType = TeensyFilterType.Images,
                    DisplayName = "KLA",
                    Extension = ".kla"
                },
                new TeensyTarget
                {
                    Type = TeensyFileType.Koa,
                    FilterType = TeensyFilterType.Images,
                    DisplayName = "KOA",
                    Extension = ".koa"
                },
                new TeensyTarget
                {
                    Type = TeensyFileType.Art,
                    FilterType = TeensyFilterType.Images,
                    DisplayName = "ART",
                    Extension = ".art"
                },
                new TeensyTarget
                {
                    Type = TeensyFileType.Aas,
                    FilterType = TeensyFilterType.Images,
                    DisplayName = "AAS",
                    Extension = ".aas"
                },
                new TeensyTarget
                {
                    Type = TeensyFileType.Hpi,
                    FilterType = TeensyFilterType.Images,
                    DisplayName = "HPI",
                    Extension = ".hpi"
                },
                new TeensyTarget
                {
                    Type = TeensyFileType.D64,
                    FilterType = TeensyFilterType.Games,
                    DisplayName = "D64",
                    Extension = ".d64"
                }
            });
        }

        public TeensyFileType[] GetFileTypes(TeensyFilterType filterType)
        {
            if (filterType == TeensyFilterType.All)
            {
                return FileTargets
                    .Where(ft => ft.Type != TeensyFileType.Hex)
                    .Select(t => t.Type).ToArray();
            }
            return FileTargets
                .Where(ft => ft.FilterType == filterType)
                .Select(t => t.Type).ToArray();
        }

        public TeensySettings GetClone() => this with
        {
            FileTargets = FileTargets.Select(t => new TeensyTarget
            {
                DisplayName = t.DisplayName,
                Extension = t.Extension,
                FilterType = t.FilterType,
                Type = t.Type
            }).ToList()
        };
    }
}
