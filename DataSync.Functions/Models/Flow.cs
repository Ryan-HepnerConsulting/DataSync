using System.Text.Json.Serialization;

namespace DataSync.Functions.Models;

public class Flow
{
    [JsonPropertyName("name")] 
    public string Name { get; set; } = default!;       // must match [Flow("...")]
    
    [JsonPropertyName("enabled")] 
    public bool Enabled { get; set; } = true;
    
    [JsonPropertyName("cron")] 
    public string Cron { get; set; } = "0 0 * * * *";  // default hourly
    
    [JsonPropertyName("nextRunUtc")] 
    public DateTime? NextRunUtc { get; set; }          // stored UTC
}

public record FlowJob(string TenantId, string FlowName);