namespace JobSearch.Desktop.Models;

public sealed class UserSettings
{
    public string[] InterestKeywords { get; set; } = Array.Empty<string>();
    public int AutoRefreshMinutes { get; set; } = 0;
    public string? SelectedSearchProfileId { get; set; }
}
