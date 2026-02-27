using Aimmy.Core.Config;
using Aimmy.Core.Enums;
using Aimmy.Platform.Linux.X11.Input;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class InputBackendFactoryTests
{
    [Fact]
    public void Create_DryRun_ReturnsNoop()
    {
        var config = AimmyConfig.CreateDefault();
        config.Runtime.DryRun = true;
        config.Input.PreferredMethod = InputMethod.UInput;

        var runner = new FakeCommandRunner(commandExists: _ => true);
        var backend = InputBackendFactory.Create(config, runner);

        Assert.Equal("noop", backend.Name);
    }

    [Fact]
    public void Create_UInputPreferred_FallsBackToXdotool_WhenYdotoolUnavailable()
    {
        var config = AimmyConfig.CreateDefault();
        config.Runtime.DryRun = false;
        config.Input.PreferredMethod = InputMethod.UInput;

        var runner = new FakeCommandRunner(commandExists: cmd => string.Equals(cmd, "xdotool", StringComparison.Ordinal));
        var backend = InputBackendFactory.Create(config, runner);

        Assert.Equal("xdotool", backend.Name);
    }

    [Fact]
    public void Create_UInputPreferred_ReturnsNoop_WhenNoBackendAvailable()
    {
        var config = AimmyConfig.CreateDefault();
        config.Runtime.DryRun = false;
        config.Input.PreferredMethod = InputMethod.UInput;

        var runner = new FakeCommandRunner();
        var backend = InputBackendFactory.Create(config, runner);

        Assert.Equal("noop", backend.Name);
    }

    [Fact]
    public void Create_UInputPreferred_FallsBackToXdotool_WhenUInputDiagnosticsFail()
    {
        var config = AimmyConfig.CreateDefault();
        config.Runtime.DryRun = false;
        config.Input.PreferredMethod = InputMethod.UInput;

        var runner = new FakeCommandRunner(commandExists: command => command is "ydotool" or "xdotool");
        var backend = InputBackendFactory.Create(
            config,
            runner,
            _ => new UInputSetupStatus(
                YDotoolInstalled: true,
                DevicePresent: true,
                DeviceWritable: false,
                DevicePath: "/dev/uinput",
                Message: "uinput device exists but is not writable."));

        Assert.Equal("xdotool", backend.Name);
    }

    [Fact]
    public void Create_UInputPreferred_UsesUInput_WhenDiagnosticsPass()
    {
        var config = AimmyConfig.CreateDefault();
        config.Runtime.DryRun = false;
        config.Input.PreferredMethod = InputMethod.UInput;

        var runner = new FakeCommandRunner(commandExists: command => command == "ydotool");
        var backend = InputBackendFactory.Create(
            config,
            runner,
            _ => new UInputSetupStatus(
                YDotoolInstalled: true,
                DevicePresent: true,
                DeviceWritable: true,
                DevicePath: "/dev/uinput",
                Message: "uinput primary path is ready (/dev/uinput)."));

        Assert.Equal("uinput(ydotool)", backend.Name);
    }
}
