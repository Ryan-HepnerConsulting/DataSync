using System.Net.Http.Json;
using DataSync.Functions.Flows._shared;
using DataSync.Functions.Flows.connectors.BuilderPrime.Models;

namespace DataSync.Functions.Flows.connectors.BuilderPrime;

public sealed class BuilderPrimeClient(string tenantId)
{
    private readonly HttpClient _http = new HttpClient();
    // 8950670626070409Ss$
    // nathan@hepnerconsulting.com
    // https://bam.builderprime.com/admin/login
    
    // API Key
    // 7zrqZo3.kJG9TPxSQrQdm5fB9j3D
    

    public string GetBuilderPrimeStatus(string leadId)
    {
        return "Job Sold";
    }
}