using Aimmy.Core.Config;
using Aimmy.Core.Enums;
using Aimmy.Platform.Abstractions.Models;
using Aimmy.UI.Avalonia.ViewModels;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class ConfigurationEditorViewModelTests
{
    [Fact]
    public void Apply_UpdatesDisplayRuntimeAimPredictionTriggerOverlayAndDataCollectionSections()
    {
        var config = AimmyConfig.CreateDefault();
        var vm = new ConfigurationEditorViewModel();
        var displays = new[]
        {
            new DisplayInfo("HDMI-0", "HDMI-0", true, 0, 0, 1920, 1080),
            new DisplayInfo("DP-1", "DP-1", false, 1920, 0, 2560, 1440, 1.25f, 1.25f)
        };

        vm.Load(config, displays);
        vm.DisplaySelection.UseDiscoveredDisplay = true;
        vm.DisplaySelection.SelectedDisplayId = "DP-1";
        vm.InputSettings.PreferredMethod = InputMethod.Xdotool.ToString();
        vm.InputSettings.AimKeybind = "F8";
        vm.InputSettings.SecondaryAimKeybind = "F9";
        vm.InputSettings.DynamicFovKeybind = "F10";
        vm.InputSettings.EmergencyStopKeybind = "End";
        vm.InputSettings.ModelSwitchKeybind = "Insert";

        vm.RuntimeSettings.Fps = 165;
        vm.RuntimeSettings.DryRun = false;
        vm.RuntimeSettings.DebugMode = true;
        vm.RuntimeSettings.GpuMode = GpuExecutionMode.Cpu.ToString();
        vm.RuntimeSettings.EnableDiagnosticsAssertions = true;
        vm.RuntimeSettings.DiagnosticsMinimumFps = 70;
        vm.RuntimeSettings.DiagnosticsMaxCaptureP95Ms = 18;
        vm.RuntimeSettings.DiagnosticsMaxInferenceP95Ms = 24;
        vm.RuntimeSettings.DiagnosticsMaxLoopP95Ms = 32;

        vm.AimSettings.Enabled = true;
        vm.AimSettings.ConstantTracking = true;
        vm.AimSettings.StickyAimEnabled = true;
        vm.AimSettings.StickyAimThreshold = 64;
        vm.AimSettings.MouseSensitivity = 0.72;
        vm.AimSettings.MouseJitter = 5;
        vm.AimSettings.MaxDeltaPerAxis = 190;
        vm.AimSettings.DetectionAreaType = DetectionAreaType.ClosestToMouse.ToString();
        vm.AimSettings.AimingBoundariesAlignment = AimingBoundariesAlignment.Top.ToString();
        vm.AimSettings.MovementPath = MovementPathStrategy.Linear.ToString();

        vm.PredictionSettings.Enabled = true;
        vm.PredictionSettings.Strategy = PredictionStrategy.WiseTheFox.ToString();
        vm.PredictionSettings.EmaSmoothingEnabled = true;
        vm.PredictionSettings.EmaSmoothingAmount = 0.42;
        vm.PredictionSettings.KalmanLeadTime = 0.12;
        vm.PredictionSettings.WiseTheFoxLeadTime = 0.22;
        vm.PredictionSettings.ShalloeLeadMultiplier = 4.5;

        vm.TriggerSettings.Enabled = true;
        vm.TriggerSettings.SprayMode = true;
        vm.TriggerSettings.CursorCheck = true;
        vm.TriggerSettings.AutoTriggerDelaySeconds = 0.09;

        vm.FovSettings.Enabled = true;
        vm.FovSettings.ShowFov = true;
        vm.FovSettings.Size = 720;
        vm.FovSettings.DynamicSize = 260;
        vm.FovSettings.Style = "Box";
        vm.FovSettings.Color = "#FF00FF00";

        vm.OverlaySettings.ShowDetectedPlayer = true;
        vm.OverlaySettings.ShowConfidence = true;
        vm.OverlaySettings.ShowTracers = true;
        vm.OverlaySettings.TracerPosition = "Center";
        vm.OverlaySettings.Opacity = 0.8;

        vm.DataCollection.CollectDataWhilePlaying = true;
        vm.DataCollection.AutoLabelData = true;
        vm.DataCollection.ImagesDirectory = "session/images";
        vm.DataCollection.LabelsDirectory = "session/labels";

        vm.Apply(config);

        Assert.Equal(2560, config.Capture.DisplayWidth);
        Assert.Equal(1440, config.Capture.DisplayHeight);
        Assert.Equal(1920, config.Capture.DisplayOffsetX);
        Assert.Equal(1.25, config.Capture.DpiScaleX, 3);

        Assert.Equal(InputMethod.Xdotool, config.Input.PreferredMethod);
        Assert.Equal("F8", config.Input.AimKeybind);
        Assert.Equal("F9", config.Input.SecondaryAimKeybind);
        Assert.Equal("F10", config.Input.DynamicFovKeybind);
        Assert.Equal("End", config.Input.EmergencyStopKeybind);
        Assert.Equal("Insert", config.Input.ModelSwitchKeybind);

        Assert.Equal(165, config.Runtime.Fps);
        Assert.False(config.Runtime.DryRun);
        Assert.True(config.Runtime.DebugMode);
        Assert.Equal(GpuExecutionMode.Cpu, config.Runtime.GpuMode);
        Assert.True(config.Runtime.EnableDiagnosticsAssertions);
        Assert.Equal(70, config.Runtime.DiagnosticsMinimumFps);

        Assert.True(config.Aim.ConstantTracking);
        Assert.True(config.Aim.StickyAimEnabled);
        Assert.Equal(64, config.Aim.StickyAimThreshold);
        Assert.Equal(DetectionAreaType.ClosestToMouse, config.Aim.DetectionAreaType);
        Assert.Equal(AimingBoundariesAlignment.Top, config.Aim.AimingBoundariesAlignment);
        Assert.Equal(MovementPathStrategy.Linear, config.Aim.MovementPath);

        Assert.True(config.Prediction.Enabled);
        Assert.Equal(PredictionStrategy.WiseTheFox, config.Prediction.Strategy);
        Assert.True(config.Prediction.EmaSmoothingEnabled);

        Assert.True(config.Trigger.Enabled);
        Assert.True(config.Trigger.SprayMode);
        Assert.True(config.Trigger.CursorCheck);

        Assert.True(config.Fov.Enabled);
        Assert.Equal(720, config.Fov.Size);
        Assert.Equal("Box", config.Fov.Style);
        Assert.Equal("#FF00FF00", config.Fov.Color);

        Assert.True(config.Overlay.ShowDetectedPlayer);
        Assert.True(config.Overlay.ShowConfidence);
        Assert.True(config.Overlay.ShowTracers);
        Assert.Equal("Center", config.Overlay.TracerPosition);
        Assert.Equal(0.8, config.Overlay.Opacity, 3);

        Assert.True(config.DataCollection.CollectDataWhilePlaying);
        Assert.True(config.DataCollection.AutoLabelData);
        Assert.Equal("session/images", config.DataCollection.ImagesDirectory);
        Assert.Equal("session/labels", config.DataCollection.LabelsDirectory);
    }
}
