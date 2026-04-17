using JobSearch.Domain.Models;

namespace JobSearch.Application.Abstractions;

public interface ICompanyRecommendationRepository
{
    Task<IReadOnlyList<CompanyRecommendation>> GetRecommendationsAsync(CancellationToken cancellationToken);
}
