namespace JobSearch.Contracts.Responses;

public sealed record RecommendationItemResponse(
    string CompanyName,
    string JobTitle,
    string AgeGroup,
    string Platform,
    string Industry,
    string ExperienceLevel,
    string EmploymentType,
    string Region,
    int SalaryMillionKrw,
    string Summary,
    double SuitabilityScore);
