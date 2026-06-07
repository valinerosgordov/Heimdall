namespace Heimdall.Application.Abstractions;

/// <summary>The single operator account's username and hashed password.</summary>
public readonly record struct OperatorCredentials(string Username, string PasswordSha256);

/// <summary>Persists the single operator account created during first-run setup.</summary>
public interface IOperatorStore
{
    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken);

    Task<OperatorCredentials?> GetAsync(CancellationToken cancellationToken);

    Task SetAsync(string username, string passwordSha256, CancellationToken cancellationToken);
}
