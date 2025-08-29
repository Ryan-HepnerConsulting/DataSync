using Azure.Security.KeyVault.Secrets;

namespace DataSync.Functions.Flows._shared;

public abstract class FlowBase
{
    protected static async Task<string> GetSecretAsync(
        SecretClient kv, string tenantId, string system, string key, CancellationToken ct) =>
        (await kv.GetSecretAsync($"tenants--{tenantId}--{system}--{key}", null, ct)).Value.Value;
}