namespace BuildingBlocks.Abstractions.Idempotency;

/// <summary>
/// API seviyesinde idempotency: İstemcinin gönderdiği Idempotency-Key ile aynı istek
/// tekrar geldiğinde işlem yeniden çalıştırılmaz, ilk yanıt döndürülür (IdempotencyBehavior).
/// </summary>
public interface IIdempotentCommand
{
    Guid IdempotencyKey { get; }
}

/// <summary>
/// Consumer seviyesinde idempotency (Inbox Pattern): Aynı integration event birden fazla kez
/// teslim edilse bile (at-least-once) yalnızca bir kez işlenir (InboxBehavior).
/// </summary>
public interface IInboxCommand
{
    /// <summary>Tekilliği sağlayan mesaj kimliği (Integration Event'in EventId değeri).</summary>
    Guid MessageId { get; }

    /// <summary>Aynı mesajı farklı amaçlarla işleyen tüketicileri ayırt eder.</summary>
    string ConsumerName { get; }
}
