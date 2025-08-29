using Azure;
using Azure.Security.KeyVault.Secrets;

namespace Tests.Common;

public sealed class FakeSecretClient(Dictionary<string, string>? secrets = null) : SecretClient
{
    private readonly Dictionary<string, string> _secrets = secrets ?? new Dictionary<string, string>();

    public override Response<KeyVaultSecret> GetSecret(string name, string? version = null, CancellationToken cancellationToken = default)
    {
        if (!_secrets.TryGetValue(name, out var value))
            throw new RequestFailedException(404, $"Secret not found: {name}");

        return Response.FromValue(new KeyVaultSecret(name, value), null!);
    }

    public override Task<Response<KeyVaultSecret>> GetSecretAsync(string name, string? version = null, CancellationToken cancellationToken = default)
        => Task.FromResult(GetSecret(name, version, cancellationToken));
}