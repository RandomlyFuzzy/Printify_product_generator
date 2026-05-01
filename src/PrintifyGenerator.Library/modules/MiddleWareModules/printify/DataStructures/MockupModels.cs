using System.Collections.Generic;
using System.Text.Json.Serialization;

// ── Image staging lookup ───────────────────────────────────────────

/// <summary>
/// Persisted record mapping a local image (by SHA-256 hash) to its
/// already-uploaded Printify image ID.  Stored in
/// src/data/staging/lookup.json.
/// </summary>
public class ImageLookupEntry
{
    [JsonPropertyName("hash")]               public string Hash               { get; set; } = "";
    [JsonPropertyName("local_path")]         public string LocalPath          { get; set; } = "";
    [JsonPropertyName("printify_image_id")]  public string PrintifyImageId    { get; set; } = "";
    [JsonPropertyName("file_name")]          public string FileName           { get; set; } = "";
    [JsonPropertyName("preview_url")]        public string PreviewUrl         { get; set; } = "";
    [JsonPropertyName("uploaded_at")]        public string UploadedAt         { get; set; } = "";
}

// ── LLM blueprint suggestion ───────────────────────────────────────

/// <summary>
/// The JSON structure the LLM is asked to return when picking a blueprint.
/// </summary>
public class BlueprintSuggestion
{
    [JsonPropertyName("id")]    public int    BlueprintId    { get; set; }
    [JsonPropertyName("title")] public string BlueprintTitle { get; set; } = "";
    [JsonPropertyName("reason")]          public string Reason         { get; set; } = "";
}

// ── Draft record ───────────────────────────────────────────────────

/// <summary>
/// Persisted record for a draft product created on Printify.  Written to
/// src/data/staging/drafts/{productId}.json for later inspection.
/// </summary>
public class MockupDraftRecord
{
    [JsonPropertyName("product_id")]                public string       ProductId               { get; set; } = "";
    [JsonPropertyName("job_id")]                    public string       JobId                   { get; set; } = "";
    [JsonPropertyName("lookup_key")]                public string       LookupKey               { get; set; } = "";
    [JsonPropertyName("group_key")]                 public string       GroupKey                { get; set; } = "";
    [JsonPropertyName("asset_key")]                 public string       AssetKey                { get; set; } = "";
    [JsonPropertyName("reference_code")]            public string       ReferenceCode           { get; set; } = "";
    [JsonPropertyName("local_image_path")]          public string       LocalImagePath          { get; set; } = "";
    [JsonPropertyName("printify_image_id")]         public string       PrintifyImageId         { get; set; } = "";
    [JsonPropertyName("printify_image_preview_url")]public string       PrintifyImagePreviewUrl { get; set; } = "";
    [JsonPropertyName("blueprint_id")]              public int          BlueprintId             { get; set; }
    [JsonPropertyName("blueprint_title")]           public string       BlueprintTitle          { get; set; } = "";
    [JsonPropertyName("llm_reason")]                public string       LlmReason               { get; set; } = "";
    [JsonPropertyName("print_provider_id")]         public int          PrintProviderId         { get; set; }
    [JsonPropertyName("print_provider_title")]      public string       PrintProviderTitle      { get; set; } = "";
    [JsonPropertyName("lookup_tags")]               public List<string> LookupTags              { get; set; } = new();
    [JsonPropertyName("channel_content")]           public Dictionary<string, ListingChannelContent> ChannelContent { get; set; } = new();
    [JsonPropertyName("created_at")]                public string       CreatedAt               { get; set; } = "";
    [JsonPropertyName("mockup_urls")]               public List<string> MockupUrls              { get; set; } = new();
}

// ── Blueprint catalogue entry (for LLM prompts) ───────────────────

/// <summary>
/// Compact representation of a blueprint sent to the LLM, including
/// available print locations and their dimensions.
/// </summary>
public class BlueprintCatalogueEntry
{
    [JsonPropertyName("id")]              public int                                      Id             { get; set; }
    [JsonPropertyName("title")]           public string                                   Title          { get; set; } = "";
    [JsonPropertyName("printLocations")]  public List<string>                             PrintLocations { get; set; } = new();
    [JsonPropertyName("locations")]       public Dictionary<string, PrintLocationSize>?   Locations      { get; set; }
}

public class PrintLocationSize
{
    [JsonPropertyName("size_x")] public int SizeX { get; set; }
    [JsonPropertyName("size_y")] public int SizeY { get; set; }
}

// ── Market signals (from category-feature intelligence) ───────────

/// <summary>
/// Signals derived from category feature data that guide blueprint and
/// variant selection toward higher-selling combinations.
/// </summary>
public class MarketSignals
{
    /// <summary>Colour names that sell well for this product type, ordered by score.</summary>
    public string[] RecommendedColors { get; init; } = Array.Empty<string>();

    /// <summary>High-intent keywords that should influence blueprint category matching.</summary>
    public string[] CategoryKeywords { get; init; } = Array.Empty<string>();

    /// <summary>Primary colour from the product definition (single strong signal).</summary>
    public string PrimaryColor { get; init; } = string.Empty;
}

// ── Processing result ──────────────────────────────────────────────

/// <summary>
/// Returned by <see cref="MockupGenerator.ProcessImageAsync"/>.
/// </summary>
public class MockupResult
{
    [JsonPropertyName("success")]           public bool              Success         { get; set; }
    [JsonPropertyName("printify_image_id")] public string?           PrintifyImageId { get; set; }
    [JsonPropertyName("draft")]             public MockupDraftRecord? Draft          { get; set; }
    [JsonPropertyName("error")]             public string?           Error           { get; set; }

    public static MockupResult Fail(string error) =>
        new() { Success = false, Error = error };
}
