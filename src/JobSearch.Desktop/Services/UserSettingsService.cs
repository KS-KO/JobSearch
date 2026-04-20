using System.IO;
using System.Text.Json;
using JobSearch.Desktop.Models;

namespace JobSearch.Desktop.Services;

public sealed class UserSettingsService
{
    private readonly string _settingsFilePath;

    public UserSettingsService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appFolder = Path.Combine(appData, "JobSearch");
        Directory.CreateDirectory(appFolder);
        
        _settingsFilePath = Path.Combine(appFolder, "settings.json");
    }

    public async Task<UserSettings> LoadAsync()
    {
        if (!File.Exists(_settingsFilePath))
        {
            return new UserSettings();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_settingsFilePath).ConfigureAwait(false);
            var settings = JsonSerializer.Deserialize<UserSettings>(json);
            return settings ?? new UserSettings();
        }
        catch
        {
            return new UserSettings();
        }
    }

    public async Task SaveAsync(UserSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_settingsFilePath, json).ConfigureAwait(false);
        }
        catch
        {
            // Ignore error for now
        }
    }
}
