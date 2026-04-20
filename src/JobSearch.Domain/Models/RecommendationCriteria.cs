using JobSearch.Domain.Enums;

namespace JobSearch.Domain.Models;

public sealed record RecommendationCriteria(
    AgeGroup? AgeGroup,
    string? ExperienceLevel,
    string? EmploymentType,
    string? Region,
    string? Industry,
    int? MinSalaryMillionKrw = null);
