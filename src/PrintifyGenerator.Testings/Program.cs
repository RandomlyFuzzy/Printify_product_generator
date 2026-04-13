// See https://aka.ms/new-console-template for more information



using System.Text.Json;

//get webpage content from "https://www.google.com/finance/quote/EUR-GBP"

// check all the different currenies combination

foreach(var code1 in Enum.GetValues<CurrencyCode>())
{
    foreach(var code2 in Enum.GetValues<CurrencyCode>())
    {
        if (code1 != code2)
        {
            var currency = new Currency(code1, 1.0m);
            // await Task.Delay(1000); // delay 1 second between requests to avoid being blocked by google
            var exchangeRate = await currency.GetCurrentExchangeRate(code2);
            Console.WriteLine($"Exchange rate from {code1} to {code2}: {exchangeRate}");
        }
    }
}