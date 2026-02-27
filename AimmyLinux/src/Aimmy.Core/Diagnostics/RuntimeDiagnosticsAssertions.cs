namespace Aimmy.Core.Diagnostics;

public static class RuntimeDiagnosticsAssertions
{
    public static IReadOnlyList<string> Evaluate(
        RuntimeSnapshot snapshot,
        RuntimeAssertionThresholds thresholds)
    {
        if (!snapshot.HasData)
        {
            return Array.Empty<string>();
        }

        var warnings = new List<string>(capacity: 4);

        if (thresholds.MinimumFps > 0 && snapshot.Fps < thresholds.MinimumFps)
        {
            warnings.Add($"FPS below threshold ({snapshot.Fps:F1} < {thresholds.MinimumFps:F1}).");
        }

        if (thresholds.MaximumCaptureP95Ms > 0 && snapshot.CaptureP95Ms > thresholds.MaximumCaptureP95Ms)
        {
            warnings.Add($"Capture p95 above threshold ({snapshot.CaptureP95Ms:F2}ms > {thresholds.MaximumCaptureP95Ms:F2}ms).");
        }

        if (thresholds.MaximumInferenceP95Ms > 0 && snapshot.InferenceP95Ms > thresholds.MaximumInferenceP95Ms)
        {
            warnings.Add($"Inference p95 above threshold ({snapshot.InferenceP95Ms:F2}ms > {thresholds.MaximumInferenceP95Ms:F2}ms).");
        }

        if (thresholds.MaximumLoopP95Ms > 0 && snapshot.LoopP95Ms > thresholds.MaximumLoopP95Ms)
        {
            warnings.Add($"Loop p95 above threshold ({snapshot.LoopP95Ms:F2}ms > {thresholds.MaximumLoopP95Ms:F2}ms).");
        }

        return warnings;
    }
}
