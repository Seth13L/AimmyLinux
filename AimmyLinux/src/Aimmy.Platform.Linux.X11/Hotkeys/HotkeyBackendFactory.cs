using Aimmy.Core.Config;
using Aimmy.Platform.Abstractions.Interfaces;

namespace Aimmy.Platform.Linux.X11.Hotkeys;

public static class HotkeyBackendFactory
{
    public static IHotkeyBackend Create(
        AimmyConfig config,
        Func<string, string?>? environmentVariableReader = null,
        Func<Func<string, string?>, (bool Supported, string Reason)>? supportProbe = null)
    {
        var envReader = environmentVariableReader ?? Environment.GetEnvironmentVariable;
        var probe = supportProbe ?? ProbeHotkeySupport;
        var (supported, reason) = probe(envReader);

        if (supported)
        {
            return new X11HotkeyBackend(config, envReader);
        }

        Console.WriteLine($"Hotkey backend fallback active: {reason}");
        return new FallbackHotkeyBackend();
    }

    private static (bool Supported, string Reason) ProbeHotkeySupport(Func<string, string?> environmentVariableReader)
    {
        var supported = X11HotkeyBackend.IsSupported(environmentVariableReader, out var reason);
        return (supported, reason);
    }
}
