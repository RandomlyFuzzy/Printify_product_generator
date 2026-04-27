using System;
using System.IO;
using System.Linq;

var repositoryRoot = ResolveRepositoryRoot();
if (repositoryRoot is null)
{
    Console.Error.WriteLine("Could not locate the repository root from the current working directory.");
    return 1;
}


var envFilePath = Path.Combine(repositoryRoot, "main.env");
var token = ReadToken(envFilePath);
if (string.IsNullOrWhiteSpace(token))
{
    Console.Error.WriteLine(
        "TOKEN was not found in main.env. Live Printify pricing is required because cached blueprint_details files do not contain comparable variant pricing.");
    return 1;
}











return 0;
static string? ResolveRepositoryRoot()
{
    var probeRoots = new[]
    {
        Directory.GetCurrentDirectory(),
        AppContext.BaseDirectory
    }
    .Where(path => !string.IsNullOrWhiteSpace(path))
    .Distinct(StringComparer.OrdinalIgnoreCase);

    foreach (var probeRoot in probeRoots)
    {
        var current = new DirectoryInfo(Path.GetFullPath(probeRoot));

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "PrintifyGenerator.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }
    }

    return null;
}


static string ReadToken(string envFilePath)
{
    if (!File.Exists(envFilePath))
    {
        return string.Empty;
    }

    foreach (var line in File.ReadLines(envFilePath))
    {
        if (line.StartsWith("TOKEN=", StringComparison.OrdinalIgnoreCase))
        {
            return line["TOKEN=".Length..].Trim();
        }
    }

    return string.Empty;
}
