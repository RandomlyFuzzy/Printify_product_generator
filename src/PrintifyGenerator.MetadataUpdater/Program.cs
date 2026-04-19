var repositoryRoot = ResolveRepositoryRoot();
if (repositoryRoot is null)
{
    Console.Error.WriteLine("[ERROR] Could not locate the repository root.");
    return 1;
}

Directory.SetCurrentDirectory(repositoryRoot);

ProductMetadataUpdaterSettings settings;
try
{
    settings = ProductMetadataUpdaterSettings.Load(repositoryRoot, args);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[ERROR] {ex.Message}");
    return 1;
}

var client = new PrintifyClient(settings.Token);

Shop stagingShop;
Shop publishingShop;
try
{
    var shops = await client.GetShopsAsync();
    stagingShop = settings.ResolveStagingShop(shops);
    publishingShop = settings.ResolvePublishingShop(shops, stagingShop.Id);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[ERROR] Failed to resolve the staging/publishing Printify shops: {ex.Message}");
    return 1;
}

Console.WriteLine($"Staging shop: {stagingShop.Id} ({stagingShop.Title}).");
Console.WriteLine($"Publishing shop: {publishingShop.Id} ({publishingShop.Title}).");
if (stagingShop.Id == publishingShop.Id)
{
    Console.WriteLine("Same-shop mode is enabled. The chosen product will be updated in place only. No clone, delete, or draft cleanup will occur.");
}
Console.WriteLine(settings.ApplyChanges
    ? stagingShop.Id == publishingShop.Id
        ? "Live mode is enabled. The best GB-priced source product from each group will be updated in place only; nothing will be published."
        : "Live mode is enabled. The best GB-priced staged draft from each group will be transferred as a draft only; nothing will be published."
    : "Dry-run mode is enabled. No products will be transferred, updated, deleted, or published.");
Console.WriteLine($"Metadata channel: {settings.MetadataChannel}");
Console.WriteLine($"Shipping country: {settings.ShippingCountryCode}");
Console.WriteLine($"Pricing rule: {settings.MarginPercent:0.##}% over production plus first-item shipping.");
Console.WriteLine($"Desired variant quantity: {settings.DesiredVariantQuantity} (informational only; Printify product updates do not expose writable inventory quantities).");

if (!string.IsNullOrWhiteSpace(settings.ConfiguredStagingShopName))
{
    Console.WriteLine($"Configured staging shop name: {settings.ConfiguredStagingShopName}");
}

if (!string.IsNullOrWhiteSpace(settings.ConfiguredPublishingShopName))
{
    Console.WriteLine($"Configured publishing shop name: {settings.ConfiguredPublishingShopName}");
}

if (settings.TransferLimit.HasValue)
{
    Console.WriteLine($"Transfer limit: {settings.TransferLimit.Value}");
}

if (settings.ProductIds.Count > 0)
{
    Console.WriteLine($"Product filters: {string.Join(", ", settings.ProductIds.OrderBy(id => id, StringComparer.OrdinalIgnoreCase))}");
}

using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    if (!cancellation.IsCancellationRequested)
    {
        Console.WriteLine("Cancellation requested. Waiting for the current product to finish...");
        cancellation.Cancel();
    }
};

var draftsRoot = Path.Combine(repositoryRoot, "src", "data", "staging", "drafts");
var blueprintDetailsDirectory = Path.Combine(repositoryRoot, "src", "data", "Cached", "blueprint_details");
var updater = new ProductMetadataUpdater(
    client,
    stagingShop.Id,
    publishingShop.Id,
    draftsRoot,
    PrintifyBlueprintDatabase.CreateQueryApi(blueprintDetailsDirectory),
    settings);

try
{
    var summary = await updater.RunOnceAsync(cancellation.Token);
    PrintSummary(summary, settings.ApplyChanges);
    return summary.HasFailures ? 1 : 0;
}
catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
{
    Console.Error.WriteLine("[ERROR] Metadata transfer run cancelled.");
    return 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[ERROR] Metadata transfer run failed: {ex.Message}");
    return 1;
}

static void PrintSummary(ProductMetadataUpdateRunSummary summary, bool applyChanges)
{
    Console.WriteLine();
    Console.WriteLine($"Run completed in {(summary.CompletedAt - summary.StartedAt).TotalSeconds:F1}s.");
    Console.WriteLine($"Mode: {(summary.IsInPlaceMode ? "in-place update" : "cross-shop transfer")}");
    Console.WriteLine($"Shipping country: {summary.ShippingCountryCode}");
    Console.WriteLine($"Margin percent: {summary.MarginPercent:0.##}");
    Console.WriteLine($"Draft records loaded: {summary.DraftRecordsLoaded}");
    Console.WriteLine($"Draft load failures: {summary.DraftLoadFailures}");
    if (summary.IsInPlaceMode)
    {
        Console.WriteLine($"Products discovered: {summary.InPlaceProductsDiscovered}");
        Console.WriteLine($"Products processed: {summary.InPlaceProductsProcessed}");
        Console.WriteLine($"Products skipped by filter: {summary.InPlaceProductsSkippedByFilter}");
        Console.WriteLine($"Products matched to metadata source: {summary.InPlaceProductsMatched}");
        Console.WriteLine($"Products reconstructed from lookup data: {summary.InPlaceProductsMatchedFromFallback}");
        Console.WriteLine($"Products without metadata source: {summary.InPlaceProductsWithoutDraftMatch}");
        Console.WriteLine($"Products unchanged: {summary.InPlaceProductsUnchanged}");
    }
    else
    {
        Console.WriteLine($"Draft groups discovered: {summary.DraftGroupsDiscovered}");
        Console.WriteLine($"Draft groups processed: {summary.DraftGroupsProcessed}");
        Console.WriteLine($"Draft groups skipped by filter: {summary.DraftGroupsSkippedByFilter}");
        Console.WriteLine($"Draft groups skipped as already transferred: {summary.DraftGroupsSkippedAlreadyTransferred}");
        Console.WriteLine($"Draft groups without a viable candidate: {summary.DraftGroupsWithoutViableCandidate}");
        Console.WriteLine($"Staging products discovered: {summary.StagingProductsDiscovered}");
        Console.WriteLine($"Publishing products discovered: {summary.PublishingProductsDiscovered}");
        Console.WriteLine($"Candidate drafts evaluated: {summary.CandidateDraftsEvaluated}");
        Console.WriteLine($"Candidate drafts missing from staging: {summary.CandidateDraftsMissingFromStaging}");
        Console.WriteLine($"Candidates with delivered-cost cache data: {summary.CandidateDraftsWithDeliveredCost}");
        Console.WriteLine($"Candidates with production-only cache data: {summary.CandidateDraftsWithProductionOnlyCost}");
        Console.WriteLine($"Candidates without cache pricing: {summary.CandidateDraftsWithoutPricing}");
        Console.WriteLine($"Draft groups selected for transfer: {summary.DraftGroupsSelectedForTransfer}");
    }
    Console.WriteLine(applyChanges
        ? summary.IsInPlaceMode
            ? $"Products updated in place: {summary.TransfersCompleted}"
            : $"Drafts transferred: {summary.TransfersCompleted}"
        : summary.IsInPlaceMode
            ? $"Products that would be updated in place: {summary.TransfersPlanned}"
            : $"Drafts that would be transferred: {summary.TransfersPlanned}");
    Console.WriteLine(summary.IsInPlaceMode
        ? $"In-place update failures: {summary.TransferFailures}"
        : $"Transfer failures: {summary.TransferFailures}");
    Console.WriteLine($"Variants repriced: {summary.VariantsPriced}");
    if (!summary.IsInPlaceMode)
    {
        Console.WriteLine(applyChanges
            ? $"Staging products removed: {summary.StagingProductsRemoved}"
            : $"Staging products that would be removed: {summary.StagingProductsPlannedForRemoval}");
        Console.WriteLine(applyChanges
            ? $"Draft records removed: {summary.DraftRecordsRemoved}"
            : $"Draft records that would be removed: {summary.DraftRecordsPlannedForRemoval}");
        Console.WriteLine($"Staging cleanup failures: {summary.StagingProductRemovalFailures}");
        Console.WriteLine($"Draft record cleanup failures: {summary.DraftRecordRemovalFailures}");
        Console.WriteLine($"Target clone cleanup failures: {summary.TargetCloneCleanupFailures}");
    }
    Console.WriteLine($"Desired variant quantity: {summary.DesiredVariantQuantity} (not applied; Printify product updates only support variant price and is_enabled).");
    Console.WriteLine(summary.IsInPlaceMode
        ? $"Updated products where quantity could not be applied: {summary.ProductsWithUnsupportedQuantityRequest}"
        : $"Transferred products where quantity could not be applied: {summary.ProductsWithUnsupportedQuantityRequest}");
    Console.WriteLine();
}

static string? ResolveRepositoryRoot()
{
    var probeRoots = new[]
    {
        Directory.GetCurrentDirectory(),
        AppContext.BaseDirectory
    }
    .Where(path => !string.IsNullOrWhiteSpace(path))
    .Distinct(StringComparer.OrdinalIgnoreCase);

    foreach (var probeRoot in probeRoots)
    {
        var current = new DirectoryInfo(Path.GetFullPath(probeRoot));

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "PrintifyGenerator.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }
    }

    return null;
}