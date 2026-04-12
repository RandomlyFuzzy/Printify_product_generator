using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

/// <summary>
/// Client for the eBay Account API (v1).
/// Covers fulfillment policies, return policies, payment policies, seller privileges,
/// and access to the Finances API for payouts and transactions.
/// </summary>
public class EbayAccountClient
{
    private readonly EbayConfig _config;
    private readonly EbayOAuthClient _oauth;
    private readonly HttpClient _http;

    private string AccountBase => $"{_config.BaseUrl}/sell/account/v1";
    private string FinancesBase => $"{_config.BaseUrl}/sell/finances/v1";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly string[] AccountScopes =
    [
        "https://api.ebay.com/oauth/api_scope/sell.account",
        "https://api.ebay.com/oauth/api_scope/sell.account.readonly",
        "https://api.ebay.com/oauth/api_scope/sell.finances"
    ];

    public EbayAccountClient(EbayConfig config, EbayOAuthClient oauth, HttpClient? http = null)
    {
        _config = config;
        _oauth = oauth;
        _http = http ?? new HttpClient();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Seller Eligibility / Privileges
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Retrieves the seller's eligibility and selling restrictions.</summary>
    public async Task<EbaySellerEligibilityResponse> GetSellerEligibilityAsync(
        CancellationToken ct = default)
    {
        // Uses program name "SELLER_PROFILE" for general eligibility
        var url = $"{AccountBase}/privilege";
        var request = await BuildRequestAsync(HttpMethod.Get, url, ct: ct);
        return await SendAsync<EbaySellerEligibilityResponse>(request, ct);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Fulfillment Policies
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Creates a new fulfillment (shipping) policy.</summary>
    public async Task<EbayFulfillmentPolicyResponse> CreateFulfillmentPolicyAsync(
        EbayFulfillmentPolicy policy,
        CancellationToken ct = default)
    {
        var url = $"{AccountBase}/fulfillment_policy";
        var request = await BuildRequestAsync(HttpMethod.Post, url, policy, ct);
        return await SendAsync<EbayFulfillmentPolicyResponse>(request, ct);
    }

    /// <summary>Retrieves a fulfillment policy by ID.</summary>
    public async Task<EbayFulfillmentPolicy> GetFulfillmentPolicyAsync(
        string fulfillmentPolicyId,
        CancellationToken ct = default)
    {
        var url = $"{AccountBase}/fulfillment_policy/{Uri.EscapeDataString(fulfillmentPolicyId)}";
        var request = await BuildRequestAsync(HttpMethod.Get, url, ct: ct);
        return await SendAsync<EbayFulfillmentPolicy>(request, ct);
    }

    /// <summary>Retrieves all fulfillment policies for the specified marketplace.</summary>
    public async Task<EbayGetFulfillmentPoliciesResponse> GetFulfillmentPoliciesAsync(
        string? marketplaceId = null,
        CancellationToken ct = default)
    {
        var mid = marketplaceId ?? _config.MarketplaceId;
        var url = $"{AccountBase}/fulfillment_policy?marketplace_id={Uri.EscapeDataString(mid)}";
        var request = await BuildRequestAsync(HttpMethod.Get, url, ct: ct);
        return await SendAsync<EbayGetFulfillmentPoliciesResponse>(request, ct);
    }

    /// <summary>Updates an existing fulfillment policy.</summary>
    public async Task<EbayFulfillmentPolicyResponse> UpdateFulfillmentPolicyAsync(
        string fulfillmentPolicyId,
        EbayFulfillmentPolicy policy,
        CancellationToken ct = default)
    {
        var url = $"{AccountBase}/fulfillment_policy/{Uri.EscapeDataString(fulfillmentPolicyId)}";
        var request = await BuildRequestAsync(HttpMethod.Put, url, policy, ct);
        return await SendAsync<EbayFulfillmentPolicyResponse>(request, ct);
    }

    /// <summary>Deletes a fulfillment policy.</summary>
    public async Task DeleteFulfillmentPolicyAsync(
        string fulfillmentPolicyId,
        CancellationToken ct = default)
    {
        var url = $"{AccountBase}/fulfillment_policy/{Uri.EscapeDataString(fulfillmentPolicyId)}";
        var request = await BuildRequestAsync(HttpMethod.Delete, url, ct: ct);
        await SendAsync(request, ct);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Return Policies
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Creates a new return policy.</summary>
    public async Task<EbayReturnPolicyResponse> CreateReturnPolicyAsync(
        EbayReturnPolicy policy,
        CancellationToken ct = default)
    {
        var url = $"{AccountBase}/return_policy";
        var request = await BuildRequestAsync(HttpMethod.Post, url, policy, ct);
        return await SendAsync<EbayReturnPolicyResponse>(request, ct);
    }

    /// <summary>Retrieves a return policy by ID.</summary>
    public async Task<EbayReturnPolicy> GetReturnPolicyAsync(
        string returnPolicyId,
        CancellationToken ct = default)
    {
        var url = $"{AccountBase}/return_policy/{Uri.EscapeDataString(returnPolicyId)}";
        var request = await BuildRequestAsync(HttpMethod.Get, url, ct: ct);
        return await SendAsync<EbayReturnPolicy>(request, ct);
    }

    /// <summary>Retrieves all return policies for the specified marketplace.</summary>
    public async Task<EbayGetReturnPoliciesResponse> GetReturnPoliciesAsync(
        string? marketplaceId = null,
        CancellationToken ct = default)
    {
        var mid = marketplaceId ?? _config.MarketplaceId;
        var url = $"{AccountBase}/return_policy?marketplace_id={Uri.EscapeDataString(mid)}";
        var request = await BuildRequestAsync(HttpMethod.Get, url, ct: ct);
        return await SendAsync<EbayGetReturnPoliciesResponse>(request, ct);
    }

    /// <summary>Updates an existing return policy.</summary>
    public async Task<EbayReturnPolicyResponse> UpdateReturnPolicyAsync(
        string returnPolicyId,
        EbayReturnPolicy policy,
        CancellationToken ct = default)
    {
        var url = $"{AccountBase}/return_policy/{Uri.EscapeDataString(returnPolicyId)}";
        var request = await BuildRequestAsync(HttpMethod.Put, url, policy, ct);
        return await SendAsync<EbayReturnPolicyResponse>(request, ct);
    }

    /// <summary>Deletes a return policy.</summary>
    public async Task DeleteReturnPolicyAsync(string returnPolicyId, CancellationToken ct = default)
    {
        var url = $"{AccountBase}/return_policy/{Uri.EscapeDataString(returnPolicyId)}";
        var request = await BuildRequestAsync(HttpMethod.Delete, url, ct: ct);
        await SendAsync(request, ct);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Payment Policies
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Creates a new payment policy.</summary>
    public async Task<EbayPaymentPolicyResponse> CreatePaymentPolicyAsync(
        EbayPaymentPolicy policy,
        CancellationToken ct = default)
    {
        var url = $"{AccountBase}/payment_policy";
        var request = await BuildRequestAsync(HttpMethod.Post, url, policy, ct);
        return await SendAsync<EbayPaymentPolicyResponse>(request, ct);
    }

    /// <summary>Retrieves a payment policy by ID.</summary>
    public async Task<EbayPaymentPolicy> GetPaymentPolicyAsync(
        string paymentPolicyId,
        CancellationToken ct = default)
    {
        var url = $"{AccountBase}/payment_policy/{Uri.EscapeDataString(paymentPolicyId)}";
        var request = await BuildRequestAsync(HttpMethod.Get, url, ct: ct);
        return await SendAsync<EbayPaymentPolicy>(request, ct);
    }

    /// <summary>Retrieves all payment policies for the specified marketplace.</summary>
    public async Task<EbayGetPaymentPoliciesResponse> GetPaymentPoliciesAsync(
        string? marketplaceId = null,
        CancellationToken ct = default)
    {
        var mid = marketplaceId ?? _config.MarketplaceId;
        var url = $"{AccountBase}/payment_policy?marketplace_id={Uri.EscapeDataString(mid)}";
        var request = await BuildRequestAsync(HttpMethod.Get, url, ct: ct);
        return await SendAsync<EbayGetPaymentPoliciesResponse>(request, ct);
    }

    /// <summary>Updates an existing payment policy.</summary>
    public async Task<EbayPaymentPolicyResponse> UpdatePaymentPolicyAsync(
        string paymentPolicyId,
        EbayPaymentPolicy policy,
        CancellationToken ct = default)
    {
        var url = $"{AccountBase}/payment_policy/{Uri.EscapeDataString(paymentPolicyId)}";
        var request = await BuildRequestAsync(HttpMethod.Put, url, policy, ct);
        return await SendAsync<EbayPaymentPolicyResponse>(request, ct);
    }

    /// <summary>Deletes a payment policy.</summary>
    public async Task DeletePaymentPolicyAsync(
        string paymentPolicyId,
        CancellationToken ct = default)
    {
        var url = $"{AccountBase}/payment_policy/{Uri.EscapeDataString(paymentPolicyId)}";
        var request = await BuildRequestAsync(HttpMethod.Delete, url, ct: ct);
        await SendAsync(request, ct);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Programs (opt-in / opt-out)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Retrieves a list of all eBay programs in which the seller is opted in.</summary>
    public async Task<string> GetOptedInProgramsAsync(CancellationToken ct = default)
    {
        var url = $"{AccountBase}/program/get_opted_in_programs";
        var request = await BuildRequestAsync(HttpMethod.Get, url, ct: ct);
        var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) ThrowApiException((int)response.StatusCode, body);
        return body;
    }

    /// <summary>Opts the seller in to the specified eBay program.</summary>
    public async Task OptInToProgramAsync(string programType, CancellationToken ct = default)
    {
        var url = $"{AccountBase}/program/opt_in";
        var request = await BuildRequestAsync(HttpMethod.Post, url, new { programType }, ct);
        await SendAsync(request, ct);
    }

    /// <summary>Opts the seller out of the specified eBay program.</summary>
    public async Task OptOutOfProgramAsync(string programType, CancellationToken ct = default)
    {
        var url = $"{AccountBase}/program/opt_out";
        var request = await BuildRequestAsync(HttpMethod.Post, url, new { programType }, ct);
        await SendAsync(request, ct);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Finances API – Payouts
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Retrieves seller payouts (paged).
    /// </summary>
    /// <param name="filter">
    /// Optional eBay filter string.  Examples:
    ///   "payoutStatus:{SUCCEEDED}"
    ///   "payoutDate:[2024-01-01T00:00:00Z..2024-12-31T23:59:59Z]"
    /// </param>
    public async Task<EbayPayoutsResponse> GetPayoutsAsync(
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

        var url = $"{FinancesBase}/payout?{string.Join("&", queryParams)}";
        var request = await BuildRequestAsync(HttpMethod.Get, url, ct: ct);
        return await SendAsync<EbayPayoutsResponse>(request, ct);
    }

    /// <summary>Retrieves details of a single payout.</summary>
    public async Task<EbayPayout> GetPayoutAsync(string payoutId, CancellationToken ct = default)
    {
        var url = $"{FinancesBase}/payout/{Uri.EscapeDataString(payoutId)}";
        var request = await BuildRequestAsync(HttpMethod.Get, url, ct: ct);
        return await SendAsync<EbayPayout>(request, ct);
    }

    /// <summary>
    /// Retrieves monetary transactions (sales, refunds, credits, fees, etc.) for the seller (paged).
    /// </summary>
    /// <param name="filter">
    /// Optional eBay filter string.  Examples:
    ///   "transactionType:{SALE}"
    ///   "transactionDate:[2024-01-01T00:00:00Z..]"
    ///   "transactionStatus:{FUNDS_AVAILABLE_FOR_PAYOUT}"
    /// </param>
    public async Task<EbayTransactionsResponse> GetTransactionsAsync(
        string? filter = null,
        string? sort = null,
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
        if (!string.IsNullOrWhiteSpace(sort))
            queryParams.Add($"sort={Uri.EscapeDataString(sort)}");

        var url = $"{FinancesBase}/transaction?{string.Join("&", queryParams)}";
        var request = await BuildRequestAsync(HttpMethod.Get, url, ct: ct);
        return await SendAsync<EbayTransactionsResponse>(request, ct);
    }

    /// <summary>Retrieves a summary of monetary transactions for the seller account.</summary>
    public async Task<string> GetTransactionSummaryAsync(
        string? filter = null,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrWhiteSpace(filter))
            queryParams.Add($"filter={Uri.EscapeDataString(filter)}");

        var query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
        var url = $"{FinancesBase}/transaction_summary{query}";
        var request = await BuildRequestAsync(HttpMethod.Get, url, ct: ct);
        var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode) ThrowApiException((int)response.StatusCode, body);
        return body;
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
        var token = await _oauth.GetUserTokenAsync(AccountScopes, ct);
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
