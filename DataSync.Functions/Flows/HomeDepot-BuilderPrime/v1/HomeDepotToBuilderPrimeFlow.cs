using Azure.Security.KeyVault.Secrets;
using DataSync.Functions.Flows._shared;
using DataSync.Functions.Flows.connectors.BuilderPrime;
using DataSync.Functions.Flows.connectors.HomeDepot;
using Microsoft.Extensions.Logging;

namespace DataSync.Functions.Flows.HomeDepot_BuilderPrime.v1;

[Flow("hd→bp:v1")]
public sealed class HomeDepotToBuilderPrimeFlow : FlowBase, IFlowTask
{
    private readonly BuilderPrimeClient _bp = new();
    private string _mvendornumber = "";
    private DateTime _toUtc = DateTime.UtcNow; // current time
    private DateTime _fromUtc = DateTime.UtcNow.AddMonths(-1); // last watermark

    public async Task RunAsync(string tenantId,SecretClient kv, CancellationToken ct)
    {
        var hdClient = new HomeDepotClient(tenantId);
        
        // 1. Pull leads from Home Depot Lookup API since the last watermark
        var leads = await hdClient.GetLeadsSinceLastPullAsListAsync(ct);


        if (leads == null || leads.Count == 0)
        {
            // no new leads found since watermark
            return;
        }

        // 2. Transform & insert into Builder Prime
        foreach (var lead in leads)
        {
            try
            {
                //var bpClient = lead.ToBuilderPrimeClient(); // extension or mapper
                //await _bp.UpsertClientAsync(bpClient, ct);

                //Logger.Info("Inserted/updated lead {leadId} into Builder Prime", lead.LeadId);
            }
            catch (Exception ex)
            {
                //Logger.Error(ex, "Failed to process lead {leadId}", lead.LeadId);
                // TODO: DLQ / retry
            }
        }

        // 3. (Optional) push status update back to HD
        // await _hd.UpdateStatusAsync(leads, "RECEIVED_BY_SP", ct);
    }

}