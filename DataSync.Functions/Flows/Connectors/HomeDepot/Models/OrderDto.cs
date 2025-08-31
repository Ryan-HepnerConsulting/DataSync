using System.Text.Json.Serialization;

namespace DataSync.Functions.Flows.connectors.HomeDepot.Models;

// --- Models the API expects exactly (JSON) ---
public sealed class LeadLookupRequest
    {
        [JsonPropertyName("SFILEADLOOKUPWS_Input")]
        public Input _Input { get; init; } = new();

        public sealed class Input
        {
            [JsonPropertyName("PageSize")] public int PageSize { get; init; } = 25;
            [JsonPropertyName("ListOfSfileadbows")] public ListOf ListOfSfileadbows { get; init; } = new();
            [JsonPropertyName("StartRowNum")] public int StartRowNum { get; init; } = 0;
        }

        public sealed class ListOf
        {
            [JsonPropertyName("Sfileadheaderws")]
            public Sfileadheaderws Sfileadheaderws { get; init; } = new();
        }

        public sealed class Sfileadheaderws
        {
            [JsonPropertyName("Searchspec")] public string? Searchspec { get; init; }
        }
    }


    public sealed class LeadLookupResponse
    {
        [JsonPropertyName("SFILEADLOOKUPWS_Output")]
        public Output _Output { get; init; } = new();

        public sealed class Output
        {
            [JsonPropertyName("Status")] public string? Status { get; init; }
            [JsonPropertyName("LastPage")] public string? LastPage { get; init; }

            [JsonIgnore]
            public bool IsLastPage =>
                string.Equals(LastPage, "true", StringComparison.OrdinalIgnoreCase) ||
                LastPage == "1" || string.Equals(LastPage, "y", StringComparison.OrdinalIgnoreCase);
            [JsonPropertyName("ListOfSfileadbows")] public ListOf? ListOf { get; init; }
        }

        public sealed class ListOf
        {
            [JsonPropertyName("Sfileadheaderws")]
            public List<LeadHeader>? Items { get; init; }
        }

        public sealed class LeadHeader
        {
            public string SFIReferralStore;
            public string MMSVStoreNumber;
            public string SFIProgramGroupNameUnconstrained;
            public string SFIMVendor;
            public string MMSVCSSVSTypeCode;
            public string MMSVPreferredContactPhoneNumber;
            public string SFIContactHomePhone;
            public string MMSVSiteAddress;
            public string MMSVSiteCity;
            public string MMSVSitePostalCode;
            public string MMSVSiteState;
            public string MMSVSiteCountry;
            public string? Id { get; init; }
            public string OrderNumber { get; set; }
            public string ContactFirstName { get; set; }
            public string ContactLastName { get; set; }
            public object MainEmailAddress { get; set; }
        }
    }