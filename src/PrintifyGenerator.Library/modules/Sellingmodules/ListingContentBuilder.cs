using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

public static class ListingContentBuilder
{
    private const int PrintifyTitleMaxLength = 180;
    private const int EtsyTitleMaxLength = 140;
    private const int EbayTitleMaxLength = 80;
    private const int GenericTitleMaxLength = 160;

    public static ListingContentBundle Build(ListingContentContext context)
    {
        var normalizedJobId = SanitizeLookupPart(context.JobId);
        if (string.IsNullOrWhiteSpace(normalizedJobId))
            normalizedJobId = SanitizeLookupPart(Path.GetFileNameWithoutExtension(context.ImagePath));

        if (string.IsNullOrWhiteSpace(normalizedJobId))
            normalizedJobId = "unknown-job";

        var assetToken = BuildAssetToken(context.ImagePath, normalizedJobId);
        var jobShort = ShortenToken(normalizedJobId, 8);
        var assetShort = ShortenToken(assetToken, 12);
        var referenceCode = $"{jobShort.ToUpperInvariant()}-B{context.BlueprintId}-P{context.PrintProviderId}";
        var lookup = new ListingLookupIdentity
        {
            LookupKey = $"mockup-{jobShort}-b{context.BlueprintId}-p{context.PrintProviderId}-{assetShort}",
            GroupKey = $"job-{ShortenToken(normalizedJobId, 16)}",
            AssetKey = $"art-{jobShort}-{assetShort}",
            ReferenceCode = referenceCode,
            JobId = normalizedJobId,
            AssetToken = assetToken
        };

        lookup.Tags = BuildLookupTags(lookup, context);

        var displayAssetName = ResolveDisplayAssetName(assetToken, jobShort);
        var blueprintTitle = SafeValue(context.BlueprintTitle, "Untitled Product");
        var providerTitle = SafeValue(context.PrintProviderTitle, "Unknown Provider");
        var fitReason = BuildFitReason(context.LlmReason);
        var publicTags = BuildPublicTags(blueprintTitle, displayAssetName);

        var channels = new Dictionary<string, ListingChannelContent>(StringComparer.OrdinalIgnoreCase)
        {
            ["printify"] = new ListingChannelContent
            {
                Title = Truncate($"{blueprintTitle} | {displayAssetName} | {referenceCode}", PrintifyTitleMaxLength),
                Description = BuildPrintifyDescription(displayAssetName, blueprintTitle, providerTitle, fitReason, lookup),
                Tags = new List<string>(lookup.Tags)
            },
            ["etsy"] = new ListingChannelContent
            {
                Title = Truncate($"{blueprintTitle} featuring {displayAssetName}", EtsyTitleMaxLength),
                Description = BuildEtsyDescription(displayAssetName, blueprintTitle, providerTitle, fitReason),
                Tags = new List<string>(publicTags)
            },
            ["ebay"] = new ListingChannelContent
            {
                Title = Truncate($"{displayAssetName} {blueprintTitle}", EbayTitleMaxLength),
                Description = BuildEbayDescription(displayAssetName, blueprintTitle, providerTitle, fitReason),
                Tags = new List<string>(publicTags)
            },
            ["shopify"] = new ListingChannelContent
            {
                Title = Truncate($"{displayAssetName} | {blueprintTitle}", GenericTitleMaxLength),
                Description = BuildShopifyDescription(displayAssetName, blueprintTitle, providerTitle, fitReason),
                Tags = new List<string>(publicTags)
            },
            ["woocommerce"] = new ListingChannelContent
            {
                Title = Truncate($"{displayAssetName} | {blueprintTitle}", GenericTitleMaxLength),
                Description = BuildWooCommerceDescription(displayAssetName, blueprintTitle, providerTitle, fitReason),
                Tags = new List<string>(publicTags)
            },
            ["generic"] = new ListingChannelContent
            {
                Title = Truncate($"{displayAssetName} | {blueprintTitle}", GenericTitleMaxLength),
                Description = BuildGenericDescription(displayAssetName, blueprintTitle, providerTitle, fitReason),
                Tags = new List<string>(publicTags)
            }
        };

        return new ListingContentBundle
        {
            Lookup = lookup,
            Channels = channels
        };
    }

    public static ListingChannelContent ResolveChannel(ListingContentBundle bundle, string? platformName)
    {
        if (bundle.Channels.Count == 0)
            return new ListingChannelContent();

        var normalizedPlatform = NormalizePlatformKey(platformName);
        if (bundle.Channels.TryGetValue(normalizedPlatform, out var exactMatch))
            return exactMatch;

        if (bundle.Channels.TryGetValue("generic", out var genericMatch))
            return genericMatch;

        return bundle.Channels.Values.First();
    }

    private static List<string> BuildLookupTags(ListingLookupIdentity lookup, ListingContentContext context)
    {
        return new List<string>
        {
            "pg-mockup",
            $"pgj-{ShortenToken(lookup.JobId, 12)}",
            $"pgb-{context.BlueprintId}",
            $"pgp-{context.PrintProviderId}",
            $"pgg-{ShortenToken(lookup.GroupKey, 12)}",
            $"pga-{ShortenToken(lookup.AssetKey, 12)}",
            $"pgk-{ShortenToken(lookup.LookupKey, 16)}"
        }
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();
    }

    private static List<string> BuildPublicTags(string blueprintTitle, string displayAssetName)
    {
        var candidates = new List<string>
        {
            blueprintTitle,
            displayAssetName,
            "made to order",
            "print on demand",
            "original artwork"
        };

        return candidates
            .Select(NormalizePublicTag)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();
    }

    private static string BuildPrintifyDescription(
        string displayAssetName,
        string blueprintTitle,
        string providerTitle,
        string fitReason,
        ListingLookupIdentity lookup)
    {
        return JoinParagraphs(
            $"{displayAssetName} prepared as a {blueprintTitle}.",
            $"Why this fit works: {fitReason}",
            $"Production partner: {providerTitle}.",
            JoinLines(
                "Internal lookup",
                $"Lookup key: {lookup.LookupKey}",
                $"Group key: {lookup.GroupKey}",
                $"Asset key: {lookup.AssetKey}",
                $"Reference: {lookup.ReferenceCode}",
                $"Job id: {lookup.JobId}"));
    }

    private static string BuildEtsyDescription(string displayAssetName, string blueprintTitle, string providerTitle, string fitReason)
    {
        return JoinParagraphs(
            $"{displayAssetName} is prepared as a made-to-order {blueprintTitle} so the artwork remains the focus of the finished product.",
            $"Why this version works: {fitReason}",
            JoinLines(
                "Production notes",
                $"- Product format: {blueprintTitle}",
                $"- Fulfillment partner: {providerTitle}",
                "- Produced on demand from the approved artwork"));
    }

    private static string BuildEbayDescription(string displayAssetName, string blueprintTitle, string providerTitle, string fitReason)
    {
        return JoinParagraphs(
            $"Made-to-order {blueprintTitle} featuring {displayAssetName}.",
            $"Format rationale: {fitReason}",
            JoinLines(
                "Listing details",
                $"- Product type: {blueprintTitle}",
                $"- Production partner: {providerTitle}",
                "- Printed after order placement"));
    }

    private static string BuildShopifyDescription(string displayAssetName, string blueprintTitle, string providerTitle, string fitReason)
    {
        return JoinParagraphs(
            $"{displayAssetName} is available here as a {blueprintTitle}, chosen to match the artwork's composition and print area.",
            $"Why this format: {fitReason}",
            $"Fulfillment partner: {providerTitle}. Each item is produced to order.");
    }

    private static string BuildWooCommerceDescription(string displayAssetName, string blueprintTitle, string providerTitle, string fitReason)
    {
        return JoinParagraphs(
            $"{displayAssetName} is configured for this {blueprintTitle} as a print-on-demand product.",
            $"Selection reason: {fitReason}",
            $"Production partner: {providerTitle}. Printed after purchase.");
    }

    private static string BuildGenericDescription(string displayAssetName, string blueprintTitle, string providerTitle, string fitReason)
    {
        return JoinParagraphs(
            $"{displayAssetName} prepared as a {blueprintTitle}.",
            $"Why this fit: {fitReason}",
            $"Fulfillment partner: {providerTitle}.");
    }

    private static string BuildAssetToken(string imagePath, string normalizedJobId)
    {
        var fileNameWithoutExtension = SanitizeLookupPart(Path.GetFileNameWithoutExtension(imagePath));
        if (string.IsNullOrWhiteSpace(fileNameWithoutExtension))
            return ShortenToken(normalizedJobId, 12);

        if (string.Equals(fileNameWithoutExtension, normalizedJobId, StringComparison.OrdinalIgnoreCase))
            return ShortenToken(normalizedJobId, 12);

        return ShortenToken(fileNameWithoutExtension, 18);
    }

    private static string ResolveDisplayAssetName(string assetToken, string jobShort)
    {
        if (IsOpaqueToken(assetToken))
            return $"Artwork {jobShort.ToUpperInvariant()}";

        return HumanizeToken(assetToken);
    }

    private static string BuildFitReason(string reason)
    {
        var normalized = SafeValue(reason, "This product format was chosen because the artwork composition suits the print area well.").Trim();
        if (!normalized.EndsWith(".", StringComparison.Ordinal))
            normalized += ".";

        return normalized;
    }

    private static string NormalizePlatformKey(string? platformName)
    {
        if (string.IsNullOrWhiteSpace(platformName))
            return "generic";

        var builder = new StringBuilder(platformName.Length);
        foreach (var character in platformName)
        {
            if (char.IsLetterOrDigit(character))
                builder.Append(char.ToLowerInvariant(character));
        }

        return builder.Length == 0 ? "generic" : builder.ToString();
    }

    private static string NormalizePublicTag(string value)
    {
        var builder = new StringBuilder(value.Length);
        var previousWasSeparator = false;

        foreach (var character in value.Trim())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                previousWasSeparator = false;
                continue;
            }

            if (builder.Length > 0 && !previousWasSeparator)
            {
                builder.Append(' ');
                previousWasSeparator = true;
            }
        }

        return Truncate(builder.ToString().Trim(), 24);
    }

    private static string SanitizeLookupPart(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var builder = new StringBuilder(value.Length);
        var previousWasSeparator = false;

        foreach (var character in value.Trim())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                previousWasSeparator = false;
                continue;
            }

            if (builder.Length > 0 && !previousWasSeparator)
            {
                builder.Append('-');
                previousWasSeparator = true;
            }
        }

        return builder.ToString().Trim('-');
    }

    private static string HumanizeToken(string token)
    {
        var parts = token
            .Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => part.Length <= 2
                ? part.ToUpperInvariant()
                : char.ToUpperInvariant(part[0]) + part[1..])
            .ToList();

        return parts.Count == 0 ? "Artwork" : string.Join(" ", parts);
    }

    private static bool IsOpaqueToken(string token)
    {
        if (Guid.TryParse(token, out _))
            return true;

        var compact = token.Replace("-", string.Empty, StringComparison.Ordinal);
        if (compact.Length < 8)
            return false;

        var hexCharacterCount = compact.Count(character => char.IsDigit(character) || (character >= 'a' && character <= 'f'));
        return (double)hexCharacterCount / compact.Length >= 0.75;
    }

    private static string ShortenToken(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static string SafeValue(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLength)
            return value;

        return value[..maxLength].TrimEnd();
    }

    private static string JoinParagraphs(params string[] sections)
    {
        return string.Join(
            Environment.NewLine + Environment.NewLine,
            sections.Where(section => !string.IsNullOrWhiteSpace(section)).Select(section => section.Trim()));
    }

    private static string JoinLines(params string[] lines)
    {
        return string.Join(Environment.NewLine, lines.Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => line.TrimEnd()));
    }
}