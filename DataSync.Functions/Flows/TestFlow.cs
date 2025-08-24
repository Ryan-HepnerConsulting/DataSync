using System.Net.Http.Json;
using Azure.Security.KeyVault.Secrets;

namespace DataSync.Functions.Flows;

// Use a unique name you can reference in tenant docs if you want to run it via queue too.
[Flow("test")]
public sealed class TestFlow : IFlowTask
{
    private readonly HttpClient _http = new();

    public async Task RunAsync(string tenantId, SecretClient kv, CancellationToken ct)
    {
        // Free, no-auth weather endpoint (Open-Meteo).
        // We'll fetch current temperature for NYC.
        var url = "https://api.open-meteo.com/v1/forecast?latitude=40.7128&longitude=-74.0060&current=temperature_2m";

        var resp = await _http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();

        var model = await resp.Content.ReadFromJsonAsync<OpenMeteoResponse>(cancellationToken: ct)
                    ?? throw new InvalidOperationException("No JSON payload returned from weather API.");

        // Simple assertion-like guard in the flow: throw if missing data
        if (model.current is null || model.current.temperature_2m is null)
            throw new InvalidOperationException("Weather payload didn't contain current.temperature_2m.");

        // (Optional) You could log or do something with the temperature here.
        // Not returning anything is fine; success = no exception.
    }

    // minimal DTOs for the specific fields we read
    private sealed class OpenMeteoResponse
    {
        public Current? current { get; set; }
    }

    private sealed class Current
    {
        public double? temperature_2m { get; set; }
    }
}