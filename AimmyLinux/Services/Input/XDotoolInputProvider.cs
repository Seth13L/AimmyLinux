using AimmyLinux.Util;

namespace AimmyLinux.Services.Input;

public sealed class XDotoolInputProvider : IInputProvider
{
    public XDotoolInputProvider()
    {
        if (!ProcessRunner.CommandExists("xdotool"))
        {
            throw new InvalidOperationException("xdotool is not installed. Install it or run with --dry-run true.");
        }
    }

    public async Task MoveRelativeAsync(int dx, int dy, CancellationToken cancellationToken)
    {
        if (dx == 0 && dy == 0)
        {
            return;
        }

        var exitCode = await ProcessRunner.RunAsync(
            "xdotool",
            $"mousemove_relative -- {dx} {dy}",
            cancellationToken).ConfigureAwait(false);

        if (exitCode != 0)
        {
            throw new InvalidOperationException($"xdotool failed with exit code {exitCode}.");
        }
    }
}
