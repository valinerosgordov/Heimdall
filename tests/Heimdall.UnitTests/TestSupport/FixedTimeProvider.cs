namespace Heimdall.UnitTests.TestSupport;

/// <summary>Minimal deterministic TimeProvider for handler tests.</summary>
internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
