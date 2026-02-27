using Aimmy.Platform.Abstractions.Interfaces;
using Aimmy.Platform.Linux.X11.Util;

namespace Aimmy.Platform.Linux.X11.Input;

public sealed class UInputInputBackend : IInputBackend
{
    private readonly YDotoolInputBackend _delegate;

    public UInputInputBackend(ICommandRunner? commandRunner = null)
    {
        var runner = commandRunner ?? ProcessRunner.Instance;

        if (!runner.CommandExists("ydotool"))
        {
            throw new InvalidOperationException("uinput backend requires ydotool to be installed.");
        }

        _delegate = new YDotoolInputBackend(runner);
    }

    public string Name => "uinput(ydotool)";

    public Task MoveRelativeAsync(int dx, int dy, CancellationToken cancellationToken)
        => _delegate.MoveRelativeAsync(dx, dy, cancellationToken);

    public Task ClickAsync(CancellationToken cancellationToken)
        => _delegate.ClickAsync(cancellationToken);

    public Task HoldLeftButtonAsync(CancellationToken cancellationToken)
        => _delegate.HoldLeftButtonAsync(cancellationToken);

    public Task ReleaseLeftButtonAsync(CancellationToken cancellationToken)
        => _delegate.ReleaseLeftButtonAsync(cancellationToken);
}
