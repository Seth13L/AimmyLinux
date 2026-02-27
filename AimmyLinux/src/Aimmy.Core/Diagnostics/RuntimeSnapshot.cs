namespace Aimmy.Core.Diagnostics;

public readonly record struct RuntimeSnapshot(
    double Fps,
    double CaptureP50Ms,
    double CaptureP95Ms,
    double InferenceP50Ms,
    double InferenceP95Ms,
    double LoopP50Ms,
    double LoopP95Ms)
{
    public static RuntimeSnapshot Empty => new(0, 0, 0, 0, 0, 0, 0);
    public bool HasData => Fps > 0;
}
