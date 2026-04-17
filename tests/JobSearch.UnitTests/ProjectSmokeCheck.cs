using JobSearch.Domain.Enums;
using JobSearch.Domain.Models;

namespace JobSearch.UnitTests;

public sealed class ProjectSmokeCheck
{
    public static CompanyRecommendation CreateSample()
    {
        return new CompanyRecommendation(
            "Hanul Tech",
            "Backend Developer",
            "https://www.saramin.co.kr",
            AgeGroup.Twenties,
            "Saramin",
            "IT/Development",
            "Entry",
            "FullTime",
            "Seoul",
            38,
            "Sample recommendation model for future test coverage.",
            0.9);
    }
}
