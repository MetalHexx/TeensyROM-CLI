using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using TeensyRom.Cli.Api;
using TeensyRom.Cli.Api.Generated;
using TeensyRom.Cli.Logging;
using TeensyRom.Cli.Services;

namespace TeensyRom.Cli.Tests;

public class DeviceLogStreamTests
{
    private const string DeviceId = "abc123";

    [Theory]
    [InlineData("(abc123) Internal: => Indexing (SD): /music", "=> Indexing (SD): /music")]
    [InlineData("(abc123) Internal Success: Indexing completed for SD storage.", "Indexing completed for SD storage.")]
    [InlineData("(abc123) External Error: Failed: timed out", "Failed: timed out")]
    public void TryReadMessage_GivesTheDevicesLineWithoutItsPrefix(string line, string expected)
    {
        DeviceLogStream.TryReadMessage(line, DeviceId, out var message).Should().BeTrue();

        message.Should().Be(expected);
    }

    [Theory]
    [InlineData("(other) Internal: => Indexing (SD): /music")]
    [InlineData("Log streaming faulted: boom")]
    [InlineData("seen (abc123) Internal: not at the start")]
    [InlineData("(abc1234) Internal: a device whose id only starts the same")]
    public void TryReadMessage_IgnoresLinesThatAreNotTheDevices(string line)
    {
        DeviceLogStream.TryReadMessage(line, DeviceId, out _).Should().BeFalse();
    }

    [Fact]
    public async Task FollowAsync_GivesTheSelectedDevicesLinesAsTheyAreLogged_AndStopsTheStreamWhenDisposed()
    {
        await using var trWeb = await FakeTrWebLogs.StartAsync();
        var stream = StreamTo(trWeb.Address);

        var following = await stream.FollowAsync(line => trWeb.Events.Enqueue($"line: {line}"));
        await trWeb.LogAsync("(abc123) Internal: => Indexing (SD): /music");
        await trWeb.LogAsync("(other) Internal: => Indexing (SD): /games");
        await trWeb.LogAsync("Log streaming faulted: boom");
        await trWeb.LogAsync("(abc123) Internal: Indexing completed for SD storage.");
        await following.DisposeAsync();

        trWeb.Events.Should().Equal("start", "line: => Indexing (SD): /music", "line: Indexing completed for SD storage.", "stop");
    }

    [Fact]
    public async Task DisposeAsync_WaitsForLinesTrWebStillHasQueuedBeforeStopping()
    {
        await using var trWeb = await FakeTrWebLogs.StartAsync();
        var stream = StreamTo(trWeb.Address);

        var following = await stream.FollowAsync(line => trWeb.Events.Enqueue($"line: {line}"));
        var disposing = following.DisposeAsync().AsTask();
        await Task.Delay(100);
        await trWeb.LogAsync("(abc123) Internal: Indexing completed for SD storage.");
        await disposing;

        trWeb.Events.Should().Equal("start", "line: Indexing completed for SD storage.", "stop");
    }

    [Fact]
    public async Task FollowAsync_WhenTrWebIsNotListening_FailsWithAPlainMessage()
    {
        var stream = StreamTo(UnusedAddress());

        var follow = () => stream.FollowAsync(_ => { });

        (await follow.Should().ThrowAsync<TeensyApiException>()).Which.Message.Should().NotBeNullOrWhiteSpace();
    }

    private static DeviceLogStream StreamTo(Uri address)
    {
        var devices = Substitute.For<IDeviceService>();
        devices.RequireDeviceId().Returns(DeviceId);
        var api = new TeensyRomApiClient(new HttpClient { BaseAddress = address });

        return new DeviceLogStream(api, devices, Substitute.For<ILoggingService>(), address);
    }

    private static Uri UnusedAddress()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return new Uri($"http://127.0.0.1:{port}");
    }

    public sealed class FakeLogsHub : Hub;

    /// <summary>The corner of tr-web the stream talks to: its log hub, and the endpoints that start and stop the stream.</summary>
    private sealed class FakeTrWebLogs : IAsyncDisposable
    {
        private readonly WebApplication _app;
        private readonly IHubContext<FakeLogsHub> _hub;

        /// <summary>The server's "start" and "stop" calls, in order, plus whatever the test adds.</summary>
        public ConcurrentQueue<string> Events { get; } = new();

        public Uri Address { get; }

        private FakeTrWebLogs(WebApplication app)
        {
            _app = app;
            _hub = app.Services.GetRequiredService<IHubContext<FakeLogsHub>>();
            Address = new Uri(app.Urls.First());
        }

        public static async Task<FakeTrWebLogs> StartAsync()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Logging.ClearProviders();
            builder.Services.AddSignalR();

            var app = builder.Build();
            FakeTrWebLogs? trWeb = null;

            app.MapHub<FakeLogsHub>("/api/logHub");
            app.MapPost("/api/logs", () => { trWeb!.Events.Enqueue("start"); return Results.Json(new { message = "Success!" }); });
            app.MapDelete("/api/logs", () => { trWeb!.Events.Enqueue("stop"); return Results.Json(new { message = "Success!" }); });

            await app.StartAsync();
            trWeb = new FakeTrWebLogs(app);
            return trWeb;
        }

        public Task LogAsync(string line) => _hub.Clients.All.SendAsync("LogProduced", line);

        public async ValueTask DisposeAsync()
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
