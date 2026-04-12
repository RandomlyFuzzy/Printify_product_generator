using System;
using System.Collections.Generic;

// ─────────────────────────────────────────────────────────────────────────────
// Shared data models for ISellerClient.
// All monetary values are in the shop's native currency unless noted otherwise.
// ─────────────────────────────────────────────────────────────────────────────

// ── Shop ─────────────────────────────────────────────────────────────────────

/// <summary>A normalized representation of a seller storefront.</summary>
public class SellerShop
{
    public string  ShopId             { get; set; } = "";
    public string  ShopName           { get; set; } = "";
    public string? Description        { get; set; }
    public string? Url                { get; set; }
    public string  CurrencyCode       { get; set; } = "GBP";
    public int     ActiveListingCount { get; set; }
    public DateTimeOffset? CreatedAt  { get; set; }
}

/// <summary>Fields that can be updated on a shop. Omit any field to leave it unchanged.</summary>
public class UpdateShopRequest
{
    public string? Title       { get; set; }
    public string? Description { get; set; }
    /// <summary>Platform-specific extension values (e.g. Etsy sale_message, eBay store URL).</summary>
    public Dictionary<string, object?>? PlatformExtensions { get; set; }
}

// ── Listings ──────────────────────────────────────────────────────────────────

/// <summary>Lifecycle state of a listing on the platform.</summary>
public enum SellerListingState
{
    Draft,
    Active,
    Inactive,
    Expired,
    Removed
}

/// <summary>A normalized product listing.</summary>
public class SellerListing
{
    public string  ListingId    { get; set; } = "";
    public string  Title        { get; set; } = "";
    public string? Description  { get; set; }
    public decimal Price        { get; set; }
    public string  CurrencyCode { get; set; } = "USD";
    public int     Quantity     { get; set; }
    public SellerListingState State { get; set; } = SellerListingState.Draft;
    public List<string>?      Tags       { get; set; }
    public List<string>?      ImageUrls  { get; set; }
    public List<SellerSku>?   Skus       { get; set; }
    public string?            Url        { get; set; }
    public DateTimeOffset?    CreatedAt  { get; set; }
    public DateTimeOffset?    UpdatedAt  { get; set; }
}

/// <summary>
/// Request to create a new listing.
/// Use <see cref="PlatformExtensions"/> to supply platform-specific fields
/// (e.g. eBay <c>categoryId</c>, <c>fulfillmentPolicyId</c>; Etsy <c>taxonomy_id</c>, <c>shipping_profile_id</c>).
/// </summary>
public class CreateListingRequest
{
    public required string  Title       { get; set; }
    public required string  Description { get; set; }
    /// <summary>Base price. On Etsy this maps to the single-offering price; on eBay to the offer price.</summary>
    public required decimal Price       { get; set; }
    public required int     Quantity    { get; set; }
    public string           CurrencyCode { get; set; } = "GBP";
    public List<string>?    Tags        { get; set; }
    /// <summary>
    /// When <c>true</c>, the listing is published/activated immediately after creation.
    /// When <c>false</c> (default), it is saved as a draft.
    /// </summary>
    public bool             AutoPublish { get; set; } = false;
    /// <summary>Platform-specific extension values passed through to the underlying API call.</summary>
    public Dictionary<string, object?>? PlatformExtensions { get; set; }
}

/// <summary>Fields to modify on an existing listing. Omit any field to leave it unchanged.</summary>
public class UpdateListingRequest
{
    public string?  Title       { get; set; }
    public string? Description  { get; set; }
    public decimal? Price       { get; set; }
    public int?     Quantity    { get; set; }
    public List<string>? Tags   { get; set; }
    /// <summary>
    /// Target state transition. Platforms only support a subset of transitions
    /// (e.g. Draft→Active via <see cref="ISellerClient.PublishListingAsync"/>).
    /// </summary>
    public SellerListingState? State { get; set; }
    /// <summary>Platform-specific extension values.</summary>
    public Dictionary<string, object?>? PlatformExtensions { get; set; }
}

// ── Images ────────────────────────────────────────────────────────────────────

/// <summary>An image that has been uploaded to a listing.</summary>
public class SellerUploadedImage
{
    /// <summary>Platform-assigned image identifier.</summary>
    public string  ImageId { get; set; } = "";
    /// <summary>Public URL of the full-size image, if available after upload.</summary>
    public string? Url     { get; set; }
    /// <summary>Display rank / position (1-based).</summary>
    public int     Rank    { get; set; } = 1;
}

// ── SKUs / Variants ───────────────────────────────────────────────────────────

/// <summary>
/// A single variant/offering on a listing (e.g. Size=Large, Color=Red).
/// On eBay this corresponds to an inventory-item SKU; on Etsy to a listing product + offering.
/// </summary>
public class SellerSku
{
    public required string  Sku        { get; set; }
    public decimal?         Price      { get; set; }
    public int?             Quantity   { get; set; }
    /// <summary>Variant attributes, e.g. <c>{ "Size": "Large", "Color": "Red" }</c>.</summary>
    public Dictionary<string, string>? Attributes { get; set; }
}

// ── Orders ────────────────────────────────────────────────────────────────────

/// <summary>Filter criteria for <see cref="ISellerClient.GetOrdersAsync"/>.</summary>
public class OrderFilter
{
    public bool?           IsPaid      { get; set; }
    public bool?           IsFulfilled { get; set; }
    public bool?           IsCancelled { get; set; }
    public DateTimeOffset? CreatedAfter  { get; set; }
    public DateTimeOffset? CreatedBefore { get; set; }
}

/// <summary>A normalized representation of a buyer order.</summary>
public class SellerOrder
{
    public string  OrderId        { get; set; } = "";
    public string  Status         { get; set; } = "";
    public decimal TotalAmount    { get; set; }
    public decimal ShippingAmount { get; set; }
    public string  CurrencyCode   { get; set; } = "USD";
    public bool    IsPaid         { get; set; }
    public bool    IsFulfilled    { get; set; }
    public bool    IsCancelled    { get; set; }
    public string? BuyerNote      { get; set; }
    public DateTimeOffset  CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public SellerAddress?           ShippingAddress { get; set; }
    public List<SellerOrderLine>    LineItems       { get; set; } = new();
}

/// <summary>A single item within an order.</summary>
public class SellerOrderLine
{
    public string  LineItemId { get; set; } = "";
    public string? ListingId  { get; set; }
    public string? Sku        { get; set; }
    public string  Title      { get; set; } = "";
    public int     Quantity   { get; set; }
    public decimal UnitPrice  { get; set; }
}

/// <summary>A shipping address.</summary>
public class SellerAddress
{
    public string? Name            { get; set; }
    public string? Street1         { get; set; }
    public string? Street2         { get; set; }
    public string? City            { get; set; }
    public string? StateOrProvince { get; set; }
    public string? PostalCode      { get; set; }
    public string? CountryCode     { get; set; }
}

/// <summary>Request to mark an order as shipped and record tracking.</summary>
public class FulfillOrderRequest
{
    public required string Carrier        { get; set; }
    public required string TrackingNumber { get; set; }
    /// <summary>
    /// Optional list of line-item IDs to fulfill (eBay only).
    /// When <c>null</c>, all line items in the order are fulfilled.
    /// </summary>
    public List<string>? LineItemIds { get; set; }
    public string?       NoteToBuyer { get; set; }
}

/// <summary>Request to issue a refund on an order.</summary>
public class IssueRefundRequest
{
    /// <summary>Refund amount. Pass <c>null</c> to request a full refund.</summary>
    public decimal? Amount  { get; set; }
    public required string Reason  { get; set; }
    public string?         Comment { get; set; }
}

// ── Paging ────────────────────────────────────────────────────────────────────

/// <summary>A page of results returned by list operations.</summary>
public class SellerPagedResult<T>
{
    public List<T> Items  { get; set; } = new();
    public int     Total  { get; set; }
    public int     Limit  { get; set; }
    public int     Offset { get; set; }
    public bool    HasMore => Offset + Items.Count < Total;
}

// ── Platform Fees ─────────────────────────────────────────────────────────────

/// <summary>
/// Indicative fee schedule for a selling platform.
/// Values represent the standard US rates and may not reflect all tiers,
/// promotional discounts, store subscriptions, or category exceptions.
/// Always verify current rates on the platform's official fee page.
/// </summary>
public class SellerPlatformFees
{
    /// <summary>
    /// Flat fee charged per listing created (USD).
    /// On eBay: $0.35 after the first 250 free insertions per calendar month.
    /// On Etsy: $0.20 per listing (renews every 4 months or upon sale).
    /// </summary>
    public decimal ListingFeeUsd { get; set; }

    /// <summary>
    /// Final-value / transaction fee as a fraction of the total sale amount (0.0–1.0).
    /// On eBay: 0.1325 (13.25%) for most categories.
    /// On Etsy: 0.065 (6.5%) on item price + shipping + gift wrap.
    /// </summary>
    public decimal TransactionFeeRate { get; set; }

    /// <summary>
    /// Payment processing percentage fee as a fraction (0.0–1.0).
    /// On eBay: included in the Final Value Fee (Managed Payments).
    /// On Etsy: 0.03 (3%).
    /// </summary>
    public decimal PaymentProcessingRate { get; set; }

    /// <summary>
    /// Flat payment processing fee per transaction (USD).
    /// On eBay: $0.30 per order (part of Final Value Fee).
    /// On Etsy: $0.25 per transaction.
    /// </summary>
    public decimal PaymentProcessingFlatFeeUsd { get; set; }

    /// <summary>
    /// Human-readable notes about tiers, exceptions, optional fees, and official fee-page links.
    /// </summary>
    public string Notes { get; set; } = "";

    /// <summary>
    /// Calculates the estimated total platform cost for a single sale.
    /// </summary>
    /// <param name="saleAmount">Total charged to the buyer (item + shipping).</param>
    /// <param name="includeListingFee">Include the per-listing fee in the estimate.</param>
    /// <returns>Estimated platform fees in USD.</returns>
    public decimal EstimatedCost(decimal saleAmount, bool includeListingFee = false)
    {
        var listing    = includeListingFee ? ListingFeeUsd : 0m;
        var txFee      = saleAmount * TransactionFeeRate;
        var processing = saleAmount * PaymentProcessingRate + PaymentProcessingFlatFeeUsd;
        return listing + txFee + processing;
    }
}
