namespace Aimmy.Platform.Abstractions.Interfaces;

public interface IInputBackend
{
    string Name { get; }
    Task MoveRelativeAsync(int dx, int dy, CancellationToken cancellationToken);
    Task ClickAsync(CancellationToken cancellationToken);
    Task HoldLeftButtonAsync(CancellationToken cancellationToken);
    Task ReleaseLeftButtonAsync(CancellationToken cancellationToken);
}
