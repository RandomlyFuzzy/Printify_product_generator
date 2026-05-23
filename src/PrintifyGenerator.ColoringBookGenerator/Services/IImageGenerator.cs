using System.Threading.Tasks;
using PrintifyGenerator.ColoringBookGenerator.Models;

namespace PrintifyGenerator.ColoringBookGenerator.Services
{
    public interface IImageGenerator
    {
        Task<string> GenerateFrontCoverAsync(string outputDirectory, string title, string theme, string styleAddon, string? promptPrefix = null);

        Task<string> GenerateBackCoverAsync(string outputDirectory, string theme, string styleAddon, string? promptPrefix = null);

        Task<string> GeneratePageAsync(string outputDirectory, int pageNumber, string theme, string styleAddon, string? promptPrefix = null);

        Task<string> GenerateImageFromJobAsync(string outputDirectory, GenerationJob job, string? promptPrefix = null);
    }
}
