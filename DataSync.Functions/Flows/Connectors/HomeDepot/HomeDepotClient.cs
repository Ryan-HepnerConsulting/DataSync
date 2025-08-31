using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DataSync.Functions.Flows._shared;
using DataSync.Functions.Flows.connectors.HomeDepot.Models;

namespace DataSync.Functions.Flows.connectors.HomeDepot;

public sealed class HomeDepotClient
{
    private readonly HttpClient _http = new HttpClient();

    private static readonly string TokenUrl      = "https://api.hs.homedepot.com/iconx/v1/auth/accesstoken";
    private static readonly string LeadLookupUrl = "https://api.hs.homedepot.com/iconx/v1/leads/lookup";
    private static readonly string PoBatchUrl    = "https://api.hs.homedepot.com/iconx/v1/leads/pobatch";

    private static DateTime? LastLookupWatermark = null;
    private static string VendorId = "PV892345";

    private readonly string _tenantId;

    // OAuth (for appToken)
    private readonly string _oauthClientId;
    private readonly string _oauthClientSecret;
    private string? _cachedToken;
    private DateTimeOffset _tokenExpiryUtc;

    private static readonly JsonSerializerOptions _json =
        new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true, PropertyNamingPolicy = null,   // keep exact case
            DictionaryKeyPolicy = null,    // keep dictionary keys exact
            WriteIndented = true           // optional, makes it pretty
        };

    public HomeDepotClient(string tenantId, HttpClient? http = null)
    {
        _tenantId = tenantId;

        // TODO: load these from Key Vault
        _oauthClientId     = "HvKDpzsPxZ7coojqyxbtaN9g0MpAcYDO";
        _oauthClientSecret = "N6xil9R6IGZzW06z";
    }

    // ========== OAuth (for appToken) ==========
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

        using var req = new HttpRequestMessage(HttpMethod.Get, $"{TokenUrl}?grant_type=client_credentials");
        req.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_oauthClientId}:{_oauthClientSecret}")));

        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        var payload = await resp.Content.ReadFromJsonAsync<TokenResponse>(_json, ct)
                      ?? throw new InvalidOperationException("Failed to acquire Home Depot OAuth token.");

        _cachedToken    = payload.AccessToken;
        _tokenExpiryUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, payload.ExpiresIn - 120));
        return _cachedToken!;
    }

    // ========== Lookup (already working) ==========
    public async IAsyncEnumerable<LeadLookupResponse.LeadHeader> GetLeadsSinceLastPullAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var appToken = await GetAccessTokenAsync(ct);
        var searchspec = LastLookupWatermark != null ? $"([Created] >= '{LastLookupWatermark:MM/dd/yyyy HH:mm:ss}')" : "";

        const int pageSize = 25;
        var start = 0;
        var last  = false;

        var handler = new SocketsHttpHandler { UseCookies = false };
        using var client = new HttpClient(handler)
        {
            DefaultRequestVersion = HttpVersion.Version11,
            DefaultVersionPolicy  = HttpVersionPolicy.RequestVersionExact
        };

        while (!last)
        {
            var json = $@"{{
              ""SFILEADLOOKUPWS_Input"": {{
                ""PageSize"": {pageSize},
                ""ListOfSfileadbows"": {{
                  ""Sfileadheaderws"": {{ ""Searchspec"": ""{searchspec.Replace("\"", "\\\"")}"" }}
                }},
                ""StartRowNum"": {start}
              }}
            }}";

            using var req = new HttpRequestMessage(HttpMethod.Post, LeadLookupUrl) { Version = HttpVersion.Version11 };
            req.Headers.TryAddWithoutValidation("appToken", appToken);
            req.Headers.Accept.Clear();
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var bytes = Encoding.UTF8.GetBytes(json);
            req.Content = new ByteArrayContent(bytes);
            req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            req.Content.Headers.ContentLength = bytes.Length;

            using var resp = await client.SendAsync(req, ct);
            var content = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
                throw new HttpRequestException($"Lead lookup failed ({(int)resp.StatusCode} {resp.ReasonPhrase}).\n{content}");

            var payload = JsonSerializer.Deserialize<LeadLookupResponse>(content, _json)
                          ?? throw new InvalidOperationException("Empty/invalid response payload.");

            var list = payload._Output?.ListOf?.Items ?? new List<LeadLookupResponse.LeadHeader>();
            foreach (var lead in list)
                yield return lead;

            last  = payload?._Output?.IsLastPage == true;
            start += pageSize;
        }
    }

    public async Task<List<LeadLookupResponse.LeadHeader>> GetLeadsSinceLastPullAsListAsync(CancellationToken ct = default)
    {
        var results = new List<LeadLookupResponse.LeadHeader>();
        await foreach (var l in GetLeadsSinceLastPullAsync(ct)) results.Add(l);
        return results;
    }

    // ========== pobatch (updates) ==========

    private HttpRequestMessage NewPoBatchRequest(string appToken, string jsonBody)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, PoBatchUrl) { Version = HttpVersion.Version11 };

        // Required headers: appToken + x-clientid; JSON content-type
        req.Headers.TryAddWithoutValidation("appToken", appToken);
        req.Headers.Accept.Clear();
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        req.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
        return req;
    }

    private async Task SubmitPoBatchAsync(IEnumerable<object> items, CancellationToken ct)
    {
        if (items is null) return;

        const int batchLimit = 10; // API limit
        var appToken = await GetAccessTokenAsync(ct);

        foreach (var chunk in items.Chunk(batchLimit))
        {
            var wrapper = new
            {
                SFILEADPOBATCHICONX_Input = new
                {
                    ListOfMmSvCsServiceProviderLeadInbound = new
                    {
                        MmSvCsServiceProviderLeadHeaderInbound = chunk
                    }
                }
            };

            var body = JsonSerializer.Serialize(wrapper, _json);
            using var req = NewPoBatchRequest(appToken, body);
            using var resp = await _http.SendAsync(req, ct);
            var text = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
                throw new HttpRequestException($"pobatch failed: {(int)resp.StatusCode} {resp.ReasonPhrase}\n{text}");
        }
    }

    // -------- Builders (ensure we include required header fields on every update) --------

    public static object BuildHeaderUpdate(
        string id,
        string referralStore4,
        string mvendor,
        string programGroup,
        string status,
        string? statusReason,
        string typeCode,
        LeadLookupResponse.LeadHeader echo // used to echo back core identity/location
    )
    {
        return new
        {
            Id = id,
            ContactFirstName = echo.ContactFirstName ?? "",
            ContactLastName  = echo.ContactLastName  ?? "",
            MMSVPreferredContactPhoneNumber = echo.MMSVPreferredContactPhoneNumber ?? echo.SFIContactHomePhone ?? "",
            MMSVSiteAddress   = echo.MMSVSiteAddress   ?? "",
            MMSVSiteCity      = echo.MMSVSiteCity      ?? "",
            MMSVSiteState     = echo.MMSVSiteState     ?? "",
            MMSVSitePostalCode= echo.MMSVSitePostalCode?? "",
            MMSVSiteCountry   = echo.MMSVSiteCountry   ?? "US",
            MMSVStoreNumber   = referralStore4,
            SFIMVendor        = mvendor,
            SFIProgramGroupNameUnconstrained = programGroup,
            SFIReferralStore  = referralStore4,
            SFIWorkflowOnlyStatus = status,                   // e.g., Acknowledged/Confirmed/Cancelled
            SFIStatusReason   = statusReason,                 // required for Cancelled etc.
            MMSVCSNeedAck     = "N",
            MMSVCSSubmitLeadFlag = "N",
            MMSVCSSVSTypeCode = typeCode,
            MainEmailAddress  = echo.MainEmailAddress ?? null
        };
    }

    public static object BuildAppointmentUpdate(
        string id,
        string referralStore4,
        string mvendor,
        string programGroup,
        string typeCode,
        string apptId,
        DateTime scheduleUtc,
        bool isReschedule,
        DateTime? originalScheduleUtc
    )
    {
        // IMPORTANT: child-only update; do not change header status here.
        var appt = new
        {
            Id = apptId,
            ScheduleDate = scheduleUtc.ToString("MM/dd/yyyy HH:mm:ss"),
            RescheduledFlag = isReschedule ? "Y" : "N",
            OriginalApptDate = isReschedule && originalScheduleUtc.HasValue
                ? originalScheduleUtc.Value.ToString("MM/dd/yyyy HH:mm:ss")
                : null,
            PreferredScheduleDate = scheduleUtc.ToString("MM/dd/yyyy HH:mm:ss")
        };

        return new
        {
            Id = id,
            SFIMVendor = mvendor,
            SFIReferralStore = referralStore4,
            SFIProgramGroupNameUnconstrained = programGroup,
            MMSVCSNeedAck = "N",
            MMSVCSSubmitLeadFlag = "N",
            MMSVCSSVSTypeCode = typeCode,
            ListOfMmSvCsServiceProviderAppointment = new
            {
                MmSvCsServiceProviderAppointment = new[] { appt }
            }
        };
    }

    // Public batch helpers
    public Task SubmitLeadHeaderBatchAsync(IEnumerable<object> headerItems, CancellationToken ct)
        => SubmitPoBatchAsync(headerItems, ct);

    public Task SubmitAppointmentBatchAsync(IEnumerable<object> apptItems, CancellationToken ct)
        => SubmitPoBatchAsync(apptItems, ct);
}
