namespace PrintifyGenerator.AnalyticsApi.Models;

public sealed class AnalyticsApiOptions
{
    public string DataRoot { get; set; } = "../data";
    public string CategoryFeaturesPath { get; set; } = "../../../category_features";
    public string IntakeRoot { get; set; } = "../data/api-intake";
}
