using Aimmy.Platform.Linux.X11.Hotkeys;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class X11HotkeyBackendBindingTests
{
    [Fact]
    public void TryResolveMouseButtonMask_ResolvesRightAndLeftButtons()
    {
        var rightResolved = X11HotkeyBackend.TryResolveMouseButtonMask("Right", out var rightMask);
        var leftResolved = X11HotkeyBackend.TryResolveMouseButtonMask("Left", out var leftMask);

        Assert.True(rightResolved);
        Assert.True(leftResolved);
        Assert.NotEqual(0u, rightMask);
        Assert.NotEqual(0u, leftMask);
        Assert.NotEqual(rightMask, leftMask);
    }

    [Fact]
    public void CanonicalizeKeysymName_MapsKnownAliases()
    {
        Assert.Equal("Alt_L", X11HotkeyBackend.CanonicalizeKeysymName("LeftAlt"));
        Assert.Equal("Control_R", X11HotkeyBackend.CanonicalizeKeysymName("RightCtrl"));
        Assert.Equal("Escape", X11HotkeyBackend.CanonicalizeKeysymName("Esc"));
    }

    [Fact]
    public void CanonicalizeKeysymName_PreservesUnknownBindings()
    {
        Assert.Equal("F9", X11HotkeyBackend.CanonicalizeKeysymName("F9"));
    }
}
