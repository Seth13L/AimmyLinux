using Aimmy.Core.Config;
using Aimmy.Platform.Linux.X11.Hotkeys;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class HotkeyBackendFactoryTests
{
    [Fact]
    public void Create_ReturnsFallback_WhenHotkeysUnsupported()
    {
        var config = AimmyConfig.CreateDefault();

        var backend = HotkeyBackendFactory.Create(
            config,
            _ => null,
            _ => (false, "unsupported"));

        Assert.IsType<FallbackHotkeyBackend>(backend);
    }

    [Fact]
    public void Create_ReturnsX11HotkeyBackend_WhenHotkeysSupported()
    {
        var config = AimmyConfig.CreateDefault();

        var backend = HotkeyBackendFactory.Create(
            config,
            _ => null,
            _ => (true, "supported"));

        Assert.IsType<X11HotkeyBackend>(backend);
    }
}
