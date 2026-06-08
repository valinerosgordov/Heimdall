using System.Globalization;
using Heimdall.Contracts;
using Heimdall.Domain.Inventory;
using Heimdall.Domain.SharedKernel;

namespace Heimdall.Application.Inventory;

internal static class ServerMappings
{
    /// <summary>Parses an optional "yyyy-MM-dd" payment date. Empty -> null; malformed -> validation error.</summary>
    public static Result<DateOnly?> ParseDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return (DateOnly?)null;

        return DateOnly.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? (DateOnly?)date
            : Error.Validation("Server.DateInvalid", "PaidUntil must be a date in yyyy-MM-dd format.");
    }

    public static ServerDraft ToDraft(CreateServerRequest r, DateOnly? paidUntil) => new(
        r.Name, r.Provider, r.IpAddress, r.Hostname, r.Role,
        r.CpuCores, r.RamGb, r.DiskGb, r.Location,
        r.MonthlyCost, r.Currency, paidUntil, r.UserCount, r.Notes,
        r.LinkedHealthCheckId, r.LinkedHostName,
        r.BillingCycle, r.AutoRenew, r.PaymentMethod);

    public static ServerRecord ToRecord(Server s) => new(
        s.Id.Value, s.Name, s.Provider, s.IpAddress, s.Hostname, s.Role,
        s.CpuCores, s.RamGb, s.DiskGb, s.Location,
        s.MonthlyCost, s.Currency, s.PaidUntil, s.UserCount, s.Notes,
        s.LinkedHealthCheckId, s.LinkedHostName,
        s.Os, s.ListeningPorts, s.LastDiscoveredAt,
        IsUp: null,
        BillingCycle: s.BillingCycle, AutoRenew: s.AutoRenew, PaymentMethod: s.PaymentMethod);

    public static ServerDto ToDto(ServerRecord r, DateOnly today)
    {
        int? daysUntilDue = r.PaidUntil is { } due ? due.DayNumber - today.DayNumber : null;
        return new ServerDto
        {
            Id = r.Id,
            Name = r.Name,
            Provider = r.Provider,
            IpAddress = r.IpAddress,
            Hostname = r.Hostname,
            Role = r.Role,
            CpuCores = r.CpuCores,
            RamGb = r.RamGb,
            DiskGb = r.DiskGb,
            Location = r.Location,
            MonthlyCost = r.MonthlyCost,
            Currency = r.Currency,
            PaidUntil = r.PaidUntil?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DaysUntilDue = daysUntilDue,
            UserCount = r.UserCount,
            Notes = r.Notes,
            LinkedHealthCheckId = r.LinkedHealthCheckId,
            LinkedHostName = r.LinkedHostName,
            BillingCycle = r.BillingCycle,
            AutoRenew = r.AutoRenew,
            PaymentMethod = r.PaymentMethod,
            IsUp = r.IsUp,
            Os = r.Os,
            ListeningPorts = r.ListeningPorts,
            LastDiscoveredAtUnixMs = r.LastDiscoveredAt?.ToUnixTimeMilliseconds(),
        };
    }
}
