using System.Diagnostics;

namespace Aimmy.Core.Diagnostics;

public sealed class RuntimeDiagnostics
{
    private readonly Queue<double> _captureMs = new();
    private readonly Queue<double> _inferenceMs = new();
    private readonly Queue<double> _loopMs = new();
    private readonly int _windowSize;
    private readonly Stopwatch _interval = Stopwatch.StartNew();
    private long _frameCount;

    public RuntimeDiagnostics(int windowSize = 240)
    {
        _windowSize = Math.Max(10, windowSize);
    }

    public void AddCaptureMs(double ms) => AddSample(_captureMs, ms);
    public void AddInferenceMs(double ms) => AddSample(_inferenceMs, ms);
    public void AddLoopMs(double ms) => AddSample(_loopMs, ms);

    public void IncrementFrame() => _frameCount++;

    public RuntimeSnapshot SnapshotAndResetIntervalIfNeeded(bool force = false)
    {
        if (!force && _interval.ElapsedMilliseconds < 1000)
        {
            return RuntimeSnapshot.Empty;
        }

        var seconds = Math.Max(0.001, _interval.Elapsed.TotalSeconds);
        var fps = _frameCount / seconds;

        var snapshot = new RuntimeSnapshot(
            fps,
            Percentile(_captureMs, 50),
            Percentile(_captureMs, 95),
            Percentile(_inferenceMs, 50),
            Percentile(_inferenceMs, 95),
            Percentile(_loopMs, 50),
            Percentile(_loopMs, 95));

        _frameCount = 0;
        _interval.Restart();
        return snapshot;
    }

    private void AddSample(Queue<double> queue, double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
        {
            return;
        }

        queue.Enqueue(value);
        while (queue.Count > _windowSize)
        {
            queue.Dequeue();
        }
    }

    private static double Percentile(IEnumerable<double> values, int percentile)
    {
        var sorted = values.OrderBy(v => v).ToArray();
        if (sorted.Length == 0)
        {
            return 0;
        }

        var index = (int)Math.Ceiling((percentile / 100d) * sorted.Length) - 1;
        index = Math.Clamp(index, 0, sorted.Length - 1);
        return sorted[index];
    }
}
