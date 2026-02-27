using Aimmy.Platform.Abstractions.Interfaces;

namespace Aimmy.Platform.Linux.X11.Hotkeys;

public sealed class FallbackHotkeyBackend : IHotkeyBackend
{
    private readonly HashSet<string> _alwaysPressed;

    public FallbackHotkeyBackend(IEnumerable<string>? alwaysPressed = null)
    {
        _alwaysPressed = new HashSet<string>(alwaysPressed ?? new[] { "Aim Keybind", "Second Aim Keybind" }, StringComparer.OrdinalIgnoreCase);
    }

    public string Name => "fallback-hotkeys";

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public bool IsPressed(string bindingId)
    {
        return _alwaysPressed.Contains(bindingId);
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
