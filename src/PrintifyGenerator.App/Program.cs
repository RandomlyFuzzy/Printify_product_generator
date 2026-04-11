
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


Console.WriteLine("Hello, Printify Generator!");

OllamaClient ollama = new OllamaClient();

WebSocketEventEmitter emitter = new WebSocketEventEmitter();
ComfyUiClient comfyUi = new ComfyUiClient("http://localhost:8188",emitter);
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


    const string JsonPrompt = @"You are a prompt engineer for AI image generation. Output ONLY a valid JSON array with no markdown, no explanation, no code fences, and no extra text — just raw JSON.
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


    string returnedPrompt = "";
    List<Prompt> promptList = new ();
    while(promptList.Count < 30){
        returnedPrompt = "";
        try
        {
            await foreach (var response in ollama.GenerateStreamAsync("llama3.2:1b", JsonPrompt))
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
                Console.WriteLine("No prompts returned from Ollama.");
                return; 
            }
            foreach (var prompt in promptList2)
            {
                if (prompt.isValid())
                {
                    promptList.Add(prompt);
                }
            }
            Console.WriteLine($"Received {promptList2.Count} prompts. Total so far: {promptList.Count}");
            //setcursor up to overwrite the previous line
            Console.SetCursorPosition(0, Console.CursorTop - 1);
        }
        catch (Exception ex)
        {
        }
    }
    Console.SetCursorPosition(0, Console.CursorTop + 1);

    // foreach (var prompt in promptList)
    // {
    //     Console.WriteLine($"Positive: {prompt.positive}");
    //     Console.WriteLine($"Negative: {prompt.negative}");
    //     Console.WriteLine("-----");
    // }


    string path = "src/data/workloads/illustration_lora_base.json";
    JsonNode baseWorkflow = JsonNode.Parse(File.ReadAllText(path));

    List<JsonNode> ImagePrimptList = new List<JsonNode>();

    foreach (var prompt in promptList)
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

        ImagePrimptList.Add(workflowCopy);
    }

    Console.WriteLine("");
    Console.WriteLine("Generated Workflows:");
    //convert the list of workflows to json and print it

    //send the first workflow to comfyui
    if (ImagePrimptList.Count < 0)
    {
        Console.WriteLine("No workflows generated. Exiting.");
        return;
    }
    JobStatus jobStatus;
    int job = 0;
    foreach (var workflow in ImagePrimptList)
    {
        job++;
        string workflowJson = workflow.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        Console.WriteLine("Sending workflow to ComfyUI...");
        // Console.WriteLine(workflowJson);
        string jobId = comfyUi.QueuePrompt(workflow);
        Console.WriteLine($"Job queued with ID: {jobId}");  
        while ((jobStatus = comfyUi.GetJob(jobId)).Status != "completed")
        {
            Console.WriteLine($"Job {jobId} status: {jobStatus.Status} {jobStatus.Progress}% {job}/{ImagePrimptList.Count}"  );
            await Task.Delay(5000); // wait for 5 seconds before checking again
            //set cursor up to overwrite the previous line
            Console.SetCursorPosition(0, Console.CursorTop - 1);
        }
        Console.SetCursorPosition(0, Console.CursorTop + 1);
        //download the images
        if(!Directory.Exists($"./src/data/phase_1/{DateTime.Now:yyyy-MM/dd}"))
            Directory.CreateDirectory($"./src/data/phase_1/{DateTime.Now:yyyy-MM/dd}");
        File.WriteAllText($"./src/data/phase_1/{DateTime.Now:yyyy-MM/dd}/{jobId}.json", promptList[job-1].ToPrityJsonString());
        await jobStatus.DownloadAllImagesAsync($"./src/data/phase_2/{DateTime.Now:yyyy-MM/dd}/");
        Console.WriteLine($"Job {jobId} completed. Outputs downloaded.");
    }
}