using System.Data;
using Dapper;

namespace Heimdall.Infrastructure.Persistence;

/// <summary>
/// Dapper 2.x does not natively bind <see cref="DateOnly"/> parameters; this maps them to PostgreSQL
/// <c>date</c> (Npgsql infers the type) and reads them back from either DateOnly or DateTime.
/// </summary>
internal sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override void SetValue(IDbDataParameter parameter, DateOnly value) => parameter.Value = value;

    public override DateOnly Parse(object value) => value switch
    {
        DateOnly dateOnly => dateOnly,
        DateTime dateTime => DateOnly.FromDateTime(dateTime),
        _ => throw new InvalidCastException($"Cannot convert '{value?.GetType().Name ?? "null"}' to DateOnly."),
    };
}
