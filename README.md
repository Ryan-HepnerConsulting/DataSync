DataSync — MVP README (Azure Functions Isolated, .NET 9)

A tiny, scalable framework to run per-tenant “flows” (API→API syncs) on schedules you control via data. One hourly function enqueues work; a queue-triggered function fans out and executes your per-file C# flow tasks concurrently. Secrets live in Key Vault; tenants & flows live in Cosmos DB.

What’s inside

DataSync.Functions (Functions App, .NET 9 isolated)

Program.cs — builder pattern wiring (Cosmos, Queue, flow registry, App Insights)

Orchestrator.cs — hourly timer: scans tenants, enqueues due flows

RunFlow.cs — queue trigger: resolves a flow by name and runs it

Models/ — TenantDoc, FlowCfg, FlowJob

Flows/ — IFlowTask, FlowAttribute, FlowRegistry (auto-discovers flows)

Flows/<YourFlow>.cs — one file per flow (e.g., HomeDepotToBuilderPrime.cs)

DataSync.Functions.Tests (xUnit)

Example tests (weather flow, registry, handler)

Quick start

Prereqs

.NET 9 SDK

Azure Functions Core Tools v4

Azurite (local storage) or an Azure Storage account

Cosmos DB account (or local emulator)

Azure Key Vault (or skip for flows that don’t need secrets)

Configure DataSync.Functions/local.settings.json

{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated"
  },
  "Cosmos:ConnectionString": "<cosmos-conn-string>",
  "Cosmos:Database": "flowsync",
  "Cosmos:Container": "tenants",
  "Queue:Name": "flow-jobs",
  "KeyVault:Uri": "https://<your-kv>.vault.azure.net/"
}


Install deps & run locally

dotnet restore
func start


The orchestrator runs hourly. To force a run, set a flow’s nextRunUtc to a past time.

Run tests

dotnet test

Data model (Cosmos)

Partition by /tenantId in a single tenants container.

{
  "id": "tenant-123",
  "type": "Tenant",
  "tenantId": "tenant-123",
  "name": "Acme Cabinets",
  "timezone": "America/Chicago",
  "flows": [
    {
      "name": "home-depot→builder-prime",
      "enabled": true,
      "cron": "0 0 */2 * * *",        // every 2 hours (CRON with seconds)
      "nextRunUtc": "2025-08-23T01:00:00Z"
    }
  ]
}


enabled: flip true/false to turn a flow on/off at data level.

cron: 6-field CRON (sec min hour day month dow) using UTC.

nextRunUtc: updated by Orchestrator after each enqueue.

Tip: For “1pm in America/Chicago”, compute the next UTC occurrence at creation and set cron accordingly, or keep cron UTC and compute nextRunUtc using the tenant’s timezone at creation/update time.

Scheduling

Orchestrator [TimerTrigger("0 0 * * * *")] runs hourly.

For each tenant: for each flow where enabled==true and nextRunUtc<=now → enqueue a small message { tenantId, flowName }, then recompute nextRunUtc from cron.

CRON parsing uses Cronos. Remember: seconds first.

Scaling & performance

QueueTrigger auto-scales instances based on queue depth.

Adjust concurrency via host.json (Storage Queues) and, if needed, FUNCTIONS_WORKER_PROCESS_COUNT (e.g., 2–4) in Function App settings.

Keep each flow run reasonably small (bounded time window / page size). If a flow gets heavy, split work internally or run multiple queue messages.

Secrets

Store per-tenant secrets in Key Vault by convention:

tenants--{tenantId}--{system}--{key}
ex: tenants--tenant-123--homedepot--api-token


Grant the Function App Managed Identity get access to secrets.

Creating a new tenant & flow (step-by-step)
A) Create the flow code (once)

Add a file DataSync.Functions/Flows/<YourFlow>.cs:

using System.Net.Http.Json;
using Azure.Security.KeyVault.Secrets;

namespace DataSync.Functions.Flows;

[Flow("my-source→my-target")]                // <-- public name used in data
public sealed class MySourceToMyTarget : IFlowTask
{
    private readonly HttpClient _http = new();

    public async Task RunAsync(string tenantId, SecretClient kv, CancellationToken ct)
    {
        // 1) Read required secrets
        var srcBase = (await kv.GetSecretAsync($"tenants--{tenantId}--mysource--api-base", ct)).Value.Value;
        var srcTok  = (await kv.GetSecretAsync($"tenants--{tenantId}--mysource--api-token", ct)).Value.Value;
        var dstBase = (await kv.GetSecretAsync($"tenants--{tenantId}--mytarget--api-base", ct)).Value.Value;
        var dstTok  = (await kv.GetSecretAsync($"tenants--{tenantId}--mytarget--api-token", ct)).Value.Value;

        // 2) Pull
        using var req1 = new HttpRequestMessage(HttpMethod.Get, $"{srcBase}/items");
        req1.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", srcTok);
        var resp1 = await _http.SendAsync(req1, ct);
        resp1.EnsureSuccessStatusCode();
        var items = await resp1.Content.ReadFromJsonAsync<List<SrcDto>>(cancellationToken: ct) ?? new();

        // 3) Transform/map
        var payload = items.Select(i => new DstDto { Id = i.Id, Name = i.Name }).ToArray();

        // 4) Push
        using var req2 = new HttpRequestMessage(HttpMethod.Post, $"{dstBase}/items:upsert");
        req2.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", dstTok);
        req2.Content = JsonContent.Create(payload);
        var resp2 = await _http.SendAsync(req2, ct);
        resp2.EnsureSuccessStatusCode();
    }

    private sealed class SrcDto { public string Id { get; set; } = ""; public string Name { get; set; } = ""; }
    private sealed class DstDto { public string Id { get; set; } = ""; public string Name { get; set; } = ""; }
}


No manual registration needed — the [Flow("…")] attribute is auto-discovered in Program.cs (FlowRegistry.RegisterAllFlows).

B) Add tenant secrets

In Key Vault:

tenants--tenant-999--mysource--api-base     = https://api.mysource.com/v1
tenants--tenant-999--mysource--api-token    = <token>
tenants--tenant-999--mytarget--api-base     = https://api.mytarget.com/v1
tenants--tenant-999--mytarget--api-token    = <token>

C) Create the tenant document

Insert into Cosmos (flowsync/tenants):

{
  "id": "tenant-999",
  "type": "Tenant",
  "tenantId": "tenant-999",
  "name": "Newco",
  "timezone": "America/Chicago",
  "flows": [
    {
      "name": "my-source→my-target",     // must match [Flow("…")]
      "enabled": true,
      "cron": "0 0 * * * *",             // hourly in UTC
      "nextRunUtc": "2025-08-23T00:00:00Z"
    }
  ]
}

D) Verify

Run func start.

Set nextRunUtc to a time in the past to trigger immediately.

Watch logs for START tenant-999/my-source→my-target then DONE ….

Toggle a flow: set "enabled": false and save.
Change cadence: edit cron (use a 6-field expression) and optionally adjust nextRunUtc for the new schedule.

Best practices (MVP-friendly)

One job = one small “window” of data. Keep runs short and idempotent.

HTTP client reuse — each flow holds a single HttpClient field (as in the examples).

Don’t log secrets — never include tokens/headers in logs.

Retries/backoff — for production flows, wrap outbound calls with a library like Polly (retry + jitter, honor Retry-After).

Time zones — store nextRunUtc in UTC; if tenants schedule by local time, compute UTC at write time.

Scaling knobs

Storage Queues: tune batchSize, newBatchThreshold in host.json.

Add FUNCTIONS_WORKER_PROCESS_COUNT=2..4 for more concurrency per plan instance.

Use a Premium plan in Azure for warm workers and faster scale.

Service Bus (optional later) — if you need strict per-tenant ordering or higher throughput, swapping Storage Queue to Service Bus is a small change.

Cosmos costs

Partition by tenantId.

Query tenants paged; avoid cross-partition scans for large fleets (or shard tenants by alpha if you grow big).

Testing strategy (what to keep)

Unit: FlowRegistry resolves flows; cron → next occurrence; handler throws on flow failure.

Integration (fast): a test flow that calls a public API (e.g., TestWeatherFlow) and asserts no exception.

Optional: Directly call RunFlow.HandleAsync(base64) with a fake registry to simulate an enqueued job.

Run:

dotnet test

Deployment (high-level)

Create resources: Function App (Premium recommended), Storage Account, Cosmos DB (SQL API), Key Vault.

Enable System-Assigned Managed Identity on the Function App; grant KV Secrets/Get.

App Settings:

FUNCTIONS_WORKER_RUNTIME=dotnet-isolated

FUNCTIONS_WORKER_PROCESS_COUNT=2 (optional)

Your Cosmos:*, Queue:Name, KeyVault:Uri, and AzureWebJobsStorage.

func azure functionapp publish <your-app> or CI/CD.

Operational tips

Force a run now: set a flow’s nextRunUtc < current UTC.

Pause a tenant: set every flow’s enabled=false.

Add a flow to many tenants: script a Cosmos update to append a new FlowCfg.

Monitor: Application Insights traces are enabled; consider alerts on exceptions and queue age.

FAQ

Why Storage Queues vs Service Bus?
Storage Queues are the simplest. If you later need sessions (per-tenant ordering), dead-letter queues, or VNET/private endpoints, swap to Service Bus with minimal code changes.

Where do I put per-tenant mapping rules?
For the MVP each flow encodes its logic. If you need data-driven mappings later, add a maps container and load a mapping per (flowName, version) at runtime.

Can I run flows more frequently than hourly?
Yes—set cron to the cadence you want (e.g., every 5 minutes) and change the Orchestrator timer to run more often (e.g., 0 */5 * * * *). The design stays the same.

That’s it. You can now add a new flow by dropping a single file in Flows/, inject tenant secrets via Key Vault, and turn flows on/off or change schedules entirely from data.
