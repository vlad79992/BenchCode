
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BenchCode.CompetitionService.Services;

public sealed class SubmissionOutboxOptions
{
    public int PollIntervalMilliseconds { get; set; } = 500;
    public int BatchSize { get; set; } = 20;
    public int InitialRetryDelayMilliseconds { get; set; } = 500;
    public int MaxRetryDelayMilliseconds { get; set; } = 30_000;
}

public sealed class SubmissionOutboxPublisher(
    IServiceScopeFactory scopeFactory,
    ISubmissionQueue submissionQueue,
    IOptions<SubmissionOutboxOptions> options,
    ILogger<SubmissionOutboxPublisher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to dispatch the submission outbox batch.");
            }

            await Task.Delay(
                TimeSpan.FromMilliseconds(options.Value.PollIntervalMilliseconds),
                stoppingToken);
        }
    }

    private async Task PublishPendingAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CompetitionDbContext>();
        var now = DateTimeOffset.UtcNow;
        var messages = await db.SubmissionOutboxMessages
            .Where(message => message.PublishedAt == null && message.NextAttemptAt <= now)
            .OrderBy(message => message.CreatedAt)
            .Take(options.Value.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await submissionQueue.EnqueueFullSubmissionAsync(
                    message.SubmissionId,
                    cancellationToken);
                message.PublishedAt = DateTimeOffset.UtcNow;
                message.LastError = null;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                message.Attempts++;
                message.LastError = TruncateError(exception.Message);
                message.NextAttemptAt = DateTimeOffset.UtcNow + GetRetryDelay(message.Attempts);
                logger.LogWarning(exception,
                    "Could not publish outbox item {OutboxId} for submission {SubmissionId}; retry {Attempt} scheduled at {NextAttemptAt}.",
                    message.Id,
                    message.SubmissionId,
                    message.Attempts,
                    message.NextAttemptAt);
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private TimeSpan GetRetryDelay(int attempt)
    {
        var initialDelay = Math.Max(1, options.Value.InitialRetryDelayMilliseconds);
        var maximumDelay = Math.Max(initialDelay, options.Value.MaxRetryDelayMilliseconds);
        var multiplier = Math.Pow(2, Math.Min(attempt - 1, 20));
        var delayMilliseconds = Math.Min(initialDelay * multiplier, maximumDelay);
        return TimeSpan.FromMilliseconds(delayMilliseconds);
    }

    private static string TruncateError(string error) =>
        error.Length <= 2000 ? error : error[..2000];
}
