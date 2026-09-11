using System.Diagnostics;
using DinoCyberScreen.Models;

namespace DinoCyberScreen.Services;

public sealed class ProcessService
{
    private readonly Dictionary<int, (TimeSpan Cpu, DateTime Time)> _previous = [];

    public IReadOnlyList<ProcessTelemetry> Read()
    {
        var now = DateTime.UtcNow;
        var result = new List<ProcessTelemetry>();
        var currentIds = new HashSet<int>();

        foreach (var process in Process.GetProcesses())
        {
            try
            {
                currentIds.Add(process.Id);
                var cpuTime = process.TotalProcessorTime;
                var cpu = 0d;
                if (_previous.TryGetValue(process.Id, out var previous))
                {
                    var elapsed = (now - previous.Time).TotalMilliseconds;
                    if (elapsed > 0)
                        cpu = Math.Clamp((cpuTime - previous.Cpu).TotalMilliseconds / elapsed / Environment.ProcessorCount * 100, 0, 100);
                }
                _previous[process.Id] = (cpuTime, now);
                result.Add(new ProcessTelemetry
                {
                    Id = process.Id,
                    Name = process.ProcessName,
                    Cpu = Math.Round(cpu, 1),
                    Memory = Math.Round(process.WorkingSet64 / 1024d / 1024d, 1)
                });
            }
            catch { }
            finally { process.Dispose(); }
        }

        foreach (var id in _previous.Keys.Where(id => !currentIds.Contains(id)).ToArray())
            _previous.Remove(id);

        return result.OrderByDescending(p => p.Cpu * 4 + p.Memory / 256).Take(8).ToArray();
    }
}
