using PrintifyGenerator.ColoringBookGenerator.Models;

namespace PrintifyGenerator.ColoringBookGenerator.Services.PromptProviders
{
    public interface IPromptProvider
    {
        string BookType { get; }
        string BaseStyleTerms { get; }
        BlueprintSpec Blueprint { get; }
        string BuildFrontCoverPrompt(string title, string theme, string styleAddon);
        string BuildBackCoverPrompt(string theme, string styleAddon);
        string BuildPagePrompt(int pageNumber, string subject, string styleAddon);
        string BuildPageSubjectPrompt(string theme, int pageNumber);
        string[] BuildPageSubjectsFallback(string theme);
        string BuildThemeAndStylePrompt(string title);
        string BuildDescription(string theme);
        string[] BuildTags(string theme);
        string? BuildFullStoryPrompt(string theme);
        string BuildTitleGenerationPrompt(int count);
    }
}
