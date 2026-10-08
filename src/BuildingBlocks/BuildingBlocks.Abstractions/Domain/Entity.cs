namespace BuildingBlocks.Abstractions.Domain;

/// <summary>
/// Tüm entity'lerin temel sınıfı. Eşitlik, nesne referansı yerine kimlik (Id) üzerinden sağlanır.
/// </summary>
public abstract class Entity<TId> : IEquatable<Entity<TId>>
    where TId : notnull
{
    protected Entity()
    {
    }

    protected Entity(TId id) => Id = id;

    public TId Id { get; protected set; } = default!;

    public bool Equals(Entity<TId>? other) =>
        other is not null &&
        GetType() == other.GetType() &&
        EqualityComparer<TId>.Default.Equals(Id, other.Id);

    public override bool Equals(object? obj) => obj is Entity<TId> entity && Equals(entity);

    public override int GetHashCode() => EqualityComparer<TId>.Default.GetHashCode(Id);
}
