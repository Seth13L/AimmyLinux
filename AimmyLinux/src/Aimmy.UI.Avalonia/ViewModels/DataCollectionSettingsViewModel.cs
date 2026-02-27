using Aimmy.Core.Config;

namespace Aimmy.UI.Avalonia.ViewModels;

public sealed class DataCollectionSettingsViewModel
{
    public bool CollectDataWhilePlaying { get; set; }
    public bool AutoLabelData { get; set; }
    public string ImagesDirectory { get; set; } = string.Empty;
    public string LabelsDirectory { get; set; } = string.Empty;

    public void Load(AimmyConfig config)
    {
        CollectDataWhilePlaying = config.DataCollection.CollectDataWhilePlaying;
        AutoLabelData = config.DataCollection.AutoLabelData;
        ImagesDirectory = config.DataCollection.ImagesDirectory;
        LabelsDirectory = config.DataCollection.LabelsDirectory;
    }

    public void Apply(AimmyConfig config)
    {
        config.DataCollection.CollectDataWhilePlaying = CollectDataWhilePlaying;
        config.DataCollection.AutoLabelData = AutoLabelData;
        config.DataCollection.ImagesDirectory = ImagesDirectory;
        config.DataCollection.LabelsDirectory = LabelsDirectory;
    }
}
