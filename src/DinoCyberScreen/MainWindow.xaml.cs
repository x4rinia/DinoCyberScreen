using System.Windows;
using DinoCyberScreen.Services;

namespace DinoCyberScreen;

public partial class MainWindow : Window
{
    private readonly SettingsService _settingsService;

    public MainWindow(SettingsService settingsService)
    {
        _settingsService = settingsService;
        InitializeComponent();
        Hud.Configure(settingsService, previewMode: false);
        Hud.SettingsRequested += OpenSettings;
        Closed += (_, _) => Hud.Dispose();
    }

    private void OpenSettings(object? sender, EventArgs e)
    {
        var dialog = new SettingsWindow(_settingsService) { Owner = this };
        if (dialog.ShowDialog() == true)
            Hud.ReloadSettings();
    }
}
