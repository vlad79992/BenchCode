using System.Text.Json;
using BenchCode.Shared.Entities;
using CompetitionRun = BenchCode.CompetitionService.Entities.Run;
using CompetitionSubmission = BenchCode.CompetitionService.Entities.Submission;
using CompetitionSubmissionStatus = BenchCode.CompetitionService.Entities.SubmissionStatus;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BenchCode.CompetitionService.Services;

public sealed class FullSubmissionQueueWorker(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ISubmissionRunner runner,
    IOptions<SubmissionQueueOptions> options,
    ILogger<FullSubmissionQueueWorker> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: stoppingToken);
        await SubmissionQueueTopology.DeclareAsync(channel, options.Value, stoppingToken);
        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: options.Value.PrefetchCount,
            global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += (_, delivery) => ProcessAsync(channel, delivery, stoppingToken);
        await channel.BasicConsumeAsync(
            queue: options.Value.FullQueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
    }

    private async Task ProcessAsync(
        IChannel channel,
        BasicDeliverEventArgs delivery,
        CancellationToken cancellationToken)
    {
        FullSubmissionMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<FullSubmissionMessage>(delivery.Body.Span, jsonOptions);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Rejecting malformed full-submission message.");
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, cancellationToken);
            return;
        }

        if (message is null || message.SubmissionId == Guid.Empty)
        {
            logger.LogWarning("Rejecting full-submission message without a submission ID.");
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, cancellationToken);
            return;
        }

        try
        {
            await ProcessSubmissionAsync(message.SubmissionId, cancellationToken);
            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (SubmissionMessageException exception)
        {
            logger.LogWarning(exception, "Dead-lettering invalid full-submission message.");
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Full submission {SubmissionId} could not be persisted; requeueing.",
                message.SubmissionId);
            await Task.Delay(options.Value.RunnerRetryDelayMilliseconds, cancellationToken);
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: true, cancellationToken);
        }
    }

    private async Task ProcessSubmissionAsync(
        Guid submissionId,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CompetitionDbContext>();
        var submission = await db.Submissions
            .Include(item => item.Task)
            .ThenInclude(task => task!.Tests)
            .SingleOrDefaultAsync(item => item.Id == submissionId, cancellationToken);
        if (submission is null)
            throw new SubmissionMessageException($"Submission {submissionId} was not found.");

        if (submission.Status != CompetitionSubmissionStatus.Testing)
            return;

        if (submission.Task is null)
        {
            submission.Status = CompetitionSubmissionStatus.SystemError;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var tests = submission.Task.Tests
            .OrderBy(test => test.OrderNo)
            .Select(test => new SubmissionTestCase(
                test.Id,
                test.OrderNo,
                test.Input,
                test.Expected))
            .ToArray();
        if (tests.Length == 0)
        {
            submission.Status = CompetitionSubmissionStatus.SystemError;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        SubmissionResult result;
        try
        {
            result = await RunWithRetryAsync(submission, tests, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "ProgramRunner failed for submission {SubmissionId}.", submissionId);
            submission.Status = CompetitionSubmissionStatus.SystemError;
            submission.Verdict = null;
            submission.BuildError = null;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (result.Runs.Count > tests.Length)
        {
            logger.LogError("ProgramRunner returned more run results than supplied tests for {SubmissionId}.",
                submissionId);
            submission.Status = CompetitionSubmissionStatus.SystemError;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        submission.Status = CompetitionSubmissionStatus.Finished;
        submission.BuildError = result.BuildError;
        submission.Verdict = result.Final;
        for (var index = 0; index < result.Runs.Count; index++)
        {
            var outcome = result.Runs[index];
            db.Runs.Add(new CompetitionRun
            {
                SubmissionId = submission.Id,
                TestId = tests[index].TestId,
                Verdict = outcome.Verdict,
                Stdout = outcome.Stdout,
                Stderr = outcome.Stderr,
                ExitCode = outcome.ExitCode,
                Time = outcome.Time
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private sealed class SubmissionMessageException(string message) : Exception(message);

    private async Task<SubmissionResult> RunWithRetryAsync(
        CompetitionSubmission submission,
        IReadOnlyList<SubmissionTestCase> tests,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await runner.RunAsync(
                    submission.Language,
                    submission.Code,
                    tests,
                    cancellationToken);
            }
            catch (Exception exception) when (
                attempt < settings.RunnerAttempts &&
                (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested))
            {
                logger.LogWarning(exception,
                    "Runner attempt {Attempt} failed for submission {SubmissionId}; retrying.",
                    attempt,
                    submission.Id);
                await Task.Delay(
                    TimeSpan.FromMilliseconds(settings.RunnerRetryDelayMilliseconds * attempt),
                    cancellationToken);
            }
        }
    }
}
