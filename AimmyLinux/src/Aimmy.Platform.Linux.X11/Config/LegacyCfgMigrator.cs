using Aimmy.Core.Config;
using Aimmy.Core.Enums;
using Aimmy.Platform.Abstractions.Interfaces;
using System.Text.Json;

namespace Aimmy.Platform.Linux.X11.Config;

public sealed class LegacyCfgMigrator : IConfigMigrator
{
    private static readonly string[] LegacyMarkers =
    {
        "Aim Assist",
        "Prediction Method",
        "FOV Size",
        "AI Minimum Confidence"
    };

    public bool CanMigrate(string path, string rawContent)
    {
        if (path.EndsWith(".cfg", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return LegacyMarkers.Any(marker => rawContent.Contains($"\"{marker}\"", StringComparison.Ordinal));
    }

    public bool TryMigrate(string path, string rawContent, out AimmyConfig migratedConfig, out string message)
    {
        migratedConfig = AimmyConfig.CreateDefault();

        try
        {
            using var document = JsonDocument.Parse(rawContent);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                message = "Legacy cfg is not a JSON object.";
                return false;
            }

            var root = document.RootElement;

            migratedConfig.Model.ConfidenceThreshold = GetFloat(root, "AI Minimum Confidence", 45f) / 100f;
            migratedConfig.Model.TargetClass = GetString(root, "Target Class", migratedConfig.Model.TargetClass);
            migratedConfig.Model.ImageSize = GetInt(root, "Image Size", migratedConfig.Model.ImageSize);

            migratedConfig.Capture.ExternalBackendPreference = GetString(root, "Preferred Capture Backend", migratedConfig.Capture.ExternalBackendPreference);
            migratedConfig.Capture.Method = ParseCaptureMethod(GetString(root, "Screen Capture Method", "GDI+"));
            migratedConfig.Capture.DisplayWidth = GetInt(root, "Display Width", migratedConfig.Capture.DisplayWidth);
            migratedConfig.Capture.DisplayHeight = GetInt(root, "Display Height", migratedConfig.Capture.DisplayHeight);
            migratedConfig.Capture.DisplayOffsetX = GetInt(root, "Display Offset X", migratedConfig.Capture.DisplayOffsetX);
            migratedConfig.Capture.DisplayOffsetY = GetInt(root, "Display Offset Y", migratedConfig.Capture.DisplayOffsetY);
            migratedConfig.Capture.DpiScaleX = GetDouble(root, "DPI Scale X", migratedConfig.Capture.DpiScaleX);
            migratedConfig.Capture.DpiScaleY = GetDouble(root, "DPI Scale Y", migratedConfig.Capture.DpiScaleY);

            migratedConfig.Input.PreferredMethod = ParseInputMethod(GetString(root, "Mouse Movement Method", "Mouse Event"));
            migratedConfig.Input.AimKeybind = GetString(root, "Aim Keybind", migratedConfig.Input.AimKeybind);
            migratedConfig.Input.SecondaryAimKeybind = GetString(root, "Second Aim Keybind", migratedConfig.Input.SecondaryAimKeybind);
            migratedConfig.Input.DynamicFovKeybind = GetString(root, "Dynamic FOV Keybind", migratedConfig.Input.DynamicFovKeybind);
            migratedConfig.Input.EmergencyStopKeybind = GetString(root, "Emergency Stop Keybind", migratedConfig.Input.EmergencyStopKeybind);
            migratedConfig.Input.ModelSwitchKeybind = GetString(root, "Model Switch Keybind", migratedConfig.Input.ModelSwitchKeybind);

            migratedConfig.Aim.Enabled = GetBool(root, "Aim Assist", migratedConfig.Aim.Enabled);
            migratedConfig.Aim.ConstantTracking = GetBool(root, "Constant AI Tracking", migratedConfig.Aim.ConstantTracking);
            migratedConfig.Aim.StickyAimEnabled = GetBool(root, "Sticky Aim", migratedConfig.Aim.StickyAimEnabled);
            migratedConfig.Aim.StickyAimThreshold = GetInt(root, "Sticky Aim Threshold", migratedConfig.Aim.StickyAimThreshold);
            migratedConfig.Aim.DynamicFovEnabled = GetBool(root, "Dynamic FOV", migratedConfig.Aim.DynamicFovEnabled);
            migratedConfig.Aim.ThirdPersonSupport = GetBool(root, "Third Person Support", migratedConfig.Aim.ThirdPersonSupport);
            migratedConfig.Aim.XAxisPercentageAdjustment = GetBool(root, "X Axis Percentage Adjustment", migratedConfig.Aim.XAxisPercentageAdjustment);
            migratedConfig.Aim.YAxisPercentageAdjustment = GetBool(root, "Y Axis Percentage Adjustment", migratedConfig.Aim.YAxisPercentageAdjustment);
            migratedConfig.Aim.MouseSensitivity = GetDouble(root, "Mouse Sensitivity (+/-)", migratedConfig.Aim.MouseSensitivity);
            migratedConfig.Aim.MouseJitter = GetInt(root, "Mouse Jitter", migratedConfig.Aim.MouseJitter);
            migratedConfig.Aim.XOffset = GetDouble(root, "X Offset (Left/Right)", migratedConfig.Aim.XOffset);
            migratedConfig.Aim.YOffset = GetDouble(root, "Y Offset (Up/Down)", migratedConfig.Aim.YOffset);
            migratedConfig.Aim.XOffsetPercent = GetDouble(root, "X Offset (%)", migratedConfig.Aim.XOffsetPercent);
            migratedConfig.Aim.YOffsetPercent = GetDouble(root, "Y Offset (%)", migratedConfig.Aim.YOffsetPercent);
            migratedConfig.Aim.DetectionAreaType = ParseDetectionAreaType(GetString(root, "Detection Area Type", "Closest to Center Screen"));
            migratedConfig.Aim.AimingBoundariesAlignment = ParseAimingAlignment(GetString(root, "Aiming Boundaries Alignment", "Center"));
            migratedConfig.Aim.MovementPath = ParseMovementPath(GetString(root, "Movement Path", "Cubic Bezier"));

            migratedConfig.Prediction.Enabled = GetBool(root, "Predictions", migratedConfig.Prediction.Enabled);
            migratedConfig.Prediction.Strategy = ParsePredictionStrategy(GetString(root, "Prediction Method", "Kalman Filter"));
            migratedConfig.Prediction.EmaSmoothingEnabled = GetBool(root, "EMA Smoothening", migratedConfig.Prediction.EmaSmoothingEnabled);
            migratedConfig.Prediction.EmaSmoothingAmount = GetDouble(root, "EMA Smoothening", migratedConfig.Prediction.EmaSmoothingAmount);
            migratedConfig.Prediction.KalmanLeadTime = GetDouble(root, "Kalman Lead Time", migratedConfig.Prediction.KalmanLeadTime);
            migratedConfig.Prediction.WiseTheFoxLeadTime = GetDouble(root, "WiseTheFox Lead Time", migratedConfig.Prediction.WiseTheFoxLeadTime);
            migratedConfig.Prediction.ShalloeLeadMultiplier = GetDouble(root, "Shalloe Lead Multiplier", migratedConfig.Prediction.ShalloeLeadMultiplier);

            migratedConfig.Trigger.Enabled = GetBool(root, "Auto Trigger", migratedConfig.Trigger.Enabled);
            migratedConfig.Trigger.SprayMode = GetBool(root, "Spray Mode", migratedConfig.Trigger.SprayMode);
            migratedConfig.Trigger.CursorCheck = GetBool(root, "Cursor Check", migratedConfig.Trigger.CursorCheck);
            migratedConfig.Trigger.AutoTriggerDelaySeconds = GetDouble(root, "Auto Trigger Delay", migratedConfig.Trigger.AutoTriggerDelaySeconds);

            migratedConfig.Fov.Enabled = GetBool(root, "FOV", migratedConfig.Fov.Enabled);
            migratedConfig.Fov.ShowFov = GetBool(root, "Show FOV", migratedConfig.Fov.ShowFov);
            migratedConfig.Fov.Size = GetInt(root, "FOV Size", migratedConfig.Fov.Size);
            migratedConfig.Fov.DynamicSize = GetInt(root, "Dynamic FOV Size", migratedConfig.Fov.DynamicSize);
            migratedConfig.Fov.Style = GetString(root, "FOV Style", migratedConfig.Fov.Style);
            migratedConfig.Fov.Color = GetString(root, "FOV Color", migratedConfig.Fov.Color);

            migratedConfig.Overlay.ShowDetectedPlayer = GetBool(root, "Show Detected Player", migratedConfig.Overlay.ShowDetectedPlayer);
            migratedConfig.Overlay.ShowConfidence = GetBool(root, "Show AI Confidence", migratedConfig.Overlay.ShowConfidence);
            migratedConfig.Overlay.ShowTracers = GetBool(root, "Show Tracers", migratedConfig.Overlay.ShowTracers);
            migratedConfig.Overlay.TracerPosition = GetString(root, "Tracer Position", migratedConfig.Overlay.TracerPosition);
            migratedConfig.Overlay.Opacity = GetDouble(root, "Opacity", migratedConfig.Overlay.Opacity);

            migratedConfig.DataCollection.CollectDataWhilePlaying = GetBool(root, "Collect Data While Playing", migratedConfig.DataCollection.CollectDataWhilePlaying);
            migratedConfig.DataCollection.AutoLabelData = GetBool(root, "Auto Label Data", migratedConfig.DataCollection.AutoLabelData);

            migratedConfig.Runtime.DebugMode = GetBool(root, "Debug Mode", migratedConfig.Runtime.DebugMode);
            migratedConfig.Runtime.DryRun = GetBool(root, "DryRun", migratedConfig.Runtime.DryRun);
            migratedConfig.Runtime.Fps = GetInt(root, "Fps", migratedConfig.Runtime.Fps);

            migratedConfig.Normalize();
            message = "Migrated legacy cfg format into typed AimmyConfig.";
            return true;
        }
        catch (Exception ex)
        {
            message = $"Failed to migrate legacy cfg: {ex.Message}";
            return false;
        }
    }

    private static float GetFloat(JsonElement root, string key, float fallback)
    {
        return (float)GetDouble(root, key, fallback);
    }

    private static double GetDouble(JsonElement root, string key, double fallback)
    {
        if (!TryGetProperty(root, key, out var element))
        {
            return fallback;
        }

        return element.ValueKind switch
        {
            JsonValueKind.Number when element.TryGetDouble(out var numericValue) => numericValue,
            JsonValueKind.String when double.TryParse(element.GetString(), out var parsedValue) => parsedValue,
            JsonValueKind.True => 1,
            JsonValueKind.False => 0,
            _ => fallback
        };
    }

    private static int GetInt(JsonElement root, string key, int fallback)
    {
        return (int)Math.Round(GetDouble(root, key, fallback));
    }

    private static bool GetBool(JsonElement root, string key, bool fallback)
    {
        if (!TryGetProperty(root, key, out var element))
        {
            return fallback;
        }

        return element.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(element.GetString(), out var parsedValue) => parsedValue,
            JsonValueKind.Number when element.TryGetInt32(out var intValue) => intValue != 0,
            _ => fallback
        };
    }

    private static string GetString(JsonElement root, string key, string fallback)
    {
        if (!TryGetProperty(root, key, out var element))
        {
            return fallback;
        }

        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? fallback,
            JsonValueKind.Number => element.ToString(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            _ => fallback
        };
    }

    private static bool TryGetProperty(JsonElement root, string key, out JsonElement element)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (string.Equals(property.Name, key, StringComparison.OrdinalIgnoreCase))
            {
                element = property.Value;
                return true;
            }
        }

        element = default;
        return false;
    }

    private static PredictionStrategy ParsePredictionStrategy(string value)
    {
        return value switch
        {
            "Shall0e's Prediction" => PredictionStrategy.Shalloe,
            "wisethef0x's EMA Prediction" => PredictionStrategy.WiseTheFox,
            _ => PredictionStrategy.Kalman
        };
    }

    private static InputMethod ParseInputMethod(string value)
    {
        return value switch
        {
            "xdotool" => InputMethod.Xdotool,
            "ydotool" => InputMethod.Ydotool,
            "Mouse Event" => InputMethod.Xdotool,
            "SendInput" => InputMethod.Xdotool,
            _ => InputMethod.UInput
        };
    }

    private static CaptureMethod ParseCaptureMethod(string value)
    {
        return value switch
        {
            "DirectX" => CaptureMethod.X11Shm,
            "GDI+" => CaptureMethod.X11Fallback,
            _ => CaptureMethod.ExternalToolFallback
        };
    }

    private static MovementPathStrategy ParseMovementPath(string value)
    {
        return value switch
        {
            "Exponential" => MovementPathStrategy.Exponential,
            "Linear" => MovementPathStrategy.Linear,
            "Adaptive" => MovementPathStrategy.Adaptive,
            "Perlin Noise" => MovementPathStrategy.PerlinNoise,
            _ => MovementPathStrategy.CubicBezier
        };
    }

    private static DetectionAreaType ParseDetectionAreaType(string value)
    {
        return value switch
        {
            "Closest to Mouse" => DetectionAreaType.ClosestToMouse,
            _ => DetectionAreaType.ClosestToCenterScreen
        };
    }

    private static AimingBoundariesAlignment ParseAimingAlignment(string value)
    {
        return value switch
        {
            "Top" => AimingBoundariesAlignment.Top,
            "Bottom" => AimingBoundariesAlignment.Bottom,
            _ => AimingBoundariesAlignment.Center
        };
    }
}
