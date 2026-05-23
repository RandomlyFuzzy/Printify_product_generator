# PrintifyGenerator.ColoringBookGenerator

Generates and publishes coloring books, story books, or paint-by-numbers books to Printify (District Photo, blueprint 2721, 8.5" × 11").

## Configuration

| File | Purpose | Location |
|---|---|---|
| `main.env` | Printify API token (`TOKEN=...`) | Project root (or any parent directory up to 5 levels) |
| `PictureBookIdeas` | One title per line; `#` for comments | Project root (optional, only for picture book) |

### Token priority

1. Environment variable `TOKEN`
2. `TOKEN=...` in `main.env` (searches current dir, then parent dirs up to 5 levels)

### Ollama

A local Ollama instance must be running. The URL is hardcoded as `http://192.168.0.181:11434`.
The model used for theme/style derivation and title generation defaults to `gemma4:e2b` (hardcoded in `Program.cs`).

## CLI Arguments

```
dotnet run -- [--book-type=<type>] [--count=<n>] [title1 title2 ...]
```

| Argument | Description | Values | Default |
|---|---|---|---|
| `--book-type=` | Book type / prompt provider | `picturebook` / `picture`, `storybook` / `story`, `paintbynumbers` / `paint-by-numbers` / `paint` | `picturebook` |
| `--count=` | Target number of titles | Positive integer | `1` (when auto-generating) |

### Title sources (checked in this order)

1. **Positional args** — all non-flag arguments are treated as titles
2. **`PictureBookIdeas` file** — read line-by-line (skipping blanks and `#` lines); used only if no positional args
3. **Auto-generation via Ollama** — used if both of the above are empty; generates `--count=` titles using the provider's `BuildTitleGenerationPrompt`
4. **Title shortfall** — if `--count=` is greater than the number of titles provided via args/file, the difference is auto-generated

### Examples

Basic usage (auto-generate one picture book):
```
dotnet run
```

Two specific titles:
```
dotnet run "Jungle Safari Coloring Book" "Ocean Adventures Coloring Book"
```

Story book, 3 titles:
```
dotnet run --book-type=story --count=3
```

Paint by numbers, providing titles:
```
dotnet run --book-type=paint "Safari Animals Paint by Numbers" "Fairytale Castle Paint by Numbers"
```

## Book Types

### Picture Book (`--book-type=picturebook`)

| Property | Value |
|---|---|
| `BookType` | "Picture Book" |
| `BaseStyleTerms` | `thick black outlines, white fill, no shading, no color` |
| Style | Pure black and white line art, coloring book |
| Page subjects | 24 fallback items (each includes `{theme}`), or Ollama-generated per-page via `BuildPageSubjectPrompt` |
| Title format | Must end with "Coloring Book" or "Colouring Book" |
| Description | "A beautiful children's picture book with a {theme} theme. Includes a full-color cover and 24 black-and-white pages..." |
| Tags | picture book, kids, children, activity book, {theme}, coloring pages, printify |
| `BuildFullStoryPrompt` | `null` (not used) |
| Front cover | B&W line art, title centered, decorative lettering |
| Back cover | B&W line art, no text, leave open spaces for coloring |
| Interior pages | B&W line art, one scene per page, no text/logos/watermarks |

### Story Book (`--book-type=storybook`)

| Property | Value |
|---|---|
| `BookType` | "Story Book" |
| `BaseStyleTerms` | `warm colors, soft watercolor textures, gentle shading, storybook illustration` |
| Style | Full-color watercolor/painted storybook art |
| Page subjects | 13 fallback items (coherent narrative), or Ollama-generated continuous story via `BuildFullStoryPrompt` (13 segments, 2-4 sentences each) |
| Title format | Story-style titles (e.g. "The Brave Little Rabbit's Adventure") |
| Description | "A charming children's story book with a {theme} theme. Features a full-color cover and 24 beautifully illustrated story pages..." |
| Tags | story book, children's book, bedtime stories, kids, {theme}, illustrated story, printify |
| `BuildFullStoryPrompt` | Returns a prompt for Ollama to generate 13 sequential story segments as JSON array |
| Front cover | Full-color, watercolor style, title + author area, narrative scene |
| Back cover | Full-color, concluding teaser scene, space for blurb and barcode |
| Interior pages | Full-color landscape layout, space reserved for story text, consistent characters |

### Paint by Numbers (`--book-type=paintbynumbers`)

| Property | Value |
|---|---|
| `BookType` | "Paint by Numbers" |
| `BaseStyleTerms` | `thick black outlines, numbered sections, polygonal regions, paint-by-number style` |
| Style | Line art with numbered sections 1-10, polygonal regions, thick outlines |
| Page subjects | 24 fallback items (each includes `{theme}`), or Ollama-generated per-page via `BuildPageSubjectPrompt` |
| Title format | Must include "Paint by Numbers" |
| Description | "A fun paint-by-numbers activity book with a {theme} theme. Includes a full-color cover and 24 paint-by-number activity pages..." |
| Tags | paint by numbers, painting book, activity book, kids, children, {theme}, paint by number, printify |
| `BuildFullStoryPrompt` | `null` (not used) |
| Front cover | Numbered sections, line art, title centered |
| Back cover | Numbered sections, simple scene, no text |
| Interior pages | 20-40 numbered regions per page, closed polygons, no overlapping sections |

## Internal Flow

```
Program.Main()
  ├── Load token from main.env / env var
  ├── Parse --book-type= → select IPromptProvider
  ├── Collect titles (args → file → auto-generate via Ollama)
  ├── For each title:
  │   ├── GenerateThemeAndStyleFromTitleAsync(title)
  │   │   ├── Calls Ollama with BuildThemeAndStylePrompt(title)
  │   │   └── Returns {theme, style} JSON
  │   └── ColoringBookService.GenerateAndPublishAsync(title, theme, styleAddon)
  │       ├── GenerateFrontCoverImage()
  │       ├── GenerateBackCoverImage()
  │       ├── For each of 24 pages:
  │       │   ├── Get subject (from Ollama or fallback list)
  │       │   └── GeneratePageImage()
  │       ├── Upload to Printify
  │       └── Publish to Etsy shop 27152940
  └── Return exit code
```

## Prompt Providers (`IPromptProvider`)

| Method | Returns | Used By |
|---|---|---|
| `BuildFrontCoverPrompt(title, theme, styleAddon)` | Full prompt string | Front cover image generation |
| `BuildBackCoverPrompt(theme, styleAddon)` | Full prompt string | Back cover image generation |
| `BuildPagePrompt(pageNumber, subject, styleAddon)` | Full prompt string | Interior page generation |
| `BuildPageSubjectPrompt(theme, pageNumber)` | Prompt for Ollama to generate one subject | Individual page subject generation |
| `BuildPageSubjectsFallback(theme)` | `string[]` of 24 (picture/paint) or 13 (story) subjects | Subject waterfall fallback |
| `BuildThemeAndStylePrompt(title)` | Prompt for Ollama to derive theme + style | Theme and style extraction from title |
| `BuildTitleGenerationPrompt(count)` | Prompt for Ollama to generate N titles | Title auto-generation |
| `BuildFullStoryPrompt(theme)` | Prompt (story/paint returns `null`) | Full coherent story generation |
| `BuildDescription(theme)` | Product description string | Printify product listing |
| `BuildTags(theme)` | `string[]` of 7 tags | Printify product tags |

### Subject resolution priority

For each page, subjects are resolved in this order:

1. If story mode (`BuildFullStoryPrompt` returns non-null), subjects come from the single Ollama call to `GenerateFullStoryAsync`
2. Otherwise, per-page via Ollama call using `BuildPageSubjectPrompt`
3. If Ollama fails, use `BuildPageSubjectsFallback` (rotated to start at a random offset)

## Prompt Logging

All prompts sent to Ollama or image generators are recorded via `PromptRecorder` and flushed to `prompts.json` in the output directory.

| Log Key | Source | Content |
|---|---|---|
| `{title}__full_story` | `ColoringBookService.GenerateFullStoryAsync` | Full story prompt sent to Ollama |
| `{title}__title_generation` | `Program.GenerateTitlesAsync` | Title generation prompt |
| `{title}__generated_titles` | `Program.GenerateTitlesAsync` | Raw generated titles array |
| `{title}__title->theme` | `Program.GenerateThemeAndStyleFromTitleAsync` | Theme+style derivation prompt |
| `{title}__page_subject_{n}` | `ColoringBookService.GetPageSubjectAsync` | Per-page subject generation prompt |
| `{title}__parsed_segments` | `ColoringBookService.GenerateFullStoryAsync` | Parsed story segments |
| `{title}__raw_response` | `ColoringBookService.GenerateFullStoryAsync` | Raw Ollama response for story |
| `final_prompt_front_cover` | `FreeGenGenerator` | Final prompt sent to image API for cover |
| `final_prompt_back_cover` | `FreeGenGenerator` | Final prompt for back cover |
| `final_prompt_page_01` .. `final_prompt_page_24` | `FreeGenGenerator` | Final prompts for each interior page |
| `final_prompt_{baseName}` | `ComfyUiGenerator` | Final prompt sent to ComfyUI |

## Hardcoded Values

| Setting | Value | Location |
|---|---|---|
| Blueprint ID | `2721` | Banner |
| Product | District Photo 8.5" × 11" | Banner |
| Etsy Shop ID | `27152940` | `Program.cs:91` |
| Ollama URL | `http://192.168.0.181:11434` | `Program.cs:97,262,319` |
| Ollama model | `gemma4:e2b` | `Program.cs:17` |
| ComfyUI URL | `http://192.168.0.181:8188` | `Program.cs:94` |
| Interior pages | `24` | Everywhere |

## Output

Each generated book is published to the configured Etsy shop on Printify. On success, the program prints:

```
  ✓ Done in 05:23
  ✓ Product ID : 12345678
  ✓ View at    : https://printify.com/app/store/products/12345678/edit
```
