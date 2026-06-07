namespace Heimdall.Application.Inventory;

/// <summary>Read model: a stored server plus the latest liveness of its linked health-check.</summary>
public readonly record struct ServerRecord(
    Guid Id,
    string Name,
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
    string? LinkedHostName,
    bool? IsUp);
