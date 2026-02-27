using Aimmy.Core.Config;
using Aimmy.Core.Enums;
using Aimmy.Platform.Abstractions.Interfaces;
using Aimmy.Platform.Linux.X11.Util;

namespace Aimmy.Platform.Linux.X11.Capture;

public static class CaptureBackendFactory
{
    public static ICaptureBackend Create(
        AimmyConfig config,
        ICommandRunner? commandRunner = null,
        Func<string, string?>? environmentVariableReader = null,
        Func<Func<string, string?>, (bool Supported, string Reason)>? nativeSupportProbe = null,
        Func<AimmyConfig, Func<string, string?>, ICaptureBackend>? nativeBackendFactory = null)
    {
        var runner = commandRunner ?? ProcessRunner.Instance;
        var envReader = environmentVariableReader ?? Environment.GetEnvironmentVariable;
        var supportProbe = nativeSupportProbe ?? DefaultNativeSupportProbe;
        var backendFactory = nativeBackendFactory ?? ((cfg, reader) => new X11CaptureBackend(cfg, reader));
        var externalFallback = new ExternalScreenshotCaptureBackend(config.Capture.ExternalBackendPreference, runner);

        return config.Capture.Method switch
        {
            CaptureMethod.X11Shm => TryNativeX11(config, envReader, supportProbe, backendFactory) ?? externalFallback,
            CaptureMethod.X11Fallback => externalFallback,
            CaptureMethod.ExternalToolFallback => externalFallback,
            _ => externalFallback
        };
    }

    private static ICaptureBackend? TryNativeX11(
        AimmyConfig config,
        Func<string, string?> environmentVariableReader,
        Func<Func<string, string?>, (bool Supported, string Reason)> supportProbe,
        Func<AimmyConfig, Func<string, string?>, ICaptureBackend> backendFactory)
    {
        var (supported, reason) = supportProbe(environmentVariableReader);
        if (!supported)
        {
            Console.WriteLine($"Capture backend fallback active: {reason}");
            return null;
        }

        try
        {
            return backendFactory(config, environmentVariableReader);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Capture backend fallback active: native X11 capture init failed ({ex.Message}).");
            return null;
        }
    }

    private static (bool Supported, string Reason) DefaultNativeSupportProbe(Func<string, string?> environmentVariableReader)
    {
        var supported = X11CaptureBackend.IsSupported(environmentVariableReader, out var reason);
        return (supported, reason);
    }
}
