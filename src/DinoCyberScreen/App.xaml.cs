using System.Globalization;
using System.Linq;
using System.Windows;
using DinoCyberScreen.Controls;
using DinoCyberScreen.Models;
using DinoCyberScreen.Services;
using Forms = System.Windows.Forms;

namespace DinoCyberScreen;

public partial class App : System.Windows.Application
{
    private readonly List<Window> _windows = [];
    private SettingsService? _settingsService;
    private Mutex? _previewMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _settingsService = new SettingsService();
        var parsed = StartupOptions.Parse(e.Args);

        if (parsed.Mode != StartupMode.Configure)
            HudView.WarmUp();

        switch (parsed.Mode)
        {
            case StartupMode.Configure:
                ShowSettingsAndExit();
                break;
            case StartupMode.Preview when parsed.PreviewHandle != IntPtr.Zero:
                ShowPreview(parsed.PreviewHandle);
                break;
            case StartupMode.Screensaver:
                ShowScreensaver();
                break;
            default:
                ShowNormalWindow();
                break;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        HudView.ShutdownSharedTelemetry();
        base.OnExit(e);
    }

    private void ShowNormalWindow()
    {
        var window = new MainWindow(_settingsService!);
        _windows.Add(window);
        MainWindow = window;
        window.Closed += (_, _) => Shutdown();
        window.Show();
    }

    private void ShowSettingsAndExit()
    {
        var dialog = new SettingsWindow(_settingsService!);
        dialog.Closed += (_, _) => Shutdown();
        dialog.Show();
    }

    private void ShowScreensaver()
    {
        var settings = _settingsService!.Load();
        var screens = Forms.Screen.AllScreens;
        var orderedScreens = screens
            .OrderByDescending(s => s.Primary)
            .ThenBy(s => s.Bounds.Left)
            .ToList();

        var starfieldCount = 0;
        for (var i = 0; i < orderedScreens.Count; i++)
        {
            var screen = orderedScreens[i];
            var isPrimary = screen.Primary;
            var displayMode = GetDisplayModeForScreen(settings, screen, i, orderedScreens.Count);
            var isBlank = displayMode == "blank";
            var isSecondary = !isPrimary;

            var isMirrored = false;
            if (displayMode == "starfield")
            {
                starfieldCount++;
                if (starfieldCount > 1 && (starfieldCount % 2 == 0))
                {
                    isMirrored = true;
                }
            }

            var window = new ScreensaverWindow(
                _settingsService,
                screen.Bounds,
                isBlank,
                isSecondary,
                displayMode,
                isPrimary,
                i,
                isMirrored);
            window.ExitRequested += ExitScreensaver;
            _windows.Add(window);
            window.Show();
        }
    }

    private static string GetDisplayModeForScreen(AppSettings settings, Forms.Screen screen, int screenIndex, int totalScreens)
    {
        if (settings.ScreenAssignments != null &&
            settings.ScreenAssignments.TryGetValue(screen.DeviceName, out var modeByDevice) &&
            !string.IsNullOrWhiteSpace(modeByDevice))
        {
            return modeByDevice;
        }

        if (settings.ScreenModes != null && screenIndex < settings.ScreenModes.Count &&
            !string.IsNullOrWhiteSpace(settings.ScreenModes[screenIndex]))
        {
            return settings.ScreenModes[screenIndex];
        }

        // Fallbacks
        if (!settings.AllMonitors)
        {
            return screen.Primary ? "full" : "blank";
        }

        if (totalScreens <= 1) return "full";
        if (screenIndex == 0) return "hud";
        if (screenIndex == 1) return "dino";
        return "starfield";
    }

    private void ShowPreview(IntPtr parentHandle)
    {
        _previewMutex = new Mutex(true, "Local\\DinoCyberScreen.Preview", out var isFirstPreview);
        if (!isFirstPreview)
        {
            _previewMutex.Dispose();
            _previewMutex = null;
            Shutdown();
            return;
        }

        var preview = new ScreensaverWindow(_settingsService!, parentHandle);
        preview.ExitRequested += ExitScreensaver;
        preview.Closed += (_, _) => ReleasePreviewMutex();
        _windows.Add(preview);
        preview.Show();
    }

    private void ReleasePreviewMutex()
    {
        if (_previewMutex is null) return;
        try { _previewMutex.ReleaseMutex(); } catch (ApplicationException) { }
        _previewMutex.Dispose();
        _previewMutex = null;
    }

    private void ExitScreensaver(object? sender, EventArgs e)
    {
        foreach (var window in _windows.ToArray())
            window.Close();
        Shutdown();
    }
}

internal enum StartupMode { Normal, Screensaver, Configure, Preview }

internal sealed record StartupOptions(StartupMode Mode, IntPtr PreviewHandle)
{
    public static StartupOptions Parse(string[] args)
    {
        if (args.Length == 0) return new(StartupMode.Normal, IntPtr.Zero);

        var first = args[0].Trim().ToLowerInvariant();
        var split = first.Split(':', 2);
        var command = split[0];
        var handleText = split.Length == 2 ? split[1] : args.ElementAtOrDefault(1);

        if (command is "/s" or "-s") return new(StartupMode.Screensaver, IntPtr.Zero);
        if (command is "/c" or "-c") return new(StartupMode.Configure, IntPtr.Zero);
        if (command is "/p" or "-p")
        {
            _ = long.TryParse(handleText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var handle);
            return new(StartupMode.Preview, new IntPtr(handle));
        }
        return new(StartupMode.Normal, IntPtr.Zero);
    }
}
