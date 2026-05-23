using PrintifyGenerator.ColoringBookGenerator.Models;

namespace PrintifyGenerator.ColoringBookGenerator.Services.PromptProviders
{
    public class StoryBookPromptProvider : IPromptProvider
    {
        public string BookType => "Story Book";
        public string BaseStyleTerms => "warm colors, soft watercolor textures, gentle shading, storybook illustration";
        public BlueprintSpec Blueprint => new()
        {
            BlueprintId = 2733,
            PrintProviderId = 28,
            VariantIds = new() { 149010, 149011 },
            DefaultVariantId = 149010,
            PageWidth = 2400,
            PageHeight = 2400,
            CoverWidth = 4790,
            CoverHeight = 2400,
            PageCount = 20,
            PageAspectRatio = "1:1",
            CoverAspectRatio = "1:1",
            CoverSpreadAspectRatio = "2:1",
            SpreadAspectRatio = "2:1",
            SpineGapPercent = 1.5
        };

        public string BuildFrontCoverPrompt(string title, string theme, string styleAddon)
        {
            return $@"
        Create a beautiful full-color storybook FRONT COVER illustration.

        Title:
        {title}

        Theme:
        {theme}

        IMPORTANT:
        - Do NOT draw any text, title, or letters in the image
        - Do NOT draw any barcode, barcode-like marks (series of vertical bars), price tags, or sticker labels
        - Barcode and any price/label text will be added programmatically later
        - Leave clean space at the top center for title placement
        - Leave clean space at the bottom for subtitle/author text

        Style Requirements:
        - Full-color illustration with rich, warm tones
        - Soft watercolor or painted storybook art style
        - Gentle shading and depth
        - Charming, whimsical, and emotionally engaging
        - Kid-friendly characters and scenes
        - Professional storybook cover illustration style
        - White or softly colored background

        Additional Style:
        {styleAddon}

        Composition:
        - Central focal illustration showing main characters in a narrative scene
        - Leave top 30% of the image as clean space for title overlay
        - Leave bottom 10% as clean space for author/subtitle overlay
        - Decorative framing or border elements around the edges
        - Balanced composition that feels like a classic storybook
        - No text, no letters, no title in the image itself

        Output Style:
        Watercolor storybook illustration, warm palette, charming characters, narrative scene, professional children's book cover design.
        ";
        }

        public string BuildBackCoverPrompt(string theme, string styleAddon)
        {
            return $@"
            Create a full-color storybook BACK COVER illustration.

            Theme:
            {theme}

            IMPORTANT:
            - Do NOT draw any barcode, barcode-like marks (series of vertical bars), price tags, or any text/letters in the image
            - Barcode and any price/label text will be added programmatically later
            - Leave clean space at the bottom center for barcode placement
            - Leave clean space at the center for story summary/blurb overlay

            Style Requirements:
            - Full-color illustration, warm and inviting tones
            - Soft painted or watercolor style
            - Gentle shading and depth
            - Storybook art aesthetic
            - Kid-friendly and emotionally warm
            - Professional illustration style

            Additional Style:
            {styleAddon}

            Composition:
            - Full-page vertical layout
            - A concluding or teaser scene from the story
            - Leave bottom 15% as clean space for barcode overlay
            - Leave center area with soft gradient or plain space for text overlay
            - No text, no barcode, no letters inside the image itself
            - Center-focused illustration with decorative border

            Output Style:
            Warm watercolor storybook art, soft edges, cohesive color palette, children's book back cover style.
        ";
        }

        public string BuildPagePrompt(int pageNumber, string subject, string styleAddon)
        {
            return $@"
        Create a full-color storybook illustration for PAGE {pageNumber} of a children's story book.

        Scene:
        {subject}

        IMPORTANT:
        - Do NOT draw any text, page numbers, letters, barcode, or barcode-like marks in the image
        - Story text, page numbers, and barcode will be added programmatically later
        - Leave clean space at the bottom of the image for story text overlay

        Style Requirements:
        - Full-color illustration with warm, rich tones
        - Soft watercolor or painted storybook art style
        - Gentle shading and depth
        - Whimsical, charming, and emotionally engaging
        - Kid-friendly characters and environments
        - Professional storybook illustration quality

        Additional Style:
        {styleAddon}

        Composition:
        - Landscape full-page layout
        - Narrative scene that tells part of the story
        - Main subject or characters prominently featured
        - Supporting background details that enhance the scene
        - Leave bottom 15% of the image as clean space for story text overlay
        - No text, no page numbers, no logos, no watermarks inside the image

        Page Design Guidance:
        - Make this page feel like part of a continuous narrative
        - Use consistent character designs and color palette
        - Show action, emotion, or discovery
        - Create immersive storybook environments
        - Maintain consistent art style across all pages

        Output Style:
        Watercolor storybook illustration, warm palette, narrative scene, charming characters, soft painted style, children's book page.
        ";
        }

        public string BuildPageSubjectPrompt(string theme, int pageNumber)
        {
            return $@"You are writing a children's story book with the theme ""{theme}"".

For page {pageNumber}, produce a single JSON object with two fields: ""scene"" and ""page_text"".

Important rules:
- Do NOT draw any text, letters, signage, or speech bubbles inside the image. All text will be placed externally beneath the image.
- Return ONLY a single, valid JSON object (no explanation, no markdown, no extra text).

JSON shape:
    {{""scene"":""..."",""page_text"":""...""}}

Field guidance:
- ""scene"": A detailed image prompt suitable for an image generator. Include characters, actions, foreground, background, props, mood, lighting, and composition notes. For single pages prefer 3:4 framing; for spreads prefer 16:9. Keep it clear and focused (up to ~200 words).
- ""page_text"": A short child-friendly piece of story text (1–3 short sentences) to be placed beneath the image. Keep phrasing simple and continuous with the narrative.

Example (return literal JSON only):
    {{""scene"":""A brave little rabbit peeks out of a hollow log in a sunlit meadow, foreground mushrooms, distant oak tree, soft morning light, centered composition."",""page_text"":""Pip peeks into the meadow and spots a curious shimmer near the log.""}}
";
        }

        public string[] BuildPageSubjectsFallback(string theme)
        {
            var t = theme.ToLowerInvariant();
            return new[]
            {
                $"a curious child discovers a magical {t} world for the first time",
                $"the main character meets a friendly creature in the {t} setting and a journey begins through a beautiful {t} landscape",
                $"the character finds a mysterious object that unlocks {t} secrets and makes new friends in a colorful {t} village",
                $"overcoming a small challenge with help from {t} friends leads to a magical transformation",
                $"exploring a hidden cave or castle in the {t} realm reveals ancient wonders",
                $"a festive celebration with {t} characters and music brings joy to everyone",
                $"the character learns an important lesson about kindness in the {t} world while enjoying a breathtaking view",
                $"solving a puzzle or riddle with {t} clues leads to a boat ride through enchanted waters",
                $"a cozy meal shared with {t} friends under the stars leads to dancing in a sunny meadow",
                $"helping a lost creature find its way home in the {t} world deepens the bond between friends",
                $"a starry night conversation about {t} dreams leads to a daring rescue mission",
                $"exploring an enchanted {t} garden with hidden wonders after the rain brings new hope",
                $"saying goodbye with promises to return, the character carries {t} memories home forever",
            };
        }

        public string? BuildFullStoryPrompt(string theme)
        {
            return $@"You are writing a children's story book with the theme ""{theme}"".

Write a coherent story divided into exactly 13 sequential segments. Each segment is 2-4 sentences describing a scene.

Segment 1: The opening scene — introduces the main character and setting (full page, standalone)
Segments 2-12: Each covers a two-page spread that advances the narrative naturally
Segment 13: The closing scene — a satisfying conclusion (full page, standalone)

The story should flow smoothly from one segment to the next with consistent characters and a clear beginning, middle, and end.

Return ONLY a valid JSON array of exactly 13 strings. No markdown, no explanations, no extra text.
Example format:
[""A brave little rabbit named Pip wakes up in his cozy burrow under an old oak tree."", ""Pip hops through the meadow and discovers a hidden door in a mossy bank. He pushes it open and peers inside."", ...]";
        }

        public string BuildTitleGenerationPrompt(int count)
        {
            return $@"You are a product copywriter for a print-on-demand children's story book store.

Generate exactly {count} title ideas for children's story books (ages 3-8).

Requirements for each title:
- 3-7 words total
- Should sound like a story or tale (e.g., ""The..."" or ""...Adventure"")
- Must be marketable, specific, and appealing
- Avoid generic marketing words (e.g., Amazing, Ultimate, Best, Fun)
- Cover a variety of themes (animals, fantasy, nature, friendship, adventure, etc.)
- Suitable for a bedtime story book audience

Return ONLY a valid JSON array of exactly {count} strings. No markdown, no explanations, no extra text.

Example format:
[""The Brave Little Rabbit's Adventure"",""Luna the Friendly Dragon"",""Sammy Star Goes to Space""]";
        }

        public string BuildThemeAndStylePrompt(string title)
        {
            return $@"You are a product copywriter specialising in children's story books (ages 3-8).

Task: Given a single BOOK TITLE, produce a concise 'theme' and a 'style' string suitable for generating storybook illustrations.

Rules for the 'theme' field:
- 10-20 words, lower-case, no punctuation suitable for a child to understand.
- Describe the story's world and characters (e.g. 'fairy forest creatures having adventures', 'brave little rocket exploring space').
- Do NOT include the words 'story', 'book', or 'illustration'.

Rules for the 'style' field:
- A comma-separated list of image-generation keywords for ONE illustration.
- MUST include the base terms: ""{BaseStyleTerms}"".
- Add 3-5 short descriptors (subject matter, mood, color palette). Keep phrases short.
- Do NOT mention paper, page layout, borders, or the words 'story book'.

BOOK TITLE:
""{title}""

OUTPUT FORMAT:
- Return ONLY one valid JSON object with exactly two fields: ""theme"" and ""style"".
- No markdown, no explanation, no extra text. Start with {{ and end with }}.

Examples:
{{""theme"":""brave little rabbit exploring magical forest"",""style"":""{BaseStyleTerms}, cute bunny character, enchanted woods, warm golden hour light""}}
{{""theme"":""friendly dinosaur adventures in prehistoric land"",""style"":""{BaseStyleTerms}, adorable dinosaurs, lush green landscapes, playful action""}}";
        }

        public string BuildDescription(string theme)
            => $"A charming children's story book with a {theme} theme. " +
               "Features a full-color cover and 24 beautifully illustrated story pages. " +
               "Perfect for bedtime reading with kids aged 3-8. Printed on high-quality 8.5\" x 11\" paper.";

        public string[] BuildTags(string theme)
            => new[]
            {
                "story book", "children's book", "bedtime stories", "kids",
                theme.ToLowerInvariant(), "illustrated story", "printify"
            };
    }
}
