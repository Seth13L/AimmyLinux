using Aimmy.Core.Config;
using Aimmy.UI.Avalonia.ViewModels;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class DataCollectionSettingsViewModelTests
{
    [Fact]
    public void LoadAndApply_RoundTripsDataCollectionSettings()
    {
        var config = AimmyConfig.CreateDefault();
        config.DataCollection.CollectDataWhilePlaying = true;
        config.DataCollection.AutoLabelData = true;
        config.DataCollection.ImagesDirectory = "captures/images";
        config.DataCollection.LabelsDirectory = "captures/labels";

        var vm = new DataCollectionSettingsViewModel();
        vm.Load(config);

        Assert.True(vm.CollectDataWhilePlaying);
        Assert.True(vm.AutoLabelData);
        Assert.Equal("captures/images", vm.ImagesDirectory);
        Assert.Equal("captures/labels", vm.LabelsDirectory);

        vm.CollectDataWhilePlaying = false;
        vm.AutoLabelData = false;
        vm.ImagesDirectory = "alt/images";
        vm.LabelsDirectory = "alt/labels";

        vm.Apply(config);

        Assert.False(config.DataCollection.CollectDataWhilePlaying);
        Assert.False(config.DataCollection.AutoLabelData);
        Assert.Equal("alt/images", config.DataCollection.ImagesDirectory);
        Assert.Equal("alt/labels", config.DataCollection.LabelsDirectory);
    }
}
