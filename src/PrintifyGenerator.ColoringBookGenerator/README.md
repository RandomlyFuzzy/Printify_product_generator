# PrintifyGenerator.ColoringBookGenerator

CLI tool that generates and publishes picture books, story books, or paint-by-numbers books to Printify (District Photo, blueprint 2721, 8.5" × 11").

## Usage

```
dotnet run -- [--book-type=<type>] [--count=<n>] [title1 title2 ...]
```

### Arguments

| Argument | Description | Values | Default |
|---|---|---|---|
| `--book-type=` | Type of book / prompt provider | `picturebook` / `picture`, `storybook` / `story`, `paintbynumbers` / `paint-by-numbers` / `paint` | `picturebook` |
| `--count=` | Target number of titles to auto-generate | Positive integer | `1` |
| `title1 title2 ...` | One or more book titles (positional args) | Any string | Loaded from `PictureBookIdeas` file, or auto-generated via Ollama |

All non-flag arguments are treated as book titles. If no titles are provided and `PictureBookIdeas` file exists, titles are loaded from it. If both are empty, titles are auto-generated via Ollama.

### Examples

```bash
# Auto-generate 1 picture book title and publish it
dotnet run

# Auto-generate 3 story book titles
dotnet run --book-type=story --count=3

# Generate 5 paint-by-numbers titles
dotnet run --book-type=paint --count=5

# Specific titles
dotnet run "Jungle Safari Coloring Book" "Ocean Adventures Coloring Book"

# Story book with explicit count (fills shortfall via Ollama)
dotnet run --book-type=story --count=5 "The Brave Little Rabbit"

# Short form aliases
dotnet run --book-type=picture --count=2
dotnet run --book-type=paint "Safari Animals Paint by Numbers"
```

### Book types

| `--book-type=` | Style | Fallback subjects | Title format |
|---|---|---|---|
| `picturebook` / `picture` | B&W line art, coloring book | 24 items | Ends with "Coloring Book" or "Colouring Book" |
| `storybook` / `story` | Full-color watercolor, storybook | 13 items (coherent narrative) | Story title (e.g. "The Brave Little Rabbit's Adventure") |
| `paintbynumbers` / `paint-by-numbers` / `paint` | Numbered-section line art, paint-by-numbers | 24 items | Includes "Paint by Numbers" |

### Configuration

| Item | Location | Format |
|---|---|---|
| Printify API token | `main.env` (project root or parent) | `TOKEN=eyJ...` |
| Title ideas file | `PictureBookIdeas` (project root) | One title per line, `#` for comments |
| Ollama URL | Hardcoded in `Program.cs` | Default: `http://192.168.0.181:11434` |
| Ollama model | Hardcoded in `Program.cs` | Default: `gemma4:e2b` |
| ComfyUI URL | Hardcoded in `Program.cs` | Default: `http://192.168.0.181:8188` |
| Etsy shop ID | Hardcoded in `Program.cs` | `27152940` |
| Blueprint | District Photo 8.5" × 11" | ID `2721` |
| Interior pages | 24 per book | |

### Output

Each book is published as a draft product on Printify. On success:

```
  ✓ Done in 05:23
  ✓ Product ID : 12345678
  ✓ View at    : https://printify.com/app/store/products/12345678/edit
```

All prompts sent to Ollama and image generators are logged to `prompts.json` in each output directory.
