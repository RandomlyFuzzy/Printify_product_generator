using System.Globalization;

sealed record ProductMetadataUpdaterSettings(
    string Token,
    int? ConfiguredStagingShopId,
    string? ConfiguredStagingShopName,
    int? ConfiguredPublishingShopId,
    string? ConfiguredPublishingShopName,
    string MetadataChannel,
    bool ApplyChanges,
    int RequestDelayMs,
    int? TransferLimit,
    int DesiredVariantQuantity,
    decimal MarginPercent,
    string ShippingCountryCode,
    HashSet<string> ProductIds)
{
    public static ProductMetadataUpdaterSettings Load(string repositoryRoot, string[] args)
    {
        var envValues = ReadEnvFile(Path.Combine(repositoryRoot, "main.env"));

        var token = GetRequiredString("TOKEN", envValues);
        var configuredStagingShopId = TryReadIntOption(args, "--staging-shop-id")
            ?? GetIntFromKeys(envValues,
                "STAGING_SHOP_ID",
                "STAGING_SHOPID");

        var configuredStagingShopName = TryReadStringOption(args, "--staging-shop-name")
            ?? GetStringFromKeys(envValues,
                "METADATA_UPDATER_STAGING_SHOP_NAME",
                "PRODUCT_METADATA_UPDATER_STAGING_SHOP_NAME");

        var configuredPublishingShopId = TryReadIntOption(args, "--publishing-shop-id")
            ?? TryReadIntOption(args, "--shop-id")
            ?? GetIntFromKeys(envValues,
                "PUBLISHING_SHOP_ID",
                "METADATA_UPDATER_PUBLISHING_SHOP_ID",
                "PRODUCT_METADATA_UPDATER_PUBLISHING_SHOP_ID",
                "PRICE_UPDATER_SHOP_ID",
                "SHOP_ID",
                "SHOPID");

        var configuredPublishingShopName = TryReadStringOption(args, "--publishing-shop-name")
            ?? TryReadStringOption(args, "--shop-name")
            ?? GetStringFromKeys(envValues,
                "METADATA_UPDATER_PUBLISHING_SHOP_NAME",
                "PRODUCT_METADATA_UPDATER_PUBLISHING_SHOP_NAME");

        var metadataChannel = NormalizeChannelKey(
            TryReadStringOption(args, "--channel")
            ?? GetStringFromKeys(envValues,
                "METADATA_UPDATER_CHANNEL",
                "PRODUCT_METADATA_UPDATER_CHANNEL")
            ?? "printify");

        var requestDelayMs = TryReadIntOption(args, "--request-delay-ms")
            ?? GetIntFromKeys(envValues,
                "METADATA_UPDATER_REQUEST_DELAY_MS",
                "PRODUCT_METADATA_UPDATER_REQUEST_DELAY_MS")
            ?? 200;

        var transferLimit = TryReadIntOption(args, "--limit-transfers")
            ?? TryReadIntOption(args, "--limit-products")
            ?? GetIntFromKeys(envValues,
                "METADATA_UPDATER_TRANSFER_LIMIT",
                "PRODUCT_METADATA_UPDATER_TRANSFER_LIMIT",
                "METADATA_UPDATER_PRODUCT_LIMIT",
                "PRODUCT_METADATA_UPDATER_PRODUCT_LIMIT");

        var desiredVariantQuantity = TryReadIntOption(args, "--variant-quantity")
            ?? GetIntFromKeys(envValues,
                "METADATA_UPDATER_VARIANT_QUANTITY",
                "PRODUCT_METADATA_UPDATER_VARIANT_QUANTITY")
            ?? 10;

        var marginPercent = TryReadDecimalOption(args, "--margin-percent")
            ?? GetDecimalFromKeys(envValues,
                "METADATA_UPDATER_MARGIN_PERCENT",
                "PRODUCT_METADATA_UPDATER_MARGIN_PERCENT",
                "PRICE_UPDATER_MARGIN_PERCENT")
            ?? 40m;

        var shippingCountryCode = NormalizeCountryCode(
            TryReadStringOption(args, "--country")
            ?? GetStringFromKeys(envValues,
                "METADATA_UPDATER_COUNTRY",
                "PRODUCT_METADATA_UPDATER_COUNTRY",
                "PRICE_UPDATER_COUNTRY")
            ?? "GB");

        var applyChanges = HasFlag(args, "--apply")
            || (GetBoolFromKeys(envValues,
                "METADATA_UPDATER_APPLY_CHANGES",
                "PRODUCT_METADATA_UPDATER_APPLY_CHANGES") ?? false);

        if (HasFlag(args, "--dry-run"))
        {
            applyChanges = false;
        }

        if (requestDelayMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requestDelayMs), "Request delay must be zero or greater.");
        }

        if (desiredVariantQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(desiredVariantQuantity), "Desired variant quantity must be greater than zero.");
        }

        if (marginPercent < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(marginPercent), "Margin percent must be zero or greater.");
        }

        var configuredProductIds = ParseProductIds(
            TryReadStringOptions(args, "--product-id")
                .Concat(ParseDelimitedValues(GetStringFromKeys(envValues,
                    "METADATA_UPDATER_PRODUCT_IDS",
                    "PRODUCT_METADATA_UPDATER_PRODUCT_IDS"))));

        return new ProductMetadataUpdaterSettings(
            Token: token,
            ConfiguredStagingShopId: configuredStagingShopId,
            ConfiguredStagingShopName: string.IsNullOrWhiteSpace(configuredStagingShopName) ? null : configuredStagingShopName.Trim(),
            ConfiguredPublishingShopId: configuredPublishingShopId,
            ConfiguredPublishingShopName: string.IsNullOrWhiteSpace(configuredPublishingShopName) ? null : configuredPublishingShopName.Trim(),
            MetadataChannel: metadataChannel,
            ApplyChanges: applyChanges,
            RequestDelayMs: requestDelayMs,
            TransferLimit: transferLimit,
            DesiredVariantQuantity: desiredVariantQuantity,
            MarginPercent: marginPercent,
            ShippingCountryCode: shippingCountryCode,
            ProductIds: configuredProductIds);
    }

    public Shop ResolveStagingShop(IReadOnlyList<Shop> shops)
    {
        if (shops.Count == 0)
        {
            throw new InvalidOperationException("No Printify shops were returned for the configured token.");
        }

        if (ConfiguredStagingShopId.HasValue)
        {
            return shops.FirstOrDefault(shop => shop.Id == ConfiguredStagingShopId.Value)
                ?? throw new InvalidOperationException($"Configured staging shop ID {ConfiguredStagingShopId.Value} was not found on this Printify account.");
        }

        if (!string.IsNullOrWhiteSpace(ConfiguredStagingShopName))
        {
            var normalizedName = ConfiguredStagingShopName.Trim();
            var exactMatch = shops.FirstOrDefault(shop => string.Equals(shop.Title, normalizedName, StringComparison.OrdinalIgnoreCase));
            if (exactMatch is not null)
            {
                return exactMatch;
            }

            var partialMatch = shops.FirstOrDefault(shop => shop.Title.Contains(normalizedName, StringComparison.OrdinalIgnoreCase));
            if (partialMatch is not null)
            {
                return partialMatch;
            }
        }

        return shops.FirstOrDefault(shop => string.Equals(shop.Title, "Production", StringComparison.OrdinalIgnoreCase))
            ?? shops.FirstOrDefault(shop => shop.Title.Contains("production", StringComparison.OrdinalIgnoreCase))
            ?? shops.FirstOrDefault(shop => string.Equals(shop.Title, "Staging", StringComparison.OrdinalIgnoreCase))
            ?? shops.FirstOrDefault(shop => shop.Title.Contains("staging", StringComparison.OrdinalIgnoreCase))
            ?? shops.FirstOrDefault(shop => string.Equals(shop.SalesChannel, "custom_integration", StringComparison.OrdinalIgnoreCase))
            ?? shops[0];
    }

    public Shop ResolvePublishingShop(IReadOnlyList<Shop> shops, int stagingShopId)
    {
        if (shops.Count == 0)
        {
            throw new InvalidOperationException("No Printify shops were returned for the configured token.");
        }

        var stagingShop = shops.FirstOrDefault(shop => shop.Id == stagingShopId);

        if (ConfiguredPublishingShopId.HasValue)
        {
            return shops.FirstOrDefault(shop => shop.Id == ConfiguredPublishingShopId.Value)
                ?? throw new InvalidOperationException($"Configured publishing shop ID {ConfiguredPublishingShopId.Value} was not found on this Printify account.");
        }

        if (!string.IsNullOrWhiteSpace(ConfiguredPublishingShopName))
        {
            var normalizedName = ConfiguredPublishingShopName.Trim();
            var exactMatch = shops.FirstOrDefault(shop =>
                string.Equals(shop.Title, normalizedName, StringComparison.OrdinalIgnoreCase));
            if (exactMatch is not null)
            {
                return exactMatch;
            }

            var partialMatch = shops.FirstOrDefault(shop =>
                shop.Title.Contains(normalizedName, StringComparison.OrdinalIgnoreCase));
            if (partialMatch is not null)
            {
                return partialMatch;
            }
        }

        if (stagingShop is not null &&
            (string.Equals(stagingShop.Title, "Production", StringComparison.OrdinalIgnoreCase)
             || stagingShop.Title.Contains("production", StringComparison.OrdinalIgnoreCase)))
        {
            return stagingShop;
        }

        return shops.FirstOrDefault(shop => shop.Id != stagingShopId && string.Equals(shop.Title, "Publishing", StringComparison.OrdinalIgnoreCase))
            ?? shops.FirstOrDefault(shop => shop.Id != stagingShopId && string.Equals(shop.Title, "Production", StringComparison.OrdinalIgnoreCase))
            ?? shops.FirstOrDefault(shop => shop.Id != stagingShopId && shop.Title.Contains("publish", StringComparison.OrdinalIgnoreCase))
            ?? shops.FirstOrDefault(shop => shop.Id != stagingShopId && shop.Title.Contains("production", StringComparison.OrdinalIgnoreCase))
            ?? shops.FirstOrDefault(shop => shop.Id != stagingShopId && !string.Equals(shop.SalesChannel, "custom_integration", StringComparison.OrdinalIgnoreCase))
            ?? shops.FirstOrDefault(shop => shop.Id != stagingShopId)
            ?? throw new InvalidOperationException("A publishing shop distinct from the staging shop could not be resolved.");
    }

    public bool ShouldProcessProduct(string productId)
    {
        return ProductIds.Count == 0 || ProductIds.Contains(productId.Trim());
    }

    private static Dictionary<string, string> ReadEnvFile(string envFilePath)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(envFilePath))
        {
            return values;
        }

        foreach (var rawLine in File.ReadLines(envFilePath))
        {
            var line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();

            if (!string.IsNullOrWhiteSpace(key))
            {
                values[key] = value;
            }
        }

        return values;
    }

    private static string GetRequiredString(string key, IReadOnlyDictionary<string, string> envValues)
    {
        return GetString(key, envValues)
            ?? throw new InvalidOperationException($"Missing required configuration value '{key}'.");
    }

    private static string? GetString(string key, IReadOnlyDictionary<string, string> envValues)
    {
        var processValue = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrWhiteSpace(processValue))
        {
            return processValue.Trim();
        }

        return envValues.TryGetValue(key, out var fileValue) && !string.IsNullOrWhiteSpace(fileValue)
            ? fileValue.Trim()
            : null;
    }

    private static string? GetStringFromKeys(IReadOnlyDictionary<string, string> envValues, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = GetString(key, envValues);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static int? GetInt(string key, IReadOnlyDictionary<string, string> envValues)
    {
        var value = GetString(key, envValues);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
        {
            return parsedValue;
        }

        throw new InvalidOperationException($"Configuration value '{key}' must be an integer.");
    }

    private static decimal? GetDecimal(string key, IReadOnlyDictionary<string, string> envValues)
    {
        var value = GetString(key, envValues);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue))
        {
            return parsedValue;
        }

        throw new InvalidOperationException($"Configuration value '{key}' must be a decimal number.");
    }

    private static int? GetIntFromKeys(IReadOnlyDictionary<string, string> envValues, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = GetInt(key, envValues);
            if (value.HasValue)
            {
                return value;
            }
        }

        return null;
    }

    private static decimal? GetDecimalFromKeys(IReadOnlyDictionary<string, string> envValues, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = GetDecimal(key, envValues);
            if (value.HasValue)
            {
                return value;
            }
        }

        return null;
    }

    private static bool? GetBool(string key, IReadOnlyDictionary<string, string> envValues)
    {
        var value = GetString(key, envValues);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "1" or "true" or "yes" or "on" => true,
            "0" or "false" or "no" or "off" => false,
            _ => throw new InvalidOperationException($"Configuration value '{key}' must be a boolean.")
        };
    }

    private static bool? GetBoolFromKeys(IReadOnlyDictionary<string, string> envValues, params string[] keys)
    {
        foreach (var key in keys)
        {
            var value = GetBool(key, envValues);
            if (value.HasValue)
            {
                return value;
            }
        }

        return null;
    }

    private static bool HasFlag(IEnumerable<string> args, string flag)
    {
        return args.Any(arg => string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase));
    }

    private static string? TryReadStringOption(string[] args, string optionName)
    {
        return TryReadStringOptions(args, optionName).LastOrDefault();
    }

    private static List<string> TryReadStringOptions(string[] args, string optionName)
    {
        var values = new List<string>();

        for (var index = 0; index < args.Length; index++)
        {
            if (!string.Equals(args[index], optionName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (index + 1 >= args.Length)
            {
                throw new InvalidOperationException($"Missing value for {optionName}.");
            }

            values.Add(args[index + 1].Trim());
        }

        return values;
    }

    private static int? TryReadIntOption(string[] args, string optionName)
    {
        var value = TryReadStringOption(args, optionName);
        if (value is null)
        {
            return null;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue))
        {
            return parsedValue;
        }

        throw new InvalidOperationException($"Value for {optionName} must be an integer.");
    }

    private static decimal? TryReadDecimalOption(string[] args, string optionName)
    {
        var value = TryReadStringOption(args, optionName);
        if (value is null)
        {
            return null;
        }

        if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedValue))
        {
            return parsedValue;
        }

        throw new InvalidOperationException($"Value for {optionName} must be a decimal number.");
    }

    private static HashSet<string> ParseProductIds(IEnumerable<string> rawValues)
    {
        return rawValues
            .SelectMany(ParseDelimitedValues)
            .Select(value => value.Trim())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> ParseDelimitedValues(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return Array.Empty<string>();
        }

        return rawValue
            .Split(new[] { ',', ';', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => !string.IsNullOrWhiteSpace(value));
    }

    private static string NormalizeChannelKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "printify";
        }

        var normalized = new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());

        return string.IsNullOrWhiteSpace(normalized) ? "printify" : normalized;
    }

    private static string NormalizeCountryCode(string? value)
    {
        return value?.Trim().ToUpperInvariant() switch
        {
            "US" or "USA" or "UNITED STATES" => "US",
            "GB" or "UK" or "UNITED KINGDOM" => "GB",
            var normalized when !string.IsNullOrWhiteSpace(normalized) => normalized,
            _ => "GB"
        };
    }
}