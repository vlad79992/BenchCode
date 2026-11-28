extern alias CompetitionService;
using System.Net;
using RunnerRetryPolicy = CompetitionService::BenchCode.CompetitionService.Services.RunnerRetryPolicy;

namespace BenchCode.Test.IntegrationTests;

public sealed class RunnerRetryPolicyTests
{
    [Theory]
    [InlineData(HttpStatusCode.BadRequest, false)]
    [InlineData(HttpStatusCode.Unauthorized, false)]
    [InlineData(HttpStatusCode.NotFound, false)]
    [InlineData(HttpStatusCode.RequestTimeout, true)]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.BadGateway, true)]
    public void HttpErrors_AreRetriedOnlyWhenTransient(HttpStatusCode statusCode, bool expected)
    {
        var exception = new HttpRequestException("Runner response", null, statusCode);

        Assert.Equal(expected, RunnerRetryPolicy.ShouldRetry(exception, CancellationToken.None));
    }

    [Fact]
    public void NetworkAndTimeoutErrors_AreRetried()
    {
        Assert.True(RunnerRetryPolicy.ShouldRetry(
            new HttpRequestException("Connection failed"),
            CancellationToken.None));
        Assert.True(RunnerRetryPolicy.ShouldRetry(
            new TimeoutException("Runner timed out"),
            CancellationToken.None));
    }

    [Fact]
    public void CallerCancellation_IsNotRetried()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.False(RunnerRetryPolicy.ShouldRetry(
            new OperationCanceledException(),
            cancellation.Token));
    }

    [Fact]
    public void PermanentRunnerClientError_IsIdentifiedSeparately()
    {
        Assert.True(RunnerRetryPolicy.IsPermanentClientError(
            new HttpRequestException("Bad request", null, HttpStatusCode.BadRequest)));
        Assert.False(RunnerRetryPolicy.IsPermanentClientError(
            new HttpRequestException("Unavailable", null, HttpStatusCode.ServiceUnavailable)));
    }
}
