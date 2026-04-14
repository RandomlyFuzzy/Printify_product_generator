using System;
using System.Collections.Generic;

public sealed class OrchestrationSettings
{
    public string PromptModel { get; set; } = "llama3.2:1b";
    public string SuitabilityModel { get; set; } = "gemma4:e2b";
    public string MockupVisionModel { get; set; } = "gemma4:e4b";
    public float MinimumPublishScore { get; set; } = 6.0f;
    public List<OrchestrationNode> Ollama { get; set; } = new List<OrchestrationNode>();
    public List<OrchestrationNode> ComfyUi { get; set; } = new List<OrchestrationNode>();
}

public sealed class OrchestrationNode
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}