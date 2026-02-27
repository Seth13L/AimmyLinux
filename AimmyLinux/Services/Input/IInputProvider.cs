namespace AimmyLinux.Services.Input;

public interface IInputProvider
{
    Task MoveRelativeAsync(int dx, int dy, CancellationToken cancellationToken);
}
