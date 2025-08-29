using Azure.Security.KeyVault.Secrets;
using DataSync.Functions.Flows;

namespace Tests.Common;

public sealed class FakeFlow : IFlowTask
{
    private readonly Action<string>? _onRun;
    private int _count;

    public FakeFlow(Action<string>? onRun = null) => _onRun = onRun;

    public int InvocationCount => _count;

    public Task RunAsync(string tenantId, SecretClient kv, CancellationToken ct)
    {
        Interlocked.Increment(ref _count);
        _onRun?.Invoke(tenantId);
        return Task.CompletedTask;
    }
}