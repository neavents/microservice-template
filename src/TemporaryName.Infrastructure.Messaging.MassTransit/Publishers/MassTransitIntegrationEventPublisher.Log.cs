using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using TemporaryName.Infrastructure.Messaging.MassTransit.Exceptions;

namespace TemporaryName.Infrastructure.Messaging.MassTransit.Publishers;

public partial class MassTransitIntegrationEventPublisher
{
    private const int ClassId = 0;
    private const int BaseEventId = Logging.MassTransitBaseId + (ClassId * Logging.IncrementPerClass);
    private const int EvtMappingScanStarted = BaseEventId + (0 * Logging.IncrementPerLog);
    private const int EvtNoAssembliesFoundForScan = BaseEventId + (1 * Logging.IncrementPerLog);
    private const int EvtFoundAssembliesForScan = BaseEventId + (2 * Logging.IncrementPerLog);
    private const int EvtPocoEventTypeDiscovered = BaseEventId + (3 * Logging.IncrementPerLog);
    private const int EvtPocoEventTypeDiscoveredWithNullFullName = BaseEventId + (4 * Logging.IncrementPerLog);
    private const int EvtInvalidMapperDomainType = BaseEventId + (5 * Logging.IncrementPerLog);
    private const int EvtInvalidMapperIntegrationType = BaseEventId + (6 * Logging.IncrementPerLog);
    private const int EvtEventMapperDelegateRegistered = BaseEventId + (7 * Logging.IncrementPerLog);
    private const int EvtMappingScanCompleted = BaseEventId + (8 * Logging.IncrementPerLog);
    private const int EvtTypeLoadErrorDuringScan = BaseEventId + (9 * Logging.IncrementPerLog);
    private const int EvtIndividualLoaderExceptionDetail = BaseEventId + (10 * Logging.IncrementPerLog);
    private const int EvtGenericAssemblyScanError = BaseEventId + (11 * Logging.IncrementPerLog);
    private const int EvtPublishAttempt = BaseEventId + (12 * Logging.IncrementPerLog);
    private const int EvtUnknownPocoEventTypeForPublish = BaseEventId + (13 * Logging.IncrementPerLog);
    private const int EvtDeserializationResultedInNullOrWrongType = BaseEventId + (14 * Logging.IncrementPerLog);
    private const int EvtDeserializationFailedDuringPublish = BaseEventId + (15 * Logging.IncrementPerLog);
    private const int EvtPocoEventDeserializedForPublish = BaseEventId + (16 * Logging.IncrementPerLog);
    private const int EvtProtobufMapperDelegateNotFoundForPublish = BaseEventId + (17 * Logging.IncrementPerLog);
    private const int EvtProtobufEventMappedSuccessfully = BaseEventId + (18 * Logging.IncrementPerLog);
    private const int EvtEventMappingExecutionFailedDuringPublish = BaseEventId + (19 * Logging.IncrementPerLog);
    private const int EvtPublishingToMassTransitEndpoint = BaseEventId + (20 * Logging.IncrementPerLog);
    private const int EvtMassTransitSendContextConfiguredForPublish = BaseEventId + (21 * Logging.IncrementPerLog);
    private const int EvtPublishSucceededToMassTransit = BaseEventId + (22 * Logging.IncrementPerLog);
    private const int EvtPublishFailedToMassTransit = BaseEventId + (23 * Logging.IncrementPerLog);
    private const int EvtEventMapperNotResolvedFromDI = BaseEventId + (24 * Logging.IncrementPerLog);
    private const int EvtEventMapMethodNotFoundOnMapper = BaseEventId + (25 * Logging.IncrementPerLog);
    private const int EvtEventMappingExecutionReturnedNull = BaseEventId + (26 * Logging.IncrementPerLog);
    private const int EvtEventMappingExecutionThrewException = BaseEventId + (27 * Logging.IncrementPerLog);


    [LoggerMessage(EventId = EvtMappingScanStarted, Level = LogLevel.Information, Message = "Publisher: Starting scan for IDomainEvent POCOs and IEventMapper implementations.")]
    public static partial void LogMappingScanStarted(ILogger logger);

    [LoggerMessage(EventId = EvtNoAssembliesFoundForScan, Level = LogLevel.Warning, Message = "Publisher: No assemblies found for scanning based on {ScanSource}. Event mapping might be incomplete.")]
    public static partial void LogNoAssembliesFoundForScan(ILogger logger, string scanSource);

    [LoggerMessage(EventId = EvtFoundAssembliesForScan, Level = LogLevel.Debug, Message = "Publisher: Assemblies targeted for scanning: {AssemblyNames}.")]
    public static partial void LogFoundAssembliesForScan(ILogger logger, string assemblyNames);

    [LoggerMessage(EventId = EvtPocoEventTypeDiscovered, Level = LogLevel.Debug, Message = "Publisher: Discovered POCO Domain Event type: {PocoTypeFullName}.")]
    public static partial void LogPocoEventTypeDiscovered(ILogger logger, string pocoTypeFullName);

    [LoggerMessage(EventId = EvtPocoEventTypeDiscoveredWithNullFullName, Level = LogLevel.Warning, Message = "Publisher: Discovered POCO Domain Event type '{PocoTypeName}' but its FullName is null. It cannot be mapped by FQN string.")]
    public static partial void LogPocoEventTypeDiscoveredWithNullFullName(ILogger logger, string pocoTypeName);

    [LoggerMessage(EventId = EvtInvalidMapperDomainType, Level = LogLevel.Warning, Message = "Publisher: Mapper '{MapperType}' has an invalid domain event type '{DomainType}' (does not implement IDomainEvent). Skipping mapper.")]
    public static partial void LogInvalidMapperDomainType(ILogger logger, string mapperType, string domainType);

    [LoggerMessage(EventId = EvtInvalidMapperIntegrationType, Level = LogLevel.Warning, Message = "Publisher: Mapper '{MapperType}' has an invalid integration event type '{IntegrationType}' (does not implement IMessage). Skipping mapper.")]
    public static partial void LogInvalidMapperIntegrationType(ILogger logger, string mapperType, string integrationType);

    [LoggerMessage(EventId = EvtEventMapperDelegateRegistered, Level = LogLevel.Information, Message = "Publisher: Registered mapper delegate for '{MapperType}' (Domain: '{DomainEventType}', Integration: '{IntegrationEventType}').")]
    public static partial void LogEventMapperDelegateRegistered(ILogger logger, string mapperType, string domainEventType, string integrationEventType);

    [LoggerMessage(EventId = EvtMappingScanCompleted, Level = LogLevel.Information, Message = "Publisher: Event type and mapper scanning completed. POCO Event Types: {PocoCount}, Mapper Delegates: {MapperCount}.")]
    public static partial void LogMappingScanCompleted(ILogger logger, int pocoCount, int mapperCount);

    [LoggerMessage(EventId = EvtTypeLoadErrorDuringScan, Level = LogLevel.Warning, Message = "Publisher: Could not load types from assembly {AssemblyName} during scan. Some mappers or events might be missed.")]
    public static partial void LogTypeLoadErrorDuringScan(ILogger logger, string assemblyName, Exception ex);

    [LoggerMessage(EventId = EvtIndividualLoaderExceptionDetail, Level = LogLevel.Debug, Message = "Publisher: Assembly {AssemblyName}, LoaderException Type: {ExceptionType}, Message: {ExceptionMessage}.")]
    public static partial void LogIndividualLoaderExceptionDetail(ILogger logger, string assemblyName, string exceptionType, string exceptionMessage, Exception ex);

    [LoggerMessage(EventId = EvtGenericAssemblyScanError, Level = LogLevel.Error, Message = "Publisher: Generic error getting types from assembly {AssemblyName} during scan.")]
    public static partial void LogGenericAssemblyScanError(ILogger logger, string assemblyName, Exception ex);

    [LoggerMessage(EventId = EvtPublishAttempt, Level = LogLevel.Debug, Message = "Publisher: Attempting to publish event. TypeNameFQN: '{EventTypeNameFqn}', PayloadLength: {PayloadLength}, HeadersCount: {HeadersCount}.")]
    public static partial void LogPublishAttempt(ILogger logger, string eventTypeNameFqn, int payloadLength, int headersCount);

    [LoggerMessage(EventId = EvtUnknownPocoEventTypeForPublish, Level = LogLevel.Error, Message = "Publisher: Unknown POCO domain event type FQN '{EventTypeFqn}' provided for publishing. Event not discovered at startup.")]
    public static partial void LogUnknownPocoEventTypeForPublish(ILogger logger, string eventTypeFqn);

    [LoggerMessage(EventId = EvtDeserializationResultedInNullOrWrongType, Level = LogLevel.Error, Message = "Publisher: JSON payload for POCO EventTypeFQN '{EventTypeFqn}' (mapped to .NET type '{PocoNetType}') deserialized to null or an incompatible type '{ActualType}'. Expected IDomainEvent.")]
    public static partial void LogDeserializationResultedInNullOrWrongType(ILogger logger, string eventTypeFqn, string pocoNetType, string? actualType);

    [LoggerMessage(EventId = EvtDeserializationFailedDuringPublish, Level = LogLevel.Error, Message = "Publisher: Failed to deserialize JSON payload for POCO EventTypeFQN '{EventTypeFqn}' to .NET type '{PocoNetType}'. Payload snapshot: '{PayloadSnippet}'.")]
    public static partial void LogDeserializationFailedDuringPublish(ILogger logger, string eventTypeFqn, string pocoNetType, string payloadSnippet, JsonException ex);

    [LoggerMessage(EventId = EvtPocoEventDeserializedForPublish, Level = LogLevel.Debug, Message = "Publisher: POCO Domain Event of type '{PocoNetType}' deserialized successfully for publishing. EventId: {EventId}, OccurredOnUtc: {OccurredOnUtc}.")]
    public static partial void LogPocoEventDeserializedForPublish(ILogger logger, string pocoNetType, Guid eventId, DateTime occurredOnUtc);

    [LoggerMessage(EventId = EvtProtobufMapperDelegateNotFoundForPublish, Level = LogLevel.Critical, Message = "Publisher: Protobuf mapper delegate not found for POCO domain event type '{PocoNetType}'. Cannot publish. This is a configuration error.")]
    public static partial void LogProtobufMapperDelegateNotFoundForPublish(ILogger logger, string pocoNetType, MessagingConfigurationException ex);

    [LoggerMessage(EventId = EvtProtobufEventMappedSuccessfully, Level = LogLevel.Debug, Message = "Publisher: POCO Domain Event '{PocoNetType}' mapped to Protobuf Integration Event '{ProtoNetType}'.")]
    public static partial void LogProtobufEventMappedSuccessfully(ILogger logger, string pocoNetType, string protoNetType);

    [LoggerMessage(EventId = EvtEventMappingExecutionFailedDuringPublish, Level = LogLevel.Error, Message = "Publisher: Error executing Protobuf mapper for POCO event type '{PocoNetType}'. See inner exception.")]
    public static partial void LogEventMappingExecutionFailedDuringPublish(ILogger logger, string pocoNetType, Exception ex);

    [LoggerMessage(EventId = EvtPublishingToMassTransitEndpoint, Level = LogLevel.Information, Message = "Publisher: Publishing Protobuf integration event type '{ProtoNetType}' (OriginalEventId: {OriginalEventId}) to MassTransit.")]
    public static partial void LogPublishingToMassTransitEndpoint(ILogger logger, string protoNetType, Guid originalEventId);

    [LoggerMessage(EventId = EvtMassTransitSendContextConfiguredForPublish, Level = LogLevel.Trace, Message = "Publisher: MassTransit SendContext configured for publish. MessageId: {MessageId}, Type: '{MessageType}', CorrelationId: {CorrelationId}.")]
    public static partial void LogMassTransitSendContextConfiguredForPublish(ILogger logger, Guid? messageId, string messageType, Guid? correlationId);

    [LoggerMessage(EventId = EvtPublishSucceededToMassTransit, Level = LogLevel.Information, Message = "Publisher: Protobuf Integration Event '{ProtoNetType}' (OriginalEventId: {OriginalEventId}) published successfully via {PublishEndpointType}.")]
    public static partial void LogPublishSucceededToMassTransit(ILogger logger, string protoNetType, Guid originalEventId, string publishEndpointType);

    [LoggerMessage(EventId = EvtPublishFailedToMassTransit, Level = LogLevel.Error, Message = "Publisher: Failed to publish Protobuf Integration Event '{ProtoNetType}' (OriginalEventId: {OriginalEventId}) via MassTransit.")]
    public static partial void LogPublishFailedToMassTransit(ILogger logger, string protoNetType, Guid originalEventId, Exception ex);


    [LoggerMessage(EventId = EvtEventMapperNotResolvedFromDI, Level = LogLevel.Critical, Message = "Publisher: Event mapper '{MapperType}' for POCO event '{PocoEventType}' could not be resolved from IServiceProvider during mapping. Ensure it's registered in DI.")]
    public static partial void LogEventMapperNotResolvedFromDI(ILogger logger, string mapperType, string pocoEventType,MessagingConfigurationException ex);

    [LoggerMessage(EventId = EvtEventMapMethodNotFoundOnMapper, Level = LogLevel.Critical, Message = "Publisher: The 'Map' method was not found on the resolved event mapper '{MapperType}' for POCO event '{PocoEventType}'. This is a critical implementation error.")]
    public static partial void LogEventMapMethodNotFoundOnMapper(ILogger logger, string mapperType, string pocoEventType, MissingMethodException ex);

    [LoggerMessage(EventId = EvtEventMappingExecutionReturnedNull, Level = LogLevel.Error, Message = "Publisher: The 'Map' method on mapper '{MapperType}' for POCO event '{PocoEventType}' returned null. Mapped Protobuf event cannot be null.")]
    public static partial void LogEventMappingExecutionReturnedNull(ILogger logger, string mapperType, string pocoEventType, InvalidOperationException ex);

    [LoggerMessage(EventId = EvtEventMappingExecutionThrewException, Level = LogLevel.Error, Message = "Publisher: Mapper '{MapperType}' for POCO event '{PocoEventType}' threw an exception during 'Map' execution.")]
    public static partial void LogEventMappingExecutionThrewException(ILogger logger, string mapperType, string pocoEventType, Exception ex);

}