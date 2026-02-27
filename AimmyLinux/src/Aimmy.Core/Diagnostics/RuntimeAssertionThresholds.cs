namespace Aimmy.Core.Diagnostics;

public sealed record RuntimeAssertionThresholds(
    double MinimumFps,
    double MaximumCaptureP95Ms,
    double MaximumInferenceP95Ms,
    double MaximumLoopP95Ms);
