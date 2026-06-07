namespace Heimdall.Domain.SharedKernel;

/// <summary>Marker for domain events raised by aggregates.</summary>
public interface IDomainEvent;

/// <summary>Entity with identity-based equality.</summary>
public abstract class Entity<TId> where TId : notnull
{
    protected Entity(TId id) => Id = id;

    // Parameterless ctor for storage rehydration in derived types.
    protected Entity() => Id = default!;

    public TId Id { get; protected init; }

    public override bool Equals(object? obj)
        => obj is Entity<TId> other
           && other.GetType() == GetType()
           && EqualityComparer<TId>.Default.Equals(Id, other.Id);

    public override int GetHashCode() => EqualityComparer<TId>.Default.GetHashCode(Id);
}

/// <summary>Aggregate root — the consistency boundary that records domain events.</summary>
public abstract class AggregateRoot<TId> : Entity<TId> where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(TId id) : base(id) { }
    protected AggregateRoot() { }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
