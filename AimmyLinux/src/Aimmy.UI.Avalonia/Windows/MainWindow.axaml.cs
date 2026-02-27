using Avalonia.Controls;
using Avalonia.Interactivity;
using Aimmy.Core.Config;
using Aimmy.UI.Avalonia.ViewModels;

namespace Aimmy.UI.Avalonia.Windows;

public partial class MainWindow : Window
{
    private readonly AimmyConfig _config;
    private readonly ConfigurationEditorViewModel _viewModel;
    private readonly Action<AimmyConfig> _saveConfigCallback;
    private readonly CheckBox _useDiscoveredDisplayCheckBox;
    private readonly ComboBox _discoveredDisplayComboBox;
    private readonly Grid _manualDisplayGrid;
    private readonly TextBlock _statusTextBlock;

    public MainWindow(ConfigurationEditorLaunchOptions launchOptions)
    {
        ArgumentNullException.ThrowIfNull(launchOptions);

        _config = launchOptions.Config;
        _saveConfigCallback = launchOptions.SaveConfigCallback;
        _viewModel = new ConfigurationEditorViewModel();
        _viewModel.Load(_config, launchOptions.Displays);

        InitializeComponent();
        DataContext = _viewModel;

        _useDiscoveredDisplayCheckBox = FindRequiredControl<CheckBox>("UseDiscoveredDisplayCheckBox");
        _discoveredDisplayComboBox = FindRequiredControl<ComboBox>("DiscoveredDisplayComboBox");
        _manualDisplayGrid = FindRequiredControl<Grid>("ManualDisplayGrid");
        _statusTextBlock = FindRequiredControl<TextBlock>("StatusTextBlock");

        var configPathTextBlock = FindRequiredControl<TextBlock>("ConfigPathTextBlock");
        configPathTextBlock.Text = $"Config: {launchOptions.ConfigPath}";

        _statusTextBlock.Text = launchOptions.Displays.Count == 0
            ? "No X11 displays discovered. Use manual geometry values."
            : $"Loaded {launchOptions.Displays.Count} discovered display(s).";

        SyncDisplaySelectionState();
    }

    private T FindRequiredControl<T>(string name) where T : Control
    {
        return this.FindControl<T>(name) ?? throw new InvalidOperationException($"Missing control '{name}'.");
    }

    private void OnDisplayModeChanged(object? sender, RoutedEventArgs e)
    {
        SyncDisplaySelectionState();
    }

    private void OnApplyClicked(object? sender, RoutedEventArgs e)
    {
        _viewModel.Apply(_config);
        _statusTextBlock.Text = $"Applied in memory at {DateTime.Now:HH:mm:ss}.";
    }

    private void OnSaveClicked(object? sender, RoutedEventArgs e)
    {
        _viewModel.Apply(_config);
        _saveConfigCallback(_config);
        _statusTextBlock.Text = $"Saved config at {DateTime.Now:HH:mm:ss}.";
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SyncDisplaySelectionState()
    {
        var hasDiscoveredDisplays = _viewModel.DisplaySelection.DisplayOptions.Count > 0;
        if (!hasDiscoveredDisplays)
        {
            _viewModel.DisplaySelection.UseDiscoveredDisplay = false;
            _useDiscoveredDisplayCheckBox.IsChecked = false;
        }

        _useDiscoveredDisplayCheckBox.IsEnabled = hasDiscoveredDisplays;
        var useDiscoveredDisplay = hasDiscoveredDisplays && (_useDiscoveredDisplayCheckBox.IsChecked ?? false);
        _discoveredDisplayComboBox.IsEnabled = useDiscoveredDisplay;
        _manualDisplayGrid.IsEnabled = !useDiscoveredDisplay;
    }
}
