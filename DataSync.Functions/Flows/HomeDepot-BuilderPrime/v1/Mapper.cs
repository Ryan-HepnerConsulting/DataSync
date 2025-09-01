// Use the same enum you showed (adjust namespace if yours is nested):
// using LeadStatus = LeadLookupRequest.LeadStatus;

using System.Text.Json;
using System.Text.RegularExpressions;
using DataSync.Functions.Flows.connectors.HomeDepot.Models;

namespace DataSync.Functions.Flows.HomeDepot_BuilderPrime.v1;

public static class Mapper
{
    // Matches your config shape
    public sealed class MapConfig
    {
        public Dictionary<string, string[]> map { get; set; } = new();
    }

    private static IReadOnlyDictionary<string, LeadLookupRequest.LeadStatus> _bpToHd = 
        new Dictionary<string, LeadLookupRequest.LeadStatus>(StringComparer.OrdinalIgnoreCase);

    public static void LoadMappingFromJson(string json)
    {
        var cfg = JsonSerializer.Deserialize<MapConfig>(json) 
                  ?? throw new ArgumentException("Invalid mapping JSON.");
        var dict = new Dictionary<string, LeadLookupRequest.LeadStatus>(StringComparer.OrdinalIgnoreCase);

        foreach (var kvp in cfg.map)
        {
            // Top-level keys are "Acknowledged", "Confirmed", "Sold", "Cancelled"
            // They map directly to the LeadStatus enum names.
            if (!Enum.TryParse<LeadLookupRequest.LeadStatus>(kvp.Key, ignoreCase: true, out var hdStatus))
                throw new ArgumentException($"Unknown HD status '{kvp.Key}' in mapping JSON.");

            foreach (var bpStatus in kvp.Value ?? Array.Empty<string>())
            {
                var key = Normalize(bpStatus);
                if (dict.ContainsKey(key))
                    continue; // last-writer-wins 
                dict[key] = hdStatus;
            }
        }

        _bpToHd = dict;
    }

    public static LeadLookupRequest.LeadStatus ConvertBuilderPrimeLeadStatusToHdLeadStatus(string builderPrimeStatus)
    {
        if (string.IsNullOrWhiteSpace(builderPrimeStatus))
            throw new ArgumentException("Builder Prime status is required.", nameof(builderPrimeStatus));

        var key = Normalize(builderPrimeStatus);
        if (_bpToHd.TryGetValue(key, out var hd))
            return hd;

        throw new KeyNotFoundException($"Unmapped Builder Prime status: '{builderPrimeStatus}'.");
    }

    // If you need the exact HDSC string (for JSON body)
    public static string ToHdString(this LeadLookupRequest.LeadStatus status) => status switch
    {
        LeadLookupRequest.LeadStatus.ReadyToSell => "RTS",
        LeadLookupRequest.LeadStatus.Sold        => "Sold. Paid In Full",
        _                      => status.ToString() // uses EnumMember for Acknowledged/Cancelled/Confirmed
    };

    private static string Normalize(string s)
        => Regex.Replace(s.Trim(), @"\s+", " ").ToLowerInvariant();
}