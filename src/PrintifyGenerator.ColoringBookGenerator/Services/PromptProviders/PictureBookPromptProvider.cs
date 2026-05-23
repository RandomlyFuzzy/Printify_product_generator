using PrintifyGenerator.ColoringBookGenerator.Models;

namespace PrintifyGenerator.ColoringBookGenerator.Services.PromptProviders
{
    public class PictureBookPromptProvider : IPromptProvider
    {
        public string BookType => "Picture Book";
        public string BaseStyleTerms => "thick black outlines, white fill, no shading, no color";
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
        Create a high-quality black and white coloring book FRONT COVER illustration.

        Title:
        {title}

        Theme:
        {theme}

        CRITICAL TITLE REQUIREMENT:
        - The title MUST be clearly visible and perfectly legible
        - The title must be well-formed, correctly spelled, and centered
        - Use a decorative but readable lettering style suitable for coloring books
        - The title should be integrated into the cover design (not floating randomly)
        - Ensure strong contrast so the text is readable in black and white line art

        Style Requirements:
        - Pure black and white line art only
        - No grayscale, shading, or color
        - Clean bold outlines suitable for coloring
        - Highly detailed but printable
        - White background
        - Professional coloring book cover illustration style
        - Kid-friendly and visually engaging

        Additional Style:
        {styleAddon}

        Composition:
        - Prominent central focal illustration related to the theme
        - Title positioned prominently (top or center depending on composition balance)
        - Decorative framing elements around the title
        - Balanced, symmetrical, polished cover layout
        - Leave clear separation between title and illustration elements
        - Avoid clutter that reduces title readability
        - No extra text besides the title

        Output Style:
        Intricate ink illustration, vector-style line art, crisp monochrome outlines, professional coloring book cover design.
        ";
        }

        public string BuildBackCoverPrompt(string theme, string styleAddon)
        {
            return $@"
            Create a high-quality black and white coloring book illustration for the BACK COVER of a coloring book.

            Theme:
            {theme}

            Style Requirements:
            - Pure black and white line art only
            - No grayscale, shading, or color
            - Clean bold outlines suitable for coloring
            - Highly detailed but printable
            - White background
            - Coloring book page aesthetic
            - Kid-friendly and visually engaging
            - Balanced composition with decorative borders and background elements
            - Include whimsical and intricate patterns
            - Professional coloring book illustration style

            Additional Style:
            {styleAddon}

            Composition:
            - Full-page vertical layout
            - Leave some open spaces for coloring
            - Center-focused design
            - Symmetrical and visually appealing
            - Avoid text, logos, watermarks, or barcode/price labels
            - Leave clean space at the bottom center for barcode placement

            Output Style:
            Intricate ink illustration, vector-style line art, coloring book page, crisp outlines, monochrome drawing.
        ";
        }

        public string BuildPagePrompt(int pageNumber, string subject, string styleAddon)
        {
            return $@"
        Create a high-quality black and white coloring book illustration for PAGE {pageNumber} of a coloring book.

        Theme:
        {subject}

        Style Requirements:
        - Pure black and white line art only
        - No grayscale, shading, or color
        - Clean bold outlines suitable for coloring
        - Highly detailed but printable
        - White background
        - Coloring book page aesthetic
        - Kid-friendly and visually engaging
        - Intricate patterns and decorative elements
        - Professional coloring book illustration style

        Additional Style:
        {styleAddon}

        Composition:
        - Full-page vertical layout
        - Unique scene composition for this page
        - Center-focused subject with supporting background details
        - Balanced use of open spaces for coloring
        - Include depth and layered elements
        - Avoid text, logos, watermarks, page numbers, or barcode/price labels inside the image

        Page Design Guidance:
        - Make this page visually distinct from other pages
        - Add thematic objects, scenery, and ornamental details related to the theme
        - Ensure the illustration feels immersive and creative
        - Maintain consistent art style across all pages

        Output Style:
        Intricate ink illustration, vector-style line art, crisp monochrome outlines, detailed coloring book page.
        ";
        }

        public string BuildPageSubjectPrompt(string theme, int pageNumber)
        {
            return $@"You are helping create a children's picture book with the theme ""{theme}"".
Generate a detailed scene description and a short overlay caption for page {pageNumber}.

Important output rules:

If this is a single page (first or last page), return this JSON shape:
    {{""scene"":""..."",""overlay"":""...""}}
             {{""scene"":""..."",""overlay"":""..."",""page_text"":""...""}}

If this is an internal paired spread (two pages to be split), return this JSON shape:
    {{""scene"":""..."",""overlay_left"":""..."",""overlay_right"":""...""}}
             {{""scene"":""..."",""overlay_left"":""..."",""overlay_right"":""..."" ,""page_text_left"":""..."",""page_text_right"":""...""}}

Field guidance:
Field guidance:
- ""scene"": A rich, actionable visual description suitable for an image generator. Include characters, actions, foreground, background, props, mood, lighting, and composition notes. For single pages write for a 3:4 page; for spreads write for a 16:9 spread. Keep it clear and focused (up to ~200 words).
- ""overlay"", ""overlay_left"", ""overlay_right"": Short child-friendly captions (3–12 words) to be placed as overlays later. No punctuation at the end, no quotes.
- ""page_text"", ""page_text_left"", ""page_text_right"": Short story text or caption for the page (1–3 short sentences). These will be placed externally beneath the image; do NOT include any text within the artwork.

Examples (return literal JSON only):
Examples (return literal JSON only):
    {{""scene"":""A small fox peeks out from behind a mossy log in a sun-dappled clearing, surrounded by mushrooms and fireflies, centered composition with a clear foreground and background"",""overlay"":""Little fox explores"",""page_text"":""Little fox explores the meadow and finds a glowing mushroom""}}
    {{""scene"":""A wide forest clearing with a fox family having breakfast on the left and a river on the right; rolling hills in the distance, lots of whimsical detail for line-art"",""overlay_left"":""Fox family breakfast"",""overlay_right"":""River adventure begins"",""page_text_left"":""The fox family shares a cozy breakfast under the big oak"",""page_text_right"":""Meanwhile the river nearby murmurs with hidden adventures""}}
";
        }

        public string[] BuildPageSubjectsFallback(string theme)
        {
            var t = theme.ToLowerInvariant();
            return new[]
            {
                $"a cute baby animal from the {t} theme playing outdoors",
                $"a happy family scene in a {t} setting",
                $"a magical garden filled with {t}-inspired plants and flowers",
                $"an adventurous child exploring a {t} landscape",
                $"a cozy house or home decorated with {t} elements",
                $"a friendly creature from the {t} world waving hello",
                $"a festive celebration or party with {t} decorations",
                $"a busy market or village in a {t} world",
                $"a playful scene at the beach or outdoors with {t} characters",
                $"a magical flying vehicle or transport in a {t} sky",
                $"a whimsical forest with {t} animals hiding among the trees",
                $"a fun food scene featuring {t}-themed treats and snacks",
                $"a rainy day with {t} characters jumping in puddles under umbrellas",
                $"a starry night sky with {t}-themed constellation art",
                $"a child reading a book surrounded by {t} characters",
                $"a silly robot or machine built from {t} objects",
                $"a treasure hunt map leading through a {t} adventure",
                $"a farm or garden with {t} animals doing chores",
                $"an underwater scene with {t}-inspired sea creatures",
                $"a snow day with {t} characters building a snowman",
                $"a superhero version of a {t} character saving the day",
                $"a sports day with {t} characters playing their favourite game",
                $"a music concert with {t} animals playing instruments",
                $"a bedtime scene with {t} characters saying goodnight under the moon",
            };
        }

        public string BuildThemeAndStylePrompt(string title)
        {
            return $@"You are a product copywriter specialising in children's picture books (ages 3-10).

Task: Given a single BOOK TITLE, produce a concise 'theme' and a 'style' string suitable for generating interior picture book illustrations.

Rules for the 'theme' field:
- 10-20 words, lower-case, no punctuation suitable for a child to understand.
- Describe the interior subject (e.g. 'jungle animals', 'space robots', 'cozy bakery').
- Do NOT include the words 'coloring', 'colouring', or 'book'.

Rules for the 'style' field:
- A comma-separated list of image-generation keywords for ONE illustration.
- MUST include the base terms: ""{BaseStyleTerms}"".
- Add 3-5 short descriptors (subject matter, mood, composition). Keep phrases short.
- Do NOT mention paper, page layout, borders, or the words 'picture book'.

BOOK TITLE:
""{title}""

OUTPUT FORMAT:
- Return ONLY one valid JSON object with exactly two fields: ""theme"" and ""style"".
- No markdown, no explanation, no extra text. Start with {{ and end with }}.

Examples:
{{""theme"":""jungle animals"",""style"":""{BaseStyleTerms}, cute jungle animals, playful composition, centered subject""}}
{{""theme"":""cozy witch bakery"",""style"":""{BaseStyleTerms}, cozy witch baking, whimsical kitchen, detailed props""}}";
        }

        public string? BuildFullStoryPrompt(string theme) => null;

        public string BuildTitleGenerationPrompt(int count)
        {
            return $@"You are a product copywriter for a print-on-demand coloring book store.

Generate exactly {count} title ideas for children's picture books (ages 3-10).

Requirements for each title:
- 4-8 words total
- Must end with exactly ""Coloring Book"" or ""Colouring Book""
- Must be marketable, specific, and appealing
- Avoid generic marketing words (e.g., Amazing, Ultimate, Best, Fun)
- Cover a variety of themes (animals, fantasy, nature, vehicles, food, etc.)
- Suitable for a children's coloring book audience

Return ONLY a valid JSON array of exactly {count} strings. No markdown, no explanations, no extra text.

Example format:
[""Jungle Safari Adventure Coloring Book"",""Magical Fairy Garden Colouring Book"",""Cute Kawaii Animals Coloring Book""]";
        }

        public string BuildDescription(string theme)
            => $"A beautiful children's picture book with a {theme} theme. " +
               "Includes a full-color cover and 24 black-and-white pages. " +
               "Perfect for kids aged 3+. Printed on high-quality 8.5\" x 11\" paper.";

        public string[] BuildTags(string theme)
            => new[]
            {
                "picture book", "kids", "children", "activity book",
                theme.ToLowerInvariant(), "coloring pages", "printify"
            };
    }
}
