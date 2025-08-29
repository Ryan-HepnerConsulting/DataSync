using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DataSync.Functions.Flows._shared;

namespace DataSync.Functions.Flows.connectors.HomeDepot;

public sealed class HomeDepotClient
{
    private readonly HttpClient _http = new HttpClient();

    // --- Endpoints (prod) ---
    private static readonly string TokenUrl      = "https://api.hs.homedepot.com/iconx/v1/auth/accesstoken";
    private static readonly string LeadLookupUrl = "https://api.hs.homedepot.com/iconx/v1/leads/lookup";
    private static readonly string PoBatchUrl    = "https://api.hs.homedepot.com/iconx/v1/leads/pobatch";

    // --- “Last pull” window (hard-coded for now) ---
    private static readonly TimeSpan Lookback = TimeSpan.FromDays(30);

    // --- Tenant-scoped secrets (loaded in ctor) ---
    private readonly string _tenantId;
    private readonly string _providerId;      // e.g., PV892345
    private readonly string _oauthClientId;   // K: ...
    private readonly string _oauthClientSecret; // S: ...

    // Token cache
    private string? _cachedToken;
    private DateTimeOffset _tokenExpiryUtc;

    private static readonly JsonSerializerOptions _json =
        new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// Create a tenant-scoped Home Depot API client.
    /// Only tenantId is required; credentials are resolved internally.
    /// </summary>
    public HomeDepotClient(string tenantId, HttpClient? http = null)
    {
        _tenantId = tenantId;

        // -------------------------- AZURE KEY VAULT (future) --------------------------
        // Example (uncomment & wire in your SecretClient/IOptions when ready):
        // var kv = new SecretClient(new Uri(kvUrl), new DefaultAzureCredential());
        // _providerId       = (await kv.GetSecretAsync($"hd:{tenantId}:providerId")).Value.Value;
        // _oauthClientId    = (await kv.GetSecretAsync($"hd:{tenantId}:clientId")).Value.Value;
        // _oauthClientSecret= (await kv.GetSecretAsync($"hd:{tenantId}:clientSecret")).Value.Value;
        // -----------------------------------------------------------------------------

        // For now, hard-code so it functions without Key Vault:
        _providerId        = "PV892345";
        _oauthClientId     = "HvKDpzsPxZ7coojqyxbtaN9g0MpAcYDO";
        _oauthClientSecret = "N6xil9R6IGZzW06z";
    }

    // -------------------- OAuth (modern) --------------------

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; init; } = default!;
        [JsonPropertyName("expires_in")]  public int    ExpiresIn   { get; init; }
        [JsonPropertyName("token_type")]  public string TokenType   { get; init; } = default!;
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_cachedToken) && DateTimeOffset.UtcNow < _tokenExpiryUtc)
            return _cachedToken!;

        var url = $"{TokenUrl}?grant_type=client_credentials";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        var basic = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_oauthClientId}:{_oauthClientSecret}"));
        req.Headers.Authorization = new AuthenticationHeaderValue("Basic", basic);

        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        var payload = await resp.Content.ReadFromJsonAsync<TokenResponse>(_json, ct)
                      ?? throw new InvalidOperationException("Failed to acquire Home Depot OAuth token.");

        _cachedToken    = payload.AccessToken;
        _tokenExpiryUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, payload.ExpiresIn - 120)); // buffer
        return _cachedToken!;
    }

    // -------------------- Lead Lookup (modern, Bearer) --------------------

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
            [JsonPropertyName("Id")] public string? Id { get; init; }
            [JsonPropertyName("MMSVCSServiceProviderOrderNumber")] public string? PoNumber { get; init; }
            [JsonPropertyName("ContactFirstName")] public string? FirstName { get; init; }
            [JsonPropertyName("ContactLastName")] public string? LastName { get; init; }
            [JsonPropertyName("MainEmailAddress")] public string? Email { get; init; }
            [JsonPropertyName("MMSVPreferredContactPhoneNumber")] public string? Phone { get; init; }
            [JsonPropertyName("MMSVSiteAddress")] public string? Address1 { get; init; }
            [JsonPropertyName("MMSVSiteCity")] public string? City { get; init; }
            [JsonPropertyName("MMSVSiteState")] public string? State { get; init; }
            [JsonPropertyName("MMSVSitePostalCode")] public string? Postal { get; init; }
            [JsonPropertyName("SFIWorkflowOnlyStatus")] public string? Status { get; init; }
            [JsonPropertyName("SFIMVendor")] public string? Mvendor { get; init; }
            [JsonPropertyName("Created")] public string? Created { get; init; }
        }
    }

    // --- Lookup using appToken only (no Bearer, no x-clientid) ---
    public async IAsyncEnumerable<LeadLookupResponse.LeadHeader> GetLeadsSinceLastPullAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var appToken = await GetAccessTokenAsync(ct);

        var fromUtc   = DateTime.UtcNow - Lookback;
        var fromLocal = fromUtc.ToLocalTime().ToString("MM/dd/yyyy HH:mm:ss");
        var searchspec = $"([SFI MVendor #] = '{_providerId}' AND [Created] >= '{fromLocal}')";

        const int pageSize = 25;
        var start = 0;
        var last  = false;

        // Force HTTP/1.1 and disable cookies
        var handler = new SocketsHttpHandler
        {
            UseCookies = false
        };

        using var client = new HttpClient(handler)
        {
            DefaultRequestVersion = HttpVersion.Version11,
            DefaultVersionPolicy  = HttpVersionPolicy.RequestVersionExact
        };

        while (!last)
        {
            // Build the exact JSON Postman sends (no trailing spaces, no extra fields)
            var json =
            $@"{{
              ""SFILEADLOOKUPWS_Input"": {{
                ""PageSize"": {pageSize},
                ""ListOfSfileadbows"": {{
                  ""Sfileadheaderws"": {{
                    ""Searchspec"": ""{searchspec.Replace("\"", "\\\"")}""
                  }}
                }},
                ""StartRowNum"": {start}
              }}
            }}";

            using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.hs.homedepot.com/iconx/v1/leads/lookup")
            {
                Version = HttpVersion.Version11
            };

            // Required headers only
            req.Headers.TryAddWithoutValidation("appToken", appToken);
            req.Headers.Accept.Clear();
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Send JSON with Content-Type EXACTLY "application/json" (no charset)
            var bytes = Encoding.UTF8.GetBytes(json);
            req.Content = new ByteArrayContent(bytes);
            req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            req.Content.Headers.ContentLength = bytes.Length;

            using var resp = await client.SendAsync(req, ct);
            var content = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
                throw new HttpRequestException($"Lead lookup failed ({(int)resp.StatusCode} {resp.ReasonPhrase}).\n{content}");

            // Deserialize
            var payload = JsonSerializer.Deserialize<LeadLookupResponse>(content, _json)
                          ?? throw new InvalidOperationException("Empty/invalid response payload.");

            var list = payload._Output?.ListOf?.Items ?? new List<LeadLookupResponse.LeadHeader>();
            foreach (var lead in list)
                yield return lead;

            last = payload?._Output?.IsLastPage == true;
            start += pageSize;
        }
    }
    
    public async Task<List<LeadLookupResponse.LeadHeader>> GetLeadsSinceLastPullAsListAsync(
        CancellationToken ct = default)
    {
        var results = new List<LeadLookupResponse.LeadHeader>();
        await foreach (var l in GetLeadsSinceLastPullAsync(ct))
            results.Add(l);
        return results;
    }

    // -------------------- Lead Update (PO Batch, modern Bearer) --------------------

    public sealed class PoBatchRequest
    {
        [JsonPropertyName("SFILEADPOBATCHICONX_Input")]
        public Inbound _Inbound { get; init; } = new();

        public sealed class Inbound
        {
            public ListOf ListOfMmSvCsServiceProviderLeadInbound { get; init; } = new();
        }
        public sealed class ListOf
        {
            public List<Lead> MmSvCsServiceProviderLeadInbound { get; init; } = new();
        }
        public sealed class Lead
        {
            public string? MMSVCSServiceProviderOrderNumber { get; init; } // HDSC lead #
            public string? SFIMVendor { get; init; }                        // e.g., PV892345
            public string? SFIWorkflowOnlyStatus { get; init; }             // e.g., "Received by SP"
            public string? Description { get; init; }                       // notes (optional)
            public string? ExternalRef { get; init; }                       // (optional) BP ids
        }
    }

    /// <summary>
    /// Minimal helper to ACK a lead as received by SP (no creds/urls required by caller).
    /// </summary>
    public async Task AcknowledgeLeadReceivedAsync(string hdLeadNumber, string? notes = null, CancellationToken ct = default)
    {
        var accessToken = await GetAccessTokenAsync(ct);

        var body = new PoBatchRequest
        {
            _Inbound = new PoBatchRequest.Inbound
            {
                ListOfMmSvCsServiceProviderLeadInbound = new PoBatchRequest.ListOf
                {
                    MmSvCsServiceProviderLeadInbound =
                    {
                        new PoBatchRequest.Lead
                        {
                            MMSVCSServiceProviderOrderNumber = hdLeadNumber,
                            SFIMVendor = _providerId,
                            SFIWorkflowOnlyStatus = "Received by SP",
                            Description = notes
                        }
                    }
                }
            }
        };

        using var req = new HttpRequestMessage(HttpMethod.Post, PoBatchUrl)
        {
            Content = JsonContent.Create(body, options: _json)
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();
    }
}
