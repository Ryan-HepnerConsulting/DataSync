using System.Text.Json.Serialization;

namespace DataSync.Functions.Models;

public class Tenant
{
    [JsonPropertyName("id")] 
    public string Id { get; set; } = null!;
    
    [JsonPropertyName("type")] 
    public string Type { get; set; } = "Tenant";
    
    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; } = null!;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;
    
    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = "America/Chicago";
    
    [JsonPropertyName("flows")]
    public List<Flow> Flows { get; set; } = new();
    
    [JsonPropertyName("_etag")] 
    public string? ETag { get; set; }
}

