using System.Text.Json;
using Cronos;
using Azure.Storage.Queues;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using DataSync.Functions.Models;
using Microsoft.Azure.Cosmos.Linq;

namespace DataSync.Functions;

public class Orchestrator
{
    private readonly Container _cosmos;
    private readonly QueueClient _queue;
    private readonly ILogger<Orchestrator> _log;

    public Orchestrator(Container cosmos, QueueClient queue, ILogger<Orchestrator> log)
    { _cosmos = cosmos; _queue = queue; _log = log; }

    [Function("HourlyTimer")]
    public async Task RunAsync([TimerTrigger("0 0 * * * *")] TimerInfo _)
    {
        _log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        /*await _queue.CreateIfNotExistsAsync();

        var now = DateTime.UtcNow;
        var it = _cosmos.GetItemLinqQueryable<Tenant>(allowSynchronousQueryExecution: false)
                        .Where(t => t.Type == "Tenant")
                        .ToFeedIterator();

        while (it.HasMoreResults)
        {
            var page = await it.ReadNextAsync();
            foreach (var tenant in page)
            {
                var changed = false;

                foreach (var flow in tenant.Flows.Where(f => f.Enabled))
                {
                    var due = flow.NextRunUtc == null || flow.NextRunUtc <= now;
                    if (!due) continue;

                    var job = new FlowJob(tenant.TenantId, flow.Name);
                    var json = JsonSerializer.Serialize(job);
                    var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json));
                    await _queue.SendMessageAsync(base64);

                    var next = CronExpression.Parse(flow.Cron)
                        .GetNextOccurrence(now, TimeZoneInfo.Utc) ?? now.AddHours(1);
                    flow.NextRunUtc = next;
                    changed = true;

                    _log.LogInformation("Enqueued {Tenant}/{Flow}; next @ {Next}",
                        tenant.TenantId, flow.Name, flow.NextRunUtc);
                }

                if (changed)
                {
                    await _cosmos.ReplaceItemAsync(tenant, tenant.Id, new PartitionKey(tenant.TenantId));
                }
            }
        }*/
    }
}
