using System.Windows;
using System.Windows.Controls;
using DinoCyberScreen.Models;
using DinoCyberScreen.Services;

namespace DinoCyberScreen;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settingsService;
    private bool _updatingSelections;

    public SettingsWindow(SettingsService settingsService)
    {
        _settingsService = settingsService;
        InitializeComponent();
        LoadSettings(settingsService.Load());
        DinoCombo.SelectionChanged += DinoSelectionChanged;
        InitializeRainbowState();
        PathText.Text = settingsService.SettingsPath;
    }

    private void LoadSettings(AppSettings settings)
    {
        RealDataCheck.IsChecked = settings.ShowRealData;
        EventsCheck.IsChecked = settings.EnableEvents;
        CustomNameText.Text = string.IsNullOrWhiteSpace(settings.CustomName) ? "DINO" : settings.CustomName;
        Select(MonitorCombo, settings.AllMonitors ? "all" : "primary", useTag: true);
        Select(FpsCombo, settings.TargetFps.ToString(), useTag: false);
        Select(QualityCombo, settings.AnimationQuality, useTag: false);
        Select(DinoCombo, settings.DinoSpecimen, useTag: true);
        Select(ThemeCombo, settings.Theme, useTag: false);
        EnergySavingCheck.IsChecked = settings.EnergySavingMode;
    }

    private void InitializeRainbowState()
    {
        var specimen = Selected(DinoCombo, true);
        if (specimen == "special")
        {
            SetSelection(ThemeCombo, "Rainbow", false);
            ThemeCombo.IsEnabled = false;
        }
        else
        {
            ThemeCombo.IsEnabled = true;
        }
    }

    private void DinoSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingSelections) return;
        var specimen = Selected(DinoCombo, true);
        
        if (specimen == "special")
        {
            SetSelection(ThemeCombo, "Rainbow", false);
            ThemeCombo.IsEnabled = false;
        }
        else
        {
            ThemeCombo.IsEnabled = true;
        }
    }

    private void SetSelection(System.Windows.Controls.ComboBox combo, string value, bool useTag)
    {
        _updatingSelections = true;
        try { Select(combo, value, useTag); }
        finally { _updatingSelections = false; }
    }

    private void SaveClick(object sender, RoutedEventArgs e)
    {
        var name = CustomNameText.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            name = "DINO";
            CustomNameText.Text = "DINO";
        }

        _settingsService.Save(new AppSettings
        {
            ShowRealData = RealDataCheck.IsChecked == true,
            EnableEvents = EventsCheck.IsChecked == true,
            CustomName = name,
            AllMonitors = Selected(MonitorCombo, true) == "all",
            TargetFps = int.TryParse(Selected(FpsCombo, false), out var fps) ? fps : 60,
            AnimationQuality = Selected(QualityCombo, false),
            DinoSpecimen = Selected(DinoCombo, true),
            Theme = Selected(ThemeCombo, false),
            EnergySavingMode = EnergySavingCheck.IsChecked == true
        });
        if (Owner is not null) DialogResult = true;
        else Close();
    }

    private void CancelClick(object sender, RoutedEventArgs e)
    {
        if (Owner is not null) DialogResult = false;
        else Close();
    }

    private static void Select(System.Windows.Controls.ComboBox combo, string value, bool useTag)
    {
        foreach (var item in combo.Items.OfType<ComboBoxItem>())
        {
            var candidate = useTag ? item.Tag?.ToString() : item.Content?.ToString();
            if (string.Equals(candidate, value, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedItem = item;
                return;
            }
        }
        combo.SelectedIndex = 0;
    }

    private static string Selected(System.Windows.Controls.ComboBox combo, bool useTag)
    {
        var item = combo.SelectedItem as ComboBoxItem;
        return (useTag ? item?.Tag : item?.Content)?.ToString() ?? string.Empty;
    }
}
