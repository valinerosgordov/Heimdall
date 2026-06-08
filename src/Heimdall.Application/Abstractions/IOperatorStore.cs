namespace Heimdall.Application.Abstractions;

/// <summary>The single operator account's username and hashed (PBKDF2) password.</summary>
public readonly record struct OperatorCredentials(string Username, string PasswordHash);

/// <summary>Persists the single operator account created during first-run setup.</summary>
public interface IOperatorStore
{
    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken);

    Task<OperatorCredentials?> GetAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Atomically creates the operator only if none exists yet. Returns false (and writes nothing) when an
    /// operator already exists — closing the first-run TOCTOU race so a concurrent setup cannot overwrite it.
    /// </summary>
    Task<bool> TryInitializeAsync(string username, string passwordHash, CancellationToken cancellationToken);
}
