using System.Buffers;
using Heimdall.Domain.SharedKernel;

namespace Heimdall.Domain.Metrics;

/// <summary>A validated metric identifier, e.g. <c>cpu.usage</c>. Lowercase dotted namespace style.</summary>
public sealed record MetricName
{
    private const int MaxLength = 100;

    // Allowed: letters, digits, dot, underscore. Used to reject injection / weird labels cheaply.
    private static readonly SearchValues<char> Allowed =
        SearchValues.Create("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789._");

    private MetricName(string value) => Value = value;

    public string Value { get; }

    public static Result<MetricName> Create(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Error.Validation("Metric.NameEmpty", "Metric name must not be empty.");

        var value = raw.Trim();
        if (value.Length > MaxLength)
            return Error.Validation("Metric.NameTooLong", $"Metric name must be {MaxLength} characters or fewer.");

        if (value.AsSpan().ContainsAnyExcept(Allowed))
            return Error.Validation("Metric.NameInvalid", "Metric name may contain only letters, digits, '.' and '_'.");

        return new MetricName(value);
    }

    public override string ToString() => Value;
}
