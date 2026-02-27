namespace Aimmy.Platform.Abstractions.Interfaces;

public interface IHotkeyBackend : IAsyncDisposable
{
    string Name { get; }
    Task StartAsync(CancellationToken cancellationToken);
    bool IsPressed(string bindingId);
}
