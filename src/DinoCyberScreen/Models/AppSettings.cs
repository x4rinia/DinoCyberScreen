namespace DinoCyberScreen.Models;

public sealed class AppSettings
{
    public bool ShowRealData { get; set; } = true;
    public bool ShowGpuData { get; set; } = true;
    public bool ShowNetworkData { get; set; } = true;
    public bool ShowTerminal { get; set; } = true;
    public bool ShowHexStream { get; set; } = true;
    public bool EnableEvents { get; set; } = true;
    public bool ShowDinoCore { get; set; } = true;
    public bool AllMonitors { get; set; } = true;
    public int TargetFps { get; set; } = 60;
    public string AnimationQuality { get; set; } = "High";
    public int ModeIntervalSeconds { get; set; } = 120;
    public string Theme { get; set; } = "Blue";
    public string BackgroundStyle { get; set; } = "Pure Black";
}
