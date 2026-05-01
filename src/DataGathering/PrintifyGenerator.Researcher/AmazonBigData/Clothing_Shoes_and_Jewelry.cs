using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class AmazonReview
{
    // Occurs 66033346
    [JsonPropertyName("rating")]
    public string rating { get; set; }

    // Occurs 66033346
    [JsonPropertyName("title")]
    public string title { get; set; }

    // Occurs 66033346
    [JsonPropertyName("text")]
    public string text { get; set; }

    // Occurs 66033346
    [JsonPropertyName("images")]
    public string images { get; set; }

    // Occurs 66033346
    [JsonPropertyName("asin")]
    public string asin { get; set; }

    // Occurs 66033346
    [JsonPropertyName("parent_asin")]
    public string parentasin { get; set; }

    // Occurs 66033346
    [JsonPropertyName("user_id")]
    public string userid { get; set; }

    // Occurs 66033346
    [JsonPropertyName("timestamp")]
    public string timestamp { get; set; }

    // Occurs 66033346
    [JsonPropertyName("helpful_vote")]
    public string helpfulvote { get; set; }

    // Occurs 66033346
    [JsonPropertyName("verified_purchase")]
    public string verifiedpurchase { get; set; }

}
public class ReviewImages
{
    // Occurs 6446363
    [JsonPropertyName("small_image_url")]
    public string smallimageurl { get; set; }

    // Occurs 6446363
    [JsonPropertyName("medium_image_url")]
    public string mediumimageurl { get; set; }

    // Occurs 6446363
    [JsonPropertyName("large_image_url")]
    public string largeimageurl { get; set; }

    // Occurs 6446363
    [JsonPropertyName("attachment_type")]
    public string attachmenttype { get; set; }

}
