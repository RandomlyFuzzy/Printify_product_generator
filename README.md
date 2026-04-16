# Printify_prodcuct_generator

## Dashboard

`src/PrintifyGenerator.Dashboard` is a local ASP.NET Core dashboard for the generator workflow.

It currently provides:

- A gallery of generated images under `src/data/Checking`
- Counts for generated, reviewed, ready-for-draft, and already-drafted images
- A manager for ComfyUI and Ollama node URLs stored in `src/data/staging/orchestration-settings.json`
- Per-image publish overrides stored in `src/data/staging/publishing-overrides.json`

The maker process now reads the same orchestration settings file and round-robins image generation and suitability work across all enabled nodes.

Run the dashboard locally:

```bash
dotnet run --project src/PrintifyGenerator.Dashboard
```

## Cache generator

`src/PrintifyGenerator.CacheGenerator` can build cached catalog data, variant-image caches, and published pricing products for later production-cost lookups.

The pricing-product mode publishes one product per `100` printable variants using the title format `BlueprintName-ProviderName-PageNumber`, keeps those products published for later production/shipping price lookups, and stores its local resume state in `src/data/Cached/pricing_products_shop_<shopId>.json`.

Run it with:

```bash
dotnet run --project src/PrintifyGenerator.CacheGenerator -- pricing-products --shop-id 27152940
```

Behavior:

- Existing matching products in the target shop are reused automatically.
- Published pricing products stay in the shop for later lookups; they are only deleted when you run with `--reset-cache`.
- Use `--reset-cache` to delete matching pricing products and rebuild them from scratch.
- `--shop-id` overrides `SHOP_ID` and `PRICE_UPDATER_SHOP_ID` for the publish target.

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
