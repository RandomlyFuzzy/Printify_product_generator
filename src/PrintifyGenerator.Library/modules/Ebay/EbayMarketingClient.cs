using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

// ──────────────────────────────────────────────────────────────────────────────
// Marketing API data structures
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>An eBay item promotion (sale, order discount, etc.).</summary>
public class EbayPromotion
{
    [JsonPropertyName("promotionId")]
    public string? PromotionId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("promotionType")]
    public string PromotionType { get; set; } = ""; // CODED_COUPON, MARKDOWN_SALE, ORDER_DISCOUNT, VOLUME_DISCOUNT

    [JsonPropertyName("status")]
    public string? Status { get; set; } // ACTIVE, DRAFT, ENDED, SCHEDULED, SUSPENDED

    [JsonPropertyName("startDate")]
    public string? StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public string? EndDate { get; set; }

    [JsonPropertyName("marketplaceId")]
    public string? MarketplaceId { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("priority")]
    public string? Priority { get; set; } // LOWEST_PRIORITY … HIGHEST_PRIORITY
}

public class EbayGetPromotionsResponse
{
    [JsonPropertyName("promotions")]
    public List<EbayPromotion>? Promotions { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("href")]
    public string? Href { get; set; }

    [JsonPropertyName("next")]
    public string? Next { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }
}

/// <summary>Promoted Listings Ad (standard or advanced).</summary>
public class EbayAdRequest
{
    [JsonPropertyName("listingId")]
    public string ListingId { get; set; } = "";

    [JsonPropertyName("bidPercentage")]
    public string? BidPercentage { get; set; } // "2.5" means 2.5%
}

public class EbayAdGroupRequest
{
    [JsonPropertyName("ads")]
    public List<EbayAdRequest> Ads { get; set; } = [];

    [JsonPropertyName("campaignId")]
    public string? CampaignId { get; set; }
}

public class EbayAdResponse
{
    [JsonPropertyName("adId")]
    public string? AdId { get; set; }

    [JsonPropertyName("listingId")]
    public string? ListingId { get; set; }

    [JsonPropertyName("bidPercentage")]
    public string? BidPercentage { get; set; }

    [JsonPropertyName("fundingModel")]
    public string? FundingModel { get; set; }
}

public class EbayGetAdsResponse
{
    [JsonPropertyName("ads")]
    public List<EbayAdResponse>? Ads { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("href")]
    public string? Href { get; set; }

    [JsonPropertyName("next")]
    public string? Next { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }
}

/// <summary>Promoted Listings campaign.</summary>
public class EbayCampaignRequest
{
    [JsonPropertyName("campaignName")]
    public string CampaignName { get; set; } = "";

    [JsonPropertyName("startDate")]
    public string StartDate { get; set; } = "";

    [JsonPropertyName("endDate")]
    public string? EndDate { get; set; }

    [JsonPropertyName("fundingStrategy")]
    public EbayFundingStrategy? FundingStrategy { get; set; }

    [JsonPropertyName("marketplaceId")]
    public string MarketplaceId { get; set; } = "EBAY_US";
}

public class EbayFundingStrategy
{
    [JsonPropertyName("bidPercentage")]
    public string? BidPercentage { get; set; }

    [JsonPropertyName("fundingModel")]
    public string FundingModel { get; set; } = "COST_PER_SALE"; // COST_PER_SALE, COST_PER_CLICK
}

public class EbayCampaignResponse : EbayCampaignRequest
{
    [JsonPropertyName("campaignId")]
    public string? CampaignId { get; set; }

    [JsonPropertyName("campaignStatus")]
    public string? CampaignStatus { get; set; } // RUNNING, PAUSED, ENDED, SCHEDULED
}

public class EbayGetCampaignsResponse
{
    [JsonPropertyName("campaigns")]
    public List<EbayCampaignResponse>? Campaigns { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("href")]
    public string? Href { get; set; }

    [JsonPropertyName("next")]
    public string? Next { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }
}

// ──────────────────────────────────────────────────────────────────────────────
// Marketing Client
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Client for the eBay Marketing API (v1).
/// Covers Promoted Listings campaigns and item/order promotions.
/// </summary>
public class EbayMarketingClient
{
    private readonly EbayConfig _config;
    private readonly EbayOAuthClient _oauth;
    private readonly HttpClient _http;

    private string MarketingBase => $"{_config.BaseUrl}/sell/marketing/v1";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly string[] MarketingScopes =
    [
        "https://api.ebay.com/oauth/api_scope/sell.marketing",
        "https://api.ebay.com/oauth/api_scope/sell.marketing.readonly"
    ];

    public EbayMarketingClient(EbayConfig config, EbayOAuthClient oauth, HttpClient? http = null)
    {
        _config = config;
        _oauth = oauth;
        _http = http ?? new HttpClient();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Promotions (Item / Order / Volume / Coupon)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Retrieves all promotions for the seller (paged).</summary>
    public async Task<EbayGetPromotionsResponse> GetPromotionsAsync(
        string? promotionType = null,
        string? status = null,
        string? sort = null,
        int limit = 200,
        int offset = 0,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>
        {
            $"marketplace_id={Uri.EscapeDataString(_config.MarketplaceId)}",
            $"limit={limit}",
            $"offset={offset}"
        };
        if (!string.IsNullOrWhiteSpace(promotionType))
            queryParams.Add($"promotion_type={Uri.EscapeDataString(promotionType)}");
        if (!string.IsNullOrWhiteSpace(status))
            queryParams.Add($"q={Uri.EscapeDataString(status)}");
        if (!string.IsNullOrWhiteSpace(sort))
            queryParams.Add($"sort={Uri.EscapeDataString(sort)}");

        var url = $"{MarketingBase}/promotion?{string.Join("&", queryParams)}";
        var request = await BuildRequestAsync(HttpMethod.Get, url, ct: ct);
        return await SendAsync<EbayGetPromotionsResponse>(request, ct);
    }

    /// <summary>Retrieves details of a single promotion.</summary>
    public async Task<EbayPromotion> GetPromotionAsync(
        string promotionId,
        CancellationToken ct = default)
    {
        var url = $"{MarketingBase}/promotion/{Uri.EscapeDataString(promotionId)}";
        var request = await BuildRequestAsync(HttpMethod.Get, url, ct: ct);
        return await SendAsync<EbayPromotion>(request, ct);
    }

    /// <summary>Pauses an active promotion.</summary>
    public async Task PausePromotionAsync(string promotionId, CancellationToken ct = default)
    {
        var url = $"{MarketingBase}/promotion/{Uri.EscapeDataString(promotionId)}/pause";
        var request = await BuildRequestAsync(HttpMethod.Post, url, ct: ct);
        await SendAsync(request, ct);
    }

    /// <summary>Resumes a paused promotion.</summary>
    public async Task ResumePromotionAsync(string promotionId, CancellationToken ct = default)
    {
        var url = $"{MarketingBase}/promotion/{Uri.EscapeDataString(promotionId)}/resume";
        var request = await BuildRequestAsync(HttpMethod.Post, url, ct: ct);
        await SendAsync(request, ct);
    }

    /// <summary>Deletes a promotion.</summary>
    public async Task DeletePromotionAsync(string promotionId, CancellationToken ct = default)
    {
        var url = $"{MarketingBase}/promotion/{Uri.EscapeDataString(promotionId)}";
        var request = await BuildRequestAsync(HttpMethod.Delete, url, ct: ct);
        await SendAsync(request, ct);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Promoted Listings – Campaigns
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Creates a new Promoted Listings campaign.</summary>
    public async Task<EbayCampaignResponse> CreateCampaignAsync(
        EbayCampaignRequest campaign,
        CancellationToken ct = default)
    {
        var url = $"{MarketingBase}/ad_campaign";
        var request = await BuildRequestAsync(HttpMethod.Post, url, campaign, ct);
        return await SendAsync<EbayCampaignResponse>(request, ct);
    }

    /// <summary>Retrieves all Promoted Listings campaigns (paged).</summary>
    public async Task<EbayGetCampaignsResponse> GetCampaignsAsync(
        string? status = null,
        int limit = 200,
        int offset = 0,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>
        {
            $"limit={limit}",
            $"offset={offset}"
        };
        if (!string.IsNullOrWhiteSpace(status))
            queryParams.Add($"campaign_status={Uri.EscapeDataString(status)}");

        var url = $"{MarketingBase}/ad_campaign?{string.Join("&", queryParams)}";
        var request = await BuildRequestAsync(HttpMethod.Get, url, ct: ct);
        return await SendAsync<EbayGetCampaignsResponse>(request, ct);
    }

    /// <summary>Retrieves a single campaign by ID.</summary>
    public async Task<EbayCampaignResponse> GetCampaignAsync(
        string campaignId,
        CancellationToken ct = default)
    {
        var url = $"{MarketingBase}/ad_campaign/{Uri.EscapeDataString(campaignId)}";
        var request = await BuildRequestAsync(HttpMethod.Get, url, ct: ct);
        return await SendAsync<EbayCampaignResponse>(request, ct);
    }

    /// <summary>Updates the name or dates of a campaign.</summary>
    public async Task UpdateCampaignNameAsync(
        string campaignId,
        string name,
        CancellationToken ct = default)
    {
        var url = $"{MarketingBase}/ad_campaign/{Uri.EscapeDataString(campaignId)}/update_campaign_identification";
        var request = await BuildRequestAsync(HttpMethod.Post, url, new { campaignName = name }, ct);
        await SendAsync(request, ct);
    }

    /// <summary>Pauses a running campaign.</summary>
    public async Task PauseCampaignAsync(string campaignId, CancellationToken ct = default)
    {
        var url = $"{MarketingBase}/ad_campaign/{Uri.EscapeDataString(campaignId)}/pause";
        var request = await BuildRequestAsync(HttpMethod.Post, url, ct: ct);
        await SendAsync(request, ct);
    }

    /// <summary>Resumes a paused campaign.</summary>
    public async Task ResumeCampaignAsync(string campaignId, CancellationToken ct = default)
    {
        var url = $"{MarketingBase}/ad_campaign/{Uri.EscapeDataString(campaignId)}/resume";
        var request = await BuildRequestAsync(HttpMethod.Post, url, ct: ct);
        await SendAsync(request, ct);
    }

    /// <summary>Ends a running or paused campaign.</summary>
    public async Task EndCampaignAsync(string campaignId, CancellationToken ct = default)
    {
        var url = $"{MarketingBase}/ad_campaign/{Uri.EscapeDataString(campaignId)}/end";
        var request = await BuildRequestAsync(HttpMethod.Post, url, ct: ct);
        await SendAsync(request, ct);
    }

    /// <summary>Deletes a campaign.</summary>
    public async Task DeleteCampaignAsync(string campaignId, CancellationToken ct = default)
    {
        var url = $"{MarketingBase}/ad_campaign/{Uri.EscapeDataString(campaignId)}";
        var request = await BuildRequestAsync(HttpMethod.Delete, url, ct: ct);
        await SendAsync(request, ct);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Promoted Listings – Ads (per campaign)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Creates one or more ads under a campaign.</summary>
    public async Task<EbayGetAdsResponse> CreateAdsAsync(
        string campaignId,
        List<EbayAdRequest> ads,
        CancellationToken ct = default)
    {
        var url = $"{MarketingBase}/ad_campaign/{Uri.EscapeDataString(campaignId)}/ad/create_ads_by_listing_id";
        var request = await BuildRequestAsync(HttpMethod.Post, url, new { ads }, ct);
        return await SendAsync<EbayGetAdsResponse>(request, ct);
    }

    /// <summary>Retrieves all ads belonging to a campaign (paged).</summary>
    public async Task<EbayGetAdsResponse> GetAdsAsync(
        string campaignId,
        int limit = 200,
        int offset = 0,
        CancellationToken ct = default)
    {
        var url = $"{MarketingBase}/ad_campaign/{Uri.EscapeDataString(campaignId)}/ad" +
                  $"?limit={limit}&offset={offset}";
        var request = await BuildRequestAsync(HttpMethod.Get, url, ct: ct);
        return await SendAsync<EbayGetAdsResponse>(request, ct);
    }

    /// <summary>Deletes one or more ads from a campaign by listing IDs.</summary>
    public async Task DeleteAdsAsync(
        string campaignId,
        List<string> listingIds,
        CancellationToken ct = default)
    {
        var url = $"{MarketingBase}/ad_campaign/{Uri.EscapeDataString(campaignId)}/ad/delete_ads_by_listing_id";
        var request = await BuildRequestAsync(HttpMethod.Post, url, new { listingIds }, ct);
        await SendAsync(request, ct);
    }

    /// <summary>Updates the bid percentage for one or more ads.</summary>
    public async Task UpdateAdsBidByListingIdAsync(
        string campaignId,
        List<EbayAdRequest> ads,
        CancellationToken ct = default)
    {
        var url = $"{MarketingBase}/ad_campaign/{Uri.EscapeDataString(campaignId)}/ad/update_ads_bid_by_listing_id";
        var request = await BuildRequestAsync(HttpMethod.Post, url, new { ads }, ct);
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
        var token = await _oauth.GetUserTokenAsync(MarketingScopes, ct);
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
