using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;
using PrintifyGenerator.Dashboard.Models;

namespace PrintifyGenerator.Dashboard.Services;

public sealed class StagingSwipeReviewService
{
    private const int PreloadedUpcomingItemCount = 6;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly HttpClient _httpClient;
    private readonly IWebHostEnvironment _environment;
    private readonly IOptionsMonitor<DashboardOptions> _options;
    private readonly SwipeReviewMoveQueueService _moveQueue;

    public StagingSwipeReviewService(
        HttpClient httpClient,
        IWebHostEnvironment environment,
        IOptionsMonitor<DashboardOptions> options,
        SwipeReviewMoveQueueService moveQueue)
    {
        _httpClient = httpClient;
        _environment = environment;
        _options = options;
        _moveQueue = moveQueue;
    }

    public async Task<SwipeReviewSnapshot> LoadSnapshotAsync(CancellationToken cancellationToken)
    {
        var dataRoot = ResolveDataRoot();
        var settings = OrchestrationSettingsStore.Load(dataRoot);
        return await LoadSnapshotAsync(dataRoot, settings, resolvedContext: null, cancellationToken);
    }

    public async Task<SwipeReviewActionResult> DeleteAsync(string productId, CancellationToken cancellationToken)
    {
        var dataRoot = ResolveDataRoot();
        var settings = OrchestrationSettingsStore.Load(dataRoot);
        var context = await ResolveContextAsync(settings, requireOllama: false, cancellationToken);

        return await DeleteAsync(productId, dataRoot, context, cancellationToken);
    }

    public async Task<SwipeReviewActionExecution> DeleteAndLoadSnapshotAsync(string productId, CancellationToken cancellationToken)
    {
        var dataRoot = ResolveDataRoot();
        var settings = OrchestrationSettingsStore.Load(dataRoot);
        var context = await ResolveContextAsync(settings, requireOllama: false, cancellationToken);
        var actionResult = await DeleteAsync(productId, dataRoot, context, cancellationToken);
        var snapshot = await LoadSnapshotAsync(dataRoot, settings, context, cancellationToken);

        return new SwipeReviewActionExecution(actionResult, snapshot);
    }

    public async Task<SwipeReviewActionResult> PromoteAsync(string productId, CancellationToken cancellationToken)
    {
        var dataRoot = ResolveDataRoot();
        var settings = OrchestrationSettingsStore.Load(dataRoot);
        var context = await ResolveContextAsync(settings, requireOllama: false, cancellationToken);

        return await PromoteAsync(productId, dataRoot, context, cancellationToken, cleanupTargetOnFailure: true);
    }

    public async Task<SwipeReviewActionExecution> EnqueuePromoteAndLoadSnapshotAsync(string productId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new InvalidOperationException("A staged product ID is required.");

        var normalizedProductId = productId.Trim();
        var dataRoot = ResolveDataRoot();
        var settings = OrchestrationSettingsStore.Load(dataRoot);

        _ = FindDraftRecord(dataRoot, normalizedProductId)
            ?? throw new FileNotFoundException($"No draft record was found for staged product {normalizedProductId}.");

        var queued = _moveQueue.TryEnqueue(normalizedProductId);
        var snapshot = await LoadSnapshotAsync(dataRoot, settings, resolvedContext: null, cancellationToken);
        var actionResult = queued
            ? new SwipeReviewActionResult(
                Success: true,
                Message: $"Queued {normalizedProductId} to move in the background.",
                TargetProductId: null)
            : new SwipeReviewActionResult(
                Success: false,
                Message: $"Move for {normalizedProductId} is already queued.",
                TargetProductId: null);

        return new SwipeReviewActionExecution(actionResult, snapshot);
    }

    public async Task<SwipeReviewActionResult> ProcessQueuedPromoteAsync(string productId, CancellationToken cancellationToken)
    {
        var dataRoot = ResolveDataRoot();
        var settings = OrchestrationSettingsStore.Load(dataRoot);
        var context = await ResolveContextAsync(settings, requireOllama: false, cancellationToken);

        return await PromoteAsync(productId, dataRoot, context, cancellationToken, cleanupTargetOnFailure: false);
    }

    public async Task<SwipeReviewActionExecution> PromoteAndLoadSnapshotAsync(string productId, CancellationToken cancellationToken)
    {
        var dataRoot = ResolveDataRoot();
        var settings = OrchestrationSettingsStore.Load(dataRoot);
        var context = await ResolveContextAsync(settings, requireOllama: false, cancellationToken);
        var actionResult = await PromoteAsync(productId, dataRoot, context, cancellationToken, cleanupTargetOnFailure: true);
        var snapshot = await LoadSnapshotAsync(dataRoot, settings, context, cancellationToken);

        return new SwipeReviewActionExecution(actionResult, snapshot);
    }

    private async Task<SwipeReviewSnapshot> LoadSnapshotAsync(
        string dataRoot,
        OrchestrationSettings settings,
        ResolvedReviewContext? resolvedContext,
        CancellationToken cancellationToken)
    {
        var queuedProductIds = _moveQueue.CreateQueuedProductIdSnapshot();
        var queuedDrafts = LoadDraftQueue(dataRoot)
            .OrderByDescending(entry => entry.CreatedAtUtc)
            .ToList();
        var drafts = queuedDrafts
            .Where(entry => !queuedProductIds.Contains(entry.Draft.ProductId))
            .ToList();
        var queuedDraftCount = queuedDrafts.Count - drafts.Count;

        if (drafts.Count == 0)
        {
            return new SwipeReviewSnapshot(
                QueueCount: 0,
                CurrentItem: null,
                StatusMessage: queuedDraftCount > 0
                    ? $"{queuedDraftCount} move{(queuedDraftCount == 1 ? string.Empty : "s")} queued in the background."
                    : "No staged draft products are waiting for review.",
                UpcomingItem: null,
                UpcomingItems: Array.Empty<SwipeReviewItem>());
        }

        var statusMessage = string.Empty;
        SwipeReviewItem currentItem;

        try
        {
            var context = resolvedContext ?? await ResolveContextAsync(settings, requireOllama: false, cancellationToken);
            currentItem = await BuildCurrentItemAsync(drafts[0], dataRoot, context, cancellationToken);
        }
        catch (Exception ex)
        {
            currentItem = BuildFallbackItem(drafts[0], dataRoot, ex.Message);
            statusMessage = ex.Message;
        }

        var upcomingItems = drafts
            .Skip(1)
            .Take(PreloadedUpcomingItemCount)
            .Select(storedDraft => BuildPreviewItem(storedDraft, dataRoot))
            .ToArray();
        var upcomingItem = upcomingItems.FirstOrDefault();

        return new SwipeReviewSnapshot(
            QueueCount: drafts.Count,
            CurrentItem: currentItem,
            StatusMessage: string.IsNullOrWhiteSpace(statusMessage) ? null : statusMessage,
            UpcomingItem: upcomingItem,
            UpcomingItems: upcomingItems);
    }

    private async Task<SwipeReviewActionResult> DeleteAsync(
        string productId,
        string dataRoot,
        ResolvedReviewContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new InvalidOperationException("A staged product ID is required.");

        if (_moveQueue.IsQueued(productId))
            throw new InvalidOperationException("This product is already queued for a background move.");

        var draft = FindDraftRecord(dataRoot, productId)
            ?? throw new FileNotFoundException($"No draft record was found for staged product {productId}.");

        try
        {
            await context.Printify.DeleteProductAsync(context.StagingShop.Id, draft.Draft.ProductId);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            // The staging product is already gone; remove the stale draft record below.
        }

        DeleteDraftRecord(draft.DraftPath);

        return new SwipeReviewActionResult(
            Success: true,
            Message: $"Removed staged product {draft.Draft.ProductId} from {context.StagingShop.Title}.",
            TargetProductId: null);
    }

    private async Task<SwipeReviewActionResult> PromoteAsync(
        string productId,
        string dataRoot,
        ResolvedReviewContext context,
        CancellationToken cancellationToken,
        bool cleanupTargetOnFailure)
    {
        if (string.IsNullOrWhiteSpace(productId))
            throw new InvalidOperationException("A staged product ID is required.");

        var draft = FindDraftRecord(dataRoot, productId)
            ?? throw new FileNotFoundException($"No draft record was found for staged product {productId}.");
        var stagingProduct = await context.Printify.GetProductAsync(context.StagingShop.Id, draft.Draft.ProductId);
        var generatedListing = await GenerateListingContentAsync(draft.Draft, stagingProduct, context, cancellationToken);
        var repricedVariants = await BuildVariantUpdatesAsync(stagingProduct, context, cancellationToken);
        ProductTransferResult? transfer = null;

        try
        {
            transfer = await context.Printify.TransferProductAsync(
                context.StagingShop.Id,
                draft.Draft.ProductId,
                context.PublishingShop.Id,
                deleteSourceProduct: false,
                publishTargetProduct: false);

            var updateRequest = new UpdateProductRequest
            {
                Title = generatedListing.Title,
                Description = generatedListing.Description,
                Tags = generatedListing.Tags.Count > 0 ? generatedListing.Tags : transfer.TargetProduct.Tags,
                Variants = repricedVariants.Count > 0 ? repricedVariants : null
            };

            await context.Printify.UpdateProductAsync(context.PublishingShop.Id, transfer.TargetProductId, updateRequest);
        }
        catch (Exception ex)
        {
            if (cleanupTargetOnFailure && !string.IsNullOrWhiteSpace(transfer?.TargetProductId))
            {
                await TryDeleteTargetCloneAsync(context, transfer.TargetProductId);
            }

            var failureReason = DescribeFailure(ex);
            var failurePrefix = string.IsNullOrWhiteSpace(transfer?.TargetProductId)
                ? $"The product {draft.Draft.ProductId} could not be moved to {context.PublishingShop.Title}."
                : $"The product clone {transfer.TargetProductId} could not be prepared in {context.PublishingShop.Title}.";
            var cleanupSummary = cleanupTargetOnFailure
                ? " The target draft copy was removed so the queue stays clean."
                : string.IsNullOrWhiteSpace(transfer?.TargetProductId)
                    ? " No product changes were applied."
                    : " No cleanup was applied after the failure, so the created draft copy was left in place for manual review.";

            throw new InvalidOperationException(
                $"{failurePrefix} {failureReason}{cleanupSummary}",
                ex);
        }

        string? completionMessage = null;

        try
        {
            await context.Printify.DeleteProductAsync(context.StagingShop.Id, draft.Draft.ProductId);
        }
        catch
        {
            completionMessage = $"Moved {transfer.TargetProductId} to {context.PublishingShop.Title} as a draft, but deleting the staging product failed. Remove the staging copy manually to avoid duplicates.";
        }

        DeleteDraftRecord(draft.DraftPath);

        return new SwipeReviewActionResult(
            Success: string.IsNullOrWhiteSpace(completionMessage),
            Message: completionMessage ?? $"Moved {transfer!.TargetProductId} to {context.PublishingShop.Title} as a draft with refreshed copy and repriced variants.",
            TargetProductId: transfer!.TargetProductId);
    }

    private async Task<SwipeReviewItem> BuildCurrentItemAsync(
        StoredDraftRecord storedDraft,
        string dataRoot,
        ResolvedReviewContext context,
        CancellationToken cancellationToken)
    {
        var draft = storedDraft.Draft;
        var product = await context.Printify.GetProductAsync(context.StagingShop.Id, draft.ProductId);
        var channelContent = ResolveChannelContent(draft);
        var pricingPreview = await BuildPricingPreviewAsync(product, context, cancellationToken);

        return new SwipeReviewItem(
            ProductId: draft.ProductId,
            JobId: ChooseValue(draft.JobId, "Unknown job"),
            ReferenceCode: ChooseValue(draft.ReferenceCode, draft.ProductId),
            BlueprintTitle: ChooseValue(draft.BlueprintTitle, "Unknown product"),
            PrintProviderTitle: ChooseValue(draft.PrintProviderTitle, "Unknown provider"),
            CreatedAtUtc: storedDraft.CreatedAtUtc,
            CurrentTitle: ChooseValue(product.Title, channelContent.Title, draft.BlueprintTitle, "Untitled product"),
            CurrentDescription: ChooseValue(product.Description, channelContent.Description, draft.LlmReason, "No description available."),
            CurrentPriceLabel: pricingPreview.CurrentLabel,
            SuggestedPriceLabel: pricingPreview.SuggestedLabel,
            MarginLabel: pricingPreview.MarginLabel,
            Images: BuildDisplayImages(draft, dataRoot),
            Tags: ResolveTags(channelContent, product, draft),
            CanDelete: true,
            CanPromote: true,
            PromoteUnavailableReason: null);
    }

    private SwipeReviewItem BuildFallbackItem(
        StoredDraftRecord storedDraft,
        string dataRoot,
        string reason)
    {
        var draft = storedDraft.Draft;
        var channelContent = ResolveChannelContent(draft);

        return new SwipeReviewItem(
            ProductId: draft.ProductId,
            JobId: ChooseValue(draft.JobId, "Unknown job"),
            ReferenceCode: ChooseValue(draft.ReferenceCode, draft.ProductId),
            BlueprintTitle: ChooseValue(draft.BlueprintTitle, "Unknown product"),
            PrintProviderTitle: ChooseValue(draft.PrintProviderTitle, "Unknown provider"),
            CreatedAtUtc: storedDraft.CreatedAtUtc,
            CurrentTitle: ChooseValue(channelContent.Title, draft.BlueprintTitle, draft.ProductId, "Untitled product"),
            CurrentDescription: ChooseValue(channelContent.Description, draft.LlmReason, "The live staging product could not be loaded."),
            CurrentPriceLabel: "Unavailable",
            SuggestedPriceLabel: "Unavailable",
            MarginLabel: "Live pricing unavailable",
            Images: BuildDisplayImages(draft, dataRoot),
            Tags: ResolveFallbackTags(channelContent, draft),
            CanDelete: false,
            CanPromote: false,
            PromoteUnavailableReason: reason);
    }

    private SwipeReviewItem BuildPreviewItem(
        StoredDraftRecord storedDraft,
        string dataRoot)
    {
        var draft = storedDraft.Draft;
        var channelContent = ResolveChannelContent(draft);

        return new SwipeReviewItem(
            ProductId: draft.ProductId,
            JobId: ChooseValue(draft.JobId, "Unknown job"),
            ReferenceCode: ChooseValue(draft.ReferenceCode, draft.ProductId),
            BlueprintTitle: ChooseValue(draft.BlueprintTitle, "Unknown product"),
            PrintProviderTitle: ChooseValue(draft.PrintProviderTitle, "Unknown provider"),
            CreatedAtUtc: storedDraft.CreatedAtUtc,
            CurrentTitle: ChooseValue(channelContent.Title, draft.BlueprintTitle, draft.ProductId, "Untitled product"),
            CurrentDescription: ChooseValue(channelContent.Description, draft.LlmReason, "Draft details are loading."),
            CurrentPriceLabel: "Loading...",
            SuggestedPriceLabel: "Loading...",
            MarginLabel: "Fetching live pricing",
            Images: BuildDisplayImages(draft, dataRoot),
            Tags: ResolveFallbackTags(channelContent, draft),
            CanDelete: true,
            CanPromote: true,
            PromoteUnavailableReason: null);
    }

    private async Task<PricingPreview> BuildPricingPreviewAsync(
        Product product,
        ResolvedReviewContext context,
        CancellationToken cancellationToken)
    {
        var variants = SelectReviewVariants(product);
        if (variants.Count == 0)
        {
            return new PricingPreview("Unavailable", "Unavailable", $"{context.MarginPercent:0.#}% margin");
        }

        ShippingInfo? shippingInfo = null;
        try
        {
            shippingInfo = await context.Printify.GetBlueprintShippingAsync(product.BlueprintId, product.PrintProviderId);
        }
        catch
        {
            // If shipping data is unavailable, fall back to production-cost-only pricing.
        }

        var currentPrices = variants
            .Select(variant => variant.Price)
            .ToList();

        var suggestedPrices = variants
            .Select(variant => CalculateAppealingPrice(
                variant.Cost,
                ResolveShippingCost(shippingInfo, variant.Id, context.ShippingCountryCode),
                context.MarginPercent))
            .ToList();

        return new PricingPreview(
            CurrentLabel: FormatCurrencyRange(currentPrices),
            SuggestedLabel: FormatCurrencyRange(suggestedPrices),
            MarginLabel: $"{context.MarginPercent:0.#}% margin with retail-friendly endings");
    }

    private async Task<List<CreateProductVariant>> BuildVariantUpdatesAsync(
        Product product,
        ResolvedReviewContext context,
        CancellationToken cancellationToken)
    {
        var variants = SelectReviewVariants(product);
        if (variants.Count == 0)
            return new List<CreateProductVariant>();

        ShippingInfo? shippingInfo = null;
        try
        {
            shippingInfo = await context.Printify.GetBlueprintShippingAsync(product.BlueprintId, product.PrintProviderId);
        }
        catch
        {
            // Allow promotion to continue with production-cost-only pricing.
        }

        return variants
            .Select(variant => new CreateProductVariant
            {
                Id = variant.Id,
                IsEnabled = variant.IsEnabled,
                Price = CalculateAppealingPrice(
                    variant.Cost,
                    ResolveShippingCost(shippingInfo, variant.Id, context.ShippingCountryCode),
                    context.MarginPercent)
            })
            .ToList();
    }

    private async Task<GeneratedListingContent> GenerateListingContentAsync(
        MockupDraftRecord draft,
        Product stagingProduct,
        ResolvedReviewContext context,
        CancellationToken cancellationToken)
    {
        var storedListing = TryResolveStoredListingContent(draft, stagingProduct);
        if (storedListing is not null)
            return storedListing;

        if (context.ActiveOllamaNode is null)
        {
            return new GeneratedListingContent(
                CleanTitle(null, stagingProduct.Title, draft.BlueprintTitle),
                CleanDescription(null, stagingProduct.Description, draft.LlmReason),
                ResolveTags(new ListingChannelContent(), stagingProduct, draft).ToList());
        }

        var prompt = BuildListingPrompt(draft, stagingProduct);
        PreparedPromptImage? preparedImage = null;

        try
        {
            preparedImage = await PreparePromptImageAsync(draft, cancellationToken);
            using var ollama = new OllamaClient(context.ActiveOllamaNode.BaseUrl);

            var rawResponse = preparedImage is not null
                ? await ollama.GenerateWithImageAsync(context.VisionModel, prompt, preparedImage.ImagePath)
                : await ollama.GenerateAsync(context.PromptModel, prompt);

            var responseBody = ExtractOllamaResponse(rawResponse);
            var payloadJson = ExtractJsonPayload(responseBody);
            var payload = JsonSerializer.Deserialize<GeneratedListingPayload>(payloadJson, JsonOptions)
                ?? throw new InvalidOperationException("The LLM did not return listing content.");

            var title = CleanTitle(payload.Title, stagingProduct.Title, draft.BlueprintTitle);
            var description = CleanDescription(payload.Description, stagingProduct.Description, draft.LlmReason);
            var tags = NormalizeTags(payload.Tags, stagingProduct.Tags, draft);

            return new GeneratedListingContent(title, description, tags);
        }
        finally
        {
            if (preparedImage is { ShouldDelete: true } && File.Exists(preparedImage.ImagePath))
            {
                File.Delete(preparedImage.ImagePath);
            }
        }
    }

    private async Task<PreparedPromptImage?> PreparePromptImageAsync(MockupDraftRecord draft, CancellationToken cancellationToken)
    {
        foreach (var mockupUrl in draft.MockupUrls.Where(url => !string.IsNullOrWhiteSpace(url)))
        {
            try
            {
                if (!Uri.TryCreate(mockupUrl, UriKind.Absolute, out var uri))
                    continue;

                var extension = Path.GetExtension(uri.AbsolutePath);
                if (string.IsNullOrWhiteSpace(extension))
                    extension = ".jpg";

                var tempPath = Path.Combine(Path.GetTempPath(), $"printify-swipe-{Guid.NewGuid():N}{extension}");

                using var response = await _httpClient.GetAsync(uri, cancellationToken);
                response.EnsureSuccessStatusCode();

                await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
                await using var output = File.Create(tempPath);
                await input.CopyToAsync(output, cancellationToken);

                return new PreparedPromptImage(tempPath, ShouldDelete: true);
            }
            catch
            {
                // Fall back to the local artwork image if the mockup fetch fails.
            }
        }

        var localImagePath = NormalizeOptionalPath(draft.LocalImagePath);
        if (localImagePath is not null && File.Exists(localImagePath))
            return new PreparedPromptImage(localImagePath, ShouldDelete: false);

        return null;
    }

    private static List<SwipeReviewImage> BuildDisplayImages(MockupDraftRecord draft, string dataRoot)
    {
        var images = new List<SwipeReviewImage>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var localImageUrl = BuildLocalImageUrl(draft.LocalImagePath, dataRoot);
        if (!string.IsNullOrWhiteSpace(localImageUrl) && seen.Add(localImageUrl))
        {
            images.Add(new SwipeReviewImage(
                Url: localImageUrl,
                AltText: $"Artwork for {ChooseValue(draft.BlueprintTitle, draft.ProductId)}",
                Caption: "Original artwork",
                SourceLabel: "Artwork"));
        }

        var mockupIndex = 1;
        foreach (var mockupUrl in draft.MockupUrls.Where(url => !string.IsNullOrWhiteSpace(url)))
        {
            if (!seen.Add(mockupUrl))
                continue;

            images.Add(new SwipeReviewImage(
                Url: mockupUrl,
                AltText: $"Mockup for {ChooseValue(draft.BlueprintTitle, draft.ProductId)}",
                Caption: $"Mockup {mockupIndex}",
                SourceLabel: "Mockup"));
            mockupIndex++;
        }

        if (!string.IsNullOrWhiteSpace(draft.PrintifyImagePreviewUrl) && seen.Add(draft.PrintifyImagePreviewUrl))
        {
            images.Add(new SwipeReviewImage(
                Url: draft.PrintifyImagePreviewUrl,
                AltText: $"Preview image for {ChooseValue(draft.BlueprintTitle, draft.ProductId)}",
                Caption: "Uploaded preview",
                SourceLabel: "Preview"));
        }

        return images;
    }

    private static GeneratedListingContent? TryResolveStoredListingContent(MockupDraftRecord draft, Product stagingProduct)
    {
        var channelContent = ResolveChannelContent(draft);
        if (!HasStoredListingContent(channelContent))
            return null;

        return new GeneratedListingContent(
            CleanTitle(channelContent.Title, stagingProduct.Title, draft.BlueprintTitle),
            CleanDescription(channelContent.Description, stagingProduct.Description, draft.LlmReason),
            ResolveTags(channelContent, stagingProduct, draft).ToList());
    }

    private static bool HasStoredListingContent(ListingChannelContent channelContent)
    {
        return !string.IsNullOrWhiteSpace(channelContent.Title)
            || !string.IsNullOrWhiteSpace(channelContent.Description)
            || channelContent.Tags.Any(tag => !string.IsNullOrWhiteSpace(tag));
    }

    private static string? BuildLocalImageUrl(string? localImagePath, string dataRoot)
    {
        var normalizedImagePath = NormalizeOptionalPath(localImagePath);
        if (normalizedImagePath is null)
            return null;

        var checkingRoot = Path.GetFullPath(Path.Combine(dataRoot, "Checking"));
        var relativePath = Path.GetRelativePath(checkingRoot, normalizedImagePath);
        if (relativePath.StartsWith("..", StringComparison.OrdinalIgnoreCase))
            return null;

        return "/generated/" + relativePath.Replace('\\', '/');
    }

    private async Task<ResolvedReviewContext> ResolveContextAsync(
        OrchestrationSettings settings,
        bool requireOllama,
        CancellationToken cancellationToken)
    {
        var repositoryRoot = ResolveRepositoryRoot();
        var envFilePath = Path.Combine(repositoryRoot, "main.env");
        var token = ReadRequiredEnvValue(envFilePath, "TOKEN");
        var printify = new PrintifyClient(token);

        var shops = await printify.GetShopsAsync();
        if (shops.Count == 0)
            throw new InvalidOperationException("No Printify shops were returned for the configured token.");

        var stagingShop = ResolveStagingShop(envFilePath, shops)
            ?? throw new InvalidOperationException("The staging shop could not be resolved.");
        var publishingShop = ResolvePublishingShop(envFilePath, shops, stagingShop.Id)
            ?? throw new InvalidOperationException("The publishing shop could not be resolved.");

        var activeOllamaNode = ResolveActiveOllamaNode(settings);
        if (requireOllama && activeOllamaNode is null)
            throw new InvalidOperationException("No enabled Ollama node is configured for listing generation.");

        var marginPercent = ReadOptionalDecimalEnvValue(envFilePath, "PRICE_UPDATER_MARGIN_PERCENT") ?? 40m;
        var shippingCountryCode = NormalizeCountryCode(ReadOptionalEnvValue(envFilePath, "PRICE_UPDATER_COUNTRY"));

        return new ResolvedReviewContext(
            RepositoryRoot: repositoryRoot,
            EnvFilePath: envFilePath,
            Printify: printify,
            StagingShop: stagingShop,
            PublishingShop: publishingShop,
            ActiveOllamaNode: activeOllamaNode,
            VisionModel: string.IsNullOrWhiteSpace(settings.MockupVisionModel) ? settings.PromptModel : settings.MockupVisionModel,
            PromptModel: settings.PromptModel,
            MarginPercent: marginPercent,
            ShippingCountryCode: shippingCountryCode);
    }

    private string ResolveDataRoot()
    {
        return DashboardOptions.ResolveDataRoot(_options.CurrentValue.DataRoot, _environment.ContentRootPath);
    }

    private string ResolveRepositoryRoot()
    {
        var probeRoots = new[]
        {
            _environment.ContentRootPath,
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        foreach (var probeRoot in probeRoots.Where(path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var current = new DirectoryInfo(Path.GetFullPath(probeRoot));
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "main.env")))
                    return current.FullName;

                current = current.Parent;
            }
        }

        throw new FileNotFoundException("Could not locate main.env from the dashboard project root.");
    }

    private static List<StoredDraftRecord> LoadDraftQueue(string dataRoot)
    {
        var draftsRoot = Path.Combine(dataRoot, "staging", "drafts");
        if (!Directory.Exists(draftsRoot))
            return new List<StoredDraftRecord>();

        var drafts = new List<StoredDraftRecord>();
        foreach (var filePath in Directory.EnumerateFiles(draftsRoot, "*.json", SearchOption.AllDirectories))
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var draft = JsonSerializer.Deserialize<MockupDraftRecord>(json, JsonOptions);
                if (draft is null || string.IsNullOrWhiteSpace(draft.ProductId))
                    continue;

                drafts.Add(new StoredDraftRecord(
                    Draft: draft,
                    DraftPath: filePath,
                    CreatedAtUtc: ParseCreatedAt(draft.CreatedAt, File.GetLastWriteTimeUtc(filePath))));
            }
            catch
            {
                // Ignore malformed draft records and keep the review queue available.
            }
        }

        return drafts;
    }

    private StoredDraftRecord? FindDraftRecord(string dataRoot, string productId)
    {
        var normalizedProductId = productId.Trim();
        return LoadDraftQueue(dataRoot)
            .FirstOrDefault(entry => string.Equals(entry.Draft.ProductId, normalizedProductId, StringComparison.OrdinalIgnoreCase));
    }

    private static void DeleteDraftRecord(string filePath)
    {
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    private static async Task TryDeleteTargetCloneAsync(ResolvedReviewContext context, string targetProductId)
    {
        if (string.IsNullOrWhiteSpace(targetProductId))
            return;

        try
        {
            await context.Printify.DeleteProductAsync(context.PublishingShop.Id, targetProductId);
        }
        catch
        {
            // Best-effort cleanup only.
        }
    }

    private static List<ProductVariant> SelectReviewVariants(Product product)
    {
        var enabledVariants = product.Variants.Where(variant => variant.IsEnabled).ToList();
        return enabledVariants.Count > 0 ? enabledVariants : product.Variants.ToList();
    }

    private static int ResolveShippingCost(ShippingInfo? shippingInfo, int variantId, string countryCode)
    {
        if (shippingInfo?.Profiles is null || shippingInfo.Profiles.Count == 0)
            return 0;

        var matchingProfiles = shippingInfo.Profiles
            .Where(profile => profile.VariantIds.Count == 0 || profile.VariantIds.Contains(variantId))
            .ToList();

        if (matchingProfiles.Count == 0)
            matchingProfiles = shippingInfo.Profiles.ToList();

        var matchingCountry = matchingProfiles.FirstOrDefault(profile =>
            profile.Countries.Any(country => string.Equals(country, countryCode, StringComparison.OrdinalIgnoreCase)));

        var profile = matchingCountry
            ?? matchingProfiles.FirstOrDefault(candidate => candidate.Countries.Count == 0)
            ?? matchingProfiles.FirstOrDefault();

        return profile?.FirstItem?.Cost ?? 0;
    }

    private static int CalculateAppealingPrice(int productionPrice, int shippingPrice, decimal marginPercent)
    {
        if (productionPrice <= 0)
            return Math.Max(0, productionPrice);

        var costFloor = (productionPrice + Math.Max(0, shippingPrice)) / 100m;
        var targetRetail = costFloor * (1m + (marginPercent / 100m));
        var roundedRetail = RoundRetailPrice(targetRetail);

        if (roundedRetail < costFloor)
            roundedRetail = costFloor;

        return checked((int)Math.Ceiling(roundedRetail * 100m));
    }

    private static decimal RoundRetailPrice(decimal amount)
    {
        if (amount <= 0)
            return 0m;

        var ending = amount < 10m ? 0.99m : amount < 50m ? 0.49m : 0.99m;
        var rounded = Math.Floor(amount) + ending;

        if (rounded < amount)
            rounded += 1m;

        return decimal.Round(rounded, 2, MidpointRounding.AwayFromZero);
    }

    private static string FormatCurrencyRange(IEnumerable<int> pricesInCents)
    {
        var ordered = pricesInCents
            .Where(price => price > 0)
            .Select(price => price / 100m)
            .OrderBy(price => price)
            .ToList();

        if (ordered.Count == 0)
            return "Unavailable";

        var min = ordered[0];
        var max = ordered[^1];
        return min == max
            ? $"${min.ToString("0.00", CultureInfo.InvariantCulture)}"
            : $"${min.ToString("0.00", CultureInfo.InvariantCulture)} - ${max.ToString("0.00", CultureInfo.InvariantCulture)}";
    }

    private static ListingChannelContent ResolveChannelContent(MockupDraftRecord draft)
    {
        if (draft.ChannelContent.TryGetValue("printify", out var printifyContent))
            return printifyContent;

        if (draft.ChannelContent.TryGetValue("generic", out var genericContent))
            return genericContent;

        return draft.ChannelContent.Values.FirstOrDefault() ?? new ListingChannelContent();
    }

    private static IReadOnlyList<string> ResolveTags(ListingChannelContent channelContent, Product product, MockupDraftRecord draft)
    {
        return channelContent.Tags
            .Concat(product.Tags)
            .Concat(draft.LookupTags)
            .Select(tag => tag?.Trim())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .Cast<string>()
            .ToList();
    }

    private static IReadOnlyList<string> ResolveFallbackTags(ListingChannelContent channelContent, MockupDraftRecord draft)
    {
        return channelContent.Tags
            .Concat(draft.LookupTags)
            .Select(tag => tag?.Trim())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .Cast<string>()
            .ToList();
    }

    private static string BuildListingPrompt(MockupDraftRecord draft, Product product)
    {
                return $@"You are rewriting a print-on-demand listing for an ecommerce storefront.

Return only JSON with this shape:
{{
    ""title"": ""sale-ready title"",
    ""description"": ""2 short paragraphs"",
    ""tags"": [""tag one"", ""tag two""]
}}

Rules:
- Keep the title under 140 characters.
- Write a description that is persuasive but factual.
- Do not mention internal lookup codes, job ids, or production partners unless essential.
- Do not invent materials, certifications, shipping times, or licensing claims.
- Focus on what the image communicates and why the product makes sense for it.
- Tags should be short, lowercase, and shopper-friendly.

Product type: {ChooseValue(draft.BlueprintTitle, product.Title)}
Existing title: {ChooseValue(product.Title, "Unavailable")}
Existing description: {ChooseValue(product.Description, draft.LlmReason, "Unavailable")}
Original fit reason: {ChooseValue(draft.LlmReason, "Unavailable")}";
    }

    private static string ExtractOllamaResponse(string rawResponse)
    {
        using var document = JsonDocument.Parse(rawResponse);
        if (document.RootElement.TryGetProperty("response", out var responseElement) &&
            responseElement.ValueKind == JsonValueKind.String)
        {
            return responseElement.GetString()?.Trim() ?? string.Empty;
        }

        return rawResponse.Trim();
    }

    private static string ExtractJsonPayload(string responseBody)
    {
        var normalized = responseBody.Trim();
        if (normalized.StartsWith("```", StringComparison.Ordinal))
        {
            normalized = normalized.Trim('`').Trim();
            if (normalized.StartsWith("json", StringComparison.OrdinalIgnoreCase))
                normalized = normalized[4..].Trim();
        }

        var startIndex = normalized.IndexOf('{');
        var endIndex = normalized.LastIndexOf('}');
        if (startIndex < 0 || endIndex <= startIndex)
            throw new InvalidOperationException("The LLM did not return valid JSON.");

        return normalized[startIndex..(endIndex + 1)];
    }

    private static string CleanTitle(string? generatedTitle, string? fallbackTitle, string? blueprintTitle)
    {
        var title = ChooseValue(generatedTitle, fallbackTitle, blueprintTitle, "Untitled product");
        return title.Length <= 140 ? title : title[..140].Trim();
    }

    private static string CleanDescription(string? generatedDescription, string? fallbackDescription, string? fallbackReason)
    {
        return ChooseValue(generatedDescription, fallbackDescription, fallbackReason, "No description available.");
    }

    private static List<string> NormalizeTags(IEnumerable<string>? generatedTags, IReadOnlyList<string> fallbackTags, MockupDraftRecord draft)
    {
        var tags = (generatedTags ?? Array.Empty<string>())
            .Select(tag => tag?.Trim().ToLowerInvariant())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .Cast<string>()
            .ToList();

        if (tags.Count > 0)
            return tags;

        return fallbackTags
            .Concat(draft.LookupTags)
            .Select(tag => tag?.Trim())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .Cast<string>()
            .ToList();
    }

    private static OrchestrationNode? ResolveActiveOllamaNode(OrchestrationSettings settings)
    {
        return settings.Ollama.FirstOrDefault(node =>
            node.Enabled &&
            !string.IsNullOrWhiteSpace(node.BaseUrl));
    }

    private static Shop? ResolveStagingShop(string envFilePath, IReadOnlyList<Shop> shops)
    {
        var configuredShopId = ReadOptionalIntEnvValue(envFilePath, "STAGING_SHOP_ID")
            ?? ReadOptionalIntEnvValue(envFilePath, "STAGING_SHOPID");

        if (configuredShopId.HasValue)
            return shops.FirstOrDefault(shop => shop.Id == configuredShopId.Value);

        return shops.FirstOrDefault(shop => string.Equals(shop.Title, "Staging", StringComparison.OrdinalIgnoreCase))
            ?? shops.FirstOrDefault(shop => shop.Title.Contains("staging", StringComparison.OrdinalIgnoreCase))
            ?? shops.FirstOrDefault(shop => string.Equals(shop.SalesChannel, "custom_integration", StringComparison.OrdinalIgnoreCase))
            ?? shops.FirstOrDefault();
    }

    private static Shop? ResolvePublishingShop(string envFilePath, IReadOnlyList<Shop> shops, int stagingShopId)
    {
        var configuredShopId = ReadOptionalIntEnvValue(envFilePath, "PUBLISHING_SHOP_ID")
            ?? ReadOptionalIntEnvValue(envFilePath, "PRICE_UPDATER_SHOP_ID")
            ?? ReadOptionalIntEnvValue(envFilePath, "SHOP_ID")
            ?? ReadOptionalIntEnvValue(envFilePath, "SHOPID");

        if (configuredShopId.HasValue)
        {
            return shops.FirstOrDefault(shop => shop.Id == configuredShopId.Value && shop.Id != stagingShopId);
        }

        return shops.FirstOrDefault(shop => shop.Id != stagingShopId && string.Equals(shop.Title, "Publishing", StringComparison.OrdinalIgnoreCase))
            ?? shops.FirstOrDefault(shop => shop.Id != stagingShopId && string.Equals(shop.Title, "Production", StringComparison.OrdinalIgnoreCase))
            ?? shops.FirstOrDefault(shop => shop.Id != stagingShopId && shop.Title.Contains("publish", StringComparison.OrdinalIgnoreCase))
            ?? shops.FirstOrDefault(shop => shop.Id != stagingShopId && shop.Title.Contains("production", StringComparison.OrdinalIgnoreCase))
            ?? shops.FirstOrDefault(shop => shop.Id != stagingShopId && !string.Equals(shop.SalesChannel, "custom_integration", StringComparison.OrdinalIgnoreCase))
            ?? shops.FirstOrDefault(shop => shop.Id != stagingShopId);
    }

    private static string ReadRequiredEnvValue(string envFilePath, string key)
    {
        var value = ReadOptionalEnvValue(envFilePath, key) ?? Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrWhiteSpace(value))
            return value.Trim();

        throw new InvalidOperationException($"{key} is required in main.env for staged product review.");
    }

    private static string? ReadOptionalEnvValue(string envFilePath, string key)
    {
        if (!File.Exists(envFilePath))
            return null;

        foreach (var line in File.ReadLines(envFilePath))
        {
            if (!line.StartsWith($"{key}=", StringComparison.OrdinalIgnoreCase))
                continue;

            return line[(key.Length + 1)..].Trim();
        }

        return null;
    }

    private static int? ReadOptionalIntEnvValue(string envFilePath, string key)
    {
        var value = ReadOptionalEnvValue(envFilePath, key) ?? Environment.GetEnvironmentVariable(key);
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue)
            ? parsedValue
            : null;
    }

    private static decimal? ReadOptionalDecimalEnvValue(string envFilePath, string key)
    {
        var value = ReadOptionalEnvValue(envFilePath, key) ?? Environment.GetEnvironmentVariable(key);
        return decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue)
            ? parsedValue
            : null;
    }

    private static DateTime ParseCreatedAt(string? createdAt, DateTime fallbackUtc)
    {
        return DateTime.TryParse(
            createdAt,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var parsed)
            ? parsed
            : fallbackUtc;
    }

    private static string FormatLocalTimestamp(DateTime utc)
    {
        return utc == DateTime.MinValue
            ? "Unknown time"
            : utc.ToLocalTime().ToString("dd MMM yyyy HH:mm", CultureInfo.InvariantCulture);
    }

    private static string NormalizeCountryCode(string? configuredCountry)
    {
        return configuredCountry?.Trim().ToUpperInvariant() switch
        {
            "US" or "USA" or "UNITED STATES" => "US",
            "GB" or "UK" or "UNITED KINGDOM" => "GB",
            _ => "US"
        };
    }

    private static string? NormalizeOptionalPath(string? path)
    {
        return string.IsNullOrWhiteSpace(path) ? null : Path.GetFullPath(path.Trim());
    }

    private static string ChooseValue(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
    }

    private static string DescribeFailure(Exception ex)
    {
        var message = ex.GetBaseException().Message.Trim();
        if (string.IsNullOrWhiteSpace(message))
            return "An unknown error occurred while preparing the draft move.";

        return message.EndsWith('.') ? message + " " : message + ".";
    }

    private sealed record StoredDraftRecord(
        MockupDraftRecord Draft,
        string DraftPath,
        DateTime CreatedAtUtc);

    private sealed record ResolvedReviewContext(
        string RepositoryRoot,
        string EnvFilePath,
        PrintifyClient Printify,
        Shop StagingShop,
        Shop PublishingShop,
        OrchestrationNode? ActiveOllamaNode,
        string VisionModel,
        string PromptModel,
        decimal MarginPercent,
        string ShippingCountryCode);

    private sealed record PricingPreview(
        string CurrentLabel,
        string SuggestedLabel,
        string MarginLabel);

    private sealed record GeneratedListingContent(
        string Title,
        string Description,
        List<string> Tags);

    private sealed record GeneratedListingPayload
    {
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public List<string> Tags { get; init; } = new();
    }

    private sealed record PreparedPromptImage(
        string ImagePath,
        bool ShouldDelete);
}

public sealed record SwipeReviewSnapshot(
    int QueueCount,
    SwipeReviewItem? CurrentItem,
    string? StatusMessage,
    SwipeReviewItem? UpcomingItem,
    IReadOnlyList<SwipeReviewItem> UpcomingItems)
{
    public static SwipeReviewSnapshot Empty { get; } = new(
        0,
        null,
        null,
        null,
        Array.Empty<SwipeReviewItem>());
}

public sealed record SwipeReviewItem(
    string ProductId,
    string JobId,
    string ReferenceCode,
    string BlueprintTitle,
    string PrintProviderTitle,
    DateTime CreatedAtUtc,
    string CurrentTitle,
    string CurrentDescription,
    string CurrentPriceLabel,
    string SuggestedPriceLabel,
    string MarginLabel,
    IReadOnlyList<SwipeReviewImage> Images,
    IReadOnlyList<string> Tags,
    bool CanDelete,
    bool CanPromote,
    string? PromoteUnavailableReason)
{
    public IReadOnlyList<SwipeReviewImage> ProductImages => Images
        .Where(image => string.Equals(image.SourceLabel, "Mockup", StringComparison.OrdinalIgnoreCase))
        .ToArray();

    public IReadOnlyList<SwipeReviewImage> SupplementalImages => Images
        .Where(image => !string.Equals(image.SourceLabel, "Mockup", StringComparison.OrdinalIgnoreCase))
        .ToArray();

    public IReadOnlyList<SwipeReviewImage> DisplayImages
    {
        get
        {
            var productImages = ProductImages;
            return productImages.Count > 0 ? productImages : Images;
        }
    }

    public SwipeReviewImage PrimaryImage => DisplayImages.Count == 0 ? SwipeReviewImage.Empty : DisplayImages[0];

    public string CreatedLabel => CreatedAtUtc == DateTime.MinValue
        ? "Unknown time"
        : CreatedAtUtc.ToLocalTime().ToString("dd MMM yyyy HH:mm", CultureInfo.InvariantCulture);
}

public sealed record SwipeReviewImage(
    string Url,
    string AltText,
    string Caption,
    string SourceLabel)
{
    public static SwipeReviewImage Empty { get; } = new(
        string.Empty,
        "No image available",
        "No mockup image available.",
        "Unavailable");
}

public sealed record SwipeReviewActionResult(
    bool Success,
    string Message,
    string? TargetProductId);

public sealed record SwipeReviewActionExecution(
    SwipeReviewActionResult ActionResult,
    SwipeReviewSnapshot Snapshot);