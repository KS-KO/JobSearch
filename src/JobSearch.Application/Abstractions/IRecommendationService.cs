using JobSearch.Domain.Models;

namespace JobSearch.Application.Abstractions;

public interface IRecommendationService
{
    Task<IReadOnlyList<CompanyRecommendation>> SearchAsync(
        RecommendationCriteria criteria,
        CancellationToken cancellationToken);
}
