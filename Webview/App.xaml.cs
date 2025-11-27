using Microsoft.Maui.Controls;

namespace Webview;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        UserAppTheme = AppTheme.Light;

        MainPage = new AppShell();
    }
}
