using System.ComponentModel;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;

namespace Webview.Services;

public sealed class ApiClient
{
    private const string BaseUrl = "http://74.243.216.77:49110/";

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private HttpClient CreateClient(string? token = null)
    {
        var http = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(15) // <= SHORTER TIMEOUT
        };

        if (!string.IsNullOrWhiteSpace(token))
        {
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(token);
        }

        return http;
    }


    // --- Authenticate ---

    public async Task<string> AuthenticateAsync(string user, string pass, CancellationToken ct = default)
    {
        using var http = CreateClient(); // dispose client after call

        var body = new { UserName = user, UserPassword = pass };
        using var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        HttpResponseMessage res;

        try
        {
            res = await http.PostAsync("api/Authenticate", content, ct);
        }
        catch (HttpRequestException ex)
        {
            // No route to host / connection refused / DNS issue, etc.
            throw new HttpRequestException(
                "Cannot reach the server. Please make sure it is running and the address is correct.",
                ex);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            // Hit our 10s HttpClient timeout
            throw new TaskCanceledException("Login request timed out. Please try again.", ex);
        }

        // Non-success HTTP status codes (401, 500, etc.)
        if (!res.IsSuccessStatusCode)
        {
            if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new InvalidOperationException("Invalid username or password.");
            }

            throw new HttpRequestException(
                $"Server error ({(int)res.StatusCode}). Please try again later.");
        }

        var payload = await res.Content.ReadAsStringAsync(ct);

        // Save raw JSON exactly as web login receives it
        await SecureStorage.SetAsync("auth_payload_raw", payload);

        var auth = JsonSerializer.Deserialize<AuthResponse>(payload, _json)
                   ?? throw new FormatException("Invalid auth response.");

        // Token may be null (your web example), so don't throw here.
        if (!string.IsNullOrWhiteSpace(auth.Token))
        {
            await SecureStorage.SetAsync("auth_token", auth.Token);
            Preferences.Set("auth_token", auth.Token);
        }

        // Return token anyway (may be null/empty)
        return auth.Token ?? string.Empty;
    }

    //public async Task<string> AuthenticateAsync(string user, string pass, CancellationToken ct = default)
    //{
    //    using var http = CreateClient();
    //    var body = new { UserName = user, UserPassword = pass };
    //    using var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    //    using var res = await http.PostAsync("api/Authenticate", content, ct);
    //    //using var res = await http.PostAsync("Dashboard/Authenticate", content, ct);

    //    res.EnsureSuccessStatusCode();

    //    var payload = await res.Content.ReadAsStringAsync(ct);

    //    // Save raw JSON exactly as web login receives it
    //    await SecureStorage.SetAsync("auth_payload_raw", payload);

    //    var auth = JsonSerializer.Deserialize<AuthResponse>(payload, _json)
    //               ?? throw new FormatException("Invalid auth response.");

    //    // Token may be null (your web example), so don't throw here.
    //    // We still store it if present for the cookie.
    //    if (!string.IsNullOrWhiteSpace(auth.Token))
    //    {
    //        await SecureStorage.SetAsync("auth_token", auth.Token);
    //        Preferences.Set("auth_token", auth.Token);
    //    }

    //    // Return token anyway (may be null/empty)
    //    return auth.Token ?? string.Empty;
    //}




    // --- DASHBOARD TREE (parents + children from Dict) ---


    public async Task<List<ViewNode>> GetViewsTreeAsync(string token, CancellationToken ct = default)
    {
        var http = CreateClient(token);

        using var res = await http.GetAsync("Dashboard/ViewsTree", ct);
        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadAsStringAsync(ct);
        System.Diagnostics.Debug.WriteLine($"[ViewsTree JSON] {json}");

        // 1) Preferred shape from your backend:
        // {
        //   "Names": ["Overview","Map","Factories","Utility"],
        //   "Dict":  {
        //       "Factories": "Factory-1,Factory-2,...,Factory-9",
        //       "Map": "",
        //       "Overview": "",
        //       "Utility": ""
        //   }
        // }
        try
        {
            var dto = JsonSerializer.Deserialize<ViewsTreeDto>(json, _json);
            if (dto?.Dict is { Count: > 0 })
            {
                IEnumerable<string> parentKeys =
                    (dto.Names is { Length: > 0 })
                        ? dto.Names.Where(n => dto.Dict.ContainsKey(n))
                        : dto.Dict.Keys;

                var list = new List<ViewNode>();

                foreach (var parent in parentKeys)
                {
                    var childrenCsv = dto.Dict[parent] ?? string.Empty;
                    var children = SplitCsv(childrenCsv); // splits by comma, trims, removes empties

                    if (children.Count == 0)
                    {
                        // Simple leaf dashboard: Overview / Map / Utility
                        list.Add(new ViewNode
                        {
                            Id = parent,       // e.g. "Map"
                            Name = parent,
                            Url = BuildDashboardUrl(parent)
                        });
                    }
                    else
                    {
                        // Parent node with children (Factories)
                        var node = new ViewNode
                        {
                            Id = parent,       // parent id: "Factories"
                            Name = parent
                        };

                        node.Children = children.Select(ch =>
                        {
                            var fullId = $"{parent}__{ch}";   // e.g. "Factories__Factory-1"

                            return new ViewNode
                            {
                                Id = fullId,
                                Name = ch,              // "Factory-1"
                                ParentName = parent,          // "Factories"
                                Url = BuildDashboardUrl(fullId)
                            };
                        }).ToList();


                        list.Add(node);
                    }
                }

                return list;
            }
        }
        catch
        {
            // ignore and fall through
        }

        // 2) Simple ["DashA","DashB", ...]
        try
        {
            var names = JsonSerializer.Deserialize<string[]>(json, _json);
            if (names is { Length: > 0 })
            {
                return names.Select(n => new ViewNode
                {
                    Id = n,
                    Name = n,
                    Url = BuildDashboardUrl(n)
                }).ToList();
            }
        }
        catch { }

        // 3) Already ViewNode-like
        try
        {
            var nodes = JsonSerializer.Deserialize<List<ViewNode>>(json, _json);
            if (nodes is { Count: > 0 })
                return nodes!;
        }
        catch { }

        // Fallback: expose raw JSON
        return new List<ViewNode>
    {
        new ViewNode { Name = "Raw JSON", Raw = json }
    };
    }



    //public async Task<List<ViewNode>> GetViewsTreeAsync(string token, CancellationToken ct = default)
    //{
    //    var http = CreateClient(token);

    //    using var res = await http.GetAsync("Dashboard/ViewsTree", ct);
    //    res.EnsureSuccessStatusCode();

    //    var json = await res.Content.ReadAsStringAsync(ct);
    //    System.Diagnostics.Debug.WriteLine($"[ViewsTree JSON] {json}");

    //    try
    //    {
    //        var dto = JsonSerializer.Deserialize<ViewsTreeDto>(json, _json);
    //        if (dto?.Dict is { Count: > 0 })
    //        {
    //            // Helper: parent or parent__child
    //            static string BuildDashboardId(string parent, string? child) =>
    //                string.IsNullOrWhiteSpace(child) ? parent : $"{parent}__{child}";

    //            IEnumerable<string> parentKeys =
    //                (dto.Names is { Length: > 0 })
    //                    ? dto.Names.Where(n => dto.Dict.ContainsKey(n))
    //                    : dto.Dict.Keys;

    //            var list = new List<ViewNode>();

    //            foreach (var parent in parentKeys)
    //            {
    //                var childrenCsv = dto.Dict[parent] ?? string.Empty;
    //                var children = SplitCsv(childrenCsv);

    //                if (children.Count == 0)
    //                {
    //                    // Leaf: Overview / Map / Utility / (maybe parent with no children)
    //                    var id = BuildDashboardId(parent, null);      // just "Map", "Overview", ...
    //                    list.Add(new ViewNode
    //                    {
    //                        Id = id,
    //                        Name = parent,
    //                        Url = BuildDashboardUrl(id)
    //                    });
    //                }
    //                else
    //                {
    //                    // Parent with children (Factories)
    //                    var parentNode = new ViewNode
    //                    {
    //                        Id = BuildDashboardId(parent, null),     // "Factories"
    //                        Name = parent
    //                    };

    //                    parentNode.Children = children.Select(ch =>
    //                    {
    //                        var fullId = BuildDashboardId(parent, ch); // "Factories__Factory-1"

    //                        return new ViewNode
    //                        {
    //                            Id = fullId,
    //                            Name = ch,                              // display text "Factory-1"
    //                            Url = BuildDashboardUrl(fullId)
    //                        };
    //                    }).ToList();

    //                    list.Add(parentNode);
    //                }
    //            }

    //            return list;
    //        }
    //    }
    //    catch
    //    {
    //        // fall through to other shapes
    //    }

    //    // 2) Simple ["DashA","DashB",...]
    //    try
    //    {
    //        var names = JsonSerializer.Deserialize<string[]>(json, _json);
    //        if (names is { Length: > 0 })
    //            return names.Select(n => new ViewNode
    //            {
    //                Id = n,
    //                Name = n,
    //                Url = BuildDashboardUrl(n)
    //            }).ToList();
    //    }
    //    catch { }

    //    // 3) Already ViewNode-like
    //    try
    //    {
    //        var nodes = JsonSerializer.Deserialize<List<ViewNode>>(json, _json);
    //        if (nodes is { Count: > 0 }) return nodes!;
    //    }
    //    catch { }

    //    return new List<ViewNode> { new ViewNode { Name = "Raw JSON", Raw = json } };
    //}

    private static List<string> SplitCsv(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return new();
        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                  .Select(s => s.Trim())
                  .Where(s => !string.IsNullOrWhiteSpace(s))
                  .Distinct()
                  .ToList();
    }

    private static string BuildDashboardUrl(string name)
    {
        return "http://74.243.216.77:49110/Dashboard/Views/index";
    }

}

// ----- Models -----
public sealed class AuthResponse
{
    public string? Token { get; set; }
    public string? UserName { get; set; }
    public string? AccessLevel { get; set; }
}

public sealed class ViewsTreeDto
{
    public string[]? Names { get; set; }
    public Dictionary<string, string>? Dict { get; set; }
}


public sealed class ViewNode : INotifyPropertyChanged
{
    public string? Id { get; set; }
    public string? Name { get; set; }

    // used only for children, e.g. "Factories"
    public string? ParentName { get; set; }
    public string? Url { get; set; }
    public string? Raw { get; set; }

    public List<ViewNode> Children { get; set; } = new();
    public bool HasChildren => Children.Count > 0;

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
