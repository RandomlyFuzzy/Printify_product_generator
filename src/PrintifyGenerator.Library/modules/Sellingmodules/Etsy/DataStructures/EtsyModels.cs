using System.Collections.Generic;
using System.Text.Json.Serialization;

// ─── Auth ──────────────────────────────────────────────────────────────────

public class EtsyTokenResponse
{
    [JsonPropertyName("access_token")]  public string AccessToken  { get; set; } = "";
    [JsonPropertyName("token_type")]    public string TokenType    { get; set; } = "";
    [JsonPropertyName("expires_in")]    public int    ExpiresIn    { get; set; }
    [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; } = "";
}

// ─── Shared primitives ─────────────────────────────────────────────────────

public class EtsyMoney
{
    [JsonPropertyName("amount")]        public int    Amount       { get; set; }
    [JsonPropertyName("divisor")]       public int    Divisor      { get; set; }
    [JsonPropertyName("currency_code")] public string CurrencyCode { get; set; } = "";

    public decimal ToDecimal() => Divisor > 0 ? (decimal)Amount / Divisor : Amount;
}

public class EtsyPagedResult<T>
{
    [JsonPropertyName("count")]   public int       Count   { get; set; }
    [JsonPropertyName("results")] public List<T>   Results { get; set; } = new();
}

// ─── Other / Ping ──────────────────────────────────────────────────────────

public class EtsyPingResponse
{
    [JsonPropertyName("application_id")] public long ApplicationId { get; set; }
}

// ─── User ──────────────────────────────────────────────────────────────────

public class EtsyUser
{
    [JsonPropertyName("user_id")]         public long   UserId        { get; set; }
    [JsonPropertyName("primary_email")]   public string PrimaryEmail  { get; set; } = "";
    [JsonPropertyName("first_name")]      public string FirstName     { get; set; } = "";
    [JsonPropertyName("last_name")]       public string LastName      { get; set; } = "";
    [JsonPropertyName("image_url_75x75")] public string ImageUrl75x75 { get; set; } = "";
    [JsonPropertyName("shop_id")]         public long?  ShopId        { get; set; }
}

public class EtsyMeResponse
{
    [JsonPropertyName("user_id")] public long  UserId { get; set; }
    [JsonPropertyName("shop_id")] public long? ShopId { get; set; }
}

// ─── Shop ──────────────────────────────────────────────────────────────────

public class EtsyShop
{
    [JsonPropertyName("shop_id")]                          public long         ShopId                        { get; set; }
    [JsonPropertyName("user_id")]                          public long         UserId                        { get; set; }
    [JsonPropertyName("shop_name")]                        public string       ShopName                      { get; set; } = "";
    [JsonPropertyName("created_timestamp")]                public long         CreatedTimestamp              { get; set; }
    [JsonPropertyName("title")]                            public string?      Title                         { get; set; }
    [JsonPropertyName("announcement")]                     public string?      Announcement                  { get; set; }
    [JsonPropertyName("currency_code")]                    public string       CurrencyCode                  { get; set; } = "";
    [JsonPropertyName("is_vacation")]                      public bool         IsVacation                    { get; set; }
    [JsonPropertyName("vacation_message")]                 public string?      VacationMessage               { get; set; }
    [JsonPropertyName("sale_message")]                     public string?      SaleMessage                   { get; set; }
    [JsonPropertyName("digital_sale_message")]             public string?      DigitalSaleMessage            { get; set; }
    [JsonPropertyName("updated_timestamp")]                public long         UpdatedTimestamp              { get; set; }
    [JsonPropertyName("listing_active_count")]             public int          ListingActiveCount            { get; set; }
    [JsonPropertyName("digital_listing_count")]            public int          DigitalListingCount           { get; set; }
    [JsonPropertyName("login_name")]                       public string       LoginName                     { get; set; } = "";
    [JsonPropertyName("accepts_custom_requests")]          public bool         AcceptsCustomRequests         { get; set; }
    [JsonPropertyName("policy_welcome")]                   public string?      PolicyWelcome                 { get; set; }
    [JsonPropertyName("policy_payment")]                   public string?      PolicyPayment                 { get; set; }
    [JsonPropertyName("policy_shipping")]                  public string?      PolicyShipping                { get; set; }
    [JsonPropertyName("policy_refunds")]                   public string?      PolicyRefunds                 { get; set; }
    [JsonPropertyName("policy_additional")]                public string?      PolicyAdditional              { get; set; }
    [JsonPropertyName("vacation_autoreply")]               public string?      VacationAutoreply             { get; set; }
    [JsonPropertyName("url")]                              public string?      Url                           { get; set; }
    [JsonPropertyName("image_url_760x100")]                public string?      ImageUrl760x100               { get; set; }
    [JsonPropertyName("num_favorers")]                     public int          NumFavorers                   { get; set; }
    [JsonPropertyName("languages")]                        public List<string> Languages                     { get; set; } = new();
    [JsonPropertyName("icon_url_fullxfull")]               public string?      IconUrlFullxfull              { get; set; }
    [JsonPropertyName("is_using_structured_policies")]     public bool         IsUsingStructuredPolicies     { get; set; }
    [JsonPropertyName("has_onboarded_structured_policies")] public bool        HasOnboardedStructuredPolicies { get; set; }
    [JsonPropertyName("is_etsy_payments_onboarded")]       public bool         IsEtsyPaymentsOnboarded       { get; set; }
    [JsonPropertyName("transaction_sold_count")]           public int          TransactionSoldCount          { get; set; }
    [JsonPropertyName("shipping_from_country_iso")]        public string?      ShippingFromCountryIso        { get; set; }
    [JsonPropertyName("shop_location_country_iso")]        public string?      ShopLocationCountryIso        { get; set; }
    [JsonPropertyName("review_count")]                     public int?         ReviewCount                   { get; set; }
    [JsonPropertyName("review_average")]                   public double?      ReviewAverage                 { get; set; }
}

public class EtsyUpdateShopRequest
{
    [JsonPropertyName("title")]                 public string? Title               { get; set; }
    [JsonPropertyName("announcement")]          public string? Announcement        { get; set; }
    [JsonPropertyName("sale_message")]          public string? SaleMessage         { get; set; }
    [JsonPropertyName("digital_sale_message")]  public string? DigitalSaleMessage  { get; set; }
    [JsonPropertyName("policy_additional")]     public string? PolicyAdditional    { get; set; }
}

// ─── Shop Section ──────────────────────────────────────────────────────────

public class EtsyShopSection
{
    [JsonPropertyName("shop_section_id")]      public long   ShopSectionId     { get; set; }
    [JsonPropertyName("title")]                public string Title              { get; set; } = "";
    [JsonPropertyName("rank")]                 public int    Rank               { get; set; }
    [JsonPropertyName("user_id")]              public long   UserId             { get; set; }
    [JsonPropertyName("active_listing_count")] public int    ActiveListingCount { get; set; }
}

// ─── Shop Return Policy ────────────────────────────────────────────────────

public class EtsyReturnPolicy
{
    [JsonPropertyName("return_policy_id")]    public long   ReturnPolicyId   { get; set; }
    [JsonPropertyName("shop_id")]             public long   ShopId           { get; set; }
    [JsonPropertyName("accepts_returns")]     public bool   AcceptsReturns   { get; set; }
    [JsonPropertyName("accepts_exchanges")]   public bool   AcceptsExchanges { get; set; }
    [JsonPropertyName("return_deadline")]     public int?   ReturnDeadline   { get; set; }
}

// ─── Listings ──────────────────────────────────────────────────────────────

public class EtsyListing
{
    [JsonPropertyName("listing_id")]                  public long          ListingId                { get; set; }
    [JsonPropertyName("user_id")]                     public long          UserId                   { get; set; }
    [JsonPropertyName("shop_id")]                     public long          ShopId                   { get; set; }
    [JsonPropertyName("title")]                       public string        Title                    { get; set; } = "";
    [JsonPropertyName("description")]                 public string        Description              { get; set; } = "";
    [JsonPropertyName("state")]                       public string        State                    { get; set; } = "";
    [JsonPropertyName("creation_timestamp")]          public long          CreationTimestamp        { get; set; }
    [JsonPropertyName("created_timestamp")]           public long          CreatedTimestamp         { get; set; }
    [JsonPropertyName("ending_timestamp")]            public long?         EndingTimestamp          { get; set; }
    [JsonPropertyName("last_modified_timestamp")]     public long          LastModifiedTimestamp    { get; set; }
    [JsonPropertyName("updated_timestamp")]           public long          UpdatedTimestamp         { get; set; }
    [JsonPropertyName("quantity")]                    public int           Quantity                 { get; set; }
    [JsonPropertyName("shop_section_id")]             public long?         ShopSectionId            { get; set; }
    [JsonPropertyName("featured_rank")]               public int           FeaturedRank             { get; set; }
    [JsonPropertyName("url")]                         public string?       Url                      { get; set; }
    [JsonPropertyName("num_favorers")]                public int           NumFavorers              { get; set; }
    [JsonPropertyName("non_taxable")]                 public bool          NonTaxable               { get; set; }
    [JsonPropertyName("is_taxable")]                  public bool          IsTaxable                { get; set; }
    [JsonPropertyName("is_customizable")]             public bool          IsCustomizable           { get; set; }
    [JsonPropertyName("is_personalizable")]           public bool          IsPersonalizable         { get; set; }
    [JsonPropertyName("listing_type")]                public string        ListingType              { get; set; } = "physical";
    [JsonPropertyName("tags")]                        public List<string>? Tags                     { get; set; }
    [JsonPropertyName("materials")]                   public List<string>? Materials                { get; set; }
    [JsonPropertyName("shipping_profile_id")]         public long?         ShippingProfileId        { get; set; }
    [JsonPropertyName("return_policy_id")]            public long?         ReturnPolicyId           { get; set; }
    [JsonPropertyName("processing_min")]              public int?          ProcessingMin            { get; set; }
    [JsonPropertyName("processing_max")]              public int?          ProcessingMax            { get; set; }
    [JsonPropertyName("who_made")]                    public string        WhoMade                  { get; set; } = "";
    [JsonPropertyName("when_made")]                   public string        WhenMade                 { get; set; } = "";
    [JsonPropertyName("is_supply")]                   public bool          IsSupply                 { get; set; }
    [JsonPropertyName("item_weight")]                 public double?       ItemWeight               { get; set; }
    [JsonPropertyName("item_weight_unit")]             public string?       ItemWeightUnit           { get; set; }
    [JsonPropertyName("item_length")]                 public double?       ItemLength               { get; set; }
    [JsonPropertyName("item_width")]                  public double?       ItemWidth                { get; set; }
    [JsonPropertyName("item_height")]                 public double?       ItemHeight               { get; set; }
    [JsonPropertyName("item_dimensions_unit")]        public string?       ItemDimensionsUnit       { get; set; }
    [JsonPropertyName("is_private")]                  public bool          IsPrivate                { get; set; }
    [JsonPropertyName("style")]                       public List<string>? Style                    { get; set; }
    [JsonPropertyName("has_variations")]              public bool          HasVariations            { get; set; }
    [JsonPropertyName("should_auto_renew")]           public bool          ShouldAutoRenew          { get; set; }
    [JsonPropertyName("language")]                    public string?       Language                 { get; set; }
    [JsonPropertyName("price")]                       public EtsyMoney?    Price                    { get; set; }
    [JsonPropertyName("taxonomy_id")]                 public long?         TaxonomyId               { get; set; }
    [JsonPropertyName("images")]                      public List<EtsyListingImage>?  Images       { get; set; }
    [JsonPropertyName("videos")]                      public List<EtsyListingVideo>?  Videos       { get; set; }
    [JsonPropertyName("skus")]                        public List<string>? Skus                     { get; set; }
    [JsonPropertyName("production_partners")]         public List<EtsyProductionPartner>? ProductionPartners { get; set; }
}

public class EtsyCreateDraftListingRequest
{
    public int     Quantity          { get; set; }
    public string  Title             { get; set; } = "";
    public string  Description       { get; set; } = "";
    public float   Price             { get; set; }
    /// <summary>i_did | someone_else | collective</summary>
    public string  WhoMade           { get; set; } = "i_did";
    /// <summary>made_to_order | 2020_2024 | 2015_2019 | 2010_2014 | 2005_2009 | before_2005 | 2000_2004 | 1990s | 1980s | 1970s | 1960s | 1950s | 1940s | 1930s | 1920s | before_1920</summary>
    public string  WhenMade          { get; set; } = "made_to_order";
    public bool?   IsSupply          { get; set; }
    public long?   TaxonomyId        { get; set; }
    public string? Type              { get; set; }
    public long?   ShippingProfileId { get; set; }
    public long?   ReturnPolicyId    { get; set; }
    public long?   ShopSectionId     { get; set; }
    public string[]? Tags            { get; set; }
    public string[]? Materials       { get; set; }
    public string[]? Style           { get; set; }
    public int?    ProcessingMin     { get; set; }
    public int?    ProcessingMax     { get; set; }
    public bool?   IsPersonalizable  { get; set; }
    public bool?   IsCustomizable    { get; set; }
    public bool?   ShouldAutoRenew   { get; set; }
    public bool?   IsTaxable         { get; set; }
    public string? ItemWeightUnit    { get; set; }
    public double? ItemWeight        { get; set; }
    public string? ItemDimensionsUnit { get; set; }
    public double? ItemLength        { get; set; }
    public double? ItemWidth         { get; set; }
    public double? ItemHeight        { get; set; }
}

public class EtsyUpdateListingRequest
{
    public int?    Quantity          { get; set; }
    public string? Title             { get; set; }
    public string? Description       { get; set; }
    public float?  Price             { get; set; }
    /// <summary>active | inactive</summary>
    public string? State             { get; set; }
    public string? WhoMade           { get; set; }
    public string? WhenMade          { get; set; }
    public bool?   IsSupply          { get; set; }
    public long?   TaxonomyId        { get; set; }
    public long?   ShippingProfileId { get; set; }
    public long?   ReturnPolicyId    { get; set; }
    public long?   ShopSectionId     { get; set; }
    public string[]? Tags            { get; set; }
    public string[]? Materials       { get; set; }
    public string[]? Style           { get; set; }
    public int?    ProcessingMin     { get; set; }
    public int?    ProcessingMax     { get; set; }
    public bool?   IsPersonalizable  { get; set; }
    public bool?   IsCustomizable    { get; set; }
    public bool?   ShouldAutoRenew   { get; set; }
    public bool?   IsTaxable         { get; set; }
    public bool?   IsTaxable2        { get; set; }
    public string? FeaturedRank      { get; set; }
}

// ─── Listing Image ─────────────────────────────────────────────────────────

public class EtsyListingImage
{
    [JsonPropertyName("listing_id")]        public long    ListingId       { get; set; }
    [JsonPropertyName("listing_image_id")]  public long    ListingImageId  { get; set; }
    [JsonPropertyName("hex_code")]          public string? HexCode         { get; set; }
    [JsonPropertyName("red")]               public int     Red             { get; set; }
    [JsonPropertyName("green")]             public int     Green           { get; set; }
    [JsonPropertyName("blue")]              public int     Blue            { get; set; }
    [JsonPropertyName("hue")]               public int     Hue             { get; set; }
    [JsonPropertyName("saturation")]        public int     Saturation      { get; set; }
    [JsonPropertyName("brightness")]        public int     Brightness      { get; set; }
    [JsonPropertyName("is_black_and_white")] public bool   IsBlackAndWhite { get; set; }
    [JsonPropertyName("created_timestamp")] public long    CreatedTimestamp { get; set; }
    [JsonPropertyName("rank")]              public int     Rank            { get; set; }
    [JsonPropertyName("url_75x75")]         public string? Url75x75        { get; set; }
    [JsonPropertyName("url_170x135")]       public string? Url170x135      { get; set; }
    [JsonPropertyName("url_570xN")]         public string? Url570xN        { get; set; }
    [JsonPropertyName("url_fullxfull")]     public string? UrlFullxfull    { get; set; }
    [JsonPropertyName("full_height")]       public int?    FullHeight      { get; set; }
    [JsonPropertyName("full_width")]        public int?    FullWidth       { get; set; }
    [JsonPropertyName("alt_text")]          public string? AltText         { get; set; }
}

// ─── Listing Video ─────────────────────────────────────────────────────────

public class EtsyListingVideo
{
    [JsonPropertyName("video_id")]          public long    VideoId         { get; set; }
    [JsonPropertyName("height")]            public int     Height          { get; set; }
    [JsonPropertyName("width")]             public int     Width           { get; set; }
    [JsonPropertyName("thumbnail_url")]     public string? ThumbnailUrl    { get; set; }
    [JsonPropertyName("video_url")]         public string? VideoUrl        { get; set; }
    [JsonPropertyName("video_state")]       public string  VideoState      { get; set; } = "";
}

// ─── Listing File (digital) ────────────────────────────────────────────────

public class EtsyListingFile
{
    [JsonPropertyName("listing_file_id")]   public long   ListingFileId   { get; set; }
    [JsonPropertyName("listing_id")]        public long   ListingId       { get; set; }
    [JsonPropertyName("rank")]              public int    Rank            { get; set; }
    [JsonPropertyName("filename")]          public string Filename        { get; set; } = "";
    [JsonPropertyName("filesize")]          public string Filesize        { get; set; } = "";
    [JsonPropertyName("size_bytes")]        public long   SizeBytes       { get; set; }
    [JsonPropertyName("filetype")]          public string Filetype        { get; set; } = "";
    [JsonPropertyName("created_timestamp")] public long   CreatedTimestamp { get; set; }
}

// ─── Listing Inventory ─────────────────────────────────────────────────────

public class EtsyListingInventory
{
    [JsonPropertyName("products")]                  public List<EtsyListingProduct>  Products                { get; set; } = new();
    [JsonPropertyName("price_on_property")]          public List<long>               PriceOnProperty          { get; set; } = new();
    [JsonPropertyName("quantity_on_property")]       public List<long>               QuantityOnProperty       { get; set; } = new();
    [JsonPropertyName("sku_on_property")]            public List<long>               SkuOnProperty            { get; set; } = new();
    [JsonPropertyName("readiness_state_on_property")] public List<long>              ReadinessStateOnProperty { get; set; } = new();
    [JsonPropertyName("listing")]                   public EtsyListing?              Listing                  { get; set; }
}

public class EtsyListingProduct
{
    [JsonPropertyName("product_id")]      public long                      ProductId       { get; set; }
    [JsonPropertyName("sku")]             public string?                   Sku             { get; set; }
    [JsonPropertyName("is_deleted")]      public bool                      IsDeleted       { get; set; }
    [JsonPropertyName("offerings")]       public List<EtsyListingOffering> Offerings       { get; set; } = new();
    [JsonPropertyName("property_values")] public List<EtsyPropertyValue>   PropertyValues  { get; set; } = new();
}

public class EtsyListingOffering
{
    [JsonPropertyName("offering_id")]       public long      OfferingId      { get; set; }
    [JsonPropertyName("quantity")]          public int       Quantity        { get; set; }
    [JsonPropertyName("is_enabled")]        public bool      IsEnabled       { get; set; }
    [JsonPropertyName("is_deleted")]        public bool      IsDeleted       { get; set; }
    [JsonPropertyName("price")]             public EtsyMoney Price           { get; set; } = new();
    [JsonPropertyName("readiness_state_id")] public long?    ReadinessStateId { get; set; }
}

public class EtsyPropertyValue
{
    [JsonPropertyName("property_id")]   public long          PropertyId   { get; set; }
    [JsonPropertyName("property_name")] public string        PropertyName { get; set; } = "";
    [JsonPropertyName("scale_id")]      public long?         ScaleId      { get; set; }
    [JsonPropertyName("scale_name")]    public string?       ScaleName    { get; set; }
    [JsonPropertyName("value_ids")]     public List<long>    ValueIds     { get; set; } = new();
    [JsonPropertyName("values")]        public List<string>  Values       { get; set; } = new();
}

public class EtsyUpdateListingInventoryRequest
{
    [JsonPropertyName("products")] public List<EtsyInventoryProductRequest> Products { get; set; } = new();
    [JsonPropertyName("price_on_property")] public List<long>? PriceOnProperty { get; set; }
    [JsonPropertyName("quantity_on_property")] public List<long>? QuantityOnProperty { get; set; }
    [JsonPropertyName("sku_on_property")] public List<long>? SkuOnProperty { get; set; }
}

public class EtsyInventoryProductRequest
{
    [JsonPropertyName("product_id")]      public long?                              ProductId      { get; set; }
    [JsonPropertyName("sku")]             public string?                            Sku            { get; set; }
    [JsonPropertyName("offerings")]       public List<EtsyInventoryOfferingRequest> Offerings      { get; set; } = new();
    [JsonPropertyName("property_values")] public List<EtsyPropertyValue>?           PropertyValues { get; set; }
}

public class EtsyInventoryOfferingRequest
{
    [JsonPropertyName("offering_id")] public long?  OfferingId { get; set; }
    [JsonPropertyName("quantity")]    public int    Quantity   { get; set; }
    [JsonPropertyName("is_enabled")]  public bool   IsEnabled  { get; set; }
    [JsonPropertyName("price")]       public float  Price      { get; set; }
}

// ─── Listing Property ──────────────────────────────────────────────────────

public class EtsyListingPropertyRequest
{
    [JsonPropertyName("value_ids")]  public List<long>   ValueIds  { get; set; } = new();
    [JsonPropertyName("values")]     public List<string> Values    { get; set; } = new();
    [JsonPropertyName("scale_id")]   public long?        ScaleId   { get; set; }
}

// ─── Listing Translation ───────────────────────────────────────────────────

public class EtsyListingTranslation
{
    [JsonPropertyName("listing_id")]  public long          ListingId   { get; set; }
    [JsonPropertyName("language")]    public string        Language    { get; set; } = "";
    [JsonPropertyName("title")]       public string        Title       { get; set; } = "";
    [JsonPropertyName("description")] public string        Description { get; set; } = "";
    [JsonPropertyName("tags")]        public List<string>? Tags        { get; set; }
}

public class EtsyListingTranslationRequest
{
    public string   Title       { get; set; } = "";
    public string   Description { get; set; } = "";
    public string[]? Tags       { get; set; }
}

// ─── Variation Images ──────────────────────────────────────────────────────

public class EtsyVariationImage
{
    [JsonPropertyName("listing_image_id")] public long ListingImageId { get; set; }
    [JsonPropertyName("value_id")]         public long ValueId        { get; set; }
    [JsonPropertyName("value")]            public string Value        { get; set; } = "";
    [JsonPropertyName("property_id")]      public long PropertyId     { get; set; }
}

public class EtsyUpdateVariationImagesRequest
{
    [JsonPropertyName("variation_images")] public List<EtsyVariationImageInput> VariationImages { get; set; } = new();
}

public class EtsyVariationImageInput
{
    [JsonPropertyName("property_id")]      public long ListingId { get; set; }
    [JsonPropertyName("value_id")]         public long ValueId   { get; set; }
    [JsonPropertyName("image_id")]         public long ImageId   { get; set; }
}

// ─── Listing Personalization ───────────────────────────────────────────────

public class EtsyListingPersonalization
{
    [JsonPropertyName("personalization_questions")] public List<EtsyPersonalizationQuestion> PersonalizationQuestions { get; set; } = new();
}

public class EtsyPersonalizationQuestion
{
    [JsonPropertyName("question")]      public string Question    { get; set; } = "";
    [JsonPropertyName("is_required")]   public bool   IsRequired  { get; set; }
    [JsonPropertyName("character_limit")] public int  CharacterLimit { get; set; }
}

public class EtsyUpdateListingPersonalizationRequest
{
    public bool    IsPersonalizable  { get; set; }
    public int?    PersonalizationCharLimit { get; set; }
    public string? PersonalizationInstructions { get; set; }
}

// ─── Production Partners ───────────────────────────────────────────────────

public class EtsyProductionPartner
{
    [JsonPropertyName("production_partner_id")] public long   ProductionPartnerId { get; set; }
    [JsonPropertyName("partner_name")]          public string PartnerName         { get; set; } = "";
    [JsonPropertyName("location")]              public string Location             { get; set; } = "";
}

// ─── Receipts (Orders) ─────────────────────────────────────────────────────

public class EtsyReceipt
{
    [JsonPropertyName("receipt_id")]          public long              ReceiptId        { get; set; }
    [JsonPropertyName("receipt_type")]        public string            ReceiptType      { get; set; } = "";
    [JsonPropertyName("seller_user_id")]      public long              SellerUserId     { get; set; }
    [JsonPropertyName("seller_email")]        public string?           SellerEmail      { get; set; }
    [JsonPropertyName("buyer_user_id")]       public long              BuyerUserId      { get; set; }
    [JsonPropertyName("buyer_email")]         public string?           BuyerEmail       { get; set; }
    [JsonPropertyName("name")]                public string            Name             { get; set; } = "";
    [JsonPropertyName("first_line")]          public string?           FirstLine        { get; set; }
    [JsonPropertyName("second_line")]         public string?           SecondLine       { get; set; }
    [JsonPropertyName("city")]                public string?           City             { get; set; }
    [JsonPropertyName("state")]               public string?           State            { get; set; }
    [JsonPropertyName("zip")]                 public string?           Zip              { get; set; }
    [JsonPropertyName("country_iso")]         public string?           CountryIso       { get; set; }
    [JsonPropertyName("formatted_address")]   public string?           FormattedAddress { get; set; }
    [JsonPropertyName("payment_method")]      public string            PaymentMethod    { get; set; } = "";
    [JsonPropertyName("payment_email")]       public string?           PaymentEmail     { get; set; }
    [JsonPropertyName("message_from_payment")] public string?          MessageFromPayment { get; set; }
    [JsonPropertyName("message_from_seller")] public string?           MessageFromSeller { get; set; }
    [JsonPropertyName("message_from_buyer")]  public string?           MessageFromBuyer { get; set; }
    [JsonPropertyName("is_shipped")]          public bool              IsShipped        { get; set; }
    [JsonPropertyName("is_paid")]             public bool              IsPaid           { get; set; }
    [JsonPropertyName("create_timestamp")]    public long              CreateTimestamp  { get; set; }
    [JsonPropertyName("created_timestamp")]   public long              CreatedTimestamp { get; set; }
    [JsonPropertyName("update_timestamp")]    public long              UpdateTimestamp  { get; set; }
    [JsonPropertyName("updated_timestamp")]   public long              UpdatedTimestamp { get; set; }
    [JsonPropertyName("is_gift")]             public bool              IsGift           { get; set; }
    [JsonPropertyName("gift_message")]        public string?           GiftMessage      { get; set; }
    [JsonPropertyName("grandtotal")]          public EtsyMoney?        Grandtotal       { get; set; }
    [JsonPropertyName("subtotal")]            public EtsyMoney?        Subtotal         { get; set; }
    [JsonPropertyName("total_price")]         public EtsyMoney?        TotalPrice       { get; set; }
    [JsonPropertyName("total_shipping_cost")] public EtsyMoney?        TotalShippingCost { get; set; }
    [JsonPropertyName("total_tax_cost")]      public EtsyMoney?        TotalTaxCost     { get; set; }
    [JsonPropertyName("total_vat_cost")]      public EtsyMoney?        TotalVatCost     { get; set; }
    [JsonPropertyName("discount_amt")]        public EtsyMoney?        DiscountAmt      { get; set; }
    [JsonPropertyName("transactions")]        public List<EtsyTransaction>? Transactions { get; set; }
    [JsonPropertyName("shop_id")]             public long              ShopId           { get; set; }
    [JsonPropertyName("status")]              public string?           Status           { get; set; }
    [JsonPropertyName("shipments")]           public List<EtsyShipment>? Shipments      { get; set; }
}

public class EtsyUpdateReceiptRequest
{
    public bool?   WasPaid      { get; set; }
    public bool?   WasShipped   { get; set; }
    public bool?   WasDelivered { get; set; }
    public bool?   IsCanceled   { get; set; }
    public string? Note         { get; set; }
}

public class EtsyCreateShipmentRequest
{
    public string? TrackingCode    { get; set; }
    public string? CarrierName     { get; set; }
    public bool?   SendBcc         { get; set; }
    public string? NoteToBuyer     { get; set; }
}

public class EtsyShipment
{
    [JsonPropertyName("receipt_shipping_id")] public long?   ReceiptShippingId { get; set; }
    [JsonPropertyName("shipment_notification_timestamp")] public long? ShipmentNotificationTimestamp { get; set; }
    [JsonPropertyName("carrier_name")]        public string? CarrierName { get; set; }
    [JsonPropertyName("tracking_code")]       public string? TrackingCode { get; set; }
}

// ─── Transactions ──────────────────────────────────────────────────────────

public class EtsyTransaction
{
    [JsonPropertyName("transaction_id")]      public long       TransactionId     { get; set; }
    [JsonPropertyName("title")]               public string     Title             { get; set; } = "";
    [JsonPropertyName("description")]         public string     Description       { get; set; } = "";
    [JsonPropertyName("seller_user_id")]      public long       SellerUserId      { get; set; }
    [JsonPropertyName("buyer_user_id")]       public long       BuyerUserId       { get; set; }
    [JsonPropertyName("create_timestamp")]    public long       CreateTimestamp   { get; set; }
    [JsonPropertyName("created_timestamp")]   public long       CreatedTimestamp  { get; set; }
    [JsonPropertyName("paid_timestamp")]      public long?      PaidTimestamp     { get; set; }
    [JsonPropertyName("shipped_timestamp")]   public long?      ShippedTimestamp  { get; set; }
    [JsonPropertyName("quantity")]            public int        Quantity          { get; set; }
    [JsonPropertyName("listing_image_id")]    public long?      ListingImageId    { get; set; }
    [JsonPropertyName("receipt_id")]          public long       ReceiptId         { get; set; }
    [JsonPropertyName("is_digital")]          public bool       IsDigital         { get; set; }
    [JsonPropertyName("listing_id")]          public long?      ListingId         { get; set; }
    [JsonPropertyName("transaction_type")]    public string     TransactionType   { get; set; } = "";
    [JsonPropertyName("product_id")]          public long?      ProductId         { get; set; }
    [JsonPropertyName("sku")]                 public string?    Sku               { get; set; }
    [JsonPropertyName("price")]               public EtsyMoney? Price             { get; set; }
    [JsonPropertyName("shipping_cost")]       public EtsyMoney? ShippingCost      { get; set; }
    [JsonPropertyName("variations")]          public List<EtsyVariation>? Variations { get; set; }
    [JsonPropertyName("shipping_profile_id")] public long?      ShippingProfileId { get; set; }
}

public class EtsyVariation
{
    [JsonPropertyName("property_id")]           public long    PropertyId          { get; set; }
    [JsonPropertyName("value_id")]              public long?   ValueId             { get; set; }
    [JsonPropertyName("formatted_name")]        public string  FormattedName       { get; set; } = "";
    [JsonPropertyName("formatted_value")]       public string  FormattedValue      { get; set; } = "";
}

// ─── Payments and Ledger ───────────────────────────────────────────────────

public class EtsyPayment
{
    [JsonPropertyName("payment_id")]          public long       PaymentId         { get; set; }
    [JsonPropertyName("buyer_user_id")]       public long       BuyerUserId       { get; set; }
    [JsonPropertyName("shop_id")]             public long       ShopId            { get; set; }
    [JsonPropertyName("receipt_id")]          public long       ReceiptId         { get; set; }
    [JsonPropertyName("amount_gross")]        public EtsyMoney? AmountGross       { get; set; }
    [JsonPropertyName("amount_fees")]         public EtsyMoney? AmountFees        { get; set; }
    [JsonPropertyName("amount_net")]          public EtsyMoney? AmountNet         { get; set; }
    [JsonPropertyName("posting_gross")]       public EtsyMoney? PostingGross      { get; set; }
    [JsonPropertyName("posting_fees")]        public EtsyMoney? PostingFees       { get; set; }
    [JsonPropertyName("posting_net")]         public EtsyMoney? PostingNet        { get; set; }
    [JsonPropertyName("currency")]            public string     Currency          { get; set; } = "";
    [JsonPropertyName("shop_currency")]       public string?    ShopCurrency      { get; set; }
    [JsonPropertyName("buyer_currency")]      public string?    BuyerCurrency     { get; set; }
    [JsonPropertyName("payment_type")]        public string?    PaymentType       { get; set; }
    [JsonPropertyName("status")]              public string     Status            { get; set; } = "";
    [JsonPropertyName("create_timestamp")]    public long       CreateTimestamp   { get; set; }
    [JsonPropertyName("created_timestamp")]   public long       CreatedTimestamp  { get; set; }
}

public class EtsyLedgerEntry
{
    [JsonPropertyName("entry_id")]            public long       EntryId           { get; set; }
    [JsonPropertyName("ledger_id")]           public long       LedgerId          { get; set; }
    [JsonPropertyName("sequence_number")]     public long       SequenceNumber    { get; set; }
    [JsonPropertyName("amount")]              public EtsyMoney? Amount            { get; set; }
    [JsonPropertyName("gross_amount")]        public EtsyMoney? GrossAmount       { get; set; }
    [JsonPropertyName("net_amount")]          public EtsyMoney? NetAmount         { get; set; }
    [JsonPropertyName("fee_amount")]          public EtsyMoney? FeeAmount         { get; set; }
    [JsonPropertyName("credit_amount")]       public EtsyMoney? CreditAmount      { get; set; }
    [JsonPropertyName("debit_amount")]        public EtsyMoney? DebitAmount       { get; set; }
    [JsonPropertyName("balance")]             public EtsyMoney? Balance           { get; set; }
    [JsonPropertyName("create_timestamp")]    public long       CreateTimestamp   { get; set; }
    [JsonPropertyName("created_timestamp")]   public long       CreatedTimestamp  { get; set; }
    [JsonPropertyName("ledger_type")]         public string     LedgerType        { get; set; } = "";
    [JsonPropertyName("reference_type")]      public string?    ReferenceType     { get; set; }
    [JsonPropertyName("reference_id")]        public string?    ReferenceId       { get; set; }
}

// ─── Reviews ───────────────────────────────────────────────────────────────

public class EtsyReview
{
    [JsonPropertyName("shop_id")]             public long    ShopId           { get; set; }
    [JsonPropertyName("listing_id")]          public long    ListingId        { get; set; }
    [JsonPropertyName("rating")]              public int     Rating           { get; set; }
    [JsonPropertyName("review")]              public string? Review           { get; set; }
    [JsonPropertyName("language")]            public string  Language         { get; set; } = "";
    [JsonPropertyName("image_url_fullxfull")] public string? ImageUrlFullxfull { get; set; }
    [JsonPropertyName("create_timestamp")]    public long    CreateTimestamp  { get; set; }
    [JsonPropertyName("created_timestamp")]   public long    CreatedTimestamp { get; set; }
    [JsonPropertyName("update_timestamp")]    public long    UpdateTimestamp  { get; set; }
    [JsonPropertyName("updated_timestamp")]   public long    UpdatedTimestamp { get; set; }
}

// ─── Shipping Profiles ─────────────────────────────────────────────────────

public class EtsyShippingProfile
{
    [JsonPropertyName("shipping_profile_id")]          public long    ShippingProfileId       { get; set; }
    [JsonPropertyName("title")]                        public string  Title                   { get; set; } = "";
    [JsonPropertyName("user_id")]                      public long    UserId                  { get; set; }
    [JsonPropertyName("origin_country_iso")]           public string  OriginCountryIso        { get; set; } = "";
    [JsonPropertyName("is_deleted")]                   public bool    IsDeleted               { get; set; }
    [JsonPropertyName("origin_postal_code")]           public string? OriginPostalCode        { get; set; }
    [JsonPropertyName("profile_type")]                 public string  ProfileType             { get; set; } = "";
    [JsonPropertyName("domestic_handling_fee")]        public double  DomesticHandlingFee     { get; set; }
    [JsonPropertyName("international_handling_fee")]   public double  InternationalHandlingFee { get; set; }
    [JsonPropertyName("shipping_profile_destinations")] public List<EtsyShippingProfileDestination>? ShippingProfileDestinations { get; set; }
    [JsonPropertyName("shipping_profile_upgrades")]    public List<EtsyShippingProfileUpgrade>?      ShippingProfileUpgrades    { get; set; }
}

public class EtsyShippingProfileDestination
{
    [JsonPropertyName("shipping_profile_destination_id")] public long       DestinationId      { get; set; }
    [JsonPropertyName("shipping_profile_id")]             public long       ShippingProfileId  { get; set; }
    [JsonPropertyName("origin_country_iso")]              public string     OriginCountryIso   { get; set; } = "";
    [JsonPropertyName("destination_country_iso")]         public string     DestCountryIso     { get; set; } = "";
    [JsonPropertyName("destination_region")]              public string?    DestinationRegion  { get; set; }
    [JsonPropertyName("primary_cost")]                    public EtsyMoney? PrimaryCost        { get; set; }
    [JsonPropertyName("secondary_cost")]                  public EtsyMoney? SecondaryCost      { get; set; }
    [JsonPropertyName("shipping_carrier_id")]             public long?      ShippingCarrierId  { get; set; }
    [JsonPropertyName("mail_class")]                      public string?    MailClass          { get; set; }
    [JsonPropertyName("min_delivery_days")]               public int?       MinDeliveryDays    { get; set; }
    [JsonPropertyName("max_delivery_days")]               public int?       MaxDeliveryDays    { get; set; }
}

public class EtsyShippingProfileUpgrade
{
    [JsonPropertyName("shipping_profile_id")]    public long       ShippingProfileId { get; set; }
    [JsonPropertyName("upgrade_id")]             public long       UpgradeId         { get; set; }
    [JsonPropertyName("upgrade_name")]           public string     UpgradeName       { get; set; } = "";
    [JsonPropertyName("type")]                   public string     Type              { get; set; } = "";
    [JsonPropertyName("rank")]                   public int        Rank              { get; set; }
    [JsonPropertyName("language")]               public string     Language          { get; set; } = "";
    [JsonPropertyName("price")]                  public EtsyMoney? Price             { get; set; }
    [JsonPropertyName("secondary_price")]        public EtsyMoney? SecondaryPrice    { get; set; }
    [JsonPropertyName("shipping_carrier_id")]    public long?      ShippingCarrierId { get; set; }
    [JsonPropertyName("mail_class")]             public string?    MailClass         { get; set; }
    [JsonPropertyName("min_delivery_days")]      public int?       MinDeliveryDays   { get; set; }
    [JsonPropertyName("max_delivery_days")]      public int?       MaxDeliveryDays   { get; set; }
}

public class EtsyCreateShippingProfileRequest
{
    public string  Title             { get; set; } = "";
    public string  OriginCountryIso  { get; set; } = "";
    public string? OriginPostalCode  { get; set; }
    public float   PrimaryCost       { get; set; }
    public float   SecondaryCost     { get; set; }
    public string  DestinationCountryIso { get; set; } = "FR"; // everywhere
    public string? DestinationRegion { get; set; }
    public string? ProcessingTime    { get; set; }
    public int?    ProcessingTimeUnit { get; set; }
    public long?   ShippingCarrierId { get; set; }
    public string? MailClass         { get; set; }
    public int?    MinDeliveryDays   { get; set; }
    public int?    MaxDeliveryDays   { get; set; }
}

public class EtsyCreateShippingProfileDestinationRequest
{
    public float   PrimaryCost           { get; set; }
    public float   SecondaryCost         { get; set; }
    public string  OriginCountryIso      { get; set; } = "";
    public string  DestinationCountryIso { get; set; } = "";
    public string? DestinationRegion     { get; set; }
    public long?   ShippingCarrierId     { get; set; }
    public string? MailClass             { get; set; }
    public int?    MinDeliveryDays       { get; set; }
    public int?    MaxDeliveryDays       { get; set; }
}

public class EtsyCreateShippingProfileUpgradeRequest
{
    public string  Type              { get; set; } = "";
    public string  UpgradeName       { get; set; } = "";
    public float   Price             { get; set; }
    public float   SecondaryPrice    { get; set; }
    public long?   ShippingCarrierId { get; set; }
    public string? MailClass         { get; set; }
    public int?    MinDeliveryDays   { get; set; }
    public int?    MaxDeliveryDays   { get; set; }
}

public class EtsyShippingCarrier
{
    [JsonPropertyName("shipping_carrier_id")] public long               ShippingCarrierId { get; set; }
    [JsonPropertyName("name")]                public string             Name              { get; set; } = "";
    [JsonPropertyName("domestic_classes")]    public List<EtsyMailClass> DomesticClasses  { get; set; } = new();
    [JsonPropertyName("international_classes")] public List<EtsyMailClass> InternationalClasses { get; set; } = new();
}

public class EtsyMailClass
{
    [JsonPropertyName("mail_class_key")]  public string MailClassKey  { get; set; } = "";
    [JsonPropertyName("name")]            public string Name          { get; set; } = "";
}

// ─── Error Response ────────────────────────────────────────────────────────

public class EtsyApiException : Exception
{
    public int    StatusCode   { get; }
    public string ResponseBody { get; }

    public EtsyApiException(int statusCode, string responseBody)
        : base($"Etsy API error {statusCode}: {responseBody}")
    {
        StatusCode   = statusCode;
        ResponseBody = responseBody;
    }
}
