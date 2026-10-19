using NSubstitute;
using TeensyRom.Cli.Api;
using TeensyRom.Cli.Api.Generated;
using TeensyRom.Cli.Services;
using static TeensyRom.Cli.Tests.ApiTestSupport;

namespace TeensyRom.Cli.Tests;

public class DeviceServiceTests
{
    private readonly ITeensyRomApiClient _api = Substitute.For<ITeensyRomApiClient>();
    private readonly DeviceService _sut;

    public DeviceServiceTests() => _sut = new DeviceService(_api);

    private void ApiReportsVersion(string version) =>
        _api.GetVersionAsync(Arg.Any<CancellationToken>()).Returns(new GetVersionResponse { Version = version });

    private void ApiKnowsDevices(params CartDto[] carts) =>
        _api.FindDevicesAsync(false, Arg.Any<CancellationToken>()).Returns(new FindDevicesResponse { Devices = carts });

    private void ApiFailsListingDevices(Exception failure) =>
        _api.FindDevicesAsync(false, Arg.Any<CancellationToken>()).Returns(Task.FromException<FindDevicesResponse>(failure));

    private static CartDto Cart(string id, string name, DeviceState state, ConnectionType connection = ConnectionType.Serial) => new()
    {
        DeviceId = id,
        Name = name,
        DeviceState = state,
        ConnectionType = connection,
        ComPort = "COM3",
        IpAddress = "10.0.0.7"
    };

    [Theory]
    [InlineData("1.0.0-alpha.8")]
    [InlineData("1.0.0-alpha.8+abc123")]
    public async Task CheckVersion_WithThePinnedVersion_Matches(string running)
    {
        ApiReportsVersion(running);

        var check = await _sut.CheckVersionAsync();

        check.Outcome.Should().Be(VersionOutcome.Match);
    }

    [Fact]
    public async Task CheckVersion_WithAnotherVersion_ReportsWhatIsRunning()
    {
        ApiReportsVersion("1.0.0-alpha.10+abc123");

        var check = await _sut.CheckVersionAsync();

        check.Outcome.Should().Be(VersionOutcome.Mismatch);
        check.RunningVersion.Should().StartWith("1.0.0-alpha.10");
    }

    public static IEnumerable<object[]> VersionFailures() =>
    [
        [new HttpRequestException("refused")],
        [new TaskCanceledException("timed out")],
        [ApiFailure(500, "")]
    ];

    [Theory]
    [MemberData(nameof(VersionFailures))]
    public async Task CheckVersion_WithoutAReply_IsUnreachable(Exception failure)
    {
        _api.GetVersionAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException<GetVersionResponse>(failure));

        var check = await _sut.CheckVersionAsync();

        check.Outcome.Should().Be(VersionOutcome.Unreachable);
        check.RunningVersion.Should().BeNull();
    }

    [Fact]
    public async Task GetDevices_ListsOnlineAndOfflineUnitsWithTheirConnection()
    {
        ApiKnowsDevices(
            Cart("a", "Alpha", DeviceState.Connected),
            Cart("b", "Bravo", DeviceState.Connectable, ConnectionType.Tcp),
            Cart("c", "Charlie", DeviceState.ConnectionLost));

        var devices = await _sut.GetDevicesAsync();

        devices.Should().BeEquivalentTo(new[]
        {
            new DeviceSummary("a", "Alpha", true, "USB COM3", "Connected"),
            new DeviceSummary("b", "Bravo", true, "Ethernet 10.0.0.7", "Connectable"),
            new DeviceSummary("c", "Charlie", false, "USB COM3", "ConnectionLost")
        }, options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task GetDevices_WhenTrWebKnowsNone_IsEmpty()
    {
        ApiFailsListingDevices(ApiFailure(404, "\"No devices found.\""));

        var devices = await _sut.GetDevicesAsync();

        devices.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDevices_WhenRateLimited_SaysTrWebIsBusy()
    {
        ApiFailsListingDevices(ApiFailure(503, ""));

        var act = () => _sut.GetDevicesAsync();

        var failure = (await act.Should().ThrowAsync<TeensyApiException>()).Which;
        failure.Message.Should().Be("tr-web is busy listing devices — try again in a few seconds.");
        failure.StatusCode.Should().Be(503);
    }

    [Fact]
    public async Task GetDevices_WhenTrWebIsGone_SaysItStoppedAnswering()
    {
        ApiFailsListingDevices(new HttpRequestException("refused"));

        var act = () => _sut.GetDevicesAsync();

        var failure = (await act.Should().ThrowAsync<TeensyApiException>()).Which;
        failure.Message.Should().Contain("tr-web stopped answering");
        failure.StatusCode.Should().BeNull();
    }

    [Fact]
    public async Task GetDevices_NeverAsksForAFullScan()
    {
        ApiKnowsDevices();

        await _sut.GetDevicesAsync();

        await _api.Received(1).FindDevicesAsync(false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void RequireDeviceId_WithoutASelection_Throws()
    {
        var act = () => _sut.RequireDeviceId();

        act.Should().Throw<TeensyApiException>().WithMessage("No TeensyROM is selected.");
    }

    [Fact]
    public void RequireDeviceId_AfterSelect_ReturnsTheSelectedId()
    {
        var device = new DeviceSummary("abc123", "Alpha", true, "USB COM3", "Connected");

        _sut.Select(device);

        _sut.SelectedDevice.Should().Be(device);
        _sut.RequireDeviceId().Should().Be("abc123");
    }
}
