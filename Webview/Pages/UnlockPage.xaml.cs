using System.Diagnostics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Webview.Services;

namespace Webview;

public partial class UnlockPage : ContentPage
{
    private readonly IBiometricService _biometrics = new BiometricService();
    private bool _autoTried;

    public UnlockPage()
    {
        InitializeComponent();

#if IOS
        SubtitleLabel.Text = "Use Face ID to continue.";
        UnlockButton.Text = "Use Face ID";
#else
        SubtitleLabel.Text = "Use fingerprint to continue.";
        UnlockButton.Text = "Use fingerprint";
#endif
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Auto-trigger once when page appears
        if (!_autoTried)
        {
            _autoTried = true;
            await TryUnlockAsync();
        }
    }

    private async Task TryUnlockAsync()
    {
        try
        {
            var token = await SecureStorage.GetAsync("auth_token");
            if (string.IsNullOrEmpty(token))
            {
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            var available = await _biometrics.IsAvailableAsync();
            if (!available)
            {
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            var ok = await _biometrics.AuthenticateAsync("Unlock the app");
            if (ok)
            {
                await Shell.Current.GoToAsync("//SingleWebView");
            }
            // if not ok (cancel/fail) -> stay on this screen, user can tap "Use password instead"
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Unlock] Error: {ex}");
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }

    private async void OnUnlockClicked(object sender, EventArgs e)
    {
        await TryUnlockAsync();
    }

    private async void OnUsePasswordClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//LoginPage");
    }
}
