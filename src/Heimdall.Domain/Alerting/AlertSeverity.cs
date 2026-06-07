using System.Collections.Frozen;
using Heimdall.Domain.SharedKernel;

namespace Heimdall.Domain.Alerting;

public enum AlertSeverity
{
    Warning,
    Critical,
}

public static class AlertSeverityParser
{
    private static readonly FrozenSet<string> Valid =
        Enum.GetNames<AlertSeverity>().ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static Result<AlertSeverity> Parse(string? raw)
        => !string.IsNullOrWhiteSpace(raw) && Valid.Contains(raw.Trim()) && Enum.TryParse<AlertSeverity>(raw.Trim(), ignoreCase: true, out var severity)
            ? severity
            : Error.Validation("Alert.SeverityInvalid", "Severity must be 'warning' or 'critical'.");
}
