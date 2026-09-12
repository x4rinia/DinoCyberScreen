using System.Text.Json;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using DinoCyberScreen.Models;
using DinoCyberScreen.Services;
using Microsoft.Web.WebView2.Core;

namespace DinoCyberScreen.Controls;

public partial class HudView : System.Windows.Controls.UserControl, IDisposable
{
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);
    private readonly DispatcherTimer _timer;
    private TelemetryService? _telemetry;
    private SettingsService? _settingsService;
    private Task<TelemetryService>? _telemetryTask;
    private AppSettings _settings = new();
    private bool _previewMode;
    private bool _isSecondary;
    private bool _ready;
    private bool _disposed;

    public event EventHandler? SettingsRequested;
    public event EventHandler? ExitRequested;

    public HudView()
    {
        InitializeComponent();
        _timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(750) };
        _timer.Tick += SendTelemetry;
        Loaded += Initialize;
    }

    public void Configure(SettingsService settingsService, bool previewMode, bool isSecondary = false)
    {
        _settingsService = settingsService;
        _previewMode = previewMode;
        _isSecondary = isSecondary;
        _settings = settingsService.Load();
        _timer.Interval = _settings.EnergySavingMode ? TimeSpan.FromMilliseconds(2000) : TimeSpan.FromMilliseconds(750);
    }

    public void ReloadSettings()
    {
        if (_settingsService is null) return;
        _settings = _settingsService.Load();
        SendEnvelope("settings", _settings);
    }

    private async void Initialize(object sender, RoutedEventArgs e)
    {
        if (_ready || _disposed) return;
        if (_previewMode)
        {
            try
            {
                var previewPath = Path.Combine(AppContext.BaseDirectory, "Web", "assets", "preview.png");
                if (File.Exists(previewPath))
                {
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage(new Uri(previewPath, UriKind.Absolute));
                    PreviewImage.Source = bitmap;
                    PreviewImage.Visibility = Visibility.Visible;
                }
            }
            catch { }
        }
        try
        {
            _telemetryTask = Task.Run(() => new TelemetryService());
            var userDataFolder = Path.Combine(Path.GetTempPath(), "DinoCyberScreen", Guid.NewGuid().ToString());
            Directory.CreateDirectory(userDataFolder);
            var webViewEnvironment = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
            await Browser.EnsureCoreWebView2Async(webViewEnvironment);
            Browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            Browser.CoreWebView2.Settings.AreDevToolsEnabled = !_previewMode;
            Browser.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = !_previewMode;
            Browser.CoreWebView2.Settings.IsStatusBarEnabled = false;
            Browser.CoreWebView2.WebMessageReceived += OnWebMessage;
            Browser.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
            Browser.CoreWebView2.ProcessFailed += (s, ev) => 
            {
                Dispatcher.Invoke(() => {
                    Fallback.Visibility = Visibility.Visible;
                    FallbackMessage.Text = $"WEBVIEW CRASH: {ev.ProcessFailedKind}\nIf this happens frequently, disable mode switching in settings.";
                });
            };

            var webRoot = Path.Combine(AppContext.BaseDirectory, "Web");
            if (!Directory.Exists(webRoot)) throw new DirectoryNotFoundException($"Web assets not found: {webRoot}");
            Browser.CoreWebView2.SetVirtualHostNameToFolderMapping(
                "dinocore.local", webRoot, CoreWebView2HostResourceAccessKind.DenyCors);
            
            var isSingleMonitor = System.Windows.Forms.Screen.AllScreens.Length == 1 || !_settings.AllMonitors;
            var url = "https://dinocore.local/index.html";
            if (_isSecondary) url += "?secondary=1";
            else if (isSingleMonitor) url += "?singleMonitor=1";
            else url += "?primaryMulti=1";
            Browser.Source = new Uri(url);
        }
        catch (Exception ex)
        {
            if (!_previewMode)
            {
                Fallback.Visibility = Visibility.Visible;
                FallbackMessage.Text = "Microsoft Edge WebView2 Runtime konnte nicht initialisiert werden.\n" + ex.Message;
            }
        }
    }

    private async void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (!e.IsSuccess) return;
        _ready = true;
        SendEnvelope("settings", _settings);
        if (_telemetryTask is null) return;
        try { _telemetry = await _telemetryTask; } catch { }
        SendTelemetry(this, EventArgs.Empty);
        _timer.Start();
    }

    private void SendTelemetry(object? sender, EventArgs e)
    {
        if (!_ready || _telemetry is null || _disposed) return;
        try { SendEnvelope("telemetry", _telemetry.Read(_settings)); }
        catch { /* A failed sensor sample must never stop the renderer. */ }
    }

    private void SendEnvelope<T>(string type, T payload)
    {
        if (!_ready || Browser.CoreWebView2 is null) return;
        var message = JsonSerializer.Serialize(new { type, payload }, _json);
        Browser.CoreWebView2.PostWebMessageAsJson(message);
    }

    private void OnWebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            using var document = JsonDocument.Parse(e.WebMessageAsJson);
            var command = document.RootElement.TryGetProperty("command", out var value) ? value.GetString() : null;
            if (command == "openSettings" && !_previewMode)
                SettingsRequested?.Invoke(this, EventArgs.Empty);
            else if (command == "exit")
                ExitRequested?.Invoke(this, EventArgs.Empty);
        }
        catch { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _telemetry?.Dispose();
        if (Browser.CoreWebView2 is not null)
        {
            Browser.CoreWebView2.WebMessageReceived -= OnWebMessage;
            Browser.CoreWebView2.NavigationCompleted -= OnNavigationCompleted;
        }
        Browser.Dispose();
    }
}
