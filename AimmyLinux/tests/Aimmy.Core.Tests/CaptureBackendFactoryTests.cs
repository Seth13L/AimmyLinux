using Aimmy.Core.Config;
using Aimmy.Core.Enums;
using Aimmy.Platform.Abstractions.Interfaces;
using Aimmy.Platform.Abstractions.Models;
using Aimmy.Platform.Linux.X11.Capture;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class CaptureBackendFactoryTests
{
    [Fact]
    public void Create_X11Shm_UsesNativeBackend_WhenNativeSupportAvailable()
    {
        var config = AimmyConfig.CreateDefault();
        config.Capture.Method = CaptureMethod.X11Shm;

        var backend = CaptureBackendFactory.Create(
            config,
            commandRunner: new FakeCommandRunner(),
            environmentVariableReader: _ => null,
            nativeSupportProbe: _ => (true, "supported"),
            nativeBackendFactory: (_, _) => new TestCaptureBackend("native-test"));

        Assert.Equal("native-test", backend.Name);
    }

    [Fact]
    public void Create_X11Shm_FallsBackToExternal_WhenNativeUnsupported()
    {
        var config = AimmyConfig.CreateDefault();
        config.Capture.Method = CaptureMethod.X11Shm;
        config.Capture.ExternalBackendPreference = "maim";

        var backend = CaptureBackendFactory.Create(
            config,
            commandRunner: new FakeCommandRunner(),
            environmentVariableReader: _ => null,
            nativeSupportProbe: _ => (false, "unsupported"),
            nativeBackendFactory: (_, _) => new TestCaptureBackend("native-test"));

        Assert.StartsWith("ExternalCapture(", backend.Name, StringComparison.Ordinal);
    }

    [Fact]
    public void Create_X11Fallback_UsesExternalEvenWhenNativeSupported()
    {
        var config = AimmyConfig.CreateDefault();
        config.Capture.Method = CaptureMethod.X11Fallback;
        config.Capture.ExternalBackendPreference = "grim";

        var backend = CaptureBackendFactory.Create(
            config,
            commandRunner: new FakeCommandRunner(),
            environmentVariableReader: _ => null,
            nativeSupportProbe: _ => (true, "supported"),
            nativeBackendFactory: (_, _) => new TestCaptureBackend("native-test"));

        Assert.StartsWith("ExternalCapture(", backend.Name, StringComparison.Ordinal);
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
