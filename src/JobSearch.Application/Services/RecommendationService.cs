using JobSearch.Application.Abstractions;
using JobSearch.Domain.Models;

namespace JobSearch.Application.Services;

public sealed class RecommendationService : IRecommendationService
{
    private readonly ICompanyRecommendationRepository _repository;

    public RecommendationService(ICompanyRecommendationRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<CompanyRecommendation>> SearchAsync(
        RecommendationCriteria criteria,
        CancellationToken cancellationToken)
    {
        var recommendations = await _repository
            .GetRecommendationsAsync(cancellationToken)
            .ConfigureAwait(false);

        // Keep the filtering order explicit so later data-source changes do not
        // silently change the recommendation behavior.
        var query = recommendations.AsEnumerable();

        if (criteria.AgeGroup is not null)
        {
            query = query.Where(item => item.AgeGroup == criteria.AgeGroup);
        }

        if (!string.IsNullOrWhiteSpace(criteria.ExperienceLevel))
        {
            query = query.Where(item =>
                item.ExperienceLevel.Equals(criteria.ExperienceLevel, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(criteria.EmploymentType))
        {
            query = query.Where(item =>
                item.EmploymentType.Equals(criteria.EmploymentType, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Region))
        {
            query = query.Where(item =>
                item.Region.Contains(criteria.Region, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Industry))
        {
            query = query.Where(item =>
                item.Industry.Contains(criteria.Industry, StringComparison.OrdinalIgnoreCase));
        }

        return query
            .OrderByDescending(item => item.SuitabilityScore)
            .ThenBy(item => item.CompanyName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
