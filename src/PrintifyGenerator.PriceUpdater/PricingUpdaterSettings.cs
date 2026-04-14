using System.Globalization;

sealed record PricingUpdaterSettings(
    string Token,
    int? ConfiguredShopId,
    Address ShippingAddress,
    PricingShippingMethod ShippingMethod,
    TimeSpan Interval,
    bool ApplyChanges,
    bool RunOnce,
    int RequestDelayMs,
    int? ProductLimit,
    int? VariantLimitPerProduct,
    decimal MarginPercent)
{
    public static PricingUpdaterSettings Load(string repositoryRoot, string[] args)
    {
        var envValues = ReadEnvFile(Path.Combine(repositoryRoot, "main.env"));

        var token = GetRequiredString("TOKEN", envValues);
        var configuredShopId = TryReadIntOption(args, "--shop-id")
            ?? GetInt("PRICE_UPDATER_SHOP_ID", envValues)
            ?? GetInt("SHOP_ID", envValues)
            ?? GetInt("SHOPID", envValues);

        var country = TryReadStringOption(args, "--country")
            ?? GetRequiredString("PRICE_UPDATER_COUNTRY", envValues);

        var region = TryReadStringOption(args, "--region")
            ?? GetString("PRICE_UPDATER_REGION", envValues)
            ?? string.Empty;

        var zip = TryReadStringOption(args, "--zip")
            ?? GetString("PRICE_UPDATER_ZIP", envValues)
            ?? string.Empty;

        var city = TryReadStringOption(args, "--city")
            ?? GetString("PRICE_UPDATER_CITY", envValues)
            ?? "Pricing City";

        var address1 = TryReadStringOption(args, "--address1")
            ?? GetString("PRICE_UPDATER_ADDRESS1", envValues)
            ?? "1 Pricing Update Way";

        var address2 = TryReadStringOption(args, "--address2")
            ?? GetString("PRICE_UPDATER_ADDRESS2", envValues);

        var shippingMethodText = TryReadStringOption(args, "--shipping-method")
            ?? GetString("PRICE_UPDATER_SHIPPING_METHOD", envValues)
            ?? "standard";

        var intervalMinutes = TryReadIntOption(args, "--interval-minutes")
            ?? GetInt("PRICE_UPDATER_INTERVAL_MINUTES", envValues)
            ?? 60;

        var requestDelayMs = TryReadIntOption(args, "--request-delay-ms")
            ?? GetInt("PRICE_UPDATER_REQUEST_DELAY_MS", envValues)
            ?? 200;

        var productLimit = TryReadIntOption(args, "--limit-products")
            ?? GetInt("PRICE_UPDATER_PRODUCT_LIMIT", envValues);

        var variantLimitPerProduct = TryReadIntOption(args, "--limit-variants")
            ?? GetInt("PRICE_UPDATER_VARIANT_LIMIT", envValues);

        var marginPercent = TryReadDecimalOption(args, "--margin-percent")
            ?? GetDecimal("PRICE_UPDATER_MARGIN_PERCENT", envValues)
            ?? 40m;

        var applyChanges = HasFlag(args, "--apply") || (GetBool("PRICE_UPDATER_APPLY_CHANGES", envValues) ?? false);
        if (HasFlag(args, "--dry-run"))
        {
            applyChanges = false;
        }

        var runOnce = HasFlag(args, "--once") || (GetBool("PRICE_UPDATER_RUN_ONCE", envValues) ?? false);

        if (intervalMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(intervalMinutes), "Update interval must be greater than zero minutes.");
        }

        if (requestDelayMs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requestDelayMs), "Request delay must be zero or greater.");
        }

        if (marginPercent < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(marginPercent), "Margin percent must be zero or greater.");
        }

        return new PricingUpdaterSettings(
            Token: token,
            ConfiguredShopId: configuredShopId,
            ShippingAddress: new Address
            {
                FirstName = GetString("PRICE_UPDATER_FIRST_NAME", envValues) ?? "Pricing",
                LastName = GetString("PRICE_UPDATER_LAST_NAME", envValues) ?? "Updater",
                Email = GetString("PRICE_UPDATER_EMAIL", envValues) ?? "pricing-updater@example.com",
                Phone = GetString("PRICE_UPDATER_PHONE", envValues) ?? "0000000000",
                Country = country,
                Region = region,
                Address1 = address1,
                Address2 = address2,
                City = city,
                Zip = zip,
                Company = GetString("PRICE_UPDATER_COMPANY", envValues)
            },
            ShippingMethod: PricingShippingMethodExtensions.Parse(shippingMethodText),
            Interval: TimeSpan.FromMinutes(intervalMinutes),
            ApplyChanges: applyChanges,
            RunOnce: runOnce,
            RequestDelayMs: requestDelayMs,
            ProductLimit: productLimit,
                VariantLimitPerProduct: variantLimitPerProduct,
            MarginPercent: marginPercent);
    }

    public Shop ResolveShop(IReadOnlyList<Shop> shops)
    {
        if (shops.Count == 0)
        {
            throw new InvalidOperationException("No Printify shops were returned for the configured token.");
        }

        if (ConfiguredShopId.HasValue)
        {
            return shops.FirstOrDefault(shop => shop.Id == ConfiguredShopId.Value)
                ?? throw new InvalidOperationException($"Configured shop ID {ConfiguredShopId.Value} was not found on this Printify account.");
        }

        return shops.FirstOrDefault(shop =>
                   string.Equals(shop.SalesChannel, "custom_integration", StringComparison.OrdinalIgnoreCase))
               ?? shops[0];
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

    private static bool HasFlag(IEnumerable<string> args, string flag)
    {
        return args.Any(arg => string.Equals(arg, flag, StringComparison.OrdinalIgnoreCase));
    }

    private static string? TryReadStringOption(string[] args, string optionName)
    {
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

            return args[index + 1].Trim();
        }

        return null;
    }

    private static int? TryReadIntOption(string[] args, string optionName)
    {
        var value = TryReadStringOption(args, optionName);
        if (value is null)
        {
            return null;
        }

        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedValue) && parsedValue > 0)
        {
            return parsedValue;
        }

        throw new InvalidOperationException($"Value for {optionName} must be a positive integer.");
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
}

enum PricingShippingMethod
{
    Standard,
    Express,
    Priority,
    PrintifyExpress,
    Economy,
    LowestAvailable
}

static class PricingShippingMethodExtensions
{
    public static PricingShippingMethod Parse(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "standard" => PricingShippingMethod.Standard,
            "express" => PricingShippingMethod.Express,
            "priority" => PricingShippingMethod.Priority,
            "printify_express" => PricingShippingMethod.PrintifyExpress,
            "economy" => PricingShippingMethod.Economy,
            "lowest" or "lowest_available" => PricingShippingMethod.LowestAvailable,
            _ => throw new InvalidOperationException(
                $"Unsupported shipping method '{value}'. Expected one of: standard, express, priority, printify_express, economy, lowest_available.")
        };
    }

    public static string ToConfigValue(this PricingShippingMethod method)
    {
        return method switch
        {
            PricingShippingMethod.Standard => "standard",
            PricingShippingMethod.Express => "express",
            PricingShippingMethod.Priority => "priority",
            PricingShippingMethod.PrintifyExpress => "printify_express",
            PricingShippingMethod.Economy => "economy",
            PricingShippingMethod.LowestAvailable => "lowest_available",
            _ => method.ToString().ToLowerInvariant()
        };
    }
}