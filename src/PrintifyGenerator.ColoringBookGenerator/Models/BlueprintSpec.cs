namespace PrintifyGenerator.ColoringBookGenerator.Models
{
    public class BlueprintSpec
    {
        public int BlueprintId { get; init; }
        public int PrintProviderId { get; init; }
        public List<int> VariantIds { get; init; } = new();
        public int DefaultVariantId { get; init; }

        public int PageWidth { get; init; }
        public int PageHeight { get; init; }
        public int CoverWidth { get; init; }
        public int CoverHeight { get; init; }
        public int PageCount { get; init; }

        public string PageAspectRatio { get; init; } = "3:4";
        public string CoverAspectRatio { get; init; } = "3:4";
        public string CoverSpreadAspectRatio { get; init; } = "3:2";
        public string SpreadAspectRatio { get; init; } = "16:9";

        public string PageLabel { get; init; } = "page";
        public string CoverLabel { get; init; } = "cover";

        public double SpineGapPercent { get; init; }

        public bool IsCoverSpread => CoverWidth > PageWidth;
    }
}
