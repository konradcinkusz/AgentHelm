// =============================================================================
// GitHub Copilot SDK adapter — M1 skeleton, gated behind COPILOT_SDK.
//
// Why gated: the SDK is in public preview, its NuGet cannot be restored in the
// build sandbox, and it has already shipped one silent breaking change
// (github/copilot-cli#1606: --headless --stdio removed in CLI v0.0.410+,
// breaking every SDK version downstream). Shipping uncompilable code that
// pretends to be done would be worse than shipping an honest, structured
// skeleton. Copilot users are fully served TODAY through the AcpAdapter
// (`copilot --acp --stdio`); this adapter adds Copilot-specific depth.
//
// To activate on a machine with NuGet access:
//   1. AgentHelm.Bridge.csproj:
//        <PackageReference Include="GitHub.Copilot.Sdk" Version="<latest-preview>" />
//        <DefineConstants>$(DefineConstants);COPILOT_SDK</DefineConstants>
//   2. Fill the four TODO blocks below against the SDK docs
//      (https://github.com/github/copilot-sdk — .NET package README).
//   3. Pin the CLI: install a known-good copilot version and run the runtime
//      with --no-auto-update (lesson of #1606).
//
// What this adapter adds over ACP once finished:
//   - model selection per session (SessionConfig.Model)
//   - session resume via SDK-native session ids
//   - custom tools/agents/skills registration
//   - TelemetryConfig pointed at the local CopilotScope collector
//     (http://localhost:4318) — Helm sessions show up in Scope automatically.
// =============================================================================
#if COPILOT_SDK
using AgentHelm.Bridge.Agents.Acp;
using AgentHelm.Bridge.Sessions;

namespace AgentHelm.Bridge.Agents.CopilotSdk;

public sealed class CopilotSdkAdapter : IAgentAdapter
{
    private readonly AgentSpec _spec;
    private readonly string _cwd;
    private readonly ILogger _logger;
    private readonly string? _model;
    private readonly string? _resumeSessionId;

    public CopilotSdkAdapter(AgentSpec spec, string cwd, ILogger logger,
        string? model = null, string? resumeSessionId = null)
    {
        _spec = spec;
        _cwd = cwd;
        _logger = logger;
        _model = model;
        _resumeSessionId = resumeSessionId;
    }

    public string? NativeSessionId { get; private set; }
    public AgentCaps? Capabilities => null;   // TODO(3b): map SDK capabilities
    public event Action<AgentEvent>? OnEvent;
    public Func<PermissionAsk, Task<string?>>? PermissionHandler { get; set; }

    public Task StartAsync(CancellationToken ct)
    {
        // TODO(1): create the CopilotClient.
        //   - default: spawn the bundled CLI over stdio
        //   - alternative: connect to a shared `copilot --headless --port` server (cliUrl)
        //   - set TelemetryConfig endpoint to the CopilotScope collector if configured
        // TODO(2): create or resume the session.
        //   - new: pass WorkingDirectory=_cwd and Model=_model
        //   - resume: _resumeSessionId → SDK resume API; set NativeSessionId
        // TODO(3): subscribe to session events and translate to AgentEvent:
        //   - assistant delta   → new AgentEvent("assistant_chunk", text)
        //   - tool invocation   → new AgentEvent("tool_call", title, raw)
        //   - tool completion   → new AgentEvent("tool_update", status, raw)
        // TODO(4): wire the SDK permission callback:
        //   - map the SDK request to PermissionAsk (options: allow/reject)
        //   - await PermissionHandler — the Bridge policy engine and UI take over
        throw new NotImplementedException("Fill TODO(1..4) against the Copilot SDK docs.");
    }

    public Task<string> PromptAsync(string text, CancellationToken ct) =>
        throw new NotImplementedException();

    public Task CancelAsync(CancellationToken ct) => Task.CompletedTask;

    public void Dispose() { }
}
#endif
