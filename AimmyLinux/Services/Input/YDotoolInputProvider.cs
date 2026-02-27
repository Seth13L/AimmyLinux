using AimmyLinux.Util;

namespace AimmyLinux.Services.Input;

public sealed class YDotoolInputProvider : IInputProvider
{
    public YDotoolInputProvider()
    {
        if (!ProcessRunner.CommandExists("ydotool"))
        {
            throw new InvalidOperationException("ydotool is not installed. Install it or use xdotool.");
        }
    }

    public async Task MoveRelativeAsync(int dx, int dy, CancellationToken cancellationToken)
    {
        if (dx == 0 && dy == 0)
        {
            return;
        }

        var exitCode = await ProcessRunner.RunAsync(
            "ydotool",
            $"mousemove -- {dx} {dy}",
            cancellationToken).ConfigureAwait(false);

        if (exitCode != 0)
        {
            throw new InvalidOperationException($"ydotool failed with exit code {exitCode}.");
        }
    }
}
