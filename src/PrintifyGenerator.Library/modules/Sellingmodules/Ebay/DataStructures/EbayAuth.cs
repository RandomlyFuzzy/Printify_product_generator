using System.Text.Json.Serialization;

/// <summary>Deserialized OAuth 2.0 token response from eBay's identity endpoint.</summary>
public class EbayTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("refresh_token_expires_in")]
    public int? RefreshTokenExpiresIn { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "";

    /// <summary>Absolute UTC time when the access token expires (set by the client after minting).</summary>
    public DateTime ExpiresAt { get; set; } = DateTime.MinValue;

    /// <summary>Absolute UTC time when the refresh token expires (set by the client after minting).</summary>
    public DateTime? RefreshTokenExpiresAt { get; set; }

    public bool IsAccessTokenExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRefreshTokenExpired => RefreshTokenExpiresAt.HasValue && DateTime.UtcNow >= RefreshTokenExpiresAt.Value;
}

/// <summary>eBay API error detail returned in error responses.</summary>
public class EbayError
{
    [JsonPropertyName("errorId")]
    public int ErrorId { get; set; }

    [JsonPropertyName("domain")]
    public string Domain { get; set; } = "";

    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

    [JsonPropertyName("message")]
    public string Message { get; set; } = "";

    [JsonPropertyName("longMessage")]
    public string? LongMessage { get; set; }

    [JsonPropertyName("parameters")]
    public List<EbayErrorParameter>? Parameters { get; set; }
}

public class EbayErrorParameter
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("value")]
    public string Value { get; set; } = "";
}

/// <summary>Wrapper for eBay API error response body.</summary>
public class EbayErrorResponse
{
    [JsonPropertyName("errors")]
    public List<EbayError>? Errors { get; set; }

    [JsonPropertyName("warnings")]
    public List<EbayError>? Warnings { get; set; }
}

/// <summary>Thrown when an eBay API call returns a non-success HTTP status.</summary>
public class EbayApiException : Exception
{
    public int StatusCode { get; }
    public List<EbayError> Errors { get; }

    public EbayApiException(int statusCode, string message, List<EbayError>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors ?? [];
    }
}
