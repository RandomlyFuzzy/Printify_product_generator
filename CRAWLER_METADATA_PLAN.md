# Crawler Metadata Enhancement Plan

## Overview
Update all 5 crawlers (Amazon, Etsy, eBay, Walmart, AliExpress) to gather comprehensive metadata, then update Analytics API to ingest and query all data.

## Part 1: Crawler Metadata Updates

### Current State
The `ProductData.cs` model (118 lines) already has 100+ fields. However, extraction coverage varies by platform:

| Field Category | Amazon | Etsy | eBay | Walmart | AliExpress |
|--------------|--------|------|------|---------|------------|
| Basic Info (Title, Price, Description) | ✅ | ✅ | ✅ | ✅ | ✅ |
| Ratings/Reviews | ✅ | ✅ | Partial | ✅ | Partial |
| Images (Main + Thumbnails) | ✅ | ✅ | ✅ | ✅ | ✅ |
| Variants (Colors, Sizes, Prices) | Partial | ✅ | ❌ | Partial | ✅ |
| Seller Info + Rating | Partial | ✅ | ✅ | Partial | ✅ |
| SEO (Meta, OG, Twitter tags) | ✅ | ✅ | ✅ | ✅ | ✅ |
| Related Products (FBT, Also Bought) | ✅ | ✅ | ✅ | ✅ | ✅ |
| Unstructured Data (JSON-LD, Microdata) | ✅ | ✅ | ✅ | ✅ | ✅ |
| Stock/Inventory | ✅ | ✅ | Partial | ✅ | ❌ |
| Shipping Options | ✅ | ✅ | ✅ | ✅ | ✅ |
| Category Hierarchy (Breadcrumbs) | ✅ | ✅ | ✅ | ✅ | ✅ |
| ViewCount/FavoriteCount | ❌ | ✅ | Partial | ❌ | ✅ |
| Condition (New/Used) | ❌ | ❌ | ✅ | ❌ | ❌ |
| Return Policy | ❌ | ✅ | ✅ | ❌ | ❌ |
| Listing Date | ❌ | ❌ | ❌ | ❌ | ❌ |
| Accolades/Badges | Partial | ✅ | Partial | ✅ | Partial |
| Variant Prices Dictionary | ❌ | ✅ | ❌ | ❌ | ✅ |
| Cross-sell Product IDs | ❌ | ❌ | ❌ | ❌ | ❌ |

### Required Updates Per Crawler

#### 1. Amazon Crawler
- [ ] Extract **Variant Prices** (different colors/sizes have different prices)
- [ ] Extract **Condition** (New, Used, Refurbished)
- [ ] Extract **ViewCount** (watchers)
- [ ] Extract **Return Policy** text
- [ ] Extract **Listing Date** from product details
- [ ] Extract **ParentSku/ChildSkus** for variant relationships
- [ ] Improve **Seller Rating/Feedback** extraction from detail page
- [ ] Extract **Accolades** (Amazon's Choice, Best Seller badges more thoroughly)

#### 2. Etsy Crawler
- [ ] Extract **Condition** (Vintage, Handmade, Supplies)
- [ ] Extract **Return Policy** (already partial, verify)
- [ ] Extract **Listing Date** from product page
- [ ] Extract **Accolades** (Star Seller badge,Featured badge)
- [ ] Extract **ParentSku/ChildSkus**

#### 3. eBay Crawler
- [ ] Extract **Variant Prices** for auctions with multiple variants
- [ ] Extract **ViewCount** (watchers count)
- [ ] Extract **Return Policy** details (already partial)
- [ ] Extract **Listing Date** 
- [ ] Improve **Seller FeedbackCount** extraction
- [ ] Extract **Cross-sell Product IDs** from recommendations

#### 4. Walmart Crawler
- [ ] Extract **ViewCount** 
- [ ] Extract **Condition** (New, Refurbished)
- [ ] Extract **Return Policy**
- [ ] Extract **Listing Date**
- [ ] Extract **Variant Prices** for items with multiple options
- [ ] Extract **Accolades** (Best Seller, Editor's Pick)
- [ ] Improve **Seller Info** extraction (Pro Seller badge, seller rating)

#### 5. AliExpress Crawler
- [ ] Extract **Stock Quantity** 
- [ ] Extract **Condition**
- [ ] Extract **Return Policy**
- [ ] Extract **Listing Date**
- [ ] Extract **ViewCount** (views count)
- [ ] Extract **Cross-sell Product IDs**
- [ ] Improve **Seller FeedbackCount** extraction

### Human-Like Behavior (Already Implemented ✅)
- [x] NavigateLikeHumanAsync() - Navigate and check for CAPTCHA
- [x] ScrollLikeHumanAsync() - Random scroll amounts
- [x] ClickRandomItemsAsync() - Random clicks on page elements
- [x] SearchViaSearchBarAsync() - Use search bar instead of direct URL
- [x] DetectAndHandleCaptchaAsync() - Detect CAPTCHA and show for human resolution

---

## Part 2: Analytics API Updates

### Current State
- Analytics API stores computed metrics only (WordMetric, PhraseMetric)
- Raw product metadata is NOT stored in the database (only in JSONL files)
- UnifiedProductDto has only 11 basic fields

### Required Changes

#### 1. Expand Database Schema

**New Table: ProductMetadata** (stores all raw product data)
```sql
CREATE TABLE ProductMetadata (
    Id INTEGER PRIMARY KEY,
    DatasetId INTEGER NOT NULL,
    ProductId TEXT NOT NULL,
    Platform TEXT NOT NULL,
    
    -- Basic Info
    Title TEXT,
    Description TEXT,
    ShortDescription TEXT,
    RawDescription TEXT,
    Price REAL,
    OriginalPrice REAL,
    Currency TEXT DEFAULT 'USD',
    
    -- Ratings
    Rating REAL,
    ReviewCount INTEGER,
    SoldCount INTEGER,
    
    -- Product Attributes
    Brand TEXT,
    PrimaryColor TEXT,
    Material TEXT,
    Dimensions TEXT,
    Weight REAL,
    Condition TEXT,
    
    -- Seller
    Seller TEXT,
    SellerId TEXT,
    SellerRating REAL,
    SellerFeedbackCount INTEGER,
    
    -- Inventory
    InStock INTEGER DEFAULT 1,
    StockQuantity INTEGER,
    Availability TEXT,
    
    -- URLs and IDs
    Url TEXT,
    Sku TEXT,
    Gtin TEXT,
    Upc TEXT,
    Ean TEXT,
    Mpn TEXT,
    ParentSku TEXT,
    
    -- SEO Metadata (stored as JSON)
    MetaTitle TEXT,
    MetaDescription TEXT,
    MetaKeywords TEXT,
    OpenGraphTagsJson TEXT,  -- JSON string
    TwitterCardTagsJson TEXT,  -- JSON string
    CanonicalUrl TEXT,
    H1TagsJson TEXT,  -- JSON array
    H2TagsJson TEXT,
    H3TagsJson TEXT,
    
    -- Images (stored as JSON arrays)
    ImagesJson TEXT,
    ThumbnailsJson TEXT,
    ColorsJson TEXT,
    SizesJson TEXT,
    
    -- Variants
    VariantPricesJson TEXT,  -- JSON dict
    
    -- Categories
    CategoriesJson TEXT,  -- JSON array
    ParentCategory TEXT,
    BreadcrumbsJson TEXT,  -- JSON array
    
    -- Related Products
    RelatedProductUrlsJson TEXT,
    FrequentlyBoughtTogetherJson TEXT,
    CustomersAlsoBoughtJson TEXT,
    SimilarProductsJson TEXT,
    CrossSellProductIdsJson TEXT,
    
    -- Unstructured Data
    JsonLdDataJson TEXT,  -- JSON object
    MicrodataJson TEXT,
    AllTextContent TEXT,
    ExtractedEmailsJson TEXT,
    ExtractedPhonesJson TEXT,
    ExtractedUrlsJson TEXT,
    
    -- Analytics Hints
    ViewCount INTEGER,
    FavoriteCount INTEGER,
    ShareCount INTEGER,
    PopularityScore TEXT,
    WordFrequencyJson TEXT,  -- JSON dict
    
    -- Additional Metadata
    ListingDate TEXT,  -- ISO date
    LastUpdated TEXT,  -- ISO date
    Language TEXT DEFAULT 'en',
    Locale TEXT DEFAULT 'en-US',
    CustomFieldsJson TEXT,  -- JSON dict
    AccoladesJson TEXT,  -- JSON array
    ReturnPolicy TEXT,
    WarrantyInfo TEXT,
    PlatformSpecificDataJson TEXT,  -- JSON dict
    
    -- Shipping
    ShippingOptionsJson TEXT,  -- JSON array
    
    -- Tracking
    ScrapedAt TEXT,  -- ISO date
    RawHtmlSnippet TEXT,
    
    FOREIGN KEY (DatasetId) REFERENCES ProductDatasets(Id)
);
```

#### 2. Update UnifiedProductDto
Expand to include ALL ProductData fields for JSONL ingestion.

#### 3. Update DataIngestController
- Modify `IngestProducts` to parse and store ALL fields
- Store data in ProductMetadata table (not just JSONL files)

#### 4. Update Query Endpoints
- Add filtering by any metadata field
- Add endpoints to retrieve full product details
- Support full-text search on descriptions, titles, tags

---

## Implementation Order

1. **Crawler Updates** (if needed beyond current state)
   - Verify VariantPrices extraction works in all crawlers
   - Add any missing field extractions identified above

2. **Analytics API Schema Update**
   - Create new ProductMetadata model
   - Update AnalyticsDbContext with new entity
   - Add EF Core migrations

3. **Ingestion Updates**
   - Update UnifiedProductDto with all fields
   - Update DataIngestController to map all fields
   - Update DatasetProcessor to process all fields

4. **Query Updates**
   - Add new query endpoints for metadata
   - Update existing endpoints to include metadata
   - Add filtering capabilities

---

## Files to Modify

### Crawlers (if needed)
- `src/PrintifyGenerator.Crawlers/Platforms/AmazonCrawler.cs`
- `src/PrintifyGenerator.Crawlers/Platforms/EtsyCrawler.cs`
- `src/PrintifyGenerator.Crawlers/Platforms/EbayCrawler.cs`
- `src/PrintifyGenerator.Crawlers/Platforms/WalmartCrawler.cs`
- `src/PrintifyGenerator.Crawlers/Platforms/AliExpressCrawler.cs`

### Analytics API
- `src/PrintifyGenerator.AnalyticsApi/Models/AnalyticsModels.cs`
- `src/PrintifyGenerator.AnalyticsApi/Data/AnalyticsDbContext.cs`
- `src/PrintifyGenerator.AnalyticsApi/Controllers/DataIngestController.cs`
- `src/PrintifyGenerator.AnalyticsApi/Controllers/AnalyticsController.cs`
- `src/PrintifyGenerator.AnalyticsApi/Services/DatasetProcessor.cs`
