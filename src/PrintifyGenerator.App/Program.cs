
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
using PrintifyGenerator.Library.printify;


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








return;
OllamaClient ollama = new OllamaClient();
OllamaClient ollamaExternal = new OllamaClient("http://192.168.0.151:11434");

WebSocketEventEmitter emitter = new WebSocketEventEmitter();
ComfyUiClient comfyUi = new ComfyUiClient("http://192.168.0.151:8188",emitter);
await comfyUi.StartListener();


while(!Console.KeyAvailable)
{
    string status = await ollama.CheckStatusAsync();
    Console.WriteLine($"Ollama Status: {status}");
    if(status == string.Empty)
    {
        Console.WriteLine("Ollama is not running. Please start Ollama and try again.");
        return;
    }


    const string initalPrompt = @"You are a prompt engineer for AI image generation. Output ONLY a valid JSON array with no markdown, no explanation, no code fences, and no extra text — just raw JSON.
    Generate 2 creative Stable Diffusion prompts for print-on-demand products (t-shirts, posters, mugs). Each should be visually striking, commercially appealing, and varied in style (e.g. illustration, watercolor, retro, minimalist, photorealistic).
    Use this exact format:
    [
    {
        ""positive"": ""detailed positive prompt here, comma separated tags, style, lighting, quality boosters"",
        ""negative"": ""ugly, deformed, blurry, low quality, watermark, text, bad anatomy, extra limbs"",
        ""width"": 512,
        ""height"": 512,
        ""steps"": 25,
        ""cfg"": 7
    }
    ]";

    string SutabilityPrompt =@"you a someone who doesnt want their inilectual property online as well not to be inapproiate to a general audience. 
     you role is to tell me things that are wrong with this image and how sutable it is to be sold and reason why it isnt suitable 
     you will return a json and only a json to explain this
     "+new ImageSuitability().PrityJsonString();

    //Find Images that are not in phase 3
    List<(string id,string path)> phase3Responses = new();
    //get files recursively in phase 3
    if(Directory.Exists("./src/data/phase_3"))
    {
        var files = Directory.GetFiles("./src/data/phase_3", "*.*", SearchOption.AllDirectories);
        foreach(var file in files)        {
            if(file.EndsWith(".json") || file.EndsWith(".jpg") || file.EndsWith(".jpeg"))
            {
                phase3Responses.Add((Path.GetFileNameWithoutExtension(file), file));
            }
        }
    }
    List<(string id,string path)> phase2Responses = new();
    if(Directory.Exists("./src/data/phase_2"))
    {
        var files = Directory.GetFiles("./src/data/phase_2", "*.*", SearchOption.AllDirectories);
        foreach(var file in files)        {
            if(file.EndsWith(".png"))
            {
                phase2Responses.Add((Path.GetFileNameWithoutExtension(file), file));
            }
        }
    }
    var toProcess = phase2Responses.Where(x => !phase3Responses.Any(y => y.id == x.id)).Select(a => a.path).ToList();

    await foreach(var suitabilities in GenerateImageSuitability(ollama, toProcess,SutabilityPrompt))
    {
        //keep the same structure just using phase_3 instaead of phase_2 and a json instead of a png
        string newpath = suitabilities.imageURL.Replace("phase_2","phase_3");
        newpath = newpath.Replace(".png", ".json");
        newpath = newpath.Substring(6);
        Console.WriteLine(newpath);
        if(!Directory.Exists(Path.GetDirectoryName(newpath)))
            Directory.CreateDirectory(Path.GetDirectoryName(newpath));

        File.WriteAllText(newpath, suitabilities.PrityJsonString());
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
                //keep the same structure just using phase_3 instaead of phase_2 and a json instead of a png
                string newpath = suitabilities.imageURL.Replace("phase_2","phase_3").Replace(".png", ".json").Substring(6);
                if(!Directory.Exists(Path.GetDirectoryName(newpath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(newpath));

                File.WriteAllText(newpath, suitabilities.PrityJsonString());
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
            await foreach (var response in ol.GenerateWithImageStreamAsync("gemma4:e2b",  SutabilityPrompt,image))
            {
                totalResp += response;
                // Console.Write(response);
            }
            totalResp = totalResp.Substring(totalResp.IndexOf("{")); // get the json part only
            totalResp = totalResp.Substring(0,totalResp.LastIndexOf("}")+1); // get the json part only
            suitability = JsonSerializer.Deserialize<ImageSuitability>(totalResp) ?? new ImageSuitability();
            if (suitability.isValid()){                
                suitability.imageURL = "file://home/rf/Desktop/Printify_prodcuct_generator/" + image.Substring(2);
            }else{
                Console.WriteLine($"Invalid suitability response for {image}. Response: {suitability.PrityJsonString()}");
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
        if(!Directory.Exists($"./src/data/phase_1/{DateTime.Now:yyyy-MM/dd}"))
            Directory.CreateDirectory($"./src/data/phase_1/{DateTime.Now:yyyy-MM/dd}");
        File.WriteAllText($"./src/data/phase_1/{DateTime.Now:yyyy-MM/dd}/{jobId}.json", prompt.ToPrityJsonString());
        yield return await jobStatus.DownloadAllImagesAsync($"./src/data/phase_2/{DateTime.Now:yyyy-MM/dd}/");
        Console.WriteLine($"Job {jobId} completed. Outputs downloaded.");
    }
    
}