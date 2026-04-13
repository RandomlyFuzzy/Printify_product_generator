using System.Text.Json;
using System.Text.Json.Serialization;

// ── Shops ──────────────────────────────────────────────────────────

public record Shop
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("sales_channel")] public string SalesChannel { get; set; } = "";
}

// ── Catalog / Blueprints ───────────────────────────────────────────

public record Blueprint
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("brand")] public string Brand { get; set; } = "";
    [JsonPropertyName("model")] public string Model { get; set; } = "";
    [JsonPropertyName("images")] public List<string> Images { get; set; } = new();

    public override string ToString()
    {
        //print as jsonpretty string
        return JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
    }
}

public record PrintProvider
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("location")] public PrintProviderLocation? Location { get; set; }
}

public record PrintProviderLocation
{
    [JsonPropertyName("address1")] public string Address1 { get; set; } = "";
    [JsonPropertyName("address2")] public string Address2 { get; set; } = "";
    [JsonPropertyName("city")] public string City { get; set; } = "";
    [JsonPropertyName("country")] public string Country { get; set; } = "";
    [JsonPropertyName("region")] public string Region { get; set; } = "";
    [JsonPropertyName("zip")] public string Zip { get; set; } = "";
}

public record BlueprintPrintProvider
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("title")] public string Title { get; set; } = "";
}

public record VariantResponse
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("variants")] public List<Variant> Variants { get; set; } = new();
}

public record Variant
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("cost")] public int? Cost { get; set; }
    [JsonPropertyName("price")] public int? Price { get; set; }
    [JsonPropertyName("prices")] public List<VariantPrice> Prices { get; set; } = new();
    [JsonPropertyName("options")] public Dictionary<string, object>? Options { get; set; }
    [JsonPropertyName("placeholders")] public List<VariantPlaceholder>? Placeholders { get; set; }
}

public record VariantPrice
{
    [JsonPropertyName("currency")] public string Currency { get; set; } = "";
    [JsonPropertyName("price")] public int Price { get; set; }
}

public record VariantPlaceholder
{
    [JsonPropertyName("position")] public string Position { get; set; } = "";
    [JsonPropertyName("decoration_method")] public string DecorationMethod { get; set; } = "";
    [JsonPropertyName("height")] public int Height { get; set; }
    [JsonPropertyName("width")] public int Width { get; set; }
}

public record ShippingInfo
{
    [JsonPropertyName("handling_time")] public HandlingTime? HandlingTime { get; set; }
    [JsonPropertyName("profiles")] public List<ShippingProfile> Profiles { get; set; } = new();
}

public record HandlingTime
{
    [JsonPropertyName("value")] public int Value { get; set; }
    [JsonPropertyName("unit")] public string Unit { get; set; } = "";
}

public record ShippingProfile
{
    [JsonPropertyName("variant_ids")] public List<int> VariantIds { get; set; } = new();
    [JsonPropertyName("first_item")] public ShippingCost? FirstItem { get; set; }
    [JsonPropertyName("additional_items")] public ShippingCost? AdditionalItems { get; set; }
    [JsonPropertyName("countries")] public List<string> Countries { get; set; } = new();
}

public record ShippingCost
{
    [JsonPropertyName("currency")] public string Currency { get; set; } = "";
    [JsonPropertyName("cost")] public int Cost { get; set; }
}

// ── Products ───────────────────────────────────────────────────────

public record Product
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = new();
    [JsonPropertyName("options")] public List<ProductOption>? Options { get; set; }
    [JsonPropertyName("variants")] public List<ProductVariant> Variants { get; set; } = new();
    [JsonPropertyName("images")] public List<ProductMockupImage>? Images { get; set; }
    [JsonPropertyName("created_at")] public string? CreatedAt { get; set; }
    [JsonPropertyName("updated_at")] public string? UpdatedAt { get; set; }
    [JsonPropertyName("visible")] public bool Visible { get; set; }
    [JsonPropertyName("blueprint_id")] public int BlueprintId { get; set; }
    [JsonPropertyName("print_provider_id")] public int PrintProviderId { get; set; }
    [JsonPropertyName("user_id")] public int UserId { get; set; }
    [JsonPropertyName("shop_id")] public int ShopId { get; set; }
    [JsonPropertyName("print_areas")] public List<PrintArea>? PrintAreas { get; set; }
    [JsonPropertyName("print_details")] public PrintDetails? PrintDetails { get; set; }
    [JsonPropertyName("is_locked")] public bool IsLocked { get; set; }
    [JsonPropertyName("external")] public ProductExternal? External { get; set; }
    [JsonPropertyName("is_printify_express_eligible")] public bool IsPrintifyExpressEligible { get; set; }
    [JsonPropertyName("is_economy_shipping_eligible")] public bool IsEconomyShippingEligible { get; set; }
    [JsonPropertyName("is_printify_express_enabled")] public bool IsPrintifyExpressEnabled { get; set; }
    [JsonPropertyName("is_economy_shipping_enabled")] public bool IsEconomyShippingEnabled { get; set; }
    [JsonPropertyName("safety_information")] public string? SafetyInformation { get; set; }
    [JsonPropertyName("sales_channel_properties")] public object? SalesChannelProperties { get; set; }
}

public record ProductOption
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("values")] public List<ProductOptionValue>? Values { get; set; }
}

public record ProductOptionValue
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("colors")] public List<string>? Colors { get; set; }
}

public record ProductVariant
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("price")] public int Price { get; set; }
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("sku")] public string? Sku { get; set; }
    [JsonPropertyName("grams")] public int Grams { get; set; }
    [JsonPropertyName("is_enabled")] public bool IsEnabled { get; set; }
    [JsonPropertyName("is_default")] public bool IsDefault { get; set; }
    [JsonPropertyName("is_available")] public bool IsAvailable { get; set; }
    [JsonPropertyName("is_printify_express_eligible")] public bool IsPrintifyExpressEligible { get; set; }
    [JsonPropertyName("cost")] public int Cost { get; set; }
    [JsonPropertyName("options")] public List<int>? Options { get; set; }
}

public record PrintArea
{
    [JsonPropertyName("variant_ids")] public List<int> VariantIds { get; set; } = new();
    [JsonPropertyName("placeholders")] public List<PrintAreaPlaceholder> Placeholders { get; set; } = new();
}

public record PrintAreaPlaceholder
{
    [JsonPropertyName("position")] public string Position { get; set; } = "";
    [JsonPropertyName("decoration_method")] public string? DecorationMethod { get; set; }
    [JsonPropertyName("images")] public List<PrintAreaImage> Images { get; set; } = new();
}

public record PrintAreaImage
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("src")] public string? Src { get; set; }
    [JsonPropertyName("name")] public string? Name { get; set; }
    [JsonPropertyName("type")] public string? Type { get; set; }
    [JsonPropertyName("height")] public double Height { get; set; }
    [JsonPropertyName("width")] public double Width { get; set; }
    [JsonPropertyName("x")] public double X { get; set; }
    [JsonPropertyName("y")] public double Y { get; set; }
    [JsonPropertyName("scale")] public double Scale { get; set; }
    [JsonPropertyName("angle")] public double Angle { get; set; }
    [JsonPropertyName("pattern")] public ImagePattern? Pattern { get; set; }
}

public record ImagePattern
{
    [JsonPropertyName("spacing_x")] public double SpacingX { get; set; }
    [JsonPropertyName("spacing_y")] public double SpacingY { get; set; }
    [JsonPropertyName("angle")] public double Angle { get; set; }
    [JsonPropertyName("offset")] public double Offset { get; set; }
    [JsonPropertyName("scale")] public double Scale { get; set; }
}

[JsonConverter(typeof(PrintDetailsConverter))]
public record PrintDetails
{
    [JsonPropertyName("print_on_side")] public string? PrintOnSide { get; set; }
    [JsonPropertyName("separator_type")] public string? SeparatorType { get; set; }
    [JsonPropertyName("separator_color")] public string? SeparatorColor { get; set; }
}

/// <summary>
/// Handles Printify API returning [] (empty array) instead of an object for print_details.
/// </summary>
public class PrintDetailsConverter : JsonConverter<PrintDetails?>
{
    public override PrintDetails? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            // Skip the entire array
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray) { }
            return null;
        }
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        // Normal object deserialization
        var element = JsonElement.ParseValue(ref reader);
        return JsonSerializer.Deserialize<PrintDetailsInner>(element.GetRawText(), options)?.ToRecord();
    }

    public override void Write(Utf8JsonWriter writer, PrintDetails? value, JsonSerializerOptions options)
    {
        if (value is null)
            writer.WriteNullValue();
        else
            JsonSerializer.Serialize(writer, new PrintDetailsInner(value), options);
    }

    // Inner record without the converter to avoid infinite recursion
    private record PrintDetailsInner
    {
        [JsonPropertyName("print_on_side")] public string? PrintOnSide { get; set; }
        [JsonPropertyName("separator_type")] public string? SeparatorType { get; set; }
        [JsonPropertyName("separator_color")] public string? SeparatorColor { get; set; }

        public PrintDetailsInner() { }
        public PrintDetailsInner(PrintDetails pd)
        {
            PrintOnSide = pd.PrintOnSide;
            SeparatorType = pd.SeparatorType;
            SeparatorColor = pd.SeparatorColor;
        }

        public PrintDetails ToRecord() => new()
        {
            PrintOnSide = PrintOnSide,
            SeparatorType = SeparatorType,
            SeparatorColor = SeparatorColor
        };
    }
}

public record ProductMockupImage
{
    [JsonPropertyName("src")] public string Src { get; set; } = "";
    [JsonPropertyName("variant_ids")] public List<int> VariantIds { get; set; } = new();
    [JsonPropertyName("position")] public string Position { get; set; } = "";
    [JsonPropertyName("is_default")] public bool IsDefault { get; set; }
}

public record ProductExternal
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("handle")] public string Handle { get; set; } = "";
    [JsonPropertyName("shipping_template_id")] public string? ShippingTemplateId { get; set; }
}

// ── Product Create / Update DTOs ───────────────────────────────────

public record CreateProductRequest
{
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("blueprint_id")] public int BlueprintId { get; set; }
    [JsonPropertyName("print_provider_id")] public int PrintProviderId { get; set; }
    [JsonPropertyName("variants")] public List<CreateProductVariant> Variants { get; set; } = new();
    [JsonPropertyName("print_areas")] public List<PrintArea> PrintAreas { get; set; } = new();
    [JsonPropertyName("tags")] public List<string>? Tags { get; set; }
    [JsonPropertyName("safety_information")] public string? SafetyInformation { get; set; }
    [JsonPropertyName("print_details")] public PrintDetails? PrintDetails { get; set; }
}

public record CreateProductVariant
{
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("price")] public int Price { get; set; }
    [JsonPropertyName("is_enabled")] public bool IsEnabled { get; set; }
}

public record UpdateProductRequest
{
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("tags")] public List<string>? Tags { get; set; }
    [JsonPropertyName("variants")] public List<CreateProductVariant>? Variants { get; set; }
    [JsonPropertyName("print_areas")] public List<PrintArea>? PrintAreas { get; set; }
    [JsonPropertyName("print_details")] public PrintDetails? PrintDetails { get; set; }
}

public record PublishProductRequest
{
    [JsonPropertyName("title")] public bool Title { get; set; } = true;
    [JsonPropertyName("description")] public bool Description { get; set; } = true;
    [JsonPropertyName("images")] public bool Images { get; set; } = true;
    [JsonPropertyName("variants")] public bool Variants { get; set; } = true;
    [JsonPropertyName("tags")] public bool Tags { get; set; } = true;
    [JsonPropertyName("keyFeatures")] public bool KeyFeatures { get; set; } = true;
    [JsonPropertyName("shipping_template")] public bool ShippingTemplate { get; set; } = true;
}

public record PublishSucceededRequest
{
    [JsonPropertyName("external")] public ProductExternal External { get; set; } = new();
}

public record PublishFailedRequest
{
    [JsonPropertyName("reason")] public string Reason { get; set; } = "";
}

// ── Orders ─────────────────────────────────────────────────────────

public record Order
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("app_order_id")] public string? AppOrderId { get; set; }
    [JsonPropertyName("address_to")] public Address? AddressTo { get; set; }
    [JsonPropertyName("line_items")] public List<OrderLineItem> LineItems { get; set; } = new();
    [JsonPropertyName("metadata")] public OrderMetadata? Metadata { get; set; }
    [JsonPropertyName("total_price")] public int TotalPrice { get; set; }
    [JsonPropertyName("total_shipping")] public int TotalShipping { get; set; }
    [JsonPropertyName("total_tax")] public int TotalTax { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = "";
    [JsonPropertyName("shipping_method")] public int ShippingMethod { get; set; }
    [JsonPropertyName("is_printify_express")] public bool IsPrintifyExpress { get; set; }
    [JsonPropertyName("is_economy_shipping")] public bool IsEconomyShipping { get; set; }
    [JsonPropertyName("shipments")] public List<Shipment>? Shipments { get; set; }
    [JsonPropertyName("created_at")] public string? CreatedAt { get; set; }
    [JsonPropertyName("sent_to_production_at")] public string? SentToProductionAt { get; set; }
    [JsonPropertyName("fulfilled_at")] public string? FulfilledAt { get; set; }
    [JsonPropertyName("printify_connect")] public PrintifyConnect? PrintifyConnect { get; set; }
}

public record Address
{
    [JsonPropertyName("first_name")] public string FirstName { get; set; } = "";
    [JsonPropertyName("last_name")] public string LastName { get; set; } = "";
    [JsonPropertyName("email")] public string Email { get; set; } = "";
    [JsonPropertyName("phone")] public string Phone { get; set; } = "";
    [JsonPropertyName("country")] public string Country { get; set; } = "";
    [JsonPropertyName("region")] public string Region { get; set; } = "";
    [JsonPropertyName("address1")] public string Address1 { get; set; } = "";
    [JsonPropertyName("address2")] public string? Address2 { get; set; }
    [JsonPropertyName("city")] public string City { get; set; } = "";
    [JsonPropertyName("zip")] public string Zip { get; set; } = "";
    [JsonPropertyName("company")] public string? Company { get; set; }
}

public record OrderLineItem
{
    [JsonPropertyName("product_id")] public string? ProductId { get; set; }
    [JsonPropertyName("variant_id")] public int VariantId { get; set; }
    [JsonPropertyName("quantity")] public int Quantity { get; set; }
    [JsonPropertyName("print_provider_id")] public int PrintProviderId { get; set; }
    [JsonPropertyName("cost")] public int Cost { get; set; }
    [JsonPropertyName("shipping_cost")] public int ShippingCost { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = "";
    [JsonPropertyName("metadata")] public LineItemMetadata? Metadata { get; set; }
    [JsonPropertyName("sent_to_production_at")] public string? SentToProductionAt { get; set; }
    [JsonPropertyName("fulfilled_at")] public string? FulfilledAt { get; set; }
    [JsonPropertyName("external_id")] public string? ExternalId { get; set; }
}

public record LineItemMetadata
{
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("price")] public int Price { get; set; }
    [JsonPropertyName("variant_label")] public string VariantLabel { get; set; } = "";
    [JsonPropertyName("sku")] public string Sku { get; set; } = "";
    [JsonPropertyName("country")] public string Country { get; set; } = "";
    [JsonPropertyName("external_id")] public string? ExternalId { get; set; }
}

public record OrderMetadata
{
    [JsonPropertyName("order_type")] public string OrderType { get; set; } = "";
    [JsonPropertyName("shop_order_id")] public long ShopOrderId { get; set; }
    [JsonPropertyName("shop_order_label")] public string ShopOrderLabel { get; set; } = "";
    [JsonPropertyName("shop_fulfilled_at")] public string? ShopFulfilledAt { get; set; }
}

public record Shipment
{
    [JsonPropertyName("carrier")] public string Carrier { get; set; } = "";
    [JsonPropertyName("number")] public string Number { get; set; } = "";
    [JsonPropertyName("url")] public string Url { get; set; } = "";
    [JsonPropertyName("delivered_at")] public string? DeliveredAt { get; set; }
}

public record PrintifyConnect
{
    [JsonPropertyName("url")] public string Url { get; set; } = "";
    [JsonPropertyName("id")] public string Id { get; set; } = "";
}

// ── Order Submit DTOs ──────────────────────────────────────────────

public record SubmitOrderRequest
{
    [JsonPropertyName("external_id")] public string ExternalId { get; set; } = "";
    [JsonPropertyName("label")] public string? Label { get; set; }
    [JsonPropertyName("line_items")] public List<SubmitOrderLineItem> LineItems { get; set; } = new();
    [JsonPropertyName("shipping_method")] public int ShippingMethod { get; set; } = 1;
    [JsonPropertyName("is_printify_express")] public bool IsPrintifyExpress { get; set; }
    [JsonPropertyName("is_economy_shipping")] public bool IsEconomyShipping { get; set; }
    [JsonPropertyName("send_shipping_notification")] public bool SendShippingNotification { get; set; }
    [JsonPropertyName("address_to")] public Address AddressTo { get; set; } = new();
}

public record SubmitOrderLineItem
{
    [JsonPropertyName("product_id")] public string? ProductId { get; set; }
    [JsonPropertyName("variant_id")] public int VariantId { get; set; }
    [JsonPropertyName("quantity")] public int Quantity { get; set; }
    [JsonPropertyName("sku")] public string? Sku { get; set; }
    [JsonPropertyName("print_provider_id")] public int? PrintProviderId { get; set; }
    [JsonPropertyName("blueprint_id")] public int? BlueprintId { get; set; }
    [JsonPropertyName("print_areas")] public Dictionary<string, object>? PrintAreas { get; set; }
    [JsonPropertyName("print_details")] public PrintDetails? PrintDetails { get; set; }
    [JsonPropertyName("external_id")] public string? ExternalId { get; set; }
}

public record ShippingCostRequest
{
    [JsonPropertyName("line_items")] public List<SubmitOrderLineItem> LineItems { get; set; } = new();
    [JsonPropertyName("address_to")] public Address AddressTo { get; set; } = new();
}

public record ShippingCostResponse
{
    [JsonPropertyName("standard")] public int Standard { get; set; }
    [JsonPropertyName("express")] public int Express { get; set; }
    [JsonPropertyName("priority")] public int Priority { get; set; }
    [JsonPropertyName("printify_express")] public int PrintifyExpress { get; set; }
    [JsonPropertyName("economy")] public int Economy { get; set; }
}

// ── Uploads ────────────────────────────────────────────────────────

public record UploadedImage
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("file_name")] public string FileName { get; set; } = "";
    [JsonPropertyName("height")] public int Height { get; set; }
    [JsonPropertyName("width")] public int Width { get; set; }
    [JsonPropertyName("size")] public long Size { get; set; }
    [JsonPropertyName("mime_type")] public string MimeType { get; set; } = "";
    [JsonPropertyName("preview_url")] public string PreviewUrl { get; set; } = "";
    [JsonPropertyName("upload_time")] public string UploadTime { get; set; } = "";
}

public record UploadImageByUrlRequest
{
    [JsonPropertyName("file_name")] public string FileName { get; set; } = "";
    [JsonPropertyName("url")] public string Url { get; set; } = "";
}

public record UploadImageByBase64Request
{
    [JsonPropertyName("file_name")] public string FileName { get; set; } = "";
    [JsonPropertyName("contents")] public string Contents { get; set; } = "";
}

// ── Webhooks ───────────────────────────────────────────────────────

public record Webhook
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("topic")] public string Topic { get; set; } = "";
    [JsonPropertyName("url")] public string Url { get; set; } = "";
    [JsonPropertyName("shop_id")] public int ShopId { get; set; }
    [JsonPropertyName("secret")] public string? Secret { get; set; }
}

public record CreateWebhookRequest
{
    [JsonPropertyName("topic")] public string Topic { get; set; } = "";
    [JsonPropertyName("url")] public string Url { get; set; } = "";
    [JsonPropertyName("secret")] public string? Secret { get; set; }
}

public record UpdateWebhookRequest
{
    [JsonPropertyName("url")] public string Url { get; set; } = "";
}

// ── Events ─────────────────────────────────────────────────────────

public record PrintifyEvent
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("created_at")] public string CreatedAt { get; set; } = "";
    [JsonPropertyName("resource")] public EventResource? Resource { get; set; }
}

public record EventResource
{
    [JsonPropertyName("id")] public object? Id { get; set; }
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("data")] public Dictionary<string, object>? Data { get; set; }
}

// ── Pagination Wrapper ─────────────────────────────────────────────

public record PaginatedResponse<T>
{
    [JsonPropertyName("current_page")] public int CurrentPage { get; set; }
    [JsonPropertyName("last_page")] public int LastPage { get; set; }
    [JsonPropertyName("total")] public int Total { get; set; }
    [JsonPropertyName("per_page")] public int PerPage { get; set; }
    [JsonPropertyName("from")] public int? From { get; set; }
    [JsonPropertyName("to")] public int? To { get; set; }
    [JsonPropertyName("first_page_url")] public string? FirstPageUrl { get; set; }
    [JsonPropertyName("last_page_url")] public string? LastPageUrl { get; set; }
    [JsonPropertyName("next_page_url")] public string? NextPageUrl { get; set; }
    [JsonPropertyName("prev_page_url")] public string? PrevPageUrl { get; set; }
    [JsonPropertyName("data")] public List<T> Data { get; set; } = new();
}
