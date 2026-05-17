using System.Threading.Tasks;

namespace PrintifyGenerator.ColoringBookGenerator.Services
{
    /// <summary>
    /// Generates the images that make up a coloring book:
    ///   - front cover  (right side of the spread)
    ///   - back cover   (left side of the spread)
    ///   - interior pages 1-24
    /// </summary>
    public interface IImageGenerator
    {
        /// <summary>Generates the colored front (right-side) cover image.</summary>
        Task<string> GenerateFrontCoverAsync(string outputDirectory, string title, string theme, string styleAddon);

        /// <summary>Generates the colored back (left-side) cover image.</summary>
        Task<string> GenerateBackCoverAsync(string outputDirectory, string theme, string styleAddon);

        /// <summary>Generates a single black-and-white interior coloring page.</summary>
        Task<string> GeneratePageAsync(string outputDirectory, int pageNumber, string theme, string styleAddon);
    }
}
