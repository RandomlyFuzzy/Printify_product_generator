using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

sealed class ProductMetadataUpdater
{
    private const string RestOfWorldCountryCode = "REST_OF_THE_WORLD";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Regex JobIdRegex = new(
        @"\b([a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12})\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ArtworkTokenRegex = new(
        @"\bArtwork\s+([A-F0-9]{8})\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ReferenceCodeTokenRegex = new(
        @"\b([A-F0-9]{8})-B\d+-P\d+\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex LookupJobTokenRegex = new(
        @"\bpgj-([a-f0-9]{8})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly PrintifyClient _client;
    private readonly int _stagingShopId;
    private readonly int _publishingShopId;
    private readonly string _draftsRoot;
    private readonly string _dataRoot;
    private readonly PrintifyBlueprintQueryApi _blueprintApi;
    private readonly ProductMetadataUpdaterSettings _settings;
    private readonly Lazy<IReadOnlyDictionary<string, string>> _jobIdByProductId;
    private readonly Lazy<IReadOnlyDictionary<string, ImageLookupEntry>> _imageLookupByJobId;
    private readonly Lazy<IReadOnlyDictionary<string, List<ImageLookupEntry>>> _imageLookupByShortToken;

    public ProductMetadataUpdater(
        PrintifyClient client,
        int stagingShopId,
        int publishingShopId,
        string draftsRoot,
        PrintifyBlueprintQueryApi blueprintApi,
        ProductMetadataUpdaterSettings settings)
    {
        _client = client;
        _stagingShopId = stagingShopId;
        _publishingShopId = publishingShopId;
        _draftsRoot = draftsRoot;
        _dataRoot = ResolveDataRoot(draftsRoot);
        _blueprintApi = blueprintApi;
        _settings = settings;
        _jobIdByProductId = new Lazy<IReadOnlyDictionary<string, string>>(LoadJobIdByProductId);
        _imageLookupByJobId = new Lazy<IReadOnlyDictionary<string, ImageLookupEntry>>(LoadImageLookupByJobId);
        _imageLookupByShortToken = new Lazy<IReadOnlyDictionary<string, List<ImageLookupEntry>>>(LoadImageLookupByShortToken);
    }

    public async Task<ProductMetadataUpdateRunSummary> RunOnceAsync(CancellationToken cancellationToken)
    {
        var isInPlaceMode = _stagingShopId == _publishingShopId;
        var summary = new ProductMetadataUpdateRunSummary
        {
            StartedAt = DateTimeOffset.UtcNow,
            DesiredVariantQuantity = _settings.DesiredVariantQuantity,
            MarginPercent = _settings.MarginPercent,
            ShippingCountryCode = _settings.ShippingCountryCode,
            IsInPlaceMode = isInPlaceMode
        };

        Console.WriteLine();
        Console.WriteLine(isInPlaceMode
            ? $"Starting in-place product update run at {summary.StartedAt:O}."
            : $"Starting draft transfer run at {summary.StartedAt:O}.");

        var draftRecords = LoadDraftQueue(summary);
        if (draftRecords.Count == 0)
        {
            Console.WriteLine($"No draft records were found under {_draftsRoot}.");
            summary.CompletedAt = DateTimeOffset.UtcNow;
            return summary;
        }

        if (isInPlaceMode)
        {
            return await RunInPlaceUpdateAsync(summary, draftRecords, cancellationToken);
        }

        var draftGroups = BuildDraftGroups(draftRecords);
        summary.DraftGroupsDiscovered = draftGroups.Count;

        var filteredGroups = draftGroups
            .Where(group => group.ShouldProcess(_settings.ProductIds))
            .ToList();
        summary.DraftGroupsSkippedByFilter = draftGroups.Count - filteredGroups.Count;

        if (_settings.TransferLimit.HasValue)
        {
            filteredGroups = filteredGroups.Take(_settings.TransferLimit.Value).ToList();
        }

        var stagingProducts = await _client.GetAllProductsAsync(_stagingShopId);
        summary.StagingProductsDiscovered = stagingProducts.Count;
        var stagingProductsById = stagingProducts.ToDictionary(product => product.Id, StringComparer.OrdinalIgnoreCase);

        var publishingProducts = await _client.GetAllProductsAsync(_publishingShopId);
        summary.PublishingProductsDiscovered = publishingProducts.Count;
        var publishingCatalog = new PublishingCatalog(publishingProducts);

        foreach (var group in filteredGroups)
        {
            cancellationToken.ThrowIfCancellationRequested();
            summary.DraftGroupsProcessed++;

            var evaluations = EvaluateGroupCandidates(group, stagingProductsById, stagingProducts, summary);
            if (evaluations.Count == 0)
            {
                summary.DraftGroupsWithoutViableCandidate++;
                Console.WriteLine($"Skipping group {group.DisplayLabel} because no source product was found in the configured source shop for any candidate draft.");
                await DelayIfNeededAsync(cancellationToken);
                continue;
            }

            summary.CandidateDraftsEvaluated += evaluations.Count;

            var existingPublishingProduct = isInPlaceMode
                ? null
                : publishingCatalog.MatchGroup(group.Records, evaluations.Select(evaluation => evaluation.SourceProduct.Id));
            if (existingPublishingProduct is not null)
            {
                summary.DraftGroupsSkippedAlreadyTransferred++;
                Console.WriteLine(
                    $"Skipping group {group.DisplayLabel} because publishing product {existingPublishingProduct.Id} ({ChooseValue(existingPublishingProduct.Title, "Untitled product")}) already matches one of its strong lookup markers.");
                await DelayIfNeededAsync(cancellationToken);
                continue;
            }

            var chosen = ChooseBestCandidate(evaluations);
            summary.DraftGroupsSelectedForTransfer++;
            summary.ProductsWithUnsupportedQuantityRequest++;

            PrintCandidateRanking(group, evaluations, chosen);

            var desiredMetadata = ResolveDesiredMetadata(chosen.SourceProduct, chosen.Record.Draft);
            var variantUpdates = BuildVariantUpdates(chosen);
            summary.VariantsPriced += variantUpdates.Count;

            Console.WriteLine($"  Requested quantity {_settings.DesiredVariantQuantity} was not applied because Printify product updates do not expose writable variant inventory quantities.");

            if (!_settings.ApplyChanges)
            {
                summary.TransfersPlanned++;

                if (!isInPlaceMode)
                {
                    summary.StagingProductsPlannedForRemoval += evaluations
                        .Select(evaluation => evaluation.SourceProduct.Id)
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .Count();
                    summary.DraftRecordsPlannedForRemoval += evaluations.Count;
                }

                Console.WriteLine(isInPlaceMode
                    ? "  Dry run: the selected source product would be updated in place. No clone or cleanup would be performed."
                    : "  Dry run: the selected draft would be transferred as a draft only, then the staging group would be cleaned up.");
                await DelayIfNeededAsync(cancellationToken);
                continue;
            }

            ProductTransferResult? transfer = null;

            try
            {
                if (isInPlaceMode)
                {
                    await _client.UpdateProductAsync(_publishingShopId, chosen.SourceProduct.Id, new UpdateProductRequest
                    {
                        Title = desiredMetadata.Title,
                        Description = desiredMetadata.Description,
                        Tags = desiredMetadata.Tags.Count > 0 ? desiredMetadata.Tags : chosen.SourceProduct.Tags,
                        Variants = variantUpdates.Count > 0 ? variantUpdates : null
                    });

                    summary.TransfersCompleted++;
                    Console.WriteLine($"  Updated source product {chosen.SourceProduct.Id} in place for draft {chosen.Record.Draft.ProductId}. No clone or cleanup was performed.");
                }
                else
                {
                    transfer = await _client.TransferProductAsync(
                        _stagingShopId,
                        chosen.SourceProduct.Id,
                        _publishingShopId,
                        deleteSourceProduct: false,
                        publishTargetProduct: false);

                    await _client.UpdateProductAsync(_publishingShopId, transfer.TargetProductId, new UpdateProductRequest
                    {
                        Title = desiredMetadata.Title,
                        Description = desiredMetadata.Description,
                        Tags = desiredMetadata.Tags.Count > 0 ? desiredMetadata.Tags : transfer.TargetProduct.Tags,
                        Variants = variantUpdates.Count > 0 ? variantUpdates : null
                    });

                    summary.TransfersCompleted++;
                    Console.WriteLine($"  Transferred source product {chosen.SourceProduct.Id} for draft {chosen.Record.Draft.ProductId} to publishing as draft {transfer.TargetProductId} without publishing.");

                    await CleanupResolvedGroupAsync(group, evaluations, chosen, summary);
                }
            }
            catch (Exception ex)
            {
                summary.TransferFailures++;
                Console.Error.WriteLine(isInPlaceMode
                    ? $"  Failed to update group {group.DisplayLabel} in place: {DescribeFailure(ex)}"
                    : $"  Failed to transfer group {group.DisplayLabel}: {DescribeFailure(ex)}");

                if (!isInPlaceMode && !string.IsNullOrWhiteSpace(transfer?.TargetProductId))
                {
                    await TryDeleteTargetCloneAsync(transfer.TargetProductId, summary);
                }
            }

            await DelayIfNeededAsync(cancellationToken);
        }

        summary.CompletedAt = DateTimeOffset.UtcNow;
        return summary;
    }

    private async Task<ProductMetadataUpdateRunSummary> RunInPlaceUpdateAsync(
        ProductMetadataUpdateRunSummary summary,
        IReadOnlyList<StoredDraftRecord> draftRecords,
        CancellationToken cancellationToken)
    {
        var products = await _client.GetAllProductsAsync(_publishingShopId);
        summary.InPlaceProductsDiscovered = products.Count;
        summary.PublishingProductsDiscovered = products.Count;

        var orderedProducts = products
            .OrderBy(product => product.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(product => product.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (_settings.TransferLimit.HasValue)
        {
            orderedProducts = orderedProducts.Take(_settings.TransferLimit.Value).ToList();
        }

        foreach (var product in orderedProducts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var matchedDraft = MatchDraftForProduct(product, draftRecords);
            var usedSyntheticMetadata = false;
            if (matchedDraft is null)
            {
                matchedDraft = TryBuildSyntheticDraftRecord(product);
                usedSyntheticMetadata = matchedDraft is not null;
            }

            if (!ShouldProcessInPlaceProduct(product, matchedDraft))
            {
                summary.InPlaceProductsSkippedByFilter++;
                continue;
            }

            summary.InPlaceProductsProcessed++;

            if (matchedDraft is null)
            {
                summary.InPlaceProductsWithoutDraftMatch++;
                Console.WriteLine($"Skipping product {product.Id} ({ChooseValue(product.Title, "Untitled product")}) because no metadata source was found for in-place update.");
                await DelayIfNeededAsync(cancellationToken);
                continue;
            }

            summary.InPlaceProductsMatched++;
            if (usedSyntheticMetadata)
            {
                summary.InPlaceProductsMatchedFromFallback++;
            }

            var desiredMetadata = ResolveDesiredMetadata(product, matchedDraft.Draft);
            var variantUpdates = BuildVariantUpdates(EvaluateCandidate(matchedDraft, product));
            var hasChanges = HasInPlaceChanges(product, desiredMetadata, variantUpdates);

            Console.WriteLine(usedSyntheticMetadata
                ? $"Reconstructed metadata for product {product.Id} ({ChooseValue(product.Title, "Untitled product")}) from lookup data for job {matchedDraft.Draft.JobId}."
                : $"Matched product {product.Id} ({ChooseValue(product.Title, "Untitled product")}) to draft {matchedDraft.Draft.ProductId} for in-place update.");
            Console.WriteLine($"  Requested quantity {_settings.DesiredVariantQuantity} was not applied because Printify product updates do not expose writable variant inventory quantities.");

            if (!hasChanges)
            {
                summary.InPlaceProductsUnchanged++;
                Console.WriteLine("  No metadata or price changes are needed.");
                await DelayIfNeededAsync(cancellationToken);
                continue;
            }

            summary.ProductsWithUnsupportedQuantityRequest++;
            summary.VariantsPriced += variantUpdates.Count;

            if (!_settings.ApplyChanges)
            {
                summary.TransfersPlanned++;
                Console.WriteLine("  Dry run: product would be updated in place.");
                await DelayIfNeededAsync(cancellationToken);
                continue;
            }

            try
            {
                await _client.UpdateProductAsync(_publishingShopId, product.Id, new UpdateProductRequest
                {
                    Title = desiredMetadata.Title,
                    Description = desiredMetadata.Description,
                    Tags = desiredMetadata.Tags.Count > 0 ? desiredMetadata.Tags : product.Tags,
                    Variants = variantUpdates.Count > 0 ? variantUpdates : null
                });

                summary.TransfersCompleted++;
                Console.WriteLine($"  Updated product {product.Id} in place.");
            }
            catch (Exception ex)
            {
                summary.TransferFailures++;
                Console.Error.WriteLine($"  Failed to update product {product.Id} in place: {DescribeFailure(ex)}");
            }

            await DelayIfNeededAsync(cancellationToken);
        }

        summary.CompletedAt = DateTimeOffset.UtcNow;
        return summary;
    }

    private List<StoredDraftRecord> LoadDraftQueue(ProductMetadataUpdateRunSummary summary)
    {
        var drafts = new List<StoredDraftRecord>();

        if (!Directory.Exists(_draftsRoot))
        {
            return drafts;
        }

        foreach (var filePath in Directory.EnumerateFiles(_draftsRoot, "*.json", SearchOption.AllDirectories))
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var draft = JsonSerializer.Deserialize<MockupDraftRecord>(json, JsonOptions);
                if (draft is null || string.IsNullOrWhiteSpace(draft.ProductId))
                {
                    summary.DraftLoadFailures++;
                    continue;
                }

                drafts.Add(new StoredDraftRecord(
                    Draft: draft,
                    DraftPath: filePath,
                    CreatedAtUtc: ParseCreatedAt(draft.CreatedAt, File.GetLastWriteTimeUtc(filePath))));
                summary.DraftRecordsLoaded++;
            }
            catch (Exception ex)
            {
                summary.DraftLoadFailures++;
                Console.Error.WriteLine($"Failed to load draft record {filePath}: {ex.Message}");
            }
        }

        return drafts
            .OrderByDescending(record => record.CreatedAtUtc)
            .ThenBy(record => record.Draft.ProductId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<DraftGroup> BuildDraftGroups(IReadOnlyList<StoredDraftRecord> draftRecords)
    {
        return draftRecords
            .GroupBy(record => ResolveGroupIdentity(record.Draft), StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var orderedRecords = group
                    .OrderByDescending(record => record.CreatedAtUtc)
                    .ThenBy(record => record.Draft.ProductId, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var displayLabel = ChooseValue(
                    orderedRecords.Select(record => record.Draft.ReferenceCode).ToArray())
                    ;
                if (string.IsNullOrWhiteSpace(displayLabel))
                {
                    displayLabel = group.Key;
                }

                return new DraftGroup(
                    GroupId: group.Key,
                    DisplayLabel: string.IsNullOrWhiteSpace(displayLabel) ? group.Key : displayLabel,
                    CreatedAtUtc: orderedRecords.Max(record => record.CreatedAtUtc),
                    Records: orderedRecords);
            })
            .OrderByDescending(group => group.CreatedAtUtc)
            .ThenBy(group => group.GroupId, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private List<DraftCandidateEvaluation> EvaluateGroupCandidates(
        DraftGroup group,
        IReadOnlyDictionary<string, Product> stagingProductsById,
        IReadOnlyList<Product> stagingProducts,
        ProductMetadataUpdateRunSummary summary)
    {
        var evaluations = new List<DraftCandidateEvaluation>(group.Records.Count);

        foreach (var record in group.Records)
        {
            var sourceProduct = ResolveSourceProduct(record, stagingProductsById, stagingProducts);
            if (sourceProduct is null)
            {
                summary.CandidateDraftsMissingFromStaging++;
                Console.WriteLine($"  Candidate {record.Draft.ProductId} was not found in the configured source shop, and no strong blueprint/provider marker match could be recovered.");
                continue;
            }

            if (!string.Equals(sourceProduct.Id, record.Draft.ProductId, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"  Candidate {record.Draft.ProductId} resolved to source product {sourceProduct.Id} via strong lookup markers.");
            }

            var evaluation = EvaluateCandidate(record, sourceProduct);
            evaluations.Add(evaluation);

            switch (evaluation.Availability)
            {
                case PricingAvailability.DeliveredCost:
                    summary.CandidateDraftsWithDeliveredCost++;
                    break;
                case PricingAvailability.ProductionOnly:
                    summary.CandidateDraftsWithProductionOnlyCost++;
                    break;
                default:
                    summary.CandidateDraftsWithoutPricing++;
                    break;
            }
        }

        return evaluations;
    }

    private static Product? ResolveSourceProduct(
        StoredDraftRecord record,
        IReadOnlyDictionary<string, Product> stagingProductsById,
        IReadOnlyList<Product> stagingProducts)
    {
        if (stagingProductsById.TryGetValue(record.Draft.ProductId, out var exactMatch))
        {
            return exactMatch;
        }

        var sameBlueprintProviderMatch = stagingProducts
            .Where(product => product.BlueprintId == record.Draft.BlueprintId && product.PrintProviderId == record.Draft.PrintProviderId)
            .Select(product => new
            {
                Product = product,
                Score = GetStrongMatchScore(record.Draft, product)
            })
            .Where(candidate => candidate.Score > 0)
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Product.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (sameBlueprintProviderMatch is not null)
        {
            return sameBlueprintProviderMatch.Product;
        }

        return null;
    }

    private DraftCandidateEvaluation EvaluateCandidate(StoredDraftRecord record, Product sourceProduct)
    {
        PrintifyCachedBlueprintProviderDetail? providerDetail = null;

        try
        {
            providerDetail = _blueprintApi
                .GetProviders(sourceProduct.BlueprintId)
                .FirstOrDefault(provider => provider.Provider.Id == sourceProduct.PrintProviderId);
        }
        catch
        {
            providerDetail = null;
        }

        var cachedVariantsById = providerDetail?.Variants.Variants.ToDictionary(variant => variant.Id)
            ?? new Dictionary<int, Variant>();
        var shippingLookup = providerDetail is null
            ? new Dictionary<int, ShippingQuoteSummary>()
            : BuildCountryShippingLookup(providerDetail.Shipping, _settings.ShippingCountryCode);

        var selectedVariants = SelectReviewVariants(sourceProduct);
        var pricing = new List<VariantPricingDecision>(selectedVariants.Count);

        foreach (var variant in selectedVariants)
        {
            cachedVariantsById.TryGetValue(variant.Id, out var cachedVariant);
            var productionCost = variant.Cost > 0
                ? variant.Cost
                : cachedVariant?.Cost;

            var hasShippingPrice = shippingLookup.TryGetValue(variant.Id, out var shippingSummary)
                && shippingSummary.FirstItemCost.HasValue;
            var shippingCost = hasShippingPrice ? shippingSummary.FirstItemCost!.Value : 0;

            var suggestedPrice = productionCost.HasValue && productionCost.Value > 0
                ? CalculateAppealingPrice(productionCost.Value, shippingCost, _settings.MarginPercent)
                : Math.Max(0, variant.Price);

            pricing.Add(new VariantPricingDecision(
                VariantId: variant.Id,
                IsEnabled: variant.IsEnabled,
                CurrentPrice: variant.Price,
                ProductionCost: productionCost,
                HasShippingPrice: hasShippingPrice,
                ShippingCost: shippingCost,
                SuggestedPrice: suggestedPrice));
        }

        return new DraftCandidateEvaluation(record, sourceProduct, pricing);
    }

    private static DraftCandidateEvaluation ChooseBestCandidate(IReadOnlyList<DraftCandidateEvaluation> evaluations)
    {
        return evaluations
            .OrderBy(evaluation => evaluation.Availability)
            .ThenBy(evaluation => evaluation.AverageDeliveredCost ?? evaluation.AverageProductionCost ?? decimal.MaxValue)
            .ThenBy(evaluation => evaluation.AverageShippingCost ?? decimal.MaxValue)
            .ThenByDescending(evaluation => evaluation.SelectedVariantCount)
            .ThenByDescending(evaluation => evaluation.Record.CreatedAtUtc)
            .ThenBy(evaluation => evaluation.Record.Draft.ProductId, StringComparer.OrdinalIgnoreCase)
            .First();
    }

    private StoredDraftRecord? MatchDraftForProduct(Product product, IReadOnlyList<StoredDraftRecord> draftRecords)
    {
        return draftRecords
            .Select(record => new
            {
                Record = record,
                Score = GetInPlaceDraftMatchScore(record.Draft, product)
            })
            .Where(candidate => candidate.Score > 0)
            .OrderByDescending(candidate => candidate.Score)
            .ThenByDescending(candidate => candidate.Record.CreatedAtUtc)
            .FirstOrDefault()
            ?.Record;
    }

    private StoredDraftRecord? TryBuildSyntheticDraftRecord(Product product)
    {
        if (!TryResolveSyntheticJob(product, out var jobId, out var imagePath))
        {
            return null;
        }

        var blueprintTitle = ResolveBlueprintTitle(product);
        var providerTitle = ResolveProviderTitle(product);
        var createdAtUtc = ParseCreatedAt(product.CreatedAt, DateTime.UtcNow);
        var bundle = ListingContentBuilder.Build(new ListingContentContext
        {
            JobId = jobId,
            ImagePath = imagePath,
            BlueprintId = product.BlueprintId,
            BlueprintTitle = blueprintTitle,
            PrintProviderId = product.PrintProviderId,
            PrintProviderTitle = providerTitle,
            LlmReason = string.Empty
        });

        return new StoredDraftRecord(
            new MockupDraftRecord
            {
                ProductId = product.Id,
                JobId = bundle.Lookup.JobId,
                LookupKey = bundle.Lookup.LookupKey,
                GroupKey = bundle.Lookup.GroupKey,
                AssetKey = bundle.Lookup.AssetKey,
                ReferenceCode = bundle.Lookup.ReferenceCode,
                LocalImagePath = imagePath,
                BlueprintId = product.BlueprintId,
                BlueprintTitle = blueprintTitle,
                PrintProviderId = product.PrintProviderId,
                PrintProviderTitle = providerTitle,
                LookupTags = new List<string>(bundle.Lookup.Tags),
                ChannelContent = bundle.Channels,
                CreatedAt = createdAtUtc.ToString("O", CultureInfo.InvariantCulture)
            },
            DraftPath: $"[synthetic:{jobId}]",
            CreatedAtUtc: createdAtUtc);
    }

    private bool ShouldProcessInPlaceProduct(Product product, StoredDraftRecord? matchedDraft)
    {
        if (_settings.ProductIds.Count == 0)
        {
            return true;
        }

        if (_settings.ProductIds.Contains(product.Id))
        {
            return true;
        }

        return matchedDraft is not null && _settings.ProductIds.Contains(matchedDraft.Draft.ProductId);
    }

    private bool TryResolveSyntheticJob(Product product, out string jobId, out string imagePath)
    {
        if (_jobIdByProductId.Value.TryGetValue(product.Id, out var mappedJobId) &&
            TryResolveImageLookupByJobId(mappedJobId, out var directEntry))
        {
            jobId = mappedJobId;
            imagePath = ResolveImagePath(directEntry);
            return !string.IsNullOrWhiteSpace(imagePath);
        }

        if (TryExtractJobIdFromProduct(product, out jobId) &&
            TryResolveImageLookupByJobId(jobId, out var exactEntry))
        {
            imagePath = ResolveImagePath(exactEntry);
            return !string.IsNullOrWhiteSpace(imagePath);
        }

        if (TryExtractArtworkToken(product, out var shortToken) &&
            TryResolveImageLookupByShortToken(shortToken, out var tokenJobId, out var tokenEntry))
        {
            jobId = tokenJobId;
            imagePath = ResolveImagePath(tokenEntry);
            return !string.IsNullOrWhiteSpace(imagePath);
        }

        jobId = string.Empty;
        imagePath = string.Empty;
        return false;
    }

    private bool TryResolveImageLookupByJobId(string jobId, out ImageLookupEntry entry)
    {
        return _imageLookupByJobId.Value.TryGetValue(jobId.Trim(), out entry!);
    }

    private bool TryResolveImageLookupByShortToken(string shortToken, out string jobId, out ImageLookupEntry entry)
    {
        if (_imageLookupByShortToken.Value.TryGetValue(shortToken.Trim(), out var entries) && entries.Count > 0)
        {
            entry = entries
                .OrderByDescending(candidate => ParseCreatedAt(candidate.UploadedAt, DateTime.MinValue))
                .First();

            jobId = ExtractJobIdFromLookupEntry(entry);
            return !string.IsNullOrWhiteSpace(jobId);
        }

        jobId = string.Empty;
        entry = new ImageLookupEntry();
        return false;
    }

    private static bool TryExtractJobIdFromProduct(Product product, out string jobId)
    {
        var searchable = $"{product.Title}\n{product.Description}\n{string.Join(' ', product.Tags)}";
        var match = JobIdRegex.Match(searchable);
        if (match.Success)
        {
            jobId = match.Groups[1].Value.ToLowerInvariant();
            return true;
        }

        jobId = string.Empty;
        return false;
    }

    private static bool TryExtractArtworkToken(Product product, out string shortToken)
    {
        var searchable = $"{product.Title}\n{product.Description}\n{string.Join(' ', product.Tags)}";

        foreach (var regex in new[] { ArtworkTokenRegex, ReferenceCodeTokenRegex, LookupJobTokenRegex })
        {
            var match = regex.Match(searchable);
            if (match.Success)
            {
                shortToken = match.Groups[1].Value.ToLowerInvariant();
                return true;
            }
        }

        shortToken = string.Empty;
        return false;
    }

    private string ResolveBlueprintTitle(Product product)
    {
        if (_blueprintApi.TryGetBlueprintDetail(product.BlueprintId, out var detail) &&
            !string.IsNullOrWhiteSpace(detail.Blueprint.Title))
        {
            return detail.Blueprint.Title.Trim();
        }

        var separatorIndex = product.Title.IndexOf("| Artwork", StringComparison.OrdinalIgnoreCase);
        if (separatorIndex > 0)
        {
            return product.Title[..separatorIndex].Trim();
        }

        return $"Blueprint {product.BlueprintId}";
    }

    private string ResolveProviderTitle(Product product)
    {
        if (_blueprintApi.TryGetBlueprintDetail(product.BlueprintId, out var detail))
        {
            var providerTitle = detail.PrintProviders
                .FirstOrDefault(provider => provider.Provider.Id == product.PrintProviderId)
                ?.Provider
                .Title;

            if (!string.IsNullOrWhiteSpace(providerTitle))
            {
                return providerTitle.Trim();
            }
        }

        return $"Provider {product.PrintProviderId}";
    }

    private void PrintCandidateRanking(
        DraftGroup group,
        IReadOnlyList<DraftCandidateEvaluation> evaluations,
        DraftCandidateEvaluation chosen)
    {
        Console.WriteLine($"Group {group.DisplayLabel} has {evaluations.Count} source candidate(s). Selecting the cheapest GB-delivered option from cache data where available.");

        foreach (var evaluation in evaluations
            .OrderBy(candidate => candidate.Availability)
            .ThenBy(candidate => candidate.AverageDeliveredCost ?? candidate.AverageProductionCost ?? decimal.MaxValue)
            .ThenBy(candidate => candidate.Record.Draft.ProductId, StringComparer.OrdinalIgnoreCase))
        {
            var marker = string.Equals(evaluation.Record.Draft.ProductId, chosen.Record.Draft.ProductId, StringComparison.OrdinalIgnoreCase)
                ? "*"
                : "-";

            Console.WriteLine(
                $"  {marker} {evaluation.Record.Draft.ProductId} | {ChooseValue(evaluation.Record.Draft.BlueprintTitle, evaluation.SourceProduct.Title, "Untitled product")} | {ChooseValue(evaluation.Record.Draft.PrintProviderTitle, evaluation.SourceProduct.PrintProviderId.ToString(CultureInfo.InvariantCulture))} | {evaluation.DescribePricing()}");
        }
    }

    private List<CreateProductVariant> BuildVariantUpdates(DraftCandidateEvaluation chosen)
    {
        return chosen.Pricing
            .Select(variant => new CreateProductVariant
            {
                Id = variant.VariantId,
                IsEnabled = variant.IsEnabled,
                Price = variant.SuggestedPrice > 0 ? variant.SuggestedPrice : Math.Max(0, variant.CurrentPrice)
            })
            .ToList();
    }

    private static bool HasInPlaceChanges(
        Product product,
        ResolvedMetadata desiredMetadata,
        IReadOnlyList<CreateProductVariant> variantUpdates)
    {
        var currentTitle = product.Title?.Trim() ?? string.Empty;
        var desiredTitle = desiredMetadata.Title?.Trim() ?? string.Empty;
        if (!string.Equals(currentTitle, desiredTitle, StringComparison.Ordinal))
        {
            return true;
        }

        var currentDescription = product.Description?.Trim() ?? string.Empty;
        var desiredDescription = desiredMetadata.Description?.Trim() ?? string.Empty;
        if (!string.Equals(currentDescription, desiredDescription, StringComparison.Ordinal))
        {
            return true;
        }

        var currentTags = NormalizeTags(product.Tags)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var desiredTags = NormalizeTags(desiredMetadata.Tags)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (!currentTags.SequenceEqual(desiredTags, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        var variantsById = product.Variants.ToDictionary(variant => variant.Id);
        foreach (var update in variantUpdates)
        {
            if (!variantsById.TryGetValue(update.Id, out var existingVariant))
            {
                return true;
            }

            if (existingVariant.Price != update.Price || existingVariant.IsEnabled != update.IsEnabled)
            {
                return true;
            }
        }

        return false;
    }

    private async Task CleanupResolvedGroupAsync(
        DraftGroup group,
        IReadOnlyList<DraftCandidateEvaluation> evaluations,
        DraftCandidateEvaluation chosen,
        ProductMetadataUpdateRunSummary summary)
    {
        var removedSourceProductIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var evaluation in evaluations)
        {
            var role = string.Equals(evaluation.Record.Draft.ProductId, chosen.Record.Draft.ProductId, StringComparison.OrdinalIgnoreCase)
                ? "selected"
                : "rejected";

            if (removedSourceProductIds.Add(evaluation.SourceProduct.Id))
            {
                await DeleteStagingProductSafeAsync(evaluation.SourceProduct.Id, role, summary);
            }

            DeleteDraftRecordSafe(evaluation.Record.DraftPath, evaluation.Record.Draft.ProductId, role, summary);
        }

        Console.WriteLine($"  Cleaned up staging group {group.DisplayLabel} after transfer.");
    }

    private async Task DeleteStagingProductSafeAsync(
        string productId,
        string role,
        ProductMetadataUpdateRunSummary summary)
    {
        try
        {
            await _client.DeleteProductAsync(_stagingShopId, productId);
            summary.StagingProductsRemoved++;
            Console.WriteLine($"  Removed {role} staging product {productId}.");
        }
        catch (PrintifyApiException ex) when (ex.StatusCode == 404)
        {
            summary.StagingProductsRemoved++;
            Console.WriteLine($"  {role} staging product {productId} was already gone.");
        }
        catch (Exception ex)
        {
            summary.StagingProductRemovalFailures++;
            Console.Error.WriteLine($"  Failed to remove {role} staging product {productId}: {ex.Message}");
        }
    }

    private static void DeleteDraftRecordSafe(
        string draftPath,
        string productId,
        string role,
        ProductMetadataUpdateRunSummary summary)
    {
        try
        {
            if (File.Exists(draftPath))
            {
                File.Delete(draftPath);
            }

            summary.DraftRecordsRemoved++;
            Console.WriteLine($"  Removed {role} draft record for {productId}.");
        }
        catch (Exception ex)
        {
            summary.DraftRecordRemovalFailures++;
            Console.Error.WriteLine($"  Failed to remove {role} draft record for {productId}: {ex.Message}");
        }
    }

    private async Task TryDeleteTargetCloneAsync(string targetProductId, ProductMetadataUpdateRunSummary summary)
    {
        try
        {
            await _client.DeleteProductAsync(_publishingShopId, targetProductId);
            Console.WriteLine($"  Removed failed target draft clone {targetProductId}.");
        }
        catch (PrintifyApiException ex) when (ex.StatusCode == 404)
        {
            // The failed clone is already gone.
        }
        catch (Exception ex)
        {
            summary.TargetCloneCleanupFailures++;
            Console.Error.WriteLine($"  Failed to remove target draft clone {targetProductId}: {ex.Message}");
        }
    }

    private ResolvedMetadata ResolveDesiredMetadata(Product product, MockupDraftRecord draft)
    {
        var channelContent = ResolveChannelContent(draft, _settings.MetadataChannel);

        if (!HasUsableListingContent(channelContent))
        {
            var rebuilt = ListingContentBuilder.Build(new ListingContentContext
            {
                JobId = draft.JobId,
                ImagePath = draft.LocalImagePath,
                BlueprintId = draft.BlueprintId,
                BlueprintTitle = draft.BlueprintTitle,
                PrintProviderId = draft.PrintProviderId,
                PrintProviderTitle = draft.PrintProviderTitle,
                LlmReason = draft.LlmReason
            });

            channelContent = ListingContentBuilder.ResolveChannel(rebuilt, _settings.MetadataChannel);
        }

        return new ResolvedMetadata(
            Title: ChooseValue(channelContent.Title, product.Title, draft.BlueprintTitle, "Untitled product"),
            Description: ChooseValue(channelContent.Description, product.Description, draft.LlmReason, "No description available."),
            Tags: ResolveTags(channelContent, product, draft).ToList());
    }

    private static ListingChannelContent ResolveChannelContent(MockupDraftRecord draft, string selectedChannel)
    {
        if (draft.ChannelContent.TryGetValue(selectedChannel, out var configuredContent))
        {
            return configuredContent;
        }

        if (draft.ChannelContent.TryGetValue("printify", out var printifyContent))
        {
            return printifyContent;
        }

        if (draft.ChannelContent.TryGetValue("generic", out var genericContent))
        {
            return genericContent;
        }

        return draft.ChannelContent.Values.FirstOrDefault() ?? new ListingChannelContent();
    }

    private static bool HasUsableListingContent(ListingChannelContent channelContent)
    {
        return !string.IsNullOrWhiteSpace(channelContent.Title)
            || !string.IsNullOrWhiteSpace(channelContent.Description)
            || channelContent.Tags.Any(tag => !string.IsNullOrWhiteSpace(tag));
    }

    private static IReadOnlyList<string> ResolveTags(ListingChannelContent channelContent, Product product, MockupDraftRecord draft)
    {
        var prioritizedTags = new List<string>();

        AddTags(prioritizedTags, channelContent.Tags);

        if (draft.ChannelContent.TryGetValue("generic", out var genericContent))
        {
            AddTags(prioritizedTags, genericContent.Tags);
        }

        if (draft.ChannelContent.TryGetValue("printify", out var printifyContent))
        {
            AddTags(prioritizedTags, printifyContent.Tags);
        }

        AddTags(prioritizedTags, product.Tags);
        AddTags(prioritizedTags, draft.LookupTags);
        AddTags(prioritizedTags, BuildFallbackTagCandidates(product, draft));

        return prioritizedTags
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();
    }

    private static void AddTags(List<string> target, IEnumerable<string>? tags)
    {
        foreach (var tag in NormalizeTags(tags))
        {
            target.Add(NormalizeTag(tag));
        }
    }

    private static IEnumerable<string> BuildFallbackTagCandidates(Product product, MockupDraftRecord draft)
    {
        var phrases = new List<string>
        {
            draft.BlueprintTitle,
            draft.PrintProviderTitle,
            product.Title,
            "made to order",
            "print on demand",
            "original artwork",
            "gift idea",
            "art gift",
            "custom decor",
            "statement piece"
        };

        foreach (var phrase in phrases)
        {
            var normalizedPhrase = NormalizeTag(phrase);
            if (!string.IsNullOrWhiteSpace(normalizedPhrase))
            {
                yield return normalizedPhrase;
            }

            foreach (var token in ExtractTagTokens(phrase))
            {
                yield return token;
            }
        }
    }

    private static IEnumerable<string> ExtractTagTokens(string? phrase)
    {
        if (string.IsNullOrWhiteSpace(phrase))
        {
            yield break;
        }

        var tokens = phrase
            .ToLowerInvariant()
            .Split(new[] { ' ', '|', ',', ';', ':', '-', '_', '/', '\\', '(', ')', '[', ']' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => token.Length >= 3 && token.All(char.IsLetterOrDigit))
            .Take(8);

        foreach (var token in tokens)
        {
            yield return token;
        }
    }

    private static string NormalizeTag(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = string.Join(' ', value
            .Trim()
            .ToLowerInvariant()
            .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        return normalized.Length <= 48
            ? normalized
            : normalized[..48].TrimEnd();
    }

    private static List<ProductVariant> SelectReviewVariants(Product product)
    {
        var enabledVariants = product.Variants.Where(variant => variant.IsEnabled).ToList();
        return enabledVariants.Count > 0 ? enabledVariants : product.Variants.ToList();
    }

    private static int CalculateAppealingPrice(int productionPrice, int shippingPrice, decimal marginPercent)
    {
        if (productionPrice <= 0)
        {
            return Math.Max(0, productionPrice);
        }

        var costFloor = (productionPrice + Math.Max(0, shippingPrice)) / 100m;
        var targetRetail = costFloor * (1m + (marginPercent / 100m));
        var roundedRetail = RoundRetailPrice(targetRetail);

        if (roundedRetail < costFloor)
        {
            roundedRetail = costFloor;
        }

        return checked((int)Math.Ceiling(roundedRetail * 100m));
    }

    private static decimal RoundRetailPrice(decimal amount)
    {
        if (amount <= 0)
        {
            return 0m;
        }

        var ending = amount < 10m ? 0.99m : amount < 50m ? 0.49m : 0.99m;
        var rounded = Math.Floor(amount) + ending;
        if (rounded < amount)
        {
            rounded += 1m;
        }

        return decimal.Round(rounded, 2, MidpointRounding.AwayFromZero);
    }

    private static Dictionary<int, ShippingQuoteSummary> BuildCountryShippingLookup(ShippingInfo shipping, string countryCode)
    {
        var exactLookup = new Dictionary<int, ShippingQuoteSummary>();
        var restOfWorldLookup = new Dictionary<int, ShippingQuoteSummary>();

        foreach (var profile in shipping.Profiles)
        {
            if (ProfileContainsCountry(profile, countryCode))
            {
                AddShippingProfile(exactLookup, profile);
                continue;
            }

            if (ProfileContainsRestOfWorld(profile))
            {
                AddShippingProfile(restOfWorldLookup, profile);
            }
        }

        foreach (var entry in restOfWorldLookup)
        {
            if (!exactLookup.ContainsKey(entry.Key))
            {
                exactLookup[entry.Key] = entry.Value;
            }
        }

        return exactLookup;
    }

    private static void AddShippingProfile(Dictionary<int, ShippingQuoteSummary> lookup, ShippingProfile profile)
    {
        var candidate = new ShippingQuoteSummary(
            profile.FirstItem?.Cost,
            profile.AdditionalItems?.Cost,
            profile.FirstItem?.Currency ?? profile.AdditionalItems?.Currency ?? string.Empty);

        foreach (var variantId in profile.VariantIds)
        {
            if (!lookup.TryGetValue(variantId, out var existing) || CompareShipping(candidate, existing) < 0)
            {
                lookup[variantId] = candidate;
            }
        }
    }

    private static bool ProfileContainsCountry(ShippingProfile profile, string countryCode)
    {
        return profile.Countries.Contains(countryCode, StringComparer.OrdinalIgnoreCase);
    }

    private static bool ProfileContainsRestOfWorld(ShippingProfile profile)
    {
        return profile.Countries.Any(IsRestOfWorldCountryCode);
    }

    private static bool IsRestOfWorldCountryCode(string? countryCode)
    {
        return string.Equals(NormalizeShippingCountryCode(countryCode), RestOfWorldCountryCode, StringComparison.Ordinal);
    }

    private static string NormalizeShippingCountryCode(string? countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode))
        {
            return string.Empty;
        }

        return string.Join('_', countryCode
            .Trim()
            .Split(new[] { '-', '_', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            .ToUpperInvariant();
    }

    private static int CompareShipping(ShippingQuoteSummary left, ShippingQuoteSummary right)
    {
        var leftCost = left.FirstItemCost ?? int.MaxValue;
        var rightCost = right.FirstItemCost ?? int.MaxValue;
        var costComparison = leftCost.CompareTo(rightCost);
        if (costComparison != 0)
        {
            return costComparison;
        }

        var leftAdditional = left.AdditionalItemCost ?? int.MaxValue;
        var rightAdditional = right.AdditionalItemCost ?? int.MaxValue;
        var additionalComparison = leftAdditional.CompareTo(rightAdditional);
        if (additionalComparison != 0)
        {
            return additionalComparison;
        }

        return string.Compare(left.Currency, right.Currency, StringComparison.OrdinalIgnoreCase);
    }

    private async Task DelayIfNeededAsync(CancellationToken cancellationToken)
    {
        if (_settings.RequestDelayMs > 0)
        {
            await Task.Delay(_settings.RequestDelayMs, cancellationToken);
        }
    }

    private static IEnumerable<string> NormalizeTags(IEnumerable<string>? tags)
    {
        return (tags ?? Array.Empty<string>())
            .Select(tag => tag?.Trim())
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Cast<string>();
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

    private static string ResolveDataRoot(string draftsRoot)
    {
        var stagingDirectory = Directory.GetParent(Path.GetFullPath(draftsRoot))?.Parent;
        return stagingDirectory?.FullName ?? Path.GetFullPath(draftsRoot);
    }

    private IReadOnlyDictionary<string, string> LoadJobIdByProductId()
    {
        var uploads = UploadedJobProductsStore.Load(_dataRoot);
        var reverseLookup = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in uploads)
        {
            foreach (var productId in entry.Value)
            {
                if (!string.IsNullOrWhiteSpace(productId))
                {
                    reverseLookup[productId.Trim()] = entry.Key.Trim();
                }
            }
        }

        return reverseLookup;
    }

    private IReadOnlyDictionary<string, ImageLookupEntry> LoadImageLookupByJobId()
    {
        var lookupPath = Path.Combine(_dataRoot, "staging", "lookup.json");
        if (!File.Exists(lookupPath))
        {
            return new Dictionary<string, ImageLookupEntry>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var json = File.ReadAllText(lookupPath);
            var keyedEntries = JsonSerializer.Deserialize<Dictionary<string, ImageLookupEntry>>(json, JsonOptions)
                ?? new Dictionary<string, ImageLookupEntry>(StringComparer.OrdinalIgnoreCase);
            var byJobId = new Dictionary<string, ImageLookupEntry>(StringComparer.OrdinalIgnoreCase);

            foreach (var entry in keyedEntries.Values)
            {
                var jobId = ExtractJobIdFromLookupEntry(entry);
                if (!string.IsNullOrWhiteSpace(jobId))
                {
                    byJobId[jobId] = entry;
                }
            }

            return byJobId;
        }
        catch
        {
            return new Dictionary<string, ImageLookupEntry>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private IReadOnlyDictionary<string, List<ImageLookupEntry>> LoadImageLookupByShortToken()
    {
        var byShortToken = new Dictionary<string, List<ImageLookupEntry>>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in _imageLookupByJobId.Value)
        {
            var separatorIndex = pair.Key.IndexOf('-');
            var shortToken = (separatorIndex > 0 ? pair.Key[..separatorIndex] : pair.Key).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(shortToken))
            {
                continue;
            }

            if (!byShortToken.TryGetValue(shortToken, out var entries))
            {
                entries = new List<ImageLookupEntry>();
                byShortToken[shortToken] = entries;
            }

            entries.Add(pair.Value);
        }

        return byShortToken;
    }

    private static string ExtractJobIdFromLookupEntry(ImageLookupEntry entry)
    {
        var fileName = ChooseValue(entry.FileName, Path.GetFileName(entry.LocalPath));
        return Path.GetFileNameWithoutExtension(fileName).Trim().ToLowerInvariant();
    }

    private static string ResolveImagePath(ImageLookupEntry entry)
    {
        return ChooseValue(entry.LocalPath, entry.FileName);
    }

    private static string ResolveGroupIdentity(MockupDraftRecord draft)
    {
        return ChooseValue(draft.GroupKey, draft.AssetKey, draft.JobId, draft.ReferenceCode, draft.ProductId);
    }

    private static string ChooseValue(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim() ?? string.Empty;
    }

    private static bool ContainsOrdinalIgnoreCase(string haystack, string? needle)
    {
        return !string.IsNullOrWhiteSpace(needle)
            && haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
    }

    private static decimal? AverageMinorUnits(IEnumerable<int> values)
    {
        var materialized = values.ToList();
        if (materialized.Count == 0)
        {
            return null;
        }

        return materialized.Sum(value => (decimal)value) / materialized.Count;
    }

    private static string FormatMinorUnits(decimal? amount)
    {
        if (!amount.HasValue)
        {
            return "n/a";
        }

        return "$" + (amount.Value / 100m).ToString("0.00", CultureInfo.InvariantCulture);
    }

    private static string DescribeFailure(Exception ex)
    {
        var message = ex.GetBaseException().Message.Trim();
        if (string.IsNullOrWhiteSpace(message))
        {
            return "An unknown error occurred.";
        }

        return message.EndsWith('.') ? message : message + ".";
    }

    private static IEnumerable<string> GetStrongLookupTags(MockupDraftRecord draft)
    {
        return NormalizeTags(draft.LookupTags)
            .Where(tag => tag.StartsWith("pgj-", StringComparison.OrdinalIgnoreCase)
                || tag.StartsWith("pgg-", StringComparison.OrdinalIgnoreCase)
                || tag.StartsWith("pga-", StringComparison.OrdinalIgnoreCase)
                || tag.StartsWith("pgk-", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class PublishingCatalog
    {
        private readonly IReadOnlyList<Product> _products;

        public PublishingCatalog(IReadOnlyList<Product> products)
        {
            _products = products;
        }

        public Product? MatchGroup(IReadOnlyList<StoredDraftRecord> records, IEnumerable<string> excludedProductIds)
        {
            var excludedIds = records
                .Select(record => record.Draft.ProductId)
                .Concat(excludedProductIds)
                .Where(productId => !string.IsNullOrWhiteSpace(productId))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var product in _products)
            {
                if (excludedIds.Contains(product.Id))
                {
                    continue;
                }

                if (records.Any(record => Matches(record.Draft, product)))
                {
                    return product;
                }
            }

            return null;
        }

        private static bool Matches(MockupDraftRecord draft, Product product)
        {
            return GetStrongMatchScore(draft, product) > 0;
        }
    }

    private static int GetStrongMatchScore(MockupDraftRecord draft, Product product)
    {
        var searchable = $"{product.Title}\n{product.Description}\n{string.Join(' ', product.Tags)}";
        var score = 0;

        if (ContainsOrdinalIgnoreCase(searchable, draft.LookupKey))
        {
            score = Math.Max(score, 100);
        }

        if (ContainsOrdinalIgnoreCase(searchable, draft.ReferenceCode))
        {
            score = Math.Max(score, 95);
        }

        if (ContainsOrdinalIgnoreCase(searchable, draft.GroupKey))
        {
            score = Math.Max(score, 90);
        }

        if (ContainsOrdinalIgnoreCase(searchable, draft.AssetKey))
        {
            score = Math.Max(score, 85);
        }

        var productTags = NormalizeTags(product.Tags).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var strongTagMatches = GetStrongLookupTags(draft).Count(productTags.Contains);

        if (strongTagMatches > 0)
        {
            score = Math.Max(score, 70 + strongTagMatches);
        }

        return score;
    }

    private static int GetInPlaceDraftMatchScore(MockupDraftRecord draft, Product product)
    {
        if (string.Equals(product.Id, draft.ProductId, StringComparison.OrdinalIgnoreCase))
        {
            return 500;
        }

        var score = GetStrongMatchScore(draft, product);
        if (score == 0)
        {
            return 0;
        }

        if (product.BlueprintId == draft.BlueprintId)
        {
            score += 20;
        }

        if (product.PrintProviderId == draft.PrintProviderId)
        {
            score += 20;
        }

        return score;
    }

    private sealed record StoredDraftRecord(
        MockupDraftRecord Draft,
        string DraftPath,
        DateTime CreatedAtUtc);

    private sealed record DraftGroup(
        string GroupId,
        string DisplayLabel,
        DateTime CreatedAtUtc,
        IReadOnlyList<StoredDraftRecord> Records)
    {
        public bool ShouldProcess(IReadOnlySet<string> productIds)
        {
            return productIds.Count == 0 || Records.Any(record => productIds.Contains(record.Draft.ProductId));
        }
    }

    private sealed record DraftCandidateEvaluation(
        StoredDraftRecord Record,
        Product SourceProduct,
        List<VariantPricingDecision> Pricing)
    {
        public int SelectedVariantCount => Pricing.Count;

        public decimal? AverageDeliveredCost => AverageMinorUnits(Pricing
            .Where(variant => variant.DeliveredCost.HasValue)
            .Select(variant => variant.DeliveredCost!.Value));

        public decimal? AverageProductionCost => AverageMinorUnits(Pricing
            .Where(variant => variant.ProductionCost.HasValue)
            .Select(variant => variant.ProductionCost!.Value));

        public decimal? AverageShippingCost => AverageMinorUnits(Pricing
            .Where(variant => variant.HasShippingPrice)
            .Select(variant => variant.ShippingCost));

        public PricingAvailability Availability => AverageDeliveredCost.HasValue
            ? PricingAvailability.DeliveredCost
            : AverageProductionCost.HasValue
                ? PricingAvailability.ProductionOnly
                : PricingAvailability.Missing;

        public string DescribePricing()
        {
            return Availability switch
            {
                PricingAvailability.DeliveredCost =>
                    $"avg delivered {FormatMinorUnits(AverageDeliveredCost)} (prod {FormatMinorUnits(AverageProductionCost)}, ship {FormatMinorUnits(AverageShippingCost)}) across {SelectedVariantCount} variant(s)",
                PricingAvailability.ProductionOnly =>
                    $"avg production {FormatMinorUnits(AverageProductionCost)} across {SelectedVariantCount} variant(s); shipping cache unavailable",
                _ => "no production or shipping cache data"
            };
        }
    }

    private sealed record VariantPricingDecision(
        int VariantId,
        bool IsEnabled,
        int CurrentPrice,
        int? ProductionCost,
        bool HasShippingPrice,
        int ShippingCost,
        int SuggestedPrice)
    {
        public int? DeliveredCost => ProductionCost.HasValue && HasShippingPrice
            ? checked(ProductionCost.Value + ShippingCost)
            : null;
    }

    private sealed record ResolvedMetadata(
        string Title,
        string Description,
        List<string> Tags);

    private readonly record struct ShippingQuoteSummary(int? FirstItemCost, int? AdditionalItemCost, string Currency);
}

enum PricingAvailability
{
    DeliveredCost,
    ProductionOnly,
    Missing
}

sealed class ProductMetadataUpdateRunSummary
{
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset CompletedAt { get; set; }
    public bool IsInPlaceMode { get; set; }
    public decimal MarginPercent { get; set; }
    public string ShippingCountryCode { get; set; } = string.Empty;
    public int DesiredVariantQuantity { get; set; }
    public int DraftRecordsLoaded { get; set; }
    public int DraftLoadFailures { get; set; }
    public int DraftGroupsDiscovered { get; set; }
    public int DraftGroupsProcessed { get; set; }
    public int DraftGroupsSkippedByFilter { get; set; }
    public int DraftGroupsSkippedAlreadyTransferred { get; set; }
    public int DraftGroupsWithoutViableCandidate { get; set; }
    public int StagingProductsDiscovered { get; set; }
    public int PublishingProductsDiscovered { get; set; }
    public int CandidateDraftsEvaluated { get; set; }
    public int CandidateDraftsMissingFromStaging { get; set; }
    public int CandidateDraftsWithDeliveredCost { get; set; }
    public int CandidateDraftsWithProductionOnlyCost { get; set; }
    public int CandidateDraftsWithoutPricing { get; set; }
    public int DraftGroupsSelectedForTransfer { get; set; }
    public int InPlaceProductsDiscovered { get; set; }
    public int InPlaceProductsProcessed { get; set; }
    public int InPlaceProductsSkippedByFilter { get; set; }
    public int InPlaceProductsMatched { get; set; }
    public int InPlaceProductsMatchedFromFallback { get; set; }
    public int InPlaceProductsWithoutDraftMatch { get; set; }
    public int InPlaceProductsUnchanged { get; set; }
    public int TransfersPlanned { get; set; }
    public int TransfersCompleted { get; set; }
    public int TransferFailures { get; set; }
    public int VariantsPriced { get; set; }
    public int StagingProductsPlannedForRemoval { get; set; }
    public int StagingProductsRemoved { get; set; }
    public int StagingProductRemovalFailures { get; set; }
    public int DraftRecordsPlannedForRemoval { get; set; }
    public int DraftRecordsRemoved { get; set; }
    public int DraftRecordRemovalFailures { get; set; }
    public int TargetCloneCleanupFailures { get; set; }
    public int ProductsWithUnsupportedQuantityRequest { get; set; }

    public bool HasFailures =>
        DraftLoadFailures > 0
        || TransferFailures > 0
        || StagingProductRemovalFailures > 0
        || DraftRecordRemovalFailures > 0
        || TargetCloneCleanupFailures > 0;
}