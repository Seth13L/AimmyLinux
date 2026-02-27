using Aimmy.Platform.Linux.X11.Input;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class UInputSetupDiagnosticsTests
{
    [Fact]
    public void Probe_ReturnsUnsupported_WhenYDotoolIsMissing()
    {
        var runner = new FakeCommandRunner(commandExists: _ => false);

        var status = UInputSetupDiagnostics.Probe(runner, isLinuxOverride: true);

        Assert.False(status.IsSupported);
        Assert.False(status.YDotoolInstalled);
        Assert.Contains("ydotool", status.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Probe_ReturnsUnsupported_WhenUInputDeviceIsMissing()
    {
        var runner = new FakeCommandRunner(commandExists: command => command == "ydotool");

        var status = UInputSetupDiagnostics.Probe(
            runner,
            fileExists: _ => false,
            writableProbe: _ => false,
            isLinuxOverride: true);

        Assert.False(status.IsSupported);
        Assert.True(status.YDotoolInstalled);
        Assert.False(status.DevicePresent);
        Assert.Contains("modprobe", status.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Probe_ReturnsUnsupported_WhenUInputDeviceIsNotWritable()
    {
        var runner = new FakeCommandRunner(commandExists: command => command == "ydotool");

        var status = UInputSetupDiagnostics.Probe(
            runner,
            fileExists: path => path == "/dev/uinput",
            writableProbe: _ => false,
            isLinuxOverride: true);

        Assert.False(status.IsSupported);
        Assert.True(status.DevicePresent);
        Assert.False(status.DeviceWritable);
        Assert.Contains("udev", status.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Probe_ReturnsSupported_WhenYDotoolAndUInputAreReady()
    {
        var runner = new FakeCommandRunner(commandExists: command => command == "ydotool");

        var status = UInputSetupDiagnostics.Probe(
            runner,
            fileExists: path => path == "/dev/uinput",
            writableProbe: _ => true,
            isLinuxOverride: true);

        Assert.True(status.IsSupported);
        Assert.True(status.YDotoolInstalled);
        Assert.True(status.DevicePresent);
        Assert.True(status.DeviceWritable);
        Assert.Equal("/dev/uinput", status.DevicePath);
    }
}
