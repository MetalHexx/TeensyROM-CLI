using NSubstitute;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using TeensyRom.Cli.Api;
using TeensyRom.Cli.Storage.Entities;
using TeensyRom.Cli.Storage.Services;
using TeensyRom.Cli.Progress;
using TeensyRom.Cli.Settings;
using System.Reactive.Linq;
using TeensyRom.Cli.Player;
using System.Reactive.Subjects;
using TeensyRom.Cli.Common;
using TeensyRom.Cli.Services;
using System.Reactive;
using System.Threading.Tasks;
using System;
using TeensyRom.Cli.Logging;

namespace TeensyRom.Cli.Tests
{
    public class PlayerServiceTests
    {
        private IStorageService _storageService;
        private ILaunchService _launchService;
        private IDeviceService _deviceService;
        private IAlertService _alert;
        private IProgressTimer _progressTimer;
        private ISettingsService _settingsService;
        private ILaunchHistory _launchHistory;
        private const string _sidPath = "/music/MUSIC/1.sid";

        private IFixture _fixture = new Fixture().Customize(new AutoNSubstituteCustomization() { ConfigureMembers = true });

        public PlayerServiceTests()
        {
            _storageService = _fixture.Freeze<IStorageService>();
            _launchService = _fixture.Freeze<ILaunchService>();
            _deviceService = _fixture.Freeze<IDeviceService>();
            _alert = _fixture.Freeze<IAlertService>();
            _progressTimer = _fixture.Freeze<IProgressTimer>();

            _deviceService.ToggleMusicAsync().Returns(Task.CompletedTask);
            _deviceService.ResetAsync().Returns(Task.CompletedTask);
            SetupLaunch();
            _launchService
                .LaunchRandom(Any<TeensyStorageType>(), Any<StorageScope>(), Any<string>(), Any<TeensyFilterType>())
                .Returns(_ => Outcome(LaunchStatus.Launched, CreateFile<SongItem>("/music/random.sid")));

            _settingsService = _fixture.Freeze<ISettingsService>();
            var settings = new TeensySettings();
            settings.InitializeDefaults();
            _settingsService.GetSettings().Returns(s => settings);

            _launchHistory = new LaunchHistory();
            _fixture.Inject(_launchHistory);
        }

        [Fact]
        public void Given_PlayerFirstInitialization_And_SDStorage_Then_SettingsCorrect()
        {
            //Arrange
            SetupSettingsWithStorage(TeensyStorageType.SD);

            var expectedSettings = new PlayerState
            {
                CurrentItem = null,
                StorageType = TeensyStorageType.SD,
                PlayState = PlayState.Stopped,
                PlayMode = PlayMode.Random,
                FilterType = TeensyFilterType.All,
                ScopePath = "/",
                SidTimer = SidTimer.SongLength,
                PlayTimer = null,
                SearchQuery = null
            };

            //Act
            var playerService = _fixture.Create<PlayerService>();
            var playerSettings = playerService.GetState();

            //Assert
            playerSettings.Should().BeEquivalentTo(expectedSettings);
        }

        [Fact]
        public void Given_PlayerFirstInitialization_And_USBStorage_Then_SettingsCorrect()
        {
            //Arrange
            SetupSettingsWithStorage(TeensyStorageType.USB);

            var expectedSettings = new PlayerState
            {
                CurrentItem = null,
                StorageType = TeensyStorageType.USB,
                PlayState = PlayState.Stopped,
                PlayMode = PlayMode.Random,
                FilterType = TeensyFilterType.All,
                ScopePath = "/",
                SidTimer = SidTimer.SongLength,
                PlayTimer = null,
                SearchQuery = null
            };

            //Act
            var playerService = _fixture.Create<PlayerService>();
            var playerSettings = playerService.GetState();

            //Assert
            playerSettings.Should().BeEquivalentTo(expectedSettings);
        }

        [Fact]
        public void Given_PlayerFirstInitialization_And_AllFilter_Then_SettingsCorrect()
        {
            //Arrange
            SetupSettingsWithFilter(TeensyFilterType.All);

            var expectedSettings = new PlayerState
            {
                CurrentItem = null,
                StorageType = TeensyStorageType.SD,
                PlayState = PlayState.Stopped,
                PlayMode = PlayMode.Random,
                FilterType = TeensyFilterType.All,
                ScopePath = "/",
                SidTimer = SidTimer.SongLength,
                PlayTimer = null,
                SearchQuery = null
            };

            //Act
            var playerService = _fixture.Create<PlayerService>();
            var playerSettings = playerService.GetState();

            //Assert
            playerSettings.Should().BeEquivalentTo(expectedSettings);
        }

        [Fact]
        public void Given_PlayerFirstInitialization_And_GameFilter_Then_SettingsCorrect()
        {
            //Arrange
            SetupSettingsWithFilter(TeensyFilterType.Games);

            var expectedSettings = new PlayerState
            {
                CurrentItem = null,
                StorageType = TeensyStorageType.SD,
                PlayState = PlayState.Stopped,
                PlayMode = PlayMode.Random,
                FilterType = TeensyFilterType.Games,
                ScopePath = "/",
                SidTimer = SidTimer.SongLength,
                PlayTimer = null,
                SearchQuery = null
            };

            //Act
            var playerService = _fixture.Create<PlayerService>();
            var playerSettings = playerService.GetState();

            //Assert
            playerSettings.Should().BeEquivalentTo(expectedSettings);
        }

        [Fact]
        public void Given_PlayerFirstInitialization_And_SetScope_Then_SettingsCorrect()
        {
            //Arrange
            var expectedSettings = new PlayerState
            {
                ScopePath = "/music",
            };

            //Act
            var playerService = _fixture.Create<PlayerService>();
            playerService.SetDirectoryScope("/images");
            playerService.SetDirectoryScope("/music");
            var playerSettings = playerService.GetState();

            //Assert
            playerSettings.Should().BeEquivalentTo(expectedSettings);
        }

        [Fact]
        public void Given_PlayerFirstInitialization_And_SetSearchMode_ThenSettingsCorrect()
        {
            //Arrange
            var expectedSettings = new PlayerState
            {
                PlayMode = PlayMode.Search,
                SearchQuery = "this is a query",
                SidTimer = SidTimer.SongLength,
                PlayTimer = null
            };

            //Act
            var playerService = _fixture.Create<PlayerService>();
            playerService.SetSearchMode("this is a query");
            var playerSettings = playerService.GetState();

            //Assert
            playerSettings.Should().BeEquivalentTo(expectedSettings);
        }

        [Fact]
        public void Given_PlayerFirstInitialization_And_SetDirectoryMode_Then_SettingsCorrect()
        {
            //Arrange
            var expectedSettings = new PlayerState
            {
                PlayMode = PlayMode.CurrentDirectory,
                ScopePath = "/music",
                SidTimer = SidTimer.SongLength,
                PlayTimer = null,
                SearchQuery = null
            };

            //Act
            var playerService = _fixture.Create<PlayerService>();
            playerService.SetSearchMode("this is a query");
            playerService.SetDirectoryMode("/music");
            var playerSettings = playerService.GetState();

            //Assert
            playerSettings.Should().BeEquivalentTo(expectedSettings);
        }

        [Fact]
        public void Given_PlayerFirstInitialization_And_SetRandom_Then_SettingsCorrect()
        {
            //Arrange
            var expectedSettings = new PlayerState
            {
                PlayMode = PlayMode.Random,
                ScopePath = "/music",
                SidTimer = SidTimer.SongLength,
                PlayTimer = null,
                SearchQuery = null
            };

            //Act
            var playerService = _fixture.Create<PlayerService>();
            playerService.SetSearchMode("this is a query");
            playerService.SetRandomMode("/music");
            var playerSettings = playerService.GetState();

            //Assert
            playerSettings.Should().BeEquivalentTo(expectedSettings);
        }

        [Fact]
        public void Given_PlayerFirstInitialization_And_SetFilter_Then_SettingsCorrect()
        {
            //Arrange
            var expectedSettings = new PlayerState
            {
                PlayMode = PlayMode.Random,
                ScopePath = "/music",
                SidTimer = SidTimer.SongLength,
                PlayTimer = null,
                SearchQuery = null
            };

            //Act
            var playerService = _fixture.Create<PlayerService>();
            playerService.SetSearchMode("this is a query");
            playerService.SetRandomMode("/music");
            var playerSettings = playerService.GetState();

            //Assert
            playerSettings.Should().BeEquivalentTo(expectedSettings);
        }

        [Fact]
        public void Given_PlayerFirstInitialization_And_SetStreamTime_Then_SettingsCorrect()
        {
            //Arrange
            var expectedSettings = new PlayerState
            {
                PlayState = PlayState.Playing,
                PlayMode = PlayMode.Random,
                PlayTimer = TimeSpan.FromSeconds(3),
            };

            //Act
            var playerService = _fixture.Create<PlayerService>();
            playerService.SetStreamTime(TimeSpan.FromSeconds(1));
            playerService.SetStreamTime(expectedSettings.PlayTimer);
            var playerSettings = playerService.GetState();

            //Assert
            playerSettings.Should().BeEquivalentTo(expectedSettings);
        }

        [Fact]
        public void Given_PlayerFirstInitialization_And_SetSidTimer_Then_SettingsCorrect()
        {
            //Arrange
            var expectedSettings = new PlayerState
            {
                SidTimer = SidTimer.TimerOverride
            };

            //Act
            var playerService = _fixture.Create<PlayerService>();
            playerService.SetSidTimer(SidTimer.SongLength);
            playerService.SetSidTimer(SidTimer.TimerOverride);
            var playerSettings = playerService.GetState();

            //Assert
            playerSettings.Should().BeEquivalentTo(expectedSettings);
        }

        [Theory]
        [InlineData(TeensyStorageType.SD)]
        [InlineData(TeensyStorageType.USB)]
        public void When_StorageSet_Then_StorageServiceSwitches_And_StateFollows(TeensyStorageType storageType)
        {
            //Act
            var playerService = _fixture.Create<PlayerService>();
            playerService.SetStorage(storageType);

            //Assert
            _storageService.Received(1).SwitchStorage(storageType);
            playerService.GetState().StorageType.Should().Be(storageType);
        }

        [Fact]
        public async Task When_SetSidTimerOverride_PlayTimerReset()
        {
            //Arrange
            SetupStorageService(CreateFile<SongItem>("/music/sid1.sid"));

            //Act
            var playerService = _fixture.Create<PlayerService>();

            await playerService.LaunchFromDirectory("/music/sid1.sid");
            playerService.SetSidTimer(SidTimer.TimerOverride);
            playerService.SetStreamTime(TimeSpan.FromDays(1));

            _progressTimer.Received(1).StartNewTimer(TimeSpan.FromDays(1));
        }

        [Fact]
        public async Task Given_SidTimeSongLength_When_SetStreamTime_Then_PlayTimerNotReset()
        {
            //Arrange
            SetupStorageService(CreateFile<SongItem>("/music/sid1.sid"));

            //Act
            var playerService = _fixture.Create<PlayerService>();

            playerService.SetSidTimer(SidTimer.SongLength);
            await playerService.LaunchFromDirectory("/music/sid1.sid");
            playerService.SetStreamTime(TimeSpan.FromDays(1));
            _progressTimer.Received(0).StartNewTimer(TimeSpan.FromDays(1));
        }

        [Fact]
        public async Task Given_SidTimerSongLength_When_SongLaunched_Then_TimerRunsForTheSongLength()
        {
            //Arrange
            var song = CreateFile<SongItem>("/music/sid1.sid");
            song.PlayLength = TimeSpan.FromSeconds(97);
            SetupStorageService(song);

            //Act
            var playerService = _fixture.Create<PlayerService>();
            playerService.SetSidTimer(SidTimer.SongLength);
            playerService.SetStreamTime(TimeSpan.FromMinutes(10));
            await playerService.LaunchFromDirectory(song.Path);

            //Assert
            _progressTimer.Received(1).StartNewTimer(TimeSpan.FromSeconds(97));
        }

        [Fact]
        public async Task Given_SidTimerOverride_When_SongLaunched_Then_TimerRunsForTheOverride()
        {
            //Arrange
            var song = CreateFile<SongItem>("/music/sid1.sid");
            song.PlayLength = TimeSpan.FromSeconds(97);
            SetupStorageService(song);

            //Act
            var playerService = _fixture.Create<PlayerService>();
            playerService.SetSidTimer(SidTimer.TimerOverride);
            playerService.SetStreamTime(TimeSpan.FromMinutes(10));
            await playerService.LaunchFromDirectory(song.Path);

            //Assert
            _progressTimer.Received(2).StartNewTimer(TimeSpan.FromMinutes(10));
            _progressTimer.DidNotReceive().StartNewTimer(TimeSpan.FromSeconds(97));
        }

        [Fact]
        public async Task When_FileLaunched_Then_Emit()
        {
            //Arrange
            var playerService = _fixture.Create<PlayerService>();
            var expectedFile = CreateFile<SongItem>("/music/sid1.sid");
            SetupStorageService(expectedFile);

            //Act
            var tcs = new TaskCompletionSource<ILaunchableItem>();
            playerService.FileLaunched.Subscribe(tcs.SetResult);

            await playerService.LaunchFromDirectory("/music/sid1.sid");

            var actualFile = await tcs.Task;

            // Assert
            actualFile.Should().BeEquivalentTo(expectedFile);
        }

        [Theory]
        [InlineData(TeensyStorageType.USB)]
        [InlineData(TeensyStorageType.SD)]
        public async Task Given_FileDoesNotExist_When_LaunchedRequested_Then_SettingsAreCorrect(TeensyStorageType storageType)
        {
            //Arrange
            PlayerState expectedSettings = new();
            expectedSettings.CurrentItem = null;
            expectedSettings.StorageType = storageType;
            expectedSettings.PlayState = PlayState.Stopped;

            //Act
            var playerService = _fixture.Create<PlayerService>();
            playerService.SetStorage(storageType);
            await playerService.LaunchFromDirectory("/music/MUSIC/doesntExist.sid");
            var playerSettings = playerService.GetState();

            //Assert
            playerSettings.Should().BeEquivalentTo(expectedSettings);
        }

        [Fact]
        public async Task Given_FileDoesNotExist_When_LaunchedRequested_Then_FileDoesNotLaunch()
        {
            //Act
            var playerService = _fixture.Create<PlayerService>();
            await playerService.LaunchFromDirectory("/music/MUSIC/doesntExist.sid");

            //Assert
            await _launchService.DidNotReceive().LaunchFile(Any<TeensyStorageType>(), Any<ILaunchableItem>());
        }

        [Fact]
        public async Task Given_FileDoesNotExist_When_LaunchedRequested_Then_TimerDoesNotStart()
        {
            //Act
            var playerService = _fixture.Create<PlayerService>();
            await playerService.LaunchFromDirectory("/music/MUSIC/doesntExist.sid");

            //Assert
            _progressTimer.DidNotReceive().StartNewTimer(Arg.Any<TimeSpan>());
        }


        [Theory]
        [InlineData(TeensyStorageType.USB)]
        [InlineData(TeensyStorageType.SD)]
        public async Task Given_DirectoryDoesNotExist_When_Launched_Then_SettingsAreCorrect(TeensyStorageType storageType)
        {
            var existingSong = CreateFile<SongItem>("/");
            existingSong.Path = "/music/MUSIC/1.sid";
            SeedStorageDirectory([]);

            //Arrange
            PlayerState expectedSettings = new();
            expectedSettings.CurrentItem = null;
            expectedSettings.StorageType = storageType;
            expectedSettings.PlayState = PlayState.Stopped;

            //Act
            var playerService = _fixture.Create<PlayerService>();
            playerService.SetStorage(storageType);
            await playerService.LaunchFromDirectory("/music/MUSIC/doesntExist.sid");
            var playerSettings = playerService.GetState();

            //Assert
            playerSettings.Should().BeEquivalentTo(expectedSettings);
        }

        [Fact]
        public async Task Given_DirectoryDoesNotExist_When_Launched_Then_FileDoesNotLaunch()
        {
            //Arrange
            var existingSong = CreateFile<SongItem>("/");
            existingSong.Path = "/music/MUSIC/1.sid";
            SeedStorageDirectory([]);

            //Act
            var playerService = _fixture.Create<PlayerService>();
            await playerService.LaunchFromDirectory("/music/MUSIC/doesntExist.sid");
            var playerSettings = playerService.GetState();

            //Assert
            await _launchService.DidNotReceive().LaunchFile(Any<TeensyStorageType>(), Any<ILaunchableItem>());
        }

        [Fact]
        public async Task Given_DirectoryDoesNotExist_When_Launched_Then_TimerDoesNotStart()
        {
            //Arrange
            var existingSong = CreateFile<SongItem>("/music/MUSIC/1.sid");
            SeedStorageDirectory([]);

            //Act
            var playerService = _fixture.Create<PlayerService>();
            await playerService.LaunchFromDirectory("/music/MUSIC/doesntExist.sid");
            var playerSettings = playerService.GetState();

            //Assert
            _progressTimer.DidNotReceive().StartNewTimer(Arg.Any<TimeSpan>());
        }

        [Fact]
        public async Task Given_ApiRefusesTheDirectory_When_Launched_Then_ItsMessageIsPublished_And_NothingLaunches()
        {
            //Arrange
            var message = "The storage USB is not available.";
            _storageService.GetDirectory(Any<string>()).Returns(Task.FromException<DirectoryListing>(new TeensyApiException(message, 404)));

            //Act
            var playerService = _fixture.Create<PlayerService>();
            await playerService.LaunchFromDirectory("/music/MUSIC/1.sid");

            //Assert
            _alert.Received(1).PublishError(message);
            playerService.GetState().PlayState.Should().Be(PlayState.Stopped);
            await _launchService.DidNotReceive().LaunchFile(Any<TeensyStorageType>(), Any<ILaunchableItem>());
        }

        private void SeedStorageDirectory(List<IFileItem> items)
        {
            _storageService.GetDirectory(Arg.Any<string>()).Returns(new DirectoryListing
            {
                Files = items
            });
        }

        [Theory]
        [InlineData(TeensyStorageType.USB)]
        [InlineData(TeensyStorageType.SD)]
        public async Task Given_FileExists_When_LaunchedRequested_Then_SettingsAreCorrect(TeensyStorageType storageType)
        {
            //Arrange
            var existingSong = CreateFile<SongItem>("/");
            SetupStorageService(existingSong);

            PlayerState expectedSettings = new();
            expectedSettings.FilterType = TeensyFilterType.All;
            expectedSettings.CurrentItem = existingSong;
            expectedSettings.StorageType = storageType;
            expectedSettings.PlayState = PlayState.Playing;

            //Act
            var playerService = _fixture.Create<PlayerService>();
            playerService.SetStorage(storageType);
            await playerService.LaunchFromDirectory(existingSong.Path);
            var playerSettings = playerService.GetState();

            //Assert
            playerSettings.Should().BeEquivalentTo(expectedSettings);
            await _launchService.Received(1).LaunchFile(storageType, existingSong);
        }

        [Fact]
        public async Task Given_FileExists_When_LaunchRequested_Then_FileLaunched()
        {
            //Arrange
            var existingSong = CreateFile<SongItem>("/");
            SetupStorageService(existingSong);

            //Act
            var playerService = _fixture.Create<PlayerService>();
            await playerService.LaunchFromDirectory(existingSong.Path);

            //Assert
            _launchService.ReceivedCalls().Should().HaveCount(1);
        }

        [Theory]
        [InlineData(TeensyFilterType.All)]
        [InlineData(TeensyFilterType.Music)]
        [InlineData(TeensyFilterType.Games)]
        [InlineData(TeensyFilterType.Images)]
        public async Task Given_ModeIsRandom_When_FilePlayEnds_Then_NextRandomPlayed_OfFilterType(TeensyFilterType filter)
        {
            //Arrange
            var timer = SetupTimer();
            SetupSettingsWithFilter(filter);
            var existingSong = CreateFile<SongItem>("/");
            SetupStorageService(existingSong);

            //Act
            var playerService = _fixture.Create<PlayerService>();
            await playerService.LaunchItem(existingSong);

            timer.OnNext(Unit.Default);

            await _launchService.Received(1).LaunchFile(Any<TeensyStorageType>(), Any<ILaunchableItem>());
            await _launchService.Received(1).LaunchRandom(TeensyStorageType.SD, StorageScope.DirDeep, "/", filter);
        }

        [Fact]
        public async Task Given_ModeIsRandom_When_FilePlayEnds_Then_NextRandomPlayed_WithinScope()
        {
            //Arrange
            var timer = SetupTimer();
            var existingSong = CreateFile<SongItem>("/music/MUSIC/1.sid");
            SetupStorageService(existingSong);
            var expectedScope = "/";

            //Act
            var playerService = _fixture.Create<PlayerService>();
            playerService.SetDirectoryScope(expectedScope);

            timer.OnNext(Unit.Default);

            await _launchService.Received(1).LaunchRandom(Any<TeensyStorageType>(), Any<StorageScope>(), expectedScope, Any<TeensyFilterType>());
        }

        [Fact]
        public async Task Given_DirectoryMode_When_FilePlayEnds_Then_NextDirectoryFilePlayed()
        {
            //Arrange
            var timer = SetupTimer();
            var currentFile = CreateFile<SongItem>("/music/2.sid");
            var expectedFile = CreateFile<SongItem>("/music/3.sid");

            SeedStorageDirectory(
            [
                CreateFile<SongItem>("/music/1.sid"),
                currentFile,
                expectedFile,
                CreateFile<SongItem>("/music/4.sid"),
            ]);
            var expectedSettings = new PlayerState
            {
                CurrentItem = expectedFile,
                PlayState = PlayState.Playing,
                ScopePath = expectedFile.Path.GetUnixParentPath(),
                PlayMode = PlayMode.CurrentDirectory
            };

            //Act
            var playerService = _fixture.Create<PlayerService>();
            playerService.SetDirectoryMode(currentFile.Path.GetUnixParentPath());
            await playerService.LaunchFromDirectory(currentFile.Path);
            timer.OnNext(Unit.Default);
            var resultingSettings = playerService.GetState();

            resultingSettings.Should().BeEquivalentTo(expectedSettings);
            await _launchService.Received(2).LaunchFile(Any<TeensyStorageType>(), Any<ILaunchableItem>());
        }

        [Fact]
        public async Task Given_DirectoryMode_When_FilePlayEnds_AndLastFile_Then_FirstPlayed()
        {
            //Arrange
            var timer = SetupTimer();
            var currentFile = CreateFile<SongItem>("/music/4.sid");
            var expectedFile = CreateFile<SongItem>("/music/1.sid");

            SeedStorageDirectory(
            [
                expectedFile,
                CreateFile<SongItem>("/music/2.sid"),
                CreateFile<SongItem>("/music/3.sid"),
                currentFile,
            ]);
            var expectedSettings = new PlayerState
            {
                CurrentItem = expectedFile,
                PlayState = PlayState.Playing,
                ScopePath = expectedFile.Path.GetUnixParentPath(),
                PlayMode = PlayMode.CurrentDirectory
            };

            //Act
            var playerService = _fixture.Create<PlayerService>();
            playerService.SetDirectoryMode(currentFile.Path.GetUnixParentPath());
            await playerService.LaunchFromDirectory(currentFile.Path);
            timer.OnNext(Unit.Default);
            var resultingSettings = playerService.GetState();

            resultingSettings.Should().BeEquivalentTo(expectedSettings);
            await _launchService.Received(2).LaunchFile(Any<TeensyStorageType>(), Any<ILaunchableItem>());
        }


        [Fact]
        public async Task Given_DirectoryMode_When_FilePlayEnds_Then_NextFilePlayed()
        {
            //Arrange
            SetupSettingsWithFilter(TeensyFilterType.All);
            var timer = SetupTimer();
            var currentFile = CreateFile<SongItem>("/files/2.sid");
            var expectedFile = CreateFile<GameItem>("/files/3.crt");

            SeedStorageDirectory(
            [
                CreateFile<GameItem>("/files/1.crt"),
                currentFile,
                expectedFile,
                CreateFile<SongItem>("/files/4.sid"),
            ]);
            var expectedSettings = new PlayerState
            {
                FilterType = TeensyFilterType.All,
                CurrentItem = expectedFile,
                PlayState = PlayState.Playing,
                ScopePath = expectedFile.Path.GetUnixParentPath(),
                PlayMode = PlayMode.CurrentDirectory
            };

            //Act
            var playerService = _fixture.Create<PlayerService>();
            playerService.SetDirectoryMode(currentFile.Path.GetUnixParentPath());
            await playerService.LaunchFromDirectory(currentFile.Path);
            timer.OnNext(Unit.Default);
            var resultingSettings = playerService.GetState();

            resultingSettings.Should().BeEquivalentTo(expectedSettings);
            await _launchService.Received(2).LaunchFile(Any<TeensyStorageType>(), Any<ILaunchableItem>());
        }

        [Fact]
        public async Task Given_DirectoryMode_And_FilterIsMusic_When_FilePlayEnds_Then_NextFilePlayed()
        {
            //Arrange
            SetupSettingsWithFilter(TeensyFilterType.All);
            var timer = SetupTimer();
            var currentFile = CreateFile<SongItem>("/files/2.sid");
            var expectedFile = CreateFile<SongItem>("/files/4.sid");

            SeedStorageDirectory(
            [
                CreateFile<GameItem>("/files/1.crt"),
                currentFile,
                CreateFile<GameItem>("/files/3.crt"),
                expectedFile,
            ]);
            var expectedSettings = new PlayerState
            {
                FilterType = TeensyFilterType.Music,
                CurrentItem = expectedFile,
                PlayState = PlayState.Playing,
                ScopePath = expectedFile.Path.GetUnixParentPath(),
                PlayMode = PlayMode.CurrentDirectory
            };

            //Act
            var playerService = _fixture.Create<PlayerService>();
            playerService.SetDirectoryMode(currentFile.Path.GetUnixParentPath());
            playerService.SetFilter(TeensyFilterType.Music);
            await playerService.LaunchFromDirectory(currentFile.Path);
            timer.OnNext(Unit.Default);
            var resultingSettings = playerService.GetState();

            resultingSettings.Should().BeEquivalentTo(expectedSettings);
            await _launchService.Received(2).LaunchFile(Any<TeensyStorageType>(), Any<ILaunchableItem>());
        }

        [Fact]
        public async Task Given_FilterSetToMusic_When_DirectoryModeSelected_FilterChangesToAll()
        {
            //Arrange
            SetupSettingsWithFilter(TeensyFilterType.Music);

            var expectedFile = CreateFile<SongItem>("/files/4.sid");

            SeedStorageDirectory(
            [
                expectedFile,
            ]);
            var expectedSettings = new PlayerState
            {
                FilterType = TeensyFilterType.All,
                CurrentItem = expectedFile,
                PlayState = PlayState.Playing,
                ScopePath = expectedFile.Path.GetUnixParentPath(),
                PlayMode = PlayMode.CurrentDirectory
            };

            //Act
            var playerService = _fixture.Create<PlayerService>();
            await playerService.LaunchFromDirectory(expectedFile.Path);
            playerService.SetDirectoryMode(expectedFile.Path.GetUnixParentPath());
            var resultingSettings = playerService.GetState();

            resultingSettings.Should().BeEquivalentTo(expectedSettings);
        }

        [Fact]
        public async Task Given_DirectoryMode_AndFilterChanged_When_FilePlayEnds_And_OutOfRange_Then_NextFilePlayed()
        {
            //Arrange
            SetupSettingsWithFilter(TeensyFilterType.Music);
            var timer = SetupTimer();
            var currentFile = CreateFile<SongItem>("/files/2.sid");
            var expectedFile = CreateFile<SongItem>("/files/6.sid");

            SeedStorageDirectory(
            [
                CreateFile<GameItem>("/files/1.crt"),
                currentFile,
                CreateFile<GameItem>("/files/3.crt"),
                CreateFile<GameItem>("/files/4.crt"),
                CreateFile<GameItem>("/files/5.crt"),
                expectedFile,
                CreateFile<SongItem>("/files/7.sid"),
                CreateFile<SongItem>("/files/8.sid"),
                CreateFile<GameItem>("/files/9.crt"),
            ]);
            var expectedSettings = new PlayerState
            {
                FilterType = TeensyFilterType.Music,
                CurrentItem = expectedFile,
                PlayState = PlayState.Playing,
                ScopePath = expectedFile.Path.GetUnixParentPath(),
                PlayMode = PlayMode.CurrentDirectory
            };

            //Act
            var playerService = _fixture.Create<PlayerService>();
            playerService.SetDirectoryMode(currentFile.Path.GetUnixParentPath());
            playerService.SetFilter(TeensyFilterType.Music);
            await playerService.LaunchFromDirectory(currentFile.Path);
            timer.OnNext(Unit.Default);
            var resultingSettings = playerService.GetState();

            resultingSettings.Should().BeEquivalentTo(expectedSettings);
            await _launchService.Received(2).LaunchFile(Any<TeensyStorageType>(), Any<ILaunchableItem>());
        }

        [Fact]
        public async Task Given_DirectoryMode_And_FilterChange_When_FilePlayEnds_And_OutOfRange_Then_WrapAround_And_PlayNextFile()
        {
            //Arrange
            SetupSettingsWithFilter(TeensyFilterType.Music);
            var timer = SetupTimer();
            var currentFile = CreateFile<GameItem>("/files/6.crt");
            var expectedFile = CreateFile<SongItem>("/files/1.sid");

            SeedStorageDirectory(
            [
                expectedFile,
                CreateFile<GameItem>("/files/1.crt"),
                CreateFile<GameItem>("/files/3.crt"),
                CreateFile<GameItem>("/files/4.crt"),
                CreateFile<GameItem>("/files/5.crt"),
                currentFile,
                CreateFile<GameItem>("/files/7.crt"),
                CreateFile<GameItem>("/files/8.crt"),
                CreateFile<GameItem>("/files/9.crt"),
            ]);
            var expectedSettings = new PlayerState
            {
                FilterType = TeensyFilterType.Music,
                CurrentItem = expectedFile,
                PlayState = PlayState.Playing,
                ScopePath = expectedFile.Path.GetUnixParentPath(),
                PlayMode = PlayMode.CurrentDirectory
            };

            //Act
            var playerService = _fixture.Create<PlayerService>();
            playerService.SetDirectoryMode(currentFile.Path.GetUnixParentPath());
            playerService.SetFilter(TeensyFilterType.Music);
            await playerService.LaunchFromDirectory(currentFile.Path);
            timer.OnNext(Unit.Default);
            var resultingSettings = playerService.GetState();

            resultingSettings.Should().BeEquivalentTo(expectedSettings);
            await _launchService.Received(2).LaunchFile(Any<TeensyStorageType>(), Any<ILaunchableItem>());
        }

        [Fact]
        public async Task Given_DirectoryMode_When_Previous_Then_PreviousFilePlayed()
        {
            //Arrange
            var timer = SetupTimer();
            var currentFile = CreateFile<SongItem>("/music/2.sid");
            var expectedFile = CreateFile<SongItem>("/music/3.sid");

            SeedStorageDirectory(
            [
                CreateFile<SongItem>("/music/1.sid"),
                expectedFile,
                currentFile,
                CreateFile<SongItem>("/music/4.sid"),
            ]);
            var expectedSettings = new PlayerState
            {
                CurrentItem = expectedFile,
                PlayState = PlayState.Playing,
                ScopePath = expectedFile.Path.GetUnixParentPath(),
                PlayMode = PlayMode.CurrentDirectory
            };

            //Act
            var playerService = _fixture.Create<PlayerService>();
            playerService.SetDirectoryMode(currentFile.Path.GetUnixParentPath());
            await playerService.LaunchFromDirectory(currentFile.Path);
            await playerService.PlayPrevious();
            var resultingSettings = playerService.GetState();

            resultingSettings.Should().BeEquivalentTo(expectedSettings);
            await _launchService.Received(2).LaunchFile(Any<TeensyStorageType>(), Any<ILaunchableItem>());
        }

        [Fact]
        public async Task Given_DirectoryMode_AndLastFile_When_Previous_Then_FirstPlayed()
        {
            //Arrange
            var timer = SetupTimer();
            var currentFile = CreateFile<SongItem>("/music/4.sid");
            var expectedFile = CreateFile<SongItem>("/music/1.sid");

            SeedStorageDirectory(
            [
                currentFile,
                CreateFile<SongItem>("/music/2.sid"),
                CreateFile<SongItem>("/music/3.sid"),
                expectedFile,

            ]);
            var expectedSettings = new PlayerState
            {
                CurrentItem = expectedFile,
                PlayState = PlayState.Playing,
                ScopePath = expectedFile.Path.GetUnixParentPath(),
                PlayMode = PlayMode.CurrentDirectory
            };

            //Act
            var playerService = _fixture.Create<PlayerService>();
            playerService.SetDirectoryMode(currentFile.Path.GetUnixParentPath());
            await playerService.LaunchFromDirectory(currentFile.Path);
            await playerService.PlayPrevious();
            var resultingSettings = playerService.GetState();

            resultingSettings.Should().BeEquivalentTo(expectedSettings);
            await _launchService.Received(2).LaunchFile(Any<TeensyStorageType>(), Any<ILaunchableItem>());
        }


        [Fact]
        public async Task Given_DirectoryMode_WithFilterChange_When_Previous_Then_PreviousFilePlayed()
        {
            //Arrange
            SetupSettingsWithFilter(TeensyFilterType.Music);
            var timer = SetupTimer();
            var currentFile = CreateFile<SongItem>("/files/2.sid");
            var expectedFile = CreateFile<SongItem>("/files/4.sid");

            SeedStorageDirectory(
            [
                CreateFile<GameItem>("/files/1.crt"),
                expectedFile,
                CreateFile<GameItem>("/files/3.crt"),
                currentFile,
            ]);
            var expectedSettings = new PlayerState
            {
                FilterType = TeensyFilterType.Music,
                CurrentItem = expectedFile,
                PlayState = PlayState.Playing,
                ScopePath = expectedFile.Path.GetUnixParentPath(),
                PlayMode = PlayMode.CurrentDirectory
            };

            //Act
            var playerService = _fixture.Create<PlayerService>();
            playerService.SetDirectoryMode(currentFile.Path.GetUnixParentPath());
            playerService.SetFilter(TeensyFilterType.Music);
            await playerService.LaunchFromDirectory(currentFile.Path);
            await playerService.PlayPrevious();
            var resultingSettings = playerService.GetState();

            resultingSettings.Should().BeEquivalentTo(expectedSettings);
            await _launchService.Received(2).LaunchFile(Any<TeensyStorageType>(), Any<ILaunchableItem>());
        }

        [Fact]
        public async Task Given_SongPlaying_When_Toggled_SongIsPaused()
        {
            //Arrange
            var playerService = _fixture.Create<PlayerService>();
            var expectedFile = CreateFile<SongItem>("/music/sid1.sid");
            var playTimer = SetupTimer();
            SetupStorageService(expectedFile);

            await playerService.LaunchFromDirectory(expectedFile.Path);

            //Act
            await playerService.TogglePlay();

            //Assert
            var settings = playerService.GetState();
            settings.PlayState.Should().Be(PlayState.Paused);
            settings.CurrentItem.Should().BeEquivalentTo(expectedFile);
            _progressTimer.Received(1).PauseTimer();
            await _deviceService.Received(1).ToggleMusicAsync();
            await _deviceService.DidNotReceive().ResetAsync();
        }

        [Fact]
        public async Task Given_SongPaused_When_Toggled_SongResumes()
        {
            //Arrange
            var playerService = _fixture.Create<PlayerService>();
            var expectedFile = CreateFile<SongItem>("/music/sid1.sid");
            var playTimer = SetupTimer();
            SetupStorageService(expectedFile);

            await playerService.LaunchFromDirectory(expectedFile.Path);
            await playerService.TogglePlay();

            //Act
            await playerService.TogglePlay();

            //Assert
            var settings = playerService.GetState();
            settings.CurrentItem.Should().BeEquivalentTo(expectedFile);
            settings.PlayState.Should().Be(PlayState.Playing);
            _progressTimer.Received(1).ResumeTimer();
            await _deviceService.Received(2).ToggleMusicAsync();
            await _launchService.Received(1).LaunchFile(Any<TeensyStorageType>(), Any<ILaunchableItem>());
        }

        [Fact]
        public async Task Given_NonSongPlaying_When_Toggled_ItemIsPaused()
        {
            //Arrange
            var playerService = _fixture.Create<PlayerService>();
            var expectedFile = CreateFile<GameItem>("/games/game.crt");
            var playTimer = SetupTimer();
            SetupStorageService(expectedFile);

            await playerService.LaunchFromDirectory(expectedFile.Path);

            //Act
            await playerService.TogglePlay();

            //Assert
            var settings = playerService.GetState();
            settings.PlayState.Should().Be(PlayState.Paused);
            settings.CurrentItem.Should().BeEquivalentTo(expectedFile);
            _progressTimer.Received(1).PauseTimer();
            await _deviceService.Received(1).ResetAsync();
            await _deviceService.DidNotReceive().ToggleMusicAsync();
        }

        [Fact]
        public async Task Given_NonSongPaused_When_Toggled_ItemResumes()
        {
            //Arrange
            var playerService = _fixture.Create<PlayerService>();
            var expectedFile = CreateFile<GameItem>("/games/game.crt");
            var playTimer = SetupTimer();
            SetupStorageService(expectedFile);

            await playerService.LaunchFromDirectory(expectedFile.Path);
            await playerService.TogglePlay();

            //Act
            await playerService.TogglePlay();

            //Assert
            var settings = playerService.GetState();
            settings.CurrentItem.Should().BeEquivalentTo(expectedFile);
            settings.PlayState.Should().Be(PlayState.Playing);
            _progressTimer.Received(1).ResumeTimer();
            await _launchService.Received(2).LaunchFile(Any<TeensyStorageType>(), expectedFile);
            await _deviceService.DidNotReceive().ToggleMusicAsync();
        }

        [Fact]
        public async Task Given_NonSongPaused_When_ApiRefusesTheRelaunch_Then_MessageIsPublished_And_ItemStaysStopped()
        {
            //Arrange
            var playerService = _fixture.Create<PlayerService>();
            var expectedFile = CreateFile<GameItem>("/games/game.crt");
            _ = SetupTimer();
            SetupStorageService(expectedFile);

            await playerService.LaunchFromDirectory(expectedFile.Path);
            await playerService.TogglePlay();
            _launchService
                .LaunchFile(Any<TeensyStorageType>(), Any<ILaunchableItem>())
                .Returns(Outcome(LaunchStatus.Failed, null, "There was an error launching /games/game.crt."));

            //Act
            await playerService.TogglePlay();

            //Assert
            _alert.Received(1).PublishError("There was an error launching /games/game.crt.");
            _alert.DidNotReceive().Publish(Arg.Is<string>(m => m.Contains("has been resumed")));
            playerService.GetState().PlayState.Should().Be(PlayState.Stopped);
        }

        [Fact]
        public async Task Given_ApiFails_When_Toggled_Then_MessageIsPublished_NotThrown_And_StreamStops()
        {
            //Arrange
            var playerService = _fixture.Create<PlayerService>();
            var expectedFile = CreateFile<SongItem>("/music/sid1.sid");
            _ = SetupTimer();
            SetupStorageService(expectedFile);
            await playerService.LaunchFromDirectory(expectedFile.Path);

            var message = "tr-web stopped answering at localhost:5000 — start it and try again.";
            _deviceService.ToggleMusicAsync().Returns(Task.FromException(new TeensyApiException(message)));

            //Act
            await playerService.TogglePlay();

            //Assert
            _alert.Received(1).PublishError(message);
            playerService.GetState().PlayState.Should().Be(PlayState.Stopped);
        }

        [Fact]
        public async Task Given_DirectoryMode_WithFilterChange_And_OutOfRange_When_Previous_Then_PreviousFilePlayed()
        {
            //Arrange
            SetupSettingsWithFilter(TeensyFilterType.Music);
            var timer = SetupTimer();
            var currentFile = CreateFile<SongItem>("/files/2.sid");
            var expectedFile = CreateFile<SongItem>("/files/6.sid");

            SeedStorageDirectory(
            [
                CreateFile<GameItem>("/files/1.crt"),
                expectedFile,
                CreateFile<GameItem>("/files/3.crt"),
                CreateFile<GameItem>("/files/4.crt"),
                CreateFile<GameItem>("/files/5.crt"),
                currentFile,
                CreateFile<SongItem>("/files/7.sid"),
                CreateFile<SongItem>("/files/8.sid"),
                CreateFile<GameItem>("/files/9.crt"),
            ]);
            var expectedSettings = new PlayerState
            {
                FilterType = TeensyFilterType.Music,
                CurrentItem = expectedFile,
                PlayState = PlayState.Playing,
                ScopePath = expectedFile.Path.GetUnixParentPath(),
                PlayMode = PlayMode.CurrentDirectory
            };

            //Act
            var playerService = _fixture.Create<PlayerService>();
            playerService.SetDirectoryMode(currentFile.Path.GetUnixParentPath());
            playerService.SetFilter(TeensyFilterType.Music);
            await playerService.LaunchFromDirectory(currentFile.Path);
            await playerService.PlayPrevious();
            var resultingSettings = playerService.GetState();

            resultingSettings.Should().BeEquivalentTo(expectedSettings);
            await _launchService.Received(2).LaunchFile(Any<TeensyStorageType>(), Any<ILaunchableItem>());
        }

        [Fact]
        public async Task Given_DirectoryMode_WithFilterChange_When_OutOfRange_Then_PreviousFilePlayed_WithWrapAround()
        {
            //Arrange
            SetupSettingsWithFilter(TeensyFilterType.Music);
            var timer = SetupTimer();
            var currentFile = CreateFile<GameItem>("/files/6.crt");
            var expectedFile = CreateFile<SongItem>("/files/1.sid");

            SeedStorageDirectory(
            [
                currentFile,
                CreateFile<GameItem>("/files/1.crt"),
                CreateFile<GameItem>("/files/3.crt"),
                CreateFile<GameItem>("/files/4.crt"),
                CreateFile<GameItem>("/files/5.crt"),
                expectedFile,
                CreateFile<GameItem>("/files/7.crt"),
                CreateFile<GameItem>("/files/8.crt"),
                CreateFile<GameItem>("/files/9.crt"),
            ]);
            var expectedSettings = new PlayerState
            {
                FilterType = TeensyFilterType.Music,
                CurrentItem = expectedFile,
                PlayState = PlayState.Playing,
                ScopePath = expectedFile.Path.GetUnixParentPath(),
                PlayMode = PlayMode.CurrentDirectory
            };

            //Act
            var playerService = _fixture.Create<PlayerService>();
            playerService.SetDirectoryMode(currentFile.Path.GetUnixParentPath());
            playerService.SetFilter(TeensyFilterType.Music);
            await playerService.LaunchFromDirectory(currentFile.Path);
            await playerService.PlayPrevious();
            var resultingSettings = playerService.GetState();

            resultingSettings.Should().BeEquivalentTo(expectedSettings);
            await _launchService.Received(2).LaunchFile(Any<TeensyStorageType>(), Any<ILaunchableItem>());
        }

        [Fact]
        public async Task Given_DirectoryMode_When_PlayRandom_Then_RandomLaunched_And_SettingsCorrect()
        {
            //Arrange
            var expectedFile = CreateFile<SongItem>("/music/1.sid");

            var expectedSettings = new PlayerState
            {
                PlayMode = PlayMode.Random,
                FilterType = TeensyFilterType.Music,
                PlayState = PlayState.Playing,
                ScopePath = "/music/",
                CurrentItem = expectedFile
            };
            var playerService = _fixture.Create<PlayerService>();
            SetupRandom(expectedFile);

            //Act
            playerService.SetDirectoryScope("/music/");
            playerService.SetFilter(TeensyFilterType.Music);
            await playerService.PlayRandom();
            var settings = playerService.GetState();

            //Assert
            settings.Should().BeEquivalentTo(expectedSettings);
            await _launchService.Received(1).LaunchRandom(TeensyStorageType.SD, StorageScope.DirDeep, "/music/", TeensyFilterType.Music);
            await _launchService.DidNotReceive().LaunchFile(Any<TeensyStorageType>(), Any<ILaunchableItem>());
        }

        [Fact]
        public async Task Given_RandomMode_And_PlayingCurrent_When_Previous_Then_PreviousInHistoryPlayed()
        {
            //Arrange
            var playerService = _fixture.Create<PlayerService>();

            SetupRandom(CreateFile<SongItem>("/music/1.sid"));
            playerService.SetDirectoryScope("/music/");
            playerService.SetFilter(TeensyFilterType.Music);
            await playerService.PlayRandom();

            SetupRandom(CreateFile<SongItem>("/music/2.sid"));
            await playerService.PlayRandom();

            //Act
            await playerService.PlayPrevious();
            var settings = playerService.GetState();

            //Assert
            settings.CurrentItem!.Path.Should().Be("/music/1.sid");
            await _launchService.Received(2).LaunchRandom(Any<TeensyStorageType>(), Any<StorageScope>(), "/music/", Any<TeensyFilterType>());
        }

        [Fact]
        public async Task Given_PlayingHistory_When_Next_Then_NextInHistoryPlayed()
        {
            //Arrange
            var playerService = _fixture.Create<PlayerService>();

            SetupRandom(CreateFile<SongItem>("/music/1.sid"));
            playerService.SetDirectoryScope("/music/");
            playerService.SetFilter(TeensyFilterType.Music);
            await playerService.PlayRandom();

            SetupRandom(CreateFile<SongItem>("/music/2.sid"));
            await playerService.PlayRandom();

            //Act
            await playerService.PlayPrevious();
            await playerService.PlayNext();
            var settings = playerService.GetState();

            //Assert
            settings.CurrentItem!.Path.Should().Be("/music/2.sid");
            await _launchService.Received(2).LaunchRandom(Any<TeensyStorageType>(), Any<StorageScope>(), "/music/", Any<TeensyFilterType>());
        }

        [Fact]
        public async Task Given_SearchMode_When_FileComples_Then_And_NextSearchSongPlayed()
        {
            //Arrange
            SetupSearchStorageService(
            [
                CreateFile<SongItem>("/music/1.sid"),
                CreateFile<SongItem>("/music/2.sid"),
                CreateFile<SongItem>("/music/3.sid"),
            ]);
            var timer = SetupTimer();

            var playerService = _fixture.Create<PlayerService>();
            await playerService.LaunchFromDirectory("/music/1.sid");
            playerService.SetSearchMode("search query");

            //Act
            timer.OnNext(Unit.Default);
            var settings = playerService.GetState();

            //Arrange
            settings.CurrentItem!.Path.Should().Be("/music/2.sid");
            await _storageService.Received(1).Search("search query", TeensyFilterType.All);
        }

        [Fact]
        public async Task Given_SearchMode_And_LastItemPlayed_When_FileComples_Then_And_FirstSearchSongPlayed()
        {
            //Arrange
            SetupSearchStorageService(
            [
                CreateFile<SongItem>("/music/1.sid"),
                CreateFile<SongItem>("/music/2.sid"),
                CreateFile<SongItem>("/music/3.sid"),
            ]);
            var timer = SetupTimer();

            var playerService = _fixture.Create<PlayerService>();
            await playerService.LaunchFromDirectory("/music/3.sid");
            playerService.SetSearchMode("search query");

            //Act
            timer.OnNext(Unit.Default);
            var settings = playerService.GetState();

            //Arrange
            settings.CurrentItem!.Path.Should().Be("/music/1.sid");
        }

        [Fact]
        public async Task Given_SearchMode_When_Previous_Then_PreviousSearchSongPlayed()
        {
            //Arrange
            SetupSearchStorageService(
            [
                CreateFile<SongItem>("/music/1.sid"),
                CreateFile<SongItem>("/music/2.sid"),
                CreateFile<SongItem>("/music/3.sid"),
            ]);

            var playerService = _fixture.Create<PlayerService>();
            await playerService.LaunchFromDirectory("/music/2.sid");
            playerService.SetSearchMode("search query");

            //Act
            await playerService.PlayPrevious();
            var settings = playerService.GetState();

            //Arrange
            settings.CurrentItem!.Path.Should().Be("/music/1.sid");
        }

        [Fact]
        public async Task Given_SearchMode_And_FirstSearchResultPlaying_When_Previous_Then_LastSearchSongPlayed()
        {
            //Arrange
            SetupSearchStorageService(
            [
                CreateFile<SongItem>("/music/1.sid"),
                CreateFile<SongItem>("/music/2.sid"),
                CreateFile<SongItem>("/music/3.sid"),
            ]);

            var playerService = _fixture.Create<PlayerService>();
            await playerService.LaunchFromDirectory("/music/1.sid");
            playerService.SetSearchMode("search query");

            //Act
            await playerService.PlayPrevious();
            var settings = playerService.GetState();

            //Arrange
            settings.CurrentItem!.Path.Should().Be("/music/3.sid");
        }

        [Fact]
        public async Task When_Launched_And_ModeRandom_And_FileBad_Then_FileMarkedIncompatible()
        {
            //Arrange
            var file1 = CreateFile<SongItem>("/music/sid1.sid");
            var file2 = CreateFile<SongItem>("/music/sid2.sid");
            SetupStorageServiceRandom(file1);
            SetupStorageService(file1, file2);
            SetupLaunch(file2);

            var playerService = _fixture.Create<PlayerService>();
            playerService.SetRandomMode("/music/");

            //Act
            await playerService.LaunchFromDirectory(file2.Path);

            //Assert
            file2.IsCompatible.Should().BeFalse();
            await _launchService.Received(1).LaunchFile(Any<TeensyStorageType>(), file2);
            await _launchService.Received(1).LaunchRandom(Any<TeensyStorageType>(), Any<StorageScope>(), Any<string>(), Any<TeensyFilterType>());
        }

        [Fact]
        public async Task When_Launched_And_ModeDirectory_And_FileBad_Then_FileMarkedIncompatible()
        {
            //Arrange
            var file1 = CreateFile<SongItem>("/music/sid1.sid");
            var file2 = CreateFile<SongItem>("/music/sid2.sid");
            SetupStorageServiceRandom(file1);
            SetupStorageService(file1, file2);
            SetupLaunch(file2);

            var playerService = _fixture.Create<PlayerService>();
            playerService.SetDirectoryMode("/music/");

            //Act
            await playerService.LaunchFromDirectory(file2.Path);

            //Assert
            file2.IsCompatible.Should().BeFalse();
            await _launchService.Received(2).LaunchFile(Any<TeensyStorageType>(), Any<ILaunchableItem>());
        }

        [Fact]
        public async Task When_Launched_And_ModeDirectory_And_FileBad_Then_SkipFileAndPlayNext()
        {
            //Arrange
            var file1 = CreateFile<SongItem>("/music/sid1.sid");
            var file2 = CreateFile<SongItem>("/music/sid2.sid");
            SetupStorageServiceRandom(file1, file2);
            SetupStorageService(file1, file2);
            SetupLaunch(file2);

            var playerService = _fixture.Create<PlayerService>();
            playerService.SetDirectoryMode("/music/");

            //Act
            await playerService.LaunchFromDirectory(file2.Path);

            //Assert
            file2.IsCompatible.Should().BeFalse();
            playerService.GetState().CurrentItem.Should().BeSameAs(file1);
            await _storageService.Received(2).GetDirectory(Any<string>());
            await _launchService.Received(2).LaunchFile(Any<TeensyStorageType>(), Any<ILaunchableItem>());
        }

        [Fact]
        public async Task When_Launched_And_ModeRandom_And_FileBad_Then_SkipFileAndPlayNext()
        {
            //Arrange
            var file1 = CreateFile<SongItem>("/music/sid1.sid");
            var file2 = CreateFile<SongItem>("/music/sid2.sid");
            SetupStorageServiceRandom(file1, file2);
            SetupStorageService(file1, file2);
            SetupLaunch(file2);

            var playerService = _fixture.Create<PlayerService>();
            playerService.SetRandomMode("/");

            //Act
            await playerService.LaunchFromDirectory(file2.Path);

            //Assert
            file2.IsCompatible.Should().BeFalse();
            playerService.GetState().CurrentItem.Should().BeSameAs(file1);
            await _launchService.Received(1).LaunchFile(Any<TeensyStorageType>(), file2);
            await _launchService.Received(1).LaunchRandom(Any<TeensyStorageType>(), Any<StorageScope>(), Any<string>(), Any<TeensyFilterType>());
            await _storageService.Received(1).GetDirectory(Any<string>());
        }

        [Fact]
        public async Task When_RandomPickIsIncompatible_Then_ItIsSkipped_And_AnotherIsPicked()
        {
            //Arrange
            var bad = CreateFile<SongItem>("/music/bad.sid");
            var good = CreateFile<SongItem>("/music/good.sid");
            var picks = new Queue<LaunchOutcome>([Outcome(LaunchStatus.Incompatible, bad), Outcome(LaunchStatus.Launched, good)]);
            _launchService
                .LaunchRandom(Any<TeensyStorageType>(), Any<StorageScope>(), Any<string>(), Any<TeensyFilterType>())
                .Returns(_ => picks.Dequeue());

            var playerService = _fixture.Create<PlayerService>();

            //Act
            await playerService.PlayRandom();

            //Assert
            bad.IsCompatible.Should().BeFalse();
            playerService.GetState().CurrentItem.Should().BeSameAs(good);
            await _launchService.Received(2).LaunchRandom(Any<TeensyStorageType>(), Any<StorageScope>(), Any<string>(), Any<TeensyFilterType>());
        }

        [Fact]
        public async Task When_Previous_And_ModeDirectory_And_FileBad_Then_SkipFileAndPlayPrevious()
        {
            //Arrange
            var file1 = CreateFile<SongItem>("/music/sid1.sid");
            var file2 = CreateFile<SongItem>("/music/sid2.sid");
            var file3 = CreateFile<SongItem>("/music/sid3.sid");
            SetupStorageService(file1, file2, file3);
            SetupLaunch(file2);

            var playerService = _fixture.Create<PlayerService>();
            playerService.SetDirectoryMode("/music/");

            //Act
            await playerService.LaunchFromDirectory(file3.Path);
            await playerService.PlayPrevious();
            var finalState = playerService.GetState();

            //Assert
            finalState.CurrentItem.Should().BeEquivalentTo(file1);
            file2.IsCompatible.Should().BeFalse();
            await _launchService.Received(3).LaunchFile(Any<TeensyStorageType>(), Any<ILaunchableItem>());
        }

        [Fact]
        public async Task When_Next_And_ModeDirectory_And_FileBad_Then_SkipFileAndPlayPrevious()
        {
            //Arrange
            var file1 = CreateFile<SongItem>("/music/sid1.sid");
            var file2 = CreateFile<SongItem>("/music/sid2.sid");
            var file3 = CreateFile<SongItem>("/music/sid3.sid");
            SetupStorageService(file1, file2, file3);
            SetupLaunch(file3);

            var playerService = _fixture.Create<PlayerService>();
            playerService.SetDirectoryMode("/music/");

            //Act
            await playerService.LaunchFromDirectory(file2.Path);
            await playerService.PlayNext();
            var finalState = playerService.GetState();

            //Assert
            finalState.CurrentItem.Should().BeEquivalentTo(file1);
            file3.IsCompatible.Should().BeFalse();
            await _launchService.Received(3).LaunchFile(Any<TeensyStorageType>(), Any<ILaunchableItem>());
        }

        [Theory]
        [InlineData("NotFound", "The storage USB is not available.", "The storage USB is not available.")]
        [InlineData("Failed", "There was an error launching /music/sid2.sid. Timed out.", "There was an error launching /music/sid2.sid. Timed out.")]
        [InlineData("Failed", null, "Failed to launch sid2.sid.")]
        public async Task When_ApiRefusesTheLaunch_Then_MessageIsPublished_And_NoFurtherLaunchIsTried(string status, string? apiMessage, string expectedAlert)
        {
            //Arrange
            var file1 = CreateFile<SongItem>("/music/sid1.sid");
            var file2 = CreateFile<SongItem>("/music/sid2.sid");
            SetupStorageService(file1, file2);
            _ = SetupTimer();
            _launchService
                .LaunchFile(Any<TeensyStorageType>(), file2)
                .Returns(Outcome(Enum.Parse<LaunchStatus>(status), null, apiMessage));

            var playerService = _fixture.Create<PlayerService>();
            playerService.SetDirectoryMode("/music/");

            //Act
            await playerService.LaunchFromDirectory(file2.Path);

            //Assert
            _alert.Received(1).PublishError(expectedAlert);
            playerService.GetState().PlayState.Should().Be(PlayState.Stopped);
            await _launchService.Received(1).LaunchFile(Any<TeensyStorageType>(), Any<ILaunchableItem>());
            await _storageService.Received(1).GetDirectory(Any<string>());
            _progressTimer.DidNotReceive().StartNewTimer(Any<TimeSpan>());
        }

        [Fact]
        public async Task When_Next_And_ApiRefusesTheLaunch_Then_PlayerStopsInsteadOfAdvancing()
        {
            //Arrange
            var file1 = CreateFile<SongItem>("/music/sid1.sid");
            var file2 = CreateFile<SongItem>("/music/sid2.sid");
            var file3 = CreateFile<SongItem>("/music/sid3.sid");
            SetupStorageService(file1, file2, file3);
            _ = SetupTimer();
            _launchService
                .LaunchFile(Any<TeensyStorageType>(), file3)
                .Returns(Outcome(LaunchStatus.Failed, null, "There was an error launching /music/sid3.sid."));

            var playerService = _fixture.Create<PlayerService>();
            playerService.SetDirectoryMode("/music/");
            await playerService.LaunchFromDirectory(file2.Path);

            //Act
            await playerService.PlayNext();

            //Assert
            _alert.Received(1).PublishError("There was an error launching /music/sid3.sid.");
            playerService.GetState().PlayState.Should().Be(PlayState.Stopped);
            await _launchService.Received(2).LaunchFile(Any<TeensyStorageType>(), Any<ILaunchableItem>());
        }

        [Fact]
        public async Task When_RandomFindsNoFiles_Then_TheFilterMessageIsPublished()
        {
            //Arrange
            _launchService
                .LaunchRandom(Any<TeensyStorageType>(), Any<StorageScope>(), Any<string>(), Any<TeensyFilterType>())
                .Returns(Outcome(LaunchStatus.NotFound, null, "No files were found."));
            var playerService = _fixture.Create<PlayerService>();
            playerService.SetDirectoryScope("/music/");
            playerService.SetFilter(TeensyFilterType.Music);

            //Act
            await playerService.PlayRandom();

            //Assert
            _alert.Received(1).PublishError("No files for the filter \"Music\" were found on the SD in /music/.");
        }

        [Theory]
        [InlineData("NotFound", "The storage USB is not available.")]
        [InlineData("NotFound", "The device abc was not found.")]
        [InlineData("Failed", "There was an error launching /music/1.sid. Timed out.")]
        public async Task When_RandomIsRefusedForAnotherReason_Then_TheApiMessageIsPublishedAsIs(string status, string apiMessage)
        {
            //Arrange
            _launchService
                .LaunchRandom(Any<TeensyStorageType>(), Any<StorageScope>(), Any<string>(), Any<TeensyFilterType>())
                .Returns(Outcome(Enum.Parse<LaunchStatus>(status), null, apiMessage));
            var playerService = _fixture.Create<PlayerService>();

            //Act
            await playerService.PlayRandom();

            //Assert
            _alert.Received(1).PublishError(apiMessage);
            playerService.GetState().PlayState.Should().Be(PlayState.Stopped);
        }

        [Fact]
        public async Task When_ApiFailsOnTimerDrivenNext_Then_MessageIsPublished_And_StreamStops()
        {
            //Arrange
            var timer = SetupTimer();
            var currentFile = CreateFile<SongItem>("/music/2.sid");
            SeedStorageDirectory([CreateFile<SongItem>("/music/1.sid"), currentFile, CreateFile<SongItem>("/music/3.sid")]);

            var playerService = _fixture.Create<PlayerService>();
            playerService.SetDirectoryMode("/music/");
            await playerService.LaunchFromDirectory(currentFile.Path);

            var message = "tr-web stopped answering at localhost:5000 — start it and try again.";
            _storageService.GetDirectory(Any<string>()).Returns(Task.FromException<DirectoryListing>(new TeensyApiException(message)));

            //Act
            timer.OnNext(Unit.Default);
            timer.OnNext(Unit.Default);

            //Assert
            _alert.Received(1).PublishError(message);
            playerService.GetState().PlayState.Should().Be(PlayState.Stopped);
            await _storageService.Received(2).GetDirectory(Any<string>());
        }

        [Fact]
        public async Task When_ApiFailsOnNextInSearchMode_Then_MessageIsPublished_NotThrown()
        {
            //Arrange
            SetupSearchStorageService([CreateFile<SongItem>("/music/1.sid"), CreateFile<SongItem>("/music/2.sid")]);
            var playerService = _fixture.Create<PlayerService>();
            await playerService.LaunchFromDirectory("/music/1.sid");
            playerService.SetSearchMode("search query");

            var message = "The device abc was not found.";
            _storageService.Search(Any<string>(), Any<TeensyFilterType>())
                .Returns(Task.FromException<IReadOnlyList<ILaunchableItem>>(new TeensyApiException(message, 404)));

            //Act
            await playerService.PlayNext();

            //Assert
            _alert.Received(1).PublishError(message);
        }

        private T CreateFile<T>(string path) where T : ILaunchableItem
            {
                var name = path.GetFileNameFromPath();
                return _fixture.Build<T>()
                    .With(s => s.Name, $"{name}")
                    .With(s => s.Path, $"{path}")
                    .With(s => s.IsCompatible, true)
                    .Create();
            }

        private static LaunchOutcome Outcome(LaunchStatus status, ILaunchableItem? item = null, string? message = null) =>
            new(status, item, message);

        private Subject<Unit> SetupTimer()
        {
            var timer = new Subject<Unit>();
            _progressTimer.TimerComplete.Returns(timer);
            return timer;
        }

        /// <summary>
        /// Every launch succeeds except those of the given files, which tr-web reports as incompatible.
        /// </summary>
        private void SetupLaunch(params ILaunchableItem[] incompatible)
        {
            _launchService
                .LaunchFile(Any<TeensyStorageType>(), Any<ILaunchableItem>())
                .Returns(call =>
                {
                    var item = call.Arg<ILaunchableItem>();
                    return Outcome(incompatible.Contains(item) ? LaunchStatus.Incompatible : LaunchStatus.Launched, item);
                });
        }

        private void SetupRandom(ILaunchableItem item)
        {
            _launchService
                .LaunchRandom(Any<TeensyStorageType>(), Any<StorageScope>(), Any<string>(), Any<TeensyFilterType>())
                .Returns(Outcome(LaunchStatus.Launched, item));
        }

        private void SetupStorageServiceRandom(params ILaunchableItem[] items)
        {
            var random = new Random();

            _launchService
                .LaunchRandom(Any<TeensyStorageType>(), Any<StorageScope>(), Any<string>(), Any<TeensyFilterType>())
                .Returns(_ =>
                {
                    var pick = items.Length > 1 ? items[random.Next(items.Length - 1)] : items[0];
                    return Outcome(LaunchStatus.Launched, pick);
                });
        }

        private void SetupStorageService(params ILaunchableItem[] items)
        {
            SetupStorageServiceRandom(items);

            _storageService
                .GetDirectory(Any<string>())
                .Returns(new DirectoryListing { Files = items.Cast<IFileItem>().ToList() });
        }

        private void SetupSearchStorageService(List<ILaunchableItem> items)
        {
            _storageService
                .GetDirectory(Any<string>())
                .Returns(new DirectoryListing { Files = items.Cast<IFileItem>().ToList() });

            _storageService.Search(Any<string>(), Any<TeensyFilterType>())
                .Returns(items);
        }

        private TeensySettings SetupSettingsWithFilter(TeensyFilterType filter)
        {
            var settings = new TeensySettings
            {
                StartupFilter = filter
            };
            settings.InitializeDefaults();

            _settingsService.GetSettings().Returns(s => settings);
            return settings;
        }

        private TeensySettings SetupSettingsWithStorage(TeensyStorageType storage)
        {
            var settings = new TeensySettings
            {
                StorageType = storage
            };
            settings.InitializeDefaults();

            _settingsService.GetSettings().Returns(s => settings);
            return settings;
        }

        private T Any<T>() => Arg.Any<T>();
    }

}
