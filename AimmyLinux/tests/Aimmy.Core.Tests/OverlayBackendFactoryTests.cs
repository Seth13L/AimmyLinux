using Aimmy.Core.Config;
using Aimmy.Platform.Linux.X11.Overlay;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class OverlayBackendFactoryTests
{
    [Fact]
    public void Create_ReturnsX11OverlayBackend_WhenX11AndPythonAvailable()
    {
        var config = AimmyConfig.CreateDefault();
        var runner = new FakeCommandRunner(commandExists: command => command == "python3");
        var backend = OverlayBackendFactory.Create(config, runner, key => key == "DISPLAY" ? ":0" : null);

        Assert.IsType<X11OverlayBackend>(backend);
    }

    [Fact]
    public void Create_ReturnsNoop_WhenNoX11Session()
    {
        var config = AimmyConfig.CreateDefault();
        var runner = new FakeCommandRunner(commandExists: command => command == "python3");
        var backend = OverlayBackendFactory.Create(config, runner, _ => null);

        Assert.IsType<NoopOverlayBackend>(backend);
    }

    [Fact]
    public void Create_ReturnsNoop_WhenPythonMissing()
    {
        var config = AimmyConfig.CreateDefault();
        var runner = new FakeCommandRunner(commandExists: _ => false);
        var backend = OverlayBackendFactory.Create(config, runner, key => key == "XDG_SESSION_TYPE" ? "x11" : null);

        Assert.IsType<NoopOverlayBackend>(backend);
    }
}
