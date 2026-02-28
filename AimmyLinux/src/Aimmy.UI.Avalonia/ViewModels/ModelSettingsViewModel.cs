using Aimmy.Core.Config;
using Aimmy.Platform.Abstractions.Models;
using System.Collections.ObjectModel;

namespace Aimmy.UI.Avalonia.ViewModels;

public sealed class ModelSettingsViewModel : ObservableObject
{
    private string _modelPath = string.Empty;
    private double _confidenceThreshold;
    private string _targetClass = "Best Confidence";
    private int _imageSize = 640;
    private bool _isDynamicModel;
    private string _metadataMessage = "Model metadata not loaded.";

    public ObservableCollection<string> TargetClassOptions { get; } = new();

    public string ModelPath
    {
        get => _modelPath;
        set => SetProperty(ref _modelPath, value);
    }

    public double ConfidenceThreshold
    {
        get => _confidenceThreshold;
        set => SetProperty(ref _confidenceThreshold, value);
    }

    public string TargetClass
    {
        get => _targetClass;
        set => SetProperty(ref _targetClass, value);
    }

    public int ImageSize
    {
        get => _imageSize;
        set => SetProperty(ref _imageSize, value);
    }

    public bool IsDynamicModel
    {
        get => _isDynamicModel;
        private set => SetProperty(ref _isDynamicModel, value);
    }

    public string MetadataMessage
    {
        get => _metadataMessage;
        private set => SetProperty(ref _metadataMessage, value);
    }

    public void Load(AimmyConfig config)
    {
        ModelPath = config.Model.ModelPath;
        ConfidenceThreshold = config.Model.ConfidenceThreshold;
        TargetClass = string.IsNullOrWhiteSpace(config.Model.TargetClass) ? "Best Confidence" : config.Model.TargetClass;
        ImageSize = config.Model.ImageSize;
        EnsureTargetClassOption(TargetClass);
        EnsureTargetClassOption("Best Confidence");
    }

    public void Apply(AimmyConfig config)
    {
        config.Model.ModelPath = ModelPath;
        config.Model.ConfidenceThreshold = (float)ConfidenceThreshold;
        config.Model.TargetClass = string.IsNullOrWhiteSpace(TargetClass) ? "Best Confidence" : TargetClass;
        config.Model.ImageSize = ImageSize;
    }

    public void ApplyMetadata(ModelMetadataInfo metadata)
    {
        IsDynamicModel = metadata.IsDynamic;
        MetadataMessage = metadata.Message;

        if (metadata.FixedImageSize.HasValue && metadata.FixedImageSize.Value > 0)
        {
            ImageSize = metadata.FixedImageSize.Value;
        }

        var currentSelection = TargetClass;
        TargetClassOptions.Clear();
        TargetClassOptions.Add("Best Confidence");
        foreach (var className in metadata.Classes.OrderBy(v => v, StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(className))
            {
                continue;
            }

            if (!ContainsIgnoreCase(className))
            {
                TargetClassOptions.Add(className);
            }
        }

        if (!ContainsIgnoreCase(currentSelection))
        {
            TargetClass = "Best Confidence";
            return;
        }

        TargetClass = currentSelection;
    }

    private void EnsureTargetClassOption(string targetClass)
    {
        if (string.IsNullOrWhiteSpace(targetClass))
        {
            return;
        }

        if (!ContainsIgnoreCase(targetClass))
        {
            TargetClassOptions.Add(targetClass);
        }
    }

    private bool ContainsIgnoreCase(string value)
    {
        return TargetClassOptions.Any(option => string.Equals(option, value, StringComparison.OrdinalIgnoreCase));
    }
}
