using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using DinoCyberScreen.Interop;
using DinoCyberScreen.Services;
using Forms = System.Windows.Forms;

namespace DinoCyberScreen;

public partial class ScreensaverWindow : Window
{
    private readonly IntPtr _previewParent;
    private DateTime _inputEnabledAt = DateTime.UtcNow.AddSeconds(2.5);
    private System.Windows.Point? _initialMousePosition;
    private DispatcherTimer? _previewWatchdog;
    private bool _closing;

    public event EventHandler? ExitRequested;

    public ScreensaverWindow(SettingsService settingsService, System.Drawing.Rectangle bounds)
    {
        InitializeComponent();
        _previewParent = IntPtr.Zero;
        WindowState = System.Windows.WindowState.Normal;
        Left = bounds.Left;
        Top = bounds.Top;
        Width = bounds.Width;
        Height = bounds.Height;
        Hud.Configure(settingsService, previewMode: false);
        AttachInputHandlers();
    }

    public ScreensaverWindow(SettingsService settingsService, IntPtr previewParent)
    {
        InitializeComponent();
        _previewParent = previewParent;
        Topmost = false;
        Hud.Configure(settingsService, previewMode: true);
        SourceInitialized += InitializePreviewHost;
    }

    private void AttachInputHandlers()
    {
        Loaded += (_, _) =>
        {
            _inputEnabledAt = DateTime.UtcNow.AddSeconds(2.0);
            _initialMousePosition = Mouse.GetPosition(this);
            Mouse.OverrideCursor = System.Windows.Input.Cursors.None;
            Focus();
        };
        MouseMove += OnMouseMove;
        MouseDown += (_, _) => RequestExit();
        KeyDown += (_, _) => RequestExit();
        PreviewKeyDown += (_, _) => RequestExit();
    }

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (DateTime.UtcNow < _inputEnabledAt)
        {
            // During grace period: keep refreshing origin so that movement BEFORE
            // the window is fully visible does not count as a trigger gesture.
            _initialMousePosition = e.GetPosition(this);
            return;
        }
        var current = e.GetPosition(this);
        if (_initialMousePosition is { } origin &&
            Math.Abs(current.X - origin.X) + Math.Abs(current.Y - origin.Y) > 30)
            RequestExit();
    }

    private void InitializePreviewHost(object? sender, EventArgs e)
    {
        if (_previewParent == IntPtr.Zero) return;
        var source = (HwndSource)PresentationSource.FromVisual(this)!;
        NativeMethods.SetParent(source.Handle, _previewParent);
        var style = NativeMethods.GetWindowLongPtr(source.Handle, NativeMethods.GwlStyle).ToInt64();
        NativeMethods.SetWindowLongPtr(source.Handle, NativeMethods.GwlStyle,
            new IntPtr((style | NativeMethods.WsChild) & ~NativeMethods.WsPopup));
        if (NativeMethods.GetClientRect(_previewParent, out var rect))
            NativeMethods.SetWindowPos(source.Handle, IntPtr.Zero, 0, 0, rect.Right, rect.Bottom,
                NativeMethods.SwpNoZOrder | NativeMethods.SwpNoActivate | NativeMethods.SwpShowWindow);

        _previewWatchdog = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _previewWatchdog.Tick += PreviewWatchdogTick;
        _previewWatchdog.Start();
    }

    private void PreviewWatchdogTick(object? sender, EventArgs e)
    {
        if (_previewParent != IntPtr.Zero && !NativeMethods.IsWindow(_previewParent))
        {
            _previewWatchdog?.Stop();
            ExitRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    private void RequestExit()
    {
        if (_previewParent != IntPtr.Zero || _closing || DateTime.UtcNow < _inputEnabledAt) return;
        _closing = true;
        Mouse.OverrideCursor = null;
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnClosed(EventArgs e)
    {
        Mouse.OverrideCursor = null;
        _previewWatchdog?.Stop();
        Hud.Dispose();
        base.OnClosed(e);
    }
}
