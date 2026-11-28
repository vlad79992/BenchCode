using System.Net;

namespace BenchCode.CompetitionService.Services;

public static class RunnerRetryPolicy
{
    public static bool ShouldRetry(Exception exception, CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException)
            return !cancellationToken.IsCancellationRequested;

        if (exception is TimeoutException)
            return true;

        if (exception is not HttpRequestException httpException)
            return false;

        if (httpException.StatusCode is null)
            return true;

        return httpException.StatusCode is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests ||
               (int)httpException.StatusCode.Value >= 500;
    }

    public static bool IsPermanentClientError(Exception exception) =>
        exception is HttpRequestException { StatusCode: { } statusCode } &&
        (int)statusCode is >= 400 and < 500 &&
        statusCode is not HttpStatusCode.RequestTimeout and not HttpStatusCode.TooManyRequests;
}
