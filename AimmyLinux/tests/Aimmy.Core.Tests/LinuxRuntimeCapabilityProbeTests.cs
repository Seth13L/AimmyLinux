using Aimmy.Core.Enums;
using Aimmy.Platform.Linux.X11.Runtime;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class LinuxRuntimeCapabilityProbeTests
{
    [Fact]
    public void Probe_WaylandSession_DisablesCaptureAndInputEvenWhenToolsExist()
    {
        var runner = new FakeCommandRunner(commandExists: _ => true);
        var probe = new LinuxRuntimeCapabilityProbe(
            runner,
            key => key == "XDG_SESSION_TYPE" ? "wayland" : null);

        var caps = probe.Probe();

        Assert.Equal(FeatureState.Disabled, caps.Get("X11Session").State);
        Assert.Equal(FeatureState.Disabled, caps.Get("CaptureBackend").State);
        Assert.Equal(FeatureState.Disabled, caps.Get("InputBackend").State);
        Assert.Equal(FeatureState.Disabled, caps.Get("Overlay").State);
        Assert.True(caps.Get("CaptureBackend").IsDegraded);
        Assert.True(caps.Get("InputBackend").IsDegraded);
        Assert.True(caps.Get("Overlay").IsDegraded);
    }

    [Fact]
    public void Probe_X11Session_WithXdotoolOnly_MarksInputAsDegraded()
    {
        var runner = new FakeCommandRunner(
            commandExists: command => command is "xdotool" or "maim");

        var probe = new LinuxRuntimeCapabilityProbe(
            runner,
            key => key == "XDG_SESSION_TYPE" ? "x11" : null);

        var caps = probe.Probe();
        var input = caps.Get("InputBackend");

        Assert.Equal(FeatureState.Enabled, caps.Get("X11Session").State);
        Assert.Equal(FeatureState.Enabled, input.State);
        Assert.True(input.IsDegraded);
        Assert.Contains("xdotool", input.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(FeatureState.Unavailable, caps.Get("Overlay").State);
    }

    [Fact]
    public void Probe_X11Session_WithYdotool_MarksInputAsEnabledWithoutDegradation()
    {
        var runner = new FakeCommandRunner(
            commandExists: command => command is "ydotool" or "maim");

        var probe = new LinuxRuntimeCapabilityProbe(
            runner,
            key => key == "DISPLAY" ? ":0" : null);

        var caps = probe.Probe();
        var input = caps.Get("InputBackend");

        Assert.Equal(FeatureState.Enabled, input.State);
        Assert.False(input.IsDegraded);
        Assert.Contains("ydotool", input.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(FeatureState.Unavailable, caps.Get("Overlay").State);
    }

    [Fact]
    public void Probe_X11Session_WithPython3_EnablesOverlay()
    {
        var runner = new FakeCommandRunner(
            commandExists: command => command is "python3" or "ydotool" or "maim");

        var probe = new LinuxRuntimeCapabilityProbe(
            runner,
            key => key == "XDG_SESSION_TYPE" ? "x11" : null);

        var caps = probe.Probe();
        var overlay = caps.Get("Overlay");

        Assert.Equal(FeatureState.Enabled, overlay.State);
        Assert.False(overlay.IsDegraded);
        Assert.Contains("FOV + detections + tracers", overlay.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Probe_X11Session_HotkeysEnabled_WhenHotkeyProbeReportsSupport()
    {
        var runner = new FakeCommandRunner(
            commandExists: command => command is "python3" or "ydotool" or "maim");

        var probe = new LinuxRuntimeCapabilityProbe(
            runner,
            key => key == "XDG_SESSION_TYPE" ? "x11" : null,
            _ => (false, "capture unsupported"),
            _ => (true, "supported"));

        var caps = probe.Probe();
        var hotkeys = caps.Get("Hotkeys");

        Assert.Equal(FeatureState.Enabled, hotkeys.State);
        Assert.False(hotkeys.IsDegraded);
    }

    [Fact]
    public void Probe_X11Session_CaptureEnabledWithoutDegradation_WhenNativeCaptureSupported()
    {
        var runner = new FakeCommandRunner();

        var probe = new LinuxRuntimeCapabilityProbe(
            runner,
            key => key == "DISPLAY" ? ":0" : null,
            _ => (true, "supported"),
            _ => (false, "hotkeys unsupported"));

        var caps = probe.Probe();
        var capture = caps.Get("CaptureBackend");

        Assert.Equal(FeatureState.Enabled, capture.State);
        Assert.False(capture.IsDegraded);
        Assert.Contains("Native X11 capture", capture.Message, StringComparison.OrdinalIgnoreCase);
    }
}
