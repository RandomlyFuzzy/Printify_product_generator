
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Specialized;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

using System.Net.Http.Headers;
using System.Text.Json;
using System.Runtime.InteropServices.JavaScript;


//read the token from the main.env file
string token = "";
if (File.Exists("./main.env"))
{
    var lines = File.ReadAllLines("./main.env");
    foreach (var line in lines)    {
        if (line.StartsWith("TOKEN="))
        {
            token = line.Substring("TOKEN=".Length).Trim();
            break;
        }
    }
}
// Console.WriteLine($"Token: {token}");

// PrintifyClient client = new PrintifyClient(token);

// // foreach(var file in Directory.GetFiles("./src/data/phase_3", "*.*", SearchOption.AllDirectories))
// // {
// //     ImageSuitability suitability = JsonSerializer.Deserialize<ImageSuitability>(File.ReadAllText(file)) ?? new ImageSuitability();
// //     if(suitability.Scoring.OverallScore() > 6.0f && suitability.IsSuitableForPrint())
// //     {
// //         // Console.WriteLine($"Uploading {file} to Printify...");
// //         //upload the image to printify using the api and get the url
// //         var imageUrl = await client.UploadImageFromFileAsync(suitability.imageURL.Substring(6)); // remove file:// from the path
// //         Console.WriteLine($"Image uploaded. URL: {imageUrl}");
// //     }
// // }

// foreach(var images in (await client.GetUploadsAsync(1,50)).Data)
// {
//     Console.WriteLine($"Uploaded image: {images.Id}, URL: {images.FileName}");
// }




// return;
OllamaClient ollama =  new OllamaClient("http://192.168.0.131:11434");//new OllamaClient("http://localhost:11434");
// OllamaClient ollamaExternal = new OllamaClient("http://192.168.0.151:11434");
OllamaClient ollamaExternal = new OllamaClient("http://192.168.0.131:11434");

WebSocketEventEmitter emitter = new WebSocketEventEmitter();
ComfyUiClient comfyUi = new ComfyUiClient("http://192.168.0.151:8188",emitter);
await comfyUi.StartListener();


while(!Console.KeyAvailable)
{
    string status = await ollamaExternal.CheckStatusAsync();
    Console.WriteLine($"Ollama Status: {status}");
    if(status == string.Empty)
    {
        Console.WriteLine("Ollama is not running. Please start Ollama and try again.");
        return;
    }


    const string initalPrompt = @"You are a prompt engineer for AI image generation. Output ONLY a valid JSON array with no markdown, no explanation, no code fences, and no extra text — just raw JSON.
    Generate 3 creative Stable Diffusion prompts for print-on-demand products (t-shirts, posters, mugs). Each should be visually striking, commercially appealing, and varied in style (e.g. illustration, watercolor, retro, minimalist, photorealistic).
    You should try and make it appealing to a wide audience and not too niche. Avoid text in the image, as it can be hard to read on products. Each prompt should have a positive part describing the main subject and style, and a negative part specifying what to avoid.
    Due to what this is going to be used for you can you make it either landscape portrait or square to give a wide range of products the prompt can be put onto.
    Use this exact format:
    [
    {
        ""positive"": ""detailed positive prompt here, comma separated tags, style, lighting, quality boosters"",
        ""negative"": ""ugly, deformed, blurry, low quality, watermark, text, bad anatomy, extra limbs"",
        ""width"": <number>,
        ""height"": <number>,
        ""steps"": <range between 20-50>,
        ""cfg"": <range between 3 and 7>
    }
    ]";

    string SutabilityPrompt =@"you a someone who doesnt want their inilectual property online as well not to be inapproiate to a general audience. 
     you role is to tell me things that are wrong with this image and how sutable it is to be sold and reason why it isnt suitable 
     you will return a json and only a json to explain this
     "+new ImageSuitability().PrityJsonString();

    var toProcess = GetPendingSuitabilityImages().ToList();

    await foreach(var suitabilities in GenerateImageSuitability(ollamaExternal, toProcess,SutabilityPrompt))
    {
        string newPath = WriteSuitabilityRecord(suitabilities);
        Console.WriteLine(newPath);
        Console.WriteLine("Generated suitability for " + suitabilities.imageURL);
    }


    Console.WriteLine(toProcess.Count());

    var getPrompts = GeneratePrompt(initalPrompt);
    var getImages = GenerateImage(getPrompts);
    List<string> images = new ();

    await foreach(var image in getImages){
        images.Add(image);
        if (images.Count > 4)
        {
            await foreach(var suitabilities in GenerateImageSuitability(ollamaExternal, images,SutabilityPrompt))
            {
                Console.WriteLine(suitabilities.PrityJsonString());
                WriteSuitabilityRecord(suitabilities);
                Console.WriteLine("Generated suitability for " + suitabilities.imageURL);
            }
            images.Clear();
        }
    }

}

async IAsyncEnumerable<ImageSuitability> GenerateImageSuitability(OllamaClient ol,List<string> images,string SutabilityPrompt)
{
    foreach (var image in images)
    {
        string totalResp = "";
        ImageSuitability suitability = new ImageSuitability();
        start:
        try
        {
            // await foreach (var response in ol.GenerateWithImageStreamAsync("gemma4:e2b",  SutabilityPrompt,image))
            await foreach (var response in ol.GenerateWithImageStreamAsync("gemma4:e2b",  SutabilityPrompt,image))
            {
                totalResp += response;
                // Console.Write(response);
            }
            totalResp = totalResp.Substring(totalResp.IndexOf("{")); // get the json part only
            totalResp = totalResp.Substring(0,totalResp.LastIndexOf("}")+1); // get the json part only
            suitability = JsonSerializer.Deserialize<ImageSuitability>(totalResp) ?? new ImageSuitability();
            if (suitability.isValid()){
                suitability.imageURL = CreateImageFileUri(image);
            }else{
                // Console.WriteLine($"Invalid suitability response for {image}. Response: {suitability.PrityJsonString()}");
                totalResp = "";
                goto start;
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Error generating suitability for {image}: {ex.Message}");
            goto start;
        }
        Console.WriteLine(suitability.PrityJsonString());
        yield return suitability;
    }
}

async IAsyncEnumerable<Prompt> GeneratePrompt(string initalPrompt,CancellationToken token = default)
{
    string returnedPrompt = "";
    int cnt = 0;
    while(!token.IsCancellationRequested){
        returnedPrompt = "";
        List<Prompt> prompts = new();
        try
        {
            await foreach (var response in ollama.GenerateStreamAsync("llama3.2:1b", initalPrompt))
            {
                // Console.Write(response);
                returnedPrompt += response;
            }
            //json parse the returned prompt
            //if the end doesnt contain ] then add it to the end to make it a valid json
            if (!returnedPrompt.Trim().EndsWith("]")){
                returnedPrompt += "]";
            }
            var promptList2 = JsonSerializer.Deserialize<List<Prompt>>(returnedPrompt);

            if (promptList2 == null || promptList2.Count == 0)
            {
                continue; 
            }
            prompts = promptList2;
        }
        catch (Exception ex)
        {
            // Console.WriteLine("Error: "+ex.Message);
            // Console.WriteLine(returnedPrompt);
        }
        foreach (var prompt in prompts)
        {
            Console.WriteLine("Generated Prompt "+(cnt++));
            yield return prompt;
        }
    }
    // Console.SetCursorPosition(0, Console.CursorTop + 1);
    
}

async IAsyncEnumerable<string> GenerateImage(IAsyncEnumerable<Prompt> prompts)
{
    string path = "src/data/workloads/illustration_lora_base.json";
    JsonNode baseWorkflow = JsonNode.Parse(File.ReadAllText(path));
    JobStatus jobStatus;
    int job = 0;
    await foreach (var prompt in prompts)
    {
        var workflowCopy = JsonXPath.Load(path);

        if (workflowCopy == null) continue;

        // possative is 76:6.inputs.text
        // negative is 76:7.inputs.text
        workflowCopy = JsonXPath.Set(workflowCopy, "//76:6/inputs/text", JsonValue.Create(prompt.positive));
        workflowCopy = JsonXPath.Set(workflowCopy, "//76:7/inputs/text", JsonValue.Create(prompt.negative));

        //76:58.inputs.width/height 
        workflowCopy = JsonXPath.Set(workflowCopy, "//76:58/inputs/width", JsonValue.Create(prompt.width));
        workflowCopy = JsonXPath.Set(workflowCopy, "//76:58/inputs/height", JsonValue.Create(prompt.height));

        //76:3.inputs.steps/cfg
        workflowCopy = JsonXPath.Set(workflowCopy, "//76:3/inputs/steps", JsonValue.Create(prompt.steps));
        workflowCopy = JsonXPath.Set(workflowCopy, "//76:3/inputs/cfg", JsonValue.Create(prompt.cfg));


        string jobId = comfyUi.QueuePrompt(workflowCopy);
        Console.WriteLine($"Job queued with ID: {jobId}");  
        while ((jobStatus = comfyUi.GetJob(jobId)).Status != "completed")
        {
            Console.WriteLine($"Job {jobId} status: {jobStatus.Status} {jobStatus.Progress.ToString("F0")}% {job}"  );
            await Task.Delay(5000); // wait for 5 seconds before checking again
            //set cursor up to overwrite the previous line
            Console.SetCursorPosition(0, Console.CursorTop - 1);
        }
        Console.SetCursorPosition(0, Console.CursorTop + 1);
        //download the images
        string baseDir = $"./src/data/Checking/{DateTime.Now:yyyy-MM/dd}/{jobId}";
        if(!Directory.Exists(baseDir))
            Directory.CreateDirectory(baseDir);
        File.WriteAllText(Path.Combine(baseDir, "phase_1.json"), prompt.ToPrityJsonString());
        yield return await jobStatus.DownloadAllImagesAsync(baseDir);
        Console.WriteLine($"Job {jobId} completed. Outputs downloaded.");
    }
    
}

List<string> GetPendingSuitabilityImages()
{
    var pendingImages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    const string checkingRoot = "./src/data/Checking";
    if (Directory.Exists(checkingRoot))
    {
        foreach (var imagePath in Directory.EnumerateFiles(checkingRoot, "*.png", SearchOption.AllDirectories))
        {
            var folderPath = Path.GetDirectoryName(imagePath);
            if (string.IsNullOrWhiteSpace(folderPath))
                continue;

            var phase3Record = Path.Combine(folderPath, "phase_3.json");
            if (!File.Exists(phase3Record))
                pendingImages.Add(imagePath);
        }
    }

    const string phase2Root = "./src/data/phase_2";
    const string phase3Root = "./src/data/phase_3";
    if (Directory.Exists(phase2Root))
    {
        foreach (var imagePath in Directory.EnumerateFiles(phase2Root, "*.png", SearchOption.AllDirectories))
        {
            var legacyOutput = GetLegacySuitabilityPath(imagePath, phase2Root, phase3Root);
            if (!File.Exists(legacyOutput))
                pendingImages.Add(imagePath);
        }
    }

    return pendingImages.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToList();
}

string WriteSuitabilityRecord(ImageSuitability suitability)
{
    var imagePath = ResolveImagePathFromUrl(suitability.imageURL);
    var outputPath = GetSuitabilityOutputPath(imagePath);

    var directory = Path.GetDirectoryName(outputPath);
    if (!string.IsNullOrWhiteSpace(directory))
        Directory.CreateDirectory(directory);

    File.WriteAllText(outputPath, suitability.ToJsonString());
    return outputPath;
}

string GetSuitabilityOutputPath(string imagePath)
{
    var fullImagePath = Path.GetFullPath(imagePath);
    var directory = Path.GetDirectoryName(fullImagePath) ?? Directory.GetCurrentDirectory();
    var folderName = Path.GetFileName(directory);
    var imageName = Path.GetFileNameWithoutExtension(fullImagePath);
    var checkingSegment = $"{Path.DirectorySeparatorChar}Checking{Path.DirectorySeparatorChar}";

    if (fullImagePath.Contains(checkingSegment, StringComparison.OrdinalIgnoreCase)
        && string.Equals(folderName, imageName, StringComparison.OrdinalIgnoreCase))
    {
        return Path.Combine(directory, "phase_3.json");
    }

    const string phase2Root = "./src/data/phase_2";
    const string phase3Root = "./src/data/phase_3";
    if (fullImagePath.Contains($"{Path.DirectorySeparatorChar}phase_2{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
        return GetLegacySuitabilityPath(fullImagePath, phase2Root, phase3Root);

    return Path.ChangeExtension(fullImagePath, ".json");
}

string GetLegacySuitabilityPath(string imagePath, string phase2Root, string phase3Root)
{
    var fullImagePath = Path.GetFullPath(imagePath);
    var fullPhase2Root = Path.GetFullPath(phase2Root)
        .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    var fullPhase3Root = Path.GetFullPath(phase3Root)
        .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    if (!fullImagePath.StartsWith(fullPhase2Root, StringComparison.OrdinalIgnoreCase))
        return Path.ChangeExtension(fullImagePath, ".json");

    var relativePath = Path.GetRelativePath(fullPhase2Root, fullImagePath);
    return Path.ChangeExtension(Path.Combine(fullPhase3Root, relativePath), ".json");
}

string ResolveImagePathFromUrl(string imageUrl)
{
    if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var imageUri) && imageUri.IsFile)
        return imageUri.LocalPath;

    if (imageUrl.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
    {
        var withoutScheme = imageUrl["file://".Length..];
        if (!withoutScheme.StartsWith('/'))
            withoutScheme = "/" + withoutScheme;

        return Path.GetFullPath(withoutScheme);
    }

    return Path.GetFullPath(imageUrl);
}

string CreateImageFileUri(string imagePath)
{
    return new Uri(Path.GetFullPath(imagePath)).AbsoluteUri;
}