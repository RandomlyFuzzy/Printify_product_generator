using System.Collections.Generic;
using System.Text.Json.Serialization;

public sealed class ListingContentContext
{
    public string JobId { get; set; } = "";
    public string ImagePath { get; set; } = "";
    public int BlueprintId { get; set; }
    public string BlueprintTitle { get; set; } = "";
    public int PrintProviderId { get; set; }
    public string PrintProviderTitle { get; set; } = "";
    public string LlmReason { get; set; } = "";
}

public sealed class ListingLookupIdentity
{
    [JsonPropertyName("lookup_key")] public string LookupKey { get; set; } = "";
    [JsonPropertyName("group_key")] public string GroupKey { get; set; } = "";
    [JsonPropertyName("asset_key")] public string AssetKey { get; set; } = "";
    [JsonPropertyName("reference_code")] public string ReferenceCode { get; set; } = "";
    [JsonPropertyName("job_id")] public string JobId { get; set; } = "";
    [JsonPropertyName("asset_token")] public string AssetToken { get; set; } = "";
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = new();
}

public sealed class ListingChannelContent
{
    [JsonPropertyName("title")] public string Title { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = new();
}

public sealed class ListingContentBundle
{
    [JsonPropertyName("lookup")] public ListingLookupIdentity Lookup { get; set; } = new();
    [JsonPropertyName("channels")] public Dictionary<string, ListingChannelContent> Channels { get; set; } = new();
}