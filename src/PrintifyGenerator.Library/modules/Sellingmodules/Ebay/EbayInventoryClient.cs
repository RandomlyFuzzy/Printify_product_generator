using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

/// <summary>
/// Client for the eBay Inventory API (v1).
/// Covers inventory items, inventory item groups, offers, and inventory locations.
///
/// All mutating operations use User access tokens; read-only calls may use
/// either Application or User tokens depending on the scope.
/// </summary>
public class EbayInventoryClient
{
    private readonly EbayConfig _config;
    private readonly EbayOAuthClient _oauth;
    private readonly HttpClient _http;

    private string InventoryBase => $"{_config.BaseUrl}/sell/inventory/v1";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly string[] SellerScopes =
    [
        "https://api.ebay.com/oauth/api_scope/sell.inventory",
        "https://api.ebay.com/oauth/api_scope/sell.inventory.readonly"
    ];

    public EbayInventoryClient(EbayConfig config, EbayOAuthClient oauth, HttpClient? http = null)
    {
        _config = config;
        _oauth = oauth;
        _http = http ?? new HttpClient();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Inventory Items
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Creates or replaces an inventory item record for the given SKU.</summary>
    public async Task CreateOrReplaceInventoryItemAsync(
        string sku,
        EbayInventoryItem item,
        string? locale = null,
        CancellationToken ct = default)
    {
        var url = $"{InventoryBase}/inventory_item/{Uri.EscapeDataString(sku)}";
        var request = await BuildRequestAsync(HttpMethod.Put, url, item, ct);
        if (!string.IsNullOrWhiteSpace(locale))
            request.Headers.Add("Content-Language", locale);

        await SendAsync(request, ct, expectEmptyBody: true);
    }

    /// <summary>Retrieves a single inventory item record by SKU.</summary>
    public async Task<EbayInventoryItemListing> GetInventoryItemAsync(
        string sku,
        CancellationToken ct = default)
    {
        var url = $"{InventoryBase}/inventory_item/{Uri.EscapeDataString(sku)}";
        var request = await BuildRequestAsync(HttpMethod.Get, url, ct: ct);
        return await SendAsync<EbayInventoryItemListing>(request, ct);
    }

    /// <summary>Retrieves all inventory item records for the seller's account (paged).</summary>
    public async Task<EbayGetInventoryItemsResponse> GetInventoryItemsAsync(
        int limit = 25,
        int offset = 0,
        CancellationToken ct = default)
    {
        var url = $"{InventoryBase}/inventory_item?limit={limit}&offset={offset}";
        var request = await BuildRequestAsync(HttpMethod.Get, url, ct: ct);
        return await SendAsync<EbayGetInventoryItemsResponse>(request, ct);
    }

    /// <summary>Deletes an inventory item record by SKU.</summary>
    public async Task DeleteInventoryItemAsync(string sku, CancellationToken ct = default)
    {
        var url = $"{InventoryBase}/inventory_item/{Uri.EscapeDataString(sku)}";
        var request = await BuildRequestAsync(HttpMethod.Delete, url, ct: ct);
        await SendAsync(request, ct, expectEmptyBody: true);
    }

    /// <summary>Creates or updates up to 25 inventory item records in a single call.</summary>
    public async Task<EbayBulkInventoryItemResponse> BulkCreateOrReplaceInventoryItemAsync(
        EbayBulkInventoryItemRequest bulkRequest,
        CancellationToken ct = default)
    {
        var url = $"{InventoryBase}/bulk_create_or_replace_inventory_item";
        var request = await BuildRequestAsync(HttpMethod.Post, url, bulkRequest, ct);
        return await SendAsync<EbayBulkInventoryItemResponse>(request, ct);
    }

    /// <summary>Retrieves up to 25 inventory item records by SKU in a single call.</summary>
    public async Task<EbayBulkInventoryItemResponse> BulkGetInventoryItemAsync(
        List<string> skus,
        CancellationToken ct = default)
    {
        var url = $"{InventoryBase}/bulk_get_inventory_item";
        var payload = new { requests = skus.Select(s => new { sku = s }).ToList() };
        var request = await BuildRequestAsync(HttpMethod.Post, url, payload, ct);
        return await SendAsync<EbayBulkInventoryItemResponse>(request, ct);
    }

    /// <summary>Updates price/quantity for up to 25 inventory items.</summary>
    public async Task<EbayBulkInventoryItemResponse> BulkUpdatePriceQuantityAsync(
        EbayBulkPriceQuantityRequest bulkRequest,
        CancellationToken ct = default)
    {
        var url = $"{InventoryBase}/bulk_update_price_quantity";
        var request = await BuildRequestAsync(HttpMethod.Post, url, bulkRequest, ct);
        return await SendAsync<EbayBulkInventoryItemResponse>(request, ct);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Inventory Item Groups
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Creates or replaces an inventory item group (multi-variation listing parent).</summary>
    public async Task CreateOrReplaceInventoryItemGroupAsync(
        string groupKey,
        EbayInventoryItemGroup group,
        CancellationToken ct = default)
    {
        var url = $"{InventoryBase}/inventory_item_group/{Uri.EscapeDataString(groupKey)}";
        var request = await BuildRequestAsync(HttpMethod.Put, url, group, ct);
        await SendAsync(request, ct, expectEmptyBody: true);
    }

    /// <summary>Retrieves an inventory item group.</summary>
    public async Task<EbayInventoryItemGroup> GetInventoryItemGroupAsync(
        string groupKey,
        CancellationToken ct = default)
    {
        var url = $"{InventoryBase}/inventory_item_group/{Uri.EscapeDataString(groupKey)}";
        var request = await BuildRequestAsync(HttpMethod.Get, url, ct: ct);
        return await SendAsync<EbayInventoryItemGroup>(request, ct);
    }

    /// <summary>Deletes an inventory item group.</summary>
    public async Task DeleteInventoryItemGroupAsync(string groupKey, CancellationToken ct = default)
    {
        var url = $"{InventoryBase}/inventory_item_group/{Uri.EscapeDataString(groupKey)}";
        var request = await BuildRequestAsync(HttpMethod.Delete, url, ct: ct);
        await SendAsync(request, ct, expectEmptyBody: true);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Offers
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Creates a new offer for the given SKU.</summary>
    public async Task<EbayOfferResponse> CreateOfferAsync(
        EbayOfferRequest offer,
        CancellationToken ct = default)
    {
        var url = $"{InventoryBase}/offer";
        var request = await BuildRequestAsync(HttpMethod.Post, url, offer, ct);
        return await SendAsync<EbayOfferResponse>(request, ct);
    }

    /// <summary>Retrieves a single offer by offer ID.</summary>
    public async Task<EbayOfferResponse> GetOfferAsync(
        string offerId,
        CancellationToken ct = default)
    {
        var url = $"{InventoryBase}/offer/{Uri.EscapeDataString(offerId)}";
        var request = await BuildRequestAsync(HttpMethod.Get, url, ct: ct);
        return await SendAsync<EbayOfferResponse>(request, ct);
    }

    /// <summary>Retrieves all existing offers for a specific SKU.</summary>
    public async Task<EbayGetOffersResponse> GetOffersAsync(
        string sku,
        string? marketplaceId = null,
        int limit = 25,
        int offset = 0,
        CancellationToken ct = default)
    {
        var queryParams = new List<string>
        {
            $"sku={Uri.EscapeDataString(sku)}",
            $"limit={limit}",
            $"offset={offset}"
        };
        if (!string.IsNullOrWhiteSpace(marketplaceId))
            queryParams.Add($"marketplace_id={Uri.EscapeDataString(marketplaceId)}");

        var url = $"{InventoryBase}/offer?{string.Join("&", queryParams)}";
        var request = await BuildRequestAsync(HttpMethod.Get, url, ct: ct);
        return await SendAsync<EbayGetOffersResponse>(request, ct);
    }

    /// <summary>Updates an existing offer.</summary>
    public async Task<EbayOfferResponse> UpdateOfferAsync(
        string offerId,
        EbayOfferRequest offer,
        CancellationToken ct = default)
    {
        var url = $"{InventoryBase}/offer/{Uri.EscapeDataString(offerId)}";
        var request = await BuildRequestAsync(HttpMethod.Put, url, offer, ct);
        return await SendAsync<EbayOfferResponse>(request, ct);
    }

    /// <summary>Deletes a published or unpublished offer.</summary>
    public async Task DeleteOfferAsync(string offerId, CancellationToken ct = default)
    {
        var url = $"{InventoryBase}/offer/{Uri.EscapeDataString(offerId)}";
        var request = await BuildRequestAsync(HttpMethod.Delete, url, ct: ct);
        await SendAsync(request, ct, expectEmptyBody: true);
    }

    /// <summary>Publishes an unpublished offer (creates a live eBay listing).</summary>
    public async Task<EbayPublishOfferResponse> PublishOfferAsync(
        string offerId,
        CancellationToken ct = default)
    {
        var url = $"{InventoryBase}/offer/{Uri.EscapeDataString(offerId)}/publish";
        var request = await BuildRequestAsync(HttpMethod.Post, url, ct: ct);
        return await SendAsync<EbayPublishOfferResponse>(request, ct);
    }

    /// <summary>Publishes a multi-variant offer by inventory item group key.</summary>
    public async Task<EbayPublishOfferResponse> PublishOfferByInventoryItemGroupAsync(
        string groupKey,
        string marketplaceId,
        CancellationToken ct = default)
    {
        var url = $"{InventoryBase}/offer/publish_by_inventory_item_group";
        var payload = new { inventoryItemGroupKey = groupKey, marketplaceId };
        var request = await BuildRequestAsync(HttpMethod.Post, url, payload, ct);
        return await SendAsync<EbayPublishOfferResponse>(request, ct);
    }

    /// <summary>Ends (withdraws) an active single-SKU listing.</summary>
    public async Task<EbayOfferResponse> WithdrawOfferAsync(
        string offerId,
        CancellationToken ct = default)
    {
        var url = $"{InventoryBase}/offer/{Uri.EscapeDataString(offerId)}/withdraw";
        var request = await BuildRequestAsync(HttpMethod.Post, url, ct: ct);
        return await SendAsync<EbayOfferResponse>(request, ct);
    }

    /// <summary>Ends an active multi-SKU listing by inventory item group key.</summary>
    public async Task WithdrawOfferByInventoryItemGroupAsync(
        string groupKey,
        string marketplaceId,
        CancellationToken ct = default)
    {
        var url = $"{InventoryBase}/offer/withdraw_by_inventory_item_group";
        var payload = new { inventoryItemGroupKey = groupKey, marketplaceId };
        var request = await BuildRequestAsync(HttpMethod.Post, url, payload, ct);
        await SendAsync(request, ct, expectEmptyBody: true);
    }

    /// <summary>Retrieves estimated listing fees for up to 250 unpublished offers.</summary>
    public async Task<EbayListingFeesResponse> GetListingFeesAsync(
        List<string> offerIds,
        CancellationToken ct = default)
    {
        var url = $"{InventoryBase}/offer/get_listing_fees";
        var payload = new { offers = offerIds.Select(id => new { offerId = id }).ToList() };
        var request = await BuildRequestAsync(HttpMethod.Post, url, payload, ct);
        return await SendAsync<EbayListingFeesResponse>(request, ct);
    }

    /// <summary>Creates up to 25 offers in a single bulk call.</summary>
    public async Task<EbayBulkOfferResponse> BulkCreateOfferAsync(
        EbayBulkOfferRequest bulkRequest,
        CancellationToken ct = default)
    {
        var url = $"{InventoryBase}/bulk_create_offer";
        var request = await BuildRequestAsync(HttpMethod.Post, url, bulkRequest, ct);
        return await SendAsync<EbayBulkOfferResponse>(request, ct);
    }

    /// <summary>Publishes up to 25 unpublished offers in a single bulk call.</summary>
    public async Task<EbayBulkPublishOfferResponse> BulkPublishOfferAsync(
        EbayBulkPublishOfferRequest bulkRequest,
        CancellationToken ct = default)
    {
        var url = $"{InventoryBase}/bulk_publish_offer";
        var request = await BuildRequestAsync(HttpMethod.Post, url, bulkRequest, ct);
        return await SendAsync<EbayBulkPublishOfferResponse>(request, ct);
    }

    /// <summary>Migrates up to 25 existing eBay listings to the Inventory API model.</summary>
    public async Task<EbayBulkOfferResponse> BulkMigrateListingAsync(
        List<string> listingIds,
        CancellationToken ct = default)
    {
        var url = $"{InventoryBase}/bulk_migrate_listing";
        var payload = new { requests = listingIds.Select(id => new { listingId = id }).ToList() };
        var request = await BuildRequestAsync(HttpMethod.Post, url, payload, ct);
        return await SendAsync<EbayBulkOfferResponse>(request, ct);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Inventory Locations
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Creates an inventory location.</summary>
    public async Task CreateInventoryLocationAsync(
        string merchantLocationKey,
        EbayInventoryLocation location,
        CancellationToken ct = default)
    {
        var url = $"{InventoryBase}/location/{Uri.EscapeDataString(merchantLocationKey)}";
        var request = await BuildRequestAsync(HttpMethod.Post, url, location, ct);
        await SendAsync(request, ct, expectEmptyBody: true);
    }

    /// <summary>Retrieves a single inventory location.</summary>
    public async Task<EbayInventoryLocation> GetInventoryLocationAsync(
        string merchantLocationKey,
        CancellationToken ct = default)
    {
        var url = $"{InventoryBase}/location/{Uri.EscapeDataString(merchantLocationKey)}";
        var request = await BuildRequestAsync(HttpMethod.Get, url, ct: ct);
        return await SendAsync<EbayInventoryLocation>(request, ct);
    }

    /// <summary>Retrieves all inventory locations for the seller's account.</summary>
    public async Task<EbayGetInventoryLocationsResponse> GetInventoryLocationsAsync(
        int limit = 25,
        int offset = 0,
        CancellationToken ct = default)
    {
        var url = $"{InventoryBase}/location?limit={limit}&offset={offset}";
        var request = await BuildRequestAsync(HttpMethod.Get, url, ct: ct);
        return await SendAsync<EbayGetInventoryLocationsResponse>(request, ct);
    }

    /// <summary>Updates non-physical details of an existing inventory location.</summary>
    public async Task UpdateInventoryLocationAsync(
        string merchantLocationKey,
        EbayInventoryLocation location,
        CancellationToken ct = default)
    {
        var url = $"{InventoryBase}/location/{Uri.EscapeDataString(merchantLocationKey)}/update_location_details";
        var request = await BuildRequestAsync(HttpMethod.Post, url, location, ct);
        await SendAsync(request, ct, expectEmptyBody: true);
    }

    /// <summary>Deletes an inventory location.</summary>
    public async Task DeleteInventoryLocationAsync(
        string merchantLocationKey,
        CancellationToken ct = default)
    {
        var url = $"{InventoryBase}/location/{Uri.EscapeDataString(merchantLocationKey)}";
        var request = await BuildRequestAsync(HttpMethod.Delete, url, ct: ct);
        await SendAsync(request, ct, expectEmptyBody: true);
    }

    /// <summary>Disables an active inventory location.</summary>
    public async Task DisableInventoryLocationAsync(
        string merchantLocationKey,
        CancellationToken ct = default)
    {
        var url = $"{InventoryBase}/location/{Uri.EscapeDataString(merchantLocationKey)}/disable";
        var request = await BuildRequestAsync(HttpMethod.Post, url, ct: ct);
        await SendAsync(request, ct, expectEmptyBody: true);
    }

    /// <summary>Re-enables a disabled inventory location.</summary>
    public async Task EnableInventoryLocationAsync(
        string merchantLocationKey,
        CancellationToken ct = default)
    {
        var url = $"{InventoryBase}/location/{Uri.EscapeDataString(merchantLocationKey)}/enable";
        var request = await BuildRequestAsync(HttpMethod.Post, url, ct: ct);
        await SendAsync(request, ct, expectEmptyBody: true);
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
        var token = await _oauth.GetUserTokenAsync(SellerScopes, ct);
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

        if (!response.IsSuccessStatusCode)
            ThrowApiException((int)response.StatusCode, body);

        return JsonSerializer.Deserialize<T>(body, _jsonOptions)
               ?? throw new InvalidOperationException("Unexpected null response body.");
    }

    private async Task SendAsync(
        HttpRequestMessage request,
        CancellationToken ct,
        bool expectEmptyBody = false)
    {
        var response = await _http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            ThrowApiException((int)response.StatusCode, body);
    }

    private static void ThrowApiException(int statusCode, string body)
    {
        List<EbayError>? errors = null;
        try
        {
            var errorResponse = JsonSerializer.Deserialize<EbayErrorResponse>(body);
            errors = errorResponse?.Errors;
        }
        catch { /* ignore parse failures */ }

        throw new EbayApiException(statusCode, $"eBay API error ({statusCode}): {body}", errors);
    }
}
