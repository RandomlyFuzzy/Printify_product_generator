/// <summary>
/// Configuration for the eBay API client.
/// Store credentials in environment variables or a secrets manager — never hard-code them.
/// </summary>
public class EbayConfig
{
    /// <summary>Application Client ID (App ID) from the eBay Developer Portal.</summary>
    public string ClientId { get; set; } = "";

    /// <summary>Application Client Secret (Cert ID) from the eBay Developer Portal. Keep this confidential.</summary>
    public string ClientSecret { get; set; } = "";

    /// <summary>eBay RuName (Redirect URL Name) used in the OAuth authorization code flow.</summary>
    public string RuName { get; set; } = "";

    /// <summary>Target eBay marketplace identifier (e.g. EBAY_US, EBAY_GB, EBAY_DE).</summary>
    public string MarketplaceId { get; set; } = "EBAY_US";

    /// <summary>When true, all calls target the eBay Sandbox environment instead of Production.</summary>
    public bool UseSandbox { get; set; } = false;

    /// <summary>Base REST API URL resolved from the sandbox flag.</summary>
    public string BaseUrl => UseSandbox
        ? "https://api.sandbox.ebay.com"
        : "https://api.ebay.com";

    /// <summary>Base OAuth authorization page URL resolved from the sandbox flag.</summary>
    public string AuthUrl => UseSandbox
        ? "https://auth.sandbox.ebay.com"
        : "https://auth.ebay.com";

    /// <summary>
    /// Default OAuth scopes required for a full seller application.
    /// Trim this list to only the scopes your application actually needs.
    /// </summary>
    public static readonly string[] DefaultSellerScopes =
    [
        "https://api.ebay.com/oauth/api_scope",
        "https://api.ebay.com/oauth/api_scope/sell.inventory",
        "https://api.ebay.com/oauth/api_scope/sell.inventory.readonly",
        "https://api.ebay.com/oauth/api_scope/sell.account",
        "https://api.ebay.com/oauth/api_scope/sell.account.readonly",
        "https://api.ebay.com/oauth/api_scope/sell.fulfillment",
        "https://api.ebay.com/oauth/api_scope/sell.fulfillment.readonly",
        "https://api.ebay.com/oauth/api_scope/sell.marketing",
        "https://api.ebay.com/oauth/api_scope/sell.marketing.readonly",
        "https://api.ebay.com/oauth/api_scope/sell.finances",
        "https://api.ebay.com/oauth/api_scope/sell.payment.dispute",
        "https://api.ebay.com/oauth/api_scope/commerce.identity.readonly"
    ];
}
