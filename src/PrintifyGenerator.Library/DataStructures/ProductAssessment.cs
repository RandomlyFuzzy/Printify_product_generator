
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
            Issues = "Unapealing product, unknown Product, Pornographic, Contains text, Defformed".Split(", "),
            shouldContinue = false
        };
    }
}