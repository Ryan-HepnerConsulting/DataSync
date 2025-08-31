using DataSync.Functions.Flows.connectors.HomeDepot.Models;
using DataSync.Functions.Flows.connectors.BuilderPrime.Models;

namespace DataSync.Functions.Flows.HomeDepot_BuilderPrime.v1;

public static class Mapping
{
    public static BuilderPrimeJob ToBuilderPrimeJob(LeadLookupResponse.LeadHeader o) => new()
    {
        /*Phone      = o.Phone,
        City       = o.City,
        State      = o.State,
        Stage = o.Status switch
        {
            "delivered" => "Completed",
            "shipped"   => "In Progress",
            _           => "New"
        }*/
    };
}