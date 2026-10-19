using System.Net;
using TeensyRom.Cli.Api;
using TeensyRom.Cli.Api.Generated;
using static TeensyRom.Cli.Tests.ApiTestSupport;

namespace TeensyRom.Cli.Tests;

public class ApiErrorsTests
{
    private static async Task<TeensyApiException> Failure(HttpStatusCode status, string body, bool documented404Operation = false)
    {
        var client = ClientAnswering(status, body);
        Func<Task> call = documented404Operation
            ? () => client.LaunchRandomAsync("abc123", TeensyStorageType.USB)
            : () => client.FindDevicesAsync(fullScan: false);

        var act = () => ApiErrors.Call(call);

        return (await act.Should().ThrowAsync<TeensyApiException>()).Which;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task NotFound_WithABareJsonString_ShowsThatString(bool documented404Operation)
    {
        var failure = await Failure(HttpStatusCode.NotFound, "\"The storage USB is not available.\"", documented404Operation);

        failure.Message.Should().Be("The storage USB is not available.");
        failure.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task BadRequest_WithProblemDetails_ShowsTheTitle()
    {
        var failure = await Failure(HttpStatusCode.BadRequest, """{"title":"The device id is required.","detail":"Ignored when a title exists.","status":400}""");

        failure.Message.Should().Be("The device id is required.");
        failure.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task BadGateway_WithProblemDetails_ShowsTheTitle()
    {
        var failure = await Failure(HttpStatusCode.BadGateway, """{"title":"The TeensyROM did not answer.","status":502}""");

        failure.Message.Should().Be("The TeensyROM did not answer.");
        failure.StatusCode.Should().Be(502);
    }

    [Fact]
    public async Task ProblemDetails_WithoutATitle_ShowsTheDetail()
    {
        var failure = await Failure(HttpStatusCode.BadRequest, """{"detail":"Path must start with a slash.","status":400}""");

        failure.Message.Should().Be("Path must start with a slash.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("<html>Bad gateway</html>")]
    [InlineData("{}")]
    public async Task UnreadableBody_ShowsTheStatusFallback(string body)
    {
        var failure = await Failure(HttpStatusCode.InternalServerError, body);

        failure.Message.Should().Be("tr-web couldn't complete that request (HTTP 500).");
    }

    [Fact]
    public async Task ConnectionRefused_SaysTrWebStoppedAnswering_WithoutAStatus()
    {
        var client = ClientWith(_ => throw new HttpRequestException("No connection could be made because the target machine actively refused it."));

        var act = () => ApiErrors.Call(() => client.GetVersionAsync());

        var failure = (await act.Should().ThrowAsync<TeensyApiException>()).Which;
        failure.Message.Should().Be("tr-web stopped answering at localhost:10213 — start it and try again.");
        failure.StatusCode.Should().BeNull();
    }

    [Fact]
    public async Task Failures_NeverCarryNSwagWordingOrAStackTrace()
    {
        var failure = await Failure(HttpStatusCode.InternalServerError, "");

        failure.Message.Should().NotContain("HTTP status code of the response was not expected").And.NotContain("Status:");
        failure.InnerException.Should().BeOfType<ApiException>();
    }

    [Fact]
    public void ToMessage_WithoutABody_UsesTheStatusFallback()
    {
        ApiErrors.ToMessage(ApiFailure(503, null)).Should().Be("tr-web couldn't complete that request (HTTP 503).");
    }

    [Fact]
    public async Task Call_PassesTheResultOfASuccessfulCallThrough()
    {
        var result = await ApiErrors.Call(() => Task.FromResult(42));

        result.Should().Be(42);
    }
}
