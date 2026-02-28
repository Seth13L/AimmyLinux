using Aimmy.Platform.Abstractions.Interfaces;

namespace Aimmy.Platform.Linux.X11.Cursor;

public sealed class NoopCursorProvider : ICursorProvider
{
    public bool TryGetPosition(out int screenX, out int screenY)
    {
        screenX = 0;
        screenY = 0;
        return false;
    }
}
