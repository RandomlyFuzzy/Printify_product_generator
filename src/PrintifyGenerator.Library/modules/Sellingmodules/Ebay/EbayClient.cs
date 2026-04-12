/// <summary>
/// Main façade for the eBay Sell APIs.
/// Compose all sub-clients under one entry point, sharing a single HttpClient and OAuth session.
///
/// Quick-start:
/// <code>
///   var config = new EbayConfig
///   {
///       ClientId     = "your-app-id",
///       ClientSecret = "your-cert-id",
///       RuName       = "your-ru-name",
///       UseSandbox   = true   // set false for production
///   };
///
///   var ebay = new EbayClient(config);
///
///   // OAuth – build the URL to redirect the user to for consent
///   var consentUrl = ebay.OAuth.BuildUserConsentUrl(EbayConfig.DefaultSellerScopes, state: "my-state");
///   // … redirect user, receive code, then:
///   await ebay.OAuth.ExchangeCodeForTokenAsync(authorizationCode);
///
///   // Inventory
///   await ebay.Inventory.CreateOrReplaceInventoryItemAsync(sku, item);
///   var list = await ebay.Inventory.GetInventoryItemsAsync(limit: 100);
///
///   // Orders
///   var orders = await ebay.Fulfillment.GetOrdersAsync(filter: "orderfulfillmentstatus:{NOT_STARTED}");
///
///   // Policies
///   var policies = await ebay.Account.GetFulfillmentPoliciesAsync();
///
///   // Marketing
///   var campaigns = await ebay.Marketing.GetCampaignsAsync();
/// </code>
/// </summary>
public class EbayClient : IDisposable
{
    private readonly HttpClient _http;
    private readonly bool _ownsHttp;

    // ──────────────────────────────────────────────────────────────────────────
    // Sub-clients (public surface)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>OAuth 2.0 token management (client credentials + authorization code + refresh).</summary>
    public EbayOAuthClient OAuth { get; }

    /// <summary>eBay Inventory API (v1) — items, offers, locations, listing migration.</summary>
    public EbayInventoryClient Inventory { get; }

    /// <summary>eBay Fulfillment API (v1) — orders, shipping, refunds, payment disputes.</summary>
    public EbayFulfillmentClient Fulfillment { get; }

    /// <summary>eBay Account API (v1) / Finances API — business policies, payouts, transactions.</summary>
    public EbayAccountClient Account { get; }

    /// <summary>eBay Marketing API (v1) — Promoted Listings campaigns, ads, item promotions.</summary>
    public EbayMarketingClient Marketing { get; }

    /// <summary>The config used to initialize this client (read-only after construction).</summary>
    public EbayConfig Config { get; }

    // ──────────────────────────────────────────────────────────────────────────
    // Construction
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates an EbayClient.  All sub-clients share a single HttpClient and OAuth session.
    /// </summary>
    /// <param name="config">Credentials and environment settings.</param>
    /// <param name="http">
    /// Optional pre-configured HttpClient to reuse (e.g. one with a retry handler).
    /// When omitted a new HttpClient is created and disposed with this instance.
    /// </param>
    public EbayClient(EbayConfig config, HttpClient? http = null)
    {
        Config = config;

        _ownsHttp = http == null;
        _http = http ?? new HttpClient();

        OAuth = new EbayOAuthClient(config, _http);
        Inventory = new EbayInventoryClient(config, OAuth, _http);
        Fulfillment = new EbayFulfillmentClient(config, OAuth, _http);
        Account = new EbayAccountClient(config, OAuth, _http);
        Marketing = new EbayMarketingClient(config, OAuth, _http);
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Convenience helpers
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Loads a previously-obtained user token (e.g. from a secure store) into the OAuth client,
    /// so that sub-clients can make immediate API calls without a new consent flow.
    /// </summary>
    public void LoadUserToken(EbayTokenResponse token) => OAuth.SetUserToken(token);

    /// <summary>
    /// Returns the currently cached user token (if any), so it can be persisted externally.
    /// Returns null if no user token has been acquired yet.
    /// </summary>
    public EbayTokenResponse? GetCurrentUserToken() => OAuth.GetCurrentUserToken();

    // ──────────────────────────────────────────────────────────────────────────
    // IDisposable
    // ──────────────────────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_ownsHttp) _http.Dispose();
    }
}
