using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.ApplicationModel.Communication;

namespace Webview;

public partial class AboutPage : ContentPage
{
    private const string CompanyName = "Stream Controls";
    private const string WebsiteUrl = "https://streamcontrols.com/";
    private const string SupportEmail = "info@streamcontrols.com\r\n";

    public AboutPage()
    {
        InitializeComponent();

        // Fill labels using real app metadata
        AppNameLabel.Text = AppInfo.Current.Name;
        VersionLabel.Text = $"Version {AppInfo.Current.VersionString} (Build {AppInfo.Current.BuildString})";
        CompanyLabel.Text = CompanyName;
    }

    private async void OnWebsiteClicked(object sender, EventArgs e)
    {
        try
        {
            await Browser.Default.OpenAsync(new Uri(WebsiteUrl), new BrowserLaunchOptions
            {
                LaunchMode = BrowserLaunchMode.SystemPreferred
            });
        }
        catch
        {
            await DisplayAlert("Error", "Unable to open the website.", "OK");
        }
    }


    private async void OnSupportEmailClicked(object sender, EventArgs e)
    {
        var recipient = "info@streamcontrols.com";
        var subject = $"{AppInfo.Current.Name} Support";
        var body = string.Empty;
        var mailto = $"mailto:{recipient}?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}";

        try
        {
            var message = new EmailMessage { Subject = subject, Body = body, To = { recipient } };
            await Email.Default.ComposeAsync(message);
        }
        catch (Exception ex)
        {
            // try mailto with Launcher as a second fallback
            try
            {
                await Launcher.Default.OpenAsync(mailto);
                return;
            }
            catch
            {
                // final fallback: copy to clipboard and show user an error including exception for debugging
                try { await Clipboard.SetTextAsync(recipient); } catch { /* ignore */ }

                await DisplayAlert(
                    "Support email",
                    $"Couldn't open an email app.\n\nPlease send an email to:\n{recipient}\n\n(We've copied it to your clipboard if supported.)\n\nDebug: {ex.Message}",
                    "OK");
            }
        }
    }

    //private async void OnSupportEmailClicked(object sender, EventArgs e)
    //{
    //    var recipient = "info@streamcontrols.com";
    //    var subject = $"{AppInfo.Current.Name} Support";
    //    var body = string.Empty;

    //    try
    //    {
    //        var message = new EmailMessage
    //        {
    //            Subject = subject,
    //            Body = body,
    //            To = { recipient }
    //        };

    //        await Email.Default.ComposeAsync(message);
    //    }
    //    catch
    //    {
    //        // Fallback: copy email and show to user
    //        try { await Clipboard.SetTextAsync(recipient); } catch { /* ignore */ }

    //        await DisplayAlert(
    //            "Support email",
    //            $"Couldn't open an email app.\n\nPlease send an email to:\n{recipient}\n\n(We've copied it to your clipboard if supported.)",
    //            "OK");
    //    }
    //}


    private async void OnShareClicked(object sender, EventArgs e)
    {
        await Share.Default.RequestAsync(new ShareTextRequest
        {
            Title = "Share App",
            Text = $"Check out {AppInfo.Current.Name}: {WebsiteUrl}",
            Uri = WebsiteUrl
        });
    }
}
