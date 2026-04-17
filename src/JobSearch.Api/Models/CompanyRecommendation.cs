namespace JobSearch.Api.Models;

public sealed record CompanyRecommendation(
    string CompanyName,
    string JobTitle,
    AgeGroup AgeGroup,
    string Platform,
    string Industry,
    string ExperienceLevel,
    string EmploymentType,
    string Region,
    int SalaryMillionKrw,
    string Summary,
    double SuitabilityScore);
