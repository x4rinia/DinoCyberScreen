using LibreHardwareMonitor.Hardware;

namespace DinoCyberScreen.Services;

public sealed class HardwareMonitorService : IDisposable
{
    private readonly Computer _computer;
    private bool _available;

    public HardwareMonitorService()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsStorageEnabled = true,
            IsMotherboardEnabled = true,
            IsControllerEnabled = true
        };
        try
        {
            _computer.Open();
            _available = true;
        }
        catch
        {
            _available = false;
        }
    }

    public IReadOnlyList<IHardware> Snapshot()
    {
        if (!_available) return [];
        try
        {
            foreach (var hardware in _computer.Hardware)
                UpdateRecursive(hardware);
            return _computer.Hardware.ToArray();
        }
        catch
        {
            return [];
        }
    }

    private static void UpdateRecursive(IHardware hardware)
    {
        hardware.Update();
        foreach (var subHardware in hardware.SubHardware)
            UpdateRecursive(subHardware);
    }

    public void Dispose()
    {
        if (!_available) return;
        try { _computer.Close(); } catch { }
        _available = false;
    }
}
