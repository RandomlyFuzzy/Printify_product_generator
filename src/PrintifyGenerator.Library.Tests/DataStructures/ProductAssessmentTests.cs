namespace PrintifyGenerator.Library.Tests.DataStructures;

public class ProductAssessmentTests
{
    [Fact]
    public void PromptExample_ReturnsExpectedValues()
    {
        var example = ProductAssessment.PromptExample();

        Assert.False(example.FitForPrintify);
        Assert.False(example.shouldContinue);
        Assert.NotEmpty(example.Issues);
    }
}
