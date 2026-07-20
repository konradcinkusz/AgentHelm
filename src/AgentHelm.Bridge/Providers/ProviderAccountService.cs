using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AgentHelm.Bridge.Providers;

/// <summary>Account status for one configured AI provider (copilot, claude, gemini).</summary>
public record ProviderInfo(
    string Id,
    string Name,
    string? Account,
    string Status,          // "logged_in" | "logged_out" | "unknown"
    string? LoginCommand,
    string? LogoutCommand
);

/// <summary>
/// Reads local auth files to report which account each provider is logged in as,
/// and manages login/logout subprocesses.
/// </summary>
public sealed class ProviderAccountService
{
    private static readonly string Home =
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    private readonly Dictionary<string, LoginProcess> _logins = new();
    private readonly object _loginLock = new();

    public ProviderInfo? GetInfo(string agentId, string agentName) => agentId switch
    {
        "copilot" => GetCopilotInfo(agentName),
        "claude"  => GetClaudeInfo(agentName),
        "gemini"  => GetGeminiInfo(agentName),
        _         => null
    };

    // GitHub Copilot: gh CLI stores the logged-in user in ~/.config/gh/hosts.yml
    private static ProviderInfo GetCopilotInfo(string name)
    {
        try
        {
            var path = Path.Combine(Home, ".config", "gh", "hosts.yml");
            if (File.Exists(path))
            {
                var text = File.ReadAllText(path);
                var m = Regex.Match(text, @"(?m)^\s+user:\s*(\S+)");
                if (m.Success)
                    return new("copilot", name, m.Groups[1].Value, "logged_in",
                        LoginCommand: "gh auth login",
                        LogoutCommand: "gh auth logout -h github.com --yes");
            }
        }
        catch { }
        return new("copilot", name, null, "logged_out", "gh auth login", null);
    }

    // Claude Code: stores OAuth account in ~/.claude.json
    private static ProviderInfo GetClaudeInfo(string name)
    {
        try
        {
            var path = Path.Combine(Home, ".claude.json");
            if (File.Exists(path))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                if (doc.RootElement.TryGetProperty("oauthAccount", out var acct))
                {
                    var email = acct.TryGetProperty("email", out var e) ? e.GetString() : null;
                    if (!string.IsNullOrEmpty(email))
                        return new("claude", name, email, "logged_in",
                            LoginCommand: "claude auth login",
                            LogoutCommand: "claude auth logout");
                }
            }
        }
        catch { }
        return new("claude", name, null, "logged_out", "claude auth login", null);
    }

    // Gemini CLI: stores selected account in ~/.gemini/settings.json
    private static ProviderInfo GetGeminiInfo(string name)
    {
        try
        {
            var path = Path.Combine(Home, ".gemini", "settings.json");
            if (File.Exists(path))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                if (doc.RootElement.TryGetProperty("selectedAccount", out var acct))
                {
                    var email = acct.TryGetProperty("email", out var e) ? e.GetString() : null;
                    if (!string.IsNullOrEmpty(email))
                        return new("gemini", name, email, "logged_in",
                            LoginCommand: "gemini auth",
                            LogoutCommand: "gemini auth logout");
                }
            }
        }
        catch { }
        return new("gemini", name, null, "logged_out", "gemini auth", null);
    }

    // ---- Login process management ----

    public LoginProcess StartLogin(string agentId, string command)
    {
        lock (_loginLock)
        {
            if (_logins.TryGetValue(agentId, out var existing) && !existing.Completed)
                return existing;
            var p = new LoginProcess(command);
            _logins[agentId] = p;
            _ = p.RunAsync();
            return p;
        }
    }

    public LoginProcess? GetLogin(string agentId)
    {
        lock (_loginLock)
            return _logins.TryGetValue(agentId, out var p) ? p : null;
    }

    public void ClearLogin(string agentId)
    {
        lock (_loginLock)
            _logins.Remove(agentId);
    }
}

/// <summary>
/// Runs a login command as a subprocess and buffers its output so multiple
/// SSE subscribers can replay from the beginning and receive new lines.
/// </summary>
public sealed class LoginProcess
{
    private readonly string _command;
    private readonly List<string> _lines = [];
    private readonly object _lock = new();

    public bool Completed { get; private set; }
    public int? ExitCode { get; private set; }

    public LoginProcess(string command) => _command = command;

    public async Task RunAsync()
    {
        Emit($"$ {_command}");
        var parts = _command.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var psi = new ProcessStartInfo(parts[0], parts.Length > 1 ? parts[1] : "")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        try
        {
            using var proc = Process.Start(psi)
                ?? throw new InvalidOperationException("Process.Start returned null.");
            await Task.WhenAll(DrainAsync(proc.StandardOutput), DrainAsync(proc.StandardError));
            await proc.WaitForExitAsync();
            ExitCode = proc.ExitCode;
            Emit(ExitCode == 0 ? "[done — exit 0]" : $"[process exited with code {ExitCode}]");
        }
        catch (Exception ex)
        {
            Emit($"[error: {ex.Message}]");
        }
        finally
        {
            lock (_lock) Completed = true;
        }
    }

    private async Task DrainAsync(StreamReader reader)
    {
        while (await reader.ReadLineAsync() is { } line)
            Emit(line);
    }

    private void Emit(string line) { lock (_lock) _lines.Add(line); }

    /// <summary>
    /// Yields all buffered lines from the beginning, then polls for new lines
    /// until the process exits. Safe to call from multiple concurrent SSE handlers.
    /// </summary>
    public async IAsyncEnumerable<string> StreamAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        int idx = 0;
        while (!ct.IsCancellationRequested)
        {
            string[] batch;
            bool done;
            lock (_lock)
            {
                batch = idx < _lines.Count ? [.. _lines[idx..]] : [];
                idx += batch.Length;
                done = Completed && idx >= _lines.Count;
            }
            foreach (var line in batch) yield return line;
            if (done) break;
            await Task.Delay(200, ct).ConfigureAwait(false);
        }
    }
}
