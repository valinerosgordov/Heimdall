using System.Net;
using Heimdall.Domain.SharedKernel;

namespace Heimdall.Domain.Inventory;

/// <summary>A directed relationship between two servers (e.g. a proxy fronting an app server).</summary>
public sealed class ServerLink
{
    private const int MaxKindLength = 60;
    private const string DefaultKind = "depends-on";

    private ServerLink(Guid id, ServerId from, ServerId to, string kind)
    {
        Id = id;
        From = from;
        To = to;
        Kind = kind;
    }

    public Guid Id { get; }
    public ServerId From { get; }
    public ServerId To { get; }

    /// <summary>Relationship label, e.g. "proxy", "database", "depends-on".</summary>
    public string Kind { get; }

    public static Result<ServerLink> Create(Guid from, Guid to, string? kind)
    {
        if (from == Guid.Empty || to == Guid.Empty)
            return Error.Validation("ServerLink.EndpointEmpty", "Both endpoints are required.");
        if (from == to)
            return Error.Validation("ServerLink.SelfLink", "A server cannot link to itself.");

        var clean = string.IsNullOrWhiteSpace(kind) ? DefaultKind : WebUtility.HtmlEncode(kind.Trim());
        if (clean.Length > MaxKindLength)
            clean = clean[..MaxKindLength];

        return new ServerLink(Guid.CreateVersion7(), new ServerId(from), new ServerId(to), clean);
    }

    public static ServerLink FromStorage(Guid id, Guid from, Guid to, string kind)
        => new(id, new ServerId(from), new ServerId(to), kind);
}
