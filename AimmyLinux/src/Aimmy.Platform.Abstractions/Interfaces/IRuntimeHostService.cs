using Aimmy.Core.Capabilities;
using Aimmy.Core.Config;
using Aimmy.Platform.Abstractions.Models;

namespace Aimmy.Platform.Abstractions.Interfaces;

public interface IRuntimeHostService : IAsyncDisposable
{
    Task StartAsync(AimmyConfig config, RuntimeCapabilities runtimeCapabilities, CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
    Task ApplyConfigAsync(AimmyConfig config, RuntimeCapabilities runtimeCapabilities, CancellationToken cancellationToken);
    RuntimeHostSnapshot GetStatusSnapshot();
}
