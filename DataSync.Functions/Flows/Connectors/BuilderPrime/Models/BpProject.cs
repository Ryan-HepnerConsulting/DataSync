using System.Text.Json.Serialization;

namespace DataSync.Functions.Flows.Connectors.BuilderPrime.Models
{
    // Root model – only what we need to reach estimate items
    public sealed class BpProject
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        // Optional: handy context if you want it later
        [JsonPropertyName("workSiteAddressCity")]
        public string? WorkSiteAddressCity { get; set; }

        [JsonPropertyName("workSiteAddressState")]
        public string? WorkSiteAddressState { get; set; }

        [JsonPropertyName("workSiteAddressZip")]
        public string? WorkSiteAddressZip { get; set; }

        // The only thing we actually care about:
        [JsonPropertyName("estimatesCalculatorItems")]
        public List<BpEstimateItem> EstimateItems { get; set; } = new();
    }

    // Minimal fields from each estimate line item
    public sealed class BpEstimateItem
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("itemQuantity")]
        public decimal? ItemQuantity { get; set; }

        [JsonPropertyName("measurementValue")]
        public decimal? MeasurementValue { get; set; }

        [JsonPropertyName("itemPrice")]      // unit/each price
        public decimal? ItemPrice { get; set; }

        [JsonPropertyName("totalPrice")]
        public decimal? TotalPrice { get; set; }

        [JsonPropertyName("notes")]          // HTML from BP; strip later if needed
        public string? NotesHtml { get; set; }

        [JsonPropertyName("usePlainText")]
        public bool? UsePlainText { get; set; }

        [JsonPropertyName("itemPhoto")]
        public string? ItemPhotoUrl { get; set; }

        [JsonPropertyName("sortOrder")]
        public int? SortOrder { get; set; }

        // Created/modified are epoch millis in the JSON; keep as long? to stay minimal
        [JsonPropertyName("createdDate")]
        public long? CreatedDateEpochMs { get; set; }

        [JsonPropertyName("lastModifiedDate")]
        public long? LastModifiedDateEpochMs { get; set; }
    }
}
