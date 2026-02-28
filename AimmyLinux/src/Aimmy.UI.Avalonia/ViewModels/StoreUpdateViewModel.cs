using Aimmy.Core.Capabilities;
using Aimmy.Core.Config;
using Aimmy.Core.Enums;
using Aimmy.Platform.Abstractions.Models;
using System.Collections.ObjectModel;

namespace Aimmy.UI.Avalonia.ViewModels;

public sealed class StoreUpdateViewModel : ObservableObject
{
    private ModelStoreEntry? _selectedModelEntry;
    private ModelStoreEntry? _selectedConfigEntry;
    private bool _storeEnabled;
    private string _modelsApiUrl = string.Empty;
    private string _configsApiUrl = string.Empty;
    private string _localModelsDirectory = string.Empty;
    private string _localConfigsDirectory = string.Empty;
    private bool _updateEnabled;
    private string _updateChannel = string.Empty;
    private string _updatePackageType = string.Empty;
    private string _releasesApiUrl = string.Empty;
    private string _currentVersion = "0.0.0";
    private bool _storeServiceAvailable = true;
    private bool _updateServiceAvailable = true;
    private string _storeCapabilityMessage = string.Empty;
    private string _updateCapabilityMessage = string.Empty;
    private bool _updateAvailable;
    private string _latestVersion = string.Empty;
    private string _updateDownloadUrl = string.Empty;
    private string _updateNotes = string.Empty;

    public ObservableCollection<ModelStoreEntry> ModelEntries { get; } = new();
    public ObservableCollection<ModelStoreEntry> ConfigEntries { get; } = new();

    public ModelStoreEntry? SelectedModelEntry
    {
        get => _selectedModelEntry;
        set => SetProperty(ref _selectedModelEntry, value);
    }

    public ModelStoreEntry? SelectedConfigEntry
    {
        get => _selectedConfigEntry;
        set => SetProperty(ref _selectedConfigEntry, value);
    }

    public bool StoreEnabled
    {
        get => _storeEnabled;
        set => SetProperty(ref _storeEnabled, value);
    }

    public string ModelsApiUrl
    {
        get => _modelsApiUrl;
        set => SetProperty(ref _modelsApiUrl, value);
    }

    public string ConfigsApiUrl
    {
        get => _configsApiUrl;
        set => SetProperty(ref _configsApiUrl, value);
    }

    public string LocalModelsDirectory
    {
        get => _localModelsDirectory;
        set => SetProperty(ref _localModelsDirectory, value);
    }

    public string LocalConfigsDirectory
    {
        get => _localConfigsDirectory;
        set => SetProperty(ref _localConfigsDirectory, value);
    }

    public bool UpdateEnabled
    {
        get => _updateEnabled;
        set => SetProperty(ref _updateEnabled, value);
    }

    public string UpdateChannel
    {
        get => _updateChannel;
        set => SetProperty(ref _updateChannel, value);
    }

    public string UpdatePackageType
    {
        get => _updatePackageType;
        set => SetProperty(ref _updatePackageType, value);
    }

    public string ReleasesApiUrl
    {
        get => _releasesApiUrl;
        set => SetProperty(ref _releasesApiUrl, value);
    }

    public string CurrentVersion
    {
        get => _currentVersion;
        set => SetProperty(ref _currentVersion, value);
    }

    public bool StoreServiceAvailable
    {
        get => _storeServiceAvailable;
        private set => SetProperty(ref _storeServiceAvailable, value);
    }

    public bool UpdateServiceAvailable
    {
        get => _updateServiceAvailable;
        private set => SetProperty(ref _updateServiceAvailable, value);
    }

    public string StoreCapabilityMessage
    {
        get => _storeCapabilityMessage;
        private set => SetProperty(ref _storeCapabilityMessage, value);
    }

    public string UpdateCapabilityMessage
    {
        get => _updateCapabilityMessage;
        private set => SetProperty(ref _updateCapabilityMessage, value);
    }

    public bool UpdateAvailable
    {
        get => _updateAvailable;
        private set => SetProperty(ref _updateAvailable, value);
    }

    public string LatestVersion
    {
        get => _latestVersion;
        private set => SetProperty(ref _latestVersion, value);
    }

    public string UpdateDownloadUrl
    {
        get => _updateDownloadUrl;
        private set => SetProperty(ref _updateDownloadUrl, value);
    }

    public string UpdateNotes
    {
        get => _updateNotes;
        private set => SetProperty(ref _updateNotes, value);
    }

    public void Load(AimmyConfig config, RuntimeCapabilities runtimeCapabilities)
    {
        StoreEnabled = config.Store.Enabled;
        ModelsApiUrl = config.Store.ModelsApiUrl;
        ConfigsApiUrl = config.Store.ConfigsApiUrl;
        LocalModelsDirectory = config.Store.LocalModelsDirectory;
        LocalConfigsDirectory = config.Store.LocalConfigsDirectory;

        UpdateEnabled = config.Update.Enabled;
        UpdateChannel = config.Update.Channel;
        UpdatePackageType = config.Update.PackageType;
        ReleasesApiUrl = config.Update.ReleasesApiUrl;

        var storeCapability = runtimeCapabilities.Get("ModelStore");
        StoreServiceAvailable = storeCapability.State != FeatureState.Unavailable;
        StoreCapabilityMessage = string.IsNullOrWhiteSpace(storeCapability.Message)
            ? "Model/config store service state is unknown."
            : storeCapability.Message;

        var updaterCapability = runtimeCapabilities.Get("Updater");
        UpdateServiceAvailable = updaterCapability.State != FeatureState.Unavailable;
        UpdateCapabilityMessage = string.IsNullOrWhiteSpace(updaterCapability.Message)
            ? "Updater service state is unknown."
            : updaterCapability.Message;
    }

    public void Apply(AimmyConfig config)
    {
        config.Store.Enabled = StoreEnabled;
        config.Store.ModelsApiUrl = ModelsApiUrl;
        config.Store.ConfigsApiUrl = ConfigsApiUrl;
        config.Store.LocalModelsDirectory = LocalModelsDirectory;
        config.Store.LocalConfigsDirectory = LocalConfigsDirectory;

        config.Update.Enabled = UpdateEnabled;
        config.Update.Channel = UpdateChannel;
        config.Update.PackageType = UpdatePackageType;
        config.Update.ReleasesApiUrl = ReleasesApiUrl;
    }

    public void SetModelEntries(IEnumerable<ModelStoreEntry> entries)
    {
        ModelEntries.Clear();
        foreach (var entry in entries.OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase))
        {
            ModelEntries.Add(entry);
        }

        SelectedModelEntry = ModelEntries.FirstOrDefault();
    }

    public void SetConfigEntries(IEnumerable<ModelStoreEntry> entries)
    {
        ConfigEntries.Clear();
        foreach (var entry in entries.OrderBy(v => v.Name, StringComparer.OrdinalIgnoreCase))
        {
            ConfigEntries.Add(entry);
        }

        SelectedConfigEntry = ConfigEntries.FirstOrDefault();
    }

    public void ApplyUpdateResult(UpdateCheckResult result)
    {
        UpdateAvailable = result.UpdateAvailable;
        LatestVersion = result.LatestVersion;
        UpdateDownloadUrl = result.DownloadUrl;
        UpdateNotes = result.Notes;
    }
}
