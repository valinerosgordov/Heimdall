using System.Collections.Frozen;
using Heimdall.Domain.SharedKernel;

namespace Heimdall.Domain.HealthChecks;

/// <summary>How a target is probed.</summary>
public enum ProbeKind
{
    Http,
    Tcp,
}

public static class ProbeKindParser
{
    private static readonly FrozenSet<string> Valid =
        Enum.GetNames<ProbeKind>().ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    public static Result<ProbeKind> Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || !Valid.Contains(raw.Trim()) || !Enum.TryParse<ProbeKind>(raw.Trim(), ignoreCase: true, out var kind))
            return Error.Validation("HealthCheck.KindInvalid", "Kind must be 'Http' or 'Tcp'.");

        return kind;
    }
}
