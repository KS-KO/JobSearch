using System.Net.Http;
using System.Net.Http.Json;
using JobSearch.Desktop.Models;

namespace JobSearch.Desktop.Services;

public sealed class RecommendationApiService
{
    private readonly HttpClient _httpClient;

    public RecommendationApiService(string baseAddress)
    {
        _httpClient = new HttpClient { BaseAddress = new Uri(baseAddress) };
    }

    public async Task<IReadOnlyList<RecommendationItem>> GetRecommendationsAsync(
        string? ageGroup,
        string? exp,
        string? emp,
        string? region,
        string? industry,
        int? minSalary,
        CancellationToken cancellationToken)
    {
        var url = "api/recommendations/search?";
        if (!string.IsNullOrEmpty(ageGroup)) url += $"ageGroup={ageGroup}&";
        if (!string.IsNullOrEmpty(exp)) url += $"experienceLevel={exp}&";
        if (!string.IsNullOrEmpty(emp)) url += $"employmentType={emp}&";
        if (!string.IsNullOrEmpty(region)) url += $"region={region}&";
        if (!string.IsNullOrEmpty(industry)) url += $"industry={industry}&";
        if (minSalary.HasValue) url += $"minSalaryMillionKrw={minSalary.Value}&";

        var response = await _httpClient.GetFromJsonAsync<RecommendationResponse[]>(url, cancellationToken).ConfigureAwait(false);
        
        if (response == null) return Array.Empty<RecommendationItem>();

        return response.Select(r => new RecommendationItem(
            r.CompanyName,
            r.JobTitle,
            r.JobUrl,
            r.AgeGroup,
            GetAgeGroupLabel(r.AgeGroup),
            r.Platform,
            r.Industry,
            r.ExperienceLevel,
            r.EmploymentType,
            r.Region,
            r.SalaryMillionKrw,
            r.Summary,
            r.SuitabilityScore
        )).ToArray();
    }

    private static string GetAgeGroupLabel(string value) => value switch
    {
        "Twenties" => "20대",
        "Thirties" => "30대",
        "Forties" => "40대",
        "FiftiesAndAbove" => "50대 이상",
        _ => value
    };

    public async Task<DashboardStatsResponse?> GetDashboardStatsAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<DashboardStatsResponse>("api/recommendations/stats", cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }

    private record RecommendationResponse(
        string CompanyName,
        string JobTitle,
        string JobUrl,
        string AgeGroup,
        string Platform,
        string Industry,
        string ExperienceLevel,
        string EmploymentType,
        string Region,
        int SalaryMillionKrw,
        string Summary,
        double SuitabilityScore);
        
    public record DashboardStatsResponse(
        int TotalCount,
        int SaraminCount,
        int JobKoreaCount,
        string LastUpdatedTime);
}
