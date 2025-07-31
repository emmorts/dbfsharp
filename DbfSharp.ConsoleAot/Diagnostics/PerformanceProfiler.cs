using System.Diagnostics;

namespace DbfSharp.ConsoleAot.Diagnostics;

/// <summary>
/// Monitors performance metrics during operations
/// </summary>
public sealed class PerformanceProfiler(string operationName) : IDisposable
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly long _initialMemory = GC.GetTotalMemory(false);
    private readonly Process _process = Process.GetCurrentProcess();
    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _stopwatch.Stop();
        var finalMemory = GC.GetTotalMemory(false);
        var memoryDelta = finalMemory - _initialMemory;

        Console.WriteLine($"\nPerformance Summary for {operationName}:");
        Console.WriteLine($@"  Elapsed Time: {_stopwatch.Elapsed:mm\:ss\.fff}");
        Console.WriteLine($"  Memory Used: {memoryDelta / (1024 * 1024):F1} MB");
        Console.WriteLine($"  Peak Working Set: {_process.PeakWorkingSet64 / (1024 * 1024):F1} MB");
    }
}
