
public record ProductAssessment
{
    public bool FitForPrintify { get; init; }
    public string[] Issues { get; init; }
    public bool shouldContinue { get; init; }


    public static ProductAssessment PromptExample()
    {
        return new ProductAssessment
        {
            FitForPrintify = false,
            Issues = "Unappealing product, unknown Product, Pornographic, Contains text, Deformed".Split(", "),
            shouldContinue = false
        };
    }
}