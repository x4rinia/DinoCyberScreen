using System.Text.Json;
using System.IO;
using DinoCyberScreen.Models;

namespace DinoCyberScreen.Services;

public sealed class SettingsService
{
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public SettingsService()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DinoCyberScreen");
        Directory.CreateDirectory(directory);
        _settingsPath = Path.Combine(directory, "settings.json");
    }

    public string SettingsPath => _settingsPath;

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath)) return new AppSettings();
            var settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_settingsPath), _jsonOptions)
                           ?? new AppSettings();
            settings.Theme = NormalizeTheme(settings.Theme);
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        settings.Theme = NormalizeTheme(settings.Theme);
        var temporaryPath = _settingsPath + ".tmp";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, _jsonOptions));
        File.Move(temporaryPath, _settingsPath, true);
    }

    private static string NormalizeTheme(string? theme) =>
        theme?.Trim().ToLowerInvariant() switch
        {
            "green" => "Green",
            "red" => "Red",
            "white" => "White",
            "pink" or "rosa" => "Pink",
            _ => "Blue"
        };
}
