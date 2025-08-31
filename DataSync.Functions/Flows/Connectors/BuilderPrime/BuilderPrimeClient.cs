using System.Net.Http.Json;
using DataSync.Functions.Flows._shared;
using DataSync.Functions.Flows.connectors.BuilderPrime.Models;

namespace DataSync.Functions.Flows.connectors.BuilderPrime;

public sealed class BuilderPrimeClient()
{
    private readonly HttpClient _http = new HttpClient();
    // 8950670626070409Ss$
    // nathan@hepnerconsulting.com
    // https://bam.builderprime.com/admin/login
    
    // API Key
    // 7zrqZo3.kJG9TPxSQrQdm5fB9j3D


    public async Task UpsertJobsAsync(string baseUrl, string token, IEnumerable<BuilderPrimeJob> jobs, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl.TrimEnd('/')}/jobs:batchUpsert");
        req.Content = JsonContent.Create(jobs);
        var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
    }
}