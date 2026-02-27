using Aimmy.Core.Config;
using Aimmy.Platform.Abstractions.Interfaces;
using Aimmy.Platform.Linux.X11.Util;

namespace Aimmy.Platform.Linux.X11.Overlay;

public static class OverlayBackendFactory
{
    public static IOverlayBackend Create(
        AimmyConfig config,
        ICommandRunner? commandRunner = null,
        Func<string, string?>? environmentVariableReader = null)
    {
        var runner = commandRunner ?? ProcessRunner.Instance;
        var envReader = environmentVariableReader ?? Environment.GetEnvironmentVariable;

        return X11OverlayBackend.IsSupported(runner, envReader, out _)
            ? new X11OverlayBackend(config, runner, envReader)
            : new NoopOverlayBackend();
    }
}
