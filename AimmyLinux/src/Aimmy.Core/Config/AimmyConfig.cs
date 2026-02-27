using Aimmy.Core.Enums;

namespace Aimmy.Core.Config;

public sealed class AimmyConfig
{
    public string ConfigVersion { get; set; } = "3.0";
    public ModelSettings Model { get; set; } = new();
    public CaptureSettings Capture { get; set; } = new();
    public InputSettings Input { get; set; } = new();
    public AimSettings Aim { get; set; } = new();
    public PredictionSettings Prediction { get; set; } = new();
    public TriggerSettings Trigger { get; set; } = new();
    public FovSettings Fov { get; set; } = new();
    public OverlaySettings Overlay { get; set; } = new();
    public RuntimeSettings Runtime { get; set; } = new();
    public DataCollectionSettings DataCollection { get; set; } = new();
    public StoreSettings Store { get; set; } = new();
    public UpdateSettings Update { get; set; } = new();

    public static AimmyConfig CreateDefault() => new();

    public void Normalize()
    {
        Model.ConfidenceThreshold = Math.Clamp(Model.ConfidenceThreshold, 0.01f, 0.99f);
        Model.ImageSize = Math.Clamp(Model.ImageSize, 160, 1280);

        Capture.Width = Math.Clamp(Capture.Width, 64, 2048);
        Capture.Height = Math.Clamp(Capture.Height, 64, 2048);
        Capture.DisplayWidth = Math.Clamp(Capture.DisplayWidth, 640, 8192);
        Capture.DisplayHeight = Math.Clamp(Capture.DisplayHeight, 480, 8192);

        Aim.MouseSensitivity = Math.Clamp(Aim.MouseSensitivity, 0.01, 1.0);
        Aim.MouseJitter = Math.Clamp(Aim.MouseJitter, 0, 20);
        Aim.MaxDeltaPerAxis = Math.Clamp(Aim.MaxDeltaPerAxis, 1, 500);
        Aim.StickyAimThreshold = Math.Clamp(Aim.StickyAimThreshold, 0, 500);

        Prediction.KalmanLeadTime = Math.Clamp(Prediction.KalmanLeadTime, 0.01, 0.5);
        Prediction.WiseTheFoxLeadTime = Math.Clamp(Prediction.WiseTheFoxLeadTime, 0.01, 0.5);
        Prediction.ShalloeLeadMultiplier = Math.Clamp(Prediction.ShalloeLeadMultiplier, 0.5, 20.0);
        Prediction.EmaSmoothingAmount = Math.Clamp(Prediction.EmaSmoothingAmount, 0.01, 1.0);

        Trigger.AutoTriggerDelaySeconds = Math.Clamp(Trigger.AutoTriggerDelaySeconds, 0.01, 1.5);

        Fov.Size = Math.Clamp(Fov.Size, 10, 2048);
        Fov.DynamicSize = Math.Clamp(Fov.DynamicSize, 10, 2048);

        Runtime.Fps = Math.Clamp(Runtime.Fps, 1, 360);
        Runtime.DiagnosticsMinimumFps = Math.Clamp(Runtime.DiagnosticsMinimumFps, 1, 360);
        Runtime.DiagnosticsMaxCaptureP95Ms = Math.Clamp(Runtime.DiagnosticsMaxCaptureP95Ms, 1, 1000);
        Runtime.DiagnosticsMaxInferenceP95Ms = Math.Clamp(Runtime.DiagnosticsMaxInferenceP95Ms, 1, 1000);
        Runtime.DiagnosticsMaxLoopP95Ms = Math.Clamp(Runtime.DiagnosticsMaxLoopP95Ms, 1, 1000);
    }
}

public sealed class ModelSettings
{
    public string ModelPath { get; set; } = "models/Universal_Hamsta_v4.onnx";
    public float ConfidenceThreshold { get; set; } = 0.45f;
    public string TargetClass { get; set; } = "Best Confidence";
    public int ImageSize { get; set; } = 640;
}

public sealed class CaptureSettings
{
    public CaptureMethod Method { get; set; } = CaptureMethod.X11Fallback;
    public string ExternalBackendPreference { get; set; } = "auto";
    public int Width { get; set; } = 640;
    public int Height { get; set; } = 640;
    public int DisplayWidth { get; set; } = 1920;
    public int DisplayHeight { get; set; } = 1080;
}

public sealed class InputSettings
{
    public InputMethod PreferredMethod { get; set; } = InputMethod.UInput;
    public string AimKeybind { get; set; } = "Right";
    public string SecondaryAimKeybind { get; set; } = "LeftAlt";
    public string DynamicFovKeybind { get; set; } = "Left";
    public string EmergencyStopKeybind { get; set; } = "Delete";
    public string ModelSwitchKeybind { get; set; } = "Backslash";
}

public sealed class AimSettings
{
    public bool Enabled { get; set; } = true;
    public bool ConstantTracking { get; set; }
    public bool StickyAimEnabled { get; set; }
    public int StickyAimThreshold { get; set; } = 50;
    public bool DynamicFovEnabled { get; set; }
    public bool ThirdPersonSupport { get; set; }
    public bool XAxisPercentageAdjustment { get; set; }
    public bool YAxisPercentageAdjustment { get; set; }
    public double MouseSensitivity { get; set; } = 0.80;
    public int MouseJitter { get; set; } = 4;
    public int MaxDeltaPerAxis { get; set; } = 150;
    public double XOffset { get; set; }
    public double YOffset { get; set; }
    public double XOffsetPercent { get; set; } = 50;
    public double YOffsetPercent { get; set; } = 50;
    public DetectionAreaType DetectionAreaType { get; set; } = DetectionAreaType.ClosestToCenterScreen;
    public AimingBoundariesAlignment AimingBoundariesAlignment { get; set; } = AimingBoundariesAlignment.Center;
    public MovementPathStrategy MovementPath { get; set; } = MovementPathStrategy.CubicBezier;
}

public sealed class PredictionSettings
{
    public bool Enabled { get; set; }
    public PredictionStrategy Strategy { get; set; } = PredictionStrategy.Kalman;
    public bool EmaSmoothingEnabled { get; set; }
    public double EmaSmoothingAmount { get; set; } = 0.5;
    public double KalmanLeadTime { get; set; } = 0.10;
    public double WiseTheFoxLeadTime { get; set; } = 0.15;
    public double ShalloeLeadMultiplier { get; set; } = 3.0;
}

public sealed class TriggerSettings
{
    public bool Enabled { get; set; }
    public bool SprayMode { get; set; }
    public bool CursorCheck { get; set; }
    public double AutoTriggerDelaySeconds { get; set; } = 0.10;
}

public sealed class FovSettings
{
    public bool Enabled { get; set; }
    public bool ShowFov { get; set; } = true;
    public int Size { get; set; } = 640;
    public int DynamicSize { get; set; } = 200;
    public string Style { get; set; } = "Circle";
    public string Color { get; set; } = "#FF8080FF";
}

public sealed class OverlaySettings
{
    public bool ShowDetectedPlayer { get; set; }
    public bool ShowConfidence { get; set; }
    public bool ShowTracers { get; set; }
    public string TracerPosition { get; set; } = "Bottom";
    public double Opacity { get; set; } = 1.0;
}

public sealed class RuntimeSettings
{
    public int Fps { get; set; } = 120;
    public bool DryRun { get; set; } = true;
    public bool DebugMode { get; set; }
    public GpuExecutionMode GpuMode { get; set; } = GpuExecutionMode.Auto;
    public bool EnableDiagnosticsAssertions { get; set; } = true;
    public int DiagnosticsMinimumFps { get; set; } = 60;
    public int DiagnosticsMaxCaptureP95Ms { get; set; } = 20;
    public int DiagnosticsMaxInferenceP95Ms { get; set; } = 25;
    public int DiagnosticsMaxLoopP95Ms { get; set; } = 35;
}

public sealed class DataCollectionSettings
{
    public bool CollectDataWhilePlaying { get; set; }
    public bool AutoLabelData { get; set; }
    public string ImagesDirectory { get; set; } = "bin/images";
    public string LabelsDirectory { get; set; } = "bin/labels";
}

public sealed class StoreSettings
{
    public bool Enabled { get; set; } = true;
    public string ModelsApiUrl { get; set; } = "https://api.github.com/repos/Babyhamsta/Aimmy/contents/models";
    public string ConfigsApiUrl { get; set; } = "https://api.github.com/repos/Babyhamsta/Aimmy/contents/configs";
    public string LocalModelsDirectory { get; set; } = "bin/models";
    public string LocalConfigsDirectory { get; set; } = "bin/configs";
}

public sealed class UpdateSettings
{
    public bool Enabled { get; set; } = true;
    public string Channel { get; set; } = "stable";
    public string PackageType { get; set; } = "deb";
    public string ReleasesApiUrl { get; set; } = "https://api.github.com/repos/Babyhamsta/Aimmy/releases/latest";
}
