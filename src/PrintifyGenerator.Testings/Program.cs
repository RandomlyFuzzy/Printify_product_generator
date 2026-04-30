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
PrintifyClient client = new PrintifyClient(token);

//read every phase4 and phase7 text file in ../data/Checking/**/**/*.txt and print the file name and the token
var dataDirectory = Path.Combine(repositoryRoot,"src", "data", "Checking");
if (!Directory.Exists(dataDirectory))
{
    Console.Error.WriteLine($"Data directory not found at {dataDirectory}");
    return 1;
}
List<string> ph4 = Directory.GetFiles(dataDirectory, "phase4.txt", SearchOption.AllDirectories).ToList();
List<string> ph7 = Directory.GetFiles(dataDirectory, "phase7.txt", SearchOption.AllDirectories).ToList();
// foreach (var year in Directory.GetDirectories(dataDirectory))
// {

    
// }

HashSet<string> PIDs = new HashSet<string>();

foreach (var file in ph4)
{
    File.ReadLines(file)
        .Select(line => line.Split(',').FirstOrDefault()?.Trim())
        .Where(pid => !string.IsNullOrWhiteSpace(pid))
        .ToList()
        .ForEach(pid => PIDs.Add(pid!));
}
foreach (var file in ph7)
{
    File.ReadLines(file)
        .Select(line => line.Split(',').LastOrDefault()?.Trim())
        .Where(pid => !string.IsNullOrWhiteSpace(pid))
        .ToList()
        .ForEach(pid => PIDs.Add(pid!));
}

Console.WriteLine($"Found {PIDs.Count} unique PIDs across phase4 and phase7 files.");

string[] StoreNames = new[] { "Staging","Ebay","My Etsy Store" };

int[] Storeids = new int[StoreNames.Length];

foreach (var store in client.GetShopsAsync().Result)
{
    var index = Array.IndexOf(StoreNames, store.Title);
    if (index >= 0)
    {
        Storeids[index] = store.Id;
        Console.WriteLine($"Mapped Store '{store.Title}' to ID {Storeids[index]}");
    }
}

foreach (var store in Storeids)
{
    var products = await client.GetAllProductsAsync(store);
    int i = 1 ;
    foreach (var product in products)
    {
        if(!PIDs.Contains(product.Id.ToString()))
        {
            if(product.External == null)
            {
                await client.DeleteProductAsync(store, product.Id);
                Console.WriteLine($"Deleted product with PID: {product.Id} from store {store} because it was not found in the phase4 or phase7 files and was not published.");
            }
        }
        LogProgress(i++, products.Count);
    }
}


static void LogProgress(int current, int total)
{
    string progressBar = new string('#', (int)((current / (double)total) * 50)).PadRight(50);
    Console.WriteLine($"\r[{progressBar}] {current}/{total} products");
    Console.CursorTop --;
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
