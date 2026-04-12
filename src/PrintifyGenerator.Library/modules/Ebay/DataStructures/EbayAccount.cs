using System.Text.Json.Serialization;

// ──────────────────────────────────────────────────────────────────────────────
// Account API – Business Policies
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>Fulfillment (shipping) policy.</summary>
public class EbayFulfillmentPolicy
{
    [JsonPropertyName("categoryTypes")]
    public List<EbayCategoryType>? CategoryTypes { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("freightShipping")]
    public bool? FreightShipping { get; set; }

    [JsonPropertyName("fulfillmentPolicyId")]
    public string? FulfillmentPolicyId { get; set; }

    [JsonPropertyName("globalShipping")]
    public bool? GlobalShipping { get; set; }

    [JsonPropertyName("handlingTime")]
    public EbayTimeDuration? HandlingTime { get; set; }

    [JsonPropertyName("localPickup")]
    public bool? LocalPickup { get; set; }

    [JsonPropertyName("marketplaceId")]
    public string MarketplaceId { get; set; } = "EBAY_US";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("pickupDropOff")]
    public bool? PickupDropOff { get; set; }

    [JsonPropertyName("shippingOptions")]
    public List<EbayShippingOption>? ShippingOptions { get; set; }

    [JsonPropertyName("shipToLocations")]
    public EbayRegionSet? ShipToLocations { get; set; }
}

public class EbayCategoryType
{
    [JsonPropertyName("default")]
    public bool? Default { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "ALL_EXCLUDING_MOTORS_VEHICLES"; // ALL_EXCLUDING_MOTORS_VEHICLES, MOTORS_VEHICLES
}

public class EbayShippingOption
{
    [JsonPropertyName("costType")]
    public string CostType { get; set; } = "FLAT_RATE"; // FLAT_RATE, CALCULATED, NOT_SPECIFIED

    [JsonPropertyName("insuranceFee")]
    public EbayAmount? InsuranceFee { get; set; }

    [JsonPropertyName("insuranceOffered")]
    public bool? InsuranceOffered { get; set; }

    [JsonPropertyName("optionType")]
    public string OptionType { get; set; } = "DOMESTIC"; // DOMESTIC, INTERNATIONAL

    [JsonPropertyName("packageHandlingCost")]
    public EbayAmount? PackageHandlingCost { get; set; }

    [JsonPropertyName("rateTableId")]
    public string? RateTableId { get; set; }

    [JsonPropertyName("shippingDiscountProfileId")]
    public string? ShippingDiscountProfileId { get; set; }

    [JsonPropertyName("shippingPromotionOffered")]
    public bool? ShippingPromotionOffered { get; set; }

    [JsonPropertyName("shippingServices")]
    public List<EbayShippingService>? ShippingServices { get; set; }
}

public class EbayShippingService
{
    [JsonPropertyName("additionalShippingCost")]
    public EbayAmount? AdditionalShippingCost { get; set; }

    [JsonPropertyName("buyerResponsibleForPickup")]
    public bool? BuyerResponsibleForPickup { get; set; }

    [JsonPropertyName("buyerResponsibleForShipping")]
    public bool? BuyerResponsibleForShipping { get; set; }

    [JsonPropertyName("cashOnDeliveryFee")]
    public EbayAmount? CashOnDeliveryFee { get; set; }

    [JsonPropertyName("freeShipping")]
    public bool? FreeShipping { get; set; }

    [JsonPropertyName("shippingCarrierCode")]
    public string? ShippingCarrierCode { get; set; }

    [JsonPropertyName("shippingCost")]
    public EbayAmount? ShippingCost { get; set; }

    [JsonPropertyName("shippingServiceCode")]
    public string? ShippingServiceCode { get; set; }

    [JsonPropertyName("shipToLocations")]
    public EbayRegionSet? ShipToLocations { get; set; }

    [JsonPropertyName("sortOrder")]
    public int? SortOrder { get; set; }

    [JsonPropertyName("surcharge")]
    public EbayAmount? Surcharge { get; set; }
}

public class EbayRegionSet
{
    [JsonPropertyName("regionExcluded")]
    public List<EbayRegion>? RegionExcluded { get; set; }

    [JsonPropertyName("regionIncluded")]
    public List<EbayRegion>? RegionIncluded { get; set; }
}

public class EbayRegion
{
    [JsonPropertyName("regionName")]
    public string RegionName { get; set; } = "";

    [JsonPropertyName("regionType")]
    public string? RegionType { get; set; } // COUNTRY, WORLD_REGION, WORLDWIDE, STATE_OR_PROVINCE, COUNTRY_REGION
}

/// <summary>Response wrapping a fulfillment policy (includes ID after creation).</summary>
public class EbayFulfillmentPolicyResponse : EbayFulfillmentPolicy
{
    [JsonPropertyName("warnings")]
    public List<EbayError>? Warnings { get; set; }
}

public class EbayGetFulfillmentPoliciesResponse
{
    [JsonPropertyName("fulfillmentPolicies")]
    public List<EbayFulfillmentPolicy>? FulfillmentPolicies { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("href")]
    public string? Href { get; set; }
}

// ──────────────────────────────────────────────────────────────────────────────
// Return Policy
// ──────────────────────────────────────────────────────────────────────────────

public class EbayReturnPolicy
{
    [JsonPropertyName("categoryTypes")]
    public List<EbayCategoryType>? CategoryTypes { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("extendedHolidayReturnsOffered")]
    public bool? ExtendedHolidayReturnsOffered { get; set; }

    [JsonPropertyName("internationalOverride")]
    public EbayInternationalReturnOverride? InternationalOverride { get; set; }

    [JsonPropertyName("marketplaceId")]
    public string MarketplaceId { get; set; } = "EBAY_US";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("refundMethod")]
    public string? RefundMethod { get; set; } // MONEY_BACK, MERCHANDISE_CREDIT, MONEY_BACK_OR_REPLACEMENT, MONEY_BACK_OR_EXCHANGE

    [JsonPropertyName("replacementApproved")]
    public bool? ReplacementApproved { get; set; }

    [JsonPropertyName("restockingFeePercentage")]
    public string? RestockingFeePercentage { get; set; }

    [JsonPropertyName("returnInstructions")]
    public string? ReturnInstructions { get; set; }

    [JsonPropertyName("returnMethod")]
    public string? ReturnMethod { get; set; } // EXCHANGE, REPLACEMENT

    [JsonPropertyName("returnPeriod")]
    public EbayTimeDuration? ReturnPeriod { get; set; }

    [JsonPropertyName("returnPolicyId")]
    public string? ReturnPolicyId { get; set; }

    [JsonPropertyName("returnsAccepted")]
    public bool ReturnsAccepted { get; set; }

    [JsonPropertyName("returnShippingCostPayer")]
    public string? ReturnShippingCostPayer { get; set; } // BUYER, SELLER
}

public class EbayInternationalReturnOverride
{
    [JsonPropertyName("refundMethod")]
    public string? RefundMethod { get; set; }

    [JsonPropertyName("returnMethod")]
    public string? ReturnMethod { get; set; }

    [JsonPropertyName("returnPeriod")]
    public EbayTimeDuration? ReturnPeriod { get; set; }

    [JsonPropertyName("returnsAccepted")]
    public bool? ReturnsAccepted { get; set; }

    [JsonPropertyName("returnShippingCostPayer")]
    public string? ReturnShippingCostPayer { get; set; }
}

public class EbayReturnPolicyResponse : EbayReturnPolicy
{
    [JsonPropertyName("warnings")]
    public List<EbayError>? Warnings { get; set; }
}

public class EbayGetReturnPoliciesResponse
{
    [JsonPropertyName("returnPolicies")]
    public List<EbayReturnPolicy>? ReturnPolicies { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("href")]
    public string? Href { get; set; }
}

// ──────────────────────────────────────────────────────────────────────────────
// Payment Policy
// ──────────────────────────────────────────────────────────────────────────────

public class EbayPaymentPolicy
{
    [JsonPropertyName("categoryTypes")]
    public List<EbayCategoryType>? CategoryTypes { get; set; }

    [JsonPropertyName("deposit")]
    public EbayDeposit? Deposit { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("fullPaymentDueIn")]
    public EbayTimeDuration? FullPaymentDueIn { get; set; }

    [JsonPropertyName("immediatePay")]
    public bool? ImmediatePay { get; set; }

    [JsonPropertyName("marketplaceId")]
    public string MarketplaceId { get; set; } = "EBAY_US";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("paymentInstructions")]
    public string? PaymentInstructions { get; set; }

    [JsonPropertyName("paymentMethods")]
    public List<EbayPaymentMethod>? PaymentMethods { get; set; }

    [JsonPropertyName("paymentPolicyId")]
    public string? PaymentPolicyId { get; set; }
}

public class EbayDeposit
{
    [JsonPropertyName("amount")]
    public EbayAmount? Amount { get; set; }

    [JsonPropertyName("dueIn")]
    public EbayTimeDuration? DueIn { get; set; }

    [JsonPropertyName("paymentMethods")]
    public List<EbayPaymentMethod>? PaymentMethods { get; set; }
}

public class EbayPaymentMethod
{
    [JsonPropertyName("brands")]
    public List<string>? Brands { get; set; }

    [JsonPropertyName("paymentMethodType")]
    public string? PaymentMethodType { get; set; } // CASH_ON_DELIVERY, ESCROW, INTEGRATED_MERCHANT_CREDIT_CARD, PAYPAL, etc.

    [JsonPropertyName("recipientAccountReference")]
    public EbayRecipientAccountReference? RecipientAccountReference { get; set; }
}

public class EbayRecipientAccountReference
{
    [JsonPropertyName("referenceId")]
    public string? ReferenceId { get; set; }

    [JsonPropertyName("referenceType")]
    public string? ReferenceType { get; set; }
}

public class EbayPaymentPolicyResponse : EbayPaymentPolicy
{
    [JsonPropertyName("warnings")]
    public List<EbayError>? Warnings { get; set; }
}

public class EbayGetPaymentPoliciesResponse
{
    [JsonPropertyName("paymentPolicies")]
    public List<EbayPaymentPolicy>? PaymentPolicies { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("href")]
    public string? Href { get; set; }
}

// ──────────────────────────────────────────────────────────────────────────────
// Seller privileges
// ──────────────────────────────────────────────────────────────────────────────

public class EbaySellerEligibilityResponse
{
    [JsonPropertyName("eligibleForSelling")]
    public bool? EligibleForSelling { get; set; }

    [JsonPropertyName("restrictionText")]
    public string? RestrictionText { get; set; }

    [JsonPropertyName("sellingLimit")]
    public EbaySellingLimit? SellingLimit { get; set; }

    [JsonPropertyName("sellingStatus")]
    public string? SellingStatus { get; set; }
}

public class EbaySellingLimit
{
    [JsonPropertyName("amount")]
    public EbayAmount? Amount { get; set; }

    [JsonPropertyName("quantity")]
    public int? Quantity { get; set; }
}

// ──────────────────────────────────────────────────────────────────────────────
// Finances API – Payouts & Transactions
// ──────────────────────────────────────────────────────────────────────────────

public class EbayPayoutsResponse
{
    [JsonPropertyName("payouts")]
    public List<EbayPayout>? Payouts { get; set; }

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

public class EbayPayout
{
    [JsonPropertyName("amount")]
    public EbayAmount? Amount { get; set; }

    [JsonPropertyName("bankReference")]
    public string? BankReference { get; set; }

    [JsonPropertyName("lastAttemptedPayoutDate")]
    public string? LastAttemptedPayoutDate { get; set; }

    [JsonPropertyName("payoutDate")]
    public string? PayoutDate { get; set; }

    [JsonPropertyName("payoutId")]
    public string? PayoutId { get; set; }

    [JsonPropertyName("payoutInstrument")]
    public EbayPayoutInstrument? PayoutInstrument { get; set; }

    [JsonPropertyName("payoutMemo")]
    public string? PayoutMemo { get; set; }

    [JsonPropertyName("payoutStatus")]
    public string? PayoutStatus { get; set; } // INITIATED, SUCCEEDED, RETRYABLE_FAILED, TERMINAL_FAILED, REVERSED

    [JsonPropertyName("payoutStatusDescription")]
    public string? PayoutStatusDescription { get; set; }

    [JsonPropertyName("transactionCount")]
    public int? TransactionCount { get; set; }
}

public class EbayPayoutInstrument
{
    [JsonPropertyName("accountLastFourDigits")]
    public string? AccountLastFourDigits { get; set; }

    [JsonPropertyName("instrumentType")]
    public string? InstrumentType { get; set; }

    [JsonPropertyName("nickname")]
    public string? Nickname { get; set; }
}

public class EbayTransactionsResponse
{
    [JsonPropertyName("transactions")]
    public List<EbayTransaction>? Transactions { get; set; }

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

public class EbayTransaction
{
    [JsonPropertyName("amount")]
    public EbayAmount? Amount { get; set; }

    [JsonPropertyName("bookingEntry")]
    public string? BookingEntry { get; set; } // CREDIT, DEBIT

    [JsonPropertyName("buyer")]
    public EbayTransactionBuyer? Buyer { get; set; }

    [JsonPropertyName("ebayCollectedTaxAmount")]
    public EbayAmount? EbayCollectedTaxAmount { get; set; }

    [JsonPropertyName("feeType")]
    public string? FeeType { get; set; }

    [JsonPropertyName("orderId")]
    public string? OrderId { get; set; }

    [JsonPropertyName("orderLineItems")]
    public List<EbayOrderLineItemReference>? OrderLineItems { get; set; }

    [JsonPropertyName("paymentsEntity")]
    public string? PaymentsEntity { get; set; }

    [JsonPropertyName("payoutId")]
    public string? PayoutId { get; set; }

    [JsonPropertyName("salesRecordReference")]
    public string? SalesRecordReference { get; set; }

    [JsonPropertyName("totalFeeAmount")]
    public EbayAmount? TotalFeeAmount { get; set; }

    [JsonPropertyName("totalFeeBasisAmount")]
    public EbayAmount? TotalFeeBasisAmount { get; set; }

    [JsonPropertyName("transactionDate")]
    public string? TransactionDate { get; set; }

    [JsonPropertyName("transactionId")]
    public string? TransactionId { get; set; }

    [JsonPropertyName("transactionMemo")]
    public string? TransactionMemo { get; set; }

    [JsonPropertyName("transactionStatus")]
    public string? TransactionStatus { get; set; } // PAYOUT, FUNDS_PROCESSING, FUNDS_AVAILABLE_FOR_PAYOUT, FUNDS_ON_HOLD

    [JsonPropertyName("transactionType")]
    public string? TransactionType { get; set; } // SALE, REFUND, CREDIT, DISPUTE, NON_SALE_CHARGE, SHIPPING_LABEL, ...
}

public class EbayTransactionBuyer
{
    [JsonPropertyName("username")]
    public string? Username { get; set; }
}

public class EbayOrderLineItemReference
{
    [JsonPropertyName("feeBasisAmount")]
    public EbayAmount? FeeBasisAmount { get; set; }

    [JsonPropertyName("lineItemId")]
    public string? LineItemId { get; set; }

    [JsonPropertyName("marketplaceFees")]
    public List<EbayFee>? MarketplaceFees { get; set; }
}
