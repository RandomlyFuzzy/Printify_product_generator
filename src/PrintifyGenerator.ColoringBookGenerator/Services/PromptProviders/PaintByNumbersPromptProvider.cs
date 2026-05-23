using PrintifyGenerator.ColoringBookGenerator.Models;

namespace PrintifyGenerator.ColoringBookGenerator.Services.PromptProviders
{
    public class PaintByNumbersPromptProvider : IPromptProvider
    {
        public string BookType => "Paint by Numbers";
        public string BaseStyleTerms => "thick black outlines, numbered sections, polygonal regions, paint-by-number style";
        public BlueprintSpec Blueprint => new()
        {
            BlueprintId = 2721,
            PrintProviderId = 28,
            VariantIds = new() { 148586 },
            DefaultVariantId = 148586,
            PageWidth = 2625,
            PageHeight = 3375,
            CoverWidth = 5175,
            CoverHeight = 3375,
            PageCount = 24,
            PageAspectRatio = "3:4",
            CoverAspectRatio = "3:4",
            CoverSpreadAspectRatio = "23:15",
            SpreadAspectRatio = "14:9"
        };

        public string BuildFrontCoverPrompt(string title, string theme, string styleAddon)
        {
            return $@"
        Create a paint-by-numbers FRONT COVER illustration.

        Title:
        {title}

        Theme:
        {theme}

        CRITICAL TITLE REQUIREMENT:
        - The title MUST be clearly visible and perfectly legible
        - The title must be well-formed, correctly spelled, and centered
        - Use a decorative but readable lettering style
        - The title should be integrated into the cover design
        - Ensure strong contrast so the title stands out
        - Do NOT draw any barcode, barcode-like marks, price tags, or unrelated text in the image
        - Leave clean space at the bottom center for barcode placement

        Style Requirements:
        - Line art with clearly numbered sections throughout
        - Each section should contain a small number indicating paint color
        - Numbers should be 1-10, placed inside each region
        - Thick black outlines separating each numbered region
        - No grayscale, shading, or color fill
        - White background
        - Professional paint-by-numbers cover style
        - Kid-friendly and visually engaging

        Additional Style:
        {styleAddon}

        Composition:
        - Central focal illustration related to the theme
        - Title positioned prominently (top or center)
        - Decorative framing elements around the title
        - Balanced, symmetrical, polished cover layout
        - Clear separation between title and illustration
        - No extra text besides the title

        Output Style:
        Line art with numbered regions, thick clear outlines, paint-by-numbers cover design, numbered sections 1 through 10.
        ";
        }

        public string BuildBackCoverPrompt(string theme, string styleAddon)
        {
            return $@"
            Create a paint-by-numbers BACK COVER illustration.

            Theme:
            {theme}

            Style Requirements:
            - Line art with clearly numbered sections
            - Thick black outlines between each region
            - Numbers 1-10 placed inside each section
            - No grayscale, shading, or color
            - White background
            - Paint-by-numbers style
            - Kid-friendly and visually engaging
            - Balanced composition with good section distribution

            Additional Style:
            {styleAddon}

            Composition:
            - Full-page vertical layout
            - Simple scene that works well divided into numbered regions
            - Center-focused design
            - Symmetrical and visually appealing
            - Avoid text, logos, watermarks, page numbers, or barcode/price labels
            - Leave clean space at the bottom center for barcode placement

            Output Style:
            Numbered line art, paint-by-numbers, thick outlines, numbered sections, clear polygonal regions.
        ";
        }

        public string BuildPagePrompt(int pageNumber, string subject, string styleAddon)
        {
            return $@"
        Create a paint-by-numbers illustration for PAGE {pageNumber}.

        Theme:
        {subject}

        Style Requirements:
        - Line art with clearly numbered sections throughout
        - Each section must contain a small number (1-10) indicating paint color
        - Numbers should be centered inside each region, roughly 12pt size
        - Thick black outlines separating every numbered region
        - Clear polygonal sections that are easy to paint within the lines
        - No grayscale, shading, or color fill
        - White background
        - Kid-friendly and visually engaging
        - Good variety of section sizes (some large, some small for detail)

        Additional Style:
        {styleAddon}

        Composition:
        - Full-page vertical layout
        - Scene divided into 20-40 numbered regions
        - Center-focused subject with sections radiating outward
        - Balanced distribution of section sizes
        - No overlapping or ambiguous regions
        - Avoid text, logos, watermarks, page numbers, or barcode/price labels inside the image

        Page Design Guidance:
        - Make this page distinct from other pages
        - Ensure all sections are closed polygons (no open lines)
        - Sections should follow natural contours of the subject
        - Maintain consistent line thickness across all sections
        - Create sections that are fun and satisfying to paint

        Output Style:
        Numbered paint-by-numbers line art, thick clear outlines, polygonal regions with numbers, no color fill.
        ";
        }

        public string BuildPageSubjectPrompt(string theme, int pageNumber)
        {
            return
                $"You are creating a paint-by-numbers activity book with the theme \"{theme}\".\n" +
                $"Generate a single scene description for page {pageNumber} that would work well divided into numbered painting sections.\n" +
                "The subject should have clear shapes, distinct regions, and be suitable for dividing into numbered paint-by-number sections (20-40 sections).\n" +
                "Describe a simple scene with distinct objects that have clear boundaries.\n" +
                "Return ONLY the scene description as plain text. No explanation, no markdown, no extra text.\n" +
                "Example: a sailboat on calm water with clouds in the sky and palm trees on the shore";
        }

        public string[] BuildPageSubjectsFallback(string theme)
        {
            var t = theme.ToLowerInvariant();
            return new[]
            {
                $"a simple {t} animal with large distinct body sections",
                $"a {t} flower with layered petals and leaves",
                $"a {t} house with clear roof, walls, windows and door sections",
                $"a {t} tree with separate trunk, branches and leaf sections",
                $"a {t} car with distinct body, wheels and window panels",
                $"a {t} butterfly with symmetrical wing sections",
                $"a {t} fish with separate fin, body and scale sections",
                $"a {t} boat with hull, sail and mast sections",
                $"a {t} fruit bowl with distinct fruit shapes",
                $"a {t} castle with towers, walls and gate sections",
                $"a {t} rocket with separate body, fins and window sections",
                $"a {t} dinosaur with large distinct body regions",
                $"a {t} hot air balloon with panel sections and basket",
                $"a {t} windmill with blades, body and door sections",
                $"a {t} train with separate engine, cars and wheels",
                $"a {t} owl with distinct wing, body and face sections",
                $"a {t} submarine with hull, periscope and window sections",
                $"a {t} birthday cake with separate tiers and decorations",
                $"a {t} lighthouse with distinct tower, light and roof sections",
                $"a {t} camel with clear humps, body and leg sections",
                $"a {t} telescope on a tripod with distinct mechanical sections",
                $"a {t} merry-go-round with clear animal and post sections",
                $"a {t} teapot with separate body, spout, lid and handle sections",
                $"a {t} pirate ship with distinct hull, mast and flag sections",
            };
        }

        public string BuildThemeAndStylePrompt(string title)
        {
            return $@"You are a product copywriter specialising in paint-by-numbers activity books (ages 5-12).

Task: Given a single BOOK TITLE, produce a concise 'theme' and a 'style' string suitable for generating paint-by-numbers illustrations.

Rules for the 'theme' field:
- 10-20 words, lower-case, no punctuation suitable for a child to understand.
- Describe the subject matter (e.g. 'safari animals', 'outer space vehicles', 'underwater creatures').
- Do NOT include the words 'paint', 'number', or 'book'.

Rules for the 'style' field:
- A comma-separated list of image-generation keywords for ONE illustration.
- MUST include the base terms: ""{BaseStyleTerms}"".
- Add 3-5 short descriptors (subject matter, composition, layout). Keep phrases short.
- Do NOT mention paper, page layout, borders, or the words 'paint by numbers'.

BOOK TITLE:
""{title}""

OUTPUT FORMAT:
- Return ONLY one valid JSON object with exactly two fields: ""theme"" and ""style"".
- No markdown, no explanation, no extra text. Start with {{ and end with }}.

Examples:
{{""theme"":""safari animals with geometric sections"",""style"":""{BaseStyleTerms}, safari animals, geometric sections, clear polygonal regions""}}
{{""theme"":""underwater fish and coral scene"",""style"":""{BaseStyleTerms}, underwater scene, distinct creature sections, coral reef details""}}";
        }

        public string BuildDescription(string theme)
            => $"A fun paint-by-numbers activity book with a {theme} theme. " +
               "Includes a full-color cover and 24 paint-by-number activity pages. " +
               "Each page features numbered sections to paint by color. " +
               "Perfect for kids aged 5-12. Printed on high-quality 8.5\" x 11\" paper.";

        public string[] BuildTags(string theme)
            => new[]
            {
                "paint by numbers", "painting book", "activity book", "kids", "children",
                theme.ToLowerInvariant(), "paint by number", "printify"
            };

        public string? BuildFullStoryPrompt(string theme) => null;

        public string BuildTitleGenerationPrompt(int count)
        {
            return $@"You are a product copywriter for a print-on-demand paint-by-numbers activity book store.

Generate exactly {count} title ideas for paint-by-numbers activity books (ages 5-12).

Requirements for each title:
- 4-8 words total
- Must include ""Paint by Numbers"" somewhere in the title
- Must be marketable, specific, and appealing
- Avoid generic marketing words (e.g., Amazing, Ultimate, Best, Fun)
- Cover a variety of themes (animals, nature, vehicles, fantasy, food, etc.)
- Suitable for a children's paint-by-numbers activity book audience

Return ONLY a valid JSON array of exactly {count} strings. No markdown, no explanations, no extra text.

Example format:
[""Safari Animals Paint by Numbers"",""Underwater World Paint by Numbers"",""Fairytale Castle Paint by Numbers""]";
        }
    }
}
