using System;
using System.Diagnostics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Webview.Services;
using System.Linq;


namespace Webview;

public partial class AppShell : Shell
{
    private readonly ApiClient _api = new();
    private List<ViewNode> _views = new();
    private bool _dashboardsLoaded;


    public AppShell()
    {
        InitializeComponent();
        Application.Current.UserAppTheme = AppTheme.Light;

        // optional, but fine to keep
        Routing.RegisterRoute("LoginPage", typeof(LoginPage));
        Routing.RegisterRoute("SingleWebView", typeof(SingleWebView));

        Loaded += AppShell_Loaded;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await GoToAsync("//LoginPage");
    }


    //to load the dashboards list AFTER the default one is fully loaded to avoid timing issues
    public async Task EnsureDashboardsLoadedAsync()
    {
        if (_dashboardsLoaded)
            return;

        try
        {
            var token = await SecureStorage.GetAsync("auth_token");
            if (string.IsNullOrWhiteSpace(token))
                return;

            var views = await _api.GetViewsTreeAsync(token);
            DashboardsList.ItemsSource = views;
            _dashboardsLoaded = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine("EnsureDashboardsLoadedAsync error: " + ex);
        }
    }

    private async void AppShell_Loaded(object? sender, EventArgs e)
    {
        await EnsureDashboardsLoadedAsync();

    }

    // Parent row tapped: Overview, Map, Factories, Utility
    private async void OnParentTapped(object sender, EventArgs e)
    {
        if (sender is not BindableObject bo || bo.BindingContext is not ViewNode node)
            return;

        // If it has children (e.g. Factories), just expand/collapse
        if (node.HasChildren)
        {
            node.IsExpanded = !node.IsExpanded;
            return;
        }

        // Leaf parent (Overview / Map / Utility) → navigate
        if (CurrentPage is SingleWebView page)
        {
            var dashId = node.Id ?? node.Name;
            if (string.IsNullOrWhiteSpace(dashId))
                return;

            await page.SwitchDashboardAsync(dashId, node.Name, parentDisplayName: null);
            FlyoutIsPresented = false; // close menu
        }
    }

    // Child row tapped: Factory-1..Factory-9
    private async void OnChildTapped(object sender, EventArgs e)
    {
        if (sender is not BindableObject bo || bo.BindingContext is not ViewNode node)
            return;

        if (CurrentPage is SingleWebView page)
        {
            var dashId = node.Id ?? node.Name;      // "Factories__Factory-1"
            if (string.IsNullOrWhiteSpace(dashId))
                return;

            await page.SwitchDashboardAsync(
                dashId,
                displayName: node.Name,             // "Factory-1"
                parentDisplayName: node.ParentName  // "Factories"
            );

            FlyoutIsPresented = false;
        }
    }



    //replaced it with OnParentTapped / OnChildTapped.
    //private async Task NavigateToDashboard(ViewNode node)
    //{
    //    //var dashName = node.Id ?? node.Name;
    //    var dashName = node.Name ?? node.Id;

    //    if (string.IsNullOrWhiteSpace(dashName))
    //        return;

    //    // auto-close the hamburger menu
    //    FlyoutIsPresented = false;

    //    if (Shell.Current.CurrentPage is not SingleWebView sv)
    //    {
    //        await Shell.Current.GoToAsync("//SingleWebView");
    //        sv = Shell.Current.CurrentPage as SingleWebView;
    //    }

    //    if (sv != null)
    //    {
    //        sv.Title = dashName;
    //        await sv.SwitchDashboardAsync(dashName);

    //    }
    //}

    private async void OnSignOutClicked(object sender, EventArgs e)
    {
        // Close the menu
        FlyoutIsPresented = false;

        // Reset dashboards list
        DashboardsList.ItemsSource = null;
        _dashboardsLoaded = false;

        // Clear auth from storage
        try
        {
            SecureStorage.Remove("auth_token");
            SecureStorage.Remove("auth_payload_raw");
        }
        catch
        {
            // ignore secure storage exceptions
        }

        Preferences.Remove("auth_token");

#if ANDROID
        // Clear cookies for the dashboard host
        try
        {
            var mgr = Android.Webkit.CookieManager.Instance;
            mgr.RemoveAllCookies(null);
            mgr.Flush();
        }
        catch
        {
            // ignore
        }
#endif

        // Navigate back to login
        await Shell.Current.GoToAsync("//LoginPage");
    }


}


