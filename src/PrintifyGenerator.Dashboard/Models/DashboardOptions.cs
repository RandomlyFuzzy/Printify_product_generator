using Microsoft.Extensions.Configuration;

namespace PrintifyGenerator.Dashboard.Models;

public sealed class DashboardOptions
{
    public const string SectionName = "Dashboard";

    public string DataRoot { get; set; } = "../data";
    public int GalleryItemLimit { get; set; } = 96;

    public static string ResolveDataRoot(string? configuredPath, string contentRootPath)
    {
        var relativeOrAbsolute = string.IsNullOrWhiteSpace(configuredPath)
            ? "../data"
            : configuredPath.Trim();

        return Path.GetFullPath(
            Path.IsPathRooted(relativeOrAbsolute)
                ? relativeOrAbsolute
                : Path.Combine(contentRootPath, relativeOrAbsolute));
    }
}