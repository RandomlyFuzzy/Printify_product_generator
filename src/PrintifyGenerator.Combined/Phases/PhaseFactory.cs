
using System.Text.Json;

public static class PhaseFactory
{
	public static IReadOnlyList<IPhaseGenerator> CreatePipeline() =>
		new IPhaseGenerator[]
		{
			new Phase1PromptGenerator(),
			new Phase2ImageGenerator(),
			new Phase3SuitabilityGenerator(),
			new Phase4ProductsGenerator(),
			new Phase5ProductAssessmentGenerator(),
			new Phase6MetadataGenerator(),
			new Phase7PublishingDirectionGenerator(),
			new Phase8PricingGenerator(),
			new Phase9ProductionQueueGenerator(),
			new Phase10ManualPublishingMarker(),
		};

	private static readonly JsonSerializerOptions PrettyJson = new()
	{
		WriteIndented = true,
	};

	private sealed class Phase1PromptGenerator : PhaseGeneratorBase
	{
		public Phase1PromptGenerator() : base(1, "Prompt Generated", "id/phase1.json => Prompt") { }

		protected override bool IsCompleteCore(PhaseBundle bundle)
			=> File.Exists(bundle.ResolvePhaseFile(1, "json", underscoreLegacy: true));

		protected override bool CanRunCore(PhaseBundle bundle) => true;

		protected override Task<PhaseExecutionResult> ExecuteCoreAsync(PhaseBundle bundle, CancellationToken cancellationToken)
		{
			var outputFile = bundle.ResolvePhaseFile(1, "json", underscoreLegacy: true);
			var prompt = new Prompt
			{
				positive = "vibrant graphic illustration, print-on-demand friendly composition",
				negative = "watermark, text artifacts, blur, low quality",
				width = 1024,
				height = 1024,
				steps = 30,
				cfg = 4.5f,
			};

			File.WriteAllText(outputFile, JsonSerializer.Serialize(prompt, PrettyJson));
			return Task.FromResult(PhaseExecutionResult.Done($"Created {Path.GetFileName(outputFile)}."));
		}
	}

	private sealed class Phase2ImageGenerator : PhaseGeneratorBase
	{
		private static readonly byte[] TinyPng = Convert.FromBase64String(
			"iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/w8AAusB9Y9xFwAAAABJRU5ErkJggg==");

		public Phase2ImageGenerator() : base(2, "Image Generated", "id/id.png => byte[]") { }

		protected override bool IsCompleteCore(PhaseBundle bundle) => bundle.FindImagePath() is not null;

		protected override bool CanRunCore(PhaseBundle bundle)
			=> File.Exists(bundle.ResolvePhaseFile(1, "json", underscoreLegacy: true));

		protected override Task<PhaseExecutionResult> ExecuteCoreAsync(PhaseBundle bundle, CancellationToken cancellationToken)
		{
			var outputFile = bundle.GetPath($"{bundle.Id}.png");
			File.WriteAllBytes(outputFile, TinyPng);
			return Task.FromResult(PhaseExecutionResult.Done($"Created {Path.GetFileName(outputFile)}."));
		}
	}

	private sealed class Phase3SuitabilityGenerator : PhaseGeneratorBase
	{
		public Phase3SuitabilityGenerator() : base(3, "Image Assessed", "id/phase3.json => ImageSuitability") { }

		protected override bool IsCompleteCore(PhaseBundle bundle)
			=> File.Exists(bundle.ResolvePhaseFile(3, "json", underscoreLegacy: true));

		protected override bool CanRunCore(PhaseBundle bundle) => bundle.FindImagePath() is not null;

		protected override Task<PhaseExecutionResult> ExecuteCoreAsync(PhaseBundle bundle, CancellationToken cancellationToken)
		{
			var imagePath = bundle.FindImagePath();
			if (imagePath is null)
			{
				return Task.FromResult(PhaseExecutionResult.Failed("Missing phase2 image."));
			}

			var outputFile = bundle.ResolvePhaseFile(3, "json", underscoreLegacy: true);
			var suitability = new ImageSuitability
			{
				imageURL = imagePath,
				suitability = 7.5f,
				DoesViolateLaw = false,
				DoesViolateIPRights = false,
				IsNSFW = false,
				Issues = new List<string>(),
				Scoring = new Scoring
				{
					commercialAppeal = 7,
					printQuality = 8,
					estimatedSalesViability = 7,
					uniqueness = 7,
					technicalSkill = 7,
					creativity = 8,
					composition = 7,
					technique = 7,
					originality = 8,
				},
			};

			File.WriteAllText(outputFile, JsonSerializer.Serialize(suitability, PrettyJson));
			return Task.FromResult(PhaseExecutionResult.Done($"Created {Path.GetFileName(outputFile)}."));
		}
	}

	private sealed class Phase4ProductsGenerator : PhaseGeneratorBase
	{
		public Phase4ProductsGenerator() : base(4, "Products Generated", "id/phase4.txt => PID[]") { }

		protected override bool IsCompleteCore(PhaseBundle bundle)
			=> File.Exists(bundle.ResolvePhaseFile(4, "txt"));

		protected override bool CanRunCore(PhaseBundle bundle)
			=> File.Exists(bundle.ResolvePhaseFile(3, "json", underscoreLegacy: true));

		protected override Task<PhaseExecutionResult> ExecuteCoreAsync(PhaseBundle bundle, CancellationToken cancellationToken)
		{
			var outputFile = bundle.ResolvePhaseFile(4, "txt");
			var prefix = bundle.Id.ToString("N")[..8].ToUpperInvariant();
			File.WriteAllLines(outputFile, new[] { $"PID-{prefix}-001", $"PID-{prefix}-002" });
			return Task.FromResult(PhaseExecutionResult.Done($"Created {Path.GetFileName(outputFile)}."));
		}
	}

	private sealed class Phase5ProductAssessmentGenerator : PhaseGeneratorBase
	{
		public Phase5ProductAssessmentGenerator() : base(5, "Products Assessed", "id/phase5.PID.json => ProductAssessment") { }

		protected override bool IsCompleteCore(PhaseBundle bundle)
		{
			var pids = bundle.ReadPhase4ProductIds();
			if (pids.Count == 0)
			{
				return false;
			}

			return pids.All(pid => File.Exists(bundle.GetPath($"phase5.{pid}.json")));
		}

		protected override bool CanRunCore(PhaseBundle bundle)
			=> bundle.ReadPhase4ProductIds().Count > 0;

		protected override Task<PhaseExecutionResult> ExecuteCoreAsync(PhaseBundle bundle, CancellationToken cancellationToken)
		{
			var pids = bundle.ReadPhase4ProductIds();
			foreach (var pid in pids)
			{
				var outputFile = bundle.GetPath($"phase5.{pid}.json");
				if (File.Exists(outputFile))
				{
					continue;
				}

				var assessment = new ProductAssessment
				{
					FitForPrintify = true,
					Issues = Array.Empty<string>(),
					shouldContinue = true,
				};
				File.WriteAllText(outputFile, JsonSerializer.Serialize(assessment, PrettyJson));
			}

			return Task.FromResult(PhaseExecutionResult.Done($"Created phase5 assessments ({pids.Count})."));
		}
	}

	private sealed class Phase6MetadataGenerator : PhaseGeneratorBase
	{
		public Phase6MetadataGenerator() : base(6, "Metadata Generated", "id/phase6.PID.Meta.json => ProductMeta") { }

		protected override bool IsCompleteCore(PhaseBundle bundle)
		{
			var pids = bundle.ReadPhase4ProductIds();
			if (pids.Count == 0)
			{
				return false;
			}

			return pids.All(pid => File.Exists(bundle.GetPath($"phase6.{pid}.meta.json")));
		}

		protected override bool CanRunCore(PhaseBundle bundle)
			=> PhaseCompleted(bundle, 5);

		protected override Task<PhaseExecutionResult> ExecuteCoreAsync(PhaseBundle bundle, CancellationToken cancellationToken)
		{
			var pids = bundle.ReadPhase4ProductIds();
			foreach (var pid in pids)
			{
				var outputFile = bundle.GetPath($"phase6.{pid}.meta.json");
				if (File.Exists(outputFile))
				{
					continue;
				}

				var meta = new ProductMeta
				{
					Title = $"{pid} premium print design",
					Description = "Auto-generated metadata. Review before publishing.",
					Tags = new[] { "print", "design", "pod" },
				};

				File.WriteAllText(outputFile, JsonSerializer.Serialize(meta, PrettyJson));
			}

			return Task.FromResult(PhaseExecutionResult.Done($"Created phase6 metadata ({pids.Count})."));
		}
	}

	private sealed class Phase7PublishingDirectionGenerator : PhaseGeneratorBase
	{
		public Phase7PublishingDirectionGenerator() : base(7, "Publishing Direction", "id/phase7.txt => PID,Store[]") { }

		protected override bool IsCompleteCore(PhaseBundle bundle)
			=> File.Exists(bundle.ResolvePhaseFile(7, "txt"));

		protected override bool CanRunCore(PhaseBundle bundle)
			=> PhaseCompleted(bundle, 6);

		protected override Task<PhaseExecutionResult> ExecuteCoreAsync(PhaseBundle bundle, CancellationToken cancellationToken)
		{
			var pids = bundle.ReadPhase4ProductIds();
			var outputFile = bundle.ResolvePhaseFile(7, "txt");
			File.WriteAllLines(outputFile, pids.Select(pid => $"{pid},Etsy,Ebay"));
			return Task.FromResult(PhaseExecutionResult.Done($"Created {Path.GetFileName(outputFile)}."));
		}
	}

	private sealed class Phase8PricingGenerator : PhaseGeneratorBase
	{
		public Phase8PricingGenerator() : base(8, "Pricing", "id/phase8.txt => PID,ProdId,VID,Shop,ProductionPrice,Price") { }

		protected override bool IsCompleteCore(PhaseBundle bundle)
			=> File.Exists(bundle.ResolvePhaseFile(8, "txt"));

		protected override bool CanRunCore(PhaseBundle bundle)
			=> File.Exists(bundle.ResolvePhaseFile(7, "txt"));

		protected override Task<PhaseExecutionResult> ExecuteCoreAsync(PhaseBundle bundle, CancellationToken cancellationToken)
		{
			var pids = bundle.ReadPhase4ProductIds();
			var outputFile = bundle.ResolvePhaseFile(8, "txt");
			var lines = pids.Select(pid => $"{pid},PROD-{pid},VID-{pid},Etsy,1299,1899");
			File.WriteAllLines(outputFile, lines);
			return Task.FromResult(PhaseExecutionResult.Done($"Created {Path.GetFileName(outputFile)}."));
		}
	}

	private sealed class Phase9ProductionQueueGenerator : PhaseGeneratorBase
	{
		public Phase9ProductionQueueGenerator() : base(9, "Moved To Production", "id/phase9.txt => PID[]") { }

		protected override bool IsCompleteCore(PhaseBundle bundle)
			=> File.Exists(bundle.ResolvePhaseFile(9, "txt"));

		protected override bool CanRunCore(PhaseBundle bundle)
			=> File.Exists(bundle.ResolvePhaseFile(8, "txt"));

		protected override Task<PhaseExecutionResult> ExecuteCoreAsync(PhaseBundle bundle, CancellationToken cancellationToken)
		{
			var phase8File = bundle.ResolvePhaseFile(8, "txt");
			var pids = File.ReadAllLines(phase8File)
				.Select(line => line.Split(',', 2, StringSplitOptions.TrimEntries)[0])
				.Where(pid => !string.IsNullOrWhiteSpace(pid))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToArray();

			var outputFile = bundle.ResolvePhaseFile(9, "txt");
			File.WriteAllLines(outputFile, pids);
			return Task.FromResult(PhaseExecutionResult.Done($"Created {Path.GetFileName(outputFile)}."));
		}
	}

	private sealed class Phase10ManualPublishingMarker : PhaseGeneratorBase
	{
		public Phase10ManualPublishingMarker() : base(10, "Human Assessment + Publishing", "id/phase10.manual.txt") { }

		protected override bool IsCompleteCore(PhaseBundle bundle)
			=> File.Exists(bundle.ResolvePhaseFile(10, "manual.txt"));

		protected override bool CanRunCore(PhaseBundle bundle)
			=> File.Exists(bundle.ResolvePhaseFile(9, "txt"));

		protected override Task<PhaseExecutionResult> ExecuteCoreAsync(PhaseBundle bundle, CancellationToken cancellationToken)
		{
			var outputFile = bundle.ResolvePhaseFile(10, "manual.txt");
			File.WriteAllText(outputFile, $"Manual publish gate reached at {DateTime.UtcNow:O}");
			return Task.FromResult(PhaseExecutionResult.Done($"Created {Path.GetFileName(outputFile)}."));
		}
	}

	private static bool PhaseCompleted(PhaseBundle bundle, int phaseNumber)
	{
		return phaseNumber switch
		{
			1 => File.Exists(bundle.ResolvePhaseFile(1, "json", underscoreLegacy: true)),
			2 => bundle.FindImagePath() is not null,
			3 => File.Exists(bundle.ResolvePhaseFile(3, "json", underscoreLegacy: true)),
			4 => File.Exists(bundle.ResolvePhaseFile(4, "txt")),
			5 => bundle.ReadPhase4ProductIds().All(pid => File.Exists(bundle.GetPath($"phase5.{pid}.json"))) &&
				 bundle.ReadPhase4ProductIds().Count > 0,
			6 => bundle.ReadPhase4ProductIds().All(pid => File.Exists(bundle.GetPath($"phase6.{pid}.meta.json"))) &&
				 bundle.ReadPhase4ProductIds().Count > 0,
			7 => File.Exists(bundle.ResolvePhaseFile(7, "txt")),
			8 => File.Exists(bundle.ResolvePhaseFile(8, "txt")),
			9 => File.Exists(bundle.ResolvePhaseFile(9, "txt")),
			10 => File.Exists(bundle.ResolvePhaseFile(10, "manual.txt")),
			_ => false,
		};
	}
}