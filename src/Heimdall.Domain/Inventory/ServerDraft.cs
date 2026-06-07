namespace Heimdall.Domain.Inventory;

/// <summary>Mutable attributes of a server, used to create or update one. Keeps the Domain free of wire contracts.</summary>
public sealed record ServerDraft(
    string? Name,
    string? Provider,
    string? IpAddress,
    string? Hostname,
    string? Role,
    int? CpuCores,
    double? RamGb,
    double? DiskGb,
    string? Location,
    decimal? MonthlyCost,
    string? Currency,
    DateOnly? PaidUntil,
    int? UserCount,
    string? Notes,
    Guid? LinkedHealthCheckId,
    string? LinkedHostName);
