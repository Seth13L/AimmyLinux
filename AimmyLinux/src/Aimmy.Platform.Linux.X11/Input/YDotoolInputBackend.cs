using Aimmy.Platform.Abstractions.Interfaces;
using Aimmy.Platform.Linux.X11.Util;

namespace Aimmy.Platform.Linux.X11.Input;

public sealed class YDotoolInputBackend : IInputBackend
{
    private readonly ICommandRunner _commandRunner;
    private bool _holding;

    public string Name => "ydotool";

    public YDotoolInputBackend(ICommandRunner? commandRunner = null)
    {
        _commandRunner = commandRunner ?? ProcessRunner.Instance;

        if (!_commandRunner.CommandExists("ydotool"))
        {
            throw new InvalidOperationException("ydotool is not installed.");
        }
    }

    public async Task MoveRelativeAsync(int dx, int dy, CancellationToken cancellationToken)
    {
        if (dx == 0 && dy == 0)
        {
            return;
        }

        await EnsureSuccess("ydotool", $"mousemove -- {dx} {dy}", cancellationToken).ConfigureAwait(false);
    }

    public Task ClickAsync(CancellationToken cancellationToken)
    {
        return EnsureSuccess("ydotool", "click 0xC0", cancellationToken);
    }

    public async Task HoldLeftButtonAsync(CancellationToken cancellationToken)
    {
        if (_holding)
        {
            return;
        }

        await EnsureSuccess("ydotool", "mousedown 0xC0", cancellationToken).ConfigureAwait(false);
        _holding = true;
    }

    public async Task ReleaseLeftButtonAsync(CancellationToken cancellationToken)
    {
        if (!_holding)
        {
            return;
        }

        await EnsureSuccess("ydotool", "mouseup 0xC0", cancellationToken).ConfigureAwait(false);
        _holding = false;
    }

    private async Task EnsureSuccess(string cmd, string args, CancellationToken token)
    {
        var result = await _commandRunner.RunAsync(cmd, args, token).ConfigureAwait(false);
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"{cmd} failed: {result.StdErr}");
        }
    }
}
