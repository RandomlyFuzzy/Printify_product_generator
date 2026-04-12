using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Platform-agnostic interface for managing a product seller storefront.
///
/// Implementations wrap a specific marketplace API (e.g. eBay, Etsy) and translate
/// to/from the shared data models in <c>SellerModels.cs</c>.
///
/// <para><b>Platform fees:</b> Every implementation exposes a <see cref="Fees"/> property
/// with the current standard fee schedule so callers can estimate costs before taking action.</para>
///
/// <example>
/// <code>
/// ISellerClient client = new EtsySellerClientV1(etsyClient, shopId);
///
/// // Create and immediately publish a listing  ($0.20 Etsy listing fee)
/// var listing = await client.CreateListingAsync(new CreateListingRequest
/// {
///     Title       = "Custom Art Print",
///     Description = "High-quality giclée print.",
///     Price       = 24.99m,
///     Quantity    = 50,
///     AutoPublish = true
/// });
///
/// // Add a size variant
/// await client.UpsertSkuAsync(listing.ListingId, new SellerSku
/// {
///     Sku        = "PRINT-LG",
///     Price      = 29.99m,
///     Quantity   = 20,
///     Attributes = new() { ["Size"] = "Large" }
/// });
///
/// // Estimate total cost for a $24.99 sale
/// decimal cost = client.Fees.EstimatedCost(24.99m, includeListingFee: true);
/// // → $0.20 + $1.62 + $1.00 = $2.82 on Etsy (US, Etsy Payments)
/// </code>
/// </example>
/// </summary>
public interface ISellerClient
{
    // ── Identity &amp; Pricing ─────────────────────────────────────────────────────

    /// <summary>Human-readable platform name (e.g. "eBay", "Etsy").</summary>
    string PlatformName { get; }

    /// <summary>
    /// Fee schedule for this platform.
    /// Costs are informational and reflect standard US rates;
    /// verify current rates at the platform's official fee page.
    /// </summary>
    SellerPlatformFees Fees { get; }

    // ── Shop ─────────────────────────────────────────────────────────────────
    // Free operations – no platform charges for reading or updating shop info.

    /// <summary>Retrieves the configured storefront.</summary>
    Task<SellerShop> GetShopAsync(CancellationToken ct = default);

    /// <summary>
    /// Updates mutable storefront fields (title, description, announcement, etc.).
    /// Only non-null fields in <paramref name="request"/> are sent.
    /// </summary>
    Task<SellerShop> UpdateShopAsync(UpdateShopRequest request, CancellationToken ct = default);

    // ── Listings ──────────────────────────────────────────────────────────────
    // Cost per new listing: see <see cref="SellerPlatformFees.ListingFeeUsd"/>.

    /// <summary>
    /// Creates a new product listing.
    /// <para><b>Cost:</b> <see cref="SellerPlatformFees.ListingFeeUsd"/> per listing created
    /// (eBay: $0.35 after 250 free/month; Etsy: $0.20, renews every 4 months).</para>
    /// Set <see cref="CreateListingRequest.AutoPublish"/> to <c>true</c> to
    /// activate immediately; otherwise a draft is saved (Etsy only drafts count toward quota
    /// when published).
    /// </summary>
    Task<SellerListing> CreateListingAsync(CreateListingRequest request, CancellationToken ct = default);

    /// <summary>Retrieves a single listing by its platform ID.</summary>
    Task<SellerListing> GetListingAsync(string listingId, CancellationToken ct = default);

    /// <summary>
    /// Returns a page of listings, optionally filtered by <paramref name="state"/>.
    /// </summary>
    Task<SellerPagedResult<SellerListing>> GetListingsAsync(
        int limit = 25, int offset = 0,
        SellerListingState? state = null,
        CancellationToken ct = default);

    /// <summary>
    /// Updates an existing listing. Only non-null fields in <paramref name="request"/> are changed.
    /// </summary>
    Task<SellerListing> UpdateListingAsync(
        string listingId, UpdateListingRequest request, CancellationToken ct = default);

    /// <summary>
    /// Permanently deletes a listing and all its variants.
    /// <para><b>Warning:</b> On Etsy a deleted listing cannot be recovered.
    /// On eBay, deleting the underlying inventory item also removes any live offer.</para>
    /// </summary>
    Task DeleteListingAsync(string listingId, CancellationToken ct = default);

    /// <summary>
    /// Publishes or activates an existing draft listing.
    /// <para><b>Cost:</b> On Etsy, publication triggers the $0.20 listing fee.
    /// On eBay, the listing insertion fee may apply if the seller's free-listing quota
    /// is exhausted.</para>
    /// </summary>
    Task<SellerListing> PublishListingAsync(string listingId, CancellationToken ct = default);

    // ── Images ────────────────────────────────────────────────────────────────
    // Image uploads are typically free; storage is included in the listing fee.

    /// <summary>
    /// Uploads raw image bytes to an existing listing at the given rank/position.
    /// Returns the platform image record containing the assigned ID and public URL.
    /// </summary>
    Task<SellerUploadedImage> UploadListingImageAsync(
        string listingId, byte[] imageData, string fileName,
        int rank = 1, CancellationToken ct = default);

    /// <summary>Removes an uploaded image from a listing.</summary>
    Task DeleteListingImageAsync(string listingId, string imageId, CancellationToken ct = default);

    // ── SKUs / Variants ───────────────────────────────────────────────────────
    // eBay: each SKU is an inventory item; offers are created per SKU per marketplace.
    // Etsy: SKUs live on listing-inventory products/offerings.

    /// <summary>Returns all SKUs (variants / offerings) attached to a listing.</summary>
    Task<List<SellerSku>> GetSkusAsync(string listingId, CancellationToken ct = default);

    /// <summary>
    /// Adds a new SKU or replaces an existing one (matched by <see cref="SellerSku.Sku"/>).
    /// <para><b>Cost:</b> On eBay, adding a new offer may trigger an insertion fee once
    /// the free-insertion quota is exhausted.</para>
    /// </summary>
    Task<SellerSku> UpsertSkuAsync(string listingId, SellerSku sku, CancellationToken ct = default);

    /// <summary>
    /// Replaces the full set of SKUs for a listing in a single call.
    /// Existing SKUs not present in <paramref name="skus"/> are removed.
    /// </summary>
    Task<List<SellerSku>> SetSkusAsync(
        string listingId, List<SellerSku> skus, CancellationToken ct = default);

    /// <summary>Removes a single SKU/variant from a listing.</summary>
    Task DeleteSkuAsync(string listingId, string sku, CancellationToken ct = default);

    // ── Orders ────────────────────────────────────────────────────────────────
    // Orders are read-only from a fee perspective; platform fees are deducted at sale time.
    // Final Value Fee: see <see cref="SellerPlatformFees.TransactionFeeRate"/>.

    /// <summary>Retrieves a single order by its platform ID.</summary>
    Task<SellerOrder> GetOrderAsync(string orderId, CancellationToken ct = default);

    /// <summary>
    /// Returns a page of orders, optionally filtered by <paramref name="filter"/>.
    /// <para><b>Cost note:</b> eBay Final Value Fee (13.25% + $0.30) and Etsy
    /// transaction fee (6.5% + 3% + $0.25) are deducted automatically at the time of sale.</para>
    /// </summary>
    Task<SellerPagedResult<SellerOrder>> GetOrdersAsync(
        OrderFilter? filter = null, int limit = 25, int offset = 0,
        CancellationToken ct = default);

    /// <summary>
    /// Records a shipment for an order and notifies the buyer.
    /// <para>Providing valid tracking protects against eBay defect metrics and
    /// satisfies Etsy's on-time shipping requirement.</para>
    /// </summary>
    Task FulfillOrderAsync(
        string orderId, FulfillOrderRequest request, CancellationToken ct = default);

    /// <summary>
    /// Issues a full or partial refund on a paid order.
    /// Pass <see cref="IssueRefundRequest.Amount"/> as <c>null</c> for a full refund.
    /// <para><b>Note:</b> On Etsy, refunds cannot be issued via the Open API v3;
    /// the implementation will throw <see cref="System.NotSupportedException"/>.</para>
    /// </summary>
    Task IssueRefundAsync(
        string orderId, IssueRefundRequest request, CancellationToken ct = default);

    /// <summary>
    /// Cancels an order (marks it as cancelled on the platform).
    /// <para><b>Note:</b> On eBay, post-payment cancellations require the Trading API;
    /// the implementation will throw <see cref="System.NotSupportedException"/>.</para>
    /// </summary>
    Task CancelOrderAsync(string orderId, string reason, CancellationToken ct = default);
}
