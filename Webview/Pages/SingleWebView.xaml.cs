using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Networking;


namespace Webview;


public partial class SingleWebView : ContentPage
{
    private const string BaseSpaUrl = "http://74.243.216.77:49110/Dashboard/Views/index";

    // Ensure we don’t re-inject & reload in a loop
    //private bool _initialNavigationDone;
    private bool _pageLoadedOnce;
    private Stopwatch? _perfWatch;
   


    public SingleWebView()
    {
        InitializeComponent();
        Title = "Dashboards"; // shown until the user picks something

#if ANDROID
        Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);

        DashboardView.HandlerChanged += (_, __) =>
        {
            if (DashboardView.Handler?.PlatformView is Android.Webkit.WebView awv)
            {
                awv.Settings.JavaScriptEnabled = true;
                awv.Settings.DomStorageEnabled = true;

                var mgr = Android.Webkit.CookieManager.Instance;
                mgr.SetAcceptCookie(true);
                Android.Webkit.CookieManager.Instance.SetAcceptThirdPartyCookies(awv, true);
            }
        };
#endif

        DashboardView.Navigated += OnWebViewNavigated;
    }


    //  called from AppShell when user picks a dashboard


    public async Task SwitchDashboardAsync(string dashboardId,
                                           string? displayName = null,
                                           string? parentDisplayName = null)
    {
        if (string.IsNullOrWhiteSpace(dashboardId))
            return;

        // Keep Shell header in sync
        Title = displayName ?? dashboardId;

        var safeId = EscapeJs(dashboardId);
        var safeLabel = EscapeJs(displayName ?? dashboardId);
        var safeParent = EscapeJs(parentDisplayName ?? string.Empty);

        var js = @"
(function (parentLabel, childLabel, dashId) {
    try {
        var root = document.getElementById('DBsSideBar4') || document.body;

        function findLeafByText(root, text) {
            if (!text) return null;

            var walker = document.createTreeWalker(
                root,
                NodeFilter.SHOW_ELEMENT,
                {
                    acceptNode: function (node) {
                        if (!node || !node.textContent) return NodeFilter.FILTER_SKIP;
                        if (node.childElementCount > 0) return NodeFilter.FILTER_SKIP; // only leaves
                        return node.textContent.trim() === text
                            ? NodeFilter.FILTER_ACCEPT
                            : NodeFilter.FILTER_SKIP;
                    }
                }
            );
            return walker.nextNode();
        }

        function clickElement(el) {
            if (!el) return;
            if (typeof el.click === 'function') {
                el.click();
            } else {
                var evt = document.createEvent('MouseEvents');
                evt.initEvent('click', true, true);
                el.dispatchEvent(evt);
            }
        }

        // If we know the parent (e.g. 'Factories'), expand it first
        if (parentLabel && parentLabel.length) {
            var parentEl = findLeafByText(root, parentLabel);
            if (parentEl) {
                console.log('[MAUI] SwitchDashboardAsync -> click parent:', parentLabel);
                clickElement(parentEl);
            }
        }

        // Try clicking the actual dashboard item (child label or top-level name)
        var labelToUse = childLabel && childLabel.length ? childLabel : dashId;

        setTimeout(function () {
            var targetEl = findLeafByText(root, labelToUse);
            if (targetEl) {
                console.log('[MAUI] SwitchDashboardAsync -> click label:', labelToUse);
                clickElement(targetEl);
                return;
            }

            // Fallback: direct call (might still trigger their error if their JS is fragile)
            if (typeof window.GetAndUpdateDashBoard === 'function' && dashId && dashId.length) {
                console.log('[MAUI] SwitchDashboardAsync -> fallback GetAndUpdateDashBoard:', dashId);
                window.GetAndUpdateDashBoard(dashId);
            } else {
                console.log('[MAUI] SwitchDashboardAsync -> no target and no GetAndUpdateDashBoard');
            }
        }, 250);
    } catch (e) {
        console.log('SwitchDashboard error', e);
    }
})('" + safeParent + "','" + safeLabel + "','" + safeId + "');";

        await DashboardView.EvaluateJavaScriptAsync(js);
    }



    /// <summary>
    /// Called after each navigation inside the WebView.
    /// First time: inject localStorage + cookie and redirect to index.
    /// Later navigations: guard prevents doing it again.
    /// </summary>
    /// 
    private async void OnWebViewNavigated(object? sender, WebNavigatedEventArgs e)
    {
        // 0) Handle navigation failure first
        if (e.Result != WebNavigationResult.Success)
        {
            Debug.WriteLine($"[WebView] Navigation failed: {e.Result} | {e.Url}");

            string message;

            // If you created HasInternet(), you can use that instead of Connectivity here
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                message = "No internet connection. Please check your network and tap Retry.";
            }
            else
            {
                message = "Couldn't load the dashboard page. Please try again.";
            }

            ShowOffline(message);
            return;
        }

        // If we reached here, navigation succeeded → hide offline overlay if it was visible
        HideOffline();

        try
        {
            // 1) Get the stored raw auth payload (exact JSON from /api/Authenticate)
            var raw = await SecureStorage.GetAsync("auth_payload_raw");
            if (string.IsNullOrWhiteSpace(raw))
            {
                await DisplayAlertAsync("Auth", "Please sign in first.", "OK");
                await Shell.Current.GoToAsync("//LoginPage");
                return;
            }

            // 2) Get the token we want to use for the cookie
            var token = await TryGetTokenAsync();

            // 3) Inject into the SPA context (localStorage + cookie) – NO reload
            await InjectAuthIntoSpaAsync(raw, token);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("OnWebViewNavigated error: " + ex);
        }

        // First time we consider the page “ready” visually
        if (!_pageLoadedOnce)
        {
            _pageLoadedOnce = true;
            DashboardView.Opacity = 0;
            DashboardView.IsVisible = true;
            LoadingOverlay.IsVisible = false;

            // Smooth fade-in
            await DashboardView.FadeToAsync(1, 250, Easing.CubicOut);
        }
        else
        {
            // For later navigations, just hide loader instantly
            LoadingOverlay.IsVisible = false;
        }

        Debug.WriteLine($"[Perf] OnWebViewNavigated finished: {_perfWatch?.ElapsedMilliseconds} ms");
    }



    /// <summary>
    /// Injects:
    ///   - localStorage.AuthRepsonseSuccess  (raw JSON string)
    ///   - document.cookie Token=...         (JWT token if we have it)
    /// Then redirects ONCE to /Dashboard/Views/index so the SPA boots
    /// with the right context (like a real browser after login).
    /// </summary>
    /// 

    private async Task InjectAuthIntoSpaAsync(string rawJson, string? token)
    {
        var payloadJs = EscapeJs(rawJson ?? string.Empty);
        var tokenJs = EscapeJs(token ?? string.Empty);

        var js = @"
(function(payload, token) {
    try {
        // Guard: only once per page load
        if (window.__authInjectedFromApp === '1')
            return;
        window.__authInjectedFromApp = '1';

        // Same key the web login uses
        if (payload && payload.length) {
            window.localStorage.setItem('AuthRepsonseSuccess', payload);
        }

        // Cookie (backend already sees it from native side; this is just extra safety)
        if (token && token.length) {
            document.cookie = 'Token=' + token + '; path=/';
        }

        console.log('Auth injected from app (no reload).');
    } catch (e) {
        console.log('Auth inject error', e);
    }
})('" + payloadJs + "','" + tokenJs + "');";

        await DashboardView.EvaluateJavaScriptAsync(js);
    }




    /// <summary>
    /// Reads the token we stored after login (SecureStorage first, then Preferences).
    /// </summary>
    private static async Task<string?> TryGetTokenAsync()
    {
        try
        {
            var t = await SecureStorage.GetAsync("auth_token");
            if (!string.IsNullOrWhiteSpace(t))
                return t;
        }
        catch
        {
            // ignore
        }

        var p = Preferences.Get("auth_token", string.Empty);
        return string.IsNullOrWhiteSpace(p) ? null : p;
    }


    /// <summary>
    /// Safely escape a C# string for embedding into single-quoted JS string.
    /// </summary>
    private static string EscapeJs(string s) =>
        (s ?? string.Empty)
            .Replace("\\", "\\\\")
            .Replace("'", "\\'");





    //buttom menu methods
    private string _activeTab = "Home";

    private void SetActiveTab(string tabName)
    {
        _activeTab = tabName;

        var activeColor = Color.FromArgb("#6200EE");
        var inactiveColor = Color.FromArgb("#9CA3AF");

        HomeLabel.TextColor = tabName == "Home" ? activeColor : inactiveColor;
        AboutLabel.TextColor = tabName == "About" ? activeColor : inactiveColor;
        SettingsLabel.TextColor = tabName == "Settings" ? activeColor : inactiveColor;
        LogoutLabel.TextColor = tabName == "Logout" ? activeColor : inactiveColor;

        HomeIndicator.IsVisible = tabName == "Home";
        AboutIndicator.IsVisible = tabName == "About";
        SettingsIndicator.IsVisible = tabName == "Settings";
        LogoutIndicator.IsVisible = tabName == "Logout";
    }




    private void OnHomeTabTapped(object sender, TappedEventArgs e)
    {
        // If we're already on Home, ignore extra taps
        if (_activeTab == "Home")
            return;

        SetActiveTab("Home");

        // Go to default dashboard
        _pageLoadedOnce = false;
        LoadingOverlay.IsVisible = true;
        DashboardView.IsVisible = false;
        DashboardView.Source = BaseSpaUrl;
    }


    private void OnReloadTabTapped(object sender, TappedEventArgs e)
    {
        SetActiveTab("Reload");
        _pageLoadedOnce = false;
        LoadingOverlay.IsVisible = true;
        DashboardView.IsVisible = false;
        DashboardView.Reload();
    }

    private async void OnSettingsTabTapped(object sender, TappedEventArgs e)
    {
        SetActiveTab("Settings");
        await Shell.Current.GoToAsync(nameof(Webview.Pages.SettingsPage));
    }

    private async void OnAboutTabTapped(object? sender, EventArgs e)
    {
        SetActiveTab("AboutPage");
        await Shell.Current.GoToAsync(nameof(AboutPage));
    }

    private async void OnLogoutTabTapped(object sender, TappedEventArgs e)
    {
        SetActiveTab("Logout");

        try
        {
            SecureStorage.Remove("auth_payload_raw");
            SecureStorage.Remove("auth_token");
        }
        catch
        {
            // ignore
        }

        Preferences.Remove("auth_token");
        Preferences.Remove("use_biometrics");
        Preferences.Remove("biometrics_asked");

        await Shell.Current.GoToAsync("//LoginPage");
    }


}