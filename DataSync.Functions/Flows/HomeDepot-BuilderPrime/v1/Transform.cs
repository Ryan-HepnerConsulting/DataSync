using DataSync.Functions.Flows.connectors.BuilderPrime.Models;

namespace DataSync.Functions.Flows.HomeDepot_BuilderPrime.v1;

public static class Transform
{
    public static BuilderPrimeJob NormalizePhone(BuilderPrimeJob j)
    {
        j.Phone = string.IsNullOrWhiteSpace(j.Phone) ? "000-000-0000" : j.Phone.Trim();
        return j;
    }
}