using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

/// <summary>
/// Client for the eBay Fulfillment API (v1).
/// Covers order retrieval, shipping fulfillment creation, refunds, and payment dispute management.
/// </summary>
public class EbayFulfillmentClient
{
    private readonly EbayConfig _config;
    private readonly EbayOAuthClient _oauth;
    private readonly HttpClient _http;

    private string FulfillmentBase => $"{_config.BaseUrl}/sell/fulfillment/v1";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly string[] FulfillmentScopes =
    [
        "https://api.ebay.com/oauth/api_scope/sell.fulfillment",
        "https://api.ebay.com/oauth/api_scope/sell.fulfillment.readonly",
        "https://api.ebay.com/oauth/api_scope/sell.payment.dispute"
    ];

    public EbayFulfillmentClient(EbayConfig config, EbayOAuthClient oauth, HttpClient? http = null)
    {
        _config = config;
        _oauth = oauth;
        _http = http ?? new HttpClient();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Orders
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Retrieves detailed information on a specific order.</summary>
    public async Task<EbayOrder> GetOrderAsync(string orderId, CancellationToken ct = default)
    {
        var url = $"{FulfillmentBase}/order/{Uri.EscapeDataString(orderId)}";
        var request = await BuildRequestAsync(HttpMethod.Get, url, ct: ct);
        return await SendAsync<EbayOrder>(request, ct);
    }

    /// <summary>
    /// Retrieves orders matching the given filters (paged).
    /// </summary>
    /// <param name="filter">
    /// Optional filter string using eBay's filter syntax.
    /// Examples:
    ///   "lastmodifieddate:[2024-01-01T00:00:00Z..]"
    ///   "orderfulfillmentstatus:{NOT_STARTED|IN_PROGRESS}"
    ///   "paymentstatus:{PAID}"
    /// </param>
    public async Task<EbayGetOrdersResponse> GetOrdersAsync(
        string? filter = null,
        string? fieldGroups = null,
        int limit = 50,
        int offset = 0,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>
        {
            $"limit={limit}",
            $"offset={offset}"
        };
        if (!string.IsNullOrWhiteSpace(filter))
            queryParams.Add($"filter={Uri.EscapeDataString(filter)}");
        if (!string.IsNullOrWhiteSpace(fieldGroups))
            queryParams.Add($"fieldgroups={Uri.EscapeDataString(fieldGroups)}");

        var url = $"{FulfillmentBase}/order?{string.Join("&", queryParams)}";
        var request = await BuildRequestAsync(HttpMethod.Get, url, ct: ct);
        return await SendAsync<EbayGetOrdersResponse>(request, ct);
    }

    /// <summary>
    /// Issues a full or partial refund to a buyer on behalf of the seller.
    /// </summary>
    public async Task IssueRefundAsync(
        string orderId,
        EbayIssueRefundRequest refundRequest,
        CancellationToken ct = default)
    {
        var url = $"{FulfillmentBase}/order/{Uri.EscapeDataString(orderId)}/issue_refund";
        var request = await BuildRequestAsync(HttpMethod.Post, url, refundRequest, ct);
        await SendAsync(request, ct);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Shipping Fulfillment
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a shipping fulfillment for one or more line items, recording the tracking info.
    /// This marks the items as shipped.
    /// </summary>
    public async Task<string> CreateShippingFulfillmentAsync(
        string orderId,
        EbayShippingFulfillmentRequest fulfillment,
        CancellationToken ct = default)
    {
        var url = $"{FulfillmentBase}/order/{Uri.EscapeDataString(orderId)}/shipping_fulfillment";
        var request = await BuildRequestAsync(HttpMethod.Post, url, fulfillment, ct);
        var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            ThrowApiException((int)response.StatusCode, body);

        // eBay returns the fulfillmentId in the Location header for 201 Created
        var locationHeader = response.Headers.Location?.ToString() ?? "";
        var fulfillmentId = locationHeader.Split('/').LastOrDefault() ?? "";
        return fulfillmentId;
    }

    /// <summary>Retrieves details of a specific shipping fulfillment.</summary>
    public async Task<EbayShippingFulfillment> GetShippingFulfillmentAsync(
        string orderId,
        string fulfillmentId,
        CancellationToken ct = default)
    {
        var url = $"{FulfillmentBase}/order/{Uri.EscapeDataString(orderId)}/shipping_fulfillment/{Uri.EscapeDataString(fulfillmentId)}";
        var request = await BuildRequestAsync(HttpMethod.Get, url, ct: ct);
        return await SendAsync<EbayShippingFulfillment>(request, ct);
    }

    /// <summary>Retrieves all shipping fulfillments for an order.</summary>
    public async Task<EbayGetShippingFulfillmentsResponse> GetShippingFulfillmentsAsync(
        string orderId,
        CancellationToken ct = default)
    {
        var url = $"{FulfillmentBase}/order/{Uri.EscapeDataString(orderId)}/shipping_fulfillment";
        var request = await BuildRequestAsync(HttpMethod.Get, url, ct: ct);
        return await SendAsync<EbayGetShippingFulfillmentsResponse>(request, ct);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Payment Disputes
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Retrieves full details on a specific payment dispute.</summary>
    public async Task<EbayPaymentDispute> GetPaymentDisputeAsync(
        string paymentDisputeId,
        CancellationToken ct = default)
    {
        var url = $"{FulfillmentBase}/payment_dispute/{Uri.EscapeDataString(paymentDisputeId)}";
        var request = await BuildRequestAsync(HttpMethod.Get, url, ct: ct);
        return await SendAsync<EbayPaymentDispute>(request, ct);
    }

    /// <summary>Retrieves payment disputes based on search criteria (paged).</summary>
    /// <param name="filter">
    /// Filter string.  Examples:
    ///   "paymentDisputeStatus:{OPEN}"
    ///   "openDate:[2024-01-01T00:00:00Z..]"
    /// </param>
    public async Task<EbayGetPaymentDisputeSummariesResponse> GetPaymentDisputeSummariesAsync(
        string? filter = null,
        int limit = 50,
        int offset = 0,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>
        {
            $"limit={limit}",
            $"offset={offset}"
        };
        if (!string.IsNullOrWhiteSpace(filter))
            queryParams.Add($"filter={Uri.EscapeDataString(filter)}");

        var url = $"{FulfillmentBase}/payment_dispute_summary?{string.Join("&", queryParams)}";
        var request = await BuildRequestAsync(HttpMethod.Get, url, ct: ct);
        return await SendAsync<EbayGetPaymentDisputeSummariesResponse>(request, ct);
    }

    /// <summary>Contests a payment dispute, indicating the seller disagrees with the claim.</summary>
    public async Task ContestPaymentDisputeAsync(
        string paymentDisputeId,
        EbayContestDisputeRequest payload,
        CancellationToken ct = default)
    {
        var url = $"{FulfillmentBase}/payment_dispute/{Uri.EscapeDataString(paymentDisputeId)}/contest";
        var request = await BuildRequestAsync(HttpMethod.Post, url, payload, ct);
        await SendAsync(request, ct);
    }

    /// <summary>Accepts a payment dispute, indicating the seller agrees with the claim.</summary>
    public async Task AcceptPaymentDisputeAsync(
        string paymentDisputeId,
        EbayAcceptDisputeRequest payload,
        CancellationToken ct = default)
    {
        var url = $"{FulfillmentBase}/payment_dispute/{Uri.EscapeDataString(paymentDisputeId)}/accept";
        var request = await BuildRequestAsync(HttpMethod.Post, url, payload, ct);
        await SendAsync(request, ct);
    }

    /// <summary>Retrieves the activity log for a payment dispute.</summary>
    public async Task<string> GetDisputeActivitiesAsync(
        string paymentDisputeId,
        CancellationToken ct = default)
    {
        var url = $"{FulfillmentBase}/payment_dispute/{Uri.EscapeDataString(paymentDisputeId)}/activity";
        var request = await BuildRequestAsync(HttpMethod.Get, url, ct: ct);
        var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) ThrowApiException((int)response.StatusCode, body);
        return body;
    }

    /// <summary>Uploads a binary evidence file for a contested payment dispute.</summary>
    public async Task<string> UploadEvidenceFileAsync(
        string paymentDisputeId,
        byte[] fileBytes,
        string fileName,
        string mediaType = "image/jpeg",
        CancellationToken ct = default)
    {
        var url = $"{FulfillmentBase}/payment_dispute/{Uri.EscapeDataString(paymentDisputeId)}/upload_evidence_file";
        var token = await _oauth.GetUserTokenAsync(FulfillmentScopes, ct);

        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(mediaType);
        content.Add(fileContent, "file", fileName);

        var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-EBAY-C-MARKETPLACE-ID", _config.MarketplaceId);

        var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) ThrowApiException((int)response.StatusCode, body);
        return body; // Contains fileId
    }

    /// <summary>Adds an evidence set to a contested payment dispute.</summary>
    public async Task<string> AddEvidenceAsync(
        string paymentDisputeId,
        EbayAddEvidenceRequest payload,
        CancellationToken ct = default)
    {
        var url = $"{FulfillmentBase}/payment_dispute/{Uri.EscapeDataString(paymentDisputeId)}/add_evidence";
        var request = await BuildRequestAsync(HttpMethod.Post, url, payload, ct);
        var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) ThrowApiException((int)response.StatusCode, body);
        return body; // Contains evidenceId
    }

    /// <summary>Updates an existing evidence set for a contested payment dispute.</summary>
    public async Task UpdateEvidenceAsync(
        string paymentDisputeId,
        EbayAddEvidenceRequest payload,
        CancellationToken ct = default)
    {
        var url = $"{FulfillmentBase}/payment_dispute/{Uri.EscapeDataString(paymentDisputeId)}/update_evidence";
        var request = await BuildRequestAsync(HttpMethod.Post, url, payload, ct);
        await SendAsync(request, ct);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Internal helpers
    // ──────────────────────────────────────────────────────────────────────────

    private async Task<HttpRequestMessage> BuildRequestAsync(
        HttpMethod method,
        string url,
        object? body = null,
        CancellationToken ct = default)
    {
        var token = await _oauth.GetUserTokenAsync(FulfillmentScopes, ct);
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-EBAY-C-MARKETPLACE-ID", _config.MarketplaceId);

        if (body != null)
        {
            var json = JsonSerializer.Serialize(body, _jsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        return request;
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request, CancellationToken ct)
    {
        var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) ThrowApiException((int)response.StatusCode, body);

        return JsonSerializer.Deserialize<T>(body, _jsonOptions)
               ?? throw new InvalidOperationException("Unexpected null response body.");
    }

    private async Task SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) ThrowApiException((int)response.StatusCode, body);
    }

    private static void ThrowApiException(int statusCode, string body)
    {
        List<EbayError>? errors = null;
        try
        {
            var errorResponse = JsonSerializer.Deserialize<EbayErrorResponse>(body);
            errors = errorResponse?.Errors;
        }
        catch { /* ignore */ }

        throw new EbayApiException(statusCode, $"eBay API error ({statusCode}): {body}", errors);
    }
}
