using Aimmy.Core.Config;
using Aimmy.Core.Enums;

namespace Aimmy.UI.Avalonia.ViewModels;

public sealed class AimSettingsViewModel
{
    public IReadOnlyList<string> DetectionAreaOptions { get; } = Enum.GetNames<DetectionAreaType>();
    public IReadOnlyList<string> AlignmentOptions { get; } = Enum.GetNames<AimingBoundariesAlignment>();
    public IReadOnlyList<string> MovementPathOptions { get; } = Enum.GetNames<MovementPathStrategy>();

    public bool Enabled { get; set; }
    public bool ConstantTracking { get; set; }
    public bool StickyAimEnabled { get; set; }
    public int StickyAimThreshold { get; set; }
    public bool DynamicFovEnabled { get; set; }
    public bool ThirdPersonSupport { get; set; }
    public bool XAxisPercentageAdjustment { get; set; }
    public bool YAxisPercentageAdjustment { get; set; }
    public double MouseSensitivity { get; set; }
    public int MouseJitter { get; set; }
    public int MaxDeltaPerAxis { get; set; }
    public double XOffset { get; set; }
    public double YOffset { get; set; }
    public double XOffsetPercent { get; set; }
    public double YOffsetPercent { get; set; }
    public string DetectionAreaType { get; set; } = Aimmy.Core.Enums.DetectionAreaType.ClosestToCenterScreen.ToString();
    public string AimingBoundariesAlignment { get; set; } = Aimmy.Core.Enums.AimingBoundariesAlignment.Center.ToString();
    public string MovementPath { get; set; } = MovementPathStrategy.CubicBezier.ToString();

    public void Load(AimmyConfig config)
    {
        Enabled = config.Aim.Enabled;
        ConstantTracking = config.Aim.ConstantTracking;
        StickyAimEnabled = config.Aim.StickyAimEnabled;
        StickyAimThreshold = config.Aim.StickyAimThreshold;
        DynamicFovEnabled = config.Aim.DynamicFovEnabled;
        ThirdPersonSupport = config.Aim.ThirdPersonSupport;
        XAxisPercentageAdjustment = config.Aim.XAxisPercentageAdjustment;
        YAxisPercentageAdjustment = config.Aim.YAxisPercentageAdjustment;
        MouseSensitivity = config.Aim.MouseSensitivity;
        MouseJitter = config.Aim.MouseJitter;
        MaxDeltaPerAxis = config.Aim.MaxDeltaPerAxis;
        XOffset = config.Aim.XOffset;
        YOffset = config.Aim.YOffset;
        XOffsetPercent = config.Aim.XOffsetPercent;
        YOffsetPercent = config.Aim.YOffsetPercent;
        DetectionAreaType = config.Aim.DetectionAreaType.ToString();
        AimingBoundariesAlignment = config.Aim.AimingBoundariesAlignment.ToString();
        MovementPath = config.Aim.MovementPath.ToString();
    }

    public void Apply(AimmyConfig config)
    {
        config.Aim.Enabled = Enabled;
        config.Aim.ConstantTracking = ConstantTracking;
        config.Aim.StickyAimEnabled = StickyAimEnabled;
        config.Aim.StickyAimThreshold = StickyAimThreshold;
        config.Aim.DynamicFovEnabled = DynamicFovEnabled;
        config.Aim.ThirdPersonSupport = ThirdPersonSupport;
        config.Aim.XAxisPercentageAdjustment = XAxisPercentageAdjustment;
        config.Aim.YAxisPercentageAdjustment = YAxisPercentageAdjustment;
        config.Aim.MouseSensitivity = MouseSensitivity;
        config.Aim.MouseJitter = MouseJitter;
        config.Aim.MaxDeltaPerAxis = MaxDeltaPerAxis;
        config.Aim.XOffset = XOffset;
        config.Aim.YOffset = YOffset;
        config.Aim.XOffsetPercent = XOffsetPercent;
        config.Aim.YOffsetPercent = YOffsetPercent;

        if (Enum.TryParse<Aimmy.Core.Enums.DetectionAreaType>(DetectionAreaType, true, out var detectionArea))
        {
            config.Aim.DetectionAreaType = detectionArea;
        }

        if (Enum.TryParse<Aimmy.Core.Enums.AimingBoundariesAlignment>(AimingBoundariesAlignment, true, out var alignment))
        {
            config.Aim.AimingBoundariesAlignment = alignment;
        }

        if (Enum.TryParse<MovementPathStrategy>(MovementPath, true, out var movementPath))
        {
            config.Aim.MovementPath = movementPath;
        }
    }
}
