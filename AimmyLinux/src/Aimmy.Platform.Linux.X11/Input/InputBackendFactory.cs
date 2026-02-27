using Aimmy.Core.Config;
using Aimmy.Core.Enums;
using Aimmy.Platform.Abstractions.Interfaces;
using Aimmy.Platform.Linux.X11.Util;

namespace Aimmy.Platform.Linux.X11.Input;

public static class InputBackendFactory
{
    public static IInputBackend Create(AimmyConfig config, ICommandRunner? commandRunner = null)
    {
        var runner = commandRunner ?? ProcessRunner.Instance;

        return config.Runtime.DryRun
            ? new NoopInputBackend()
            : config.Input.PreferredMethod switch
            {
                InputMethod.UInput => TryCreateBackend(() => new UInputInputBackend(runner))
                    ?? TryCreateBackend(() => new XDotoolInputBackend(runner))
                    ?? TryCreateBackend(() => new YDotoolInputBackend(runner))
                    ?? new NoopInputBackend(),
                InputMethod.Xdotool => TryCreateBackend(() => new XDotoolInputBackend(runner))
                    ?? TryCreateBackend(() => new YDotoolInputBackend(runner))
                    ?? new NoopInputBackend(),
                InputMethod.Ydotool => TryCreateBackend(() => new YDotoolInputBackend(runner))
                    ?? TryCreateBackend(() => new XDotoolInputBackend(runner))
                    ?? new NoopInputBackend(),
                _ => new NoopInputBackend()
            };
    }

    private static IInputBackend? TryCreateBackend(Func<IInputBackend> factory)
    {
        try
        {
            return factory();
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}
