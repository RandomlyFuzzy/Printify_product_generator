# RL-enhanced Image Generation Pipeline

## Progress
| Component | Status | Tests |
|-----------|--------|-------|
| Data Models (VectorRecord, CosineSimilarity, ToolDefinition, ToolCall, ChatMessage) | ✅ | 24 |
| HnswIndex<T> (M=16, ef=200/50) | ✅ | 9 |
| VectorStore (scan, batch embed, persist, search) | ✅ | 6 |
| OllamaClient ChatWithToolsAsync (multi-turn tool loop) | ✅ | 6 |
| MarketDataEmbedder (parse category_features/*.txt → VectorStore) | ✅ | — |
| VectorSearch CLI (interactive REPL with 12 commands) | ✅ | — |
| RL Loop (generate → CLIP score → store → retrieve → refine) | ⏳ | — |
| **Total** | **168/168 passing** | **168** |

## Core Components

### 1. HNSW Vector Store (`PrintifyGenerator.Library/VectorStorage/`)
- **CosyneSimilarity** — L2-normalize → dot product. Guards: zero vectors return 0.
- **VectorRecord** — Embedding, Score, Concept, Prompt, Source, Timestamp
- **HnswIndex<T>** — Pure C# HNSW with M=16, efConstruction=200, efSearch=50, `GetAllMetadata()` for zero-embedding scans
- **VectorStore** — High-level wrapper: `SearchByText`, `SearchSimilar`, `GetTopByScore`, `GetByConcept`, `GetRecent`, `Store`, `Load/Save`, `ScanAllDataSources`, `BatchEmbedWithOllamaAsync`

### 2. Ollama Tool Integration (`PrintifyGenerator.Library/modules/GeneratingModules/ollama/`)
- **ToolDefinition** — Name, Description, Parameters (JSON Schema)
- **ToolCall** — Name, Arguments (Dictionary<string, JsonElement>)
- **ChatMessage** — Role, Content, ToolCalls, ToolCallId; `ToDictionary()` serialization
- **OllamaClient** — `GetEmbeddingVectorAsync()`, `ChatWithToolsAsync()` multi-turn loop, testable `HttpMessageHandler` constructor

### 3. Market Data Embedder (`PrintifyGenerator.Library/VectorStorage/MarketDataEmbedder.cs`)
- Parses `category_features/*.txt` files via `CategoryFeatureIntelligence` schema
- Generates text representation for embedding
- Calculates quality score from CTR lifts, trends
- Stores as `market_data` concept entries in the shared VectorStore

### 4. VectorSearch CLI (`PrintifyGenerator.VectorSearch/Program.cs`)
Interactive REPL with 12 commands:
- `<text>` — text substring search
- `/embed <text>` — semantic search via Ollama embedding
- `/scan` — scan `output/` + `Checking/` for prompt data
- `/market` — load market data from `category_features/`
- `/purchase <q>` — combined search across prompts + market data
- `/eval <prompt>` — RAG-enhanced quality prediction
- `/tools` — Ollama tool-calling demo (RAG + scoring)
- `/store` — stats (count, prompt/market breakdown, top scores)
- `/list [n]` — top n by score
- `/recent [n]` — n most recent
- `/concept <c>` — filter by concept
- `/save` — persist to disk

### 5. Technical Decisions
- System.Text.Json for new types, Newtonsoft.Json for existing
- Cosine distance = 1 - cosine_similarity
- No external vector DB — embedded HNSW with JSON persistence
- Tests: xUnit, [Fact], custom HttpMessageHandler for HTTP mocks
- Market data and prompt vectors share the same HNSW index for cross-modal RAG
