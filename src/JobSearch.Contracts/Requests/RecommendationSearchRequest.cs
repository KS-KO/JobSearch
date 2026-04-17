namespace JobSearch.Contracts.Requests;

public sealed class RecommendationSearchRequest
{
    public string? AgeGroup { get; init; }

    public string? ExperienceLevel { get; init; }

    public string? EmploymentType { get; init; }

    public string? Region { get; init; }

    public string? Industry { get; init; }
}
