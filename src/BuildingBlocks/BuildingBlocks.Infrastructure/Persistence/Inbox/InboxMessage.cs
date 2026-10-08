namespace BuildingBlocks.Infrastructure.Persistence.Inbox;

/// <summary>İşlenmiş integration event kaydı. (MessageId, Consumer) birleşik birincil anahtardır.</summary>
public sealed class InboxMessage
{
    public Guid MessageId { get; init; }
    public string Consumer { get; init; } = default!;
    public DateTime ProcessedOnUtc { get; init; }
}
