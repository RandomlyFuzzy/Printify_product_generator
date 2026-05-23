namespace PrintifyGenerator.ColoringBookGenerator.Models
{
    public class GenerationJob
    {
        public string JobId { get; init; } = Guid.NewGuid().ToString("N");
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
        public string BookType { get; init; } = "";
        public string Title { get; init; } = "";
        public string Theme { get; init; } = "";
        public string StyleAddon { get; init; } = "";

        public int? PageNumber { get; init; }
        public string PageLabel { get; init; } = "";
        public bool IsCover { get; init; }
        public bool IsFrontCover { get; init; }
        public bool IsBackCover { get; init; }
        public string AspectRatio { get; init; } = "3:4";

        public string Prompt { get; init; } = "";
        public string Subject { get; init; } = "";
        public string? StoryText { get; init; }
        public string? TitleOverlayText { get; init; }
        public string? FooterText { get; init; }

        public string OutputDirectory { get; set; } = "";
        public string OutputFileName { get; set; } = "";
        public string? OutputPath { get; set; }

        public bool ConvertToBlackAndWhite { get; init; }
        public bool Antialias { get; init; } = true;
        public bool AddPageNumberOverlay { get; init; } = true;
        public string? FinisherFontName { get; set; }
        public string? FinisherFontFamily { get; set; }

        public Dictionary<string, string> Metadata { get; init; } = new();
    }
}
