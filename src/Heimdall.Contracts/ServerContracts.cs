namespace Heimdall.Contracts;

/// <summary>Create or update a server in the inventory. All fields except Name are optional.</summary>
public sealed record CreateServerRequest
{
    public required string Name { get; init; }
    public string? Provider { get; init; }
    public string? IpAddress { get; init; }
    public string? Hostname { get; init; }

    /// <summary>What the server is for, e.g. "Prod site + API".</summary>
    public string? Role { get; init; }

    public int? CpuCores { get; init; }
    public double? RamGb { get; init; }
    public double? DiskGb { get; init; }
    public string? Location { get; init; }

    /// <summary>Recurring cost amount (per billing period).</summary>
    public decimal? MonthlyCost { get; init; }

    /// <summary>ISO 4217-ish currency label, e.g. "RUB", "USD", "EUR".</summary>
    public string? Currency { get; init; }

    /// <summary>Next payment due date, "yyyy-MM-dd".</summary>
    public string? PaidUntil { get; init; }

    /// <summary>Number of users/tenants the server serves.</summary>
    public int? UserCount { get; init; }
    public string? Notes { get; init; }

    /// <summary>Optional health-check whose latest result colours this server's status.</summary>
    public Guid? LinkedHealthCheckId { get; init; }

    /// <summary>Optional agent host name whose metrics belong to this server.</summary>
    public string? LinkedHostName { get; init; }
}

/// <summary>A server inventory record, with billing countdown and linked liveness.</summary>
public sealed record ServerDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Provider { get; init; }
    public string? IpAddress { get; init; }
    public string? Hostname { get; init; }
    public string? Role { get; init; }
    public int? CpuCores { get; init; }
    public double? RamGb { get; init; }
    public double? DiskGb { get; init; }
    public string? Location { get; init; }
    public decimal? MonthlyCost { get; init; }
    public string? Currency { get; init; }
    public string? PaidUntil { get; init; }

    /// <summary>Days until <see cref="PaidUntil"/> (negative = overdue). Null when no date is set.</summary>
    public int? DaysUntilDue { get; init; }

    public int? UserCount { get; init; }
    public string? Notes { get; init; }
    public Guid? LinkedHealthCheckId { get; init; }
    public string? LinkedHostName { get; init; }

    /// <summary>Latest liveness of the linked health-check, if any.</summary>
    public bool? IsUp { get; init; }
}

/// <summary>A directed link between two servers (topology edge).</summary>
public sealed record ServerLinkDto
{
    public required Guid Id { get; init; }
    public required Guid FromServerId { get; init; }
    public required Guid ToServerId { get; init; }
    public required string Kind { get; init; }
}

/// <summary>Create a topology link between two servers.</summary>
public sealed record CreateServerLinkRequest
{
    public required Guid FromServerId { get; init; }
    public required Guid ToServerId { get; init; }
    public string? Kind { get; init; }
}

/// <summary>The whole inventory: servers plus the links between them.</summary>
public sealed record InventoryResponse
{
    public required IReadOnlyList<ServerDto> Servers { get; init; }
    public required IReadOnlyList<ServerLinkDto> Links { get; init; }
}
