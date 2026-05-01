using Microsoft.AspNetCore.Mvc;
using PrintifyGenerator.AnalyticsApi.Models;
using PrintifyGenerator.AnalyticsApi.Services;

namespace PrintifyGenerator.AnalyticsApi.Controllers;

[ApiController]
[Route("api/market")]
public sealed class MarketController : ControllerBase
{
    private readonly MarketFeatureService _market;

    public MarketController(MarketFeatureService market)
    {
        _market = market;
    }

    // ── Global summary ───────────────────────────────────────────────────────

    /// <summary>Global top keywords, colours and materials across all category files.</summary>
    [HttpGet("summary")]
    public ActionResult<MarketSummaryResponse> GetSummary([FromQuery] int top = 20)
        => Ok(_market.BuildSummary(top));

    // ── Category acquisition ─────────────────────────────────────────────────

    /// <summary>List all parsed categories with name, type and product count.</summary>
    [HttpGet("categories")]
    public ActionResult<IReadOnlyList<CategoryListItem>> GetCategories()
        => Ok(_market.GetCategories());

    /// <summary>Full detail for a single category: keywords, colours, materials, price bands.</summary>
    [HttpGet("categories/{name}")]
    public ActionResult<CategoryDetailResponse> GetCategory(string name)
    {
        var result = _market.GetCategory(name);
        return result is null ? NotFound($"Category '{name}' not found.") : Ok(result);
    }

    /// <summary>Search categories by type (MainCategory, Gender, ColorBased, MaterialBased, PhraseCluster…).</summary>
    [HttpGet("categories/search")]
    public ActionResult<IReadOnlyList<CategoryDetailResponse>> SearchCategories(
        [FromQuery] string? type = null,
        [FromQuery] int limit = 20)
        => Ok(_market.SearchCategories(type, limit));

    // ── Cross-category overlap ────────────────────────────────────────────────

    /// <summary>
    /// Features (keywords, colours, materials) that appear in at least <paramref name="minCategories"/> categories.
    /// Useful for identifying universally sellable signals.
    /// </summary>
    [HttpGet("overlap")]
    public ActionResult<CrossCategoryOverlapResponse> GetOverlap(
        [FromQuery] int minCategories = 3,
        [FromQuery] int top = 30)
        => Ok(_market.GetCrossOverlap(minCategories, top));

    // ── Product scoring ───────────────────────────────────────────────────────

    /// <summary>
    /// Score a product title, description, colour and material against market data.
    /// Returns a 0-100 score with per-signal breakdown and recommended keywords.
    /// </summary>
    [HttpPost("score")]
    public ActionResult<ProductScoreResponse> ScoreProduct([FromBody] ProductScoreRequest request)
        => Ok(_market.ScoreProduct(request));

    // ── Ingest ────────────────────────────────────────────────────────────────

    /// <summary>Write or overwrite a category feature txt file and invalidate the in-memory cache.</summary>
    [HttpPost("ingest/category-feature")]
    public ActionResult<FileIngestResult> IngestCategoryFeature([FromBody] CategoryFeatureIngestRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FileName))  return BadRequest("fileName is required.");
        if (string.IsNullOrWhiteSpace(request.Content))   return BadRequest("content is required.");
        return Ok(_market.SaveCategoryFeatureFile(request.FileName, request.Content));
    }
}
