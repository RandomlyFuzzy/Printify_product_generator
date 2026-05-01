# Analytics API Endpoints

Base URL: `http://localhost:5219`

## Service

| Method | Route | What it does |
| --- | --- | --- |
| GET | `/` | Health/status entrypoint. Returns service name, current status, and the OpenAPI document path. |

## Phase Data

| Method | Route | What it does |
| --- | --- | --- |
| GET | `/api/phases/bundles?limit=100` | Lists recent phase bundles, including bundle path, completed phases, and last update timestamp. |
| GET | `/api/phases/overview` | Returns aggregate counts and last-update timestamps per phase. |
| GET | `/api/phases/bundles/{bundleId}` | Returns one bundle summary by bundle ID. Returns `404` if the bundle does not exist. |
| GET | `/api/phases/bundles/{bundleId}/phase/{phase}` | Lists artifacts for one bundle and phase. |
| GET | `/api/phases/artifacts/{phase}?limit=200` | Returns the latest artifacts for a phase across bundles. |
| GET | `/api/phases/bundles/{bundleId}/artifacts/{fileName}` | Returns one artifact payload for a bundle. Supports text or JSON artifacts. Returns `404` if not found. |
| POST | `/api/phases/ingest/product-definition` | Persists a product-definition payload into phase storage and returns the bundle/file path written. |
| POST | `/api/phases/ingest/artifact` | Persists a generic phase artifact payload and returns the bundle/file path written. |

## Market Intelligence

| Method | Route | What it does |
| --- | --- | --- |
| GET | `/api/market/summary?top=20` | Returns top keywords, colors, and materials across all parsed market category files. |
| GET | `/api/market/categories` | Lists parsed market categories with category type and product count. |
| GET | `/api/market/categories/{name}` | Returns full detail for a category, including keywords, colors, materials, and price terms. Returns `404` if missing. |
| GET | `/api/market/categories/search?type=PhraseCluster&limit=20` | Filters category details by category type and limits the result count. |
| GET | `/api/market/overlap?minCategories=3&top=30` | Returns features that overlap across multiple categories to expose broad market signals. |
| POST | `/api/market/score` | Scores a candidate product against market data and returns the score, signal breakdown, recommendations, and notes. |
| POST | `/api/market/ingest/category-feature` | Writes or overwrites a category feature text file and invalidates the market cache. |

## Shared Client

The reusable typed client for these endpoints lives in `src/PrintifyGenerator.Library/modules/AnalyticsApi/AnalyticsApiClient.cs` and mirrors the request/response models in `src/PrintifyGenerator.Library/modules/AnalyticsApi/AnalyticsApiContracts.cs`.