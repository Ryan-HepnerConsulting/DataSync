using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace DataSync.Functions.Flows.connectors.HomeDepot.Models;

// --- Models the API expects exactly (JSON) ---
public sealed class LeadLookupRequest
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum LeadStatus
        {
            [EnumMember(Value = "Acknowledged")]
            Acknowledged,

            [EnumMember(Value = "Cancelled")]
            Cancelled,

            [EnumMember(Value = "Confirmed")]
            Confirmed,
            [EnumMember(Value = "RTS")]
            ReadyToSell,
            [EnumMember(Value = "Sold. Paid in Full")]
            Sold,
        }
        
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum LeadFlag
        {
            [EnumMember(Value = "N")]
            DefaultOrCancelled,
            [EnumMember(Value = "Z")]
            Sold,
        }
        
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
            [JsonPropertyName("SFIWorkflowOnlyStatus")]
            public string SFIWorkflowOnlyStatus;
            
            [JsonPropertyName("MMSVCSubmitLeadFlag")]
            public LeadLookupRequest.LeadFlag MMSVCSubmitLeadFlag;

            [JsonPropertyName("SFIReferralStore")]
            public string SFIReferralStore { get; set; }

            [JsonPropertyName("MMSVStoreNumber")]
            public string MMSVStoreNumber { get; set; }

            [JsonPropertyName("SFIProgramGroupNameUnconstrained")]
            public string SFIProgramGroupNameUnconstrained { get; set; }

            [JsonPropertyName("SFIMVendor")]
            public string SFIMVendor { get; set; }

            [JsonPropertyName("MMSVCSSVSTypeCode")]
            public string MMSVCSSVSTypeCode { get; set; }

            [JsonPropertyName("MMSVPreferredContactPhoneNumber")]
            public string MMSVPreferredContactPhoneNumber { get; set; }

            [JsonPropertyName("SFIContactHomePhone")]
            public string SFIContactHomePhone { get; set; }

            [JsonPropertyName("MMSVSiteAddress")]
            public string MMSVSiteAddress { get; set; }

            [JsonPropertyName("MMSVSiteCity")]
            public string MMSVSiteCity { get; set; }

            [JsonPropertyName("MMSVSitePostalCode")]
            public string MMSVSitePostalCode { get; set; }

            [JsonPropertyName("MMSVSiteState")]
            public string MMSVSiteState { get; set; }

            [JsonPropertyName("MMSVSiteCountry")]
            public string MMSVSiteCountry { get; set; }

            [JsonPropertyName("Id")]
            public string? Id { get; init; }

            [JsonPropertyName("OrderNumber")]
            public string OrderNumber { get; set; }

            [JsonPropertyName("ContactFirstName")]
            public string ContactFirstName { get; set; }

            [JsonPropertyName("ContactLastName")]
            public string ContactLastName { get; set; }

            [JsonPropertyName("MainEmailAddress")]
            public object MainEmailAddress { get; set; }
        }

    }