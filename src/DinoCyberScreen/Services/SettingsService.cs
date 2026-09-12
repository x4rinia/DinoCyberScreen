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
            Normalize(settings);
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        Normalize(settings);
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
            "rainbow" => "Rainbow",
            _ => "Blue"
        };

    private static string NormalizeDinoSpecimen(string? specimen) =>
        specimen?.Trim().ToLowerInvariant() switch
        {
            "triceratops" => "triceratops",
            "raptor" or "velociraptor" => "raptor",
            "stego" or "stegosaurus" => "stego",
            "ptero" or "pterodactylus" or "pterodax" => "ptero",
            "compy" or "compsognathus" => "compy",
            "special" or "dino-special" or "dino (spezial)" => "special",
            _ => "ankylo"
        };

    private static string NormalizeBackgroundStyle(string? backgroundStyle) =>
        backgroundStyle?.Trim().ToLowerInvariant() switch
        {
            "dark blue tint" => "Dark Blue Tint",
            "dark green tint" => "Dark Green Tint",
            _ => "Pure Black"
        };

    private static void Normalize(AppSettings settings)
    {
        settings.Theme = NormalizeTheme(settings.Theme);
        settings.DinoSpecimen = NormalizeDinoSpecimen(settings.DinoSpecimen);
        settings.BackgroundStyle = NormalizeBackgroundStyle(settings.BackgroundStyle);

        if (settings.DinoSpecimen == "special" && settings.Theme != "Rainbow")
        {
            settings.Theme = "Rainbow";
        }
    }
}
