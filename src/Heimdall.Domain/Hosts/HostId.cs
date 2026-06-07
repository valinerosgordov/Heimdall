namespace Heimdall.Domain.Hosts;

/// <summary>Strongly-typed identity for a monitored host. UUIDv7 = time-ordered, index-friendly.</summary>
public readonly record struct HostId(Guid Value)
{
    public static HostId New() => new(Guid.CreateVersion7());

    public override string ToString() => Value.ToString();
}
