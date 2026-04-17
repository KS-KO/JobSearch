using JobSearch.Domain.Enums;

namespace JobSearch.Domain.Models;

public sealed record CompanyRecommendation(
    string CompanyName,
    string JobTitle,
    string JobUrl,
    AgeGroup AgeGroup,
    string Platform,
    string Industry,
    string ExperienceLevel,
    string EmploymentType,
    string Region,
    int SalaryMillionKrw,
    string Summary,
    double SuitabilityScore);
