using System.Text.Json;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DataSync.Functions.Models;
using DataSync.Functions.Flows;
using Microsoft.Azure.Functions.Worker;

namespace DataSync.Functions;

public class RunFlow
{
    private readonly IFlowRegistry _registry;
    private readonly IConfiguration _cfg;
    private readonly ILogger<RunFlow> _log;

    public RunFlow(IFlowRegistry registry, IConfiguration cfg, ILogger<RunFlow> log)
    { _registry = registry; _cfg = cfg; _log = log; }

    [Function("RunFlow")]
    public async Task HandleAsync([QueueTrigger("%Queue:Name%")] string base64, CancellationToken ct)
    {
        var job = JsonSerializer.Deserialize<FlowJob>(Convert.FromBase64String(base64))!;
        var kv = new SecretClient(new Uri(_cfg["KeyVault:Uri"]!), new DefaultAzureCredential());

        var flow = _registry.Resolve(job.FlowName);

        _log.LogInformation("START {Tenant}/{Flow}", job.TenantId, job.FlowName);
        await flow.RunAsync(job.TenantId, kv, ct);
        _log.LogInformation("DONE  {Tenant}/{Flow}", job.TenantId, job.FlowName);
    }
}