using Heimdall.Agent.Push;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Heimdall.Agent.Enrollment;

/// <summary>Caches and persists this host's agent key, enrolling on first use and re-enrolling if it is rejected.</summary>
internal sealed class AgentKeyStore(
    HeimdallApiClient apiClient,
    IOptions<HeimdallAgentOptions> options,
    ILogger<AgentKeyStore> logger)
{
    private readonly HeimdallAgentOptions _options = options.Value;
    private readonly string _keyFilePath = BuildKeyFilePath(options.Value.HostName);
    private string? _cachedKey;

    public async Task<string?> EnsureKeyAsync(CancellationToken cancellationToken)
    {
        if (_cachedKey is not null)
            return _cachedKey;

        if (File.Exists(_keyFilePath))
        {
            var stored = (await File.ReadAllTextAsync(_keyFilePath, cancellationToken)).Trim();
            if (!string.IsNullOrEmpty(stored))
            {
                _cachedKey = stored;
                return _cachedKey;
            }
        }

        var key = await apiClient.EnrollAsync(_options.HostName, _options.EnrollmentKey, cancellationToken);
        if (key is null)
            return null;

        await PersistAsync(key, cancellationToken);
        _cachedKey = key;
        logger.LogInformation("Enrolled host {HostName}; agent key stored at {KeyFilePath}.", _options.HostName, _keyFilePath);
        return key;
    }

    public void Invalidate()
    {
        _cachedKey = null;
        try
        {
            if (File.Exists(_keyFilePath))
                File.Delete(_keyFilePath);
        }
        catch (IOException)
        {
            // Best effort; a stale file just causes one extra failed push before re-enrollment.
        }
    }

    private async Task PersistAsync(string key, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_keyFilePath)!);

        // Write to a temp file with owner-only permissions, then atomically move into place.
        var tempPath = _keyFilePath + ".tmp";
        await File.WriteAllTextAsync(tempPath, key, cancellationToken);
        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(tempPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        File.Move(tempPath, _keyFilePath, overwrite: true);
    }

    private static string BuildKeyFilePath(string hostName)
    {
        var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Heimdall");
        var safeName = string.Concat(hostName.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(directory, $"{safeName}.key");
    }
}
