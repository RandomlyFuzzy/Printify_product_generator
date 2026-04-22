using System.Text.Json;



class Program
{
    static HttpClient httpClient = new();
    static string CurrentDebugPlace = string.Empty;
    static async Task<int> Main(string[] args)
    {
        var repositoryRoot = ResolveRepositoryRoot();

        if (repositoryRoot is null)
        {
            Console.Error.WriteLine("[ERROR] Could not locate the repository root.");
            return 1;
        }

        Directory.SetCurrentDirectory(repositoryRoot);

        ProductMetadataUpdaterSettings settings;
        try
        {
            settings = ProductMetadataUpdaterSettings.Load(repositoryRoot, args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ERROR] {ex.Message}");
            return 1;
        }
        var client = new PrintifyClient(settings.Token);
        var ollamaClient = new OllamaClient("http://192.168.0.180:11434");

        Shop stagingShop;
        try
        {
            var shops = await client.GetShopsAsync();
            stagingShop = settings.ResolveStagingShop(shops);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ERROR] Failed to resolve the staging/publishing Printify shops: {ex.Message} at {CurrentDebugPlace}");
            return 1;
        }

        Console.WriteLine($"Staging shop: {stagingShop.Id} ({stagingShop.Title}).");

        string completedProductsFilePath = Path.Combine(repositoryRoot,"src","data", "Cached", "completed_products.txt");
        var completedProductIds = GetCompletedProductIds(completedProductsFilePath);
        var Products = await client.GetAllProductsAsync(stagingShop.Id);
        Console.WriteLine($"Products in staging shop: {Products.Count()}");
        Products = Products.Where(p => !completedProductIds.Contains(p.Id)).ToList();
        Console.WriteLine($"Products to process (excluding already completed): {Products.Count()}");
        int index =0;
        foreach (var product in Products)
        {
            //1.download all the images for the product and save them to a local folder
            //download images to a temp folder 
            //2. generate metadata for the product using the ollama client and the downloaded images
            //3. update the product metadata in printify using the client
            //4. cleanup the temp images
            var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            bool completed = false;
            while(!completed){
                try
                {        
                    Directory.CreateDirectory(tempFolder);
                    var imagePaths = await DownloadProductImagesAsync(client, product, tempFolder);
                    var metadata = await GenerateMetadataAsync(ollamaClient, imagePaths);
                    await UpdateProductMetadataAsync(client, product, metadata);
                    Console.WriteLine(ProgressBar(++index, Products.Count(), $"Processed product {product.Id}"));
                    Console.CursorTop -= 1; // Move the cursor up one line to overwrite the progress bar in the next iteration.
                    AddCompletedProductId(completedProductsFilePath, product.Id);
                }catch (Exception ex)
                {
                    Console.Error.WriteLine($"[ERROR] Failed to process product {product.Id}: {ex.Message} at {CurrentDebugPlace}");
                }
                finally
                {
                    Directory.Delete(tempFolder, true);
                }
                completed = true;
                await Task.Delay(1000); // Add a small delay between products to avoid overwhelming the API or the model, and to make it easier to read the console output.
            }
        }
        return 0;
    }

    static string[] GetCompletedProductIds(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return Array.Empty<string>();
        }

        return File.ReadAllLines(filePath)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
    }
    static void AddCompletedProductId(string filePath, string productId)
    {
        File.AppendAllLines(filePath, new[] { productId });
    }

    static string ProgressBar(int current, int total,string suffix = "", int barLength = 30)
    {
        if (total == 0) total = 1; // Avoid division by zero
        double progress = (double)current / total;
        int filledLength = (int)(barLength * progress);
        string bar = new string('█', filledLength) + new string('-', barLength - filledLength);
        return $"|{bar}| {current}/{total} ({progress:P1}) {suffix}";
    }

    static async Task<IEnumerable<string>> DownloadProductImagesAsync(PrintifyClient client, Product product, string tempFolder)
    {
        var imagePaths = new List<string>();
        int id = 0;
        // Console.WriteLine($"Downloading images for product {product.Id} total images: {product.Images.Count()}");
        
        for(int i = 0; i < Math.Min(12, product.Images.Count()); i++)
        {
            var image = product.Images[i];
            var imageData = await httpClient.GetByteArrayAsync(image.Src);
            var extension = Path.GetExtension(image.Src).Split('?')[0]; // Handle URLs with query parameters
            var imagePath = Path.Combine(tempFolder, $"{id++}{extension}");
            await File.WriteAllBytesAsync(imagePath, imageData);
            imagePaths.Add(imagePath);
        }
        return imagePaths;
    }

    static async Task<ProductMetadata> GenerateMetadataAsync(OllamaClient ollamaClient, IEnumerable<string> imagePaths)
    {
        // For simplicity, we'll just send the image paths to the model and get back some dummy metadata.
        // In a real implementation, you'd likely want to send the actual image data or use a more complex prompt.
        var prompt = $@"Generate metadata for a product as if you are an e-commerce product listing generator.
            you should provide as much detail as possible with as many keywords as possible based on the images provided.
            As well as extra possible uses never use copywriten content like names of brands or celebrities but you can use 
            general terms like 'inspired by celebrity styles' or 'similar to popular brand aesthetics' if the images 
            suggest that style. Make sure the Title uses as much of the 80 character limit as possible and is very 
            descriptive. The description should be detailed and include potential use cases for the product. 
            The tags should be relevant keywords that potential customers might search for when looking for a 
            product like this. The title should include what the product is, and the main use case. 
            The description should expand on the title and include more details about the product, 
            its features, and potential use cases. The tags should be a mix of specific and broad keywords 
            that are relevant to the product. The Description should be formated like a html string
            this includes using <br>, <strong>, <em>,<u> for line breaks and <ul><li> for lists if needed.

            Provide the metadata in JSON format with the following structure:
            {{
                ""title"": ""Generated Product Title"",
                ""description"": ""Generated product description based on the images."",
                ""tags"": [""tag1"", ""tag2"", ""tag3""]
            }}
        ";
        CurrentDebugPlace = "Before calling GenerateWithImageAsync";
        string response = "";
        await foreach(var data in ollamaClient.GenerateWithImagesStreamAsync("gemma4:e4b", prompt, imagePaths.ToArray()))
        {
            response += data;
            // Console.Write(data);
        }
        CurrentDebugPlace = "After calling GenerateWithImageAsync";

        response = response.Trim();
        response = response.Substring(response.IndexOf('{')); // Ensure we start parsing from the first '{' character in case there is any extra text before the JSON.
        response = response.Substring(0, response.LastIndexOf('}') + 1); // Ensure we end parsing at the last '}' character in case there is any extra text after the JSON.

        return JsonSerializer.Deserialize<ProductMetadata>(response) ?? throw new Exception("Failed to parse metadata from model response.");
    }

    static async Task UpdateProductMetadataAsync(PrintifyClient client, Product product, ProductMetadata metadata)
    {
        // Parse the metadata JSON and update the product in Printify.
        // This is a simplified example; in a real implementation, you'd want to handle errors and edge cases.
        if (metadata is null)
        {
            Console.Error.WriteLine($"[ERROR] Failed to parse metadata JSON. Skipping product {product.Id}.");
            return;
        }

        var prices = product.Variants.Select(v =>new CreateProductVariant
        {
            Id = v.Id,
            Price = GetRoundedPrice(v.Price),
            IsEnabled = v.IsEnabled
        }).ToList();

        var updatedProduct = new UpdateProductRequest
        {
            Title = metadata.title,
            Description = metadata.description,
            Tags = metadata.tags.ToList(),
            Variants = prices
            // Note: In a real implementation, you'd likely want to preserve other product properties and only update the relevant metadata fields.
        };
        CurrentDebugPlace = "Before calling UpdateProductAsync";
        await client.UpdateProductAsync(product.ShopId,product.Id, updatedProduct);
        CurrentDebugPlace = "After calling UpdateProductAsync";
    }

    static int GetRoundedPrice(int price)
    {
        // Round the price to the nearest 0.99
        return (int)(Math.Ceiling(price / 100.0) * 100 - 1);
    }

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
}
record ProductMetadata(string title, string description, IEnumerable<string> tags);
