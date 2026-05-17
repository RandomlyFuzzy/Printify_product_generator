# PrintifyGenerator.ColoringBookGenerator

Small CLI that loads a Printify blueprint JSON and generates a simple children's colouring book as SVG pages. The image-generator is intentionally simple and extendable; add new classes implementing `IImageGenerator` to produce PNGs, vector art, or call external AI/image services.

Usage:

dotnet run --project src/PrintifyGenerator.ColoringBookGenerator/ [path/to/blueprint.json] [output/folder]

Defaults: `src/data/Cached/blueprint_details/2721.json` and `output/` + a generated folder name.

Files of interest:
