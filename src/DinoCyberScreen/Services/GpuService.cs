using DinoCyberScreen.Models;
using LibreHardwareMonitor.Hardware;

namespace DinoCyberScreen.Services;

public sealed class GpuService
{
    public GpuTelemetry Read(IReadOnlyList<IHardware> hardware)
    {
        var gpu = hardware.FirstOrDefault(h => h.HardwareType is HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel);
        if (gpu is null) return new GpuTelemetry();

        var load = Find(gpu, SensorType.Load, "GPU Core") ??
                   gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Load)?.Value;
        var temperature = Find(gpu, SensorType.Temperature, "GPU Core") ??
                          gpu.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature)?.Value;
        var usedMb = Find(gpu, SensorType.SmallData, "GPU Memory Used");
        var totalMb = Find(gpu, SensorType.SmallData, "GPU Memory Total");

        if (!totalMb.HasValue)
        {
            var freeMb = Find(gpu, SensorType.SmallData, "GPU Memory Free");
            if (usedMb.HasValue && freeMb.HasValue) totalMb = usedMb + freeMb;
        }

        return new GpuTelemetry
        {
            Name = gpu.Name,
            Usage = Round(load),
            Temperature = Round(temperature),
            VramUsed = usedMb.HasValue ? Math.Round(usedMb.Value / 1024d, 2) : null,
            VramTotal = totalMb.HasValue ? Math.Round(totalMb.Value / 1024d, 2) : null
        };
    }

    private static float? Find(IHardware hardware, SensorType type, string name) =>
        hardware.Sensors.FirstOrDefault(s => s.SensorType == type && s.Name.Contains(name, StringComparison.OrdinalIgnoreCase))?.Value;

    private static double? Round(float? value) => value.HasValue ? Math.Round(value.Value, 1) : null;
}
