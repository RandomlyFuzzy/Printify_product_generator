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

    public async Task<ProductTransferResult> TransferProductAsync(
        int sourceShopId,
        string sourceProductId,
        int targetShopId,
        bool deleteSourceProduct = false,
        bool publishTargetProduct = false,
        PublishProductRequest? publishRequest = null)
    {
        if (sourceShopId <= 0)
            throw new ArgumentOutOfRangeException(nameof(sourceShopId), "Source shop ID must be greater than zero.");
        if (targetShopId <= 0)
            throw new ArgumentOutOfRangeException(nameof(targetShopId), "Target shop ID must be greater than zero.");
        if (string.IsNullOrWhiteSpace(sourceProductId))
            throw new ArgumentException("Source product ID is required.", nameof(sourceProductId));

        var sourceProduct = await GetProductAsync(sourceShopId, sourceProductId);
        var createRequest = await BuildCreateProductRequestAsync(sourceProduct);
        var createdProduct = await CreateProductAsync(targetShopId, createRequest);
        var targetProductPublished = false;

        if (publishTargetProduct)
        {
            await PublishProductAsync(targetShopId, createdProduct.Id, publishRequest ?? new PublishProductRequest());
            targetProductPublished = true;
        }

        if (deleteSourceProduct)
        {
            try
            {
                await DeleteProductAsync(sourceShopId, sourceProduct.Id);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Product {sourceProduct.Id} was cloned to shop {targetShopId} as {createdProduct.Id}, but deleting the source product from shop {sourceShopId} failed.",
                    ex);
            }
        }

        return new ProductTransferResult
        {
            SourceShopId = sourceShopId,
            SourceProductId = sourceProduct.Id,
            TargetShopId = targetShopId,
            TargetProductId = createdProduct.Id,
            SourceProductDeleted = deleteSourceProduct,
            TargetProductPublished = targetProductPublished,
            TargetProduct = createdProduct
        };
    }

    public Task<ProductVariantPriceBreakdown> GetProductVariantPriceBreakdownAsync(
        int shopId,
        string productId,
        int variantId,
        Address addressTo,
        int quantity = 1)
    {
        return GetProductVariantPriceBreakdownAsync(shopId, new ProductVariantPriceBreakdownRequest
        {
            ProductId = productId,
            VariantId = variantId,
            Quantity = quantity,
            AddressTo = addressTo
        });
    }

    public async Task<ProductVariantPriceBreakdown> GetProductVariantPriceBreakdownAsync(
        int shopId,
        ProductVariantPriceBreakdownRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));
        if (request.AddressTo is null)
            throw new ArgumentNullException(nameof(request.AddressTo));
        if (shopId <= 0)
            throw new ArgumentOutOfRangeException(nameof(shopId), "Shop ID must be greater than zero.");
        if (string.IsNullOrWhiteSpace(request.ProductId))
            throw new ArgumentException("Product ID is required.", nameof(request));
        if (request.VariantId <= 0)
            throw new ArgumentOutOfRangeException(nameof(request), "Variant ID must be greater than zero.");
        if (request.Quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(request), "Quantity must be greater than zero.");
        if (string.IsNullOrWhiteSpace(request.AddressTo.Country))
            throw new ArgumentException("Destination country is required.", nameof(request));

        var product = await GetProductAsync(shopId, request.ProductId);
        var variant = product.Variants.Find(candidate => candidate.Id == request.VariantId);

        if (variant is null)
            throw new InvalidOperationException($"Variant {request.VariantId} was not found on product {request.ProductId}.");

        var shipping = await CalculateShippingAsync(shopId, new ShippingCostRequest
        {
            LineItems = new List<SubmitOrderLineItem>
            {
                new()
                {
                    ProductId = request.ProductId,
                    VariantId = request.VariantId,
                    Quantity = request.Quantity
                }
            },
            AddressTo = request.AddressTo
        });

        var totalRetailPrice = checked(variant.Price * request.Quantity);
        var totalProductionCost = checked(variant.Cost * request.Quantity);

        return new ProductVariantPriceBreakdown
        {
            ShopId = shopId,
            ProductId = product.Id,
            ProductTitle = product.Title,
            BlueprintId = product.BlueprintId,
            PrintProviderId = product.PrintProviderId,
            VariantId = variant.Id,
            VariantTitle = variant.Title,
            Sku = variant.Sku,
            Quantity = request.Quantity,
            IsEnabled = variant.IsEnabled,
            IsAvailable = variant.IsAvailable,
            DestinationCountry = request.AddressTo.Country,
            DestinationRegion = request.AddressTo.Region,
            DestinationZip = request.AddressTo.Zip,
            UnitRetailPrice = variant.Price,
            UnitProductionCost = variant.Cost,
            TotalRetailPrice = totalRetailPrice,
            TotalProductionCost = totalProductionCost,
            ShippingOptions = BuildShippingOptions(shipping, totalProductionCost)
        };
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

    private async Task<CreateProductRequest> BuildCreateProductRequestAsync(Product product)
    {
        if (product.PrintAreas is null || product.PrintAreas.Count == 0)
        {
            throw new InvalidOperationException(
                $"Product {product.Id} does not contain any print areas, so it cannot be recreated in another shop.");
        }

        var uploadCache = new Dictionary<string, UploadedImage>(StringComparer.OrdinalIgnoreCase);
        var verifiedUploadIds = new HashSet<string>(StringComparer.Ordinal);
        var printAreas = new List<PrintArea>(product.PrintAreas.Count);

        foreach (var printArea in product.PrintAreas)
        {
            printAreas.Add(await ClonePrintAreaAsync(printArea, uploadCache, verifiedUploadIds));
        }

        var emptyPrintArea = printAreas.FirstOrDefault(area => area.Placeholders.Count == 0);

        if (emptyPrintArea is not null)
        {
            var variantIds = string.Join(", ", emptyPrintArea.VariantIds.OrderBy(id => id));
            throw new InvalidOperationException(
                $"Product {product.Id} contains a print area for variants [{variantIds}] without any placeholder images, so it cannot be recreated in another shop.");
        }

        return new CreateProductRequest
        {
            Title = product.Title,
            Description = product.Description,
            BlueprintId = product.BlueprintId,
            PrintProviderId = product.PrintProviderId,
            Variants = product.Variants.Select(CreateCreateProductVariant).ToList(),
            PrintAreas = printAreas,
            Tags = product.Tags.ToList(),
            SafetyInformation = product.SafetyInformation,
            PrintDetails = ClonePrintDetails(product.PrintDetails)
        };
    }

    private static CreateProductVariant CreateCreateProductVariant(ProductVariant variant)
    {
        return new CreateProductVariant
        {
            Id = variant.Id,
            Price = variant.Price,
            IsEnabled = variant.IsEnabled
        };
    }

    private async Task<PrintArea> ClonePrintAreaAsync(
        PrintArea printArea,
        Dictionary<string, UploadedImage> uploadCache,
        HashSet<string> verifiedUploadIds)
    {
        var placeholders = new List<PrintAreaPlaceholder>();

        foreach (var placeholder in printArea.Placeholders ?? new List<PrintAreaPlaceholder>())
        {
            var clonedPlaceholder = await ClonePrintAreaPlaceholderAsync(placeholder, uploadCache, verifiedUploadIds);
            if (clonedPlaceholder.Images.Count > 0)
            {
                placeholders.Add(clonedPlaceholder);
            }
        }

        return new PrintArea
        {
            VariantIds = printArea.VariantIds.ToList(),
            Placeholders = placeholders
        };
    }

    private async Task<PrintAreaPlaceholder> ClonePrintAreaPlaceholderAsync(
        PrintAreaPlaceholder placeholder,
        Dictionary<string, UploadedImage> uploadCache,
        HashSet<string> verifiedUploadIds)
    {
        var images = new List<PrintAreaImage>();

        foreach (var image in placeholder.Images ?? new List<PrintAreaImage>())
        {
            if (!HasUsablePrintAreaImage(image))
            {
                continue;
            }

            images.Add(await NormalizePrintAreaImageAsync(image, uploadCache, verifiedUploadIds));
        }

        return new PrintAreaPlaceholder
        {
            Position = placeholder.Position,
            DecorationMethod = placeholder.DecorationMethod,
            Images = images
        };
    }

    private static bool HasUsablePrintAreaImage(PrintAreaImage image)
    {
        return !string.IsNullOrWhiteSpace(image.Id)
            || !string.IsNullOrWhiteSpace(image.Src);
    }

    private async Task<PrintAreaImage> NormalizePrintAreaImageAsync(
        PrintAreaImage image,
        Dictionary<string, UploadedImage> uploadCache,
        HashSet<string> verifiedUploadIds)
    {
        var normalizedSrc = string.IsNullOrWhiteSpace(image.Src)
            ? null
            : image.Src.Trim();
        var normalizedId = string.IsNullOrWhiteSpace(image.Id)
            ? null
            : image.Id.Trim();

        if (!string.IsNullOrWhiteSpace(normalizedId)
            && await UploadExistsAsync(normalizedId, verifiedUploadIds))
        {
            return ClonePrintAreaImage(image, normalizedId, normalizedSrc);
        }

        if (!string.IsNullOrWhiteSpace(normalizedSrc))
        {
            var uploaded = await EnsureUploadedImageAsync(normalizedSrc, image.Name, uploadCache);
            return ClonePrintAreaImage(image, uploaded.Id, normalizedSrc);
        }

        throw new InvalidOperationException(
            "A print area image is missing a valid Printify upload ID and does not expose a source URL that can be re-uploaded for transfer.");
    }

    private async Task<bool> UploadExistsAsync(string uploadId, HashSet<string> verifiedUploadIds)
    {
        if (verifiedUploadIds.Contains(uploadId))
        {
            return true;
        }

        try
        {
            await GetUploadAsync(uploadId);
            verifiedUploadIds.Add(uploadId);
            return true;
        }
        catch (PrintifyApiException ex) when (ex.StatusCode == 404)
        {
            return false;
        }
    }

    private async Task<UploadedImage> EnsureUploadedImageAsync(
        string sourceUrl,
        string? imageName,
        Dictionary<string, UploadedImage> uploadCache)
    {
        if (uploadCache.TryGetValue(sourceUrl, out var cachedUpload))
        {
            return cachedUpload;
        }

        var fileName = ResolveTransferUploadFileName(sourceUrl, imageName);
        var uploaded = await UploadImageByUrlAsync(fileName, sourceUrl);
        uploadCache[sourceUrl] = uploaded;
        return uploaded;
    }

    private static string ResolveTransferUploadFileName(string sourceUrl, string? imageName)
    {
        if (!string.IsNullOrWhiteSpace(imageName))
        {
            return imageName.Trim();
        }

        if (Uri.TryCreate(sourceUrl, UriKind.Absolute, out var imageUri))
        {
            var fileName = System.IO.Path.GetFileName(imageUri.LocalPath);
            if (!string.IsNullOrWhiteSpace(fileName))
            {
                return fileName;
            }
        }

        return "print-area-image";
    }

    private static PrintAreaImage ClonePrintAreaImage(PrintAreaImage image, string? resolvedId, string? resolvedSrc)
    {
        var normalizedId = string.IsNullOrWhiteSpace(resolvedId)
            ? null
            : resolvedId.Trim();
        var normalizedSrc = string.IsNullOrWhiteSpace(resolvedSrc)
            ? null
            : resolvedSrc.Trim();

        return new PrintAreaImage
        {
            // Cross-shop Create Product validation requires a real upload ID.
            // If the source product exposes only a stale or missing ID, re-upload the
            // source URL first and clone against that fresh upload.
            Id = normalizedId,
            Src = normalizedSrc,
            Name = image.Name,
            Type = image.Type,
            Height = image.Height,
            Width = image.Width,
            X = image.X,
            Y = image.Y,
            Scale = image.Scale,
            Angle = image.Angle,
            Pattern = CloneImagePattern(image.Pattern)
        };
    }

    private static ImagePattern? CloneImagePattern(ImagePattern? pattern)
    {
        if (pattern is null)
            return null;

        return new ImagePattern
        {
            SpacingX = pattern.SpacingX,
            SpacingY = pattern.SpacingY,
            Angle = pattern.Angle,
            Offset = pattern.Offset,
            Scale = pattern.Scale
        };
    }

    private static PrintDetails? ClonePrintDetails(PrintDetails? printDetails)
    {
        if (printDetails is null)
            return null;

        return new PrintDetails
        {
            PrintOnSide = printDetails.PrintOnSide,
            SeparatorType = printDetails.SeparatorType,
            SeparatorColor = printDetails.SeparatorColor
        };
    }

    private async Task<List<T>> GetAllPagesAsync<T>(string firstPagePath)
    {
        var all = new List<T>();
        string? url = $"{BaseUrl}{firstPagePath}";
        int pagenum = 1;
        int limit = 50;
        List<T>? pageData = new List<T>();

        string pageurl = url + (url.Contains("?") ? $"&page={pagenum}&limit={limit}" : $"?page={pagenum}&limit={limit}");
        Console.WriteLine($"Fetching page {pagenum} from {pageurl}...");
        var response = await _http.GetAsync(pageurl);
        await EnsureSuccessAsync(response);
        var json = await response.Content.ReadAsStringAsync();
        var page = JsonSerializer.Deserialize<PaginatedResponse<T>>(json, JsonOpts)!;
        int pages = (int)Math.Floor((double)page.Total / limit)+1;
        var tasks = new List<Task<HttpResponseMessage>>(Math.Max(pages - 1, 0));

        Console.WriteLine($"Page 1: Fetched {page.Data.Count} items. Total items: {page.Total}. Total pages: {pages}.");
        all.AddRange(page.Data);

        Enumerable.Range(2, pages-1).ToList().ForEach(i =>
        {
            string pageurl = url + (url.Contains("?") ? $"&page={i}&limit={limit}" : $"?page={i}&limit={limit}");
            int indx = i; // capture loop variable
            // Console.WriteLine($"Queueing page {indx} from {pageurl}...");
            tasks.Add(_http.GetAsync(pageurl));
        });

        while(tasks.Count > 0)
        {
            var completedTask = await Task.WhenAny(tasks);
            tasks.Remove(completedTask);

            var pageResponse = await completedTask;
            await EnsureSuccessAsync(pageResponse);
            var pageJson = await pageResponse.Content.ReadAsStringAsync();
            var pageDataObj = JsonSerializer.Deserialize<PaginatedResponse<T>>(pageJson, JsonOpts)!;
            all.AddRange(pageDataObj.Data);
        }


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

    private static List<ProductVariantShippingOptionBreakdown> BuildShippingOptions(
        ShippingCostResponse shipping,
        int totalProductionCost)
    {
        return new List<ProductVariantShippingOptionBreakdown>
        {
            CreateShippingOption("standard", shipping.Standard, totalProductionCost),
            CreateShippingOption("express", shipping.Express, totalProductionCost),
            CreateShippingOption("priority", shipping.Priority, totalProductionCost),
            CreateShippingOption("printify_express", shipping.PrintifyExpress, totalProductionCost),
            CreateShippingOption("economy", shipping.Economy, totalProductionCost)
        };
    }

    private static ProductVariantShippingOptionBreakdown CreateShippingOption(
        string method,
        int shippingCost,
        int totalProductionCost)
    {
        return new ProductVariantShippingOptionBreakdown
        {
            Method = method,
            ShippingCost = shippingCost,
            TotalCost = checked(totalProductionCost + shippingCost)
        };
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

public record ProductTransferResult
{
    public int SourceShopId { get; set; }
    public string SourceProductId { get; set; } = "";
    public int TargetShopId { get; set; }
    public string TargetProductId { get; set; } = "";
    public bool SourceProductDeleted { get; set; }
    public bool TargetProductPublished { get; set; }
    public Product TargetProduct { get; set; } = new();
}
