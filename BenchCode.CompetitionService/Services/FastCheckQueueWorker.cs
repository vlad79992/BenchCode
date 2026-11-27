using System.Text.Json;
using BenchCode.Shared.Entities;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BenchCode.CompetitionService.Services;

public sealed class FastCheckQueueWorker(
    IConnection connection,
    ISubmissionRunner runner,
    IOptions<SubmissionQueueOptions> options,
    ILogger<FastCheckQueueWorker> logger) : BackgroundService
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
            queue: options.Value.FastQueueName,
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
        PublicCheckMessage? message;
        try
        {
            message = JsonSerializer.Deserialize<PublicCheckMessage>(delivery.Body.Span, jsonOptions);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Rejecting malformed fast-check message.");
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, cancellationToken);
            return;
        }

        if (message is null || string.IsNullOrWhiteSpace(delivery.BasicProperties.ReplyTo) ||
            string.IsNullOrWhiteSpace(delivery.BasicProperties.CorrelationId))
        {
            logger.LogWarning("Rejecting fast-check message without payload or reply metadata.");
            await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, cancellationToken);
            return;
        }

        PublicCheckResponse response;
        try
        {
            var result = await RunWithRetryAsync(message, cancellationToken);
            if (result.Runs.Count > message.Tests.Count)
                throw new InvalidOperationException("ProgramRunner returned more run results than supplied tests.");

            var runs = result.Runs
                .Select((outcome, index) => new PublicCheckRun(
                    message.Tests[index].TestId,
                    message.Tests[index].OrderNo,
                    outcome))
                .ToArray();
            response = new PublicCheckResponse(
                Completed: true,
                Built: result.Built,
                BuildError: result.BuildError,
                FinalVerdict: result.Final,
                Runs: runs);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogError(exception, "Fast check failed while calling ProgramRunner.");
            response = new PublicCheckResponse(
                Completed: false,
                Built: false,
                BuildError: null,
                FinalVerdict: null,
                Runs: [],
                Error: "The program runner is unavailable.");
        }

        var properties = new BasicProperties
        {
            ContentType = "application/json",
            CorrelationId = delivery.BasicProperties.CorrelationId
        };
        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: delivery.BasicProperties.ReplyTo,
            mandatory: false,
            basicProperties: properties,
            body: JsonSerializer.SerializeToUtf8Bytes(response, jsonOptions),
            cancellationToken: cancellationToken);
        await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken);
    }

    private async Task<SubmissionResult> RunWithRetryAsync(
        PublicCheckMessage message,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await runner.RunAsync(
                    message.Language,
                    message.Code,
                    message.Tests,
                    cancellationToken);
            }
            catch (Exception exception) when (
                attempt < settings.RunnerAttempts &&
                (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested))
            {
                logger.LogWarning(exception, "Fast check attempt {Attempt} failed; retrying.", attempt);
                await Task.Delay(
                    TimeSpan.FromMilliseconds(settings.RunnerRetryDelayMilliseconds * attempt),
                    cancellationToken);
            }
        }
    }
}
