using System.Text.Json.Serialization;

// ──────────────────────────────────────────────────────────────────────────────
// Inventory Item structures
// ──────────────────────────────────────────────────────────────────────────────

public class EbayInventoryItem
{
    [JsonPropertyName("availability")]
    public EbayAvailability? Availability { get; set; }

    [JsonPropertyName("condition")]
    public string Condition { get; set; } = "NEW"; // NEW, LIKE_NEW, VERY_GOOD, GOOD, ACCEPTABLE, FOR_PARTS_OR_NOT_WORKING

    [JsonPropertyName("conditionDescription")]
    public string? ConditionDescription { get; set; }

    [JsonPropertyName("product")]
    public EbayProduct? Product { get; set; }

    [JsonPropertyName("packageWeightAndSize")]
    public EbayPackageWeightAndSize? PackageWeightAndSize { get; set; }

    [JsonPropertyName("sku")]
    public string? Sku { get; set; }
}

public class EbayAvailability
{
    [JsonPropertyName("shipToLocationAvailability")]
    public EbayShipToLocationAvailability? ShipToLocationAvailability { get; set; }

    [JsonPropertyName("pickupAtLocationAvailability")]
    public List<EbayPickupAtLocationAvailability>? PickupAtLocationAvailability { get; set; }
}

public class EbayShipToLocationAvailability
{
    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("allocationByFormat")]
    public EbayAllocationByFormat? AllocationByFormat { get; set; }
}

public class EbayAllocationByFormat
{
    [JsonPropertyName("auction")]
    public int? Auction { get; set; }

    [JsonPropertyName("fixedPrice")]
    public int? FixedPrice { get; set; }
}

public class EbayPickupAtLocationAvailability
{
    [JsonPropertyName("merchantLocationKey")]
    public string MerchantLocationKey { get; set; } = "";

    [JsonPropertyName("availabilityType")]
    public string AvailabilityType { get; set; } = "IN_STOCK"; // IN_STOCK, OUT_OF_STOCK, SHIP_TO_STORE

    [JsonPropertyName("fulfillmentTime")]
    public EbayTimeDuration? FulfillmentTime { get; set; }

    [JsonPropertyName("quantity")]
    public int? Quantity { get; set; }
}

public class EbayTimeDuration
{
    [JsonPropertyName("unit")]
    public string Unit { get; set; } = "BUSINESS_DAY"; // YEAR, MONTH, DAY, HOUR, CALENDAR_DAY, BUSINESS_DAY, MINUTE, SECOND

    [JsonPropertyName("value")]
    public int Value { get; set; }
}

public class EbayProduct
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("aspects")]
    public Dictionary<string, List<string>>? Aspects { get; set; }

    [JsonPropertyName("brand")]
    public string? Brand { get; set; }

    [JsonPropertyName("ean")]
    public List<string>? Ean { get; set; }

    [JsonPropertyName("epid")]
    public string? Epid { get; set; }

    [JsonPropertyName("imageUrls")]
    public List<string>? ImageUrls { get; set; }

    [JsonPropertyName("isbn")]
    public List<string>? Isbn { get; set; }

    [JsonPropertyName("mpn")]
    public string? Mpn { get; set; }

    [JsonPropertyName("subtitle")]
    public string? Subtitle { get; set; }

    [JsonPropertyName("upc")]
    public List<string>? Upc { get; set; }

    [JsonPropertyName("videoIds")]
    public List<string>? VideoIds { get; set; }
}

public class EbayPackageWeightAndSize
{
    [JsonPropertyName("dimensions")]
    public EbayDimensions? Dimensions { get; set; }

    [JsonPropertyName("packageType")]
    public string? PackageType { get; set; } // LETTER, BULKY_GOODS, CARAVAN, CARS, EUROPALLET, ...

    [JsonPropertyName("weight")]
    public EbayWeight? Weight { get; set; }
}

public class EbayDimensions
{
    [JsonPropertyName("height")]
    public double Height { get; set; }

    [JsonPropertyName("length")]
    public double Length { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; } = "INCH"; // INCH, FEET, CENTIMETER, METER

    [JsonPropertyName("width")]
    public double Width { get; set; }
}

public class EbayWeight
{
    [JsonPropertyName("unit")]
    public string Unit { get; set; } = "POUND"; // POUND, KILOGRAM, OUNCE, GRAM

    [JsonPropertyName("value")]
    public double Value { get; set; }
}

// ──────────────────────────────────────────────────────────────────────────────
// Inventory Item response (read)
// ──────────────────────────────────────────────────────────────────────────────

public class EbayGetInventoryItemsResponse
{
    [JsonPropertyName("inventoryItems")]
    public List<EbayInventoryItemListing>? InventoryItems { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("href")]
    public string? Href { get; set; }

    [JsonPropertyName("next")]
    public string? Next { get; set; }

    [JsonPropertyName("prev")]
    public string? Prev { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }
}

public class EbayInventoryItemListing : EbayInventoryItem
{
    [JsonPropertyName("locale")]
    public string? Locale { get; set; }
}

// ──────────────────────────────────────────────────────────────────────────────
// Inventory Item Group
// ──────────────────────────────────────────────────────────────────────────────

public class EbayInventoryItemGroup
{
    [JsonPropertyName("aspects")]
    public Dictionary<string, List<string>>? Aspects { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("imageUrls")]
    public List<string>? ImageUrls { get; set; }

    [JsonPropertyName("inventoryItemGroupKey")]
    public string? InventoryItemGroupKey { get; set; }

    [JsonPropertyName("skus")]
    public List<string> Skus { get; set; } = [];

    [JsonPropertyName("subtitle")]
    public string? Subtitle { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("variantSKUs")]
    public List<string>? VariantSKUs { get; set; }

    [JsonPropertyName("videoIds")]
    public List<string>? VideoIds { get; set; }
}

// ──────────────────────────────────────────────────────────────────────────────
// Offer structures
// ──────────────────────────────────────────────────────────────────────────────

public class EbayOfferRequest
{
    [JsonPropertyName("availableQuantity")]
    public int AvailableQuantity { get; set; }

    [JsonPropertyName("categoryId")]
    public string? CategoryId { get; set; }

    [JsonPropertyName("charity")]
    public EbayCharity? Charity { get; set; }

    [JsonPropertyName("extendedProducerResponsibility")]
    public object? ExtendedProducerResponsibility { get; set; }

    [JsonPropertyName("format")]
    public string Format { get; set; } = "FIXED_PRICE"; // FIXED_PRICE, AUCTION

    [JsonPropertyName("hideBuyerDetails")]
    public bool? HideBuyerDetails { get; set; }

    [JsonPropertyName("includeCatalogProductDetails")]
    public bool? IncludeCatalogProductDetails { get; set; }

    [JsonPropertyName("listingDescription")]
    public string? ListingDescription { get; set; }

    [JsonPropertyName("listingDuration")]
    public string? ListingDuration { get; set; } // GTC, DAYS_1, DAYS_3, DAYS_5, DAYS_7, DAYS_10, DAYS_30

    [JsonPropertyName("listingPolicies")]
    public EbayListingPolicies? ListingPolicies { get; set; }

    [JsonPropertyName("listingStartDate")]
    public string? ListingStartDate { get; set; } // ISO 8601

    [JsonPropertyName("lotSize")]
    public int? LotSize { get; set; }

    [JsonPropertyName("marketplaceId")]
    public string MarketplaceId { get; set; } = "EBAY_US";

    [JsonPropertyName("merchantLocationKey")]
    public string? MerchantLocationKey { get; set; }

    [JsonPropertyName("pricingSummary")]
    public EbayPricingSummary? PricingSummary { get; set; }

    [JsonPropertyName("quantityLimitPerBuyer")]
    public int? QuantityLimitPerBuyer { get; set; }

    [JsonPropertyName("secondaryCategoryId")]
    public string? SecondaryCategoryId { get; set; }

    [JsonPropertyName("sku")]
    public string Sku { get; set; } = "";

    [JsonPropertyName("storeCategoryNames")]
    public List<string>? StoreCategoryNames { get; set; }

    [JsonPropertyName("tax")]
    public EbayTax? Tax { get; set; }
}

public class EbayCharity
{
    [JsonPropertyName("charityId")]
    public string CharityId { get; set; } = "";

    [JsonPropertyName("donationPercentage")]
    public string DonationPercentage { get; set; } = ""; // "10" to "100"
}

public class EbayListingPolicies
{
    [JsonPropertyName("bestOfferTerms")]
    public EbayBestOfferTerms? BestOfferTerms { get; set; }

    [JsonPropertyName("eBayPlusIfEligible")]
    public bool? EBayPlusIfEligible { get; set; }

    [JsonPropertyName("fulfillmentPolicyId")]
    public string? FulfillmentPolicyId { get; set; }

    [JsonPropertyName("paymentPolicyId")]
    public string? PaymentPolicyId { get; set; }

    [JsonPropertyName("productCompliancePolicyIds")]
    public List<string>? ProductCompliancePolicyIds { get; set; }

    [JsonPropertyName("returnPolicyId")]
    public string? ReturnPolicyId { get; set; }

    [JsonPropertyName("shippingCostOverrides")]
    public List<EbayShippingCostOverride>? ShippingCostOverrides { get; set; }
}

public class EbayBestOfferTerms
{
    [JsonPropertyName("autoAcceptPrice")]
    public EbayAmount? AutoAcceptPrice { get; set; }

    [JsonPropertyName("autoDeclinePrice")]
    public EbayAmount? AutoDeclinePrice { get; set; }

    [JsonPropertyName("bestOfferEnabled")]
    public bool BestOfferEnabled { get; set; }
}

public class EbayAmount
{
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "USD";

    [JsonPropertyName("value")]
    public string Value { get; set; } = "0";
}

public class EbayShippingCostOverride
{
    [JsonPropertyName("additionalShippingCost")]
    public EbayAmount? AdditionalShippingCost { get; set; }

    [JsonPropertyName("priority")]
    public int Priority { get; set; }

    [JsonPropertyName("shippingCost")]
    public EbayAmount? ShippingCost { get; set; }

    [JsonPropertyName("shippingServiceType")]
    public string ShippingServiceType { get; set; } = "DOMESTIC"; // DOMESTIC, INTERNATIONAL

    [JsonPropertyName("surcharge")]
    public EbayAmount? Surcharge { get; set; }
}

public class EbayPricingSummary
{
    [JsonPropertyName("auctionReservePrice")]
    public EbayAmount? AuctionReservePrice { get; set; }

    [JsonPropertyName("auctionStartPrice")]
    public EbayAmount? AuctionStartPrice { get; set; }

    [JsonPropertyName("minimumAdvertisedPrice")]
    public EbayAmount? MinimumAdvertisedPrice { get; set; }

    [JsonPropertyName("originallySoldForRetailPriceOn")]
    public string? OriginallySoldForRetailPriceOn { get; set; }

    [JsonPropertyName("originalRetailPrice")]
    public EbayAmount? OriginalRetailPrice { get; set; }

    [JsonPropertyName("price")]
    public EbayAmount? Price { get; set; }

    [JsonPropertyName("pricingVisibility")]
    public string? PricingVisibility { get; set; } // NONE, PRE_CHECKOUT, DURING_CHECKOUT
}

public class EbayTax
{
    [JsonPropertyName("applyTax")]
    public bool ApplyTax { get; set; }

    [JsonPropertyName("thirdPartyTaxCategory")]
    public string? ThirdPartyTaxCategory { get; set; }

    [JsonPropertyName("vatPercentage")]
    public double? VatPercentage { get; set; }
}

// ──────────────────────────────────────────────────────────────────────────────
// Offer response
// ──────────────────────────────────────────────────────────────────────────────

public class EbayOfferResponse : EbayOfferRequest
{
    [JsonPropertyName("offerId")]
    public string? OfferId { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; } // PUBLISHED, UNPUBLISHED

    [JsonPropertyName("listing")]
    public EbayOfferListing? Listing { get; set; }

    [JsonPropertyName("warnings")]
    public List<EbayError>? Warnings { get; set; }
}

public class EbayOfferListing
{
    [JsonPropertyName("listingId")]
    public string? ListingId { get; set; }

    [JsonPropertyName("listingStatus")]
    public string? ListingStatus { get; set; }

    [JsonPropertyName("soldQuantity")]
    public int? SoldQuantity { get; set; }
}

public class EbayGetOffersResponse
{
    [JsonPropertyName("offers")]
    public List<EbayOfferResponse>? Offers { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("href")]
    public string? Href { get; set; }

    [JsonPropertyName("next")]
    public string? Next { get; set; }

    [JsonPropertyName("prev")]
    public string? Prev { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("offset")]
    public int Offset { get; set; }
}

public class EbayPublishOfferResponse
{
    [JsonPropertyName("listingId")]
    public string? ListingId { get; set; }

    [JsonPropertyName("warnings")]
    public List<EbayError>? Warnings { get; set; }
}

public class EbayListingFeesResponse
{
    [JsonPropertyName("feeSummaries")]
    public List<EbayFeeSummary>? FeeSummaries { get; set; }

    [JsonPropertyName("warnings")]
    public List<EbayError>? Warnings { get; set; }
}

public class EbayFeeSummary
{
    [JsonPropertyName("fees")]
    public List<EbayFee>? Fees { get; set; }

    [JsonPropertyName("marketplaceId")]
    public string? MarketplaceId { get; set; }

    [JsonPropertyName("warnings")]
    public List<EbayError>? Warnings { get; set; }
}

public class EbayFee
{
    [JsonPropertyName("amount")]
    public EbayAmount? Amount { get; set; }

    [JsonPropertyName("feeType")]
    public string? FeeType { get; set; }

    [JsonPropertyName("promotionalDiscount")]
    public EbayAmount? PromotionalDiscount { get; set; }
}

// ──────────────────────────────────────────────────────────────────────────────
// Inventory Location
// ──────────────────────────────────────────────────────────────────────────────

public class EbayInventoryLocation
{
    [JsonPropertyName("location")]
    public EbayLocationDetails? Location { get; set; }

    [JsonPropertyName("locationAdditionalInformation")]
    public string? LocationAdditionalInformation { get; set; }

    [JsonPropertyName("locationInstructions")]
    public string? LocationInstructions { get; set; }

    [JsonPropertyName("locationTypes")]
    public List<string>? LocationTypes { get; set; } // WAREHOUSE, STORE

    [JsonPropertyName("locationWebUrl")]
    public string? LocationWebUrl { get; set; }

    [JsonPropertyName("merchantLocationKey")]
    public string? MerchantLocationKey { get; set; }

    [JsonPropertyName("merchantLocationStatus")]
    public string? MerchantLocationStatus { get; set; } // ENABLED, DISABLED

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("operatingHours")]
    public List<EbayOperatingHours>? OperatingHours { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("specialHours")]
    public List<EbaySpecialHours>? SpecialHours { get; set; }
}

public class EbayLocationDetails
{
    [JsonPropertyName("address")]
    public EbayAddress? Address { get; set; }

    [JsonPropertyName("geoCoordinates")]
    public EbayGeoCoordinates? GeoCoordinates { get; set; }

    [JsonPropertyName("locationId")]
    public string? LocationId { get; set; }
}

public class EbayAddress
{
    [JsonPropertyName("addressLine1")]
    public string? AddressLine1 { get; set; }

    [JsonPropertyName("addressLine2")]
    public string? AddressLine2 { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("country")]
    public string Country { get; set; } = "US";

    [JsonPropertyName("county")]
    public string? County { get; set; }

    [JsonPropertyName("postalCode")]
    public string? PostalCode { get; set; }

    [JsonPropertyName("stateOrProvince")]
    public string? StateOrProvince { get; set; }
}

public class EbayGeoCoordinates
{
    [JsonPropertyName("latitude")]
    public double Latitude { get; set; }

    [JsonPropertyName("longitude")]
    public double Longitude { get; set; }
}

public class EbayOperatingHours
{
    [JsonPropertyName("dayOfWeek")]
    public string DayOfWeek { get; set; } = ""; // MONDAY, TUESDAY, ...

    [JsonPropertyName("intervals")]
    public List<EbayInterval>? Intervals { get; set; }
}

public class EbayInterval
{
    [JsonPropertyName("close")]
    public string Close { get; set; } = ""; // HH:MM format

    [JsonPropertyName("open")]
    public string Open { get; set; } = "";
}

public class EbaySpecialHours
{
    [JsonPropertyName("date")]
    public string Date { get; set; } = ""; // ISO 8601

    [JsonPropertyName("intervals")]
    public List<EbayInterval>? Intervals { get; set; }
}

public class EbayGetInventoryLocationsResponse
{
    [JsonPropertyName("locations")]
    public List<EbayInventoryLocation>? Locations { get; set; }

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
// Bulk operation helpers
// ──────────────────────────────────────────────────────────────────────────────

public class EbayBulkInventoryItemRequest
{
    [JsonPropertyName("requests")]
    public List<EbayBulkInventoryItemEntry> Requests { get; set; } = [];
}

public class EbayBulkInventoryItemEntry : EbayInventoryItem
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("sku")]
    public new string? Sku { get; set; }

    [JsonPropertyName("locale")]
    public string? Locale { get; set; }
}

public class EbayBulkInventoryItemResponse
{
    [JsonPropertyName("responses")]
    public List<EbayBulkInventoryItemResult>? Responses { get; set; }
}

public class EbayBulkInventoryItemResult
{
    [JsonPropertyName("sku")]
    public string? Sku { get; set; }

    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    [JsonPropertyName("errors")]
    public List<EbayError>? Errors { get; set; }

    [JsonPropertyName("warnings")]
    public List<EbayError>? Warnings { get; set; }
}

public class EbayBulkPriceQuantityRequest
{
    [JsonPropertyName("requests")]
    public List<EbayBulkPriceQuantityEntry> Requests { get; set; } = [];
}

public class EbayBulkPriceQuantityEntry
{
    [JsonPropertyName("offers")]
    public List<EbayOfferPriceQuantity>? Offers { get; set; }

    [JsonPropertyName("shipToLocationAvailability")]
    public EbayShipToLocationAvailability? ShipToLocationAvailability { get; set; }

    [JsonPropertyName("sku")]
    public string Sku { get; set; } = "";
}

public class EbayOfferPriceQuantity
{
    [JsonPropertyName("availableQuantity")]
    public int? AvailableQuantity { get; set; }

    [JsonPropertyName("offerId")]
    public string OfferId { get; set; } = "";

    [JsonPropertyName("price")]
    public EbayAmount? Price { get; set; }
}

public class EbayBulkOfferRequest
{
    [JsonPropertyName("requests")]
    public List<EbayOfferRequest> Requests { get; set; } = [];
}

public class EbayBulkOfferResponse
{
    [JsonPropertyName("responses")]
    public List<EbayBulkOfferResult>? Responses { get; set; }
}

public class EbayBulkOfferResult
{
    [JsonPropertyName("offerId")]
    public string? OfferId { get; set; }

    [JsonPropertyName("sku")]
    public string? Sku { get; set; }

    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    [JsonPropertyName("errors")]
    public List<EbayError>? Errors { get; set; }

    [JsonPropertyName("warnings")]
    public List<EbayError>? Warnings { get; set; }
}

public class EbayBulkPublishOfferRequest
{
    [JsonPropertyName("requests")]
    public List<EbayPublishOfferEntry> Requests { get; set; } = [];
}

public class EbayPublishOfferEntry
{
    [JsonPropertyName("offerId")]
    public string OfferId { get; set; } = "";
}

public class EbayBulkPublishOfferResponse
{
    [JsonPropertyName("responses")]
    public List<EbayBulkPublishResult>? Responses { get; set; }
}

public class EbayBulkPublishResult
{
    [JsonPropertyName("offerId")]
    public string? OfferId { get; set; }

    [JsonPropertyName("listingId")]
    public string? ListingId { get; set; }

    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    [JsonPropertyName("errors")]
    public List<EbayError>? Errors { get; set; }

    [JsonPropertyName("warnings")]
    public List<EbayError>? Warnings { get; set; }
}
