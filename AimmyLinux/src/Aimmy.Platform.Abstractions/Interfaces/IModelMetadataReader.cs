using Aimmy.Platform.Abstractions.Models;

namespace Aimmy.Platform.Abstractions.Interfaces;

public interface IModelMetadataReader
{
    Task<ModelMetadataInfo> ReadAsync(string modelPath, CancellationToken cancellationToken);
}
