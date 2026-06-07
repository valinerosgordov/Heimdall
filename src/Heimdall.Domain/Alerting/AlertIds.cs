namespace Heimdall.Domain.Alerting;

public readonly record struct AlertRuleId(Guid Value)
{
    public static AlertRuleId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}

public readonly record struct AlertId(Guid Value)
{
    public static AlertId New() => new(Guid.CreateVersion7());
    public override string ToString() => Value.ToString();
}
