using Azure.Security.KeyVault.Secrets;

namespace DataSync.Functions.Flows;

public interface IFlowTask
{
    Task RunAsync(string tenantId, SecretClient kv, CancellationToken ct);
}