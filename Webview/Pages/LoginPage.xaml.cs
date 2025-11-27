using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System.Diagnostics;
using Webview.Services;

namespace Webview;

public partial class LoginPage : ContentPage
{
    private readonly ApiClient _api = new();

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
        StatusLabel.Text = "Signing in…";
        var sw = Stopwatch.StartNew();


        try
        {
            var token = await _api.AuthenticateAsync(
                UserEntry.Text?.Trim() ?? string.Empty,
                PassEntry.Text ?? string.Empty);
            Debug.WriteLine($"[Perf] AuthenticateAsync: {sw.ElapsedMilliseconds} ms");


            // Still store for convenience
            Preferences.Set("auth_token", token);
            await SecureStorage.SetAsync("auth_token", token);

            StatusLabel.Text = "Login successful";


            if (Shell.Current is AppShell shell)
            {
                await shell.EnsureDashboardsLoadedAsync();
                Debug.WriteLine($"[Perf] EnsureDashboardsLoadedAsync (total so far): {sw.ElapsedMilliseconds} ms");

            }

            // Go directly to the single WebView host page
            await Shell.Current.GoToAsync("//SingleWebView");
            Debug.WriteLine($"[Perf] After GoToAsync: {sw.ElapsedMilliseconds} ms");

        }
        catch (Exception ex)
        {
            StatusLabel.Text = $"Error: {ex.Message}";
        }
    }

    //private async void OnLoginClicked(object sender, EventArgs e)
    //{
    //    StatusLabel.Text = "Signing in�";
    //    try
    //    {
    //        var token = await _api.AuthenticateAsync(
    //            UserEntry.Text?.Trim() ?? string.Empty,
    //            PassEntry.Text ?? string.Empty);

    //        Preferences.Set("auth_token", token);
    //        StatusLabel.Text = "Login successful";

    //        //await Shell.Current.GoToAsync("//Dashboards");
    //        await Shell.Current.GoToAsync("//SingleWebView");

    //    }
    //    catch (Exception ex)
    //    {
    //        StatusLabel.Text = $"Error: {ex.Message}";
    //    }
    //}
}
