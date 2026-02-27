using Aimmy.Platform.Abstractions.Interfaces;

namespace Aimmy.Platform.Linux.X11.Input;

public sealed class NoopInputBackend : IInputBackend
{
    public string Name => "noop";

    public Task MoveRelativeAsync(int dx, int dy, CancellationToken cancellationToken) => Task.CompletedTask;
    public Task ClickAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task HoldLeftButtonAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    public Task ReleaseLeftButtonAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
