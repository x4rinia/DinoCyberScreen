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
    private System.Drawing.Rectangle _bounds;
    private DispatcherTimer? _previewWatchdog;
    private DispatcherTimer _cursorTimer;
    private bool _closing;

    public event EventHandler? ExitRequested;

    public ScreensaverWindow(SettingsService settingsService, System.Drawing.Rectangle bounds, bool isBlank = false, bool isSecondary = false)
    {
        InitializeComponent();
        _previewParent = IntPtr.Zero;
        WindowState = System.Windows.WindowState.Normal;
        _bounds = bounds;
        this.Cursor = System.Windows.Input.Cursors.None;
        _cursorTimer = new DispatcherTimer(DispatcherPriority.Input) { Interval = TimeSpan.FromSeconds(2.5) };
        _cursorTimer.Tick += (_, _) =>
        {
            this.Cursor = System.Windows.Input.Cursors.None;
            _cursorTimer.Stop();
        };
        
        if (isBlank)
        {
            Hud.Visibility = Visibility.Collapsed;
        }
        else
        {
            Hud.Configure(settingsService, previewMode: false, isSecondary: isSecondary);
        }
        
        AttachInputHandlers();
    }

    public ScreensaverWindow(SettingsService settingsService, IntPtr previewParent)
    {
        InitializeComponent();
        _previewParent = previewParent;
        Topmost = false;
        this.Cursor = System.Windows.Input.Cursors.None;
        _cursorTimer = new DispatcherTimer(DispatcherPriority.Input) { Interval = TimeSpan.FromSeconds(2.5) };
        _cursorTimer.Tick += (_, _) => { this.Cursor = System.Windows.Input.Cursors.None; _cursorTimer.Stop(); };
        Hud.Configure(settingsService, previewMode: true);
    }

    private void AttachInputHandlers()
    {
        Loaded += (_, _) =>
        {
            Focus();
        };
        MouseMove += OnMouseMove;
        MouseDown += (_, _) => RequestExit();
        KeyDown += (_, _) => RequestExit();
        PreviewKeyDown += (_, _) => RequestExit();
        Hud.ExitRequested += (_, _) => RequestExit();
    }

    private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (this.Cursor != System.Windows.Input.Cursors.Arrow)
        {
            this.Cursor = System.Windows.Input.Cursors.Arrow;
        }
        _cursorTimer.Stop();
        _cursorTimer.Start();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        if (_previewParent != IntPtr.Zero)
        {
            InitializePreviewHost();
        }
        else
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            NativeMethods.SetWindowPos(hwnd, IntPtr.Zero, _bounds.Left, _bounds.Top, _bounds.Width, _bounds.Height,
                NativeMethods.SwpNoZOrder | NativeMethods.SwpNoActivate | NativeMethods.SwpShowWindow);
        }
    }

    private void InitializePreviewHost()
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
        if (_previewParent != IntPtr.Zero || _closing) return;
        _closing = true;
        this.Cursor = System.Windows.Input.Cursors.Arrow;
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnClosed(EventArgs e)
    {
        _cursorTimer.Stop();
        this.Cursor = System.Windows.Input.Cursors.Arrow;
        _previewWatchdog?.Stop();
        Hud.Dispose();
        base.OnClosed(e);
    }
}
