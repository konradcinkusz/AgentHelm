using AgentHelm.Web.Services;
using Microsoft.AspNetCore.Components;

namespace AgentHelm.Web.Components.Pages;

public partial class Settings : IDisposable
{
    private List<ProviderInfoDto> _providers = [];
    private Dictionary<string, List<ProviderModelDto>> _models = new();
    private bool _loading = true;
    private string? _loggingOut;

    // Per-provider login SSE state
    private readonly Dictionary<string, List<string>> _loginLogs = new();
    private readonly Dictionary<string, CancellationTokenSource> _loginCts = new();

    protected override async Task OnInitializedAsync()
    {
        await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        _loading = true;
        StateHasChanged();
        _providers = await Bridge.GetProvidersAsync();

        // Load models for each logged-in provider that supports model listing.
        _models.Clear();
        foreach (var p in _providers.Where(p => p.Status == "logged_in"))
        {
            var list = await Bridge.GetProviderModelsAsync(p.Id);
            if (list.Count > 0) _models[p.Id] = list;
        }

        _loading = false;
        StateHasChanged();
    }

    private bool IsLoggingIn(string id) => _loginCts.ContainsKey(id);

    private async Task StartLoginAsync(string id)
    {
        if (_loginCts.ContainsKey(id)) return;

        _loginLogs[id] = [];
        var cts = new CancellationTokenSource();
        _loginCts[id] = cts;
        StateHasChanged();

        var started = await Bridge.StartProviderLoginAsync(id, cts.Token);
        if (!started)
        {
            _loginLogs[id].Add("[error: failed to start login process]");
            _loginCts.Remove(id);
            StateHasChanged();
            return;
        }

        try
        {
            await Bridge.SubscribeProviderLoginAsync(id, async evt =>
            {
                if (evt.Kind == "output" && evt.Text is not null)
                {
                    _loginLogs[id].Add(evt.Text);
                    await InvokeAsync(StateHasChanged);
                }
                else if (evt.Kind == "done")
                {
                    _loginCts.Remove(id);
                    await InvokeAsync(async () =>
                    {
                        await RefreshAsync();
                    });
                }
            }, cts.Token);
        }
        catch (OperationCanceledException) { }
        finally
        {
            _loginCts.Remove(id);
            await InvokeAsync(StateHasChanged);
        }
    }

    private async Task LogoutAsync(string id)
    {
        _loggingOut = id;
        StateHasChanged();
        try
        {
            await Bridge.LogoutProviderAsync(id);
            await RefreshAsync();
        }
        finally
        {
            _loggingOut = null;
            StateHasChanged();
        }
    }

    private static string ProviderIcon(string id) => id switch
    {
        "copilot" => "◆",
        "claude"  => "◈",
        "gemini"  => "✦",
        _         => "○"
    };

    private static string BadgeClass(string status) => status switch
    {
        "logged_in"  => "badge-ok",
        "logged_out" => "badge-off",
        _            => "badge-unknown"
    };

    private static string BadgeLabel(string status) => status switch
    {
        "logged_in"  => "Logged in",
        "logged_out" => "Not logged in",
        _            => "Unknown"
    };

    public void Dispose()
    {
        foreach (var cts in _loginCts.Values)
            cts.Cancel();
    }
}
