namespace AimmyLinux.Services.Input;

public sealed class NoopInputProvider : IInputProvider
{
    public Task MoveRelativeAsync(int dx, int dy, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
