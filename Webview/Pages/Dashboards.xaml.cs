using Microsoft.Maui.Graphics;
using Microsoft.Maui.Storage;
using Webview.Services;

namespace Webview;

public partial class DashboardsPage : ContentPage
{
    private readonly ApiClient _api = new();
    private List<ViewNode> _roots = new();

    // single shared host page
    private static readonly SingleWebView _singleWebView = new();

    public DashboardsPage()
    {
        InitializeComponent(); // required for RootStack to exist
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Read token 
        string? token = null;
        try { token = await SecureStorage.GetAsync("auth_token"); } catch { }
        token ??= Preferences.Get("auth_token", "");

        if (string.IsNullOrWhiteSpace(token))
        {
            await DisplayAlert("Auth", "No token found. Please sign in.", "OK");
            await Shell.Current.GoToAsync("//LoginPage");
            return;
        }

        try
        {
            _roots = await _api.GetViewsTreeAsync(token);
            RenderTree();
        }
        catch (Exception ex)
        {
            RootStack.Children.Clear();
            RootStack.Children.Add(new Label { Text = $"Error: {ex.Message}", TextColor = Colors.Red });
        }
    }

    private void RenderTree()
    {
        RootStack.Children.Clear();

        if (_roots.Count == 0)
        {
            RootStack.Children.Add(new Label { Text = "No dashboards available.", Opacity = 0.7 });
            return;
        }

        foreach (var node in _roots)
            RootStack.Children.Add(RenderNode(node, 0));
    }

    private View RenderNode(ViewNode node, int depth)
    {
        // Card row
        var card = new Grid
        {
            Padding = new Thickness(12, 10),
            BackgroundColor = Colors.White,
            Margin = new Thickness(16 * depth, 0, 0, 10),
            ColumnDefinitions =
        {
            new ColumnDefinition { Width = 28 },
            new ColumnDefinition { Width = GridLength.Star },
            new ColumnDefinition { Width = 28 }
        },
            Shadow = new Shadow { Offset = new Point(0, 1), Opacity = 0.15f, Radius = 4 }
        };

        // icon
        card.Add(new Label { Text = "▦", FontSize = 18, VerticalOptions = LayoutOptions.Center }, 0, 0);

        // title
        card.Add(new Label
        {
            Text = node.Name ?? "Unnamed",
            FontAttributes = FontAttributes.Bold,
            FontSize = 16,
            VerticalOptions = LayoutOptions.Center
        }, 1, 0);

        // chevron
        var chevron = new Label
        {
            Text = node.HasChildren ? "▸" : "",
            FontSize = 16,
            VerticalOptions = LayoutOptions.Center,
            HorizontalTextAlignment = TextAlignment.End
        };
        card.Add(chevron, 2, 0);
 
        // Tap handler
        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, __) =>
        {
            if (node.HasChildren)
            {
                var container = (VerticalStackLayout)((VerticalStackLayout)card.Parent).Children[1];
                container.IsVisible = !container.IsVisible;
                chevron.Text = container.IsVisible ? "▾" : "▸";
                return;
            }

            if (string.IsNullOrWhiteSpace(node.Url)) return;

            // node.Url must be: http://74.243.216.77:49110/Dashboard/Views/index
            var dashName = node.Name ?? string.Empty;

            // No token or hash here; the page will inject token + drive the SPA internally
            //await Shell.Current.Navigation.PushAsync(new DashboardPageWithUrl(node.Url, dashName));

            // NEW:
            //await _singleWebView.OpenDashboardFromNativeAsync(dashName);
            //await Shell.Current.Navigation.PushAsync(_singleWebView);
        };
        card.GestureRecognizers.Add(tap);



        // Children (collapsed by default)
        var childrenContainer = new VerticalStackLayout { Spacing = 10, IsVisible = false };
        foreach (var child in node.Children)
            childrenContainer.Add(RenderNode(child, depth + 1));

        // Return a two-row stack: [card] + [children]
        return new VerticalStackLayout { Spacing = 6, Children = { card, childrenContainer } };
    }
}
