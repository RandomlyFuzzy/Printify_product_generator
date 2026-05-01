using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

public sealed class AnalyticsApiClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly bool _disposeHttpClient;

    public AnalyticsApiClient(string baseUrl)
        : this(new HttpClient(), baseUrl, disposeHttpClient: true)
    {
    }

    public AnalyticsApiClient(HttpClient httpClient, string? baseUrl = null)
        : this(httpClient, baseUrl, disposeHttpClient: false)
    {
    }

    private AnalyticsApiClient(HttpClient httpClient, string? baseUrl, bool disposeHttpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _disposeHttpClient = disposeHttpClient;

        if (!string.IsNullOrWhiteSpace(baseUrl))
        {
            _httpClient.BaseAddress = CreateBaseUri(baseUrl);
        }
        else if (_httpClient.BaseAddress is null)
        {
            throw new ArgumentException("A base URL is required when the HttpClient does not already have BaseAddress set.", nameof(baseUrl));
        }
    }

    public async Task<AnalyticsApiServiceStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        return await GetRequiredAsync<AnalyticsApiServiceStatus>("/", cancellationToken);
    }

    public async Task<IReadOnlyList<PhaseBundleSummary>> ListBundlesAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        return await GetRequiredAsync<IReadOnlyList<PhaseBundleSummary>>($"/api/phases/bundles?limit={limit}", cancellationToken);
    }

    public async Task<IReadOnlyList<PhaseOverviewItem>> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        return await GetRequiredAsync<IReadOnlyList<PhaseOverviewItem>>("/api/phases/overview", cancellationToken);
    }

    public async Task<PhaseBundleSummary?> GetBundleAsync(Guid bundleId, CancellationToken cancellationToken = default)
    {
        return await GetOptionalAsync<PhaseBundleSummary>($"/api/phases/bundles/{bundleId}", cancellationToken);
    }

    public async Task<IReadOnlyList<PhaseArtifactReference>> GetBundlePhaseArtifactsAsync(Guid bundleId, string phase, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phase);
        return await GetRequiredAsync<IReadOnlyList<PhaseArtifactReference>>($"/api/phases/bundles/{bundleId}/phase/{Uri.EscapeDataString(phase)}", cancellationToken);
    }

    public async Task<IReadOnlyList<PhaseArtifactReference>> GetLatestPhaseArtifactsAsync(string phase, int limit = 200, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phase);
        return await GetRequiredAsync<IReadOnlyList<PhaseArtifactReference>>($"/api/phases/artifacts/{Uri.EscapeDataString(phase)}?limit={limit}", cancellationToken);
    }

    public async Task<PhaseArtifactResult?> GetArtifactAsync(Guid bundleId, string fileName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        return await GetOptionalAsync<PhaseArtifactResult>($"/api/phases/bundles/{bundleId}/artifacts/{Uri.EscapeDataString(fileName)}", cancellationToken);
    }

    public async Task<IngestResult> IngestProductDefinitionAsync(IngestProductDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await PostAsync<IngestProductDefinitionRequest, IngestResult>("/api/phases/ingest/product-definition", request, cancellationToken);
    }

    public async Task<IngestResult> IngestPhaseArtifactAsync(IngestPhaseDataRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await PostAsync<IngestPhaseDataRequest, IngestResult>("/api/phases/ingest/artifact", request, cancellationToken);
    }

    public async Task<MarketSummaryResponse> GetMarketSummaryAsync(int top = 20, CancellationToken cancellationToken = default)
    {
        return await GetRequiredAsync<MarketSummaryResponse>($"/api/market/summary?top={top}", cancellationToken);
    }

    public async Task<IReadOnlyList<CategoryListItem>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await GetRequiredAsync<IReadOnlyList<CategoryListItem>>("/api/market/categories", cancellationToken);
    }

    public async Task<CategoryDetailResponse?> GetCategoryAsync(string name, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return await GetOptionalAsync<CategoryDetailResponse>($"/api/market/categories/{Uri.EscapeDataString(name)}", cancellationToken);
    }

    public async Task<IReadOnlyList<CategoryDetailResponse>> SearchCategoriesAsync(string? type = null, int limit = 20, CancellationToken cancellationToken = default)
    {
        var query = new List<string> { $"limit={limit}" };
        if (!string.IsNullOrWhiteSpace(type))
        {
            query.Add($"type={Uri.EscapeDataString(type)}");
        }

        return await GetRequiredAsync<IReadOnlyList<CategoryDetailResponse>>($"/api/market/categories/search?{string.Join("&", query)}", cancellationToken);
    }

    public async Task<CrossCategoryOverlapResponse> GetOverlapAsync(int minCategories = 3, int top = 30, CancellationToken cancellationToken = default)
    {
        return await GetRequiredAsync<CrossCategoryOverlapResponse>($"/api/market/overlap?minCategories={minCategories}&top={top}", cancellationToken);
    }

    public async Task<ProductScoreResponse> ScoreProductAsync(ProductScoreRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await PostAsync<ProductScoreRequest, ProductScoreResponse>("/api/market/score", request, cancellationToken);
    }

    public async Task<FileIngestResult> IngestCategoryFeatureAsync(CategoryFeatureIngestRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        return await PostAsync<CategoryFeatureIngestRequest, FileIngestResult>("/api/market/ingest/category-feature", request, cancellationToken);
    }

    public void Dispose()
    {
        if (_disposeHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private async Task<T> GetRequiredAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(path, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await DeserializeAsync<T>(response, cancellationToken);
    }

    private async Task<T?> GetOptionalAsync<T>(string path, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(path, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        await EnsureSuccessAsync(response, cancellationToken);
        return await DeserializeAsync<T>(response, cancellationToken);
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(path, request, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await DeserializeAsync<TResponse>(response, cancellationToken);
    }

    private static async Task<T> DeserializeAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
        if (value is null)
        {
            throw new InvalidOperationException($"The response body for '{response.RequestMessage?.RequestUri}' was empty or could not be deserialized to {typeof(T).Name}.");
        }

        return value;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = response.Content is null
            ? string.Empty
            : await response.Content.ReadAsStringAsync(cancellationToken);

        throw new HttpRequestException(
            $"Request to '{response.RequestMessage?.RequestUri}' failed with status {(int)response.StatusCode} ({response.ReasonPhrase}). Body: {body}",
            null,
            response.StatusCode);
    }

    private static Uri CreateBaseUri(string baseUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseUrl);

        if (!Uri.TryCreate(baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/", UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("The Analytics API base URL must be an absolute URI.", nameof(baseUrl));
        }

        return uri;
    }
}