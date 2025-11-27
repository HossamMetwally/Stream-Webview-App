using Microsoft.Maui.Storage;
namespace Webview;
using Microsoft.Maui.Storage;

//public partial class DashboardPageWithUrl : ContentPage
//{
//    private readonly string _url;
//    private readonly string _dashName;
//    private bool _routeDriven = false; // guard: run routing once per page load





//    public DashboardPageWithUrl(string url, string dashName)
//    {
//        InitializeComponent();
//        _url = url;
//        _dashName = dashName;

//#if ANDROID
//        Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);
//        DashboardView.HandlerChanged += (_, __) =>
//        {
//            if (DashboardView.Handler?.PlatformView is Android.Webkit.WebView awv)
//            {
//                awv.Settings.JavaScriptEnabled = true;
//                awv.Settings.DomStorageEnabled = true;
//                //awv.ClearCache(true); likley causes slowness

//                var mgr = Android.Webkit.CookieManager.Instance;
//                mgr.SetAcceptCookie(true);
//                Android.Webkit.CookieManager.Instance.SetAcceptThirdPartyCookies(awv, true);
//            }
//        };
//#endif

//        // Log navigations
//        DashboardView.Navigating += (_, e) =>
//            System.Diagnostics.Debug.WriteLine("WebView navigating: " + e.Url);

//        // After first paint, inject token and auto-discover the real route
//        DashboardView.Navigated += async (_, __) =>
//        {
//            var token = await TryGetTokenAsync();
//            if (string.IsNullOrWhiteSpace(token)) return;
//            var jsToken = EscapeJs(token);

//            // --- Inject token into cookie + storage and wrap fetch/xhr ---
//            var js = "(function(t){try{"
//                   + "document.cookie='Authorization='+t+'; Path=/; Max-Age=28800';"
//                   + "try{localStorage.setItem('Authorization',t);}catch(_){}"
//                   + "try{sessionStorage.setItem('Authorization',t);}catch(_){}"
//                   + "if(!window.__authWrapped){window.__authWrapped=true;"
//                   + "var f=window.fetch;window.fetch=function(u,o){o=o||{};o.headers=o.headers||{};"
//                   + "if(!o.headers['Authorization'])o.headers['Authorization']=t;return f(u,o);};"
//                   + "var s=XMLHttpRequest.prototype.send,o=XMLHttpRequest.prototype.open;"
//                   + "XMLHttpRequest.prototype.open=function(){o.apply(this,arguments);};"
//                   + "XMLHttpRequest.prototype.send=function(b){try{this.setRequestHeader('Authorization',t);}catch(_){ } s.call(this,b);};"
//                   + "}"
//                   + "}catch(_){}})('" + jsToken + "');";
//            try { await DashboardView.EvaluateJavaScriptAsync(js); } catch { }



//            var diag = @"
//(function(){
//  function list(selector){
//    return Array.from(document.querySelectorAll(selector)).map(function(n){
//      return {
//        text: (n.textContent||'').trim(),
//        db: (n.getAttribute('data-dbname')||'').trim(),
//        classes: n.className||''
//      };
//    });
//  }
//  var info = {
//    has_GetAndUpdate: typeof window.GetAndUpdateDashBoard === 'function',
//    has_OpenByName:   typeof window.OpenDashboardByName === 'function',
//    hash: location.hash,
//    ls_keys: Object.keys(localStorage),
//    ss_keys: Object.keys(sessionStorage),
//    opendb: list('.opendb[data-dbname]'),
//    activeGuess: (function(){
//      var a = document.querySelector('.opendb.active,[aria-current=""page""] .opendb');
//      return a ? ((a.getAttribute('data-dbname')||a.textContent||'').trim()) : '';
//    })()
//  };
//  return JSON.stringify(info);
//})();";
//            try
//            {
//                var probe = await DashboardView.EvaluateJavaScriptAsync(diag);
//                System.Diagnostics.Debug.WriteLine("[SPA DIAG] " + probe);
//            }
//            catch { }


//            // === Drive SPA to a dashboard by its .opendb[data-dbname] WITHOUT re-click loops ===
//            //            var wanted = EscapeJs((_dashName ?? "").Trim());
//            //            var driveOnce = $@"
//            //(function(name){{
//            //  // guard: don't run twice per page
//            //  if (window.__driveOnceGuard) return;
//            //  window.__driveOnceGuard = true;

//            //  //  probe to help debugging
//            //  try {{
//            //    var probe = document.createElement('div');
//            //    probe.style.cssText='position:fixed;left:12px;bottom:12px;background:#1e1;padding:6px 10px;border-radius:8px;z-index:99999;font:12px monospace;color:#0a0';
//            //    probe.textContent='[dashboard:] target='+name;
//            //    document.body.appendChild(probe);
//            //    setTimeout(function(){{probe.remove();}}, 1500);
//            //  }} catch(e){{}}

//            //  function findItem() {{
//            //    var nodes = Array.from(document.querySelectorAll('.opendb[data-dbname]'));
//            //    var target = nodes.find(n =>
//            //      (n.getAttribute('data-dbname')||'').trim().toLowerCase() === name.toLowerCase() ||
//            //      (n.textContent||'').trim().toLowerCase() === name.toLowerCase()
//            //    );
//            //    // prefer clicking the <a> wrapper if present
//            //    return target ? (target.closest('a') || target) : null;
//            //  }}

//            //  function looksActive() {{
//            //    // Heuristics to decide we're already on the requested dashboard:
//            //    // 1) a sidebar item is marked active (common patterns)
//            //    var active = document.querySelector('.opendb.active,[aria-current=""page""] .opendb');
//            //    if (active) {{
//            //      var n = (active.getAttribute('data-dbname')||active.textContent||'').trim().toLowerCase();
//            //      if (n === name.toLowerCase()) return true;
//            //    }}
//            //    // 2) location hash equals #<name> (some builds use hashes)
//            //    if (location.hash && decodeURIComponent(location.hash.slice(1)).trim().toLowerCase() === name.toLowerCase()) return true;
//            //    return false;
//            //  }}

//            //  var clicked = false;
//            //  var retryTimer = null;

//            //  function clickOnce() {{
//            //    if (clicked) return;
//            //    var el = findItem();
//            //    if (!el) return;               // menu not ready yet
//            //    clicked = true;                // <- prevent multiple clicks
//            //    // If app exposes helper, use it; else synthesize a real user click
//            //    if (typeof window.OpenDashboardByName === 'function') {{
//            //      window.OpenDashboardByName(name);
//            //    }} else {{
//            //      el.dispatchEvent(new MouseEvent('click', {{bubbles:true, cancelable:true, view:window}}));
//            //    }}
//            //    // start an observer to detect the route/content change and stop everything
//            //    var done = false;
//            //    var stopAll = function() {{
//            //      if (done) return;
//            //      done = true;
//            //      if (retryTimer) clearTimeout(retryTimer);
//            //      if (mo) try {{ mo.disconnect(); }} catch(_){{
//            //      }}
//            //      window.__driveOnceGuard = false; // allow future pages to drive again
//            //    }};

//            //    // mutation observer: any DOM change likely means route swap
//            //    var mo = new MutationObserver(function() {{
//            //      if (looksActive()) stopAll();
//            //    }});
//            //    try {{ mo.observe(document.body, {{subtree:true, childList:true, attributes:true}}); }} catch(_){{
//            //      // ignore
//            //    }}

//            //    // safety: if not active after 2.5s, allow a single retry
//            //    retryTimer = setTimeout(function(){{
//            //      if (!looksActive()) {{
//            //        clicked = false;   // allow one more click
//            //        clickOnce();
//            //      }} else {{
//            //        stopAll();
//            //      }}
//            //    }}, 2500);
//            //  }}

//            //  // Wait until menu exists, then click ONCE.
//            //  var tries = 80;  // ~20s (80 * 250ms)
//            //  (function tick(){{
//            //    if (looksActive()) {{
//            //      // already on the right dashboard
//            //      window.__driveOnceGuard = false;
//            //      return;
//            //    }}
//            //    var el = findItem();
//            //    if (el) {{
//            //      clickOnce();
//            //      return; // don't keep polling; observer will manage success/1-retry
//            //    }}
//            //    if (--tries > 0) setTimeout(tick, 250);
//            //    else window.__driveOnceGuard = false;
//            //  }})();
//            //}})('{wanted}');
//            //";
//            //            try { await DashboardView.EvaluateJavaScriptAsync(driveOnce); } catch { }


//            if (_routeDriven) return; // run once per navigation

//            var wanted = EscapeJs((_dashName ?? "").Trim());






//            await Task.Delay(400);
//            try
//            {
//                var where = await DashboardView.EvaluateJavaScriptAsync("location.pathname + location.search + location.hash");
//                System.Diagnostics.Debug.WriteLine("SPA at: " + where);
//            }
//            catch { }
//        };
//    }

//    protected override async void OnAppearing()
//    {
//        base.OnAppearing();
//        try
//        {
//            // Reset first to avoid reused SPA state
//            //DashboardView.Source = "about:blank";
//            //await Task.Delay(50);

//#if ANDROID
//            // Set native cookie so first HTML/JSON requests see the token
//            var token = await TryGetTokenAsync();
//            if (!string.IsNullOrWhiteSpace(token))
//            {
//                try
//                {
//                    var mgr = Android.Webkit.CookieManager.Instance;
//                    mgr.SetAcceptCookie(true);
//                    mgr.SetCookie("http://74.243.216.77:49110", $"Authorization={token}; Path=/; Max-Age=28800");
//                    Android.Webkit.CookieManager.Instance.Flush();
//                }
//                catch { }
//            }
//#endif
//            System.Diagnostics.Debug.WriteLine("Navigating to " + _url);
//            DashboardView.Source = _url;

//            await Task.Delay(300);
//            var where = await DashboardView.EvaluateJavaScriptAsync("location.href");
//            System.Diagnostics.Debug.WriteLine("After navigation, WebView at: " + where);
//        }
//        catch (Exception ex)
//        {
//            System.Diagnostics.Debug.WriteLine("OnAppearing error: " + ex);
//        }
//    }




//    private static async Task<string?> TryGetTokenAsync()
//    {
//        try { var t = await SecureStorage.GetAsync("auth_token"); if (!string.IsNullOrWhiteSpace(t)) return t; } catch { }
//        var p = Preferences.Get("auth_token", string.Empty);
//        return string.IsNullOrWhiteSpace(p) ? null : p;
//    }

//    private static string EscapeJs(string s) => s.Replace("\\", "\\\\").Replace("'", "\\'");
//}




































//v2
//public partial class DashboardPageWithUrl : ContentPage
//{
//    private readonly string _url;
//    private readonly string _dashName;
//    private bool _routeDriven;

//    public DashboardPageWithUrl(string url, string dashName)
//    {
//        InitializeComponent();
//        _url = url;
//        _dashName = dashName;

//#if ANDROID
//        Android.Webkit.WebView.SetWebContentsDebuggingEnabled(true);
//        DashboardView.HandlerChanged += (_, __) =>
//        {
//            if (DashboardView.Handler?.PlatformView is Android.Webkit.WebView awv)
//            {
//                awv.Settings.JavaScriptEnabled = true;
//                awv.Settings.DomStorageEnabled = true;

//                var mgr = Android.Webkit.CookieManager.Instance;
//                mgr.SetAcceptCookie(true);
//                Android.Webkit.CookieManager.Instance.SetAcceptThirdPartyCookies(awv, true);
//            }
//        };
//#endif

//        //DashboardView.Navigating += (_, e) =>
//        //    System.Diagnostics.Debug.WriteLine("Nav -> " + e.Url);

//        DashboardView.Navigated += OnWebViewNavigated;
//    }

//    //no hash method
//    private async void OnWebViewNavigated(object? sender, WebNavigatedEventArgs e)
//    {
//        var token = await TryGetTokenAsync();
//        if (string.IsNullOrWhiteSpace(token)) return;
//        var jsToken = EscapeJs(token);

//        // Inject cookie + storage + wrap fetch/xhr (small + robust)
//        var authJs =
//            "(function(t){try{" +
//            "document.cookie='Authorization='+t+'; Path=/; Max-Age=28800';" +
//            "try{localStorage.setItem('Authorization',t);}catch(_){ }" +
//            "try{sessionStorage.setItem('Authorization',t);}catch(_){ }" +
//            "if(!window.__authWrapped){window.__authWrapped=true;" +
//            " var _f=window.fetch; window.fetch=function(u,o){o=o||{};o.headers=o.headers||{};" +
//            "  if(!o.headers['Authorization'])o.headers['Authorization']=t; return _f(u,o);};" +
//            " var _o=XMLHttpRequest.prototype.open,_s=XMLHttpRequest.prototype.send;" +
//            " XMLHttpRequest.prototype.open=function(){_o.apply(this,arguments);};" +
//            " XMLHttpRequest.prototype.send=function(b){try{this.setRequestHeader('Authorization',t);}catch(_){ } _s.call(this,b);};" +
//            "}" +
//            "}catch(_){}})('" + jsToken + "');";

//        try { await DashboardView.EvaluateJavaScriptAsync(authJs); } catch { }




//        // 1) list available dashboards (text + data-dbname)
//        // --- enumerate dashboards (text + data-dbname) ---
//        try
//        {
//            var listDashboards = @"
//(function(){
//  try{
//    var sels=['.opendb[data-dbname]','[data-dbname].opendb','[data-dbname]','.menu .opendb'];
//    var seen={}; var list=[];
//    for (var i=0;i<sels.length;i++){
//      var q=document.querySelectorAll(sels[i]);
//      q.forEach(function(n){
//        var txt=(n.textContent||'').trim();
//        var db=(n.getAttribute('data-dbname')||'').trim();
//        var key=txt+'|'+db; if(seen[key]) return; seen[key]=1;
//        list.push({text:txt, db:db});
//      });
//    }
//    return JSON.stringify(list);
//  }catch(e){ return 'ERR '+(e && e.message?e.message:e); }
//})();";
//            await Task.Delay(300);

//            var res = await DashboardView.EvaluateJavaScriptAsync(listDashboards);
//            System.Diagnostics.Debug.WriteLine("[DashList] " + (res ?? "NULL"));
//        }
//        catch { }

//        try
//        {
//            var probeFrames = @"
//(function(){
//  function frameInfo(win, idx){
//    var out = { idx: idx, href: '', hasAPI: false, opendbCount: 0 };
//    try{ out.href = win.location.href; }catch(e){}
//    try{ out.hasAPI = (typeof win.GetAndUpdateDashBoard === 'function'); }catch(e){}
//    try{ out.opendbCount = win.document ? win.document.querySelectorAll('.opendb,[data-dbname]').length : -1; }catch(e){}
//    return out;
//  }
//  var res = { top: frameInfo(window, -1), frames: [] };
//  try{
//    for (var i=0;i<window.frames.length;i++){
//      res.frames.push(frameInfo(window.frames[i], i));
//    }
//  }catch(e){}
//  return JSON.stringify(res);
//})();";
//            var probe = await DashboardView.EvaluateJavaScriptAsync(probeFrames);
//            System.Diagnostics.Debug.WriteLine("[FRAMES] " + (probe ?? "NULL"));
//        }
//        catch { }


//        // 2) drive once by API using the correct key (dbname if available)
//        if (_routeDriven) return;
//        _routeDriven = true;

//        try
//        {
//            var wanted = EscapeJs((_dashName ?? "").Trim());
//            var drive = @"
//(function(name){
//  // Try top first
//  try{ if (typeof window.GetAndUpdateDashBoard === 'function'){ window.GetAndUpdateDashBoard(name); return 'TOP_OK'; } }catch(e){}
//  // Then scan same-origin frames
//  try{
//    for (var i=0;i<window.frames.length;i++){
//      try{
//        var f = window.frames[i];
//        if (typeof f.GetAndUpdateDashBoard === 'function'){
//          f.GetAndUpdateDashBoard(name);
//          return 'FRAME_OK_'+i;
//        }
//      }catch(e){}
//    }
//  }catch(e){}
//  return 'NO_API';
//})('" + wanted + "');";
//            var result = await DashboardView.EvaluateJavaScriptAsync(drive);
//            System.Diagnostics.Debug.WriteLine("[DRIVE] " + (result ?? "NULL"));
//        }
//        catch { }


//    }


//    //    private async void OnWebViewNavigated(object? sender, WebNavigatedEventArgs e)
//    //    {
//    //        // 1) Inject auth (cookie + storage + wrap fetch/xhr)
//    //        var token = await TryGetTokenAsync();
//    //        if (string.IsNullOrWhiteSpace(token)) return;
//    //        var jsToken = EscapeJs(token);

//    //        var authJs = $@"
//    //(function(t){{
//    //  try {{
//    //    document.cookie = 'Authorization=' + t + '; Path=/; Max-Age=28800';
//    //    try {{ localStorage.setItem('Authorization', t); }} catch(_){{
//    //    }}
//    //    try {{ sessionStorage.setItem('Authorization', t); }} catch(_){{
//    //    }}
//    //    if (!window.__authWrapped) {{
//    //      window.__authWrapped = true;
//    //      var _fetch = window.fetch;
//    //      window.fetch = function(u,o) {{
//    //        o = o || {{}};
//    //        o.headers = o.headers || {{}};
//    //        if (!o.headers['Authorization']) o.headers['Authorization'] = t;
//    //        return _fetch(u,o);
//    //      }};
//    //      var _open = XMLHttpRequest.prototype.open;
//    //      var _send = XMLHttpRequest.prototype.send;
//    //      XMLHttpRequest.prototype.open = function() {{ _open.apply(this, arguments); }};
//    //      XMLHttpRequest.prototype.send = function(b) {{
//    //        try {{ this.setRequestHeader('Authorization', t); }} catch(_){{
//    //        }}
//    //        _send.call(this, b);
//    //      }};
//    //    }}
//    //  }} catch(_){{
//    //  }}
//    //}})('{jsToken}');
//    //";
//    //        try { await DashboardView.EvaluateJavaScriptAsync(authJs); } catch { }

//    //        //step 2


//    //        if (_routeDriven) return;
//    //        _routeDriven = true;

//    //        var wanted = EscapeJs((_dashName ?? string.Empty).Trim());

//    //        var drive =
//    //            "(function(name){" +
//    //            "  try { if (window.__routeRun2) return; window.__routeRun2 = true; } catch(_){}" +
//    //            "  window.__routeProbe2 = 'START name=' + name;" +

//    //            // keep editor/overlays disabled so UI doesn’t flash
//    //            "  try { " +
//    //            "    localStorage.setItem('edit','false');" +
//    //            "    localStorage.setItem('showEditor','false');" +
//    //            "    localStorage.setItem('IsEditMode','false');" +
//    //            "  } catch(_){}" +

//    //            "  function findCtx(w){" +
//    //            "    try { if (typeof w.GetAndUpdateDashBoard === 'function') return w; } catch(_){}" +
//    //            "    try { var fr = w.frames || []; for (var i=0;i<fr.length;i++){" +
//    //            "          try { if (typeof fr[i].GetAndUpdateDashBoard === 'function') return fr[i]; } catch(_){}" +
//    //            "        }" +
//    //            "    } catch(_){}" +
//    //            "    return null;" +
//    //            "  }" +

//    //            "  var t0 = Date.now();" +
//    //            "  var poll = setInterval(function(){" +
//    //            "    var ctx = findCtx(window) || findCtx(window.top);" +
//    //            "    if (!ctx) { if (Date.now()-t0 > 5000) { window.__routeProbe2 = 'NO_API_5s'; clearInterval(poll); } return; }" +
//    //            "    clearInterval(poll);" +
//    //            "    window.__routeProbe2 = 'API_FOUND after=' + (Date.now()-t0) + 'ms';" +

//    //            // let default boot finish, then call once
//    //            "    setTimeout(function(){" +
//    //            "      try { ctx.GetAndUpdateDashBoard(name);" +
//    //            "            window.__routeProbe2 += ' CALLED@' + (Date.now()-t0) + 'ms';" +
//    //            "            try { ctx.history && ctx.history.replaceState && ctx.history.replaceState(ctx.history.state, '', location.pathname); } catch(_){}" +
//    //            "      } catch(e) { window.__routeProbe2 += ' CALL_ERR ' + (e && e.message ? e.message : e); }" +
//    //            "    }, 1200);" +

//    //            // reinforce once more
//    //            "    setTimeout(function(){" +
//    //            "      try { ctx.GetAndUpdateDashBoard(name);" +
//    //            "            window.__routeProbe2 += ' CALLED_AGAIN@' + (Date.now()-t0) + 'ms';" +
//    //            "            try { ctx.history && ctx.history.replaceState && ctx.history.replaceState(ctx.history.state, '', location.pathname); } catch(_){}" +
//    //            "      } catch(e) { window.__routeProbe2 += ' CALL2_ERR ' + (e && e.message ? e.message : e); }" +
//    //            "    }, 2400);" +
//    //            "  }, 100);" +
//    //            "})('" + wanted + "');";

//    //        try { await DashboardView.EvaluateJavaScriptAsync(drive); } catch { }

//    //        // Read probe result
//    //        await Task.Delay(2700);
//    //        string probe2;
//    //        try { probe2 = await DashboardView.EvaluateJavaScriptAsync("window.__routeProbe2 || 'NO_PROBE2'"); }
//    //        catch { probe2 = "NO_PROBE2 (eval failed)"; }
//    //        System.Diagnostics.Debug.WriteLine("RouteProbe2 => " + probe2);

//    //        // (optional) where did we end up?
//    //        await Task.Delay(300);
//    //        try
//    //        {
//    //            var where = await DashboardView.EvaluateJavaScriptAsync("location.pathname + ' ' + location.hash");
//    //            System.Diagnostics.Debug.WriteLine("SPA at: " + where);
//    //        }
//    //        catch { }


//    //        // 2) Read back the result after the driver had time to act
//    //        await Task.Delay(3500);
//    //        string probe;
//    //        try { probe = await DashboardView.EvaluateJavaScriptAsync("window.__routeProbe || 'NO_PROBE'"); }
//    //        catch { probe = "NO_PROBE (eval failed)"; }
//    //        System.Diagnostics.Debug.WriteLine("RouteProbe => " + probe);

//    //        // 3) Optional: log where we ended up
//    //        await Task.Delay(300);
//    //        try
//    //        {
//    //            var where = await DashboardView.EvaluateJavaScriptAsync("location.pathname + ' ' + location.hash");
//    //            System.Diagnostics.Debug.WriteLine("SPA at: " + where);
//    //        }
//    //        catch { }


//    //        // 3) Optional: where did we land?
//    //        await Task.Delay(300);
//    //        try
//    //        {
//    //            var where = await DashboardView.EvaluateJavaScriptAsync("location.pathname + ' ' + location.hash");
//    //            System.Diagnostics.Debug.WriteLine("SPA at: " + where);
//    //        }
//    //        catch { }


//    //        // 3) Optional: log where we ended up
//    //        await Task.Delay(300);
//    //        try
//    //        {
//    //            var where = await DashboardView.EvaluateJavaScriptAsync("location.pathname + ' ' + location.hash");
//    //            System.Diagnostics.Debug.WriteLine("SPA at: " + where);
//    //        }
//    //        catch { }
//    //    }

//    protected override async void OnAppearing()
//    {
//        base.OnAppearing();

//        try
//        {
//#if ANDROID
//            var token = await TryGetTokenAsync();
//            if (!string.IsNullOrWhiteSpace(token))
//            {
//                var mgr = Android.Webkit.CookieManager.Instance;
//                mgr.SetAcceptCookie(true);
//                mgr.SetCookie("http://74.243.216.77:49110", $"Authorization={token}; Path=/; Max-Age=28800");
//                Android.Webkit.CookieManager.Instance.Flush();
//            }
//#endif

//            // Always load the main SPA shell, not /View
//            const string baseSpaUrl = "http://74.243.216.77:49110/Dashboard/Views/index";
//            System.Diagnostics.Debug.WriteLine("Navigating to " + baseSpaUrl);
//            DashboardView.Source = baseSpaUrl;

//            // small boot wait (adjust as needed)
//            await Task.Delay(800);

//            // drive to the chosen dashboard AFTER the SPA exposes its list
//            var wanted = EscapeJs(_dashName ?? "");

//            var driveLate =
//                "(function(name){"
//              + "  var tries=80, called=false;"
//              + "  function lc(s){return (s||'').trim().toLowerCase();}"
//              + "  function hasInList(){"
//              + "    try{var L=window.dbNameList||[]; return L.some(function(x){return lc(x&&x.Name?x.Name:x)===lc(name);});}"
//              + "    catch(e){return false;}"
//              + "  }"
//              + "  function clickMenu(){"
//              + "    try{"
//              + "      var nodes=[].slice.call(document.querySelectorAll('[data-dbname], .opendb, .menu-text'));"
//              + "      var target=nodes.find(function(n){return lc(n.getAttribute&&n.getAttribute('data-dbname')||n.textContent)===lc(name);});"
//              + "      if(!target) return false;"
//              + "      (target.closest?target.closest('a'):null || target)"
//              + "        .dispatchEvent(new MouseEvent('click',{bubbles:true,cancelable:true,view:window}));"
//              + "      console.log('[Drive] clicked menu for',name);"
//              + "      return true;"
//              + "    }catch(e){return false;}"
//              + "  }"
//              + "  (function tick(){"
//              + "    try{"
//              + "      if(!called && typeof window.GetAndUpdateDashBoard==='function' && (hasInList() || (window.dbNameList&&window.dbNameList.length))){"
//              + "        window.GetAndUpdateDashBoard(name);"
//              + "        console.log('[Drive] API call for',name);"
//              + "        called=true; return;"
//              + "      }"
//              + "      if(!called && clickMenu()){ called=true; return; }"
//              + "    }catch(e){}"
//              + "    if(--tries>0){ setTimeout(tick,250); }"
//              + "    else{ try{ location.href='/Dashboard/View?name='+encodeURIComponent(name); console.log('[Drive] hard nav fallback'); }catch(e){} }"
//              + "  })();"
//              + "})('" + wanted + "');";


//            try { await DashboardView.EvaluateJavaScriptAsync(driveLate); } catch { }
//            var jsResult = await DashboardView.EvaluateJavaScriptAsync(driveLate);
//            System.Diagnostics.Debug.WriteLine("[JSResult] " + jsResult);

//        }
//        catch (Exception ex)
//        {
//            System.Diagnostics.Debug.WriteLine("OnAppearing error: " + ex);
//        }
//    }

//    private static async Task<string?> TryGetTokenAsync()
//    {
//        try
//        {
//            var t = await SecureStorage.GetAsync("auth_token");
//            if (!string.IsNullOrWhiteSpace(t)) return t;
//        }
//        catch { }
//        var p = Preferences.Get("auth_token", string.Empty);
//        return string.IsNullOrWhiteSpace(p) ? null : p;
//    }

//    private static string EscapeJs(string s) =>
//        s.Replace("\\", "\\\\").Replace("'", "\\'");
//}







public partial class DashboardPageWithUrl : ContentPage
{
    private readonly string _url;      // should be: http://74.243.216.77:49110/Dashboard/Views/index
    private readonly string _dashName; // e.g. "Map"

    public DashboardPageWithUrl(string url, string dashName)
    {
        InitializeComponent();
        _url = url;
        _dashName = dashName;

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
        System.Diagnostics.Debug.WriteLine("OnAppearing, navigating to: " + _url);
        DashboardView.Source = _url;
        try
        {
#if ANDROID
            // Put the token in native cookie so the first HTML/JSON calls see it
            var cookieToken = await TryGetTokenAsync();
            if (!string.IsNullOrWhiteSpace(cookieToken))
            {
                var mgr = Android.Webkit.CookieManager.Instance;
                mgr.SetAcceptCookie(true);
                mgr.SetCookie(
                    "http://74.243.216.77:49110",
                    $"Authorization={cookieToken}; Path=/; Max-Age=28800"
                );
                Android.Webkit.CookieManager.Instance.Flush();
            }
#endif
            // Always load the SPA shell – no ?token, no /View
            System.Diagnostics.Debug.WriteLine("Navigating to " + _url);
            try { await DashboardView.EvaluateJavaScriptAsync("window.__driveOnce=false;"); } catch { }

            DashboardView.Source = _url;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("OnAppearing error: " + ex);
        }
    }

    private async void OnWebViewNavigated(object? sender, WebNavigatedEventArgs e)
    {
        try
        {
            // 1) Inject Authorization into cookies + storage + fetch/xhr
            var token = await TryGetTokenAsync();
            if (!string.IsNullOrWhiteSpace(token))
            {
                var jsToken = EscapeJs(token);
                var authJs =
                    "(function(t){try{" +
                    "document.cookie='Authorization='+t+'; Path=/; Max-Age=28800';" +
                    "try{localStorage.setItem('Authorization',t);}catch(_){ }" +
                    "try{sessionStorage.setItem('Authorization',t);}catch(_){ }" +
                    "if(!window.__authWrapped){window.__authWrapped=true;" +
                    " var _f=window.fetch; window.fetch=function(u,o){o=o||{};o.headers=o.headers||{};" +
                    "  if(!o.headers['Authorization'])o.headers['Authorization']=t; return _f(u,o);};" +
                    " var _o=XMLHttpRequest.prototype.open,_s=XMLHttpRequest.prototype.send;" +
                    " XMLHttpRequest.prototype.open=function(){_o.apply(this,arguments);};" +
                    " XMLHttpRequest.prototype.send=function(b){try{this.setRequestHeader('Authorization',t);}catch(_){ } _s.call(this,b);};" +
                    "}" +
                    "}catch(_){}})('" + jsToken + "');";

                await DashboardView.EvaluateJavaScriptAsync(authJs);
            }

            // 2) Drive via GetAndUpdateDashBoard(name) ONLY
            var wanted = EscapeJs((_dashName ?? "").Trim());
            //var driveJs =
            //    "(function(name){" +
            //    " if(window.__driveOnce) return;" +
            //    " window.__driveOnce = true;" +
            //    " var tries = 80;" +
            //    " function ready(){" +
            //    "   try{" +
            //    "     if(typeof window.GetAndUpdateDashBoard!=='function') return false;" +
            //    "     var L = window.dbNameList || [];" +
            //    "     return !L.length || true;" +  // don't over-validate list, just wait for API
            //    "   }catch(e){return false;}" +
            //    " }" +
            //    " (function tick(){" +
            //    "   if(ready()){" +
            //    "     try{ window.GetAndUpdateDashBoard(name); }catch(e){}" +
            //    "     try{ history.replaceState(history.state,'','/Dashboard/Views/index'); }catch(e){}" +
            //    "     return;" +
            //    "   }" +
            //    "   if(--tries > 0) setTimeout(tick,250); else window.__driveOnce = false;" +
            //    " })();" +
            //    "})('" + wanted + "');";

            var driveJs =
           "(function(name){"
         + " if(window.__driveOnce)return; window.__driveOnce=true;"
         + " var tries=80;"
         + " function lc(s){return (s||'').trim().toLowerCase();}"
         + " function ready(){"
         + "   try{"
         + "     if(typeof window.GetAndUpdateDashBoard!=='function') return false;"
         + "     var L=window.dbNameList||[];"
         + "     return !L.length || L.some(function(x){var n=(x&&x.Name?x.Name:x)||''; return lc(n)===lc(name);});"
         + "   }catch(e){return false;}"
         + " }"
         + " (function tick(){"
         + "   if(ready()){"
         + "     try{window.GetAndUpdateDashBoard(name);}catch(_){ }"
         + "     try{history.replaceState(history.state,'','/Dashboard/Views/index');}catch(_){ }"
         + "     return;"
         + "   }"
         + "   if(--tries>0) setTimeout(tick,250); else window.__driveOnce=false;"
         + " })();"
         + "})('" + wanted + "');";
            await DashboardView.EvaluateJavaScriptAsync(driveJs);

            var driveRes = await DashboardView.EvaluateJavaScriptAsync(driveJs);
            System.Diagnostics.Debug.WriteLine("[DriveRes] " + driveRes);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine("OnWebViewNavigated error: " + ex);
        }
    }


    //private async void OnWebViewNavigated(object? sender, WebNavigatedEventArgs e)
    //{
    //    try
    //    {
    //        // 1) Inject Authorization (cookie + storage + fetch/xhr)
    //        var token = await TryGetTokenAsync();
    //        if (!string.IsNullOrWhiteSpace(token))
    //        {
    //            var jsToken = EscapeJs(token);
    //            var authJs =
    //                "(function(t){try{" +
    //                "document.cookie='Authorization='+t+'; Path=/; Max-Age=28800';" +
    //                "try{localStorage.setItem('Authorization',t);}catch(_){ }" +
    //                "try{sessionStorage.setItem('Authorization',t);}catch(_){ }" +
    //                "if(!window.__authWrapped){window.__authWrapped=true;" +
    //                " var _f=window.fetch; window.fetch=function(u,o){o=o||{};o.headers=o.headers||{};" +
    //                " if(!o.headers['Authorization'])o.headers['Authorization']=t; return _f(u,o);};" +
    //                " var _o=XMLHttpRequest.prototype.open,_s=XMLHttpRequest.prototype.send;" +
    //                " XMLHttpRequest.prototype.open=function(){_o.apply(this,arguments);};" +
    //                " XMLHttpRequest.prototype.send=function(b){try{this.setRequestHeader('Authorization',t);}catch(_){ } _s.call(this,b);};" +
    //                "}" +
    //                "}catch(_){}})('" + jsToken + "');";

    //            await DashboardView.EvaluateJavaScriptAsync(authJs);
    //        }

    //        // 2) Drive via GetAndUpdateDashBoard(name) ONLY (no hash/menu clicks)
    //        var wanted = EscapeJs((_dashName ?? "").Trim());
    //        var driveJs =
    //            "(function(name){" +
    //            " if(window.__driveOnce)return; window.__driveOnce=true;" +
    //            " var tries=80;" +
    //            " function lc(s){return (s||'').trim().toLowerCase();}" +
    //            " function ready(){" +
    //            " try{" +
    //            " if(typeof window.GetAndUpdateDashBoard!=='function') return false;" +
    //            " var L=window.dbNameList||[];" +
    //            " return !L.length || L.some(function(x){var n=(x&&x.Name?x.Name:x)||''; return lc(n)===lc(name);});" +
    //            " }catch(e){return false;}" +
    //            " }" +
    //            " (function tick(){" +
    //            " if(ready()){" +
    //            " try{window.GetAndUpdateDashBoard(name);}catch(_){ }" +
    //            " try{history.replaceState(history.state,'','/Dashboard/Views/index');}catch(_){ }" +
    //            " return;" +
    //            " }" +
    //            " if(--tries>0) setTimeout(tick,250); else window.__driveOnce=false;" +
    //            " })();" +
    //            "})('" + wanted + "');";

    //        await DashboardView.EvaluateJavaScriptAsync(driveJs);

    //        // 2) Wrap GetAndUpdateDashBoard so our target runs
    //        //var target = EscapeJs((_dashName ?? "").Trim());
    //        //if (!string.IsNullOrWhiteSpace(target))
    //        //{
    //        //    var wrapJs =
    //        //        "(function(targetName){" +
    //        //        "  if(!targetName) return;" +
    //        //        "  function wait(){" +
    //        //        "    try{" +
    //        //        "      if(typeof window.GetAndUpdateDashBoard!=='function'){" +
    //        //        "        setTimeout(wait,200); return;" +
    //        //        "      }" +
    //        //        "      if(window.__dbWrapped) return;" +
    //        //        "      window.__dbWrapped=true;" +
    //        //        "      var original=window.GetAndUpdateDashBoard;" +
    //        //        "      var switched=false;" +
    //        //        "      window.GetAndUpdateDashBoard=function(dbName){" +
    //        //        "        var res = original.apply(this, arguments);" +
    //        //        "        if(!switched){" +
    //        //        "          switched=true;" +
    //        //        "          if(targetName && targetName!==dbName){" +
    //        //        "            setTimeout(function(){ original.call(window, targetName); }, 0);" +
    //        //        "          }" +
    //        //        "        }" +
    //        //        "        return res;" +
    //        //        "      };" +
    //        //        "    }catch(e){ setTimeout(wait,200); }" +
    //        //        "  }" +
    //        //        "  wait();" +
    //        //        "})('" + target + "');";

    //        //    await DashboardView.EvaluateJavaScriptAsync(wrapJs);
    //        //}

    //    }
    //    catch (Exception ex)
    //    {
    //        System.Diagnostics.Debug.WriteLine("OnWebViewNavigated error: " + ex);
    //    }
    //}


    private static async Task<string?> TryGetTokenAsync()
    {
        try
        {
            var t = await SecureStorage.GetAsync("auth_token");
            if (!string.IsNullOrWhiteSpace(t)) return t;
        }
        catch { }

        var p = Preferences.Get("auth_token", string.Empty);
        return string.IsNullOrWhiteSpace(p) ? null : p;
    }

    private static string EscapeJs(string s) =>
        s.Replace("\\", "\\\\").Replace("'", "\\'");
}

