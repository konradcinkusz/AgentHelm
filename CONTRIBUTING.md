# Contributing to AgentHelm

Thank you for your interest in AgentHelm! This document describes how to set up a development environment, run tests, and submit changes.

## Prerequisites

| Tool | Version | Notes |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | 8.0.303+ | `dotnet --version` to verify — must be 8.0.303+ for the Aspire SDK element |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | any recent | needed for Postgres + pgAdmin containers via Aspire |
| (optional) ACP agent | any | GitHub Copilot CLI, Claude Code, or Gemini CLI for end-to-end testing |

No `aspire` workload — this repo uses the SDK-based setup (`<Sdk Name="Aspire.AppHost.Sdk" .../>`) so the workload is not required. If you installed it before, `dotnet workload uninstall aspire` removes it.

## Quick dev loop

```bash
# clone
git clone https://github.com/konradcinkusz/agenthelm.git
cd agenthelm

# restore & build everything (including AppHost)
dotnet build AgentHelm.sln -c Release

# run the full stack (Postgres + Bridge + Web via Aspire)
dotnet run --project src/AgentHelm.AppHost

# the Aspire dashboard URL is printed in the console;
# the Web UI URL is shown as the "web" resource endpoint
```

The built-in echo agent works immediately — no external agent required. To test a real agent, have it on your PATH and start a new session selecting it from the catalog.

## Running tests

```bash
dotnet test tests/AgentHelm.Tests -c Release --logger "console;verbosity=normal"
```

All tests live in `tests/AgentHelm.Tests`. They run without Docker or a live agent.

## Project layout

```
src/
  AgentHelm.AppHost/   Aspire orchestration (Postgres container, Bridge + Web as local processes)
  AgentHelm.Bridge/    Session management, ACP protocol client, permission gateway, REST API, SSE
  AgentHelm.Web/       Blazor Server UI (sessions, transcript, git diff, terminal, history)
tests/
  AgentHelm.Tests/     xUnit — ACP protocol, sessions, policy engine, git service, terminal, handoff
tools/
  AgentHelm.EchoAgent/ Built-in demo ACP agent (also included in the Bridge Docker image)
```

## Container development

```bash
# Build and run the full stack from source (no .NET SDK needed at runtime):
docker compose up --build

# UI at http://localhost:5200 · Bridge at http://localhost:5199
```

See `Dockerfile`, `Dockerfile.web`, and `docker-compose.yml` for details.
Only the built-in echo agent is available inside containers — real ACP agents
(Copilot CLI, Claude Code, Gemini) need your local environment and credentials.

## Submitting changes

1. **Fork** the repository and create a feature branch from `main`.
2. Keep changes focused — one logical change per PR.
3. Add or update tests for any new logic in `AgentHelm.Bridge`.
4. Run `dotnet test` and `dotnet build AgentHelm.sln` before pushing.
5. Open a pull request against `main`. The PR description should explain *why* the change is needed, not just what it does.

## Architecture notes

- **Bridge binds to loopback (`127.0.0.1:5199`) by default.** This is a security constraint — ACP agents are local subprocesses with access to your repositories and credentials. Exposing the Bridge further requires an explicit `AgentHelm:Urls` override and an `AgentHelm:ApiToken`. Container deployments set `AgentHelm__Urls=http://0.0.0.0:5199` automatically (see `Dockerfile`) but should always be run with a token.
- **Session aggregation** happens in `HelmSession` inside `SessionManager`. Transcript mutations are lock-guarded; permission resolution blocks the agent turn until the user decides.
- **Policy engine** (`PermissionPolicies`) — `ask` (default), `auto_read`, `yolo`. `auto_read` never auto-allows `fetch` (network exfiltration risk). `yolo` requires explicit per-session confirmation and audits every auto-decision.
- **ACP adapter** (`AcpAdapter`) decodes JSON-RPC over stdio; the `IAgentAdapter` seam keeps protocol details out of sessions and UI.
- **Persistence** (`SessionRepository`) stores a JSONB snapshot per session in Postgres. The `PersistenceWriter` upserts on a 1 s write-behind; a Postgres outage degrades to memory-only without blocking the agent.
- **Git endpoints** hard-guard every path to the session's `Cwd` — the Bridge re-derives tracked/untracked status server-side, never trusting the request.

## Code style

- C# 12 / .NET 8 idioms (primary constructors, collection expressions, pattern switches).
- No XML doc comments except on non-obvious public APIs.
- No abbreviations unless domain-standard (`acp`, `pty`, `sse`, `cwd`).
- Records for DTOs and value objects; mutable classes only for aggregates that need lock-guarded mutation (`HelmSession`, `SessionManager`).

## Questions / ideas

Open a [GitHub Discussion](https://github.com/konradcinkusz/agenthelm/discussions) for design questions or feature proposals before writing a large PR. Bug reports go to [Issues](https://github.com/konradcinkusz/agenthelm/issues).
