using Aimmy.Platform.Abstractions.Models;
using Aimmy.Platform.Linux.X11.Capture;
using Aimmy.Platform.Linux.X11.Util;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class ExternalScreenshotCaptureBackendTests
{
    [Fact]
    public async Task CaptureAsync_AutoMode_AttemptsAvailableBackendsInOrder()
    {
        var runner = new FakeCommandRunner(
            commandExists: command => command is "maim" or "import",
            runCommand: (_, _, _) => new CommandResult(1, string.Empty, "tool failed"));

        var backend = new ExternalScreenshotCaptureBackend("auto", runner);
        var region = new CaptureRegion(0, 0, 64, 64);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => backend.CaptureAsync(region, CancellationToken.None));

        Assert.Contains("Attempted: maim, import", error.Message, StringComparison.Ordinal);
        Assert.Equal(new[] { "maim", "import" }, runner.RunOrder);
    }

    [Fact]
    public async Task CaptureAsync_PreferredBackend_AttemptsOnlyThatBackend()
    {
        var runner = new FakeCommandRunner(
            commandExists: _ => true,
            runCommand: (_, _, _) => new CommandResult(1, string.Empty, "tool failed"));

        var backend = new ExternalScreenshotCaptureBackend("scrot", runner);
        var region = new CaptureRegion(10, 10, 80, 80);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => backend.CaptureAsync(region, CancellationToken.None));

        Assert.Contains("Attempted: scrot", error.Message, StringComparison.Ordinal);
        Assert.Equal(new[] { "scrot" }, runner.RunOrder);
    }
}
