using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace Webview.Pages;

public partial class SettingsPage : ContentPage
{
    public SettingsPage()
    {
        InitializeComponent();

        // simple version text
        VersionLabel.Text = AppInfo.Current.VersionString;
    }

    private async void OnClearLoginClicked(object sender, EventArgs e)
    {
        try
        {
            // Remove stored auth
            SecureStorage.Remove("auth_payload_raw");
            SecureStorage.Remove("auth_token");
        }
        catch
        {
            // ignore SecureStorage exceptions
        }

        Preferences.Remove("auth_token");

        // (Optional) small confirmation – you can keep or remove
        // await DisplayAlert("Done", "Saved login has been cleared.", "OK");

        // Go back to login screen
        await Shell.Current.GoToAsync("//LoginPage");
    }


    private async void OnContactSupportClicked(object sender, EventArgs e)
    {
        try
        {
            var message = new EmailMessage
            {
                Subject = "StreamControls – Support",
                Body = "",
                To = new List<string> { "info@streamcontrols.com" } // put your email
            };

            await Email.ComposeAsync(message);
        }
        catch (FeatureNotSupportedException)
        {
            await DisplayAlert("Error", "Email is not supported on this device.", "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", $"Could not open email. {ex.Message}", "OK");
        }
    }
}
