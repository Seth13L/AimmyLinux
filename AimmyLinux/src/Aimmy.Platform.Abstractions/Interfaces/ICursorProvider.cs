namespace Aimmy.Platform.Abstractions.Interfaces;

public interface ICursorProvider
{
    bool TryGetPosition(out int screenX, out int screenY);
}
