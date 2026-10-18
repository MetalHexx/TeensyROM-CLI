using NSubstitute;
using TeensyRom.Cli.Api;
using TeensyRom.Cli.Api.Generated;
using TeensyRom.Cli.Services;
using TeensyRom.Cli.Logging;
using TeensyRom.Cli.Settings;
using TeensyRom.Cli.Storage.Entities;
using static TeensyRom.Cli.Tests.ApiTestSupport;
using ApiStorageType = TeensyRom.Cli.Api.Generated.TeensyStorageType;
using TeensyFilterType = TeensyRom.Cli.Settings.TeensyFilterType;
using TeensyStorageType = TeensyRom.Cli.Storage.Entities.TeensyStorageType;

namespace TeensyRom.Cli.Tests;

public class StorageServiceTests
{
    private const string DeviceId = "abc123";

    private readonly ITeensyRomApiClient _api = Substitute.For<ITeensyRomApiClient>();
    private readonly IDeviceService _devices = Substitute.For<IDeviceService>();
    private readonly ISettingsService _settings = Substitute.For<ISettingsService>();
    private readonly IAlertService _alert = Substitute.For<IAlertService>();
    private readonly StorageService _sut;

    public StorageServiceTests()
    {
        _devices.RequireDeviceId().Returns(DeviceId);
        _settings.GetSettings().Returns(_ => new TeensySettings { StorageType = TeensyStorageType.SD });
        _sut = new StorageService(_api, _devices, _settings, _alert);
    }

    private static FileItemDto File(string path, FileItemType type = FileItemType.Song, ApiStorageType storage = ApiStorageType.SD) => new()
    {
        Name = path[(path.LastIndexOf('/') + 1)..],
        Path = path,
        Type = type,
        StorageType = storage,
        IsCompatible = true
    };

    private static SongItem Song(string path = "/music/Commando.sid") => new()
    {
        Name = path[(path.LastIndexOf('/') + 1)..],
        Path = path
    };

    private static GetSettingsResponse ServerSettings(params string[] bannedFiles) => new()
    {
        KnownDevices = [new DeviceSettingsDto()],
        PlayerSettings = new PlayerSettingsDto(),
        FileTransferSettings = new FileTransferSettingsDto(),
        SearchSettings = new SearchSettingsDto { BannedFiles = [.. bannedFiles], BannedDirectories = ["Docs"], SearchStopWords = ["the"] },
        AppSettings = new AppSettingsDto()
    };

    [Fact]
    public void StorageType_StartsAsTheSavedPreference()
    {
        _settings.GetSettings().Returns(_ => new TeensySettings { StorageType = TeensyStorageType.USB });

        new StorageService(_api, _devices, _settings, _alert).StorageType.Should().Be(TeensyStorageType.USB);
    }

    [Fact]
    public void SwitchStorage_ChangesTheStorage_AndSavesItAsThePreference()
    {
        _sut.SwitchStorage(TeensyStorageType.USB);

        _sut.StorageType.Should().Be(TeensyStorageType.USB);
        _settings.Received(1).SaveSettings(Arg.Is<TeensySettings>(s => s.StorageType == TeensyStorageType.USB));
    }

    [Fact]
    public async Task GetDirectory_ListsTheChosenStorage_AsTheCliModels()
    {
        _sut.SwitchStorage(TeensyStorageType.USB);
        _api.GetDirectoryAsync(DeviceId, ApiStorageType.USB, "/music", Arg.Any<CancellationToken>()).Returns(new GetDirectoryResponse
        {
            StorageItem = new StorageCacheDto
            {
                Path = "/music",
                Directories = [new DirectoryItemDto { Name = "MUSICIANS", Path = "/music/MUSICIANS" }],
                Files = [File("/music/a.sid"), File("/music/b.crt", FileItemType.Game)]
            }
        });

        var listing = await _sut.GetDirectory("/music");

        listing.Path.Should().Be("/music");
        listing.Directories.Should().ContainSingle().Which.Should().BeEquivalentTo(new { Name = "MUSICIANS", Path = "/music/MUSICIANS" });
        listing.Files.Should().HaveCount(2);
        listing.Files[0].Should().BeOfType<SongItem>();
        listing.Files[1].Should().BeOfType<GameItem>();
    }

    [Fact]
    public async Task GetDirectory_WhenTheStorageIsNotAvailable_SurfacesTheApisMessage()
    {
        _api.GetDirectoryAsync(DeviceId, Arg.Any<ApiStorageType>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<GetDirectoryResponse>(ApiFailure(404, "\"The storage USB is not available.\"")));

        var act = () => _sut.GetDirectory("/");

        var failure = (await act.Should().ThrowAsync<TeensyApiException>()).Which;
        failure.Message.Should().Be("The storage USB is not available.");
        failure.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Search_KeepsOnlyTheChosenStorage_AndOnlyLaunchableFiles()
    {
        _sut.SwitchStorage(TeensyStorageType.USB);
        _api.SearchAsync(DeviceId, "iron maiden", 0, 1000, NullableOfTeensyFilterType.Music, Arg.Any<CancellationToken>()).Returns(new SearchResponse
        {
            Files =
            [
                File("/music/sd.sid", storage: ApiStorageType.SD),
                File("/music/first.sid", storage: ApiStorageType.USB),
                File("/music/notes.txt", FileItemType.Unknown, ApiStorageType.USB),
                File("/games/second.crt", FileItemType.Game, ApiStorageType.USB)
            ]
        });

        var results = await _sut.Search("iron maiden", TeensyFilterType.Music);

        results.Select(r => r.Path).Should().Equal("/music/first.sid", "/games/second.crt");
        await _api.Received(1).SearchAsync(DeviceId, "iron maiden", 0, 1000, NullableOfTeensyFilterType.Music, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Search_WhenTheApiHasMoreResults_KeepsPagingAndKeepsTheChosenStoragesFilesFromEveryPage()
    {
        _sut.SwitchStorage(TeensyStorageType.USB);
        _api.SearchAsync(DeviceId, "sid", 0, 1000, Arg.Any<NullableOfTeensyFilterType?>(), Arg.Any<CancellationToken>()).Returns(new SearchResponse
        {
            Files = [File("/music/sd-a.sid", storage: ApiStorageType.SD), File("/music/usb-a.sid", storage: ApiStorageType.USB)],
            HasMore = true
        });
        _api.SearchAsync(DeviceId, "sid", 1000, 1000, Arg.Any<NullableOfTeensyFilterType?>(), Arg.Any<CancellationToken>()).Returns(new SearchResponse
        {
            Files = [File("/music/usb-b.sid", storage: ApiStorageType.USB)],
            HasMore = false
        });

        var results = await _sut.Search("sid", TeensyFilterType.All);

        results.Select(r => r.Path).Should().Equal("/music/usb-a.sid", "/music/usb-b.sid");
        await _api.Received(2).SearchAsync(DeviceId, "sid", Arg.Any<int>(), 1000, Arg.Any<NullableOfTeensyFilterType?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Index_SendsTheChosenDeviceStorageAndPath()
    {
        _sut.SwitchStorage(TeensyStorageType.USB);

        await _sut.Index("/music/MUSICIANS");

        await _api.Received(1).IndexAsync(DeviceId, ApiStorageType.USB, "/music/MUSICIANS", Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("/")]
    public async Task Index_OfTheRoot_IndexesTheWholeStorage(string path)
    {
        await _sut.Index(path);

        await _api.Received(1).IndexAsync(DeviceId, ApiStorageType.SD, null, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveFavorite_TagsTheFile_AndFlipsItsFlag()
    {
        var song = Song();
        _api.SaveFavoriteAsync(DeviceId, ApiStorageType.SD, song.Path, Arg.Any<CancellationToken>())
            .Returns(new SaveFavoriteResponse { FavoritePath = "/favorites/music" });

        await _sut.SaveFavorite(song);

        song.IsFavorite.Should().BeTrue();
        _alert.Received(1).Publish("A copy was placed in /favorites/music.");
    }

    [Fact]
    public async Task SaveFavorite_WhenTheApiFails_LeavesTheFlagAlone()
    {
        var song = Song();
        _api.SaveFavoriteAsync(DeviceId, Arg.Any<ApiStorageType>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<SaveFavoriteResponse>(ApiFailure(502, """{"title":"Failed to save favorite. Please try again."}""")));

        var act = () => _sut.SaveFavorite(song);

        await act.Should().ThrowAsync<TeensyApiException>().WithMessage("Failed to save favorite. Please try again.");
        song.IsFavorite.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveFavorite_UntagsTheFile_AndClearsItsFlag()
    {
        var song = Song("/favorites/music/Commando.sid");
        song.IsFavorite = true;

        await _sut.RemoveFavorite(song);

        song.IsFavorite.Should().BeFalse();
        await _api.Received(1).RemoveFavoriteAsync(DeviceId, ApiStorageType.SD, "/favorites/music/Commando.sid", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BanFile_SavesEverySettingsSectionBackAsRead_WithTheNameAdded()
    {
        var current = ServerSettings("Old.sid");
        _api.GetSettingsAsync(Arg.Any<CancellationToken>()).Returns(current);
        SaveSettingsRequest? saved = null;
        _api.SaveSettingsAsync(Arg.Do<SaveSettingsRequest>(request => saved = request), Arg.Any<CancellationToken>())
            .Returns(new SaveSettingsResponse());

        await _sut.BanFile(Song("/music/Commando.sid"));

        saved.Should().NotBeNull();
        saved!.KnownDevices.Should().BeSameAs(current.KnownDevices);
        saved.PlayerSettings.Should().BeSameAs(current.PlayerSettings);
        saved.FileTransferSettings.Should().BeSameAs(current.FileTransferSettings);
        saved.AppSettings.Should().BeSameAs(current.AppSettings);
        saved.SearchSettings.Should().BeSameAs(current.SearchSettings);
        saved.SearchSettings.BannedFiles.Should().Equal("Old.sid", "Commando.sid");
        saved.SearchSettings.BannedDirectories.Should().Equal("Docs");
        saved.SearchSettings.SearchStopWords.Should().Equal("the");
        _alert.Received(1).Publish("Commando.sid has been banned and will no longer show up in the application.");
    }

    [Fact]
    public async Task BanFile_OfAnAlreadyBannedName_AddsItOnlyOnce()
    {
        var current = ServerSettings();
        _api.GetSettingsAsync(Arg.Any<CancellationToken>()).Returns(current);
        var song = Song();

        await _sut.BanFile(song);
        await _sut.BanFile(song);

        current.SearchSettings.BannedFiles.Should().Equal("Commando.sid");
        await _api.Received(1).SaveSettingsAsync(Arg.Any<SaveSettingsRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BanFile_NeedsNoSelectedDevice()
    {
        _devices.RequireDeviceId().Returns(_ => throw new TeensyApiException("No TeensyROM is selected."));
        _api.GetSettingsAsync(Arg.Any<CancellationToken>()).Returns(ServerSettings());

        await _sut.BanFile(Song());

        await _api.Received(1).SaveSettingsAsync(Arg.Any<SaveSettingsRequest>(), Arg.Any<CancellationToken>());
        _devices.DidNotReceive().RequireDeviceId();
    }

    [Fact]
    public async Task GetDirectory_WithNoSelectedDevice_Throws()
    {
        _devices.RequireDeviceId().Returns(_ => throw new TeensyApiException("No TeensyROM is selected."));

        var act = () => _sut.GetDirectory("/");

        await act.Should().ThrowAsync<TeensyApiException>().WithMessage("No TeensyROM is selected.");
    }
}
