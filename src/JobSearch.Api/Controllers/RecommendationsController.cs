using JobSearch.Api.Services;
using JobSearch.Domain.Models;
using JobSearch.Domain.Enums;
using JobSearch.Api.Requests;
using JobSearch.Api.Responses;
using Microsoft.AspNetCore.Mvc;

namespace JobSearch.Api.Controllers;

[ApiController]
[Route("api/recommendations")]
public sealed class RecommendationsController : ControllerBase
{
    private readonly RecommendationQueryService _recommendationService;

    public RecommendationsController(RecommendationQueryService recommendationService)
    {
        _recommendationService = recommendationService;
    }

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<RecommendationItemResponse>>> SearchAsync(
        [FromQuery] RecommendationSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParseAgeGroup(request.AgeGroup, out var ageGroup))
        {
            return BadRequest("Invalid age group.");
        }

        var criteria = new RecommendationCriteria(
            ageGroup,
            request.ExperienceLevel,
            request.EmploymentType,
            request.Region,
            request.Industry,
            request.MinSalaryMillionKrw);

        var items = await _recommendationService
            .SearchAsync(criteria, cancellationToken)
            .ConfigureAwait(false);

        return Ok(items.Select(item => new RecommendationItemResponse(
                item.CompanyName,
                item.JobTitle,
                item.JobUrl,
                item.AgeGroup.ToString(),
                item.Platform,
                item.Industry,
                item.ExperienceLevel,
                item.EmploymentType,
                item.Region,
                item.SalaryMillionKrw,
                item.Summary,
                item.SuitabilityScore))
            .ToArray());
    }

    private static bool TryParseAgeGroup(string? value, out AgeGroup? ageGroup)
    {
        ageGroup = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var normalized = value.Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Trim();

        if (Enum.TryParse<AgeGroup>(normalized, ignoreCase: true, out var parsed))
        {
            ageGroup = parsed;
            return true;
        }

        return false;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsResponse>> GetStatsAsync(CancellationToken cancellationToken)
    {
        var stats = await _recommendationService.GetStatsAsync(cancellationToken).ConfigureAwait(false);
        var response = new DashboardStatsResponse(
            stats.Total,
            stats.Saramin,
            stats.JobKorea,
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        );

        return Ok(response);
    }
}
