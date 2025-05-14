namespace TemporaryName.Infrastructure.Outbox.EFCore;

public class OutboxOptions
{
    public const string SectionName = "Infrastructure:Outbox"; 
    public bool Enabled { get; set; } = true;
    public bool CleanupEnabled { get; set; } = true;
    public int DeleteProcessedMessagesOlderThanDays { get; set; } = 30;
    public int CleanupBatchSize { get; set; } = 100;
}