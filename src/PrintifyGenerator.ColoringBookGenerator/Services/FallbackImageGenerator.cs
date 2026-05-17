using System;
using System.Threading.Tasks;

namespace PrintifyGenerator.ColoringBookGenerator.Services
{
    /// <summary>
    /// Wrapper that routes requests to a primary generator and falls back to a secondary
    /// generator when the primary is unavailable or fails (e.g., FreeGen rate-limit).
    /// </summary>
    public class FallbackImageGenerator : IImageGenerator
    {
        private readonly IImageGenerator _primary;
        private readonly IImageGenerator _fallback;

        public FallbackImageGenerator(IImageGenerator primary, IImageGenerator fallback)
        {
            _primary = primary ?? throw new ArgumentNullException(nameof(primary));
            _fallback = fallback ?? throw new ArgumentNullException(nameof(fallback));
        }

        public async Task<string> GenerateFrontCoverAsync(string outputDirectory, string title, string theme, string styleAddon)
        {
            if (_primary is FreeGenGenerator && DateTime.UtcNow < FreeGenGenerator.FreeGenUnavailableUntilUtc)
            {
                Console.WriteLine($"[Generator] Primary unavailable — using fallback: {_fallback.GetType().Name}");
                return await _fallback.GenerateFrontCoverAsync(outputDirectory, title, theme, styleAddon);
            }

            try
            {
                Console.WriteLine($"[Generator] Attempting primary generator: {_primary.GetType().Name} for front cover");
                var res = await _primary.GenerateFrontCoverAsync(outputDirectory, title, theme, styleAddon);
                if (!string.IsNullOrWhiteSpace(res))
                {
                    Console.WriteLine($"[Generator] Primary generator succeeded: {_primary.GetType().Name}");
                    return res;
                }
                else
                {
                    Console.WriteLine($"[Generator] Primary generator returned empty result: {_primary.GetType().Name} — falling back to {_fallback.GetType().Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Generator] Primary generator {_primary.GetType().Name} failed: {ex.Message}. Falling back to {_fallback.GetType().Name}.");
            }

            Console.WriteLine($"[Generator] Using fallback generator: {_fallback.GetType().Name} for front cover");
            return await _fallback.GenerateFrontCoverAsync(outputDirectory, title, theme, styleAddon);
        }

        public async Task<string> GenerateBackCoverAsync(string outputDirectory, string theme, string styleAddon)
        {
            if (_primary is FreeGenGenerator && DateTime.UtcNow < FreeGenGenerator.FreeGenUnavailableUntilUtc)
            {
                Console.WriteLine($"[Generator] Primary unavailable — using fallback: {_fallback.GetType().Name}");
                return await _fallback.GenerateBackCoverAsync(outputDirectory, theme, styleAddon);
            }

            try
            {
                Console.WriteLine($"[Generator] Attempting primary generator: {_primary.GetType().Name} for back cover");
                var res = await _primary.GenerateBackCoverAsync(outputDirectory, theme, styleAddon);
                if (!string.IsNullOrWhiteSpace(res))
                {
                    Console.WriteLine($"[Generator] Primary generator succeeded: {_primary.GetType().Name}");
                    return res;
                }
                else
                {
                    Console.WriteLine($"[Generator] Primary generator returned empty result: {_primary.GetType().Name} — falling back to {_fallback.GetType().Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Generator] Primary generator {_primary.GetType().Name} failed: {ex.Message}. Falling back to {_fallback.GetType().Name}.");
            }

            Console.WriteLine($"[Generator] Using fallback generator: {_fallback.GetType().Name} for back cover");
            return await _fallback.GenerateBackCoverAsync(outputDirectory, theme, styleAddon);
        }

        public async Task<string> GeneratePageAsync(string outputDirectory, int pageNumber, string theme, string styleAddon)
        {
            if (_primary is FreeGenGenerator && DateTime.UtcNow < FreeGenGenerator.FreeGenUnavailableUntilUtc)
            {
                Console.WriteLine($"[Generator] Primary unavailable — using fallback: {_fallback.GetType().Name} for page {pageNumber}");
                return await _fallback.GeneratePageAsync(outputDirectory, pageNumber, theme, styleAddon);
            }

            try
            {
                Console.WriteLine($"[Generator] Attempting primary generator: {_primary.GetType().Name} for page {pageNumber}");
                var res = await _primary.GeneratePageAsync(outputDirectory, pageNumber, theme, styleAddon);
                if (!string.IsNullOrWhiteSpace(res))
                {
                    Console.WriteLine($"[Generator] Primary generator succeeded: {_primary.GetType().Name} for page {pageNumber}");
                    return res;
                }
                else
                {
                    Console.WriteLine($"[Generator] Primary generator returned empty for page {pageNumber}: {_primary.GetType().Name} — falling back to {_fallback.GetType().Name}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Generator] Primary generator {_primary.GetType().Name} failed for page {pageNumber}: {ex.Message}. Falling back to {_fallback.GetType().Name}.");
            }

            Console.WriteLine($"[Generator] Using fallback generator: {_fallback.GetType().Name} for page {pageNumber}");
            return await _fallback.GeneratePageAsync(outputDirectory, pageNumber, theme, styleAddon);
        }
    }
}
