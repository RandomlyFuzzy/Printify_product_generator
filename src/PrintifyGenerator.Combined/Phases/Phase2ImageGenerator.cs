using System.Text.Json;
using System.Text.Json.Nodes;

public static partial class PhaseFactory
{
	private sealed class Phase2ImageGenerator : PhaseGeneratorBase
	{
		public Phase2ImageGenerator() : base(2, "Image Generated", "id/id.png => byte[]") { }

		protected override bool IsCompleteCore(PhaseBundle bundle) => bundle.FindImagePath() is not null;

		protected override bool CanRunCore(PhaseBundle bundle)
			=> File.Exists(bundle.ResolvePhaseFile(1, "json", underscoreLegacy: true));

		protected override async Task<PhaseExecutionResult> ExecuteCoreAsync(PhaseBundle bundle, CancellationToken cancellationToken)
		{
			var runtime = CombinedRuntime.Current;
			var phase1Path = bundle.ResolvePhaseFile(1, "json", underscoreLegacy: true);
			var prompt = JsonSerializer.Deserialize<Prompt>(File.ReadAllText(phase1Path));
			if (prompt is null || !prompt.isValid())
			{
				return PhaseExecutionResult.Failed("Phase2 could not read phase1 prompt.");
			}

			var workflowPath = Path.Combine(runtime.DataRoot, "workloads", "better_default.json");
			if (!File.Exists(workflowPath))
			{
				return PhaseExecutionResult.Failed("Missing ComfyUI workflow src/data/workloads/better_default.json.");
			}

			var workflow = JsonXPath.Load(workflowPath);
			workflow = JsonXPath.Set(workflow, "//30:45/inputs/text", JsonValue.Create(prompt.positive)!);
			workflow = JsonXPath.Set(workflow, "//30:85/inputs/text", JsonValue.Create(prompt.negative)!);
			workflow = JsonXPath.Set(workflow, "//30:41/inputs/width", JsonValue.Create(prompt.width)!);
			workflow = JsonXPath.Set(workflow, "//30:41/inputs/height", JsonValue.Create(prompt.height)!);
			workflow = JsonXPath.Set(workflow, "//30:44/inputs/steps", JsonValue.Create(prompt.steps)!);
			workflow = JsonXPath.Set(workflow, "//30:44/inputs/cfg", JsonValue.Create(prompt.cfg)!);

			var emitter = new WebSocketEventEmitter();
			await using var comfyClient = new ComfyUiClient(runtime.NextComfyBaseUrl(), emitter);
			await comfyClient.StartListener();

			var promptId = comfyClient.QueuePrompt(workflow);
			var timeoutAt = DateTimeOffset.UtcNow.AddMinutes(10);
			JobStatus status;
			do
			{
				cancellationToken.ThrowIfCancellationRequested();
				status = comfyClient.GetJob(promptId);
				if (DateTimeOffset.UtcNow > timeoutAt)
				{
					return PhaseExecutionResult.Failed("Phase2 timed out waiting for ComfyUI job completion.");
				}

				await Task.Delay(2000, cancellationToken);
			}
			while (!string.Equals(status.Status, "completed", StringComparison.OrdinalIgnoreCase));

			var downloaded = await status.DownloadAllImagesAsync(bundle.DirectoryPath, bundle.Id.ToString());
			if (string.IsNullOrWhiteSpace(downloaded) || !File.Exists(downloaded))
			{
				return PhaseExecutionResult.Failed("Phase2 did not download an image from ComfyUI output.");
			}

			return PhaseExecutionResult.Done($"Created {Path.GetFileName(downloaded)} using ComfyUI.");
		}
	}
}
