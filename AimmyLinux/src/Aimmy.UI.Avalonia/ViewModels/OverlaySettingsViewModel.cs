using Aimmy.Core.Config;

namespace Aimmy.UI.Avalonia.ViewModels;

public sealed class OverlaySettingsViewModel
{
    public bool ShowDetectedPlayer { get; set; }
    public bool ShowConfidence { get; set; }
    public bool ShowTracers { get; set; }
    public string TracerPosition { get; set; } = string.Empty;
    public double Opacity { get; set; }
    public string DetectedPlayerColor { get; set; } = string.Empty;
    public int ConfidenceFontSize { get; set; }
    public double BorderThickness { get; set; }
    public int CornerRadius { get; set; }

    public void Load(AimmyConfig config)
    {
        ShowDetectedPlayer = config.Overlay.ShowDetectedPlayer;
        ShowConfidence = config.Overlay.ShowConfidence;
        ShowTracers = config.Overlay.ShowTracers;
        TracerPosition = config.Overlay.TracerPosition;
        Opacity = config.Overlay.Opacity;
        DetectedPlayerColor = config.Overlay.DetectedPlayerColor;
        ConfidenceFontSize = config.Overlay.ConfidenceFontSize;
        BorderThickness = config.Overlay.BorderThickness;
        CornerRadius = config.Overlay.CornerRadius;
    }

    public void Apply(AimmyConfig config)
    {
        config.Overlay.ShowDetectedPlayer = ShowDetectedPlayer;
        config.Overlay.ShowConfidence = ShowConfidence;
        config.Overlay.ShowTracers = ShowTracers;
        config.Overlay.TracerPosition = TracerPosition;
        config.Overlay.Opacity = Opacity;
        config.Overlay.DetectedPlayerColor = DetectedPlayerColor;
        config.Overlay.ConfidenceFontSize = ConfidenceFontSize;
        config.Overlay.BorderThickness = BorderThickness;
        config.Overlay.CornerRadius = CornerRadius;
    }
}
