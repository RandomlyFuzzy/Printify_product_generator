using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// eBay v1 implementation of <see cref="ISellerClient"/>.
/// Wraps the <see cref="EbayClient"/> façade (Inventory + Fulfillment + Account sub-clients)
/// and maps platform-specific types to the shared seller models.
///
/// <para><b>eBay Model:</b> A "listing" in this client corresponds to an <em>offer</em> in the
/// eBay Inventory API.  Inventory items (SKUs) are created first, then an offer is created on top
/// of a SKU to produce a visible listing.  The offer ID is used as the public <c>listingId</c>.</para>
///
/// <para><b>Policy IDs:</b> eBay requires fulfillment, return, and payment policy IDs to publish
/// an offer.  Supply them via <see cref="CreateListingRequest.PlatformExtensions"/> using the keys
/// <c>"fulfillmentPolicyId"</c>, <c>"returnPolicyId"</c>, and <c>"paymentPolicyId"</c>.
/// Category ID can be passed as <c>"categoryId"</c> (defaults to 220 – Clothing, Shoes &amp; Accessories).</para>
///
/// <para><b>Images:</b> The eBay Inventory API references images by URL; raw byte uploads are not
/// supported.  Supply a CDN URL via <c>PlatformExtensions["imageUrl"]</c> on
/// <see cref="CreateListingRequest"/>, or host the bytes yourself and pass the resulting URL.</para>
///
/// <example>
/// <code>
/// var ebay   = new EbayClient(config);
/// ISellerClient seller = new EbaySellerClientV1(ebay, marketplaceId: "EBAY_US");
///
/// var listing = await seller.CreateListingAsync(new CreateListingRequest
/// {
///     Title       = "Awesome T-Shirt",
///     Description = "100% cotton tee.",
///     Price       = 19.99m,
///     Quantity    = 100,
///     AutoPublish = true,
///     PlatformExtensions = new()
///     {
///         ["categoryId"]          = "15687",
///         ["fulfillmentPolicyId"] = "my-ff-policy-id",
///         ["returnPolicyId"]      = "my-rt-policy-id",
///         ["paymentPolicyId"]     = "my-pm-policy-id"
///     }
/// });
/// </code>
/// </example>
/// </summary>
public class EbaySellerClientV1 : ISellerClient
{
    private readonly EbayClient _ebay;
    private readonly string     _marketplaceId;

    /// <param name="ebay">Initialized <see cref="EbayClient"/> with valid OAuth credentials.</param>
    /// <param name="marketplaceId">eBay marketplace identifier (default: <c>"EBAY_US"</c>).</param>
    public EbaySellerClientV1(EbayClient ebay, string marketplaceId = "EBAY_US")
    {
        _ebay          = ebay;
        _marketplaceId = marketplaceId;
    }

    // ── Identity & Pricing ───────────────────────────────────────────────────

    public string PlatformName => "eBay";

    /// <summary>
    /// eBay US Managed Payments fee schedule (no store subscription).
    /// <list type="bullet">
    ///   <item>Insertion fee: $0.35 per listing after 250 free/calendar month.</item>
    ///   <item>Final Value Fee: 13.25% of total sale amount + $0.30 per order (most categories).
    ///         This includes payment processing; no separate processing fee.</item>
    ///   <item>Promoted Listings Standard: variable ad rate (optional, set by seller).</item>
    ///   <item>Store subscriptions reduce/eliminate insertion fees: Starter $4.95/mo,
    ///         Basic $21.95/mo, Premium $59.95/mo, Anchor $299.95/mo.</item>
    /// </list>
    /// See https://www.ebay.com/help/selling/fees-credits-invoices for current rates.
    /// </summary>
    public SellerPlatformFees Fees { get; } = new()
    {
        ListingFeeUsd               = 0.35m,
        TransactionFeeRate          = 0.1325m,
        PaymentProcessingRate       = 0m,      // Included in Final Value Fee
        PaymentProcessingFlatFeeUsd = 0.30m,
        Notes =
            "First 250 listings/month are free (zero-insertion-fee allotment). " +
            "Final Value Fee (13.25% + $0.30) covers both the transaction and payment processing. " +
            "Category exceptions: Motors Vehicles 2.35%, Trading Cards 2.35%, Real Estate flat fee. " +
            "Promoted Listings Standard charges an additional ad fee selected by the seller. " +
            "Store subscriptions lower or eliminate insertion fees. " +
            "See https://www.ebay.com/help/selling/fees-credits-invoices for full details."
    };

    // ── Shop ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns basic shop info derived from the seller's eligibility / privilege data.
    /// The eBay Sell APIs do not expose a dedicated "get my store" endpoint;
    /// full store details are available via the eBay Trading API.
    /// </summary>
    public async Task<SellerShop> GetShopAsync(CancellationToken ct = default)
    {
        var eligibility = await _ebay.Account.GetSellerEligibilityAsync(ct);
        return new SellerShop
        {
            ShopId       = _ebay.Config.ClientId,
            ShopName     = _ebay.Config.ClientId,
            CurrencyCode = "USD",
            Description  = eligibility.SellingStatus
        };
    }

    /// <summary>
    /// eBay store details (title, description) are managed through the Trading API
    /// or My eBay settings pages, not through the Sell APIs.
    /// This method returns the current shop info unchanged.
    /// </summary>
    public async Task<SellerShop> UpdateShopAsync(UpdateShopRequest request, CancellationToken ct = default)
        => await GetShopAsync(ct);

    // ── Listings ─────────────────────────────────────────────────────────────

    public async Task<SellerListing> CreateListingAsync(
        CreateListingRequest request, CancellationToken ct = default)
    {
        var sku = ExtOrDefault(request.PlatformExtensions, "sku", $"sku-{Guid.NewGuid():N}");

        // 1. Create the inventory item (the underlying product record)
        var item = new EbayInventoryItem
        {
            Condition = ExtOrDefault(request.PlatformExtensions, "condition", "NEW"),
            Product   = new EbayProduct
            {
                Title       = request.Title,
                Description = request.Description,
                ImageUrls   = ExtOrDefault(request.PlatformExtensions, "imageUrl", (string?)null) is string imgUrl
                                  ? new List<string> { imgUrl }
                                  : null
            },
            Availability = new EbayAvailability
            {
                ShipToLocationAvailability = new EbayShipToLocationAvailability
                {
                    Quantity = request.Quantity
                }
            }
        };
        await _ebay.Inventory.CreateOrReplaceInventoryItemAsync(sku, item, ct: ct);

        // 2. Create an offer on the inventory item
        var offer = new EbayOfferRequest
        {
            Sku                = sku,
            MarketplaceId      = _marketplaceId,
            Format             = "FIXED_PRICE",
            CategoryId         = ExtOrDefault(request.PlatformExtensions, "categoryId", "220"),
            ListingDescription = request.Description,
            AvailableQuantity  = request.Quantity,
            ListingPolicies    = new EbayListingPolicies
            {
                FulfillmentPolicyId = ExtOrDefault(request.PlatformExtensions, "fulfillmentPolicyId", (string?)null),
                ReturnPolicyId      = ExtOrDefault(request.PlatformExtensions, "returnPolicyId", (string?)null),
                PaymentPolicyId     = ExtOrDefault(request.PlatformExtensions, "paymentPolicyId", (string?)null)
            },
            PricingSummary = new EbayPricingSummary
            {
                Price = new EbayAmount
                {
                    Value    = request.Price.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                    Currency = request.CurrencyCode
                }
            }
        };

        var offerResponse = await _ebay.Inventory.CreateOfferAsync(offer, ct);
        var listingId     = offerResponse.OfferId ?? sku;

        // 3. Optionally publish the offer immediately
        if (request.AutoPublish && !string.IsNullOrEmpty(offerResponse.OfferId))
        {
            var published = await _ebay.Inventory.PublishOfferAsync(offerResponse.OfferId, ct);
            listingId = published.ListingId ?? listingId;
        }

        return new SellerListing
        {
            ListingId    = listingId,
            Title        = request.Title,
            Description  = request.Description,
            Price        = request.Price,
            CurrencyCode = request.CurrencyCode,
            Quantity     = request.Quantity,
            State        = request.AutoPublish ? SellerListingState.Active : SellerListingState.Draft,
            Tags         = request.Tags,
            CreatedAt    = DateTimeOffset.UtcNow
        };
    }

    public async Task<SellerListing> GetListingAsync(
        string listingId, CancellationToken ct = default)
    {
        var offer = await _ebay.Inventory.GetOfferAsync(listingId, ct);
        return await MapOfferWithProductAsync(offer, ct);
    }

    public async Task<SellerPagedResult<SellerListing>> GetListingsAsync(
        int limit = 25, int offset = 0, SellerListingState? state = null,
        CancellationToken ct = default)
    {
        var resp = await _ebay.Inventory.GetInventoryItemsAsync(limit, offset, ct);
        var items = (resp.InventoryItems ?? new List<EbayInventoryItemListing>())
            .Select(i => new SellerListing
            {
                ListingId    = i.Sku ?? "",
                Title        = i.Product?.Title ?? "",
                Description  = i.Product?.Description,
                Quantity     = i.Availability?.ShipToLocationAvailability?.Quantity ?? 0,
                CurrencyCode = "USD",
                State        = SellerListingState.Active
            })
            .ToList();

        return new SellerPagedResult<SellerListing>
        {
            Items  = items,
            Total  = resp.Total,
            Limit  = limit,
            Offset = offset
        };
    }

    public async Task<SellerListing> UpdateListingAsync(
        string listingId, UpdateListingRequest request, CancellationToken ct = default)
    {
        var offer = await _ebay.Inventory.GetOfferAsync(listingId, ct);

        // Update the inventory item's product fields and/or quantity
        if ((request.Title ?? request.Description ?? (object?)request.Quantity) != null
            && offer.Sku != null)
        {
            var item = await _ebay.Inventory.GetInventoryItemAsync(offer.Sku, ct);
            item.Product ??= new EbayProduct { Title = "" };

            if (request.Title       != null) item.Product.Title       = request.Title;
            if (request.Description != null) item.Product.Description = request.Description;

            if (request.Quantity.HasValue &&
                item.Availability?.ShipToLocationAvailability != null)
            {
                item.Availability.ShipToLocationAvailability.Quantity = request.Quantity.Value;
            }

            await _ebay.Inventory.CreateOrReplaceInventoryItemAsync(offer.Sku, item, ct: ct);
        }

        // Update the offer's price if changed
        var updatedOffer = new EbayOfferRequest
        {
            Sku                = offer.Sku ?? "",
            MarketplaceId      = offer.MarketplaceId,
            Format             = offer.Format,
            CategoryId         = offer.CategoryId,
            ListingDescription = request.Description ?? offer.ListingDescription,
            AvailableQuantity  = request.Quantity ?? offer.AvailableQuantity,
            ListingPolicies    = offer.ListingPolicies,
            PricingSummary     = request.Price.HasValue
                ? new EbayPricingSummary
                  {
                      Price = new EbayAmount
                      {
                          Value    = request.Price.Value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                          Currency = offer.PricingSummary?.Price?.Currency ?? "USD"
                      }
                  }
                : offer.PricingSummary
        };

        var result = await _ebay.Inventory.UpdateOfferAsync(listingId, updatedOffer, ct);
        return await MapOfferWithProductAsync(result, ct);
    }

    public async Task DeleteListingAsync(string listingId, CancellationToken ct = default)
    {
        string? sku = null;
        try
        {
            var offer = await _ebay.Inventory.GetOfferAsync(listingId, ct);
            sku = offer.Sku;
            await _ebay.Inventory.DeleteOfferAsync(listingId, ct);
        }
        catch { /* offer may not exist – fall through to SKU deletion */ }

        if (!string.IsNullOrEmpty(sku))
            await _ebay.Inventory.DeleteInventoryItemAsync(sku, ct);
    }

    public async Task<SellerListing> PublishListingAsync(string listingId, CancellationToken ct = default)
    {
        var published = await _ebay.Inventory.PublishOfferAsync(listingId, ct);
        var offer     = await _ebay.Inventory.GetOfferAsync(listingId, ct);
        var result    = await MapOfferWithProductAsync(offer, ct);
        result.ListingId = published.ListingId ?? listingId;
        result.State     = SellerListingState.Active;
        return result;
    }

    // ── Images ───────────────────────────────────────────────────────────────

    /// <summary>
    /// eBay does not accept raw image bytes through the Inventory API.
    /// This method stores the bytes URI pattern in the inventory item so callers
    /// can see the mapping; in production, host the bytes on a CDN and supply the URL
    /// via <c>PlatformExtensions["imageUrl"]</c> on the create/update request instead.
    /// </summary>
    public async Task<SellerUploadedImage> UploadListingImageAsync(
        string listingId, byte[] imageData, string fileName,
        int rank = 1, CancellationToken ct = default)
    {
        var offer = await _ebay.Inventory.GetOfferAsync(listingId, ct);
        if (offer.Sku != null)
        {
            var item = await _ebay.Inventory.GetInventoryItemAsync(offer.Sku, ct);
            item.Product              ??= new EbayProduct { Title = "" };
            item.Product.ImageUrls    ??= new List<string>();

            // Placeholder – replace with your CDN upload logic
            var imageUrl = $"https://cdn.example.com/{Uri.EscapeDataString(fileName)}";
            if (rank - 1 < item.Product.ImageUrls.Count)
                item.Product.ImageUrls[rank - 1] = imageUrl;
            else
                item.Product.ImageUrls.Add(imageUrl);

            await _ebay.Inventory.CreateOrReplaceInventoryItemAsync(offer.Sku, item, ct: ct);
            return new SellerUploadedImage { ImageId = fileName, Url = imageUrl, Rank = rank };
        }

        return new SellerUploadedImage { ImageId = fileName, Rank = rank };
    }

    public async Task DeleteListingImageAsync(string listingId, string imageId, CancellationToken ct = default)
    {
        var offer = await _ebay.Inventory.GetOfferAsync(listingId, ct);
        if (offer.Sku != null)
        {
            var item = await _ebay.Inventory.GetInventoryItemAsync(offer.Sku, ct);
            item.Product?.ImageUrls?.RemoveAll(u => u.Contains(imageId));
            await _ebay.Inventory.CreateOrReplaceInventoryItemAsync(offer.Sku, item, ct: ct);
        }
    }

    // ── SKUs / Variants ──────────────────────────────────────────────────────

    public async Task<List<SellerSku>> GetSkusAsync(string listingId, CancellationToken ct = default)
    {
        // listingId may be an offerId or a SKU – try both
        try
        {
            var offersResp = await _ebay.Inventory.GetOffersAsync(sku: listingId, ct: ct);
            return (offersResp.Offers ?? new List<EbayOfferResponse>())
                .Select(o => new SellerSku
                {
                    Sku      = o.Sku ?? "",
                    Price    = decimal.TryParse(o.PricingSummary?.Price?.Value,
                                   System.Globalization.NumberStyles.Any,
                                   System.Globalization.CultureInfo.InvariantCulture, out var p)
                               ? p : null
                })
                .ToList();
        }
        catch
        {
            return new List<SellerSku>();
        }
    }

    public async Task<SellerSku> UpsertSkuAsync(
        string listingId, SellerSku sku, CancellationToken ct = default)
    {
        var item = new EbayInventoryItem
        {
            Condition = "NEW",
            Product   = new EbayProduct { Title = sku.Sku },
            Availability = new EbayAvailability
            {
                ShipToLocationAvailability = new EbayShipToLocationAvailability
                {
                    Quantity = sku.Quantity ?? 0
                }
            }
        };
        await _ebay.Inventory.CreateOrReplaceInventoryItemAsync(sku.Sku, item, ct: ct);
        return sku;
    }

    public async Task<List<SellerSku>> SetSkusAsync(
        string listingId, List<SellerSku> skus, CancellationToken ct = default)
    {
        var results = new List<SellerSku>();
        foreach (var sku in skus)
            results.Add(await UpsertSkuAsync(listingId, sku, ct));
        return results;
    }

    public async Task DeleteSkuAsync(string listingId, string sku, CancellationToken ct = default)
        => await _ebay.Inventory.DeleteInventoryItemAsync(sku, ct);

    // ── Orders ───────────────────────────────────────────────────────────────

    public async Task<SellerOrder> GetOrderAsync(string orderId, CancellationToken ct = default)
    {
        var order = await _ebay.Fulfillment.GetOrderAsync(orderId, ct);
        return MapOrder(order);
    }

    public async Task<SellerPagedResult<SellerOrder>> GetOrdersAsync(
        OrderFilter? filter = null, int limit = 25, int offset = 0,
        CancellationToken ct = default)
    {
        var ebayFilter = BuildEbayOrderFilter(filter);
        var resp = await _ebay.Fulfillment.GetOrdersAsync(
            filter: ebayFilter, limit: limit, offset: offset, ct: ct);

        return new SellerPagedResult<SellerOrder>
        {
            Items  = (resp.Orders ?? new List<EbayOrder>()).Select(MapOrder).ToList(),
            Total  = resp.Total,
            Limit  = resp.Limit,
            Offset = resp.Offset
        };
    }

    public async Task FulfillOrderAsync(
        string orderId, FulfillOrderRequest request, CancellationToken ct = default)
    {
        var fulfillRequest = new EbayShippingFulfillmentRequest
        {
            LineItems = request.LineItemIds
                ?.Select(id => new EbayFulfillmentLineItem { LineItemId = id, Quantity = 1 })
                .ToList() ?? new List<EbayFulfillmentLineItem>(),
            ShippedDate         = DateTimeOffset.UtcNow.ToString("o"),
            ShippingCarrierCode = request.Carrier,
            TrackingNumber      = request.TrackingNumber
        };
        await _ebay.Fulfillment.CreateShippingFulfillmentAsync(orderId, fulfillRequest, ct);
    }

    public async Task IssueRefundAsync(
        string orderId, IssueRefundRequest request, CancellationToken ct = default)
    {
        var refundReq = new EbayIssueRefundRequest
        {
            ReasonForRefund = request.Reason,
            Comments        = request.Comment != null
                ? new EbayComment { Content = request.Comment }
                : null,
            RefundAmount = request.Amount.HasValue
                ? new EbayAmount
                  {
                      Value    = request.Amount.Value.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
                      Currency = "USD"
                  }
                : null
        };
        await _ebay.Fulfillment.IssueRefundAsync(orderId, refundReq, ct);
    }

    /// <summary>
    /// eBay post-payment cancellations require the Trading API's CancelOrder call
    /// or the Selling Manager flow – they are not exposed in the Sell Fulfillment API.
    /// </summary>
    public Task CancelOrderAsync(string orderId, string reason, CancellationToken ct = default)
        => throw new NotSupportedException(
            "eBay order cancellation via the Sell Fulfillment API is not supported. " +
            "Use the eBay Trading API CancelOrder call or My eBay for cancellations.");

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<SellerListing> MapOfferWithProductAsync(
        EbayOfferResponse offer, CancellationToken ct)
    {
        string title       = "";
        string? description = null;

        if (!string.IsNullOrEmpty(offer.Sku))
        {
            try
            {
                var item  = await _ebay.Inventory.GetInventoryItemAsync(offer.Sku, ct);
                title       = item.Product?.Title ?? "";
                description = item.Product?.Description;
            }
            catch { /* may not exist */ }
        }

        return new SellerListing
        {
            ListingId    = offer.OfferId ?? offer.Sku ?? "",
            Title        = title,
            Description  = description,
            Price        = decimal.TryParse(
                               offer.PricingSummary?.Price?.Value,
                               System.Globalization.NumberStyles.Any,
                               System.Globalization.CultureInfo.InvariantCulture, out var p) ? p : 0m,
            CurrencyCode = offer.PricingSummary?.Price?.Currency ?? "USD",
            Quantity     = offer.AvailableQuantity,
            State        = string.Equals(offer.Status, "PUBLISHED", StringComparison.OrdinalIgnoreCase)
                               ? SellerListingState.Active
                               : SellerListingState.Draft
        };
    }

    private static SellerOrder MapOrder(EbayOrder order)
    {
        var shipStep = order.FulfillmentStartInstructions
            ?.FirstOrDefault()?.ShippingStep;

        var addr = shipStep?.ShipTo?.ContactAddress;

        return new SellerOrder
        {
            OrderId     = order.OrderId ?? "",
            Status      = order.OrderFulfillmentStatus ?? order.OrderPaymentStatus ?? "",
            TotalAmount = decimal.TryParse(order.PricingSummary?.Total?.Value,
                              System.Globalization.NumberStyles.Any,
                              System.Globalization.CultureInfo.InvariantCulture, out var t) ? t : 0m,
            ShippingAmount = decimal.TryParse(order.PricingSummary?.DeliveryCost?.Value,
                                  System.Globalization.NumberStyles.Any,
                                  System.Globalization.CultureInfo.InvariantCulture, out var sc) ? sc : 0m,
            CurrencyCode = order.PricingSummary?.Total?.Currency ?? "USD",
            IsPaid       = string.Equals(order.OrderPaymentStatus, "PAID",
                               StringComparison.OrdinalIgnoreCase),
            IsFulfilled  = string.Equals(order.OrderFulfillmentStatus, "FULFILLED",
                               StringComparison.OrdinalIgnoreCase),
            IsCancelled  = string.Equals(order.CancelStatus?.CancelState, "CANCELED",
                               StringComparison.OrdinalIgnoreCase),
            BuyerNote    = order.BuyerCheckoutNotes,
            CreatedAt    = DateTimeOffset.TryParse(order.CreationDate, out var dt)
                               ? dt : DateTimeOffset.MinValue,
            UpdatedAt    = DateTimeOffset.TryParse(order.LastModifiedDate, out var upd)
                               ? upd : (DateTimeOffset?)null,
            ShippingAddress = addr != null ? new SellerAddress
            {
                Name            = shipStep?.ShipTo?.FullName,
                Street1         = addr.AddressLine1,
                Street2         = addr.AddressLine2,
                City            = addr.City,
                StateOrProvince = addr.StateOrProvince,
                PostalCode      = addr.PostalCode,
                CountryCode     = addr.Country
            } : null,
            LineItems = (order.LineItems ?? new List<EbayLineItem>())
                .Select(li => new SellerOrderLine
                {
                    LineItemId = li.LineItemId ?? "",
                    ListingId  = li.LegacyItemId,
                    Sku        = li.Sku,
                    Title      = li.Title ?? "",
                    Quantity   = li.Quantity,
                    UnitPrice  = decimal.TryParse(li.LineItemCost?.Value,
                                     System.Globalization.NumberStyles.Any,
                                     System.Globalization.CultureInfo.InvariantCulture, out var u) ? u : 0m
                })
                .ToList()
        };
    }

    private static string? BuildEbayOrderFilter(OrderFilter? filter)
    {
        if (filter == null) return null;

        var parts = new List<string>();

        if (filter.IsFulfilled.HasValue)
            parts.Add(filter.IsFulfilled.Value
                ? "orderfulfillmentstatus:{FULFILLED}"
                : "orderfulfillmentstatus:{NOT_STARTED|IN_PROGRESS}");

        if (filter.IsPaid.HasValue)
            parts.Add(filter.IsPaid.Value
                ? "paymentstatus:{PAID}"
                : "paymentstatus:{PENDING|FAILED}");

        if (filter.CreatedAfter.HasValue)
            parts.Add($"creationdate:[{filter.CreatedAfter.Value:yyyy-MM-ddTHH:mm:ssZ}..]");

        if (filter.CreatedBefore.HasValue)
            parts.Add($"creationdate:[..{filter.CreatedBefore.Value:yyyy-MM-ddTHH:mm:ssZ}]");

        return parts.Count > 0 ? string.Join(",", parts) : null;
    }

    /// <summary>Reads a typed value from the extensions dictionary, returning a default when absent.</summary>
    private static T ExtOrDefault<T>(Dictionary<string, object?>? extensions, string key, T fallback)
    {
        if (extensions != null && extensions.TryGetValue(key, out var raw) && raw is T typed)
            return typed;
        return fallback;
    }
}
