using Aimmy.Platform.Abstractions.Models;

namespace Aimmy.Platform.Abstractions.Interfaces;

public interface IModelStoreClient
{
    Task<IReadOnlyList<ModelStoreEntry>> GetModelEntriesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ModelStoreEntry>> GetConfigEntriesAsync(CancellationToken cancellationToken);
    Task<string> DownloadAsync(ModelStoreEntry entry, string destinationDirectory, CancellationToken cancellationToken);
}
