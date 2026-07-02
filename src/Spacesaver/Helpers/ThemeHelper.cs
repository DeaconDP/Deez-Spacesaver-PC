using Microsoft.UI.Xaml;
using Spacesaver.Models;
using Windows.UI.ViewManagement;

namespace Spacesaver.Helpers;

public static class ThemeHelper
{
    public static ApplicationTheme ResolveTheme(ThemePreference preference) =>
        preference switch
        {
            ThemePreference.Light => ApplicationTheme.Light,
            ThemePreference.Dark => ApplicationTheme.Dark,
            _ => GetSystemTheme()
        };

    public static void ApplyTheme(ThemePreference preference)
    {
        if (Application.Current is not Application app)
            return;

        var theme = ResolveTheme(preference);

        if (app.RequestedTheme == theme)
            return;

        app.RequestedTheme = theme;
    }

    public static ApplicationTheme GetSystemTheme()
    {
        var background = new UISettings().GetColorValue(UIColorType.Background);
        var brightness = (background.R * 0.299 + background.G * 0.587 + background.B * 0.114) / 255.0;
        return brightness > 0.5 ? ApplicationTheme.Light : ApplicationTheme.Dark;
    }
}
