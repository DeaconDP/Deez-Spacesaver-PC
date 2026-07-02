using Microsoft.UI.Xaml;
using Spacesaver.Helpers;
using Spacesaver.Services;

namespace Spacesaver;

public partial class App : Application
{
    private Window? _window;

    public App()
    {
        RequestedTheme = ThemeHelper.ResolveTheme(AppServices.Settings.ThemePreference);
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
