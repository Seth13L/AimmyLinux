using Aimmy.Core.Capabilities;
using Aimmy.Core.Config;
using Aimmy.Core.Enums;
using Aimmy.Platform.Abstractions.Models;
using Aimmy.UI.Avalonia.ViewModels;
using Xunit;

namespace Aimmy.Core.Tests;

public sealed class StoreUpdateViewModelTests
{
    [Fact]
    public void LoadAndApply_RoundTripsSettingsAndCapabilities()
    {
        var config = AimmyConfig.CreateDefault();
        config.Store.Enabled = false;
        config.Store.ModelsApiUrl = "https://example.com/api/models";
        config.Store.ConfigsApiUrl = "https://example.com/api/configs";
        config.Store.LocalModelsDirectory = "models-cache";
        config.Store.LocalConfigsDirectory = "configs-cache";
        config.Update.Enabled = true;
        config.Update.Channel = "beta";
        config.Update.PackageType = "rpm";
        config.Update.ReleasesApiUrl = "https://example.com/releases";

        var capabilities = RuntimeCapabilities.CreateDefault();
        capabilities.Set("ModelStore", FeatureState.Unavailable, true, "Store unavailable.");
        capabilities.Set("Updater", FeatureState.Enabled, true, "Updater degraded.");

        var vm = new StoreUpdateViewModel();
        vm.Load(config, capabilities);

        Assert.False(vm.StoreEnabled);
        Assert.Equal("https://example.com/api/models", vm.ModelsApiUrl);
        Assert.Equal("https://example.com/api/configs", vm.ConfigsApiUrl);
        Assert.Equal("models-cache", vm.LocalModelsDirectory);
        Assert.Equal("configs-cache", vm.LocalConfigsDirectory);
        Assert.True(vm.UpdateEnabled);
        Assert.Equal("beta", vm.UpdateChannel);
        Assert.Equal("rpm", vm.UpdatePackageType);
        Assert.Equal("https://example.com/releases", vm.ReleasesApiUrl);
        Assert.False(vm.StoreServiceAvailable);
        Assert.True(vm.UpdateServiceAvailable);
        Assert.Contains("Store unavailable", vm.StoreCapabilityMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Updater degraded", vm.UpdateCapabilityMessage, StringComparison.OrdinalIgnoreCase);

        vm.StoreEnabled = true;
        vm.ModelsApiUrl = "https://new.example.com/models";
        vm.ConfigsApiUrl = "https://new.example.com/configs";
        vm.LocalModelsDirectory = "bin/models2";
        vm.LocalConfigsDirectory = "bin/configs2";
        vm.UpdateEnabled = false;
        vm.UpdateChannel = "stable";
        vm.UpdatePackageType = "deb";
        vm.ReleasesApiUrl = "https://new.example.com/releases";

        vm.Apply(config);

        Assert.True(config.Store.Enabled);
        Assert.Equal("https://new.example.com/models", config.Store.ModelsApiUrl);
        Assert.Equal("https://new.example.com/configs", config.Store.ConfigsApiUrl);
        Assert.Equal("bin/models2", config.Store.LocalModelsDirectory);
        Assert.Equal("bin/configs2", config.Store.LocalConfigsDirectory);
        Assert.False(config.Update.Enabled);
        Assert.Equal("stable", config.Update.Channel);
        Assert.Equal("deb", config.Update.PackageType);
        Assert.Equal("https://new.example.com/releases", config.Update.ReleasesApiUrl);
    }

    [Fact]
    public void SetEntries_SortsByNameAndSelectsFirstEntry()
    {
        var vm = new StoreUpdateViewModel();
        var unsorted = new[]
        {
            new ModelStoreEntry("zeta.onnx", "https://example.com/zeta.onnx", "model"),
            new ModelStoreEntry("Alpha.onnx", "https://example.com/alpha.onnx", "model"),
            new ModelStoreEntry("beta.onnx", "https://example.com/beta.onnx", "model")
        };

        vm.SetModelEntries(unsorted);
        vm.SetConfigEntries(unsorted);

        Assert.Equal("Alpha.onnx", vm.ModelEntries[0].Name);
        Assert.Equal("beta.onnx", vm.ModelEntries[1].Name);
        Assert.Equal("zeta.onnx", vm.ModelEntries[2].Name);
        Assert.Equal("Alpha.onnx", vm.SelectedModelEntry?.Name);
        Assert.Equal("Alpha.onnx", vm.SelectedConfigEntry?.Name);
    }

    [Fact]
    public void ApplyUpdateResult_StoresUpdateFields()
    {
        var vm = new StoreUpdateViewModel();
        var result = new UpdateCheckResult(
            true,
            "3.0.0",
            "3.1.0",
            "https://example.com/aimmy_3.1.0_amd64.deb",
            "New package available.");

        vm.ApplyUpdateResult(result);

        Assert.True(vm.UpdateAvailable);
        Assert.Equal("3.1.0", vm.LatestVersion);
        Assert.Equal("https://example.com/aimmy_3.1.0_amd64.deb", vm.UpdateDownloadUrl);
        Assert.Equal("New package available.", vm.UpdateNotes);
    }
}
