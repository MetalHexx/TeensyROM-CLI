﻿using TeensyRom.Core.Player;
using TeensyRom.Core.Settings;
using TeensyRom.Core.Storage.Entities;

namespace TeensyRom.Cli.Services
{
    internal class PlayerSettings
    {
        public TeensyStorageType StorageType { get; set; }
        public ILaunchableItem? CurrentItem = null;
        public PlayState PlayState { get; set; } = PlayState.Stopped;
        public PlayMode PlayMode { get; set; } = PlayMode.Random;
        public TeensyFilterType FilterType { get; set; } = TeensyFilterType.All;
        public string ScopePath { get; set; } = "/";
        public TimeSpan? PlayTimer { get; set; } = null;
        public SidTimer SidTimer { get; set; } = SidTimer.SongLength;
        public string? SearchQuery { get; set; } = null;
    }
}