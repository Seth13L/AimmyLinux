using Aimmy.Platform.Abstractions.Interfaces;

namespace Aimmy.Platform.Linux.X11.Cursor;

public static class CursorProviderFactory
{
    public static ICursorProvider Create(Func<string, string?>? environmentVariableReader = null)
    {
        try
        {
            if (X11CursorProvider.IsSupported(environmentVariableReader))
            {
                return new X11CursorProvider();
            }
        }
        catch
        {
            // Fall through to noop provider.
        }

        return new NoopCursorProvider();
    }
}
