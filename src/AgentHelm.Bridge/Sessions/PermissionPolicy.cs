namespace AgentHelm.Bridge.Sessions;

/// <summary>
/// Permission policy levels. A policy can AUTO-ALLOW; it never auto-rejects —
/// rejection is always a human decision.
/// </summary>
public static class PermissionPolicies
{
    /// <summary>Every tool call asks the user. The default.</summary>
    public const string Ask = "ask";

    /// <summary>
    /// Read-only tool kinds are auto-allowed; anything that mutates or executes
    /// still asks. Note: "fetch" (network) is deliberately NOT auto-allowed —
    /// a network call can exfiltrate what a read just loaded.
    /// </summary>
    public const string AutoRead = "auto_read";

    /// <summary>
    /// Everything is auto-allowed. Explicit, per-session opt-in only; every
    /// auto-decision still lands in the transcript as an audit entry.
    /// </summary>
    public const string Yolo = "yolo";

    public static readonly string[] All = [Ask, AutoRead, Yolo];

    public static bool IsValid(string? policy) =>
        policy is Ask or AutoRead or Yolo;
}

public sealed record PolicyDecision(bool IsAuto, string? OptionId, string Reason)
{
    public static readonly PolicyDecision AskUser = new(false, null, "policy requires user decision");
}

/// <summary>
/// Decides whether a permission request can be answered automatically under
/// the session's policy. Stateless and pure — trivially testable.
/// </summary>
public static class PolicyEngine
{
    /// <summary>ACP ToolKind values considered read-only for auto_read.</summary>
    private static readonly HashSet<string> ReadOnlyKinds =
        new(StringComparer.OrdinalIgnoreCase) { "read", "search", "think" };

    public static PolicyDecision Decide(PermissionAsk ask, string policy)
    {
        var auto = policy switch
        {
            PermissionPolicies.Yolo => true,
            PermissionPolicies.AutoRead => ReadOnlyKinds.Contains(ask.ToolKind),
            _ => false
        };
        if (!auto) return PolicyDecision.AskUser;

        // Prefer allow_once over allow_always: the policy lives in AgentHelm,
        // so we do not let the agent persist a broader grant on its side.
        var allowOptions = ask.Options
            .Where(o => o.Kind.Contains("allow", StringComparison.OrdinalIgnoreCase))
            .ToList();
        var option = allowOptions.FirstOrDefault(o => o.Kind.Contains("once", StringComparison.OrdinalIgnoreCase));
        if (option == default && allowOptions.Count > 0) option = allowOptions[0];

        return option == default
            ? PolicyDecision.AskUser   // agent offered no allow option — a human should look at that
            : new PolicyDecision(true, option.OptionId,
                $"auto-allowed by policy '{policy}': {ask.ToolTitle} ({ask.ToolKind})");
    }
}
