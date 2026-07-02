using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Spacesaver.Helpers;
using Spacesaver.Services;

namespace Spacesaver.Pages;

public sealed partial class WelcomePage : Page
{
    public WelcomePage()
    {
        InitializeComponent();
        AnimationHelper.WirePageLoaded(this, [PageHeaderControl, GetStartedButton]);
    }

    private void GetStarted_Click(object sender, RoutedEventArgs e)
    {
        AnimationHelper.Pulse(GetStartedButton);
        AppServices.Settings.HasCompletedOnboarding = true;
        NavigationHelper.NavigateToDashboard(this);
    }
}
