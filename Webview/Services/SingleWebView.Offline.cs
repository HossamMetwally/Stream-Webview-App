using System.Net.Http;
using System.Diagnostics;
using Microsoft.Maui.Networking;
using Microsoft.Maui.ApplicationModel;

namespace Webview;

public partial class SingleWebView
{
    private bool _isConnectivitySubscribed;

    // Shared HttpClient for pinging the server
    private static readonly HttpClient _pingClient = new()
    {
        Timeout = TimeSpan.FromSeconds(5)
    };

    private bool HasInternet() =>
        Connectivity.Current.NetworkAccess == NetworkAccess.Internet;

    private async Task<bool> IsServerReachableAsync()
    {
        try
        {
            var resp = await _pingClient.GetAsync(
                "http://74.243.216.77:49110/",
                HttpCompletionOption.ResponseHeadersRead);

            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private void ShowOffline(string message)
    {
        OfflineMessageLabel.Text = message;

        OfflineOverlay.IsVisible = true;
        LoadingOverlay.IsVisible = false;
        DashboardView.IsVisible = false;
    }

    private void HideOffline()
    {
        OfflineOverlay.IsVisible = false;
    }

    private async Task LoadDashboardAsync()
    {
        // Pre-check network + server
        if (!HasInternet())
        {
            ShowOffline("No internet connection. Please check your network and tap Retry.");
            return;
        }

        if (!await IsServerReachableAsync())
        {
            ShowOffline("Couldn't reach the dashboard server. Please check it and tap Retry.");
            return;
        }

        _pageLoadedOnce = false;
        LoadingOverlay.IsVisible = true;
        DashboardView.IsVisible = false;
        HideOffline();

        Title = "Dashboards";
        _perfWatch = Stopwatch.StartNew();

#if ANDROID
        var token = await TryGetTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
        {
            var mgr = Android.Webkit.CookieManager.Instance;
            mgr.SetAcceptCookie(true);
            mgr.SetCookie(
                "http://74.243.216.77:49110",
                $"Token={token}; Path=/"
            );
            Android.Webkit.CookieManager.Instance.Flush();
        }
#endif

        DashboardView.Source = BaseSpaUrl;
        Debug.WriteLine($"[Perf] After setting Source: {_perfWatch?.ElapsedMilliseconds} ms");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (!_isConnectivitySubscribed)
        {
            Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;
            _isConnectivitySubscribed = true;
        }

        await LoadDashboardAsync();

        // Whenever this page becomes visible again, reset the active tab
        SetActiveTab("Home");
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();

        if (_isConnectivitySubscribed)
        {
            Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
            _isConnectivitySubscribed = false;
        }
    }

    private void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (e.NetworkAccess == NetworkAccess.Internet)
            {
                if (OfflineOverlay.IsVisible)
                    HideOffline();
            }
            else
            {
                ShowOffline("Connection lost. Please check your network and tap Retry.");
            }
        });
    }

    // Retry button from the offline overlay
    private async void OnRetryClicked(object sender, EventArgs e)
    {
        await LoadDashboardAsync();
    }

    // Reload bottom tab – uses same logic
    //private async void OnReloadTabTapped(object sender, TappedEventArgs e)
    //{
    //    SetActiveTab("Reload");
    //    _pageLoadedOnce = false;
    //    LoadingOverlay.IsVisible = true;
    //    DashboardView.IsVisible = false;
    //    await LoadDashboardAsync();
    //}



}
