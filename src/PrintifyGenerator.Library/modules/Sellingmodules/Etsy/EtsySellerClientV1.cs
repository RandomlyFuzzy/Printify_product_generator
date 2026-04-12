using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Etsy v1 implementation of <see cref="ISellerClient"/>.
/// Wraps <see cref="EtsyClient"/> (Open API v3) and maps platform-specific types
/// to the shared seller models.
///
/// <para><b>Etsy Model:</b> A listing is identified by its <c>listing_id</c> (long).
/// Variants are stored as <em>inventory products</em> on the listing, each with one or more
/// <em>offerings</em> (price + quantity pairs).  <see cref="SellerSku.Sku"/> maps to the
/// Etsy listing product's <c>sku</c> field.</para>
///
/// <para><b>Required scopes:</b>
/// <c>listings_r</c>, <c>listings_w</c>, <c>listings_d</c>,
/// <c>shops_r</c>, <c>shops_w</c>,
/// <c>transactions_r</c>, <c>transactions_w</c>.
/// </para>
///
/// <para><b>Platform-specific fields:</b> Supply Etsy-only fields via
/// <see cref="CreateListingRequest.PlatformExtensions"/>:
/// <c>taxonomy_id</c> (long), <c>shipping_profile_id</c> (long),
/// <c>return_policy_id</c> (long), <c>who_made</c>, <c>when_made</c>,
/// <c>shop_section_id</c> (long).</para>
///
/// <example>
/// <code>
/// var etsy   = new EtsyClient(apiKey, accessToken);
/// ISellerClient seller = new EtsySellerClientV1(etsy, shopId: 12345678L);
///
/// var listing = await seller.CreateListingAsync(new CreateListingRequest
/// {
///     Title       = "Custom Wall Art",
///     Description = "12×16 inch fine-art print.",
///     Price       = 29.99m,
///     Quantity    = 25,
///     AutoPublish = true,
///     PlatformExtensions = new()
///     {
///         ["taxonomy_id"]         = 1L,        // Etsy category
///         ["shipping_profile_id"] = 987654321L,
///         ["return_policy_id"]    = 123456789L
///     }
/// });
/// </code>
/// </example>
/// </summary>
public class EtsySellerClientV1 : ISellerClient
{
    private readonly EtsyClient _etsy;
    private readonly long       _shopId;

    /// <param name="etsy">Initialized <see cref="EtsyClient"/> with a valid OAuth access token.</param>
    /// <param name="shopId">Numeric Etsy shop ID of the authenticated seller.</param>
    public EtsySellerClientV1(EtsyClient etsy, long shopId)
    {
        _etsy   = etsy;
        _shopId = shopId;
    }

    // ── Identity & Pricing ───────────────────────────────────────────────────

    public string PlatformName => "Etsy";

    /// <summary>
    /// Etsy US fee schedule with Etsy Payments enabled.
    /// <list type="bullet">
    ///   <item>Listing fee: $0.20 per listing, renews every 4 months or upon sale.</item>
    ///   <item>Transaction fee: 6.5% of total sale amount (item + shipping + gift wrap).</item>
    ///   <item>Payment processing (US): 3% + $0.25 per transaction.</item>
    ///   <item>Offsite Ads: 15% for shops earning &lt;$10K/year (optional opt-out for these shops);
    ///         12% for shops with ≥$10K/year in sales (mandatory, applies only to referred sales).</item>
    ///   <item>Currency conversion: 2.5% for non-local-currency sales.</item>
    /// </list>
    /// See https://www.etsy.com/help/article/40 for current rates.
    /// </summary>
    public SellerPlatformFees Fees { get; } = new()
    {
        ListingFeeUsd               = 0.20m,
        TransactionFeeRate          = 0.065m,
        PaymentProcessingRate       = 0.03m,
        PaymentProcessingFlatFeeUsd = 0.25m,
        Notes =
            "Listing fee of $0.20 renews every 4 months or when the listing sells. " +
            "Transaction fee (6.5%) applies to item price + shipping cost + gift wrapping. " +
            "Payment processing (US): 3% + $0.25 per transaction. " +
            "Offsite Ads: 15% commission on referred sales for shops under $10K/year revenue " +
            "(opt-out available); 12% mandatory for shops at or above $10K/year. " +
            "Currency conversion fee of 2.5% for sales in a non-native currency. " +
            "See https://www.etsy.com/help/article/40 for full details."
    };

    // ── Shop ─────────────────────────────────────────────────────────────────

    public async Task<SellerShop> GetShopAsync(CancellationToken ct = default)
    {
        var shop = await _etsy.GetShopAsync(_shopId);
        return MapShop(shop);
    }

    public async Task<SellerShop> UpdateShopAsync(UpdateShopRequest request, CancellationToken ct = default)
    {
        var etsyReq = new EtsyUpdateShopRequest
        {
            Title        = request.Title,
            Announcement = request.Description
        };
        var shop = await _etsy.UpdateShopAsync(_shopId, etsyReq);
        return MapShop(shop);
    }

    // ── Listings ─────────────────────────────────────────────────────────────

    public async Task<SellerListing> CreateListingAsync(
        CreateListingRequest request, CancellationToken ct = default)
    {
        var draftReq = new EtsyCreateDraftListingRequest
        {
            Title       = request.Title,
            Description = request.Description,
            Price       = (float)request.Price,
            Quantity    = request.Quantity,
            WhoMade     = ExtOrDefault(request.PlatformExtensions, "who_made",  "i_did"),
            WhenMade    = ExtOrDefault(request.PlatformExtensions, "when_made", "made_to_order"),
            Tags        = request.Tags?.ToArray(),
            TaxonomyId  = ExtOrNullLong(request.PlatformExtensions,  "taxonomy_id"),
            ShippingProfileId = ExtOrNullLong(request.PlatformExtensions, "shipping_profile_id"),
            ReturnPolicyId    = ExtOrNullLong(request.PlatformExtensions, "return_policy_id"),
            ShopSectionId     = ExtOrNullLong(request.PlatformExtensions, "shop_section_id")
        };

        var listing = await _etsy.CreateDraftListingAsync(_shopId, draftReq);

        if (request.AutoPublish)
            listing = await _etsy.UpdateListingAsync(_shopId, listing.ListingId,
                new EtsyUpdateListingRequest { State = "active" });

        return MapListing(listing);
    }

    public async Task<SellerListing> GetListingAsync(string listingId, CancellationToken ct = default)
    {
        var listing = await _etsy.GetListingAsync(ParseId(listingId), new[] { "Images" });
        return MapListing(listing);
    }

    public async Task<SellerPagedResult<SellerListing>> GetListingsAsync(
        int limit = 25, int offset = 0, SellerListingState? state = null,
        CancellationToken ct = default)
    {
        var etsyState = state switch
        {
            SellerListingState.Active   => "active",
            SellerListingState.Inactive => "inactive",
            SellerListingState.Draft    => "draft",
            SellerListingState.Expired  => "expired",
            _                           => "active"
        };

        var resp = await _etsy.GetListingsByShopAsync(
            _shopId, state: etsyState, limit: limit, offset: offset, includes: new[] { "Images" });

        return new SellerPagedResult<SellerListing>
        {
            Items  = resp.Results.Select(MapListing).ToList(),
            Total  = resp.Count,
            Limit  = limit,
            Offset = offset
        };
    }

    public async Task<SellerListing> UpdateListingAsync(
        string listingId, UpdateListingRequest request, CancellationToken ct = default)
    {
        var etsyReq = new EtsyUpdateListingRequest
        {
            Title       = request.Title,
            Description = request.Description,
            Price       = request.Price.HasValue ? (float?)request.Price.Value : null,
            Quantity    = request.Quantity,
            Tags        = request.Tags?.ToArray(),
            State       = request.State switch
            {
                SellerListingState.Active   => "active",
                SellerListingState.Inactive => "inactive",
                _                           => null
            }
        };

        var listing = await _etsy.UpdateListingAsync(_shopId, ParseId(listingId), etsyReq);
        return MapListing(listing);
    }

    public async Task DeleteListingAsync(string listingId, CancellationToken ct = default)
        => await _etsy.DeleteListingAsync(ParseId(listingId));

    public async Task<SellerListing> PublishListingAsync(string listingId, CancellationToken ct = default)
    {
        var listing = await _etsy.UpdateListingAsync(_shopId, ParseId(listingId),
            new EtsyUpdateListingRequest { State = "active" });
        return MapListing(listing);
    }

    // ── Images ───────────────────────────────────────────────────────────────

    public async Task<SellerUploadedImage> UploadListingImageAsync(
        string listingId, byte[] imageData, string fileName,
        int rank = 1, CancellationToken ct = default)
    {
        var image = await _etsy.UploadListingImageAsync(
            _shopId, ParseId(listingId),
            imageBytes:    imageData,
            imageFileName: fileName,
            rank:          rank);

        return new SellerUploadedImage
        {
            ImageId = image.ListingImageId.ToString(),
            Url     = image.UrlFullxfull ?? image.Url570xN,
            Rank    = image.Rank
        };
    }

    public async Task DeleteListingImageAsync(
        string listingId, string imageId, CancellationToken ct = default)
        => await _etsy.DeleteListingImageAsync(_shopId, ParseId(listingId), ParseId(imageId));

    // ── SKUs / Variants ──────────────────────────────────────────────────────

    public async Task<List<SellerSku>> GetSkusAsync(string listingId, CancellationToken ct = default)
    {
        var inventory = await _etsy.GetListingInventoryAsync(ParseId(listingId));

        return inventory.Products
            .Where(p => !p.IsDeleted)
            .SelectMany(p => p.Offerings
                .Where(o => o.IsEnabled && !o.IsDeleted)
                .Select(o => new SellerSku
                {
                    Sku      = p.Sku ?? "",
                    Price    = (decimal?)o.Price.ToDecimal(),
                    Quantity = o.Quantity,
                    Attributes = p.PropertyValues?.Count > 0
                        ? p.PropertyValues.ToDictionary(
                            pv => pv.PropertyName,
                            pv => pv.Values.FirstOrDefault() ?? "")
                        : null
                }))
            .ToList();
    }

    public async Task<SellerSku> UpsertSkuAsync(
        string listingId, SellerSku sku, CancellationToken ct = default)
    {
        var inventory = await _etsy.GetListingInventoryAsync(ParseId(listingId));
        var existing  = inventory.Products.FirstOrDefault(p => p.Sku == sku.Sku && !p.IsDeleted);

        EtsyInventoryProductRequest productReq;

        if (existing != null)
        {
            // Rebuild from the existing product, updating price/quantity on each enabled offering
            productReq = new EtsyInventoryProductRequest
            {
                ProductId      = existing.ProductId,
                Sku            = existing.Sku,
                PropertyValues = existing.PropertyValues,
                Offerings      = existing.Offerings
                    .Where(o => !o.IsDeleted)
                    .Select(o => new EtsyInventoryOfferingRequest
                    {
                        OfferingId = o.OfferingId,
                        IsEnabled  = o.IsEnabled,
                        Quantity   = sku.Quantity ?? o.Quantity,
                        Price      = sku.Price.HasValue ? (float)sku.Price.Value : o.Price.ToDecimal()
                    })
                    .ToList()
            };

            // Replace the existing record in the product list
            var updateList = inventory.Products
                .Where(p => !(p.Sku == sku.Sku && !p.IsDeleted))
                .Select(ToProductRequest)
                .ToList();
            updateList.Add(productReq);
            await _etsy.UpdateListingInventoryAsync(
                ParseId(listingId),
                new EtsyUpdateListingInventoryRequest { Products = updateList });
        }
        else
        {
            // Add a new product entry
            productReq = new EtsyInventoryProductRequest
            {
                Sku       = sku.Sku,
                Offerings = new List<EtsyInventoryOfferingRequest>
                {
                    new()
                    {
                        IsEnabled = true,
                        Quantity  = sku.Quantity ?? 0,
                        Price     = sku.Price.HasValue ? (float)sku.Price.Value : 0f
                    }
                }
            };

            var updateList = inventory.Products
                .Where(p => !p.IsDeleted)
                .Select(ToProductRequest)
                .ToList();
            updateList.Add(productReq);
            await _etsy.UpdateListingInventoryAsync(
                ParseId(listingId),
                new EtsyUpdateListingInventoryRequest { Products = updateList });
        }

        return sku;
    }

    public async Task<List<SellerSku>> SetSkusAsync(
        string listingId, List<SellerSku> skus, CancellationToken ct = default)
    {
        var products = skus.Select(sku => new EtsyInventoryProductRequest
        {
            Sku      = sku.Sku,
            Offerings = new List<EtsyInventoryOfferingRequest>
            {
                new()
                {
                    IsEnabled = true,
                    Quantity  = sku.Quantity ?? 0,
                    Price     = sku.Price.HasValue ? (float)sku.Price.Value : 0f
                }
            }
        }).ToList();

        await _etsy.UpdateListingInventoryAsync(
            ParseId(listingId),
            new EtsyUpdateListingInventoryRequest { Products = products });

        return skus;
    }

    public async Task DeleteSkuAsync(string listingId, string sku, CancellationToken ct = default)
    {
        var inventory = await _etsy.GetListingInventoryAsync(ParseId(listingId));
        var remaining = inventory.Products
            .Where(p => !(p.Sku == sku) && !p.IsDeleted)
            .Select(ToProductRequest)
            .ToList();

        await _etsy.UpdateListingInventoryAsync(
            ParseId(listingId),
            new EtsyUpdateListingInventoryRequest { Products = remaining });
    }

    // ── Orders ───────────────────────────────────────────────────────────────

    public async Task<SellerOrder> GetOrderAsync(string orderId, CancellationToken ct = default)
    {
        var receipt = await _etsy.GetShopReceiptAsync(_shopId, ParseId(orderId));
        return MapReceipt(receipt);
    }

    public async Task<SellerPagedResult<SellerOrder>> GetOrdersAsync(
        OrderFilter? filter = null, int limit = 25, int offset = 0,
        CancellationToken ct = default)
    {
        var resp = await _etsy.GetShopReceiptsAsync(
            _shopId,
            limit:          limit,
            offset:         offset,
            isPaid:         filter?.IsPaid,
            isShipped:      filter?.IsFulfilled,
            isCanceled:     filter?.IsCancelled,
            minCreated:     filter?.CreatedAfter?.ToUnixTimeSeconds(),
            maxCreated:     filter?.CreatedBefore?.ToUnixTimeSeconds());

        return new SellerPagedResult<SellerOrder>
        {
            Items  = resp.Results.Select(MapReceipt).ToList(),
            Total  = resp.Count,
            Limit  = limit,
            Offset = offset
        };
    }

    public async Task FulfillOrderAsync(
        string orderId, FulfillOrderRequest request, CancellationToken ct = default)
    {
        var shipReq = new EtsyCreateShipmentRequest
        {
            TrackingCode = request.TrackingNumber,
            CarrierName  = request.Carrier,
            NoteToBuyer  = request.NoteToBuyer
        };
        await _etsy.CreateReceiptShipmentAsync(_shopId, ParseId(orderId), shipReq);
    }

    /// <summary>
    /// Etsy Open API v3 does not expose a refund endpoint.
    /// Issue refunds via the Etsy Seller Dashboard (Orders &amp; Shipping → Refund).
    /// </summary>
    public Task IssueRefundAsync(
        string orderId, IssueRefundRequest request, CancellationToken ct = default)
        => throw new NotSupportedException(
            "Etsy Open API v3 does not provide a refund endpoint. " +
            "Issue refunds via the Etsy Seller Dashboard under Orders & Shipping.");

    public async Task CancelOrderAsync(string orderId, string reason, CancellationToken ct = default)
        => await _etsy.UpdateShopReceiptAsync(_shopId, ParseId(orderId),
               new EtsyUpdateReceiptRequest { IsCanceled = true });

    // ── Private helpers ──────────────────────────────────────────────────────

    private static SellerShop MapShop(EtsyShop shop) => new()
    {
        ShopId             = shop.ShopId.ToString(),
        ShopName           = shop.ShopName,
        Description        = shop.Title ?? shop.Announcement,
        Url                = shop.Url,
        CurrencyCode       = shop.CurrencyCode,
        ActiveListingCount = shop.ListingActiveCount,
        CreatedAt          = DateTimeOffset.FromUnixTimeSeconds(shop.CreatedTimestamp)
    };

    private static SellerListing MapListing(EtsyListing l) => new()
    {
        ListingId    = l.ListingId.ToString(),
        Title        = l.Title,
        Description  = l.Description,
        Price        = (decimal)(l.Price?.ToDecimal() ?? 0m),
        CurrencyCode = l.Price?.CurrencyCode ?? "USD",
        Quantity     = l.Quantity,
        State        = l.State?.ToLowerInvariant() switch
        {
            "active"   => SellerListingState.Active,
            "inactive" => SellerListingState.Inactive,
            "draft"    => SellerListingState.Draft,
            "expired"  => SellerListingState.Expired,
            "removed"  => SellerListingState.Removed,
            _          => SellerListingState.Draft
        },
        Tags      = l.Tags,
        ImageUrls = l.Images?
            .OrderBy(i => i.Rank)
            .Select(i => i.UrlFullxfull ?? i.Url570xN ?? "")
            .Where(u => !string.IsNullOrEmpty(u))
            .ToList(),
        Url       = l.Url,
        CreatedAt = l.CreatedTimestamp > 0
            ? DateTimeOffset.FromUnixTimeSeconds(l.CreatedTimestamp) : null,
        UpdatedAt = l.UpdatedTimestamp > 0
            ? DateTimeOffset.FromUnixTimeSeconds(l.UpdatedTimestamp) : null
    };

    private static SellerOrder MapReceipt(EtsyReceipt r) => new()
    {
        OrderId        = r.ReceiptId.ToString(),
        Status         = r.Status ?? (r.IsShipped ? "shipped" : r.IsPaid ? "paid" : "pending"),
        TotalAmount    = r.Grandtotal?.ToDecimal() ?? r.TotalPrice?.ToDecimal() ?? 0m,
        ShippingAmount = r.TotalShippingCost?.ToDecimal() ?? 0m,
        CurrencyCode   = r.Grandtotal?.CurrencyCode ?? "USD",
        IsPaid         = r.IsPaid,
        IsFulfilled    = r.IsShipped,
        IsCancelled    = false,   // EtsyReceipt in Open API v3 has no is_cancelled field
        BuyerNote      = r.MessageFromBuyer,
        CreatedAt      = DateTimeOffset.FromUnixTimeSeconds(r.CreatedTimestamp),
        UpdatedAt      = r.UpdatedTimestamp > 0
            ? DateTimeOffset.FromUnixTimeSeconds(r.UpdatedTimestamp) : null,
        ShippingAddress = new SellerAddress
        {
            Name            = r.Name,
            Street1         = r.FirstLine,
            Street2         = r.SecondLine,
            City            = r.City,
            StateOrProvince = r.State,
            PostalCode      = r.Zip,
            CountryCode     = r.CountryIso
        },
        LineItems = (r.Transactions ?? new List<EtsyTransaction>())
            .Select(t => new SellerOrderLine
            {
                LineItemId = t.TransactionId.ToString(),
                ListingId  = t.ListingId?.ToString(),
                Sku        = t.Sku,
                Title      = t.Title,
                Quantity   = t.Quantity,
                UnitPrice  = t.Price?.ToDecimal() ?? 0m
            })
            .ToList()
    };

    /// <summary>Converts a read-model <see cref="EtsyListingProduct"/> to the write-model request type.</summary>
    private static EtsyInventoryProductRequest ToProductRequest(EtsyListingProduct p) =>
        new()
        {
            ProductId      = p.ProductId,
            Sku            = p.Sku,
            PropertyValues = p.PropertyValues,
            Offerings      = p.Offerings
                .Where(o => !o.IsDeleted)
                .Select(o => new EtsyInventoryOfferingRequest
                {
                    OfferingId = o.OfferingId,
                    IsEnabled  = o.IsEnabled,
                    Quantity   = o.Quantity,
                    Price      = (float)o.Price.ToDecimal()
                })
                .ToList()
        };

    private static long ParseId(string id) => long.Parse(id);

    private static string ExtOrDefault(Dictionary<string, object?>? ext, string key, string fallback)
    {
        if (ext != null && ext.TryGetValue(key, out var v) && v is string s && !string.IsNullOrEmpty(s))
            return s;
        return fallback;
    }

    private static long? ExtOrNullLong(Dictionary<string, object?>? ext, string key)
    {
        if (ext == null) return null;
        if (!ext.TryGetValue(key, out var v)) return null;
        return v switch
        {
            long l   => l,
            int  i   => i,
            string s => long.TryParse(s, out var parsed) ? parsed : null,
            _        => null
        };
    }
}
