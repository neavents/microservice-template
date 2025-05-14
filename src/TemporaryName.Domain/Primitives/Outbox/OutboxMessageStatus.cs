namespace TemporaryName.Domain.Primitives.Outbox;

public enum OutboxMessageStatus
{
    Pending = 0,
    Published = 1,
    ProcessedByConsumer = 2,
    FailedToPublish = 3,
    Requeued = 4
}
