using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;



//i want to see the all items in the catalog
string token = "";
if (File.Exists("./main.env"))
{
    var lines = File.ReadAllLines("./main.env");
    foreach (var line in lines)    {
        if (line.StartsWith("TOKEN="))
        {
            token = line.Substring("TOKEN=".Length).Trim();
            break;
        }
    }
}
PrintifyClient client = new PrintifyClient(token);
var ebay = new EbayClient(new EbayConfig {
    ClientId = "your-app-id",
    ClientSecret = "your-cert-id",
    RuName = "your-ru-name",
    UseSandbox = true
});



//list all items in the catalog
var items = await client.GetBlueprintsAsync();
foreach (var item in items)
{
    Console.WriteLine(item);
}

// 1. Get user consent URL, exchange code, then all sub-clients auto-use the token
var url = ebay.OAuth.BuildUserConsentUrl(EbayConfig.DefaultSellerScopes, state: "xyz");
// …after redirect: await ebay.OAuth.ExchangeCodeForTokenAsync(code);

// 2. Persist/restore tokens across sessions
var saved = ebay.GetCurrentUserToken();     // serialize & store securely
ebay.LoadUserToken(saved);                  // restore on next run

// 3. Full seller workflow
await ebay.Inventory.CreateOrReplaceInventoryItemAsync(sku, item);
await ebay.Inventory.CreateOfferAsync(offer);
await ebay.Inventory.PublishOfferAsync(offerId);
var orders = await ebay.Fulfillment.GetOrdersAsync(filter: "orderfulfillmentstatus:{NOT_STARTED}");
await ebay.Fulfillment.CreateShippingFulfillmentAsync(orderId, fulfillment);
var policies = await ebay.Account.GetFulfillmentPoliciesAsync();
var payouts = await ebay.Account.GetPayoutsAsync();
var campaigns = await ebay.Marketing.GetCampaignsAsync();