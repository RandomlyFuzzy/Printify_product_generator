

using System.Text.Json;

public class Currency {

    private static HttpClient client = new HttpClient() { Timeout = TimeSpan.FromSeconds(30) };
    public CurrencyCode Code { get; set; }
    public decimal Amount { get; set; }

    public Currency(CurrencyCode code, decimal amount)
    {
        Code = code;
        Amount = amount;
    }

    public Currency ConvertTo(CurrencyCode targetCode, decimal exchangeRate)
    {
        decimal targetAmount = Amount * exchangeRate;
        return new Currency(targetCode, targetAmount);
    }

    public async Task<decimal> GetCurrentExchangeRate(CurrencyCode targetCode)
    {
        //read file see if there is a recent exchange rate for this currency pair (within the last 12 hours)
        string filePath = $"./src/data/Cached/exchange_rates/{Code}_{targetCode}.txt";
        if (File.Exists(filePath))
        {            
            string lastLine = File.ReadLines(filePath).LastOrDefault() ?? "0";
            var lastValue = lastLine.Split(',').LastOrDefault()?.Trim() ?? "0";
            var lastTimestamp = long.Parse(lastLine.Split(',').FirstOrDefault() ?? "0");
            if (DateTime.UtcNow - new DateTime(lastTimestamp) < TimeSpan.FromHours(12))
            {                
                // Console.WriteLine($"[Currency] Using cached exchange rate for {Code} to {targetCode}: {lastValue} at {new DateTime(lastTimestamp)}");
                return decimal.Parse(lastValue);
            }
        }

        var response = await client.GetAsync($"https://open.er-api.com/v6/latest/{Code}");
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        if (root.TryGetProperty("rates", out var rates) && rates.TryGetProperty(targetCode.ToString(), out var rateEl))
        {
            decimal exchangeRate = rateEl.GetDecimal();
            string filePath2 = $"./src/data/Cached/exchange_rates/{targetCode}_{Code}.txt";
            Directory.CreateDirectory(Path.GetDirectoryName(filePath) ?? "./src/data/Cached/exchange_rates");
            Directory.CreateDirectory(Path.GetDirectoryName(filePath2) ?? "./src/data/Cached/exchange_rates");
            await File.AppendAllTextAsync(filePath, $"{DateTime.UtcNow.Ticks}, {exchangeRate}{Environment.NewLine}");
            await File.WriteAllTextAsync(filePath2, $"{DateTime.UtcNow.Ticks}, {1.0m / exchangeRate}{Environment.NewLine}");
            return exchangeRate;
        }

        throw new NotImplementedException($"Exchange rate from {Code} to {targetCode} not found in API response.");
    }


    public async Task<Currency> ConvertTo(CurrencyCode targetCode)
    {   
        decimal exchangeRate = await GetCurrentExchangeRate(targetCode);
        return ConvertTo(targetCode, exchangeRate);
    }

    public override string ToString()
    {
        return $"{Amount:C2} {Code}";
    }
}

public enum CurrencyCode
{
    GBP,
    USD,
    EUR,
    JPY,
    AUD,
    CAD,
    CHF,
    CNY,
    SEK,
    NZD
}