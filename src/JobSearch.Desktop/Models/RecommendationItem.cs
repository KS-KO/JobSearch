namespace JobSearch.Desktop.Models;

public sealed record RecommendationItem(
    string CompanyName,
    string JobTitle,
    string JobUrl,
    string AgeGroup,
    string AgeGroupLabel,
    string Platform,
    string Industry,
    string ExperienceLevel,
    string EmploymentType,
    string Region,
    int SalaryMillionKrw,
    string Summary,
    double SuitabilityScore);
