# Printify Product Generator

Automated print-on-demand product generation system for [Printify](https://printify.com). Researches trending products, generates AI artwork via Stable Diffusion, validates images with LLMs, creates Printify products with SEO metadata, and publishes to eBay/Etsy.

## System Requirements

| Dependency | Version | Required For |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | 10.0+ | All C# projects |
| [Ollama](https://ollama.ai) | Latest | LLM-based prompt generation, image suitability checks, SEO metadata |
| [ComfyUI](https://github.com/comfyanonymous/ComfyUI) | Latest | Stable Diffusion image generation |
| [Node.js](https://nodejs.org/) | 18+ | JS-based web crawler (`src/DataGathering/crawler/`) |
| [Printify API Token](https://developers.printify.com/) | - | All Printify API operations |

Recommended Ollama models (configured in `orchestration-settings.json`):
- `llama3.2:1b` — prompt generation
- `gemma4:e2b` or `gemma4:latest` — image suitability + mockup vision

## Quick Start

```bash
# Set your Printify API token (see "Environment Setup" below)
export TOKEN=your_printify_api_token_here

# Run the dashboard (for reviewing generated images)
dotnet run --project src/PrintifyGenerator.Dashboard

# Run the full generation pipeline
dotnet run --project src/PrintifyGenerator.Combined

# Run tests
dotnet test src/PrintifyGenerator.Library.Tests
```

## Environment Setup

Create a `main.env` file in the project root (it's in `.gitignore`):

```env
TOKEN=your_printify_api_token_here
SHOP_ID=your_shop_id
```

Or export as environment variables:

```bash
export TOKEN=eyJ...                # Printify API token (required)
export SHOP_ID=123456              # Your Printify shop ID
export PRICE_UPDATER_COUNTRY=GB    # Country for price calculations
```

## Project Structure

### Core Library
- **`PrintifyGenerator.Library`** — Shared library: Printify API client, eBay/Etsy selling clients, ComfyUI/Ollama clients, data models, orchestration settings, publishing overrides

### Main Pipeline
- **`PrintifyGenerator.Combined`** — 10-phase generation pipeline:
  1. Product ideation (queries best blueprint + category combos)
  2. Prompt generation via Ollama LLM
  3. Image generation via ComfyUI (Stable Diffusion)
  4. Suitability checks (IP violations, NSFW, print quality)
  5. Image upscaling
  6. Printify product creation as drafts
  7. SEO metadata generation (titles, descriptions, tags)
  8. Publishing decision (eBay, Etsy, or both)
  9. Pricing calculation with configurable markups
  10. Manual publishing review

### Web Dashboard
- **`PrintifyGenerator.Dashboard`** — ASP.NET Core Razor Pages app for reviewing generated images, managing the QC queue, and configuring ComfyUI/Ollama nodes
  ```bash
  dotnet run --project src/PrintifyGenerator.Dashboard
  ```

### Support Tools
| Project | Purpose | Run Command |
|---|---|---|
| `PrintifyGenerator.CacheGenerator` | Builds cached Printify catalog data | `dotnet run --project src/PrintifyGenerator.CacheGenerator -- pricing-products --shop-id <id>` |
| `PrintifyGenerator.AnalyticsApi` | Serves phase data and market feature intelligence | `dotnet run --project src/PrintifyGenerator.AnalyticsApi` |
| `PrintifyGenerator.Testings` | Cleans up orphaned Printify products | `dotnet run --project src/PrintifyGenerator.Testings` |
| `PrintifyGenerator.BlueprintCategoryAudit` | Audits blueprints against category data | `dotnet run --project src/PrintifyGenerator.BlueprintCategoryAudit` |
| `PrintifyGenerator.PriceUpdater` | Recalculates variant prices and pushes to Printify | See "Price Updater" below |

### Data Gathering
| Project | Purpose | Run Command |
|---|---|---|
| `PrintifyGenerator.Researcher` | Data science pipeline: processes Amazon reviews, eBay listings, CTR data → computes category features | `dotnet run --project src/DataGathering/PrintifyGenerator.Researcher` |
| `PrintifyGenerator.Scraper` | Scrapes eBay/Etsy listings via PuppeteerSharp | `dotnet run --project src/DataGathering/PrintifyGenerator.Scraper` |

### Tests
- **`PrintifyGenerator.Library.Tests`** — xUnit unit tests for core library logic
  ```bash
  dotnet test src/PrintifyGenerator.Library.Tests
  ```

## Key Configuration Files

| File | Purpose |
|---|---|
| `src/data/staging/orchestration-settings.json` | ComfyUI/Ollama node URLs, LLM model names, minimum publish score |
| `src/data/staging/publishing-overrides.json` | Per-image manual allow/block overrides |
| `config.json` | Root config (project-specific overrides) |

## Price Updater

Recalculates and optionally pushes Printify product prices:

```bash
# Dry run (single pass, no changes applied)
PRICE_UPDATER_COUNTRY=GB dotnet run --project src/PrintifyGenerator.PriceUpdater -- --once --limit-products 1 --limit-variants 5

# Live run (applies price changes every 60 minutes)
PRICE_UPDATER_COUNTRY=GB PRICE_UPDATER_APPLY_CHANGES=true dotnet run --project src/PrintifyGenerator.PriceUpdater
```

| Env Variable | Default | Description |
|---|---|---|
| `TOKEN` | — | Printify API token (required) |
| `PRICE_UPDATER_COUNTRY` | — | Country for shipping calculations (required) |
| `PRICE_UPDATER_MARGIN_PERCENT` | `40` | Markup percent over production cost |
| `PRICE_UPDATER_INTERVAL_MINUTES` | `60` | How often to recheck prices |
| `PRICE_UPDATER_APPLY_CHANGES` | `false` | Set to `true` to push changes to Printify |
| `PRICE_UPDATER_SHIPPING_METHOD` | `standard` | `standard`, `express`, `priority`, `printify_express`, `economy`, or `lowest_available` |

## API Token Security

⚠️ **Never commit your Printify API token to git.** The file `main.env` is listed in `.gitignore` for local config. Set `TOKEN` via environment variable in production.
