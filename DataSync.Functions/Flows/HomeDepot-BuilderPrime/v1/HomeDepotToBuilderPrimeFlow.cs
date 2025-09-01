using Azure.Security.KeyVault.Secrets;
using DataSync.Functions.Flows._shared;
using DataSync.Functions.Flows.connectors.BuilderPrime;
using DataSync.Functions.Flows.connectors.HomeDepot;
using DataSync.Functions.Flows.connectors.HomeDepot.Models;
using DataSync.Functions.Flows.HomeDepot_BuilderPrime.v1.Sample;

namespace DataSync.Functions.Flows.HomeDepot_BuilderPrime.v1;

[Flow("home-depot-to-builder-prime:v1")]
public sealed class HomeDepotToBuilderPrimeFlow : FlowBase, IFlowTask
{
    public async Task RunAsync(string tenantId, SecretClient kv, CancellationToken ct)
    {
        var hd = new HomeDepotClient(tenantId);
        var bp = new BuilderPrimeClient(tenantId);
        
        // Load the mapping from the tenants hd->bp:v1 flow
        var flowMapping = FlowMapping.SampleFlowMapping();
        Mapper.LoadMappingFromJson(flowMapping);

        // 1) Pull new/changed leads from HD since last watermark and create them in Builder Prime
        var hdLeads = await hd.GetLeadsSinceLastPullAsListAsync(ct);
        await CreateBuilderPrimeLeads(hdLeads, ct);

        // 2) Check Builder Prime for fields that HDSC requires us to echo back via updates
        //    (ACK/CONFIRM, APPOINTMENTS, DISPOSITION). 
        var headerUpdates = new List<object>();     // lead header only (status/fields)
        var apptUpdates   = new List<object>();     // appointment child object only

        foreach (var lead in hdLeads)
        {
            if (string.IsNullOrEmpty(lead.Id))
                return;
            
            // ------------------ PLACEHOLDER: replace with real BP → HD comparison ------------------
            // Pretend BP has made these changes since last sync (toggle as needed):
            var leadStatus = LeadLookupRequest.LeadStatus.ReadyToSell;
            string? cancelReason      = "Cannot connect with customer";     // required when cancelling
            
            var submitLeadFlag = LeadLookupRequest.LeadFlag.DefaultOrCancelled;
            
            bool bpScheduledAppt      = false;                          // created an appointment
            bool bpRescheduledAppt    = false;                         // rescheduled an appointment
            string   apptId           = Guid.NewGuid().ToString();
            DateTime apptDateUtc      = DateTime.UtcNow.AddDays(3).Date.AddHours(16); // 3pm UTC
            DateTime? originalApptUtc = null;
            
            // >>>>>>>> MAP BUILDER PRIME DATA TO PERFORM HC LEAD UPDATES HERE USING THE VARIABLES ABOVE
            
            leadStatus = Mapper.ConvertBuilderPrimeLeadStatusToHdLeadStatus(bp.GetBuilderPrimeStatus(lead.Id));
            
            if (leadStatus == LeadLookupRequest.LeadStatus.Sold)
            {
                submitLeadFlag = LeadLookupRequest.LeadFlag.Sold;
            }
            
            lead.MMSVCSubmitLeadFlag = submitLeadFlag;
            
            // Check for appointments and create them if necessary
            
            // ---------------------------------------------------------------------------------------

            // Common required header fields for any update (mirror values HD already has)
            string id     = lead.Id ?? lead.OrderNumber ?? throw new InvalidOperationException("Missing lead Id");
            string store  = (lead.SFIReferralStore ?? lead.MMSVStoreNumber ?? "0000").PadLeft(4, '0');
            string prog = lead.SFIProgramGroupNameUnconstrained;
            string mvendor= lead.SFIMVendor;
            string typeCd = lead.MMSVCSSVSTypeCode; 

            // 2c) Appointment create / reschedule (child object ONLY; do not mix with header updates)
            if (bpScheduledAppt || bpRescheduledAppt)
                apptUpdates.Add(HomeDepotClient.BuildAppointmentUpdate(
                    id, store, mvendor, prog, typeCode: typeCd,
                    apptId: apptId,
                    scheduleUtc: apptDateUtc,
                    isReschedule: bpRescheduledAppt,
                    originalScheduleUtc: originalApptUtc));
            
            
            // 2d) Disposition (Cancelled with reason)
            headerUpdates.Add(HomeDepotClient.BuildHeaderUpdate(
                id, store, mvendor, prog, status: leadStatus, statusReason: cancelReason, typeCode: typeCd,
                lead));
        }

        // 3) Send updates to HDSC in batches of 10, keeping header and child updates separate
        if (headerUpdates.Count > 0)
            await hd.SubmitLeadHeaderBatchAsync(headerUpdates, ct);

        if (apptUpdates.Count > 0)
            await hd.SubmitAppointmentBatchAsync(apptUpdates, ct);
    }

    private async Task CreateBuilderPrimeLeads(List<LeadLookupResponse.LeadHeader> leads, CancellationToken ct)
    {
        if (leads == null || leads.Count == 0) return;

        foreach (var l in leads)
        {
            try
            {
                // ------------------ PLACEHOLDER: call Builder Prime create lead ------------------
                // await _bp.CreateLeadAsync(new BpLead { ... map from 'l' ... }, ct);
                // --------------------------------------------------------------------------------
            }
            catch
            {
                // TODO: DLQ / retry
            }
        }
    }
}
