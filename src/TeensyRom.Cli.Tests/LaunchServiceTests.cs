using System.Net;
using NSubstitute;
using TeensyRom.Cli.Api;
using TeensyRom.Cli.Api.Generated;
using TeensyRom.Cli.Services;
using TeensyRom.Cli.Settings;
using TeensyRom.Cli.Storage.Entities;
using static TeensyRom.Cli.Tests.ApiTestSupport;
using ApiStorageType = TeensyRom.Cli.Api.Generated.TeensyStorageType;
using TeensyFilterType = TeensyRom.Cli.Settings.TeensyFilterType;
using TeensyStorageType = TeensyRom.Cli.Storage.Entities.TeensyStorageType;

namespace TeensyRom.Cli.Tests;

public class LaunchServiceTests
{
    private const string DeviceId = "abc123";

    private readonly IDeviceService _devices = Substitute.For<IDeviceService>();

    public LaunchServiceTests() => _devices.RequireDeviceId().Returns(DeviceId);

    private static string FileJson(string type = "Song", string playLength = "00:03:21.5000000") => $$"""
        {
          "name": "Artillery.sid", "path": "/music/MUSICIANS/T/Tjelta_Geir/Artillery.sid", "size": 4123,
          "storageType": "USB", "isFavorite": true, "isCompatible": true,
          "title": "Artillery", "creator": "Geir Tjelta", "releaseInfo": "1990 Cascade", "description": "A comment.",
          "shareUrl": "https://deepsid.example/?file=Artillery.sid", "metadataSource": "HVSC",
          "meta1": "PAL", "meta2": "6581", "metadataSourcePath": "/hvsc/Artillery.sid", "parentPath": "/music/MUSICIANS/T/Tjelta_Geir",
          "playLength": "{{playLength}}", "subtuneLengths": ["00:03:21.5000000", "00:01:05"], "startSubtuneNum": 2,
          "type": "{{type}}", "links": [], "tags": [], "youTubeVideos": [], "competitions": [], "ratingCount": 0, "images": []
        }
        """;

    private static string LaunchBody(bool compatible, string file) => $$"""
        { "message": "answer", "launchedFile": {{file}}, "isCompatible": {{(compatible ? "true" : "false")}} }
        """;

    private LaunchService Sut(HttpStatusCode status, string body) => new(ClientAnswering(status, body), _devices);

    private static SongItem Requested() => new()
    {
        Name = "Artillery.sid",
        Path = "/music/MUSICIANS/T/Tjelta_Geir/Artillery.sid"
    };

    [Fact]
    public async Task LaunchFile_WhenCompatible_IsLaunched_WithTheItemItWasGiven()
    {
        var requested = Requested();
        var sut = Sut(HttpStatusCode.OK, LaunchBody(true, FileJson()));

        var outcome = await sut.LaunchFile(TeensyStorageType.USB, requested);

        outcome.Status.Should().Be(LaunchStatus.Launched);
        outcome.Item.Should().BeSameAs(requested);
    }

    [Fact]
    public async Task LaunchFile_SendsTheSelectedDeviceStorageAndPath()
    {
        Uri? requestUri = null;
        var client = ClientWith(request =>
        {
            requestUri = request.RequestUri;
            return Task.FromResult(Reply(HttpStatusCode.OK, LaunchBody(true, FileJson())));
        });

        await new LaunchService(client, _devices).LaunchFile(TeensyStorageType.USB, Requested());

        var sent = Uri.UnescapeDataString(requestUri!.PathAndQuery);
        sent.Should().Contain(DeviceId).And.Contain("USB").And.Contain("/music/MUSICIANS/T/Tjelta_Geir/Artillery.sid");
    }

    [Fact]
    public async Task LaunchFile_WhenTheUnitCantRunIt_IsIncompatible()
    {
        var requested = Requested();
        var sut = Sut(HttpStatusCode.OK, LaunchBody(false, FileJson()));

        var outcome = await sut.LaunchFile(TeensyStorageType.SD, requested);

        outcome.Status.Should().Be(LaunchStatus.Incompatible);
        outcome.Item.Should().BeSameAs(requested);
    }

    [Fact]
    public async Task LaunchFile_WhenTrWebAnswersNotFound_IsNotFound_WithItsMessage()
    {
        var sut = Sut(HttpStatusCode.NotFound, "\"The file /music/gone.sid was not found.\"");

        var outcome = await sut.LaunchFile(TeensyStorageType.SD, Requested());

        outcome.Should().BeEquivalentTo(new { Status = LaunchStatus.NotFound, Item = (ILaunchableItem?)null, Message = "The file /music/gone.sid was not found." });
    }

    [Fact]
    public async Task LaunchFile_WhenTrWebAnswersBadGateway_Fails_WithItsMessage()
    {
        var sut = Sut(HttpStatusCode.BadGateway, """{"title":"The TeensyROM did not answer.","status":502}""");

        var outcome = await sut.LaunchFile(TeensyStorageType.SD, Requested());

        outcome.Should().BeEquivalentTo(new { Status = LaunchStatus.Failed, Item = (ILaunchableItem?)null, Message = "The TeensyROM did not answer." });
    }

    [Fact]
    public async Task LaunchFile_WhenTrWebDoesNotAnswer_Throws()
    {
        var client = ClientWith(_ => throw new HttpRequestException("refused"));

        var act = () => new LaunchService(client, _devices).LaunchFile(TeensyStorageType.SD, Requested());

        var failure = (await act.Should().ThrowAsync<TeensyApiException>()).Which;
        failure.Message.Should().Contain("tr-web stopped answering");
        failure.StatusCode.Should().BeNull();
    }

    [Fact]
    public async Task LaunchFile_WithNoSelectedDevice_Throws()
    {
        _devices.RequireDeviceId().Returns(_ => throw new TeensyApiException("No TeensyROM is selected."));

        var act = () => Sut(HttpStatusCode.OK, LaunchBody(true, FileJson())).LaunchFile(TeensyStorageType.SD, Requested());

        await act.Should().ThrowAsync<TeensyApiException>().WithMessage("No TeensyROM is selected.");
    }

    [Fact]
    public async Task LaunchRandom_AsksTheApiToPickWithinTheScope_AndTakesTheFileItLaunched()
    {
        var api = Substitute.For<ITeensyRomApiClient>();
        api.LaunchRandomAsync(DeviceId, ApiStorageType.USB, NullableOfTeensyFilterType.Music, Scope.DirShallow, "/demos", Arg.Any<CancellationToken>())
            .Returns(new LaunchRandomResponse
            {
                IsCompatible = true,
                LaunchedFile = new FileItemDto { Name = "x.sid", Path = "/demos/x.sid", Type = FileItemType.Song, IsCompatible = true }
            });

        var outcome = await new LaunchService(api, _devices)
            .LaunchRandom(TeensyStorageType.USB, StorageScope.DirShallow, "/demos", TeensyFilterType.Music);

        outcome.Status.Should().Be(LaunchStatus.Launched);
        outcome.Item!.Path.Should().Be("/demos/x.sid");
    }

    [Fact]
    public async Task LaunchRandom_MapsTheLaunchedSong()
    {
        var sut = Sut(HttpStatusCode.OK, LaunchBody(true, FileJson()));

        var outcome = await sut.LaunchRandom(TeensyStorageType.USB, StorageScope.Storage, "/", TeensyFilterType.All);

        var song = outcome.Item.Should().BeOfType<SongItem>().Which;
        song.Should().BeEquivalentTo(new
        {
            Name = "Artillery.sid",
            Path = "/music/MUSICIANS/T/Tjelta_Geir/Artillery.sid",
            Size = 4123L,
            IsFavorite = true,
            IsCompatible = true,
            Title = "Artillery",
            Creator = "Geir Tjelta",
            ReleaseInfo = "1990 Cascade",
            Description = "A comment.",
            ShareUrl = "https://deepsid.example/?file=Artillery.sid",
            MetadataSource = "HVSC",
            Meta1 = "PAL",
            Meta2 = "6581",
            MetadataSourcePath = "/hvsc/Artillery.sid",
            StartSubtuneNum = 2,
            PlayLength = new TimeSpan(0, 0, 3, 21, 500)
        });
        song.SubtuneLengths.Should().Equal(new TimeSpan(0, 0, 3, 21, 500), TimeSpan.FromMinutes(1) + TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData("Game", typeof(GameItem))]
    [InlineData("Image", typeof(ImageItem))]
    [InlineData("Hex", typeof(HexItem))]
    [InlineData("Song", typeof(SongItem))]
    public async Task LaunchRandom_MapsEachFileTypeToItsItem(string type, Type expected)
    {
        var sut = Sut(HttpStatusCode.OK, LaunchBody(true, FileJson(type)));

        var outcome = await sut.LaunchRandom(TeensyStorageType.USB, StorageScope.Storage, "/", TeensyFilterType.All);

        outcome.Item.Should().BeOfType(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not a time")]
    public async Task LaunchRandom_WithoutAUsablePlayLength_KeepsTheSongsDefault(string playLength)
    {
        var sut = Sut(HttpStatusCode.OK, LaunchBody(true, FileJson(playLength: playLength)));

        var outcome = await sut.LaunchRandom(TeensyStorageType.USB, StorageScope.Storage, "/", TeensyFilterType.All);

        ((SongItem)outcome.Item!).PlayLength.Should().Be(new SongItem().PlayLength);
    }

    [Fact]
    public async Task LaunchRandom_WhenTheFileIsNotLaunchable_Fails()
    {
        var sut = Sut(HttpStatusCode.OK, LaunchBody(true, FileJson("Unknown")));

        var outcome = await sut.LaunchRandom(TeensyStorageType.USB, StorageScope.Storage, "/", TeensyFilterType.All);

        outcome.Status.Should().Be(LaunchStatus.Failed);
        outcome.Item.Should().BeNull();
    }

    [Fact]
    public async Task LaunchRandom_WhenTheUnitCantRunIt_IsIncompatible_WithTheFileThatWasPicked()
    {
        var sut = Sut(HttpStatusCode.OK, LaunchBody(false, FileJson()));

        var outcome = await sut.LaunchRandom(TeensyStorageType.USB, StorageScope.Storage, "/", TeensyFilterType.All);

        outcome.Status.Should().Be(LaunchStatus.Incompatible);
        outcome.Item!.IsCompatible.Should().BeFalse();
    }

    [Fact]
    public async Task LaunchRandom_WhenThereAreNoFiles_IsNotFound_WithTheApisWording()
    {
        var sut = Sut(HttpStatusCode.NotFound, "\"No files were found.\"");

        var outcome = await sut.LaunchRandom(TeensyStorageType.USB, StorageScope.Storage, "/", TeensyFilterType.All);

        outcome.Status.Should().Be(LaunchStatus.NotFound);
        outcome.Message.Should().Be(LaunchOutcome.NoFilesFoundMessage);
    }

    [Fact]
    public async Task LaunchRandom_WhenTrWebDoesNotAnswer_Throws()
    {
        var client = ClientWith(_ => throw new HttpRequestException("refused"));

        var act = () => new LaunchService(client, _devices).LaunchRandom(TeensyStorageType.USB, StorageScope.Storage, "/", TeensyFilterType.All);

        await act.Should().ThrowAsync<TeensyApiException>();
    }

    [Fact]
    public void TheCliEnums_MapToTheGeneratedOnesByName()
    {
        foreach (var storage in Enum.GetValues<TeensyStorageType>()) storage.ToApi().ToString().Should().Be(storage.ToString());
        foreach (var filter in Enum.GetValues<TeensyFilterType>()) filter.ToApi().ToString().Should().Be(filter.ToString());
        foreach (var scope in Enum.GetValues<StorageScope>()) scope.ToApi().ToString().Should().Be(scope.ToString());
    }
}
