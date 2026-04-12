using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Web;

/// <summary>
/// Manages eBay OAuth 2.0 tokens for both Application (client credentials) and
/// User (authorization code) grant flows, including automatic refresh.
///
/// Security: credentials are held in memory only — never log or persist them.
/// </summary>
public class EbayOAuthClient
{
    private readonly EbayConfig _config;
    private readonly HttpClient _http;

    // Cached application token (client credentials grant)
    private EbayTokenResponse? _appToken;
    // Cached user token (authorization code grant)
    private EbayTokenResponse? _userToken;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public EbayOAuthClient(EbayConfig config, HttpClient? http = null)
    {
        _config = config;
        _http = http ?? new HttpClient();
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Application Token – Client Credentials Grant
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a valid Application access token, using the cached one if still fresh.
    /// Used for calls that don't require a specific eBay user's identity.
    /// </summary>
    public async Task<string> GetApplicationTokenAsync(
        string[]? scopes = null,
        CancellationToken ct = default)
    {
        if (_appToken != null && !_appToken.IsAccessTokenExpired)
            return _appToken.AccessToken;

        var targetScopes = scopes ?? ["https://api.ebay.com/oauth/api_scope"];
        var scopeString = Uri.EscapeDataString(string.Join(" ", targetScopes));

        var body = $"grant_type=client_credentials&scope={scopeString}";

        var response = await SendTokenRequestAsync(body, ct);
        // Reserve 60 s buffer so we refresh slightly before hard expiry
        response.ExpiresAt = DateTime.UtcNow.AddSeconds(response.ExpiresIn - 60);
        _appToken = response;

        return _appToken.AccessToken;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // User Token – Authorization Code Grant (two-step)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds the URL to which you redirect the eBay user to grant consent.
    /// After consenting, eBay redirects back to your RuName Accept URL with
    /// a <c>code</c> query parameter you pass to <see cref="ExchangeCodeForTokenAsync"/>.
    /// </summary>
    public string BuildUserConsentUrl(string[] scopes, string? state = null)
    {
        if (string.IsNullOrWhiteSpace(_config.RuName))
            throw new InvalidOperationException("EbayConfig.RuName must be set to use the authorization code flow.");

        var scopeString = Uri.EscapeDataString(string.Join(" ", scopes));
        var url = $"{_config.AuthUrl}/oauth2/authorize" +
                  $"?client_id={Uri.EscapeDataString(_config.ClientId)}" +
                  $"&redirect_uri={Uri.EscapeDataString(_config.RuName)}" +
                  "&response_type=code" +
                  $"&scope={scopeString}";

        if (!string.IsNullOrWhiteSpace(state))
            url += $"&state={Uri.EscapeDataString(state)}";

        return url;
    }

    /// <summary>
    /// Exchanges a one-time authorization code (returned from the consent redirect)
    /// for a User access token + refresh token pair.
    /// </summary>
    public async Task<EbayTokenResponse> ExchangeCodeForTokenAsync(
        string authorizationCode,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_config.RuName))
            throw new InvalidOperationException("EbayConfig.RuName must be set.");

        // The auth code returned by eBay is URL-encoded; decode before embedding it.
        var decodedCode = HttpUtility.UrlDecode(authorizationCode);
        var encodedCode = Uri.EscapeDataString(decodedCode);

        var body = $"grant_type=authorization_code" +
                   $"&code={encodedCode}" +
                   $"&redirect_uri={Uri.EscapeDataString(_config.RuName)}";

        var response = await SendTokenRequestAsync(body, ct);
        response.ExpiresAt = DateTime.UtcNow.AddSeconds(response.ExpiresIn - 60);
        if (response.RefreshTokenExpiresIn.HasValue)
            response.RefreshTokenExpiresAt = DateTime.UtcNow.AddSeconds(response.RefreshTokenExpiresIn.Value - 60);

        _userToken = response;
        return response;
    }

    /// <summary>
    /// Uses the stored refresh token to mint a new User access token.
    /// Called automatically by <see cref="GetUserTokenAsync"/> when the token is expired.
    /// </summary>
    public async Task<EbayTokenResponse> RefreshUserTokenAsync(
        string refreshToken,
        string[]? scopes = null,
        CancellationToken ct = default)
    {
        var body = $"grant_type=refresh_token" +
                   $"&refresh_token={Uri.EscapeDataString(refreshToken)}";

        if (scopes is { Length: > 0 })
            body += $"&scope={Uri.EscapeDataString(string.Join(" ", scopes))}";

        var response = await SendTokenRequestAsync(body, ct);
        response.ExpiresAt = DateTime.UtcNow.AddSeconds(response.ExpiresIn - 60);

        // Preserve the existing refresh token if a new one isn't returned
        if (string.IsNullOrWhiteSpace(response.RefreshToken))
            response.RefreshToken = refreshToken;

        if (_userToken != null)
            response.RefreshTokenExpiresAt = _userToken.RefreshTokenExpiresAt;

        _userToken = response;
        return response;
    }

    /// <summary>
    /// Returns a valid User access token, automatically refreshing with the stored
    /// refresh token if the access token has expired.
    /// Throws <see cref="InvalidOperationException"/> if no token has been acquired.
    /// </summary>
    public async Task<string> GetUserTokenAsync(
        string[]? scopes = null,
        CancellationToken ct = default)
    {
        if (_userToken == null)
            throw new InvalidOperationException(
                "No user token available. Obtain one via ExchangeCodeForTokenAsync first.");

        if (!_userToken.IsAccessTokenExpired)
            return _userToken.AccessToken;

        if (string.IsNullOrWhiteSpace(_userToken.RefreshToken) || _userToken.IsRefreshTokenExpired)
            throw new InvalidOperationException(
                "User access token is expired and the refresh token is missing or expired. " +
                "Re-initiate the authorization code flow to get a new token.");

        await RefreshUserTokenAsync(_userToken.RefreshToken, scopes, ct);
        return _userToken!.AccessToken;
    }

    /// <summary>
    /// Persists a previously acquired token (e.g. loaded from secure storage).
    /// </summary>
    public void SetUserToken(EbayTokenResponse token) => _userToken = token;

    /// <summary>Returns the currently held user token (may be null).</summary>
    public EbayTokenResponse? GetCurrentUserToken() => _userToken;

    // ──────────────────────────────────────────────────────────────────────────
    // Internal helpers
    // ──────────────────────────────────────────────────────────────────────────

    private async Task<EbayTokenResponse> SendTokenRequestAsync(
        string formBody,
        CancellationToken ct)
    {
        var tokenUrl = $"{_config.BaseUrl}/identity/v1/oauth2/token";

        var request = new HttpRequestMessage(HttpMethod.Post, tokenUrl)
        {
            Content = new StringContent(formBody, Encoding.UTF8, "application/x-www-form-urlencoded")
        };

        // Authorization: Basic <Base64(clientId:clientSecret)>
        var credentials = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{_config.ClientId}:{_config.ClientSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var httpResponse = await _http.SendAsync(request, ct);
        var responseBody = await httpResponse.Content.ReadAsStringAsync(ct);

        if (!httpResponse.IsSuccessStatusCode)
        {
            throw new EbayApiException(
                (int)httpResponse.StatusCode,
                $"eBay OAuth token request failed ({(int)httpResponse.StatusCode}): {responseBody}");
        }

        var token = JsonSerializer.Deserialize<EbayTokenResponse>(responseBody, _jsonOptions)
            ?? throw new InvalidOperationException("Unexpected null token response from eBay.");

        return token;
    }
}
