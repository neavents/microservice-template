namespace TemporaryName.Domain.Primitives.Outbox;

public enum OutboxMessageStatus
{
    Pending = 0,
    Published = 1,
    ProcessedByConsumer = 2,
    Processing = 3,
    FailedToPublish = 4,
    Requeued = 5
}
