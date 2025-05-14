using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TemporaryName.Infrastructure.Outbox.Abstractions;
using TemporaryName.Infrastructure.Outbox.EFCore.Settings;

namespace TemporaryName.Infrastructure.Outbox.EFCore.Services; // Corrected namespace
public class OutboxEventRelayService : BackgroundService
{
    private readonly string _relayInstanceName;
    private readonly ILogger<OutboxEventRelayService> _logger;
    private readonly OutboxEventRelaySettings _settings;
    private readonly IOutboxMessageSource _messageSource;
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly AsyncRetryPolicy _retryPolicy;

    public OutboxEventRelayService(
        ILogger<OutboxEventRelayService> logger,
        IOptions<OutboxEventRelaySettings> settings,
        IOutboxMessageSource messageSource,
        IIntegrationEventPublisher eventPublisher)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _relayInstanceName = $"OutboxRelay-{_settings.GroupId?.Replace(" ", "_") ?? "DefaultGroup"}";
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _messageSource = messageSource ?? throw new ArgumentNullException(nameof(messageSource));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));

        if (!_settings.Enabled)
        {
            _logger.LogInformation("[{RelayName}] OutboxEventRelayService is disabled by configuration.", _relayInstanceName);
            _retryPolicy = Policy.NoOpAsync(); // No-op policy
            return;
        }

        _retryPolicy = Policy
            .Handle<Exception>(ex => !(ex is OperationCanceledException || ex is ArgumentException /*More specific non-retryable exceptions can be added*/ ))
            .WaitAndRetryAsync(
                _settings.HandlerMaxRetryAttempts,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(_settings.HandlerRetryBaseDelaySeconds, retryAttempt)),
                (exception, timeSpan, retryCount, context) =>
                {
                    string? outboxId = context.Contains("OutboxId") ? context["OutboxId"]?.ToString() : "N/A";
                    _logger.LogWarning(exception,
                        "[{RelayName}] Error relaying outbox event (Original Outbox ID: {OutboxId}). Retry attempt {RetryCount}/{MaxAttempts}. Waiting {TimeSpan}...",
                       _relayInstanceName, outboxId, retryCount, _settings.HandlerMaxRetryAttempts, timeSpan);
                });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("[{RelayName}] OutboxEventRelayService is disabled. ExecuteAsync exiting.", _relayInstanceName);
            return;
        }

        _logger.LogInformation("[{RelayName}] Worker started for GroupId: {GroupId}", _relayInstanceName, _settings.GroupId);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessNextMessageAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("[{RelayName}] Worker stopping due to cancellation request.", _relayInstanceName);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "[{RelayName}] Critical unhandled exception in ExecuteAsync loop. Worker is stopping.", _relayInstanceName);
        }
        finally
        {
            _logger.LogInformation("[{RelayName}] Worker shutting down.", _relayInstanceName);
        }
    }

    private async Task ProcessNextMessageAsync(CancellationToken stoppingToken)
    {
        ConsumedOutboxMessage? consumedMessage = null;
        try
        {
            // Consume with a timeout to allow graceful shutdown
            consumedMessage = await _messageSource.ConsumeNextAsync(TimeSpan.FromMilliseconds(_settings.ConsumeTimeoutMs), stoppingToken).ConfigureAwait(false);

            if (consumedMessage == null)
            {
                // Timeout, no message received, loop again
                return;
            }

            _logger.LogDebug("[{RelayName}] Received outbox message: ID='{OutboxId}', EventType='{EventType}'. Attempting to relay.",
                _relayInstanceName, consumedMessage.MessageId, consumedMessage.EventTypeName);

            Context pollyCtx = new Context($"OutboxRelay-{consumedMessage.MessageId}") { ["OutboxId"] = consumedMessage.MessageId };

            // Polly will retry publishing the event to the integration event publisher
            await _retryPolicy.ExecuteAsync(async (ctx, ct) =>
            {
                await _eventPublisher.PublishAsync(
                    consumedMessage.EventTypeName,
                    consumedMessage.Payload,
                    consumedMessage.Headers,
                    ct); // Pass the cancellation token from Polly/stoppingToken
            }, pollyCtx, stoppingToken);

            // If successful, acknowledge the message (e.g., commit Kafka offset)
            await consumedMessage.AcknowledgeAsync(stoppingToken);
            _logger.LogInformation("[{RelayName}] Successfully relayed and acknowledged outbox message ID {OutboxId}, EventType: {EventType}.",
                _relayInstanceName, consumedMessage.MessageId, consumedMessage.EventTypeName);

        }
        catch (Exception ex) when (stoppingToken.IsCancellationRequested && ex is OperationCanceledException)
        {
            _logger.LogInformation("[{RelayName}] Outbox relay processing loop cancelled by request for message ID {OutboxId}.", _relayInstanceName, consumedMessage?.MessageId);
            // Do not FailAsync here as the operation was cancelled. The message might be reprocessed on next start.
            throw; // Allow ExecuteAsync to handle OperationCanceledException
        }
        catch (Exception ex) // Includes Polly's RetryLimitReachedException or non-retryable exceptions from publisher
        {
            _logger.LogError(ex, "[{RelayName}] Failed to process and relay outbox message ID {OutboxId}, EventType: {EventType} after retries. Signalling failure.",
                             _relayInstanceName, consumedMessage?.MessageId, consumedMessage?.EventTypeName);

            if (consumedMessage != null)
            {
                // Signal failure to the source (e.g., move to DLQ or don't commit)
                await consumedMessage.FailAsync(ex, stoppingToken);
                _logger.LogWarning("[{RelayName}] Signalled failure for outbox message ID {OutboxId}.", _relayInstanceName, consumedMessage.MessageId);
            }
            // Depending on the severity or type of 'ex', you might want to add a small delay here
            // to prevent tight loops if the source keeps providing the same problematic message.
        }
    }

    public override void Dispose()
    {
        _logger.LogInformation("[{RelayName}] Disposing OutboxEventRelayService.", _relayInstanceName);
        // No Kafka consumer to dispose here directly.
        // The IOutboxMessageSource implementation will manage its resources.
        base.Dispose();
        GC.SuppressFinalize(this);
    }
}