namespace JobSearch.Desktop.Models;

public sealed record SearchProfileOption(
    string Id,
    string DisplayName,
    string[] DefaultKeywords,
    int RecommendedRefreshMinutes);
