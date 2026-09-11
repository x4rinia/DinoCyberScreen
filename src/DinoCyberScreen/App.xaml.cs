using System.Globalization;
using System.Windows;
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
        foreach (var screen in screens)
        {
            var isPrimary = screen.Primary;
            var isBlank = !settings.AllMonitors && !isPrimary;
            
            var window = new ScreensaverWindow(_settingsService, screen.Bounds, isBlank);
            window.ExitRequested += ExitScreensaver;
            _windows.Add(window);
            window.Show();
        }
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
