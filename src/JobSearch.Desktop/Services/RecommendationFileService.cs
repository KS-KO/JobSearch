using System.IO;
using System.Text.Json;
using JobSearch.Desktop.Models;

namespace JobSearch.Desktop.Services;

public sealed class RecommendationFileService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly string _filePath;

    public RecommendationFileService(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<IReadOnlyList<RecommendationItem>> GetRecommendationsAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            throw new FileNotFoundException("Desktop recommendation data file was not found.", _filePath);
        }

        var json = await File
            .ReadAllTextAsync(_filePath, cancellationToken)
            .ConfigureAwait(false);

        var items = JsonSerializer.Deserialize<List<RecommendationRecord>>(json, SerializerOptions);

        if (items is null || items.Count == 0)
        {
            return Array.Empty<RecommendationItem>();
        }

        return items.Select(item => new RecommendationItem(
                item.CompanyName,
                item.JobTitle,
                item.JobUrl,
                item.AgeGroup,
                MapAgeGroupLabel(item.AgeGroup),
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

    private static string MapAgeGroupLabel(string value) =>
        value.Trim().ToLowerInvariant() switch
        {
            "twenties" => "20대",
            "thirties" => "30대",
            "forties" => "40대",
            "fiftiesandabove" => "50대 이상",
            _ => value
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
