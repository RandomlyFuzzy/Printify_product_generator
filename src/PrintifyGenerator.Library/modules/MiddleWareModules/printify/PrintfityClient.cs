using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

/// <summary>
/// Printify V1 API client.
/// Docs: https://developers.printify.com/#orders
/// Base URL: https://api.printify.com/v1/
/// Auth: Bearer token via personal access token or OAuth 2.0.
/// Rate limit: 600 req/min global, 100 req/min catalog.
/// </summary>
public class PrintifyClient
{
    private const string BaseUrl = "https://api.printify.com/v1";
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public PrintifyClient(string apiToken)
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
        _http.DefaultRequestHeaders.Add("User-Agent", "PrintifyGenerator/1.0");
    }

    // ─── Shops ─────────────────────────────────────────────────────

    public async Task<List<Shop>> GetShopsAsync()
    {
        return await GetAsync<List<Shop>>("/shops.json");
    }

    public async Task DisconnectShopAsync(int shopId)
    {
        await DeleteAsync($"/shops/{shopId}/connection.json");
    }

    // ─── Catalog – Blueprints ──────────────────────────────────────

    public async Task<List<Blueprint>> GetBlueprintsAsync()
    {
        return await GetAsync<List<Blueprint>>("/catalog/blueprints.json");
    }

    public async Task<Blueprint> GetBlueprintAsync(int blueprintId)
    {
        return await GetAsync<Blueprint>($"/catalog/blueprints/{blueprintId}.json");
    }

    public async Task<List<BlueprintPrintProvider>> GetBlueprintPrintProvidersAsync(int blueprintId)
    {
        return await GetAsync<List<BlueprintPrintProvider>>($"/catalog/blueprints/{blueprintId}/print_providers.json");
    }

    public async Task<VariantResponse> GetBlueprintVariantsAsync(int blueprintId, int printProviderId, bool showOutOfStock = false)
    {
        var qs = showOutOfStock ? "?show-out-of-stock=1" : "";
        return await GetAsync<VariantResponse>($"/catalog/blueprints/{blueprintId}/print_providers/{printProviderId}/variants.json{qs}");
    }

    public async Task<ShippingInfo> GetBlueprintShippingAsync(int blueprintId, int printProviderId)
    {
        return await GetAsync<ShippingInfo>($"/catalog/blueprints/{blueprintId}/print_providers/{printProviderId}/shipping.json");
    }

    // ─── Catalog – Print Providers ─────────────────────────────────

    public async Task<List<PrintProvider>> GetPrintProvidersAsync()
    {
        return await GetAsync<List<PrintProvider>>("/catalog/print_providers.json");
    }

    public async Task<PrintProvider> GetPrintProviderAsync(int printProviderId)
    {
        return await GetAsync<PrintProvider>($"/catalog/print_providers/{printProviderId}.json");
    }

    // ─── Products ──────────────────────────────────────────────────

    public async Task<PaginatedResponse<Product>> GetProductsAsync(int shopId, int page = 1, int limit = 10)
    {
        return await GetAsync<PaginatedResponse<Product>>($"/shops/{shopId}/products.json?page={page}&limit={limit}");
    }

    public async Task<List<Product>> GetAllProductsAsync(int shopId)
    {
        return await GetAllPagesAsync<Product>($"/shops/{shopId}/products.json");
    }

    public async Task<Product> GetProductAsync(int shopId, string productId)
    {
        return await GetAsync<Product>($"/shops/{shopId}/products/{productId}.json");
    }

    public async Task<Product> CreateProductAsync(int shopId, CreateProductRequest request)
    {
        return await PostAsync<Product>($"/shops/{shopId}/products.json", request);
    }

    public async Task<Product> UpdateProductAsync(int shopId, string productId, UpdateProductRequest request)
    {
        return await PutAsync<Product>($"/shops/{shopId}/products/{productId}.json", request);
    }

    public async Task DeleteProductAsync(int shopId, string productId)
    {
        await DeleteAsync($"/shops/{shopId}/products/{productId}.json");
    }

    public async Task PublishProductAsync(int shopId, string productId, PublishProductRequest request)
    {
        await PostAsync($"/shops/{shopId}/products/{productId}/publish.json", request);
    }

    public async Task PublishSucceededAsync(int shopId, string productId, PublishSucceededRequest request)
    {
        await PostAsync($"/shops/{shopId}/products/{productId}/publishing_succeeded.json", request);
    }

    public async Task PublishFailedAsync(int shopId, string productId, PublishFailedRequest request)
    {
        await PostAsync($"/shops/{shopId}/products/{productId}/publishing_failed.json", request);
    }

    public async Task UnpublishProductAsync(int shopId, string productId)
    {
        await PostAsync($"/shops/{shopId}/products/{productId}/unpublish.json", null);
    }

    // ─── Orders ────────────────────────────────────────────────────

    public async Task<PaginatedResponse<Order>> GetOrdersAsync(int shopId, int page = 1, int limit = 10, string? status = null, string? sku = null)
    {
        var url = $"/shops/{shopId}/orders.json?page={page}&limit={limit}";
        if (!string.IsNullOrEmpty(status)) url += $"&status={Uri.EscapeDataString(status)}";
        if (!string.IsNullOrEmpty(sku)) url += $"&sku={Uri.EscapeDataString(sku)}";
        return await GetAsync<PaginatedResponse<Order>>(url);
    }

    public async Task<List<Order>> GetAllOrdersAsync(int shopId, string? status = null, string? sku = null)
    {
        var url = $"/shops/{shopId}/orders.json?limit=10";
        if (!string.IsNullOrEmpty(status)) url += $"&status={Uri.EscapeDataString(status)}";
        if (!string.IsNullOrEmpty(sku)) url += $"&sku={Uri.EscapeDataString(sku)}";
        return await GetAllPagesAsync<Order>(url);
    }

    public async Task<Order> GetOrderAsync(int shopId, string orderId)
    {
        return await GetAsync<Order>($"/shops/{shopId}/orders/{orderId}.json");
    }

    public async Task<Order> SubmitOrderAsync(int shopId, SubmitOrderRequest request)
    {
        return await PostAsync<Order>($"/shops/{shopId}/orders.json", request);
    }

    public async Task<List<Order>> SubmitExpressOrderAsync(int shopId, SubmitOrderRequest request)
    {
        return await PostAsync<List<Order>>($"/shops/{shopId}/orders/express.json", request);
    }

    public async Task SendOrderToProductionAsync(int shopId, string orderId)
    {
        await PostAsync($"/shops/{shopId}/orders/{orderId}/send_to_production.json", null);
    }

    public async Task<ShippingCostResponse> CalculateShippingAsync(int shopId, ShippingCostRequest request)
    {
        return await PostAsync<ShippingCostResponse>($"/shops/{shopId}/orders/shipping.json", request);
    }

    public async Task CancelOrderAsync(int shopId, string orderId)
    {
        await PostAsync($"/shops/{shopId}/orders/{orderId}/cancel.json", null);
    }

    // ─── Uploads ───────────────────────────────────────────────────

    public async Task<PaginatedResponse<UploadedImage>> GetUploadsAsync(int page = 1, int limit = 10)
    {
        return await GetAsync<PaginatedResponse<UploadedImage>>($"/uploads.json?page={page}&limit={limit}");
    }

    public async Task<List<UploadedImage>> GetAllUploadsAsync()
    {
        return await GetAllPagesAsync<UploadedImage>("/uploads.json?limit=50");
    }

    public async Task<UploadedImage> GetUploadAsync(string imageId)
    {
        return await GetAsync<UploadedImage>($"/uploads/{imageId}.json");
    }

    public async Task<UploadedImage> UploadImageByUrlAsync(string fileName, string imageUrl)
    {
        return await PostAsync<UploadedImage>("/uploads/images.json", new UploadImageByUrlRequest
        {
            FileName = fileName,
            Url = imageUrl
        });
    }

    public async Task<UploadedImage> UploadImageByBase64Async(string fileName, string base64Contents)
    {
        return await PostAsync<UploadedImage>("/uploads/images.json", new UploadImageByBase64Request
        {
            FileName = fileName,
            Contents = base64Contents
        });
    }

    public async Task<UploadedImage> UploadImageFromFileAsync(string filePath)
    {
        var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
        var base64 = Convert.ToBase64String(bytes);
        var fileName = System.IO.Path.GetFileName(filePath);
        return await UploadImageByBase64Async(fileName, base64);
    }

    public async Task ArchiveUploadAsync(string imageId)
    {
        await PostAsync($"/uploads/{imageId}/archive.json", null);
    }

    // ─── Webhooks ──────────────────────────────────────────────────

    public async Task<List<Webhook>> GetWebhooksAsync(int shopId)
    {
        return await GetAsync<List<Webhook>>($"/shops/{shopId}/webhooks.json");
    }

    public async Task<Webhook> CreateWebhookAsync(int shopId, CreateWebhookRequest request)
    {
        return await PostAsync<Webhook>($"/shops/{shopId}/webhooks.json", request);
    }

    public async Task<Webhook> UpdateWebhookAsync(int shopId, string webhookId, UpdateWebhookRequest request)
    {
        return await PutAsync<Webhook>($"/shops/{shopId}/webhooks/{webhookId}.json", request);
    }

    public async Task DeleteWebhookAsync(int shopId, string webhookId, string host)
    {
        await DeleteAsync($"/shops/{shopId}/webhooks/{webhookId}.json?host={Uri.EscapeDataString(host)}");
    }

    public async Task SimulateWebhookAsync(int shopId, string webhookId, object payload)
    {
        await PostAsync($"/shops/{shopId}/webhooks/{webhookId}/simulate", payload);
    }

    // ─── HTTP Helpers ──────────────────────────────────────────────

    private async Task<T> GetAsync<T>(string path)
    {
        var response = await _http.GetAsync($"{BaseUrl}{path}");
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOpts)!;
    }

    private async Task<T> PostAsync<T>(string path, object? body)
    {
        var content = body != null
            ? new StringContent(JsonSerializer.Serialize(body, JsonOpts), Encoding.UTF8, "application/json")
            : null;
        var response = await _http.PostAsync($"{BaseUrl}{path}", content);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOpts)!;
    }

    private async Task PostAsync(string path, object? body)
    {
        var content = body != null
            ? new StringContent(JsonSerializer.Serialize(body, JsonOpts), Encoding.UTF8, "application/json")
            : null;
        var response = await _http.PostAsync($"{BaseUrl}{path}", content);
        await EnsureSuccessAsync(response);
    }

    private async Task<T> PutAsync<T>(string path, object body)
    {
        var content = new StringContent(JsonSerializer.Serialize(body, JsonOpts), Encoding.UTF8, "application/json");
        var response = await _http.PutAsync($"{BaseUrl}{path}", content);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOpts)!;
    }

    private async Task DeleteAsync(string path)
    {
        var response = await _http.DeleteAsync($"{BaseUrl}{path}");
        await EnsureSuccessAsync(response);
    }

    private async Task<List<T>> GetAllPagesAsync<T>(string firstPagePath)
    {
        var all = new List<T>();
        string? url = $"{BaseUrl}{firstPagePath}";
        int pagenum = 1;
        int limit = 50;
        List<T>? pageData = new List<T>();

        do
        {
            string pageurl = url + (url.Contains("?") ? $"&page={pagenum}&limit={limit}" : $"?page={pagenum}&limit={limit}");
            var response = await _http.GetAsync(pageurl);
            await EnsureSuccessAsync(response);
            var json = await response.Content.ReadAsStringAsync();
            var page = JsonSerializer.Deserialize<PaginatedResponse<T>>(json, JsonOpts)!;
            pageData = page.Data;
            all.AddRange(pageData);
            pagenum++;

            url = page.NextPageUrl;
            if (url != null && !url.StartsWith("http"))
                url = $"{BaseUrl}{url}";
        }
        while (url != null&&pageData.Count != limit);

        return all;
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new PrintifyApiException((int)response.StatusCode, body);
        }
    }
}

public class PrintifyApiException : Exception
{
    public int StatusCode { get; }
    public string ResponseBody { get; }

    public PrintifyApiException(int statusCode, string responseBody)
        : base($"Printify API error {statusCode}: {responseBody}")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
