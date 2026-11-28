using System.Text.Json;
using BenchCode.Shared.Entities;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BenchCode.CompetitionService.Services;

public sealed class SubmissionQueueClient(
    IConnection connection,
    IOptions<SubmissionQueueOptions> options) : ISubmissionQueue
{
    private static readonly JsonSerializerOptions jsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PublicCheckResponse> RunPublicCheckAsync(
        PublicCheckMessage message,
        CancellationToken cancellationToken)
    {
        if (message.Tests.Count == 0)
            throw new ArgumentException("At least one public test is required.", nameof(message));

        await using var channel = await connection.CreateChannelAsync(
            cancellationToken: cancellationToken);
        await SubmissionQueueTopology.DeclareAsync(channel, options.Value, cancellationToken);
        var replyQueue = await channel.QueueDeclareAsync(
            queue: string.Empty,
            durable: false,
            exclusive: true,
            autoDelete: true,
            cancellationToken: cancellationToken);
        var correlationId = Guid.NewGuid().ToString("N");
        var responseSource = new TaskCompletionSource<PublicCheckResponse>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var consumer = new AsyncEventingBasicConsumer(channel);

        consumer.ReceivedAsync += (_, delivery) =>
        {
            if (delivery.BasicProperties.CorrelationId == correlationId)
            {
                try
                {
                    var response = JsonSerializer.Deserialize<PublicCheckResponse>(
                        delivery.Body.Span,
                        jsonOptions);
                    if (response is not null)
                        responseSource.TrySetResult(response);
                    else
                        responseSource.TrySetException(
                            new InvalidOperationException("The fast-check worker returned an empty response."));
                }
                catch (JsonException exception)
                {
                    responseSource.TrySetException(exception);
                }
            }

            return Task.CompletedTask;
        };

        await channel.BasicConsumeAsync(
            queue: replyQueue.QueueName,
            autoAck: true,
            consumer: consumer,
            cancellationToken: cancellationToken);

        var properties = new BasicProperties
        {
            ContentType = "application/json",
            CorrelationId = correlationId,
            ReplyTo = replyQueue.QueueName,
            Expiration = options.Value.FastMessageTtlMilliseconds.ToString()
        };
        var body = JsonSerializer.SerializeToUtf8Bytes(message, jsonOptions);
        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: options.Value.FastQueueName,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);

        return await responseSource.Task.WaitAsync(
            TimeSpan.FromSeconds(options.Value.FastReplyTimeoutSeconds),
            cancellationToken);
    }

    public async Task EnqueueFullSubmissionAsync(
        Guid submissionId,
        CancellationToken cancellationToken)
    {
        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);
        await using var channel = await connection.CreateChannelAsync(
            channelOptions,
            cancellationToken);
        await SubmissionQueueTopology.DeclareAsync(channel, options.Value, cancellationToken);
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };
        var body = JsonSerializer.SerializeToUtf8Bytes(
            new FullSubmissionMessage(submissionId),
            jsonOptions);

        await channel.BasicPublishAsync(
            exchange: string.Empty,
            routingKey: options.Value.FullQueueName,
            mandatory: true,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken);
    }
}

internal static class SubmissionQueueTopology
{
    public static async Task DeclareAsync(
        IChannel channel,
        SubmissionQueueOptions options,
        CancellationToken cancellationToken)
    {
        const string deadLetterRoutingKey = "dead";

        await channel.ExchangeDeclareAsync(
            exchange: options.DeadLetterExchangeName,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(
            queue: options.DeadLetterQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken);
        await channel.QueueBindAsync(
            queue: options.DeadLetterQueueName,
            exchange: options.DeadLetterExchangeName,
            routingKey: deadLetterRoutingKey,
            cancellationToken: cancellationToken);

        var deadLetterArguments = new Dictionary<string, object?>
        {
            ["x-dead-letter-exchange"] = options.DeadLetterExchangeName,
            ["x-dead-letter-routing-key"] = deadLetterRoutingKey
        };
        await channel.QueueDeclareAsync(
            queue: options.FastQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: deadLetterArguments,
            cancellationToken: cancellationToken);
        await channel.QueueDeclareAsync(
            queue: options.FullQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: deadLetterArguments,
            cancellationToken: cancellationToken);
    }
}
