using Aimmy.Core.Config;
using Aimmy.Core.Enums;

namespace Aimmy.UI.Avalonia.ViewModels;

public sealed class RuntimeSettingsViewModel
{
    public IReadOnlyList<string> GpuModeOptions { get; } = Enum.GetNames<GpuExecutionMode>();

    public int Fps { get; set; }
    public bool DryRun { get; set; }
    public bool DebugMode { get; set; }
    public string GpuMode { get; set; } = GpuExecutionMode.Auto.ToString();
    public bool EnableDiagnosticsAssertions { get; set; }
    public int DiagnosticsMinimumFps { get; set; }
    public int DiagnosticsMaxCaptureP95Ms { get; set; }
    public int DiagnosticsMaxInferenceP95Ms { get; set; }
    public int DiagnosticsMaxLoopP95Ms { get; set; }

    public void Load(AimmyConfig config)
    {
        Fps = config.Runtime.Fps;
        DryRun = config.Runtime.DryRun;
        DebugMode = config.Runtime.DebugMode;
        GpuMode = config.Runtime.GpuMode.ToString();
        EnableDiagnosticsAssertions = config.Runtime.EnableDiagnosticsAssertions;
        DiagnosticsMinimumFps = config.Runtime.DiagnosticsMinimumFps;
        DiagnosticsMaxCaptureP95Ms = config.Runtime.DiagnosticsMaxCaptureP95Ms;
        DiagnosticsMaxInferenceP95Ms = config.Runtime.DiagnosticsMaxInferenceP95Ms;
        DiagnosticsMaxLoopP95Ms = config.Runtime.DiagnosticsMaxLoopP95Ms;
    }

    public void Apply(AimmyConfig config)
    {
        config.Runtime.Fps = Fps;
        config.Runtime.DryRun = DryRun;
        config.Runtime.DebugMode = DebugMode;
        if (Enum.TryParse<GpuExecutionMode>(GpuMode, ignoreCase: true, out var mode))
        {
            config.Runtime.GpuMode = mode;
        }

        config.Runtime.EnableDiagnosticsAssertions = EnableDiagnosticsAssertions;
        config.Runtime.DiagnosticsMinimumFps = DiagnosticsMinimumFps;
        config.Runtime.DiagnosticsMaxCaptureP95Ms = DiagnosticsMaxCaptureP95Ms;
        config.Runtime.DiagnosticsMaxInferenceP95Ms = DiagnosticsMaxInferenceP95Ms;
        config.Runtime.DiagnosticsMaxLoopP95Ms = DiagnosticsMaxLoopP95Ms;
    }
}
