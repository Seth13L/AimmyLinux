using Aimmy.Core.Capabilities;
using Aimmy.Core.Enums;
using Aimmy.Platform.Abstractions.Interfaces;
using Aimmy.Platform.Linux.X11.Capture;
using Aimmy.Platform.Linux.X11.Hotkeys;
using Aimmy.Platform.Linux.X11.Input;
using Aimmy.Platform.Linux.X11.Overlay;
using Aimmy.Platform.Linux.X11.Util;

namespace Aimmy.Platform.Linux.X11.Runtime;

public sealed class LinuxRuntimeCapabilityProbe : IRuntimeCapabilityProbe
{
    private readonly ICommandRunner _commandRunner;
    private readonly Func<string, string?> _environmentVariableReader;
    private readonly Func<Func<string, string?>, (bool Supported, string Reason)> _nativeCaptureSupportProbe;
    private readonly Func<Func<string, string?>, (bool Supported, string Reason)> _hotkeySupportProbe;
    private readonly Func<ICommandRunner, UInputSetupStatus> _uinputSetupProbe;

    public LinuxRuntimeCapabilityProbe(
        ICommandRunner? commandRunner = null,
        Func<string, string?>? environmentVariableReader = null,
        Func<Func<string, string?>, (bool Supported, string Reason)>? nativeCaptureSupportProbe = null,
        Func<Func<string, string?>, (bool Supported, string Reason)>? hotkeySupportProbe = null,
        Func<ICommandRunner, UInputSetupStatus>? uinputSetupProbe = null)
    {
        _commandRunner = commandRunner ?? ProcessRunner.Instance;
        _environmentVariableReader = environmentVariableReader ?? Environment.GetEnvironmentVariable;
        _nativeCaptureSupportProbe = nativeCaptureSupportProbe ?? DefaultNativeCaptureSupportProbe;
        _hotkeySupportProbe = hotkeySupportProbe ?? DefaultHotkeySupportProbe;
        _uinputSetupProbe = uinputSetupProbe ?? (runner => UInputSetupDiagnostics.Probe(runner));
    }

    public RuntimeCapabilities Probe()
    {
        var caps = RuntimeCapabilities.CreateDefault();

        var sessionType = _environmentVariableReader("XDG_SESSION_TYPE") ?? string.Empty;
        var display = _environmentVariableReader("DISPLAY");
        var isX11 = string.Equals(sessionType, "x11", StringComparison.OrdinalIgnoreCase)
            || !string.IsNullOrWhiteSpace(display);

        caps.Set(
            "X11Session",
            isX11 ? FeatureState.Enabled : FeatureState.Disabled,
            !isX11,
            isX11
                ? "X11 session detected."
                : "X11 session not detected. Wayland aim pipeline is unsupported in v1.");

        var hasCaptureTool = _commandRunner.CommandExists("grim")
            || _commandRunner.CommandExists("maim")
            || _commandRunner.CommandExists("import")
            || _commandRunner.CommandExists("scrot");

        var hasXdotool = _commandRunner.CommandExists("xdotool");
        var uinputStatus = _uinputSetupProbe(_commandRunner);

        if (!isX11)
        {
            caps.Set("CaptureBackend", FeatureState.Disabled, true, "Capture pipeline requires X11 for v1 aim-assist mode.");
            caps.Set("InputBackend", FeatureState.Disabled, true, "Input injection requires X11 for v1 aim-assist mode.");
        }
        else
        {
            var (nativeCaptureSupported, nativeCaptureReason) = _nativeCaptureSupportProbe(_environmentVariableReader);
            if (nativeCaptureSupported)
            {
                caps.Set("CaptureBackend", FeatureState.Enabled, false, "Native X11 capture backend available.");
            }
            else if (hasCaptureTool)
            {
                caps.Set("CaptureBackend", FeatureState.Enabled, true, $"External capture tooling fallback detected. {nativeCaptureReason}");
            }
            else
            {
                caps.Set("CaptureBackend", FeatureState.Unavailable, true, $"No capture backend found. {nativeCaptureReason} (grim/maim/import/scrot unavailable)");
            }

            var inputCapability = BuildInputCapability(hasXdotool, uinputStatus);
            caps.Set("InputBackend", inputCapability.State, inputCapability.IsDegraded, inputCapability.Message);
        }

        if (!isX11)
        {
            caps.Set("Hotkeys", FeatureState.Disabled, true, "Global hotkeys require X11.");
        }
        else
        {
            var (hotkeysSupported, hotkeyReason) = _hotkeySupportProbe(_environmentVariableReader);
            if (hotkeysSupported)
            {
                caps.Set("Hotkeys", FeatureState.Enabled, false, "X11 global hotkeys backend available.");
            }
            else
            {
                caps.Set("Hotkeys", FeatureState.Disabled, true, $"Fallback hotkeys are in use. {hotkeyReason}");
            }
        }

        if (!isX11)
        {
            caps.Set("Overlay", FeatureState.Disabled, true, "Overlay requires an X11 session.");
        }
        else if (X11OverlayBackend.IsSupported(_commandRunner, _environmentVariableReader, out var overlayReason))
        {
            caps.Set("Overlay", FeatureState.Enabled, false, "X11 overlay is available (FOV + detections + tracers).");
        }
        else
        {
            caps.Set("Overlay", FeatureState.Unavailable, true, overlayReason);
        }

        caps.Set("WaylandAimAssist", FeatureState.Unavailable, false, "Wayland aim pipeline is intentionally unsupported in v1.");

        return caps;
    }

    private static (FeatureState State, bool IsDegraded, string Message) BuildInputCapability(
        bool hasXdotool,
        UInputSetupStatus uinputStatus)
    {
        if (uinputStatus.IsSupported)
        {
            var suffix = hasXdotool ? " xdotool fallback is also available." : string.Empty;
            return (FeatureState.Enabled, false, uinputStatus.Message + suffix);
        }

        if (hasXdotool)
        {
            return (
                FeatureState.Enabled,
                true,
                $"uinput path unavailable: {uinputStatus.Message} Falling back to xdotool.");
        }

        var missingFallbackHint = uinputStatus.YDotoolInstalled
            ? "Install xdotool as an additional fallback."
            : "Install ydotool (uinput path) or xdotool (fallback path).";

        return (
            FeatureState.Unavailable,
            true,
            $"No usable input backend. {uinputStatus.Message} {missingFallbackHint}");
    }

    private static (bool Supported, string Reason) DefaultHotkeySupportProbe(Func<string, string?> environmentVariableReader)
    {
        var supported = X11HotkeyBackend.IsSupported(environmentVariableReader, out var reason);
        return (supported, reason);
    }

    private static (bool Supported, string Reason) DefaultNativeCaptureSupportProbe(Func<string, string?> environmentVariableReader)
    {
        var supported = X11CaptureBackend.IsSupported(environmentVariableReader, out var reason);
        return (supported, reason);
    }
}
