using System.Net;
using Heimdall.Domain.SharedKernel;

namespace Heimdall.Domain.HealthChecks;

/// <summary>An external endpoint Heimdall probes on a schedule (HTTP URL or TCP host:port).</summary>
public sealed class HealthCheckTarget : AggregateRoot<HealthCheckTargetId>
{
    private const int MaxNameLength = 200;
    private const int MinIntervalSeconds = 5;
    private const int MaxIntervalSeconds = 3600;

    private HealthCheckTarget() { } // storage rehydration

    private HealthCheckTarget(
        HealthCheckTargetId id,
        string name,
        ProbeKind kind,
        string target,
        int intervalSeconds,
        bool enabled,
        DateTimeOffset createdAt) : base(id)
    {
        Name = name;
        Kind = kind;
        Target = target;
        IntervalSeconds = intervalSeconds;
        Enabled = enabled;
        CreatedAt = createdAt;
    }

    public string Name { get; private set; } = string.Empty;
    public ProbeKind Kind { get; private set; }
    public string Target { get; private set; } = string.Empty;
    public int IntervalSeconds { get; private set; }
    public bool Enabled { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastCheckedAt { get; private set; }

    public static Result<HealthCheckTarget> Create(
        string? name,
        string? kindRaw,
        string? target,
        int intervalSeconds,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation("HealthCheck.NameEmpty", "Name must not be empty.");

        var cleanName = WebUtility.HtmlEncode(name.Trim());
        if (cleanName.Length > MaxNameLength)
            return Error.Validation("HealthCheck.NameTooLong", $"Name must be {MaxNameLength} characters or fewer.");

        var kind = ProbeKindParser.Parse(kindRaw);
        if (kind.IsFailure)
            return kind.Error;

        var target_ = ValidateTarget(kind.Value, target);
        if (target_.IsFailure)
            return target_.Error;

        var interval = Math.Clamp(intervalSeconds, MinIntervalSeconds, MaxIntervalSeconds);
        return new HealthCheckTarget(HealthCheckTargetId.New(), cleanName, kind.Value, target_.Value, interval, enabled: true, now);
    }

    public void MarkChecked(DateTimeOffset now) => LastCheckedAt = now;

    public bool IsDue(DateTimeOffset now)
        => LastCheckedAt is null || now - LastCheckedAt.Value >= TimeSpan.FromSeconds(IntervalSeconds);

    private static Result<string> ValidateTarget(ProbeKind kind, string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
            return Error.Validation("HealthCheck.TargetEmpty", "Target must not be empty.");

        var value = target.Trim();
        switch (kind)
        {
            case ProbeKind.Http:
                if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
                    || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                    return Error.Validation("HealthCheck.TargetInvalid", "Http target must be an absolute http(s) URL.");
                return uri.ToString();

            case ProbeKind.Tcp:
                var parts = value.Split(':');
                if (parts.Length != 2
                    || string.IsNullOrWhiteSpace(parts[0])
                    || !int.TryParse(parts[1], out var port)
                    || port is < 1 or > 65535)
                    return Error.Validation("HealthCheck.TargetInvalid", "Tcp target must be 'host:port'.");
                return value;

            default:
                return Error.Validation("HealthCheck.KindInvalid", "Unsupported probe kind.");
        }
    }

    public static HealthCheckTarget FromStorage(
        HealthCheckTargetId id,
        string name,
        ProbeKind kind,
        string target,
        int intervalSeconds,
        bool enabled,
        DateTimeOffset createdAt,
        DateTimeOffset? lastCheckedAt)
        => new(id, name, kind, target, intervalSeconds, enabled, createdAt) { LastCheckedAt = lastCheckedAt };
}
