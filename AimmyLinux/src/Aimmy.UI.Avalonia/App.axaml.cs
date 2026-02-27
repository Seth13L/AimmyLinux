using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Aimmy.UI.Avalonia.Windows;

namespace Aimmy.UI.Avalonia;

public sealed class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (!ConfigurationEditorLauncher.TryConsumePendingLaunchOptions(out var launchOptions))
            {
                throw new InvalidOperationException("Configuration editor launch options were not provided.");
            }

            desktop.MainWindow = new MainWindow(launchOptions);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
