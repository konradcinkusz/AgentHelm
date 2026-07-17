using System.Text.Json.Nodes;
using AgentHelm.Bridge.Sessions;

namespace AgentHelm.Bridge.Integrations;

// CopilotScope integration: the value axis inside the control plane. Helm
// asks the Scope collector's REST API for recent sessions and shows their
// quality scores next to the Helm session.
//
// Honest limitation, stated in the UI too: correlation is TIME-BASED
// best-effort. Scope sessions are keyed by OTel conversation ids that Helm
// cannot see (they live inside the agent's telemetry), so Helm shows Scope
// sessions whose activity overlaps this session's lifetime and lets the user
// eyeball the match. Exact correlation needs session tagging on the telemetry
// path — tracked as a Beyond-M3 item on both projects.

public sealed record ScopeSessionScore(
    string Id, string? Title, double? Score, string? Grade, double? Confidence,
    DateTimeOffset? LastActivity);

public sealed class ScopeClient
{
    private readonly HttpClient? _http;
    private readonly ILogger<ScopeClient> _logger;
    private DateTimeOffset _backoffUntil = DateTimeOffset.MinValue;

    public ScopeClient(IConfiguration configuration, ILogger<ScopeClient> logger)
    {
        _logger = logger;
        var baseUrl = configuration["AgentHelm:Scope:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl)) baseUrl = "http://localhost:4318";
        if (baseUrl == "disabled") return;
        _http = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(3)   // an absent Scope must never make Helm feel slow
        };
    }

    /// <summary>Recent Scope sessions, or null when Scope is unreachable/disabled.</summary>
    public async Task<List<ScopeSessionScore>?> RecentAsync(CancellationToken ct)
    {
        if (_http is null || DateTimeOffset.UtcNow < _backoffUntil) return null;
        try
        {
            var raw = await _http.GetStringAsync("/api/sessions", ct);
            return ParseSessions(raw);
        }
        catch (Exception ex)
        {
            // Back off for a minute so every UI click doesn't retry a dead endpoint.
            _backoffUntil = DateTimeOffset.UtcNow.AddMinutes(1);
            _logger.LogDebug(ex, "CopilotScope not reachable; hiding scores for 60 s.");
            return null;
        }
    }

    /// <summary>
    /// Tolerant parser: Scope's DTO shape may drift between versions, so every
    /// field is probed under its known aliases and nothing throws on absence.
    /// </summary>
    internal static List<ScopeSessionScore> ParseSessions(string json)
    {
        var result = new List<ScopeSessionScore>();
        JsonNode? root;
        try { root = JsonNode.Parse(json); }
        catch { return result; }

        var array = root as JsonArray ?? root?["sessions"] as JsonArray;
        if (array is null) return result;

        foreach (var node in array.OfType<JsonNode>())
        {
            var quality = node["quality"];
            var id = FirstString(node, "id", "sessionId", "conversationId");
            if (id is null) continue;
            result.Add(new ScopeSessionScore(
                id,
                FirstString(node, "title", "name", "model"),
                FirstDouble(quality, node, "score"),
                FirstString(quality, "grade") ?? FirstString(node, "grade"),
                FirstDouble(quality, node, "confidence"),
                FirstDate(node, "lastActivity", "lastActivityUtc", "updatedAt", "lastSeen")));
        }
        return result;
    }

    private static string? FirstString(JsonNode? node, params string[] keys)
    {
        foreach (var key in keys)
            if (node?[key] is { } v)
                try { return v.GetValue<string>(); } catch { /* wrong type — keep probing */ }
        return null;
    }

    private static double? FirstDouble(JsonNode? preferred, JsonNode? fallback, string key)
    {
        foreach (var node in new[] { preferred, fallback })
            if (node?[key] is { } v)
                try { return v.GetValue<double>(); } catch { /* keep probing */ }
        return null;
    }

    private static DateTimeOffset? FirstDate(JsonNode? node, params string[] keys)
    {
        foreach (var key in keys)
            if (node?[key] is { } v)
            {
                try { return v.GetValue<DateTimeOffset>(); } catch { /* keep probing */ }
                try { if (DateTimeOffset.TryParse(v.GetValue<string>(), out var parsed)) return parsed; }
                catch { /* keep probing */ }
            }
        return null;
    }

    /// <summary>
    /// Scope sessions whose activity overlaps the Helm session window
    /// (± 2 min slack for clock and flush latency), newest first, top 3.
    /// Sessions without a timestamp are excluded — no timestamp, no claim.
    /// </summary>
    internal static List<ScopeSessionScore> Correlate(
        IEnumerable<ScopeSessionScore> sessions, DateTimeOffset from, DateTimeOffset to)
    {
        var slack = TimeSpan.FromMinutes(2);
        return sessions
            .Where(s => s.LastActivity is { } t && t >= from - slack && t <= to + slack)
            .OrderByDescending(s => s.LastActivity)
            .Take(3)
            .ToList();
    }
}
