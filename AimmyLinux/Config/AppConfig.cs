namespace AimmyLinux.Config;

public sealed class AppConfig
{
    public string ModelPath { get; set; } = "models/Universal_Hamsta_v4.onnx";
    public int CaptureWidth { get; set; } = 640;
    public int CaptureHeight { get; set; } = 640;
    public int DisplayWidth { get; set; } = 1920;
    public int DisplayHeight { get; set; } = 1080;
    public float ConfidenceThreshold { get; set; } = 0.45f;
    public double MouseSensitivity { get; set; } = 0.80;
    public int MaxDeltaPerAxis { get; set; } = 150;
    public int Fps { get; set; } = 60;
    public bool AimAssistEnabled { get; set; } = true;
    public bool DryRun { get; set; } = false;
    public string PreferredInputBackend { get; set; } = "xdotool";
    public string PreferredCaptureBackend { get; set; } = "auto";
}
