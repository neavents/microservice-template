// src/TemporaryName.Infrastructure.Outbox.EFCore/Services/OutboxEventRelayService.cs
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic; // For IReadOnlyDictionary
using System.Threading;
using System.Threading.Tasks;
using TemporaryName.Application.Contracts.Abstractions.Messaging; // For IIntegrationEventPublisher
using TemporaryName.Infrastructure.Outbox.Abstractions;         // For IOutboxMessageSource, ConsumedOutboxMessage
using TemporaryName.Infrastructure.Outbox.EFCore.Settings;    // For OutboxEventRelaySettings

namespace TemporaryName.Infrastructure.Outbox.EFCore.Services;

/// <summary>
/// A background service responsible for relaying messages from an IOutboxMessageSource
/// (e.g., Kafka topic populated by Debezium) to an IIntegrationEventPublisher (e.g., MassTransit).
/// It ensures at-least-once processing semantics using acknowledgment and robust retry policies.
/// This service is a critical component of the transactional outbox pattern, ensuring reliable
/// propagation of domain events to other services or message queues.
/// </summary>
public class OutboxEventRelayService : BackgroundService
{
    private readonly string _relayInstanceLogName;
    private readonly ILogger<OutboxEventRelayService> _logger;
    private readonly OutboxEventRelaySettings _settings;
    private readonly IOutboxMessageSource _messageSource;
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly AsyncRetryPolicy _retryPolicy;

    public OutboxEventRelayService(
        ILogger<OutboxEventRelayService> logger,
        IOptions<OutboxEventRelaySettings> settingsOptions,
        IOutboxMessageSource messageSource,
        IIntegrationEventPublisher eventPublisher)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(settingsOptions);
        _settings = settingsOptions.Value ?? throw new InvalidOperationException("OutboxEventRelaySettings configuration is missing or invalid.");

        _relayInstanceLogName = string.IsNullOrWhiteSpace(_settings.RelayInstanceLogName)
            ? $"OutboxRelay-{Guid.NewGuid().ToString("N")[..8]}" // Generate a unique default if not provided
            : _settings.RelayInstanceLogName;
        _logger = logger;

        if (!_settings.Enabled)
        {
            _logger.LogInformation("[{RelayName}] OutboxEventRelayService is configured but DISABLED. It will not perform any operations.", _relayInstanceLogName);
            // Assign Null Object Pattern implementations to dependencies to prevent NullReferenceExceptions
            // if ExecuteAsync were somehow invoked despite the Enabled flag (defensive programming).
            _messageSource = new NoOpOutboxMessageSource();
            _eventPublisher = new NoOpIntegrationEventPublisher();
            _retryPolicy = Policy.NoOpAsync(); // No operations will be retried.
            return;
        }

        _messageSource = messageSource ?? throw new ArgumentNullException(nameof(messageSource));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));

        // Define a robust Polly retry policy for publishing events to the IIntegrationEventPublisher.
        _retryPolicy = Policy
            .Handle<Exception>(ex => // Lambda to determine if an exception should be retried.
            {
                // Do NOT retry on these common non-transient error types.
                if (ex is ArgumentNullException ||
                    ex is ArgumentException ||
                    ex is InvalidOperationException || // Often indicates a programming error or unrecoverable state.
                    ex is System.Text.Json.JsonException || // Serialization errors are unlikely to be resolved by retrying.
                    ex is NotImplementedException ||
                    ex is NullReferenceException) // Should ideally not happen with proper checks.
                {
                    _logger.LogWarning(ex,
                        "[{RelayName}] Non-retryable exception type ({ExceptionType}) encountered during event publishing. This attempt will NOT be retried by Polly.",
                        _relayInstanceLogName, ex.GetType().Name);
                    return false; // Do not retry this exception type.
                }

                // Consider specific transient exceptions from your IIntegrationEventPublisher (e.g., MassTransit broker unavailable).
                // For example: if (ex is MassTransit.RabbitMqConnectionException) return true;
                // For now, we assume most other exceptions might be transient (e.g., network glitches, temporary service unavailability).
                _logger.LogDebug(ex,
                    "[{RelayName}] Exception of type {ExceptionType} encountered during event publishing. Will attempt retry based on policy.",
                    _relayInstanceLogName, ex.GetType().Name);
                return true; // Retry other exceptions not explicitly marked non-retryable.
            })
            .WaitAndRetryAsync(
                retryCount: _settings.HandlerMaxRetryAttempts,
                sleepDurationProvider: retryAttempt => // Exponential backoff strategy.
                {
                    TimeSpan delay = TimeSpan.FromSeconds(Math.Pow(_settings.HandlerRetryBaseDelaySeconds, retryAttempt));
                    // Add jitter to prevent thundering herd on retries. Max 1 second jitter.
                    TimeSpan jitter = TimeSpan.FromMilliseconds(new Random().Next(0, 1000));
                    return delay + jitter;
                },
                onRetryAsync: async (exception, timeSpan, retryCount, context) => // Action to perform on each retry.
                {
                    string? outboxId = context.TryGetValue("OutboxId", out object? id) ? id?.ToString() : "N/A";
                    string? eventType = context.TryGetValue("EventType", out object? type) ? type?.ToString() : "N/A";
                    _logger.LogWarning(exception,
                        "[{RelayName}] Transient error publishing outbox event (Original Outbox ID: {OutboxId}, EventType: {EventType}). Retry attempt {RetryCount}/{MaxAttempts}. Waiting {TimeSpanString} before next attempt...",
                       _relayInstanceLogName, outboxId, eventType, retryCount, _settings.HandlerMaxRetryAttempts, timeSpan.ToString("g"));
                    await Task.CompletedTask; // onRetryAsync must return Task.
                });

        _logger.LogInformation(
            "[{RelayName}] OutboxEventRelayService initialized and ENABLED. Max publish retries: {MaxRetries}, Base retry delay: {BaseDelay}s. Consume timeout: {ConsumeTimeoutMs}ms.",
            _relayInstanceLogName, _settings.HandlerMaxRetryAttempts, _settings.HandlerRetryBaseDelaySeconds, _settings.ConsumeTimeoutMs);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("[{RelayName}] OutboxEventRelayService is disabled. ExecuteAsync exiting immediately.", _relayInstanceLogName);
            return;
        }

        _logger.LogInformation("[{RelayName}] OutboxEventRelayService worker process started. Listening for messages...", _relayInstanceLogName);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ProcessNextMessageAsync(stoppingToken).ConfigureAwait(false);
                // If ProcessNextMessageAsync completed (either processed a message or timed out),
                // loop again immediately. The ConsumeNextAsync in IOutboxMessageSource handles the polling timeout.
                // A very small delay can be added here if the source might return null very rapidly without error,
                // but typically the source's timeout (`_settings.ConsumeTimeoutMs`) manages this.
                // if (stoppingToken.IsCancellationRequested) break; // Check again before potential tiny delay
                // await Task.Delay(TimeSpan.FromMilliseconds(50), stoppingToken); // Example: tiny breather if source is hyperactive with nulls
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("[{RelayName}] OutboxEventRelayService worker stopping gracefully due to cancellation request (OperationCanceledException).", _relayInstanceLogName);
        }
        catch (Exception ex) // Should ideally not be reached if ProcessNextMessageAsync handles its exceptions.
        {
            _logger.LogCritical(ex, "[{RelayName}] CRITICAL UNHANDLED EXCEPTION in OutboxEventRelayService ExecuteAsync main loop. Worker is forced to stop. This indicates a potential bug in the loop or unhandled exception propagation.", _relayInstanceLogName);
        }
        finally
        {
            _logger.LogInformation("[{RelayName}] OutboxEventRelayService worker has shut down its main processing loop.", _relayInstanceLogName);
        }
    }

    private async Task ProcessNextMessageAsync(CancellationToken stoppingToken)
    {
        ConsumedOutboxMessage? consumedMessage = null;
        string? currentMessageIdForLogging = "N/A_NotYetConsumed";

        try
        {
            // Attempt to consume the next message from the source.
            // This call will block for at most _settings.ConsumeTimeoutMs or until a message is available or cancellation.
            consumedMessage = await _messageSource.ConsumeNextAsync(TimeSpan.FromMilliseconds(_settings.ConsumeTimeoutMs), stoppingToken).ConfigureAwait(false);

            if (consumedMessage == null)
            {
                // No message was available from the source within the timeout, or source indicated no message. This is normal.
                _logger.LogTrace("[{RelayName}] No message consumed from source within timeout or source is idle.", _relayInstanceLogName);
                return;
            }
            currentMessageIdForLogging = consumedMessage.MessageId; // For more accurate logging in catch blocks

            _logger.LogInformation("[{RelayName}] Received outbox message for processing: ID='{OutboxId}', EventType='{EventType}'. Attempting to publish to integration bus.",
                _relayInstanceLogName, consumedMessage.MessageId, consumedMessage.EventTypeName);

            // Prepare context for Polly retry policy, useful for logging within retry attempts.
            Context pollyContext = new Context($"RelayAttempt-{consumedMessage.MessageId}")
            {
                { "OutboxId", consumedMessage.MessageId },
                { "EventType", consumedMessage.EventTypeName },
                { "RelayInstance", _relayInstanceLogName }
            };

            // Execute the event publishing through the configured Polly retry policy.
            // The stoppingToken is passed to Polly to ensure that if the service is shutting down,
            // ongoing retries are also cancelled.
            await _retryPolicy.ExecuteAsync(
                async (ctx, ct) => // The action to execute, 'ct' is Polly's CancellationToken.
                {
                    await _eventPublisher.PublishAsync(
                        consumedMessage.EventTypeName,
                        consumedMessage.Payload,
                        consumedMessage.Headers, // Pass all headers from ConsumedOutboxMessage
                        ct // Use Polly's CancellationToken for the actual publish operation
                    ).ConfigureAwait(false);
                },
                pollyContext,
                stoppingToken // Overall cancellation for the Polly execution itself
            ).ConfigureAwait(false);

            // If Polly's ExecuteAsync completes without throwing, the publishing was successful (possibly after retries).
            // Now, acknowledge the message with the source (e.g., commit Kafka offset).
            await consumedMessage.AcknowledgeAsync(stoppingToken).ConfigureAwait(false);
            _logger.LogInformation("[{RelayName}] Successfully published and acknowledged outbox message: ID='{OutboxId}', EventType='{EventType}'.",
                _relayInstanceLogName, consumedMessage.MessageId, consumedMessage.EventTypeName);

        }
        catch (Exception ex) when (stoppingToken.IsCancellationRequested && ex is OperationCanceledException)
        {
             // This specifically catches OperationCanceledException if stoppingToken was triggered during an await.
             _logger.LogInformation(ex, "[{RelayName}] Processing of message ID '{OutboxId}' was cancelled by service shutdown request. Message was not fully processed or acknowledged.",
                _relayInstanceLogName, currentMessageIdForLogging);
             // Do not call FailAsync if cancellation was external. The message will likely be re-processed on next service start if not acknowledged.
             throw; // Re-throw OperationCanceledException to be handled by ExecuteAsync for graceful shutdown.
        }
        catch (Exception ex) // This catches exceptions if Polly retries are exhausted, or non-retryable exceptions from publisher, or issues in ConsumeNextAsync not caught there.
        {
            _logger.LogError(ex, "[{RelayName}] Failed to publish outbox message ID '{OutboxId}', EventType: '{EventType}' after all retries or due to a non-retryable/unexpected error. Signaling failure to message source.",
                             _relayInstanceLogName, consumedMessage?.MessageId ?? currentMessageIdForLogging, consumedMessage?.EventTypeName ?? "N/A_EventTypeUnknown");

            if (consumedMessage != null)
            {
                try
                {
                    // Signal failure to the message source. This might mean not committing a Kafka offset,
                    // or if the source implements DLT logic, it might try to send it there.
                    await consumedMessage.FailAsync(ex, stoppingToken).ConfigureAwait(false);
                    _logger.LogWarning("[{RelayName}] Failure signaled to message source for outbox message ID '{OutboxId}'. Message may be reprocessed by source or sent to DLT by source if configured.",
                        _relayInstanceLogName, consumedMessage.MessageId);
                }
                catch (Exception failEx)
                {
                    // This is a critical situation: processing failed, AND signaling that failure back to the source also failed.
                    _logger.LogCritical(failEx, "[{RelayName}] CRITICAL FAILURE: Could not signal failure to message source for outbox message ID '{OutboxId}' after processing error. Original error type: {OriginalErrorType}. This could lead to message loss or stuck messages if the source doesn't have its own robust dead-lettering or retry/skip mechanisms.",
                        _relayInstanceLogName, consumedMessage.MessageId, ex.GetType().Name);
                    // The message might be re-processed indefinitely if the source doesn't handle this.
                    // This scenario requires careful monitoring and potentially manual intervention.
                }
            }
            // Optional: Implement a circuit breaker for the entire relay service if it encounters too many consecutive terminal failures.
            // Or add a delay here if this path is hit frequently, to prevent tight loops on persistently failing messages,
            // especially if the message source doesn't have robust DLT/skip logic.
            // await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false); // Example delay
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("[{RelayName}] OutboxEventRelayService StopAsync called by host. Initiating graceful shutdown of background processing.", _relayInstanceLogName);
        // Allow any ongoing ProcessNextMessageAsync to complete or be cancelled by the stoppingToken passed to it.
        await base.StopAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("[{RelayName}] OutboxEventRelayService has completed its StopAsync and shut down.", _relayInstanceLogName);
    }

    // Dispose is handled by the BackgroundService base class.
    // IOutboxMessageSource and IIntegrationEventPublisher are injected dependencies and their lifecycle
    // (including Dispose if they implement IDisposable) is managed by the DI container.
}

// --- Null Object Pattern Implementations for Disabled State (can be file-scoped for C# 9+) ---
file sealed class NoOpOutboxMessageSource : IOutboxMessageSource
{
    public Task<ConsumedOutboxMessage?> ConsumeNextAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        // Simulate a timeout or no message.
        return Task.FromResult<ConsumedOutboxMessage?>(null);
    }
    public void Dispose() { /* No resources to dispose for NoOp */ }
}

file sealed class NoOpIntegrationEventPublisher : IIntegrationEventPublisher
{
    public Task PublishAsync(string eventTypeName, string payload, IReadOnlyDictionary<string, string> headers, CancellationToken cancellationToken)
    {
        // Do nothing.
        return Task.CompletedTask;
    }
}
