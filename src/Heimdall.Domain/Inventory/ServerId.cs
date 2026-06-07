namespace Heimdall.Domain.Inventory;

/// <summary>Strongly-typed identity for an inventory server (UUIDv7, time-ordered).</summary>
public readonly record struct ServerId(Guid Value)
{
    public static ServerId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
