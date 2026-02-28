using Aimmy.Platform.Abstractions.Interfaces;
using Aimmy.Platform.Abstractions.Models;
using Microsoft.ML.OnnxRuntime;
using System.Text.Json;

namespace Aimmy.Linux.App.Services.Runtime;

public sealed class OnnxModelMetadataReader : IModelMetadataReader
{
    public Task<ModelMetadataInfo> ReadAsync(string modelPath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(modelPath) || !File.Exists(modelPath))
        {
            return Task.FromResult(new ModelMetadataInfo(
                Exists: false,
                IsDynamic: false,
                FixedImageSize: null,
                Classes: Array.Empty<string>(),
                Message: "Model file does not exist."));
        }

        try
        {
            using var session = new InferenceSession(modelPath, new SessionOptions());
            var input = session.InputMetadata.Values.FirstOrDefault();
            var dims = input?.Dimensions?.ToArray() ?? Array.Empty<int>();

            var isDynamic = dims.Any(d => d <= 0);
            int? fixedImageSize = null;
            if (!isDynamic && dims.Length >= 4 && dims[2] > 0 && dims[3] > 0 && dims[2] == dims[3])
            {
                fixedImageSize = dims[2];
            }

            var classes = LoadClassNames(session);
            return Task.FromResult(new ModelMetadataInfo(
                Exists: true,
                IsDynamic: isDynamic,
                FixedImageSize: fixedImageSize,
                Classes: classes,
                Message: isDynamic ? "Dynamic image-size model metadata loaded." : "Fixed image-size model metadata loaded."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ModelMetadataInfo(
                Exists: true,
                IsDynamic: false,
                FixedImageSize: null,
                Classes: Array.Empty<string>(),
                Message: $"Failed to read model metadata: {ex.Message}"));
        }
    }

    private static IReadOnlyList<string> LoadClassNames(InferenceSession session)
    {
        try
        {
            var metadata = session.ModelMetadata;
            if (metadata?.CustomMetadataMap is null ||
                !metadata.CustomMetadataMap.TryGetValue("names", out var namesJson) ||
                string.IsNullOrWhiteSpace(namesJson))
            {
                return Array.Empty<string>();
            }

            using var document = JsonDocument.Parse(namesJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return Array.Empty<string>();
            }

            return document.RootElement
                .EnumerateObject()
                .Where(property => property.Value.ValueKind == JsonValueKind.String)
                .Select(property =>
                {
                    var label = property.Value.GetString();
                    return string.IsNullOrWhiteSpace(label) ? string.Empty : label;
                })
                .Where(label => !string.IsNullOrWhiteSpace(label))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(label => label, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}
