using System;
using Microsoft.Extensions.Logging;

namespace TemporaryName.Infrastructure.Observability;

public static partial class DependencyInjection
{
    private const int ClassId = 1;
    private const int BaseEventId = Logging.ObservabilityBaseEventId + (ClassId * Logging.IncrementPerClass);

    private const int EvtObservabilitySetupStarting = BaseEventId + (0 * Logging.IncrementPerLog);
    private const int EvtSerilogConfigured = BaseEventId + (1 * Logging.IncrementPerLog);
    private const int EvtElasticsearchSinkConfigured = BaseEventId + (2 * Logging.IncrementPerLog);
    private const int EvtElasticApmConfigured = BaseEventId + (3 * Logging.IncrementPerLog);
    private const int EvtObservabilitySetupCompleted = BaseEventId + (4 * Logging.IncrementPerLog);
    private const int EvtElasticsearchSinkDisabled = BaseEventId + (5 * Logging.IncrementPerLog);
    private const int EvtElasticApmDisabled = BaseEventId + (6 * Logging.IncrementPerLog);
    private const int EvtSelfLogEnabled = BaseEventId + (7 * Logging.IncrementPerLog);
    private const int EvtElasticApmMiddlewareRegistered = BaseEventId + (8 * Logging.IncrementPerLog);


    /// <summary>
    /// Logs the start of the ELK-focused observability services registration.
    /// </summary>
    [LoggerMessage(EventId = EvtObservabilitySetupStarting, Level = LogLevel.Information, Message = "Starting ELK-focused {ProjectName} services registration for '{ServiceName}'.")]
    public static partial void LogObservabilitySetupStarting(ILogger logger, string serviceName, string projectName = Logging.ProjectName);

    /// <summary>
    /// Logs the configuration of Serilog.
    /// </summary>
    [LoggerMessage(EventId = EvtSerilogConfigured, Level = LogLevel.Information, Message = "Serilog configured for application. Console Sink: {ConsoleEnabled}. Default Minimum Level: {MinLevel}.")]
    public static partial void LogSerilogConfigured(ILogger logger, bool consoleEnabled, string minLevel);
    
    /// <summary>
    /// Logs that Serilog SelfLog has been enabled.
    /// </summary>
    [LoggerMessage(EventId = EvtSelfLogEnabled, Level = LogLevel.Warning, Message = "Serilog SelfLog is enabled, writing to Console. Use for diagnostics only.")]
    public static partial void LogSelfLogEnabled(ILogger logger);

    /// <summary>
    /// Logs the configuration of the Serilog Elasticsearch Sink.
    /// </summary>
    [LoggerMessage(EventId = EvtElasticsearchSinkConfigured, Level = LogLevel.Information, Message = "Serilog Elasticsearch Sink configured. Nodes: {Nodes}. IndexFormat: {IndexFormat}. MinimumLevelForSink: {MinimumLogEventLevel}")]
    public static partial void LogElasticsearchSinkConfigured(ILogger logger, string nodes, string? indexFormat, string minimumLogEventLevel);
    
    /// <summary>
    /// Logs that the Serilog Elasticsearch Sink is disabled in settings.
    /// </summary>
    [LoggerMessage(EventId = EvtElasticsearchSinkDisabled, Level = LogLevel.Information, Message = "Serilog Elasticsearch Sink is DISABLED in settings.")]
    public static partial void LogElasticsearchSinkDisabled(ILogger logger);

    /// <summary>
    /// Logs the configuration of the Elastic APM Agent.
    /// </summary>
    [LoggerMessage(EventId = EvtElasticApmConfigured, Level = LogLevel.Information, Message = "Elastic APM Agent configured for service '{ServiceName}'. ServerUrl: {ServerUrl}. TransactionSampleRate: {SampleRate}. Environment: {Environment}.")]
    public static partial void LogElasticApmConfigured(ILogger logger, string serviceName, string? serverUrl, string? sampleRate, string? environment);

    /// <summary>
    /// Logs that the Elastic APM Agent is disabled in settings.
    /// </summary>
    [LoggerMessage(EventId = EvtElasticApmDisabled, Level = LogLevel.Information, Message = "Elastic APM Agent is DISABLED in settings for service '{ServiceName}'.")]
    public static partial void LogElasticApmDisabled(ILogger logger, string serviceName);
    
    /// <summary>
    /// Logs that the Elastic APM middleware has been registered.
    /// </summary>
    [LoggerMessage(EventId = EvtElasticApmMiddlewareRegistered, Level = LogLevel.Information, Message = "Elastic APM middleware registered for service '{ServiceName}'.")]
    public static partial void LogElasticApmMiddlewareRegistered(ILogger logger, string serviceName);

    /// <summary>
    /// Logs the completion of the ELK-focused observability services registration.
    /// </summary>
    [LoggerMessage(EventId = EvtObservabilitySetupCompleted, Level = LogLevel.Information, Message = "{ProjectName} for '{ServiceName}' (ELK focus) registration completed.")]
    public static partial void LogObservabilitySetupCompleted(ILogger logger, string serviceName, string projectName = Logging.ProjectName);
}