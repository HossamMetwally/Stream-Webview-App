using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Diagnostics;
using Webview.Services;
using Microsoft.Maui.Networking;
using System.Net.Http;
using System.Threading.Tasks;

namespace Webview;

public partial class LoginPage : ContentPage
{
    private readonly ApiClient _api = new();

    //  biometric helper
    private readonly IBiometricService _biometricService = new BiometricService();

    public LoginPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        StatusLabel.Text = string.Empty;   // hide old message when logged out

        // PassEntry.Text = string.Empty; later
     }

    private async void OnLoginClicked(object sender, EventArgs e)
    {
        StatusLabel.TextColor = Colors.Gray;
        StatusLabel.Text = "Signing in…";

        var sw = Stopwatch.StartNew();

        // 0) Device offline? -> show message and STOP
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            StatusLabel.TextColor = Colors.Red;
            StatusLabel.Text = "No internet connection.";
            return;
        }

        var user = UserEntry.Text?.Trim() ?? string.Empty;
        var pass = PassEntry.Text ?? string.Empty;

        if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
        {
            StatusLabel.TextColor = Colors.Red;
            StatusLabel.Text = "Enter username and password.";
            return;
        }

        try
        {
            var token = await _api.AuthenticateAsync(user, pass);
            Debug.WriteLine($"[Perf] AuthenticateAsync: {sw.ElapsedMilliseconds} ms");

            if (string.IsNullOrWhiteSpace(token))
            {
                StatusLabel.TextColor = Colors.Red;
                StatusLabel.Text = "Login failed. Please try again.";
                return;
            }

            // 🔐 Biometric preference: ask at most once
            bool enableBiometrics = Preferences.Get("use_biometrics", false);
            bool biometricsAsked = Preferences.Get("biometrics_asked", false);

            try
            {
                // Only ask once, and only if not already enabled
                if (!enableBiometrics && !biometricsAsked && await _biometricService.IsAvailableAsync())
                {
#if IOS
                const string title = "Quick unlock";
                const string message = "Use Face ID next time?";
#else
                    const string title = "Quick unlock";
                    const string message = "Use fingerprint next time?";
#endif

                    var useIt = await DisplayAlert(title, message, "Yes", "No");

                    // Mark that we already asked on this device
                    Preferences.Set("biometrics_asked", true);

                    if (useIt)
                    {
                        enableBiometrics = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Login] Biometric preference check failed: {ex}");
            }

            Preferences.Set("use_biometrics", enableBiometrics);

            StatusLabel.TextColor = Colors.Green;
            StatusLabel.Text = "Login successful";

            if (Shell.Current is AppShell shell)
            {
                await shell.EnsureDashboardsLoadedAsync();
                Debug.WriteLine($"[Perf] EnsureDashboardsLoadedAsync (total so far): {sw.ElapsedMilliseconds} ms");
            }

            await Shell.Current.GoToAsync("//SingleWebView");
            Debug.WriteLine($"[Perf] After GoToAsync: {sw.ElapsedMilliseconds} ms");
        }
        catch (HttpRequestException httpEx)
        {
            // Device has internet, but server is unreachable
            StatusLabel.TextColor = Colors.Red;
            StatusLabel.Text = "Can't reach the server.";
            Debug.WriteLine($"[Login] HttpRequestException: {httpEx}");
        }
        catch (TaskCanceledException tex)
        {
            // Likely a timeout
            StatusLabel.TextColor = Colors.Red;
            StatusLabel.Text = "Request timed out.";
            Debug.WriteLine($"[Login] Timeout: {tex}");
        }
        catch (Exception ex)
        {
            StatusLabel.TextColor = Colors.Red;
            StatusLabel.Text = "Unexpected error during login.";
            Debug.WriteLine($"[Login] Error: {ex}");
        }
    }



}
