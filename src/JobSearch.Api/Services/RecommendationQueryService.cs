using JobSearch.Domain.Models;
using JobSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JobSearch.Api.Services;

public sealed class RecommendationQueryService
{
    private readonly JobSearchDbContext _context;

    public RecommendationQueryService(JobSearchDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CompanyRecommendation>> SearchAsync(
        RecommendationCriteria criteria,
        CancellationToken cancellationToken)
    {
        var query = _context.Recommendations.AsQueryable();

        if (criteria.AgeGroup is not null)
        {
            query = query.Where(item => item.AgeGroup == criteria.AgeGroup);
        }

        if (!string.IsNullOrWhiteSpace(criteria.ExperienceLevel))
        {
            query = query.Where(item =>
                EF.Functions.Like(item.ExperienceLevel, $"%{criteria.ExperienceLevel}%"));
        }

        if (!string.IsNullOrWhiteSpace(criteria.EmploymentType))
        {
            query = query.Where(item =>
                EF.Functions.Like(item.EmploymentType, $"%{criteria.EmploymentType}%"));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Region))
        {
            query = query.Where(item =>
                EF.Functions.Like(item.Region, $"%{criteria.Region}%"));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Industry))
        {
            query = query.Where(item =>
                EF.Functions.Like(item.Industry, $"%{criteria.Industry}%"));
        }

        var results = await query
            .OrderByDescending(item => item.SuitabilityScore)
            .ThenBy(item => item.CompanyName)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return results
            .Select(item => new CompanyRecommendation(
                item.CompanyName,
                item.JobTitle,
                item.JobUrl,
                item.AgeGroup,
                item.Platform,
                item.Industry,
                item.ExperienceLevel,
                item.EmploymentType,
                item.Region,
                item.SalaryMillionKrw,
                item.Summary,
                item.SuitabilityScore))
            .ToArray();
    }
}
