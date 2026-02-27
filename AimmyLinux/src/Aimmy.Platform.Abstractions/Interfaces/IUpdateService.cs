using Aimmy.Platform.Abstractions.Models;

namespace Aimmy.Platform.Abstractions.Interfaces;

public interface IUpdateService
{
    Task<UpdateCheckResult> CheckForUpdatesAsync(string currentVersion, CancellationToken cancellationToken);
}
