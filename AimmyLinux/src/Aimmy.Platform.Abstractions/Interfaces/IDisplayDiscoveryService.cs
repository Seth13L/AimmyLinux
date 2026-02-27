using Aimmy.Platform.Abstractions.Models;

namespace Aimmy.Platform.Abstractions.Interfaces;

public interface IDisplayDiscoveryService
{
    Task<IReadOnlyList<DisplayInfo>> DiscoverAsync(CancellationToken cancellationToken);
}
