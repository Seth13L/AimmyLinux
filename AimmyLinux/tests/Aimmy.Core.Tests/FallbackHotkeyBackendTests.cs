using Aimmy.Platform.Linux.X11.Hotkeys;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class FallbackHotkeyBackendTests
{
    [Fact]
    public void DefaultFallback_ReportsAimBindingsPressed()
    {
        var backend = new FallbackHotkeyBackend();

        Assert.True(backend.IsPressed("Aim Keybind"));
        Assert.True(backend.IsPressed("Second Aim Keybind"));
        Assert.False(backend.IsPressed("Unbound Key"));
    }

    [Fact]
    public void CustomFallbackSet_UsesProvidedBindings()
    {
        var backend = new FallbackHotkeyBackend(new[] { "CustomToggle" });

        Assert.True(backend.IsPressed("CustomToggle"));
        Assert.False(backend.IsPressed("Aim Keybind"));
    }
}
