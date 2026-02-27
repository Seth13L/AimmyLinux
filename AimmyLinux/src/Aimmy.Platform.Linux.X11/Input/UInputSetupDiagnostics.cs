using Aimmy.Platform.Linux.X11.Util;

namespace Aimmy.Platform.Linux.X11.Input;

public sealed record UInputSetupStatus(
    bool YDotoolInstalled,
    bool DevicePresent,
    bool DeviceWritable,
    string DevicePath,
    string Message)
{
    public bool IsSupported => YDotoolInstalled && DevicePresent && DeviceWritable;
}

public static class UInputSetupDiagnostics
{
    private static readonly string[] DeviceCandidates =
    {
        "/dev/uinput",
        "/dev/input/uinput"
    };

    public static UInputSetupStatus Probe(
        ICommandRunner commandRunner,
        Func<string, bool>? fileExists = null,
        Func<string, bool>? writableProbe = null,
        bool? isLinuxOverride = null)
    {
        ArgumentNullException.ThrowIfNull(commandRunner);

        var hasYdotool = commandRunner.CommandExists("ydotool");
        if (!hasYdotool)
        {
            return new UInputSetupStatus(
                YDotoolInstalled: false,
                DevicePresent: false,
                DeviceWritable: false,
                DevicePath: string.Empty,
                Message: "ydotool is not installed. Install it for the uinput primary input path.");
        }

        var isLinux = isLinuxOverride ?? OperatingSystem.IsLinux();
        if (!isLinux)
        {
            return new UInputSetupStatus(
                YDotoolInstalled: true,
                DevicePresent: true,
                DeviceWritable: true,
                DevicePath: "n/a",
                Message: "ydotool is installed (non-Linux environment; uinput device checks skipped).");
        }

        var exists = fileExists ?? File.Exists;
        var canWrite = writableProbe ?? DefaultWritableProbe;

        var devicePath = DeviceCandidates.FirstOrDefault(exists) ?? DeviceCandidates[0];
        var devicePresent = DeviceCandidates.Any(exists);
        if (!devicePresent)
        {
            return new UInputSetupStatus(
                YDotoolInstalled: true,
                DevicePresent: false,
                DeviceWritable: false,
                DevicePath: devicePath,
                Message:
                    "uinput device is missing. Run `sudo modprobe uinput` and persist it via `/etc/modules-load.d/uinput.conf`.");
        }

        var writable = canWrite(devicePath);
        if (!writable)
        {
            return new UInputSetupStatus(
                YDotoolInstalled: true,
                DevicePresent: true,
                DeviceWritable: false,
                DevicePath: devicePath,
                Message:
                    "uinput device exists but is not writable. Add your user to the `input` group and install a udev rule: " +
                    "`KERNEL==\"uinput\", GROUP=\"input\", MODE=\"0660\"`.");
        }

        return new UInputSetupStatus(
            YDotoolInstalled: true,
            DevicePresent: true,
            DeviceWritable: true,
            DevicePath: devicePath,
            Message: $"uinput primary path is ready ({devicePath}).");
    }

    private static bool DefaultWritableProbe(string path)
    {
        try
        {
            using var stream = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            return stream.CanWrite;
        }
        catch
        {
            return false;
        }
    }
}
