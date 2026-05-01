using Microsoft.AspNetCore.Mvc;
using PrintifyGenerator.AnalyticsApi.Models;
using PrintifyGenerator.AnalyticsApi.Services;

namespace PrintifyGenerator.AnalyticsApi.Controllers;

[ApiController]
[Route("api/phases")]
public sealed class PhasesController : ControllerBase
{
    private readonly PhaseDataService _phaseData;

    public PhasesController(PhaseDataService phaseData)
    {
        _phaseData = phaseData;
    }

    [HttpGet("bundles")]
    public ActionResult<IReadOnlyList<PhaseBundleSummary>> ListBundles([FromQuery] int limit = 100)
    {
        return Ok(_phaseData.ListBundles(limit));
    }

    [HttpGet("overview")]
    public ActionResult<IReadOnlyList<PhaseOverviewItem>> GetOverview()
    {
        return Ok(_phaseData.GetPhaseOverview());
    }

    [HttpGet("bundles/{bundleId:guid}")]
    public ActionResult<PhaseBundleSummary> GetBundle(Guid bundleId)
    {
        var bundle = _phaseData.GetBundle(bundleId);
        return bundle is null ? NotFound() : Ok(bundle);
    }

    [HttpGet("bundles/{bundleId:guid}/phase/{phase}")]
    public ActionResult<IReadOnlyList<PhaseArtifactReference>> GetPhaseArtifacts(Guid bundleId, string phase)
    {
        var artifacts = _phaseData.GetPhaseArtifacts(bundleId, phase);
        return Ok(artifacts);
    }

    [HttpGet("artifacts/{phase}")]
    public ActionResult<IReadOnlyList<PhaseArtifactReference>> GetLatestPhaseArtifacts(string phase, [FromQuery] int limit = 200)
    {
        return Ok(_phaseData.GetLatestPhaseArtifacts(phase, limit));
    }

    [HttpGet("bundles/{bundleId:guid}/artifacts/{fileName}")]
    public ActionResult<PhaseArtifactResult> GetArtifact(Guid bundleId, string fileName)
    {
        var artifact = _phaseData.GetArtifact(bundleId, fileName);
        return artifact is null ? NotFound() : Ok(artifact);
    }

    [HttpPost("ingest/product-definition")]
    public ActionResult<IngestResult> IngestProductDefinition([FromBody] IngestProductDefinitionRequest request)
    {
        var result = _phaseData.SaveProductDefinition(request);
        return Ok(result);
    }

    [HttpPost("ingest/artifact")]
    public ActionResult<IngestResult> IngestPhaseArtifact([FromBody] IngestPhaseDataRequest request)
    {
        var result = _phaseData.SavePhaseData(request);
        return Ok(result);
    }
}
