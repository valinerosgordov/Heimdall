using System.Net;
using Heimdall.Domain.SharedKernel;

namespace Heimdall.Domain.Inventory;

/// <summary>A server/VPS in the infrastructure inventory: identity, specs, billing and topology links.</summary>
public sealed class Server : AggregateRoot<ServerId>
{
    private const int MaxNameLength = 200;
    private const int MaxTextLength = 500;
    private const int MaxNotesLength = 2000;

    private Server() { } // storage rehydration

    private Server(ServerId id, DateTimeOffset createdAt) : base(id) => CreatedAt = createdAt;

    public string Name { get; private set; } = string.Empty;
    public string? Provider { get; private set; }
    public string? IpAddress { get; private set; }
    public string? Hostname { get; private set; }
    public string? Role { get; private set; }
    public int? CpuCores { get; private set; }
    public double? RamGb { get; private set; }
    public double? DiskGb { get; private set; }
    public string? Location { get; private set; }
    public decimal? MonthlyCost { get; private set; }
    public string? Currency { get; private set; }
    public DateOnly? PaidUntil { get; private set; }
    public int? UserCount { get; private set; }
    public string? Notes { get; private set; }
    public Guid? LinkedHealthCheckId { get; private set; }
    public string? LinkedHostName { get; private set; }

    // Auto-discovered (reported by the agent; never typed by hand).
    public string? Os { get; private set; }
    public string? ListeningPorts { get; private set; }
    public DateTimeOffset? LastDiscoveredAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static Result<Server> Create(ServerDraft draft, DateTimeOffset now)
    {
        var server = new Server(ServerId.New(), now);
        var applied = server.Apply(draft, now);
        return applied.IsFailure ? applied.Error : server;
    }

    public Result Update(ServerDraft draft, DateTimeOffset now) => Apply(draft, now);

    /// <summary>Creates a server straight from an agent inventory report (auto-discovered host).</summary>
    public static Result<Server> CreateDiscovered(
        string hostName, string? os, int? cpuCores, double? ramGb, double? diskGb, string? listeningPorts, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(hostName))
            return Error.Validation("Server.NameEmpty", "Name must not be empty.");

        var name = WebUtility.HtmlEncode(hostName.Trim());
        if (name.Length > MaxNameLength)
            return Error.Validation("Server.NameTooLong", $"Name must be {MaxNameLength} characters or fewer.");

        var server = new Server(ServerId.New(), now) { Name = name, LinkedHostName = name };
        server.ApplyDiscovery(os, cpuCores, ramGb, diskGb, hostName, listeningPorts, now);
        return server;
    }

    /// <summary>Updates only auto-discovered fields; manual fields (cost, paid-until, notes, role…) are preserved.</summary>
    public void ApplyDiscovery(
        string? os, int? cpuCores, double? ramGb, double? diskGb, string? hostname, string? listeningPorts, DateTimeOffset now)
    {
        // Preserve last-good values when a report omits a field (transient null/empty), matching the numeric guards.
        var cleanOs = Clean(os, 200);
        if (cleanOs is not null) Os = cleanOs;
        if (cpuCores is >= 0) CpuCores = cpuCores;
        if (ramGb is >= 0) RamGb = ramGb;
        if (diskGb is >= 0) DiskGb = diskGb;
        var cleanHost = Clean(hostname);
        if (cleanHost is not null) Hostname = cleanHost;
        var cleanPorts = Clean(listeningPorts, MaxTextLength);
        if (cleanPorts is not null) ListeningPorts = cleanPorts;
        LastDiscoveredAt = now;
        UpdatedAt = now;
    }

    private Result Apply(ServerDraft d, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(d.Name))
            return Result.Failure(Error.Validation("Server.NameEmpty", "Name must not be empty."));

        var name = WebUtility.HtmlEncode(d.Name.Trim());
        if (name.Length > MaxNameLength)
            return Result.Failure(Error.Validation("Server.NameTooLong", $"Name must be {MaxNameLength} characters or fewer."));

        if (d.MonthlyCost is < 0)
            return Result.Failure(Error.Validation("Server.CostNegative", "Monthly cost cannot be negative."));
        if (d.CpuCores is < 0 || d.RamGb is < 0 || d.DiskGb is < 0 || d.UserCount is < 0)
            return Result.Failure(Error.Validation("Server.SpecNegative", "Specs and user count cannot be negative."));

        Name = name;
        Provider = Clean(d.Provider);
        IpAddress = Clean(d.IpAddress);
        Hostname = Clean(d.Hostname);
        Role = Clean(d.Role);
        CpuCores = d.CpuCores;
        RamGb = d.RamGb;
        DiskGb = d.DiskGb;
        Location = Clean(d.Location);
        MonthlyCost = d.MonthlyCost;
        Currency = Clean(d.Currency, 8);
        PaidUntil = d.PaidUntil;
        UserCount = d.UserCount;
        Notes = Clean(d.Notes, MaxNotesLength);
        LinkedHealthCheckId = d.LinkedHealthCheckId;
        LinkedHostName = Clean(d.LinkedHostName);
        UpdatedAt = now;
        return Result.Success();
    }

    private static string? Clean(string? value, int max = MaxTextLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var clean = WebUtility.HtmlEncode(value.Trim());
        return clean.Length > max ? clean[..max] : clean;
    }

    public static Server FromStorage(
        ServerId id,
        string name,
        string? provider,
        string? ipAddress,
        string? hostname,
        string? role,
        int? cpuCores,
        double? ramGb,
        double? diskGb,
        string? location,
        decimal? monthlyCost,
        string? currency,
        DateOnly? paidUntil,
        int? userCount,
        string? notes,
        Guid? linkedHealthCheckId,
        string? linkedHostName,
        string? os,
        string? listeningPorts,
        DateTimeOffset? lastDiscoveredAt,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
        => new(id, createdAt)
        {
            Name = name,
            Provider = provider,
            IpAddress = ipAddress,
            Hostname = hostname,
            Role = role,
            CpuCores = cpuCores,
            RamGb = ramGb,
            DiskGb = diskGb,
            Location = location,
            MonthlyCost = monthlyCost,
            Currency = currency,
            PaidUntil = paidUntil,
            UserCount = userCount,
            Notes = notes,
            LinkedHealthCheckId = linkedHealthCheckId,
            LinkedHostName = linkedHostName,
            Os = os,
            ListeningPorts = listeningPorts,
            LastDiscoveredAt = lastDiscoveredAt,
            UpdatedAt = updatedAt,
        };
}
