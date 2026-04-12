using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

/// <summary>
/// Etsy Open API v3 client.
/// Docs:      https://developers.etsy.com/documentation/reference
/// Base URL:  https://api.etsy.com/v3
/// Auth:      x-api-key header (keystring:shared_secret) for public endpoints;
///            additionally an OAuth2 Bearer token for scoped endpoints.
/// OAuth2:    Authorization Code + PKCE flow.
///            Authorization URL : https://www.etsy.com/oauth/connect
///            Token URL         : https://api.etsy.com/v3/public/oauth/token
/// </summary>
public class EtsyClient
{
    private const string BaseUrl     = "https://api.etsy.com/v3";
    private const string AuthBaseUrl = "https://www.etsy.com/oauth/connect";

    private readonly HttpClient _http;
    private readonly string     _apiKey; // "keystring:shared_secret"
    private string?             _accessToken;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy          = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive   = true,
        DefaultIgnoreCondition        = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    // ─── Construction ──────────────────────────────────────────────

    /// <param name="apiKey">Keystring and shared-secret joined with colon, e.g. "abc123:xyz789"</param>
    /// <param name="accessToken">Optional OAuth2 bearer token for scoped endpoints.</param>
    public EtsyClient(string apiKey, string? accessToken = null)
    {
        _apiKey      = apiKey;
        _accessToken = accessToken;
        _http        = new HttpClient();
        _http.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        _http.DefaultRequestHeaders.Add("User-Agent", "PrintifyGenerator/1.0");
    }

    /// <summary>Set or replace the OAuth2 access token after completing the OAuth flow.</summary>
    public void SetAccessToken(string accessToken) => _accessToken = accessToken;

    // ─── OAuth2 Helpers ────────────────────────────────────────────

    /// <summary>
    /// Generates a cryptographically-random PKCE code verifier (43–128 chars).
    /// Save this value to exchange the authorization code for a token later.
    /// </summary>
    public static string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Base64UrlEncode(bytes);
    }

    /// <summary>Derives the PKCE code challenge (S256) from a verifier.</summary>
    public static string GenerateCodeChallenge(string codeVerifier)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(hash);
    }

    /// <summary>
    /// Builds the Etsy OAuth2 authorization URL the end-user must visit.
    /// </summary>
    /// <param name="redirectUri">Your registered redirect URI.</param>
    /// <param name="scopes">
    ///   Space-separated Etsy scopes, e.g. "listings_r listings_w shops_r email_r".
    ///   Available scopes: address_r address_w billing_r cart_r cart_w email_r
    ///   favorites_r favorites_w feedback_r listings_d listings_r listings_w
    ///   profile_r profile_w recommend_r recommend_w shops_r shops_w
    ///   transactions_r transactions_w
    /// </param>
    /// <param name="state">CSRF state string. Must be replayed back in the callback.</param>
    /// <param name="codeChallenge">PKCE challenge derived from <see cref="GenerateCodeChallenge"/>.</param>
    public string BuildAuthorizationUrl(string redirectUri, string scopes, string state, string codeChallenge)
    {
        var keystring = _apiKey.Split(':')[0];
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["response_type"]          = "code";
        query["redirect_uri"]           = redirectUri;
        query["scope"]                  = scopes;
        query["client_id"]              = keystring;
        query["state"]                  = state;
        query["code_challenge"]         = codeChallenge;
        query["code_challenge_method"]  = "S256";
        return $"{AuthBaseUrl}?{query}";
    }

    /// <summary>
    /// Exchanges the authorization code (received at your redirect_uri) for an access + refresh token.
    /// </summary>
    public async Task<EtsyTokenResponse> ExchangeCodeForTokenAsync(
        string code, string redirectUri, string codeVerifier)
    {
        var keystring = _apiKey.Split(':')[0];
        var body = new
        {
            grant_type    = "authorization_code",
            client_id     = keystring,
            redirect_uri  = redirectUri,
            code,
            code_verifier = codeVerifier
        };
        var content  = new StringContent(JsonSerializer.Serialize(body, JsonOpts), Encoding.UTF8, "application/json");
        var response = await _http.PostAsync($"{BaseUrl}/public/oauth/token", content);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync();
        var token = JsonSerializer.Deserialize<EtsyTokenResponse>(json, JsonOpts)!;
        SetAccessToken(token.AccessToken);
        return token;
    }

    /// <summary>Refreshes a previously obtained access token using the refresh token.</summary>
    public async Task<EtsyTokenResponse> RefreshTokenAsync(string refreshToken)
    {
        var keystring = _apiKey.Split(':')[0];
        var body = new { grant_type = "refresh_token", client_id = keystring, refresh_token = refreshToken };
        var content  = new StringContent(JsonSerializer.Serialize(body, JsonOpts), Encoding.UTF8, "application/json");
        var response = await _http.PostAsync($"{BaseUrl}/public/oauth/token", content);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync();
        var token = JsonSerializer.Deserialize<EtsyTokenResponse>(json, JsonOpts)!;
        SetAccessToken(token.AccessToken);
        return token;
    }

    // ─── Other ─────────────────────────────────────────────────────

    /// <summary>Verifies connectivity and that your API key is valid. No OAuth token required.</summary>
    public async Task<EtsyPingResponse> PingAsync()
        => await GetAsync<EtsyPingResponse>("/application/openapi-ping");

    /// <summary>Returns the scopes associated with the provided bearer token.</summary>
    public async Task<JsonElement> GetTokenScopesAsync(string token)
    {
        var form = new Dictionary<string, string> { ["token"] = token };
        return await PostFormAsync<JsonElement>("/application/scopes", form);
    }

    // ─── User ──────────────────────────────────────────────────────

    /// <summary>Retrieves a user by their numeric user ID. Requires email_r scope.</summary>
    public async Task<EtsyUser> GetUserAsync(long userId)
        => await GetAsync<EtsyUser>($"/application/users/{userId}", requiresToken: true);

    /// <summary>Returns the user_id and shop_id of the currently authenticated user. Requires shops_r scope.</summary>
    public async Task<EtsyMeResponse> GetMeAsync()
        => await GetAsync<EtsyMeResponse>("/application/users/me", requiresToken: true);

    // ─── Shop ──────────────────────────────────────────────────────

    /// <summary>Retrieves a shop by its numeric shop ID.</summary>
    public async Task<EtsyShop> GetShopAsync(long shopId)
        => await GetAsync<EtsyShop>($"/application/shops/{shopId}");

    /// <summary>Updates shop fields. Requires shops_r + shops_w scope.</summary>
    public async Task<EtsyShop> UpdateShopAsync(long shopId, EtsyUpdateShopRequest request)
        => await PutFormAsync<EtsyShop>($"/application/shops/{shopId}", ToFormDict(request));

    /// <summary>Retrieves the shop owned by the specified user.</summary>
    public async Task<EtsyShop> GetShopByOwnerUserIdAsync(long userId)
        => await GetAsync<EtsyShop>($"/application/users/{userId}/shops");

    /// <summary>Searches shops by name.</summary>
    public async Task<EtsyPagedResult<EtsyShop>> FindShopsAsync(string shopName, int limit = 25, int offset = 0)
        => await GetAsync<EtsyPagedResult<EtsyShop>>($"/application/shops?shop_name={Uri.EscapeDataString(shopName)}&limit={limit}&offset={offset}");

    // ─── Shop Section ──────────────────────────────────────────────

    /// <summary>Creates a new section in the shop. Requires shops_w scope.</summary>
    public async Task<EtsyShopSection> CreateShopSectionAsync(long shopId, string title)
        => await PostFormAsync<EtsyShopSection>($"/application/shops/{shopId}/sections", new() { ["title"] = title }, requiresToken: true);

    /// <summary>Lists all sections in a shop.</summary>
    public async Task<EtsyPagedResult<EtsyShopSection>> GetShopSectionsAsync(long shopId)
        => await GetAsync<EtsyPagedResult<EtsyShopSection>>($"/application/shops/{shopId}/sections");

    /// <summary>Retrieves a single shop section.</summary>
    public async Task<EtsyShopSection> GetShopSectionAsync(long shopId, long shopSectionId)
        => await GetAsync<EtsyShopSection>($"/application/shops/{shopId}/sections/{shopSectionId}");

    /// <summary>Updates a shop section title. Requires shops_w scope.</summary>
    public async Task<EtsyShopSection> UpdateShopSectionAsync(long shopId, long shopSectionId, string title)
        => await PutFormAsync<EtsyShopSection>($"/application/shops/{shopId}/sections/{shopSectionId}",
            new() { ["title"] = title }, requiresToken: true);

    /// <summary>Deletes a shop section. Requires shops_w scope.</summary>
    public async Task DeleteShopSectionAsync(long shopId, long shopSectionId)
        => await DeleteAsync($"/application/shops/{shopId}/sections/{shopSectionId}", requiresToken: true);

    // ─── Shop Return Policy ────────────────────────────────────────

    /// <summary>Lists all return policies for a shop.</summary>
    public async Task<EtsyPagedResult<EtsyReturnPolicy>> GetShopReturnPoliciesAsync(long shopId)
        => await GetAsync<EtsyPagedResult<EtsyReturnPolicy>>($"/application/shops/{shopId}/policies/return");

    /// <summary>Deletes a return policy. Requires shops_w scope.</summary>
    public async Task DeleteShopReturnPolicyAsync(long shopId, long returnPolicyId)
        => await DeleteAsync($"/application/shops/{shopId}/policies/return/{returnPolicyId}", requiresToken: true);

    // ─── Shop Production Partners ──────────────────────────────────

    /// <summary>Lists all production partners for a shop. Requires shops_r scope.</summary>
    public async Task<EtsyPagedResult<EtsyProductionPartner>> GetShopProductionPartnersAsync(long shopId)
        => await GetAsync<EtsyPagedResult<EtsyProductionPartner>>($"/application/shops/{shopId}/production-partners", requiresToken: true);

    // ─── Listings ──────────────────────────────────────────────────

    /// <summary>
    /// Creates a physical draft listing. Requires listings_w scope.
    /// </summary>
    public async Task<EtsyListing> CreateDraftListingAsync(long shopId, EtsyCreateDraftListingRequest request)
    {
        var form = new Dictionary<string, string>
        {
            ["quantity"]    = request.Quantity.ToString(),
            ["title"]       = request.Title,
            ["description"] = request.Description,
            ["price"]       = request.Price.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
            ["who_made"]    = request.WhoMade,
            ["when_made"]   = request.WhenMade,
        };
        if (request.IsSupply.HasValue)           form["is_supply"]           = request.IsSupply.Value.ToString().ToLower();
        if (request.TaxonomyId.HasValue)         form["taxonomy_id"]         = request.TaxonomyId.Value.ToString();
        if (request.Type != null)                form["type"]                = request.Type;
        if (request.ShippingProfileId.HasValue)  form["shipping_profile_id"] = request.ShippingProfileId.Value.ToString();
        if (request.ReturnPolicyId.HasValue)     form["return_policy_id"]    = request.ReturnPolicyId.Value.ToString();
        if (request.ShopSectionId.HasValue)      form["shop_section_id"]     = request.ShopSectionId.Value.ToString();
        if (request.Tags != null)                form["tags[]"]              = string.Join(",", request.Tags);
        if (request.Materials != null)           form["materials[]"]         = string.Join(",", request.Materials);
        if (request.Style != null)               form["style[]"]             = string.Join(",", request.Style);
        if (request.ProcessingMin.HasValue)      form["processing_min"]      = request.ProcessingMin.Value.ToString();
        if (request.ProcessingMax.HasValue)      form["processing_max"]      = request.ProcessingMax.Value.ToString();
        if (request.IsPersonalizable.HasValue)   form["is_personalizable"]   = request.IsPersonalizable.Value.ToString().ToLower();
        if (request.IsCustomizable.HasValue)     form["is_customizable"]     = request.IsCustomizable.Value.ToString().ToLower();
        if (request.ShouldAutoRenew.HasValue)    form["should_auto_renew"]   = request.ShouldAutoRenew.Value.ToString().ToLower();
        if (request.IsTaxable.HasValue)          form["is_taxable"]          = request.IsTaxable.Value.ToString().ToLower();
        if (request.ItemWeightUnit != null)      form["item_weight_unit"]    = request.ItemWeightUnit;
        if (request.ItemWeight.HasValue)         form["item_weight"]         = request.ItemWeight.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (request.ItemDimensionsUnit != null)  form["item_dimensions_unit"] = request.ItemDimensionsUnit;
        if (request.ItemLength.HasValue)         form["item_length"]         = request.ItemLength.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (request.ItemWidth.HasValue)          form["item_width"]          = request.ItemWidth.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (request.ItemHeight.HasValue)         form["item_height"]         = request.ItemHeight.Value.ToString(System.Globalization.CultureInfo.InvariantCulture);

        return await PostFormAsync<EtsyListing>($"/application/shops/{shopId}/listings", form, requiresToken: true);
    }

    /// <summary>
    /// Updates a listing. Requires listings_w scope.
    /// Only non-null fields in the request are sent.
    /// </summary>
    public async Task<EtsyListing> UpdateListingAsync(long shopId, long listingId, EtsyUpdateListingRequest request)
    {
        var form = new Dictionary<string, string>();
        if (request.Quantity.HasValue)           form["quantity"]            = request.Quantity.Value.ToString();
        if (request.Title != null)               form["title"]               = request.Title;
        if (request.Description != null)         form["description"]         = request.Description;
        if (request.Price.HasValue)              form["price"]               = request.Price.Value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        if (request.State != null)               form["state"]               = request.State;
        if (request.WhoMade != null)             form["who_made"]            = request.WhoMade;
        if (request.WhenMade != null)            form["when_made"]           = request.WhenMade;
        if (request.IsSupply.HasValue)           form["is_supply"]           = request.IsSupply.Value.ToString().ToLower();
        if (request.TaxonomyId.HasValue)         form["taxonomy_id"]         = request.TaxonomyId.Value.ToString();
        if (request.ShippingProfileId.HasValue)  form["shipping_profile_id"] = request.ShippingProfileId.Value.ToString();
        if (request.ReturnPolicyId.HasValue)     form["return_policy_id"]    = request.ReturnPolicyId.Value.ToString();
        if (request.ShopSectionId.HasValue)      form["shop_section_id"]     = request.ShopSectionId.Value.ToString();
        if (request.Tags != null)                form["tags[]"]              = string.Join(",", request.Tags);
        if (request.Materials != null)           form["materials[]"]         = string.Join(",", request.Materials);
        if (request.Style != null)               form["style[]"]             = string.Join(",", request.Style);
        if (request.ProcessingMin.HasValue)      form["processing_min"]      = request.ProcessingMin.Value.ToString();
        if (request.ProcessingMax.HasValue)      form["processing_max"]      = request.ProcessingMax.Value.ToString();
        if (request.IsPersonalizable.HasValue)   form["is_personalizable"]   = request.IsPersonalizable.Value.ToString().ToLower();
        if (request.IsCustomizable.HasValue)     form["is_customizable"]     = request.IsCustomizable.Value.ToString().ToLower();
        if (request.ShouldAutoRenew.HasValue)    form["should_auto_renew"]   = request.ShouldAutoRenew.Value.ToString().ToLower();
        if (request.IsTaxable.HasValue)          form["is_taxable"]          = request.IsTaxable.Value.ToString().ToLower();
        if (request.FeaturedRank != null)        form["featured_rank"]       = request.FeaturedRank;

        return await PatchFormAsync<EtsyListing>($"/application/shops/{shopId}/listings/{listingId}", form, requiresToken: true);
    }

    /// <summary>Retrieves a listing by its ID.</summary>
    public async Task<EtsyListing> GetListingAsync(long listingId, string[]? includes = null)
    {
        var qs = BuildQueryString(new() { ["includes"] = includes != null ? string.Join(",", includes) : null });
        return await GetAsync<EtsyListing>($"/application/listings/{listingId}{qs}");
    }

    /// <summary>Lists all listings in a shop. Requires listings_r scope.</summary>
    public async Task<EtsyPagedResult<EtsyListing>> GetListingsByShopAsync(
        long shopId, string state = "active", int limit = 25, int offset = 0,
        string sortOn = "created", string sortOrder = "desc", string[]? includes = null)
    {
        var qs = BuildQueryString(new()
        {
            ["state"]      = state,
            ["limit"]      = limit.ToString(),
            ["offset"]     = offset.ToString(),
            ["sort_on"]    = sortOn,
            ["sort_order"] = sortOrder,
            ["includes"]   = includes != null ? string.Join(",", includes) : null
        });
        return await GetAsync<EtsyPagedResult<EtsyListing>>($"/application/shops/{shopId}/listings{qs}", requiresToken: true);
    }

    /// <summary>Retrieves all listings in a shop, automatically paging through all results. Requires listings_r scope.</summary>
    public async Task<List<EtsyListing>> GetAllListingsByShopAsync(long shopId, string state = "active")
    {
        var all = new List<EtsyListing>();
        int offset = 0, limit = 100;
        while (true)
        {
            var page = await GetListingsByShopAsync(shopId, state: state, limit: limit, offset: offset);
            all.AddRange(page.Results);
            if (all.Count >= page.Count || page.Results.Count < limit) break;
            offset += limit;
        }
        return all;
    }

    /// <summary>Searches all active listings across Etsy.</summary>
    public async Task<EtsyPagedResult<EtsyListing>> FindAllListingsActiveAsync(
        int limit = 25, int offset = 0, string? keywords = null,
        string sortOn = "created", string sortOrder = "desc",
        float? minPrice = null, float? maxPrice = null,
        long? taxonomyId = null, string? shopLocation = null)
    {
        var qs = BuildQueryString(new()
        {
            ["limit"]       = limit.ToString(),
            ["offset"]      = offset.ToString(),
            ["keywords"]    = keywords,
            ["sort_on"]     = sortOn,
            ["sort_order"]  = sortOrder,
            ["min_price"]   = minPrice?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["max_price"]   = maxPrice?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["taxonomy_id"] = taxonomyId?.ToString(),
            ["shop_location"] = shopLocation
        });
        return await GetAsync<EtsyPagedResult<EtsyListing>>($"/application/listings/active{qs}");
    }

    /// <summary>Lists all active listings in a specific shop (public, no OAuth).</summary>
    public async Task<EtsyPagedResult<EtsyListing>> FindAllActiveListingsByShopAsync(
        long shopId, int limit = 25, int offset = 0, string? keywords = null,
        string sortOn = "created", string sortOrder = "desc")
    {
        var qs = BuildQueryString(new()
        {
            ["limit"]      = limit.ToString(),
            ["offset"]     = offset.ToString(),
            ["keywords"]   = keywords,
            ["sort_on"]    = sortOn,
            ["sort_order"] = sortOrder
        });
        return await GetAsync<EtsyPagedResult<EtsyListing>>($"/application/shops/{shopId}/listings/active{qs}");
    }

    /// <summary>Fetches up to 100 listings by their IDs in one request.</summary>
    public async Task<EtsyPagedResult<EtsyListing>> GetListingsByListingIdsAsync(
        IEnumerable<long> listingIds, string[]? includes = null)
    {
        var ids = string.Join("&", listingIds.Select(id => $"listing_ids[]={id}"));
        var incl = includes != null ? $"&includes={string.Join(",", includes)}" : "";
        return await GetAsync<EtsyPagedResult<EtsyListing>>($"/application/listings/batch?{ids}{incl}");
    }

    /// <summary>Retrieves featured listings from a shop.</summary>
    public async Task<EtsyPagedResult<EtsyListing>> GetFeaturedListingsByShopAsync(long shopId, int limit = 25, int offset = 0)
        => await GetAsync<EtsyPagedResult<EtsyListing>>($"/application/shops/{shopId}/listings/featured?limit={limit}&offset={offset}");

    /// <summary>Retrieves listings associated with a receipt. Requires transactions_r scope.</summary>
    public async Task<EtsyPagedResult<EtsyListing>> GetListingsByShopReceiptAsync(
        long shopId, long receiptId, int limit = 25, int offset = 0)
        => await GetAsync<EtsyPagedResult<EtsyListing>>(
            $"/application/shops/{shopId}/receipts/{receiptId}/listings?limit={limit}&offset={offset}", requiresToken: true);

    /// <summary>Retrieves listings associated with a return policy. Requires listings_r scope.</summary>
    public async Task<EtsyPagedResult<EtsyListing>> GetListingsByShopReturnPolicyAsync(long shopId, long returnPolicyId)
        => await GetAsync<EtsyPagedResult<EtsyListing>>(
            $"/application/shops/{shopId}/policies/return/{returnPolicyId}/listings", requiresToken: true);

    /// <summary>Retrieves listings in specific shop sections.</summary>
    public async Task<EtsyPagedResult<EtsyListing>> GetListingsByShopSectionIdAsync(
        long shopId, IEnumerable<long> shopSectionIds,
        int limit = 25, int offset = 0, string sortOn = "created", string sortOrder = "desc")
    {
        var ids = string.Join("&", shopSectionIds.Select(id => $"shop_section_ids[]={id}"));
        return await GetAsync<EtsyPagedResult<EtsyListing>>(
            $"/application/shops/{shopId}/shop-sections/listings?{ids}&limit={limit}&offset={offset}&sort_on={sortOn}&sort_order={sortOrder}");
    }

    /// <summary>Deletes a listing. Requires listings_d scope.</summary>
    public async Task DeleteListingAsync(long listingId)
        => await DeleteAsync($"/application/listings/{listingId}", requiresToken: true);

    // ─── Listing Properties ────────────────────────────────────────

    /// <summary>Retrieves all properties for a listing.</summary>
    public async Task<EtsyPagedResult<EtsyPropertyValue>> GetListingPropertiesAsync(long shopId, long listingId)
        => await GetAsync<EtsyPagedResult<EtsyPropertyValue>>($"/application/shops/{shopId}/listings/{listingId}/properties");

    /// <summary>Updates a single listing property (e.g. color, size). Requires listings_w scope.</summary>
    public async Task<EtsyPropertyValue> UpdateListingPropertyAsync(
        long shopId, long listingId, long propertyId, EtsyListingPropertyRequest request)
    {
        var form = new Dictionary<string, string>();
        form["value_ids[]"] = string.Join(",", request.ValueIds);
        form["values[]"]    = string.Join(",", request.Values);
        if (request.ScaleId.HasValue) form["scale_id"] = request.ScaleId.Value.ToString();
        return await PutFormAsync<EtsyPropertyValue>(
            $"/application/shops/{shopId}/listings/{listingId}/properties/{propertyId}", form, requiresToken: true);
    }

    /// <summary>Deletes a listing property. Requires listings_w scope.</summary>
    public async Task DeleteListingPropertyAsync(long shopId, long listingId, long propertyId)
        => await DeleteAsync($"/application/shops/{shopId}/listings/{listingId}/properties/{propertyId}", requiresToken: true);

    // ─── Listing Images ────────────────────────────────────────────

    /// <summary>Retrieves all images for a listing.</summary>
    public async Task<EtsyPagedResult<EtsyListingImage>> GetListingImagesAsync(long listingId)
        => await GetAsync<EtsyPagedResult<EtsyListingImage>>($"/application/listings/{listingId}/images");

    /// <summary>Retrieves a single listing image.</summary>
    public async Task<EtsyListingImage> GetListingImageAsync(long listingId, long listingImageId)
        => await GetAsync<EtsyListingImage>($"/application/listings/{listingId}/images/{listingImageId}");

    /// <summary>
    /// Uploads an image for a listing. Requires listings_w scope.
    /// Pass either imageBytes or listingImageId (to re-assign a deleted image).
    /// </summary>
    public async Task<EtsyListingImage> UploadListingImageAsync(
        long shopId, long listingId,
        byte[]? imageBytes = null, string? imageFileName = null,
        long? listingImageId = null, int rank = 1, bool overwrite = false,
        bool watermark = false, string? altText = null)
    {
        using var form = new MultipartFormDataContent();
        if (imageBytes != null && imageFileName != null)
        {
            var imageContent = new ByteArrayContent(imageBytes);
            imageContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
            form.Add(imageContent, "image", imageFileName);
        }
        if (listingImageId.HasValue) form.Add(new StringContent(listingImageId.Value.ToString()), "listing_image_id");
        form.Add(new StringContent(rank.ToString()), "rank");
        form.Add(new StringContent(overwrite.ToString().ToLower()), "overwrite");
        form.Add(new StringContent(watermark.ToString().ToLower()), "is_watermarked");
        if (altText != null) form.Add(new StringContent(altText), "alt_text");

        return await PostMultipartAsync<EtsyListingImage>(
            $"/application/shops/{shopId}/listings/{listingId}/images", form, requiresToken: true);
    }

    /// <summary>Uploads a listing image from a local file path. Requires listings_w scope.</summary>
    public async Task<EtsyListingImage> UploadListingImageFromFileAsync(
        long shopId, long listingId, string filePath, int rank = 1, string? altText = null)
    {
        var bytes = await File.ReadAllBytesAsync(filePath);
        return await UploadListingImageAsync(shopId, listingId,
            imageBytes: bytes,
            imageFileName: Path.GetFileName(filePath),
            rank: rank,
            altText: altText);
    }

    /// <summary>Deletes a listing image. Requires listings_w scope.</summary>
    public async Task DeleteListingImageAsync(long shopId, long listingId, long listingImageId)
        => await DeleteAsync($"/application/shops/{shopId}/listings/{listingId}/images/{listingImageId}", requiresToken: true);

    // ─── Listing Videos ────────────────────────────────────────────

    /// <summary>Retrieves all videos for a listing.</summary>
    public async Task<EtsyPagedResult<EtsyListingVideo>> GetListingVideosAsync(long listingId)
        => await GetAsync<EtsyPagedResult<EtsyListingVideo>>($"/application/listings/{listingId}/videos");

    /// <summary>Deletes a listing video. Requires listings_w scope.</summary>
    public async Task DeleteListingVideoAsync(long shopId, long listingId, long videoId)
        => await DeleteAsync($"/application/shops/{shopId}/listings/{listingId}/videos/{videoId}", requiresToken: true);

    // ─── Listing Files (digital) ───────────────────────────────────

    /// <summary>Retrieves all digital files for a listing. Requires listings_r scope.</summary>
    public async Task<EtsyPagedResult<EtsyListingFile>> GetAllListingFilesAsync(long shopId, long listingId)
        => await GetAsync<EtsyPagedResult<EtsyListingFile>>($"/application/shops/{shopId}/listings/{listingId}/files", requiresToken: true);

    /// <summary>Retrieves a single digital file. Requires listings_r scope.</summary>
    public async Task<EtsyListingFile> GetListingFileAsync(long shopId, long listingId, long listingFileId)
        => await GetAsync<EtsyListingFile>(
            $"/application/shops/{shopId}/listings/{listingId}/files/{listingFileId}", requiresToken: true);

    /// <summary>Uploads a digital file for a listing. Requires listings_w scope.</summary>
    public async Task<EtsyListingFile> UploadListingFileAsync(
        long shopId, long listingId, byte[] fileBytes, string fileName, int? rank = null)
    {
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        form.Add(fileContent, "file", fileName);
        form.Add(new StringContent(fileName), "name");
        if (rank.HasValue) form.Add(new StringContent(rank.Value.ToString()), "rank");
        return await PostMultipartAsync<EtsyListingFile>(
            $"/application/shops/{shopId}/listings/{listingId}/files", form, requiresToken: true);
    }

    /// <summary>Deletes a digital listing file. Requires listings_w scope.</summary>
    public async Task DeleteListingFileAsync(long shopId, long listingId, long listingFileId)
        => await DeleteAsync($"/application/shops/{shopId}/listings/{listingId}/files/{listingFileId}", requiresToken: true);

    // ─── Listing Inventory ─────────────────────────────────────────

    /// <summary>
    /// Retrieves the inventory record for a listing.
    /// Requires listings_r scope for owner details (SKU, etc.).
    /// </summary>
    public async Task<EtsyListingInventory> GetListingInventoryAsync(
        long listingId, bool showDeleted = false, bool includeListing = false)
    {
        var qs = BuildQueryString(new()
        {
            ["show_deleted"] = showDeleted ? "true" : null,
            ["includes"]     = includeListing ? "Listing" : null
        });
        return await GetAsync<EtsyListingInventory>(
            $"/application/listings/{listingId}/inventory{qs}", requiresToken: true);
    }

    /// <summary>
    /// Updates the inventory (products/offerings/SKUs) for a listing.
    /// Requires listings_w scope.
    /// </summary>
    public async Task<EtsyListingInventory> UpdateListingInventoryAsync(
        long listingId, EtsyUpdateListingInventoryRequest request)
        => await PutJsonAsync<EtsyListingInventory>(
            $"/application/listings/{listingId}/inventory", request, requiresToken: true);

    // ─── Listing Offering ──────────────────────────────────────────

    /// <summary>Retrieves a single offering for a listing product.</summary>
    public async Task<EtsyListingOffering> GetListingOfferingAsync(
        long listingId, long productId, long productOfferingId)
        => await GetAsync<EtsyListingOffering>(
            $"/application/listings/{listingId}/products/{productId}/offerings/{productOfferingId}");

    // ─── Listing Product ───────────────────────────────────────────

    /// <summary>Retrieves a single inventory product for a listing. Requires listings_r scope.</summary>
    public async Task<EtsyListingProduct> GetListingProductAsync(long listingId, long productId)
        => await GetAsync<EtsyListingProduct>(
            $"/application/listings/{listingId}/inventory/products/{productId}", requiresToken: true);

    // ─── Listing Variation Images ──────────────────────────────────

    /// <summary>Gets all variation images for a listing (links images to property values).</summary>
    public async Task<EtsyPagedResult<EtsyVariationImage>> GetListingVariationImagesAsync(long shopId, long listingId)
        => await GetAsync<EtsyPagedResult<EtsyVariationImage>>(
            $"/application/shops/{shopId}/listings/{listingId}/variation-images");

    /// <summary>Updates variation image associations. Requires listings_w scope.</summary>
    public async Task<EtsyPagedResult<EtsyVariationImage>> UpdateVariationImagesAsync(
        long shopId, long listingId, EtsyUpdateVariationImagesRequest request)
        => await PostJsonAsync<EtsyPagedResult<EtsyVariationImage>>(
            $"/application/shops/{shopId}/listings/{listingId}/variation-images", request, requiresToken: true);

    // ─── Listing Personalization ───────────────────────────────────

    /// <summary>Retrieves personalization questions for a listing.</summary>
    public async Task<EtsyListingPersonalization> GetListingPersonalizationAsync(long listingId)
        => await GetAsync<EtsyListingPersonalization>($"/application/listings/{listingId}/personalization");

    /// <summary>Creates or replaces personalization settings for a listing. Requires listings_w scope.</summary>
    public async Task<EtsyListingPersonalization> UpdateListingPersonalizationAsync(
        long shopId, long listingId, EtsyUpdateListingPersonalizationRequest request)
    {
        var form = new Dictionary<string, string>
        {
            ["is_personalizable"] = request.IsPersonalizable.ToString().ToLower()
        };
        if (request.PersonalizationCharLimit.HasValue)
            form["personalization_char_count_max"] = request.PersonalizationCharLimit.Value.ToString();
        if (request.PersonalizationInstructions != null)
            form["personalization_instructions"] = request.PersonalizationInstructions;
        return await PostFormAsync<EtsyListingPersonalization>(
            $"/application/shops/{shopId}/listings/{listingId}/personalization", form, requiresToken: true);
    }

    /// <summary>Deletes personalization from a listing. Requires listings_w scope.</summary>
    public async Task DeleteListingPersonalizationAsync(long shopId, long listingId)
        => await DeleteAsync($"/application/shops/{shopId}/listings/{listingId}/personalization", requiresToken: true);

    // ─── Listing Translations ──────────────────────────────────────

    /// <summary>Retrieves a listing translation for the specified language.</summary>
    public async Task<EtsyListingTranslation> GetListingTranslationAsync(long shopId, long listingId, string language)
        => await GetAsync<EtsyListingTranslation>(
            $"/application/shops/{shopId}/listings/{listingId}/translations/{Uri.EscapeDataString(language)}");

    /// <summary>Creates a listing translation. Requires listings_w scope.</summary>
    public async Task<EtsyListingTranslation> CreateListingTranslationAsync(
        long shopId, long listingId, string language, EtsyListingTranslationRequest request)
    {
        var form = new Dictionary<string, string>
        {
            ["title"]       = request.Title,
            ["description"] = request.Description
        };
        if (request.Tags != null) form["tags[]"] = string.Join(",", request.Tags);
        return await PostFormAsync<EtsyListingTranslation>(
            $"/application/shops/{shopId}/listings/{listingId}/translations/{Uri.EscapeDataString(language)}",
            form, requiresToken: true);
    }

    /// <summary>Updates a listing translation. Requires listings_w scope.</summary>
    public async Task<EtsyListingTranslation> UpdateListingTranslationAsync(
        long shopId, long listingId, string language, EtsyListingTranslationRequest request)
    {
        var form = new Dictionary<string, string>
        {
            ["title"]       = request.Title,
            ["description"] = request.Description
        };
        if (request.Tags != null) form["tags[]"] = string.Join(",", request.Tags);
        return await PutFormAsync<EtsyListingTranslation>(
            $"/application/shops/{shopId}/listings/{listingId}/translations/{Uri.EscapeDataString(language)}",
            form, requiresToken: true);
    }

    // ─── Receipts (Orders) ─────────────────────────────────────────

    /// <summary>Retrieves a single receipt. Requires transactions_r scope.</summary>
    public async Task<EtsyReceipt> GetShopReceiptAsync(long shopId, long receiptId)
        => await GetAsync<EtsyReceipt>($"/application/shops/{shopId}/receipts/{receiptId}", requiresToken: true);

    /// <summary>
    /// Lists receipts for a shop. Requires transactions_r scope.
    /// </summary>
    public async Task<EtsyPagedResult<EtsyReceipt>> GetShopReceiptsAsync(
        long shopId, int limit = 25, int offset = 0,
        string sortOn = "created", string sortOrder = "desc",
        bool? isPaid = null, bool? isShipped = null, bool? isCanceled = null,
        long? minCreated = null, long? maxCreated = null,
        long? minLastModified = null, long? maxLastModified = null)
    {
        var qs = BuildQueryString(new()
        {
            ["limit"]             = limit.ToString(),
            ["offset"]            = offset.ToString(),
            ["sort_on"]           = sortOn,
            ["sort_order"]        = sortOrder,
            ["was_paid"]          = isPaid?.ToString().ToLower(),
            ["was_shipped"]       = isShipped?.ToString().ToLower(),
            ["is_canceled"]       = isCanceled?.ToString().ToLower(),
            ["min_created"]       = minCreated?.ToString(),
            ["max_created"]       = maxCreated?.ToString(),
            ["min_last_modified"] = minLastModified?.ToString(),
            ["max_last_modified"] = maxLastModified?.ToString()
        });
        return await GetAsync<EtsyPagedResult<EtsyReceipt>>($"/application/shops/{shopId}/receipts{qs}", requiresToken: true);
    }

    /// <summary>
    /// Retrieves all receipts for a shop, automatically paging through results.
    /// Requires transactions_r scope.
    /// </summary>
    public async Task<List<EtsyReceipt>> GetAllShopReceiptsAsync(
        long shopId, bool? isPaid = null, bool? isShipped = null, bool? isCanceled = null)
    {
        var all = new List<EtsyReceipt>();
        int offset = 0, limit = 100;
        while (true)
        {
            var page = await GetShopReceiptsAsync(shopId, limit: limit, offset: offset,
                isPaid: isPaid, isShipped: isShipped, isCanceled: isCanceled);
            all.AddRange(page.Results);
            if (all.Count >= page.Count || page.Results.Count < limit) break;
            offset += limit;
        }
        return all;
    }

    /// <summary>Updates a receipt (mark shipped, add note, etc.). Requires transactions_w scope.</summary>
    public async Task<EtsyReceipt> UpdateShopReceiptAsync(long shopId, long receiptId, EtsyUpdateReceiptRequest request)
    {
        var form = new Dictionary<string, string>();
        if (request.WasPaid.HasValue)      form["was_paid"]      = request.WasPaid.Value.ToString().ToLower();
        if (request.WasShipped.HasValue)   form["was_shipped"]   = request.WasShipped.Value.ToString().ToLower();
        if (request.WasDelivered.HasValue) form["was_delivered"] = request.WasDelivered.Value.ToString().ToLower();
        if (request.IsCanceled.HasValue)   form["is_canceled"]   = request.IsCanceled.Value.ToString().ToLower();
        if (request.Note != null)          form["message_from_seller"] = request.Note;
        return await PutFormAsync<EtsyReceipt>($"/application/shops/{shopId}/receipts/{receiptId}", form, requiresToken: true);
    }

    /// <summary>Submits shipment tracking for a receipt. Requires transactions_w scope.</summary>
    public async Task<EtsyReceipt> CreateReceiptShipmentAsync(long shopId, long receiptId, EtsyCreateShipmentRequest request)
    {
        var form = new Dictionary<string, string>();
        if (request.TrackingCode != null) form["tracking_code"] = request.TrackingCode;
        if (request.CarrierName  != null) form["carrier_name"]  = request.CarrierName;
        if (request.SendBcc.HasValue)     form["send_bcc"]      = request.SendBcc.Value.ToString().ToLower();
        if (request.NoteToBuyer != null)  form["note_to_buyer"] = request.NoteToBuyer;
        return await PostFormAsync<EtsyReceipt>(
            $"/application/shops/{shopId}/receipts/{receiptId}/tracking", form, requiresToken: true);
    }

    // ─── Transactions ──────────────────────────────────────────────

    /// <summary>Retrieves a single transaction. Requires transactions_r scope.</summary>
    public async Task<EtsyTransaction> GetShopReceiptTransactionAsync(long shopId, long transactionId)
        => await GetAsync<EtsyTransaction>($"/application/shops/{shopId}/transactions/{transactionId}", requiresToken: true);

    /// <summary>Lists all transactions for a shop. Requires transactions_r scope.</summary>
    public async Task<EtsyPagedResult<EtsyTransaction>> GetShopReceiptTransactionsByShopAsync(
        long shopId, int limit = 25, int offset = 0)
        => await GetAsync<EtsyPagedResult<EtsyTransaction>>(
            $"/application/shops/{shopId}/transactions?limit={limit}&offset={offset}", requiresToken: true);

    /// <summary>Lists transactions for a specific receipt. Requires transactions_r scope.</summary>
    public async Task<EtsyPagedResult<EtsyTransaction>> GetShopReceiptTransactionsByReceiptAsync(
        long shopId, long receiptId)
        => await GetAsync<EtsyPagedResult<EtsyTransaction>>(
            $"/application/shops/{shopId}/receipts/{receiptId}/transactions", requiresToken: true);

    /// <summary>Lists transactions for a specific listing. Requires transactions_r scope.</summary>
    public async Task<EtsyPagedResult<EtsyTransaction>> GetShopReceiptTransactionsByListingAsync(
        long shopId, long listingId, int limit = 25, int offset = 0)
        => await GetAsync<EtsyPagedResult<EtsyTransaction>>(
            $"/application/shops/{shopId}/listings/{listingId}/transactions?limit={limit}&offset={offset}",
            requiresToken: true);

    // ─── Payments ──────────────────────────────────────────────────

    /// <summary>Retrieves payments for a shop by payment IDs. Requires transactions_r scope.</summary>
    public async Task<EtsyPagedResult<EtsyPayment>> GetPaymentsAsync(long shopId, IEnumerable<long> paymentIds)
    {
        var ids = string.Join("&", paymentIds.Select(id => $"payment_ids[]={id}"));
        return await GetAsync<EtsyPagedResult<EtsyPayment>>($"/application/shops/{shopId}/payments?{ids}", requiresToken: true);
    }

    /// <summary>Retrieves payments associated with a receipt. Requires transactions_r scope.</summary>
    public async Task<EtsyPagedResult<EtsyPayment>> GetShopPaymentByReceiptIdAsync(long shopId, long receiptId)
        => await GetAsync<EtsyPagedResult<EtsyPayment>>(
            $"/application/shops/{shopId}/receipts/{receiptId}/payments", requiresToken: true);

    // ─── Ledger Entries ────────────────────────────────────────────

    /// <summary>Retrieves a single ledger entry. Requires transactions_r scope.</summary>
    public async Task<EtsyLedgerEntry> GetShopPaymentAccountLedgerEntryAsync(long shopId, long ledgerEntryId)
        => await GetAsync<EtsyLedgerEntry>(
            $"/application/shops/{shopId}/payment-account/ledger-entries/{ledgerEntryId}", requiresToken: true);

    /// <summary>
    /// Lists ledger entries for a shop's payment account. Requires transactions_r scope.
    /// </summary>
    public async Task<EtsyPagedResult<EtsyLedgerEntry>> GetShopPaymentAccountLedgerEntriesAsync(
        long shopId, int limit = 25, int offset = 0,
        long? minCreated = null, long? maxCreated = null)
    {
        var qs = BuildQueryString(new()
        {
            ["limit"]       = limit.ToString(),
            ["offset"]      = offset.ToString(),
            ["min_created"] = minCreated?.ToString(),
            ["max_created"] = maxCreated?.ToString()
        });
        return await GetAsync<EtsyPagedResult<EtsyLedgerEntry>>(
            $"/application/shops/{shopId}/payment-account/ledger-entries{qs}", requiresToken: true);
    }

    /// <summary>Retrieves payments for given ledger entry IDs. Requires transactions_r scope.</summary>
    public async Task<EtsyPagedResult<EtsyPayment>> GetPaymentAccountLedgerEntryPaymentsAsync(
        long shopId, IEnumerable<long> ledgerEntryIds)
    {
        var ids = string.Join("&", ledgerEntryIds.Select(id => $"ledger_entry_ids[]={id}"));
        return await GetAsync<EtsyPagedResult<EtsyPayment>>(
            $"/application/shops/{shopId}/payment-account/ledger-entries/payments?{ids}", requiresToken: true);
    }

    // ─── Reviews ───────────────────────────────────────────────────

    /// <summary>Lists reviews for a listing.</summary>
    public async Task<EtsyPagedResult<EtsyReview>> GetReviewsByListingAsync(
        long listingId, int limit = 25, int offset = 0,
        long? minCreated = null, long? maxCreated = null)
    {
        var qs = BuildQueryString(new()
        {
            ["limit"]       = limit.ToString(),
            ["offset"]      = offset.ToString(),
            ["min_created"] = minCreated?.ToString(),
            ["max_created"] = maxCreated?.ToString()
        });
        return await GetAsync<EtsyPagedResult<EtsyReview>>($"/application/listings/{listingId}/reviews{qs}");
    }

    /// <summary>Lists reviews for a shop.</summary>
    public async Task<EtsyPagedResult<EtsyReview>> GetReviewsByShopAsync(
        long shopId, int limit = 25, int offset = 0,
        long? minCreated = null, long? maxCreated = null)
    {
        var qs = BuildQueryString(new()
        {
            ["limit"]       = limit.ToString(),
            ["offset"]      = offset.ToString(),
            ["min_created"] = minCreated?.ToString(),
            ["max_created"] = maxCreated?.ToString()
        });
        return await GetAsync<EtsyPagedResult<EtsyReview>>($"/application/shops/{shopId}/reviews{qs}");
    }

    // ─── Shipping Profiles ─────────────────────────────────────────

    /// <summary>Lists available shipping carriers for a country of origin.</summary>
    public async Task<EtsyPagedResult<EtsyShippingCarrier>> GetShippingCarriersAsync(string originCountryIso)
        => await GetAsync<EtsyPagedResult<EtsyShippingCarrier>>(
            $"/application/shipping-carriers?origin_country_iso={Uri.EscapeDataString(originCountryIso)}");

    /// <summary>Lists all shipping profiles for a shop. Requires shops_r scope.</summary>
    public async Task<EtsyPagedResult<EtsyShippingProfile>> GetShopShippingProfilesAsync(long shopId)
        => await GetAsync<EtsyPagedResult<EtsyShippingProfile>>(
            $"/application/shops/{shopId}/shipping-profiles", requiresToken: true);

    /// <summary>Retrieves a single shipping profile. Requires shops_r scope.</summary>
    public async Task<EtsyShippingProfile> GetShopShippingProfileAsync(long shopId, long shippingProfileId)
        => await GetAsync<EtsyShippingProfile>(
            $"/application/shops/{shopId}/shipping-profiles/{shippingProfileId}", requiresToken: true);

    /// <summary>Creates a shipping profile. Requires shops_w scope.</summary>
    public async Task<EtsyShippingProfile> CreateShopShippingProfileAsync(
        long shopId, EtsyCreateShippingProfileRequest request)
    {
        var form = new Dictionary<string, string>
        {
            ["title"]                    = request.Title,
            ["origin_country_iso"]       = request.OriginCountryIso,
            ["primary_cost"]             = request.PrimaryCost.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
            ["secondary_cost"]           = request.SecondaryCost.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
            ["destination_country_iso"]  = request.DestinationCountryIso,
        };
        if (request.OriginPostalCode  != null) form["origin_postal_code"]   = request.OriginPostalCode;
        if (request.DestinationRegion != null) form["destination_region"]   = request.DestinationRegion;
        if (request.ShippingCarrierId != null) form["shipping_carrier_id"]  = request.ShippingCarrierId.Value.ToString();
        if (request.MailClass         != null) form["mail_class"]           = request.MailClass;
        if (request.MinDeliveryDays   != null) form["min_delivery_days"]    = request.MinDeliveryDays.Value.ToString();
        if (request.MaxDeliveryDays   != null) form["max_delivery_days"]    = request.MaxDeliveryDays.Value.ToString();
        return await PostFormAsync<EtsyShippingProfile>(
            $"/application/shops/{shopId}/shipping-profiles", form, requiresToken: true);
    }

    /// <summary>Updates a shipping profile. Requires shops_w scope.</summary>
    public async Task<EtsyShippingProfile> UpdateShopShippingProfileAsync(
        long shopId, long shippingProfileId, Dictionary<string, string> fields)
        => await PutFormAsync<EtsyShippingProfile>(
            $"/application/shops/{shopId}/shipping-profiles/{shippingProfileId}", fields, requiresToken: true);

    /// <summary>Deletes a shipping profile. Requires shops_w scope.</summary>
    public async Task DeleteShopShippingProfileAsync(long shopId, long shippingProfileId)
        => await DeleteAsync($"/application/shops/{shopId}/shipping-profiles/{shippingProfileId}", requiresToken: true);

    /// <summary>Lists destinations for a shipping profile. Requires shops_r scope.</summary>
    public async Task<EtsyPagedResult<EtsyShippingProfileDestination>> GetShopShippingProfileDestinationsAsync(
        long shopId, long shippingProfileId, int limit = 25, int offset = 0)
        => await GetAsync<EtsyPagedResult<EtsyShippingProfileDestination>>(
            $"/application/shops/{shopId}/shipping-profiles/{shippingProfileId}/destinations?limit={limit}&offset={offset}",
            requiresToken: true);

    /// <summary>Creates a shipping destination. Requires shops_w scope.</summary>
    public async Task<EtsyShippingProfileDestination> CreateShopShippingProfileDestinationAsync(
        long shopId, long shippingProfileId, EtsyCreateShippingProfileDestinationRequest request)
    {
        var form = new Dictionary<string, string>
        {
            ["primary_cost"]             = request.PrimaryCost.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
            ["secondary_cost"]           = request.SecondaryCost.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
            ["origin_country_iso"]       = request.OriginCountryIso,
            ["destination_country_iso"]  = request.DestinationCountryIso,
        };
        if (request.DestinationRegion != null) form["destination_region"]  = request.DestinationRegion;
        if (request.ShippingCarrierId != null) form["shipping_carrier_id"] = request.ShippingCarrierId.Value.ToString();
        if (request.MailClass         != null) form["mail_class"]          = request.MailClass;
        if (request.MinDeliveryDays   != null) form["min_delivery_days"]   = request.MinDeliveryDays.Value.ToString();
        if (request.MaxDeliveryDays   != null) form["max_delivery_days"]   = request.MaxDeliveryDays.Value.ToString();
        return await PostFormAsync<EtsyShippingProfileDestination>(
            $"/application/shops/{shopId}/shipping-profiles/{shippingProfileId}/destinations",
            form, requiresToken: true);
    }

    /// <summary>Updates a shipping destination. Requires shops_w scope.</summary>
    public async Task<EtsyShippingProfileDestination> UpdateShopShippingProfileDestinationAsync(
        long shopId, long shippingProfileId, long destinationId, EtsyCreateShippingProfileDestinationRequest request)
    {
        var form = new Dictionary<string, string>
        {
            ["primary_cost"]             = request.PrimaryCost.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
            ["secondary_cost"]           = request.SecondaryCost.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
            ["origin_country_iso"]       = request.OriginCountryIso,
            ["destination_country_iso"]  = request.DestinationCountryIso,
        };
        if (request.DestinationRegion != null) form["destination_region"]  = request.DestinationRegion;
        if (request.ShippingCarrierId != null) form["shipping_carrier_id"] = request.ShippingCarrierId.Value.ToString();
        if (request.MailClass         != null) form["mail_class"]          = request.MailClass;
        if (request.MinDeliveryDays   != null) form["min_delivery_days"]   = request.MinDeliveryDays.Value.ToString();
        if (request.MaxDeliveryDays   != null) form["max_delivery_days"]   = request.MaxDeliveryDays.Value.ToString();
        return await PutFormAsync<EtsyShippingProfileDestination>(
            $"/application/shops/{shopId}/shipping-profiles/{shippingProfileId}/destinations/{destinationId}",
            form, requiresToken: true);
    }

    /// <summary>Deletes a shipping destination. Requires shops_w scope.</summary>
    public async Task DeleteShopShippingProfileDestinationAsync(
        long shopId, long shippingProfileId, long destinationId)
        => await DeleteAsync(
            $"/application/shops/{shopId}/shipping-profiles/{shippingProfileId}/destinations/{destinationId}",
            requiresToken: true);

    /// <summary>Lists shipping upgrades for a profile. Requires shops_r scope.</summary>
    public async Task<EtsyPagedResult<EtsyShippingProfileUpgrade>> GetShopShippingProfileUpgradesAsync(
        long shopId, long shippingProfileId)
        => await GetAsync<EtsyPagedResult<EtsyShippingProfileUpgrade>>(
            $"/application/shops/{shopId}/shipping-profiles/{shippingProfileId}/upgrades", requiresToken: true);

    /// <summary>Creates a shipping upgrade. Requires shops_w scope.</summary>
    public async Task<EtsyShippingProfileUpgrade> CreateShopShippingProfileUpgradeAsync(
        long shopId, long shippingProfileId, EtsyCreateShippingProfileUpgradeRequest request)
    {
        var form = new Dictionary<string, string>
        {
            ["type"]            = request.Type,
            ["upgrade_name"]    = request.UpgradeName,
            ["price"]           = request.Price.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
            ["secondary_price"] = request.SecondaryPrice.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
        };
        if (request.ShippingCarrierId != null) form["shipping_carrier_id"] = request.ShippingCarrierId.Value.ToString();
        if (request.MailClass         != null) form["mail_class"]          = request.MailClass;
        if (request.MinDeliveryDays   != null) form["min_delivery_days"]   = request.MinDeliveryDays.Value.ToString();
        if (request.MaxDeliveryDays   != null) form["max_delivery_days"]   = request.MaxDeliveryDays.Value.ToString();
        return await PostFormAsync<EtsyShippingProfileUpgrade>(
            $"/application/shops/{shopId}/shipping-profiles/{shippingProfileId}/upgrades", form, requiresToken: true);
    }

    /// <summary>Updates a shipping upgrade. Requires shops_w scope.</summary>
    public async Task<EtsyShippingProfileUpgrade> UpdateShopShippingProfileUpgradeAsync(
        long shopId, long shippingProfileId, long upgradeId, Dictionary<string, string> fields)
        => await PutFormAsync<EtsyShippingProfileUpgrade>(
            $"/application/shops/{shopId}/shipping-profiles/{shippingProfileId}/upgrades/{upgradeId}",
            fields, requiresToken: true);

    // ─── HTTP Helpers ──────────────────────────────────────────────

    private async Task<T> GetAsync<T>(string path, bool requiresToken = false)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}{path}");
        AddBearerToken(request, requiresToken);
        var response = await _http.SendAsync(request);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOpts)!;
    }

    private async Task<T> PostFormAsync<T>(string path, Dictionary<string, string> form, bool requiresToken = false)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}{path}")
        {
            Content = new FormUrlEncodedContent(form)
        };
        AddBearerToken(request, requiresToken);
        var response = await _http.SendAsync(request);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOpts)!;
    }

    private async Task<T> PostJsonAsync<T>(string path, object body, bool requiresToken = false)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}{path}")
        {
            Content = new StringContent(JsonSerializer.Serialize(body, JsonOpts), Encoding.UTF8, "application/json")
        };
        AddBearerToken(request, requiresToken);
        var response = await _http.SendAsync(request);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOpts)!;
    }

    private async Task<T> PostMultipartAsync<T>(string path, MultipartFormDataContent form, bool requiresToken = false)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl}{path}") { Content = form };
        AddBearerToken(request, requiresToken);
        var response = await _http.SendAsync(request);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOpts)!;
    }

    private async Task<T> PutFormAsync<T>(string path, Dictionary<string, string> form, bool requiresToken = false)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"{BaseUrl}{path}")
        {
            Content = new FormUrlEncodedContent(form)
        };
        AddBearerToken(request, requiresToken);
        var response = await _http.SendAsync(request);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOpts)!;
    }

    private async Task<T> PutJsonAsync<T>(string path, object body, bool requiresToken = false)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"{BaseUrl}{path}")
        {
            Content = new StringContent(JsonSerializer.Serialize(body, JsonOpts), Encoding.UTF8, "application/json")
        };
        AddBearerToken(request, requiresToken);
        var response = await _http.SendAsync(request);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOpts)!;
    }

    private async Task<T> PatchFormAsync<T>(string path, Dictionary<string, string> form, bool requiresToken = false)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"{BaseUrl}{path}")
        {
            Content = new FormUrlEncodedContent(form)
        };
        AddBearerToken(request, requiresToken);
        var response = await _http.SendAsync(request);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOpts)!;
    }

    private async Task DeleteAsync(string path, bool requiresToken = false)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}{path}");
        AddBearerToken(request, requiresToken);
        var response = await _http.SendAsync(request);
        await EnsureSuccessAsync(response);
    }

    private void AddBearerToken(HttpRequestMessage request, bool required)
    {
        if (_accessToken != null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        else if (required)
            throw new InvalidOperationException(
                "This endpoint requires an OAuth2 access token. Call ExchangeCodeForTokenAsync() first.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new EtsyApiException((int)response.StatusCode, body);
        }
    }

    private static string BuildQueryString(Dictionary<string, string?> parameters)
    {
        var pairs = parameters
            .Where(kvp => kvp.Value != null)
            .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value!)}");
        var qs = string.Join("&", pairs);
        return qs.Length > 0 ? $"?{qs}" : "";
    }

    private static Dictionary<string, string> ToFormDict(object obj)
    {
        var dict = new Dictionary<string, string>();
        foreach (var prop in obj.GetType().GetProperties())
        {
            var value = prop.GetValue(obj);
            if (value == null) continue;
            // Convert PascalCase to snake_case using JsonPropertyName if present, else manual
            var jsonAttr = prop.GetCustomAttributes(typeof(JsonPropertyNameAttribute), false)
                               .OfType<JsonPropertyNameAttribute>()
                               .FirstOrDefault();
            var key = jsonAttr?.Name ?? ToSnakeCase(prop.Name);
            dict[key] = value.ToString()!;
        }
        return dict;
    }

    private static string ToSnakeCase(string name)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            if (char.IsUpper(name[i]) && i > 0) sb.Append('_');
            sb.Append(char.ToLower(name[i]));
        }
        return sb.ToString();
    }

    private static string Base64UrlEncode(byte[] input)
        => Convert.ToBase64String(input)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
}
