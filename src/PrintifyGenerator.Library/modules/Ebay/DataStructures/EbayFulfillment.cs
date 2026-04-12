using System.Text.Json.Serialization;

// ──────────────────────────────────────────────────────────────────────────────
// Orders – Fulfillment API
// ──────────────────────────────────────────────────────────────────────────────

public class EbayGetOrdersResponse
{
    [JsonPropertyName("orders")]
    public List<EbayOrder>? Orders { get; set; }

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

    [JsonPropertyName("warnings")]
    public List<EbayError>? Warnings { get; set; }
}

public class EbayOrder
{
    [JsonPropertyName("orderId")]
    public string? OrderId { get; set; }

    [JsonPropertyName("orderFulfillmentStatus")]
    public string? OrderFulfillmentStatus { get; set; } // FULFILLED, IN_PROGRESS, NOT_STARTED

    [JsonPropertyName("orderPaymentStatus")]
    public string? OrderPaymentStatus { get; set; } // FULLY_REFUNDED, PAID, PARTIALLY_REFUNDED, PENDING, FAILED

    [JsonPropertyName("sellerId")]
    public string? SellerId { get; set; }

    [JsonPropertyName("buyer")]
    public EbayBuyer? Buyer { get; set; }

    [JsonPropertyName("buyerCheckoutNotes")]
    public string? BuyerCheckoutNotes { get; set; }

    [JsonPropertyName("cancelStatus")]
    public EbayCancelStatus? CancelStatus { get; set; }

    [JsonPropertyName("creationDate")]
    public string? CreationDate { get; set; }

    [JsonPropertyName("ebayCollectAndRemitTax")]
    public bool? EbayCollectAndRemitTax { get; set; }

    [JsonPropertyName("fulfillmentHrefs")]
    public List<string>? FulfillmentHrefs { get; set; }

    [JsonPropertyName("fulfillmentStartInstructions")]
    public List<EbayFulfillmentStartInstruction>? FulfillmentStartInstructions { get; set; }

    [JsonPropertyName("lastModifiedDate")]
    public string? LastModifiedDate { get; set; }

    [JsonPropertyName("legacyOrderId")]
    public string? LegacyOrderId { get; set; }

    [JsonPropertyName("lineItems")]
    public List<EbayLineItem>? LineItems { get; set; }

    [JsonPropertyName("paymentSummary")]
    public EbayPaymentSummary? PaymentSummary { get; set; }

    [JsonPropertyName("pricingSummary")]
    public EbayOrderPricingSummary? PricingSummary { get; set; }

    [JsonPropertyName("salesRecordReference")]
    public string? SalesRecordReference { get; set; }
}

public class EbayBuyer
{
    [JsonPropertyName("taxAddress")]
    public EbayTaxAddress? TaxAddress { get; set; }

    [JsonPropertyName("taxIdentifier")]
    public EbayTaxIdentifier? TaxIdentifier { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }
}

public class EbayTaxAddress
{
    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("postalCode")]
    public string? PostalCode { get; set; }

    [JsonPropertyName("stateOrProvince")]
    public string? StateOrProvince { get; set; }
}

public class EbayTaxIdentifier
{
    [JsonPropertyName("issuingCountry")]
    public string? IssuingCountry { get; set; }

    [JsonPropertyName("taxIdentifierType")]
    public string? TaxIdentifierType { get; set; }

    [JsonPropertyName("taxpayerId")]
    public string? TaxpayerId { get; set; }
}

public class EbayCancelStatus
{
    [JsonPropertyName("cancelledDate")]
    public string? CancelledDate { get; set; }

    [JsonPropertyName("cancelRequests")]
    public List<EbayCancelRequest>? CancelRequests { get; set; }

    [JsonPropertyName("cancelState")]
    public string? CancelState { get; set; } // CANCEL_REQUESTED, CANCELED, NONE_REQUESTED

    [JsonPropertyName("cancelReason")]
    public string? CancelReason { get; set; }
}

public class EbayCancelRequest
{
    [JsonPropertyName("cancelCompletedDate")]
    public string? CancelCompletedDate { get; set; }

    [JsonPropertyName("cancelInitiator")]
    public string? CancelInitiator { get; set; }

    [JsonPropertyName("cancelReason")]
    public string? CancelReason { get; set; }

    [JsonPropertyName("cancelRequestedDate")]
    public string? CancelRequestedDate { get; set; }

    [JsonPropertyName("cancelRequestId")]
    public string? CancelRequestId { get; set; }

    [JsonPropertyName("cancelRequestState")]
    public string? CancelRequestState { get; set; }
}

public class EbayFulfillmentStartInstruction
{
    [JsonPropertyName("ebaySupportedFulfillment")]
    public bool? EbaySupportedFulfillment { get; set; }

    [JsonPropertyName("finalDestinationAddress")]
    public EbayAddress? FinalDestinationAddress { get; set; }

    [JsonPropertyName("fulfillmentInstructionsType")]
    public string? FulfillmentInstructionsType { get; set; } // SHIP_TO, DIGITAL_DELIVERY, PICKUP_FROM_SELLER, PREPARE_FOR_PICKUP

    [JsonPropertyName("maxEstimatedDeliveryDate")]
    public string? MaxEstimatedDeliveryDate { get; set; }

    [JsonPropertyName("minEstimatedDeliveryDate")]
    public string? MinEstimatedDeliveryDate { get; set; }

    [JsonPropertyName("pickupStep")]
    public EbayPickupStep? PickupStep { get; set; }

    [JsonPropertyName("shippingStep")]
    public EbayShippingStep? ShippingStep { get; set; }
}

public class EbayPickupStep
{
    [JsonPropertyName("merchantLocationKey")]
    public string? MerchantLocationKey { get; set; }
}

public class EbayShippingStep
{
    [JsonPropertyName("shippingCarrierCode")]
    public string? ShippingCarrierCode { get; set; }

    [JsonPropertyName("shippingServiceCode")]
    public string? ShippingServiceCode { get; set; }

    [JsonPropertyName("shipTo")]
    public EbayShipTo? ShipTo { get; set; }

    [JsonPropertyName("shipToReferenceId")]
    public string? ShipToReferenceId { get; set; }
}

public class EbayShipTo
{
    [JsonPropertyName("companyName")]
    public string? CompanyName { get; set; }

    [JsonPropertyName("contactAddress")]
    public EbayAddress? ContactAddress { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }

    [JsonPropertyName("primaryPhone")]
    public EbayPhone? PrimaryPhone { get; set; }
}

public class EbayPhone
{
    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber { get; set; }
}

public class EbayLineItem
{
    [JsonPropertyName("appliedPromotions")]
    public List<EbayAppliedPromotion>? AppliedPromotions { get; set; }

    [JsonPropertyName("deliveryCost")]
    public EbayDeliveryCost? DeliveryCost { get; set; }

    [JsonPropertyName("discountedLineItemCost")]
    public EbayAmount? DiscountedLineItemCost { get; set; }

    [JsonPropertyName("ebayCollectAndRemitTaxes")]
    public List<EbayTaxes>? EbayCollectAndRemitTaxes { get; set; }

    [JsonPropertyName("giftDetails")]
    public EbayGiftDetails? GiftDetails { get; set; }

    [JsonPropertyName("itemLocation")]
    public EbayItemLocation? ItemLocation { get; set; }

    [JsonPropertyName("legacyItemId")]
    public string? LegacyItemId { get; set; }

    [JsonPropertyName("legacyVariationId")]
    public string? LegacyVariationId { get; set; }

    [JsonPropertyName("lineItemCost")]
    public EbayAmount? LineItemCost { get; set; }

    [JsonPropertyName("lineItemFulfillmentInstructions")]
    public EbayLineItemFulfillmentInstructions? LineItemFulfillmentInstructions { get; set; }

    [JsonPropertyName("lineItemFulfillmentStatus")]
    public string? LineItemFulfillmentStatus { get; set; }

    [JsonPropertyName("lineItemId")]
    public string? LineItemId { get; set; }

    [JsonPropertyName("listingMarketplaceId")]
    public string? ListingMarketplaceId { get; set; }

    [JsonPropertyName("properties")]
    public EbayLineItemProperties? Properties { get; set; }

    [JsonPropertyName("purchaseMarketplaceId")]
    public string? PurchaseMarketplaceId { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("refunds")]
    public List<EbayRefundItem>? Refunds { get; set; }

    [JsonPropertyName("sku")]
    public string? Sku { get; set; }

    [JsonPropertyName("soldFormat")]
    public string? SoldFormat { get; set; }

    [JsonPropertyName("taxes")]
    public List<EbayTaxes>? Taxes { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("total")]
    public EbayAmount? Total { get; set; }
}

public class EbayAppliedPromotion
{
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("discountAmount")]
    public EbayAmount? DiscountAmount { get; set; }

    [JsonPropertyName("promotionId")]
    public string? PromotionId { get; set; }
}

public class EbayDeliveryCost
{
    [JsonPropertyName("importCharges")]
    public EbayAmount? ImportCharges { get; set; }

    [JsonPropertyName("shippingCost")]
    public EbayAmount? ShippingCost { get; set; }

    [JsonPropertyName("shippingIntermediationFee")]
    public EbayAmount? ShippingIntermediationFee { get; set; }
}

public class EbayTaxes
{
    [JsonPropertyName("amount")]
    public EbayAmount? Amount { get; set; }

    [JsonPropertyName("taxType")]
    public string? TaxType { get; set; }

    [JsonPropertyName("collectionMethod")]
    public string? CollectionMethod { get; set; }
}

public class EbayGiftDetails
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("recipientEmail")]
    public string? RecipientEmail { get; set; }

    [JsonPropertyName("senderName")]
    public string? SenderName { get; set; }
}

public class EbayItemLocation
{
    [JsonPropertyName("countryCode")]
    public string? CountryCode { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("postalCode")]
    public string? PostalCode { get; set; }
}

public class EbayLineItemFulfillmentInstructions
{
    [JsonPropertyName("guaranteedDelivery")]
    public bool? GuaranteedDelivery { get; set; }

    [JsonPropertyName("maxEstimatedDeliveryDate")]
    public string? MaxEstimatedDeliveryDate { get; set; }

    [JsonPropertyName("minEstimatedDeliveryDate")]
    public string? MinEstimatedDeliveryDate { get; set; }

    [JsonPropertyName("shipByDate")]
    public string? ShipByDate { get; set; }
}

public class EbayLineItemProperties
{
    [JsonPropertyName("buyerProtection")]
    public bool? BuyerProtection { get; set; }
}

public class EbayRefundItem
{
    [JsonPropertyName("refundAmount")]
    public EbayAmount? RefundAmount { get; set; }

    [JsonPropertyName("refundId")]
    public string? RefundId { get; set; }

    [JsonPropertyName("refundReferenceId")]
    public string? RefundReferenceId { get; set; }

    [JsonPropertyName("refundStatus")]
    public string? RefundStatus { get; set; }

    [JsonPropertyName("refundType")]
    public string? RefundType { get; set; }
}

public class EbayPaymentSummary
{
    [JsonPropertyName("payments")]
    public List<EbayPayment>? Payments { get; set; }

    [JsonPropertyName("refunds")]
    public List<EbayOrderRefund>? Refunds { get; set; }

    [JsonPropertyName("totalDueSeller")]
    public EbayAmount? TotalDueSeller { get; set; }
}

public class EbayPayment
{
    [JsonPropertyName("amount")]
    public EbayAmount? Amount { get; set; }

    [JsonPropertyName("paymentDate")]
    public string? PaymentDate { get; set; }

    [JsonPropertyName("paymentHolds")]
    public List<EbayPaymentHold>? PaymentHolds { get; set; }

    [JsonPropertyName("paymentMethod")]
    public string? PaymentMethod { get; set; }

    [JsonPropertyName("paymentReferenceId")]
    public string? PaymentReferenceId { get; set; }

    [JsonPropertyName("paymentStatus")]
    public string? PaymentStatus { get; set; } // FAILED, PAID, PENDING
}

public class EbayPaymentHold
{
    [JsonPropertyName("expectedReleaseDate")]
    public string? ExpectedReleaseDate { get; set; }

    [JsonPropertyName("holdAmount")]
    public EbayAmount? HoldAmount { get; set; }

    [JsonPropertyName("holdReason")]
    public string? HoldReason { get; set; }

    [JsonPropertyName("holdState")]
    public string? HoldState { get; set; }

    [JsonPropertyName("releaseDate")]
    public string? ReleaseDate { get; set; }
}

public class EbayOrderRefund
{
    [JsonPropertyName("amount")]
    public EbayAmount? Amount { get; set; }

    [JsonPropertyName("refundDate")]
    public string? RefundDate { get; set; }

    [JsonPropertyName("refundId")]
    public string? RefundId { get; set; }

    [JsonPropertyName("refundReferenceId")]
    public string? RefundReferenceId { get; set; }

    [JsonPropertyName("refundStatus")]
    public string? RefundStatus { get; set; }
}

public class EbayOrderPricingSummary
{
    [JsonPropertyName("adjustment")]
    public EbayAmount? Adjustment { get; set; }

    [JsonPropertyName("deliveryCost")]
    public EbayAmount? DeliveryCost { get; set; }

    [JsonPropertyName("deliveryDiscount")]
    public EbayAmount? DeliveryDiscount { get; set; }

    [JsonPropertyName("fee")]
    public EbayAmount? Fee { get; set; }

    [JsonPropertyName("priceDiscountSubtotal")]
    public EbayAmount? PriceDiscountSubtotal { get; set; }

    [JsonPropertyName("priceSubtotal")]
    public EbayAmount? PriceSubtotal { get; set; }

    [JsonPropertyName("tax")]
    public EbayAmount? Tax { get; set; }

    [JsonPropertyName("total")]
    public EbayAmount? Total { get; set; }
}

// ──────────────────────────────────────────────────────────────────────────────
// Shipping Fulfillment
// ──────────────────────────────────────────────────────────────────────────────

public class EbayShippingFulfillmentRequest
{
    [JsonPropertyName("lineItems")]
    public List<EbayFulfillmentLineItem> LineItems { get; set; } = [];

    [JsonPropertyName("shippedDate")]
    public string ShippedDate { get; set; } = DateTime.UtcNow.ToString("o");

    [JsonPropertyName("shippingCarrierCode")]
    public string ShippingCarrierCode { get; set; } = ""; // USPS, UPS, FEDEX, DHL, ...

    [JsonPropertyName("trackingNumber")]
    public string TrackingNumber { get; set; } = "";
}

public class EbayFulfillmentLineItem
{
    [JsonPropertyName("lineItemId")]
    public string LineItemId { get; set; } = "";

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
}

public class EbayShippingFulfillment
{
    [JsonPropertyName("fulfillmentId")]
    public string? FulfillmentId { get; set; }

    [JsonPropertyName("lineItems")]
    public List<EbayFulfillmentLineItem>? LineItems { get; set; }

    [JsonPropertyName("shippedDate")]
    public string? ShippedDate { get; set; }

    [JsonPropertyName("shippingCarrierCode")]
    public string? ShippingCarrierCode { get; set; }

    [JsonPropertyName("trackingNumber")]
    public string? TrackingNumber { get; set; }
}

public class EbayGetShippingFulfillmentsResponse
{
    [JsonPropertyName("fulfillments")]
    public List<EbayShippingFulfillment>? Fulfillments { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("warnings")]
    public List<EbayError>? Warnings { get; set; }
}

// ──────────────────────────────────────────────────────────────────────────────
// Refund
// ──────────────────────────────────────────────────────────────────────────────

public class EbayIssueRefundRequest
{
    [JsonPropertyName("comments")]
    public EbayComment? Comments { get; set; }

    [JsonPropertyName("lineItems")]
    public List<EbayRefundLineItem>? LineItems { get; set; }

    [JsonPropertyName("reasonForRefund")]
    public string ReasonForRefund { get; set; } = ""; // BUYER_CANCEL, ITEM_NOT_AS_DESCRIBED, ...

    [JsonPropertyName("refundAmount")]
    public EbayAmount? RefundAmount { get; set; }
}

public class EbayComment
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("translatedFromContent")]
    public string? TranslatedFromContent { get; set; }

    [JsonPropertyName("translatedFromLanguage")]
    public string? TranslatedFromLanguage { get; set; }
}

public class EbayRefundLineItem
{
    [JsonPropertyName("lineItemId")]
    public string LineItemId { get; set; } = "";

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
}

// ──────────────────────────────────────────────────────────────────────────────
// Payment Disputes
// ──────────────────────────────────────────────────────────────────────────────

public class EbayPaymentDispute
{
    [JsonPropertyName("amount")]
    public EbayAmount? Amount { get; set; }

    [JsonPropertyName("availableChoices")]
    public List<string>? AvailableChoices { get; set; }

    [JsonPropertyName("buyer")]
    public EbayDisputeBuyer? Buyer { get; set; }

    [JsonPropertyName("closedDate")]
    public string? ClosedDate { get; set; }

    [JsonPropertyName("disputeOutcome")]
    public EbayDisputeOutcome? DisputeOutcome { get; set; }

    [JsonPropertyName("evidenceRequests")]
    public List<EbayEvidenceRequest>? EvidenceRequests { get; set; }

    [JsonPropertyName("lineItems")]
    public List<EbayDisputeLineItem>? LineItems { get; set; }

    [JsonPropertyName("monetaryTransactions")]
    public List<EbayMonetaryTransaction>? MonetaryTransactions { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("openDate")]
    public string? OpenDate { get; set; }

    [JsonPropertyName("orderId")]
    public string? OrderId { get; set; }

    [JsonPropertyName("paymentDisputeId")]
    public string? PaymentDisputeId { get; set; }

    [JsonPropertyName("paymentDisputeStatus")]
    public string? PaymentDisputeStatus { get; set; } // OPEN, WAITING_SELLER_RESPONSE, ...

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("resolution")]
    public string? Resolution { get; set; }

    [JsonPropertyName("respondByDate")]
    public string? RespondByDate { get; set; }

    [JsonPropertyName("returnAddress")]
    public EbayReturnAddress? ReturnAddress { get; set; }

    [JsonPropertyName("revision")]
    public int? Revision { get; set; }

    [JsonPropertyName("sellerResponse")]
    public string? SellerResponse { get; set; }
}

public class EbayDisputeBuyer
{
    [JsonPropertyName("buyerLoginId")]
    public string? BuyerLoginId { get; set; }
}

public class EbayDisputeOutcome
{
    [JsonPropertyName("closedDate")]
    public string? ClosedDate { get; set; }

    [JsonPropertyName("fees")]
    public List<EbayAmount>? Fees { get; set; }

    [JsonPropertyName("protectedAmount")]
    public EbayAmount? ProtectedAmount { get; set; }

    [JsonPropertyName("protectionStatus")]
    public string? ProtectionStatus { get; set; }

    [JsonPropertyName("resolvedDate")]
    public string? ResolvedDate { get; set; }
}

public class EbayEvidenceRequest
{
    [JsonPropertyName("evidenceId")]
    public string? EvidenceId { get; set; }

    [JsonPropertyName("evidenceRequiredByDate")]
    public string? EvidenceRequiredByDate { get; set; }

    [JsonPropertyName("evidenceType")]
    public string? EvidenceType { get; set; }

    [JsonPropertyName("lineItems")]
    public List<EbayDisputeLineItem>? LineItems { get; set; }

    [JsonPropertyName("requestDate")]
    public string? RequestDate { get; set; }
}

public class EbayDisputeLineItem
{
    [JsonPropertyName("itemId")]
    public string? ItemId { get; set; }

    [JsonPropertyName("lineItemId")]
    public string? LineItemId { get; set; }

    [JsonPropertyName("quantity")]
    public int? Quantity { get; set; }
}

public class EbayMonetaryTransaction
{
    [JsonPropertyName("amount")]
    public EbayAmount? Amount { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

public class EbayReturnAddress
{
    [JsonPropertyName("addressLine1")]
    public string? AddressLine1 { get; set; }

    [JsonPropertyName("addressLine2")]
    public string? AddressLine2 { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }

    [JsonPropertyName("postalCode")]
    public string? PostalCode { get; set; }

    [JsonPropertyName("stateOrProvince")]
    public string? StateOrProvince { get; set; }
}

public class EbayGetPaymentDisputeSummariesResponse
{
    [JsonPropertyName("paymentDisputeSummaries")]
    public List<EbayPaymentDisputeSummary>? PaymentDisputeSummaries { get; set; }

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

public class EbayPaymentDisputeSummary
{
    [JsonPropertyName("amount")]
    public EbayAmount? Amount { get; set; }

    [JsonPropertyName("buyerLoginId")]
    public string? BuyerLoginId { get; set; }

    [JsonPropertyName("buyerUsername")]
    public string? BuyerUsername { get; set; }

    [JsonPropertyName("closedDate")]
    public string? ClosedDate { get; set; }

    [JsonPropertyName("openDate")]
    public string? OpenDate { get; set; }

    [JsonPropertyName("orderId")]
    public string? OrderId { get; set; }

    [JsonPropertyName("paymentDisputeId")]
    public string? PaymentDisputeId { get; set; }

    [JsonPropertyName("paymentDisputeStatus")]
    public string? PaymentDisputeStatus { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    [JsonPropertyName("respondByDate")]
    public string? RespondByDate { get; set; }
}

public class EbayContestDisputeRequest
{
    [JsonPropertyName("availableChoices")]
    public List<string>? AvailableChoices { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("returnAddress")]
    public EbayReturnAddress? ReturnAddress { get; set; }

    [JsonPropertyName("revision")]
    public int Revision { get; set; }
}

public class EbayAcceptDisputeRequest
{
    [JsonPropertyName("returnAddress")]
    public EbayReturnAddress? ReturnAddress { get; set; }

    [JsonPropertyName("resolution")]
    public string? Resolution { get; set; }

    [JsonPropertyName("revision")]
    public int Revision { get; set; }
}

public class EbayAddEvidenceRequest
{
    [JsonPropertyName("evidenceType")]
    public string EvidenceType { get; set; } = "";

    [JsonPropertyName("files")]
    public List<EbayFileEvidence> Files { get; set; } = [];

    [JsonPropertyName("lineItems")]
    public List<EbayDisputeLineItem>? LineItems { get; set; }
}

public class EbayFileEvidence
{
    [JsonPropertyName("fileId")]
    public string FileId { get; set; } = "";
}
