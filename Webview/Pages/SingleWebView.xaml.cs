using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Webview.Services; 
namespace Webview;


public partial class SingleWebView : ContentPage
{
    private const string BaseSpaUrl = "http://74.243.216.77:49110/Dashboard/Views/index";

    // Ensure we don’t re-inject & reload in a loop
    private bool _initialNavigationDone;
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


    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Reset title for a fresh session; will be updated when user picks a dash
        Title = "Dashboards";

        // start perf timer for this load
        _perfWatch = Stopwatch.StartNew();

        // reset visual state every time
        _pageLoadedOnce = false;
        LoadingOverlay.IsVisible = true;
        DashboardView.IsVisible = false;

#if ANDROID
        // Put Token cookie in native WebView BEFORE loading the SPA
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

        // Load the SPA shell – first XHR will already include the cookie
        DashboardView.Source = BaseSpaUrl;
        Debug.WriteLine($"[Perf] After setting Source: {_perfWatch?.ElapsedMilliseconds} ms");
    }


    //    protected override async void OnAppearing()
    //    {
    //        base.OnAppearing();

    //        if (_initialNavigationDone)
    //            return;

    //        _initialNavigationDone = true;

    //        _perfWatch = Stopwatch.StartNew();


    //        // Show loader immediately
    //        LoadingOverlay.IsVisible = true;
    //        DashboardView.IsVisible = false;

    //#if ANDROID
    //        // 1) Put Token cookie in native WebView BEFORE loading the SPA
    //        var token = await TryGetTokenAsync();
    //        if (!string.IsNullOrWhiteSpace(token))
    //        {
    //            var mgr = Android.Webkit.CookieManager.Instance;
    //            mgr.SetAcceptCookie(true);
    //            mgr.SetCookie(
    //                "http://74.243.216.77:49110",
    //                $"Token={token}; Path=/"
    //            );
    //            Android.Webkit.CookieManager.Instance.Flush();
    //        }
    //#endif

    //        // 2) Now load the SPA shell – first XHR will already include the cookie
    //        DashboardView.Source = BaseSpaUrl;
    //        Debug.WriteLine($"[Perf] After setting Source: {_perfWatch?.ElapsedMilliseconds} ms");

    //    }



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


    //    public async Task SwitchDashboardAsync(string dashboardName)
    //    {
    //        if (string.IsNullOrWhiteSpace(dashboardName))
    //            return;

    //        // Keep Shell header in sync with the selected dashboard, in case of user
    //        // logged out and logged in again
    //        Title = dashboardName;

    //        var safe = EscapeJs(dashboardName);

    //        var js = @"
    //(function(label){
    //  try {
    //    // Try sidebar container first if it exists (name guessed from UpdateDBsSideBar4)
    //    var root = document.getElementById('DBsSideBar4') || document.body;

    //    function matches(el) {
    //      if (!el || !el.textContent) return false;
    //      return el.textContent.trim() === label;
    //    }

    //    var candidates = root.querySelectorAll('*');

    //    for (var i = 0; i < candidates.length; i++) {
    //      var el = candidates[i];

    //      // Only leaf nodes (no children) with exact text
    //      if (el.childElementCount === 0 && matches(el)) {
    //        if (typeof el.click === 'function') {
    //          el.click();
    //        } else {
    //          var evt = document.createEvent('MouseEvents');
    //          evt.initEvent('click', true, true);
    //          el.dispatchEvent(evt);
    //        }

    //        console.log('Clicked sidebar item for:', label);
    //        return;
    //      }
    //    }

    //    console.log('Sidebar item not found for label:', label);
    //  } catch (e) {
    //    console.log('SwitchDashboard click error', e);
    //  }
    //})('" + safe + "');";

    //        await DashboardView.EvaluateJavaScriptAsync(js);
    //    }





    /// <summary>
    /// Called after each navigation inside the WebView.
    /// First time: inject localStorage + cookie and redirect to index.
    /// Later navigations: guard prevents doing it again.
    /// </summary>
    private async void OnWebViewNavigated(object? sender, WebNavigatedEventArgs e)
    {
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

    //private async void OnWebViewNavigated(object? sender, WebNavigatedEventArgs e)
    //{
    //    try
    //    {
    //        // 1) Get the stored raw auth payload (exact JSON from /api/Authenticate)
    //        var raw = await SecureStorage.GetAsync("auth_payload_raw");
    //        if (string.IsNullOrWhiteSpace(raw))
    //        {
    //            await DisplayAlert("Auth", "Please sign in first.", "OK");
    //            await Shell.Current.GoToAsync("//LoginPage");
    //            return;
    //        }

    //        // 2) Get the token we want to use for the cookie
    //        var token = await TryGetTokenAsync();

    //        // 3) Inject into the SPA context (localStorage + cookie) and
    //        //    redirect once to /Dashboard/Views/index
    //        await InjectAuthIntoSpaAsync(raw, token);
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine("OnWebViewNavigated error: " + ex);
    //    }


    //    // First time we consider the page “ready” visually
    //    if (!_pageLoadedOnce)
    //    {
    //        _pageLoadedOnce = true;
    //        DashboardView.Opacity = 0;
    //        DashboardView.IsVisible = true;
    //        LoadingOverlay.IsVisible = false;

    //        // Smooth fade-in
    //        await DashboardView.FadeTo(1, 250, Easing.CubicOut);
    //    }
    //    else
    //    {
    //        // For later navigations, just hide loader instantly
    //        LoadingOverlay.IsVisible = false;
    //    }
    //}


    //private async Task ShowSlowLoadingHintAfterDelay()
    //{
    //    // Wait 10 seconds before showing any hint
    //    await Task.Delay(10000);

    //    // Only show the hint if the first page hasn't finished loading
    //    // and the loading overlay is still visible.
    //    if (!_pageLoadedOnce && LoadingOverlay.IsVisible)
    //    {
    //        await MainThread.InvokeOnMainThreadAsync(async () =>
    //        {
    //            await DisplayAlertAsync(
    //                "Still loading.",
    //                "The dashboard is taking longer than usual to load.\n" +
    //                "Please check your connection.",
    //                "OK");
    //        });
    //    }
    //}





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


    //    private async Task InjectAuthIntoSpaAsync(string rawJson, string? token)
    //    {
    //        var payloadJs = EscapeJs(rawJson ?? string.Empty);
    //        var tokenJs = EscapeJs(token ?? string.Empty);

    //        var js = @"
    //(function(payload, token) {
    //    try {
    //        // Guard: inject only once per page lifetime
    //        if (window.localStorage.getItem('AuthInjectedFromApp') === '1')
    //            return;

    //        window.localStorage.setItem('AuthInjectedFromApp', '1');

    //        // Same key the web login uses
    //        if (payload && payload.length) {
    //            window.localStorage.setItem('AuthRepsonseSuccess', payload);
    //        }

    //        // Cookie
    //        if (token && token.length) {
    //            document.cookie = 'Token=' + token + '; path=/';
    //        }

    //        console.log('Auth injected from app (no reload).');
    //    } catch (e) {
    //        console.log('Auth inject error', e);
    //    }
    //})('" + payloadJs + "','" + tokenJs + "');";

    //        await DashboardView.EvaluateJavaScriptAsync(js);
    //    }





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
}






