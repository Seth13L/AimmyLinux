using Aimmy.Core.Diagnostics;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class RuntimeDiagnosticsAssertionsTests
{
    [Fact]
    public void Evaluate_ReturnsWarnings_WhenThresholdsAreViolated()
    {
        var snapshot = new RuntimeSnapshot(
            Fps: 40,
            CaptureP50Ms: 12,
            CaptureP95Ms: 30,
            InferenceP50Ms: 14,
            InferenceP95Ms: 35,
            LoopP50Ms: 20,
            LoopP95Ms: 50);

        var thresholds = new RuntimeAssertionThresholds(
            MinimumFps: 60,
            MaximumCaptureP95Ms: 20,
            MaximumInferenceP95Ms: 25,
            MaximumLoopP95Ms: 35);

        var warnings = RuntimeDiagnosticsAssertions.Evaluate(snapshot, thresholds);

        Assert.Equal(4, warnings.Count);
    }

    [Fact]
    public void Evaluate_ReturnsNoWarnings_WhenSnapshotIsWithinThresholds()
    {
        var snapshot = new RuntimeSnapshot(
            Fps: 120,
            CaptureP50Ms: 4,
            CaptureP95Ms: 9,
            InferenceP50Ms: 5,
            InferenceP95Ms: 12,
            LoopP50Ms: 10,
            LoopP95Ms: 15);

        var thresholds = new RuntimeAssertionThresholds(
            MinimumFps: 60,
            MaximumCaptureP95Ms: 20,
            MaximumInferenceP95Ms: 25,
            MaximumLoopP95Ms: 35);

        var warnings = RuntimeDiagnosticsAssertions.Evaluate(snapshot, thresholds);

        Assert.Empty(warnings);
    }

    [Fact]
    public void Evaluate_IgnoresDisabledThresholds()
    {
        var snapshot = new RuntimeSnapshot(
            Fps: 1,
            CaptureP50Ms: 1,
            CaptureP95Ms: 999,
            InferenceP50Ms: 1,
            InferenceP95Ms: 999,
            LoopP50Ms: 1,
            LoopP95Ms: 999);

        var thresholds = new RuntimeAssertionThresholds(
            MinimumFps: 0,
            MaximumCaptureP95Ms: 0,
            MaximumInferenceP95Ms: 0,
            MaximumLoopP95Ms: 0);

        var warnings = RuntimeDiagnosticsAssertions.Evaluate(snapshot, thresholds);

        Assert.Empty(warnings);
    }
}
