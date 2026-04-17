using System.Text.Json;
using JobSearch.Domain.Models;
using JobSearch.Domain.Enums;

namespace JobSearch.Api.Services;

public sealed class RecommendationFileRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly string _seedFilePath;

    public RecommendationFileRepository(string seedFilePath)
    {
        _seedFilePath = seedFilePath;
    }

    public async Task<IReadOnlyList<CompanyRecommendation>> GetRecommendationsAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_seedFilePath))
        {
            throw new FileNotFoundException("Recommendation seed file was not found.", _seedFilePath);
        }

        var json = await File
            .ReadAllTextAsync(_seedFilePath, cancellationToken)
            .ConfigureAwait(false);

        var rawItems = JsonSerializer.Deserialize<List<RecommendationRecord>>(json, SerializerOptions);

        if (rawItems is null || rawItems.Count == 0)
        {
            return Array.Empty<CompanyRecommendation>();
        }

        return rawItems
            .Select(item => new CompanyRecommendation(
                item.CompanyName,
                item.JobTitle,
                item.JobUrl,
                ParseAgeGroup(item.AgeGroup),
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

    private static AgeGroup ParseAgeGroup(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "twenties" => AgeGroup.Twenties,
            "thirties" => AgeGroup.Thirties,
            "forties" => AgeGroup.Forties,
            "fiftiesandabove" => AgeGroup.FiftiesAndAbove,
            "fifties_and_above" => AgeGroup.FiftiesAndAbove,
            "50s+" => AgeGroup.FiftiesAndAbove,
            _ => throw new InvalidOperationException($"Unsupported age group value: {value}")
        };

    private sealed class RecommendationRecord
    {
        public string CompanyName { get; init; } = string.Empty;

        public string JobTitle { get; init; } = string.Empty;
        public string JobUrl { get; init; } = string.Empty;
        public string AgeGroup { get; init; } = string.Empty;

        public string Platform { get; init; } = string.Empty;

        public string Industry { get; init; } = string.Empty;

        public string ExperienceLevel { get; init; } = string.Empty;

        public string EmploymentType { get; init; } = string.Empty;

        public string Region { get; init; } = string.Empty;

        public int SalaryMillionKrw { get; init; }

        public string Summary { get; init; } = string.Empty;

        public double SuitabilityScore { get; init; }
    }
}
