using Aimmy.Core.Config;
using Aimmy.Core.Enums;
using Aimmy.Platform.Abstractions.Interfaces;
using Aimmy.Platform.Abstractions.Models;
using Aimmy.Platform.Linux.X11.Capture;
using Aimmy.Platform.Linux.X11.Hotkeys;
using Aimmy.Platform.Linux.X11.Input;
using Aimmy.Platform.Linux.X11.Overlay;
using Aimmy.Platform.Linux.X11.Runtime;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class LinuxBackendIntegrationHarnessTests
{
    [Fact]
    public async Task Compose_X11NativePreferred_SelectsNativeBackendsAndEnabledCapabilities()
    {
        var config = AimmyConfig.CreateDefault();
        config.Capture.Method = CaptureMethod.X11Shm;
        config.Input.PreferredMethod = InputMethod.UInput;
        config.Runtime.DryRun = false;

        string? EnvReader(string key) => key switch
        {
            "XDG_SESSION_TYPE" => "x11",
            "DISPLAY" => ":0",
            _ => null
        };

        var runner = new FakeCommandRunner(commandExists: command => command is "ydotool" or "python3");
        var capture = CaptureBackendFactory.Create(
            config,
            commandRunner: runner,
            environmentVariableReader: EnvReader,
            nativeSupportProbe: _ => (true, "native supported"),
            nativeBackendFactory: (_, _) => new TestCaptureBackend("native-test"));
        var input = InputBackendFactory.Create(
            config,
            runner,
            _ => new UInputSetupStatus(true, true, true, "/dev/uinput", "uinput primary path is ready (/dev/uinput)."));
        var hotkeys = HotkeyBackendFactory.Create(config, EnvReader, _ => (true, "hotkeys supported"));
        var overlay = OverlayBackendFactory.Create(config, runner, EnvReader);

        var probe = new LinuxRuntimeCapabilityProbe(
            runner,
            EnvReader,
            _ => (true, "native supported"),
            _ => (true, "hotkeys supported"),
            _ => new UInputSetupStatus(true, true, true, "/dev/uinput", "uinput primary path is ready (/dev/uinput)."));
        var caps = probe.Probe();

        Assert.Equal("native-test", capture.Name);
        Assert.Equal("uinput(ydotool)", input.Name);
        Assert.IsType<X11HotkeyBackend>(hotkeys);
        Assert.IsType<X11OverlayBackend>(overlay);

        Assert.Equal(FeatureState.Enabled, caps.Get("CaptureBackend").State);
        Assert.False(caps.Get("CaptureBackend").IsDegraded);
        Assert.Equal(FeatureState.Enabled, caps.Get("InputBackend").State);
        Assert.False(caps.Get("InputBackend").IsDegraded);
        Assert.Equal(FeatureState.Enabled, caps.Get("Hotkeys").State);
        Assert.False(caps.Get("Hotkeys").IsDegraded);
        Assert.Equal(FeatureState.Enabled, caps.Get("Overlay").State);
        Assert.False(caps.Get("Overlay").IsDegraded);

        await hotkeys.DisposeAsync();
        await overlay.DisposeAsync();
    }

    [Fact]
    public async Task Compose_X11FallbackScenario_SelectsFallbackBackendsAndDegradedCapabilities()
    {
        var config = AimmyConfig.CreateDefault();
        config.Capture.Method = CaptureMethod.X11Shm;
        config.Capture.ExternalBackendPreference = "maim";
        config.Input.PreferredMethod = InputMethod.UInput;
        config.Runtime.DryRun = false;

        string? EnvReader(string key) => key switch
        {
            "XDG_SESSION_TYPE" => "x11",
            "DISPLAY" => ":0",
            _ => null
        };

        var runner = new FakeCommandRunner(commandExists: command => command is "maim" or "xdotool");
        var capture = CaptureBackendFactory.Create(
            config,
            commandRunner: runner,
            environmentVariableReader: EnvReader,
            nativeSupportProbe: _ => (false, "native unsupported"),
            nativeBackendFactory: (_, _) => new TestCaptureBackend("native-test"));
        var input = InputBackendFactory.Create(
            config,
            runner,
            _ => new UInputSetupStatus(false, false, false, string.Empty, "ydotool is not installed."));
        var hotkeys = HotkeyBackendFactory.Create(config, EnvReader, _ => (false, "hotkeys unsupported"));
        var overlay = OverlayBackendFactory.Create(config, runner, EnvReader);

        var probe = new LinuxRuntimeCapabilityProbe(
            runner,
            EnvReader,
            _ => (false, "native unsupported"),
            _ => (false, "hotkeys unsupported"),
            _ => new UInputSetupStatus(false, false, false, string.Empty, "ydotool is not installed."));
        var caps = probe.Probe();

        Assert.StartsWith("ExternalCapture(", capture.Name, StringComparison.Ordinal);
        Assert.Equal("xdotool", input.Name);
        Assert.IsType<FallbackHotkeyBackend>(hotkeys);
        Assert.IsType<NoopOverlayBackend>(overlay);

        Assert.Equal(FeatureState.Enabled, caps.Get("CaptureBackend").State);
        Assert.True(caps.Get("CaptureBackend").IsDegraded);
        Assert.Equal(FeatureState.Enabled, caps.Get("InputBackend").State);
        Assert.True(caps.Get("InputBackend").IsDegraded);
        Assert.Equal(FeatureState.Disabled, caps.Get("Hotkeys").State);
        Assert.True(caps.Get("Hotkeys").IsDegraded);
        Assert.Equal(FeatureState.Unavailable, caps.Get("Overlay").State);
        Assert.True(caps.Get("Overlay").IsDegraded);

        await hotkeys.DisposeAsync();
        await overlay.DisposeAsync();
    }

    private sealed class TestCaptureBackend : ICaptureBackend
    {
        private readonly string _name;

        public TestCaptureBackend(string name)
        {
            _name = name;
        }

        public string Name => _name;

        public Task<Image<Rgba32>> CaptureAsync(CaptureRegion region, CancellationToken cancellationToken)
        {
            return Task.FromResult(new Image<Rgba32>(1, 1));
        }
    }
}
