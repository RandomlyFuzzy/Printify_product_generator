# Printify_prodcuct_generator

## Price updater

`src/PrintifyGenerator.PriceUpdater` periodically recalculates Printify product variant prices for a shop and can push the updated prices back to Printify.

The pricing function is defined in `Program.cs` as a `Func<int, int, int>` that receives `(shippingPrice, productionPrice)`. It currently returns a flat 40% markup over production cost, so shipping is available to the function but not used by the default formula yet.

Required configuration:

- `TOKEN`
- `PRICE_UPDATER_COUNTRY`

Optional configuration:

- `SHOP_ID` or `PRICE_UPDATER_SHOP_ID`
- `PRICE_UPDATER_REGION`
- `PRICE_UPDATER_ZIP`
- `PRICE_UPDATER_INTERVAL_MINUTES` (default `60`)
- `PRICE_UPDATER_MARGIN_PERCENT` (default `40`)
- `PRICE_UPDATER_SHIPPING_METHOD` (`standard`, `express`, `priority`, `printify_express`, `economy`, or `lowest_available`)
- `PRICE_UPDATER_REQUEST_DELAY_MS` (default `200`)
- `PRICE_UPDATER_PRODUCT_LIMIT`
- `PRICE_UPDATER_VARIANT_LIMIT`
- `PRICE_UPDATER_APPLY_CHANGES` (`false` by default)

Run a single dry-run pass:

```bash
PRICE_UPDATER_COUNTRY=GB dotnet run --project src/PrintifyGenerator.PriceUpdater -- --once --limit-products 1 --limit-variants 5
```

Run continuously and apply live updates:

```bash
PRICE_UPDATER_COUNTRY=GB PRICE_UPDATER_APPLY_CHANGES=true dotnet run --project src/PrintifyGenerator.PriceUpdater
```
