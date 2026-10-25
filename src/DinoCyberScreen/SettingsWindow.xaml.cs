using System.Windows;
using System.Windows.Controls;
using DinoCyberScreen.Models;
using DinoCyberScreen.Services;
using Forms = System.Windows.Forms;
using WpfComboBox = System.Windows.Controls.ComboBox;

namespace DinoCyberScreen;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settingsService;
    private readonly List<(Forms.Screen Screen, WpfComboBox ComboBox)> _screenControls = new();
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
        
        LoadScreenControls(settings);

        Select(FpsCombo, settings.TargetFps.ToString(), useTag: false);
        Select(QualityCombo, settings.AnimationQuality, useTag: false);
        Select(DinoCombo, settings.DinoSpecimen, useTag: true);
        Select(ThemeCombo, settings.Theme, useTag: false);
        EnergySavingCheck.IsChecked = settings.EnergySavingMode;
    }

    private void LoadScreenControls(AppSettings settings)
    {
        _screenControls.Clear();
        ScreensPanel.Children.Clear();

        var screens = Forms.Screen.AllScreens;
        var orderedScreens = screens
            .OrderByDescending(s => s.Primary)
            .ThenBy(s => s.Bounds.Left)
            .ToList();

        MonitorGroupBox.Header = orderedScreens.Count == 1
            ? "MONITOR-KONFIGURATION (1 Monitor erkannt)"
            : $"MONITOR-KONFIGURATION ({orderedScreens.Count} Monitore erkannt)";

        var primaryScreen = orderedScreens.FirstOrDefault(s => s.Primary) ?? orderedScreens.FirstOrDefault();

        for (var i = 0; i < orderedScreens.Count; i++)
        {
            var screen = orderedScreens[i];
            var isPrimary = screen.Primary;
            var num = i + 1;

            var title = isPrimary
                ? $"SCREEN {num} (HAUPTMONITOR) — {screen.Bounds.Width}x{screen.Bounds.Height}"
                : $"SCREEN {num} — {screen.Bounds.Width}x{screen.Bounds.Height}";

            if (!isPrimary && primaryScreen != null)
            {
                var rel = "";
                if (screen.Bounds.X < primaryScreen.Bounds.X) rel = "Links";
                else if (screen.Bounds.X > primaryScreen.Bounds.X) rel = "Rechts";
                else if (screen.Bounds.Y < primaryScreen.Bounds.Y) rel = "Oben";
                else if (screen.Bounds.Y > primaryScreen.Bounds.Y) rel = "Unten";

                if (!string.IsNullOrEmpty(rel))
                    title += $" ({rel})";
            }

            var label = new TextBlock
            {
                Text = title,
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(120, 149, 164)),
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, i == 0 ? 0 : 8, 0, 3)
            };

            var combo = new WpfComboBox
            {
                Margin = new Thickness(0, 0, 0, 4)
            };

            combo.Items.Add(new ComboBoxItem { Content = "Komplett-HUD (mit Dino)", Tag = "full" });
            combo.Items.Add(new ComboBoxItem { Content = "Haupt-HUD (ohne Dino)", Tag = "hud" });
            combo.Items.Add(new ComboBoxItem { Content = "Dino-Hologramm", Tag = "dino" });
            combo.Items.Add(new ComboBoxItem { Content = "Starfield (Sterne, Kometen & Planeten)", Tag = "starfield" });
            combo.Items.Add(new ComboBoxItem { Content = "Schwarz (Inaktiv / Deaktiviert)", Tag = "blank" });

            string mode = "";
            if (settings.ScreenAssignments != null &&
                settings.ScreenAssignments.TryGetValue(screen.DeviceName, out var assigned) &&
                !string.IsNullOrWhiteSpace(assigned))
            {
                mode = assigned;
            }
            else if (settings.ScreenModes != null && i < settings.ScreenModes.Count &&
                     !string.IsNullOrWhiteSpace(settings.ScreenModes[i]))
            {
                mode = settings.ScreenModes[i];
            }
            else
            {
                if (orderedScreens.Count == 1) mode = "full";
                else if (i == 0) mode = "hud";
                else if (i == 1) mode = "dino";
                else mode = "starfield";
            }

            Select(combo, mode, useTag: true);

            _screenControls.Add((screen, combo));
            ScreensPanel.Children.Add(label);
            ScreensPanel.Children.Add(combo);
        }
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

        var screenModes = new List<string>();
        var screenAssignments = new Dictionary<string, string>();
        var anyActive = false;

        foreach (var (screen, combo) in _screenControls)
        {
            var mode = Selected(combo, useTag: true);
            if (string.IsNullOrWhiteSpace(mode)) mode = "full";
            screenModes.Add(mode);
            screenAssignments[screen.DeviceName] = mode;
            if (mode != "blank") anyActive = true;
        }

        _settingsService.Save(new AppSettings
        {
            ShowRealData = RealDataCheck.IsChecked == true,
            EnableEvents = EventsCheck.IsChecked == true,
            CustomName = name,
            AllMonitors = anyActive,
            ScreenModes = screenModes,
            ScreenAssignments = screenAssignments,
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
