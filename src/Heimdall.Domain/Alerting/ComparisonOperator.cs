using System.Collections.Frozen;
using Heimdall.Domain.SharedKernel;

namespace Heimdall.Domain.Alerting;

public enum ComparisonOperator
{
    GreaterThan,
    LessThan,
    GreaterOrEqual,
    LessOrEqual,
}

public static class ComparisonOperatorParser
{
    private static readonly FrozenDictionary<string, ComparisonOperator> Map =
        new Dictionary<string, ComparisonOperator>(StringComparer.OrdinalIgnoreCase)
        {
            ["gt"] = ComparisonOperator.GreaterThan,
            [">"] = ComparisonOperator.GreaterThan,
            ["lt"] = ComparisonOperator.LessThan,
            ["<"] = ComparisonOperator.LessThan,
            ["gte"] = ComparisonOperator.GreaterOrEqual,
            [">="] = ComparisonOperator.GreaterOrEqual,
            ["lte"] = ComparisonOperator.LessOrEqual,
            ["<="] = ComparisonOperator.LessOrEqual,
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    public static Result<ComparisonOperator> Parse(string? raw)
        => !string.IsNullOrWhiteSpace(raw) && Map.TryGetValue(raw.Trim(), out var op)
            ? op
            : Error.Validation("Alert.OperatorInvalid", "Operator must be one of gt, lt, gte, lte.");

    public static string ToWire(ComparisonOperator op) => op switch
    {
        ComparisonOperator.GreaterThan => "gt",
        ComparisonOperator.LessThan => "lt",
        ComparisonOperator.GreaterOrEqual => "gte",
        ComparisonOperator.LessOrEqual => "lte",
        _ => "gt",
    };
}
