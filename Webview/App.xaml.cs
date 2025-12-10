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


    protected override async void OnStart()
    {
        base.OnStart();

        // Check if we have a token and biometrics are enabled
        var token = await SecureStorage.GetAsync("auth_token");
        var useBiometrics = Preferences.Get("use_biometrics", false);

        if (!string.IsNullOrEmpty(token) && useBiometrics)
        {
            // Returning user with biometrics → go to Unlock screen
            await Shell.Current.GoToAsync("//UnlockPage");
        }
        else
        {
            // Fresh user / no token / biometrics disabled → go to normal Login
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
}
