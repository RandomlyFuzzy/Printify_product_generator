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
        var ollamaClient = new OllamaClient("http://localhost:11434");

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


        var Products = await client.GetAllProductsAsync(stagingShop.Id);
        Console.WriteLine($"Products in staging shop: {Products.Count()}");

        foreach (var product in Products)
        {
            //1.download all the images for the product and save them to a local folder
            //download images to a temp folder 
            //2. generate metadata for the product using the ollama client and the downloaded images
            //3. update the product metadata in printify using the client
            //4. cleanup the temp images
            var tempFolder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempFolder);
            var imagePaths = await DownloadProductImagesAsync(client, product, tempFolder);
            bool completed = false;
            while(!completed){
                try
                {        
                    var metadata = await GenerateMetadataAsync(ollamaClient, imagePaths);
                    await UpdateProductMetadataAsync(client, product, metadata);
                }catch (Exception ex)
                {
                    Console.Error.WriteLine($"[ERROR] Failed to process product {product.Id}: {ex.Message} at {CurrentDebugPlace}");
                }
                finally
                {
                    Directory.Delete(tempFolder, true);
                }
                completed = true;
            }
        }
        return 0;
    }



    static async Task<IEnumerable<string>> DownloadProductImagesAsync(PrintifyClient client, Product product, string tempFolder)
    {
        var imagePaths = new List<string>();
        int id = 0;
        Console.WriteLine($"Downloading images for product {product.Id} total images: {product.Images.Count()}");
        foreach (var image in product.Images)
        {
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
            product like this.

            Provide the metadata in JSON format with the following structure:
            {{
                ""title"": ""Generated Product Title"",
                ""description"": ""Generated product description based on the images."",
                ""tags"": [""tag1"", ""tag2"", ""tag3""]
            }}
        ";
        CurrentDebugPlace = "Before calling GenerateWithImageAsync";
        string response = "";
        await foreach(var data in ollamaClient.GenerateWithImagesStreamAsync("gemma4:e2b", prompt, imagePaths.ToArray()))
        {
            response += data;
            Console.Write(data);
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

        var updatedProduct = new UpdateProductRequest
        {
            Title = metadata.title,
            Description = metadata.description,
            Tags = metadata.tags.ToList(),
            // Note: In a real implementation, you'd likely want to preserve other product properties and only update the relevant metadata fields.
        };
        CurrentDebugPlace = "Before calling UpdateProductAsync";
        await client.UpdateProductAsync(product.ShopId,product.Id, updatedProduct);
        CurrentDebugPlace = "After calling UpdateProductAsync";
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
