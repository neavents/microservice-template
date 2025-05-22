using Microsoft.Extensions.Logging;

namespace TemporaryName.WebApi.Configurators;

public static partial class OpenTelemetryConfigurator
{
    private const int ClassId = 6;
    private const int BaseEventId = Logging.MassTransitBaseId + (ClassId * Logging.IncrementPerClass);
    private const int EvtMethodCalled = BaseEventId + 0;
    private const int EvtOpenTelemetryOptionsMissing = BaseEventId + 1;
    private const int EvtConfiguringOpenTelemetry = BaseEventId + 2;
    private const int EvtOpenTelemetrySourcesAdded = BaseEventId + 3;
    private const int EvtOpenTelemetrySuccessfullyConfigured = BaseEventId + 4;
    private const int EvtOpenTelemetryDisabledByConfiguration = BaseEventId + 5;


    [LoggerMessage(EventId = EvtMethodCalled, Level = LogLevel.Debug, Message = "OpenTelemetryConfigurator: Method called: {MethodName}.")]
    private static partial void LogMethodCalled(ILogger logger, string methodName);

    [LoggerMessage(EventId = EvtOpenTelemetryOptionsMissing, Level = LogLevel.Warning, Message = "OpenTelemetryConfigurator: MassTransitOptions section '{ConfigSectionName}' not found or is null. OpenTelemetry configuration might be incomplete.")]
    public static partial void LogOpenTelemetryOptionsMissing(ILogger logger, string configSectionName);

    [LoggerMessage(EventId = EvtConfiguringOpenTelemetry, Level = LogLevel.Information, Message = "OpenTelemetryConfigurator: Configuring OpenTelemetry for MassTransit. ServiceName: '{ServiceName}'.")]
    public static partial void LogConfiguringOpenTelemetry(ILogger logger, string serviceName);

    [LoggerMessage(EventId = EvtOpenTelemetrySourcesAdded, Level = LogLevel.Debug, Message = "OpenTelemetryConfigurator: Added MassTransit diagnostic sources for '{ServiceName}'. MainSource: {MainSource}, RabbitMQ: MassTransit.Transport.RabbitMQ, Kafka: MassTransit.Transport.Kafka.")]
    public static partial void LogOpenTelemetrySourcesAdded(ILogger logger, string serviceName, string mainSource);

    [LoggerMessage(EventId = EvtOpenTelemetrySuccessfullyConfigured, Level = LogLevel.Information, Message = "OpenTelemetryConfigurator: OpenTelemetry tracing for MassTransit successfully configured.")]
    public static partial void LogOpenTelemetrySuccessfullyConfigured(ILogger logger);

    [LoggerMessage(EventId = EvtOpenTelemetryDisabledByConfiguration, Level = LogLevel.Information, Message = "OpenTelemetryConfigurator: OpenTelemetry for MassTransit is disabled by configuration (EnableOpenTelemetry=false).")]
    public static partial void LogOpenTelemetryDisabledByConfiguration(ILogger logger);
}