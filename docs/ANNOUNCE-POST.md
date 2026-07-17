# AgentHelm: one web cockpit for your coding agents

I have been building AI-coding tooling in the open lately. First came
[CopilotScope](https://github.com/konradcinkusz/copilotscope) — a local
collector that turns GitHub Copilot's OpenTelemetry export into a per-session
quality score. Today its sibling:
[AgentHelm](https://github.com/konradcinkusz/agenthelm) — a web GUI that
*drives* agents, where Scope *observes* them.

## What it does

One Blazor cockpit for any agent speaking the
[Agent Client Protocol](https://agentclientprotocol.com) — GitHub Copilot CLI
(`copilot --acp`), Claude Code, Gemini CLI, and the rest of the ~50-agent ACP
registry. An agent is a config entry, not a plugin.

- **Transparent sessions** — streamed chat, full transcript, nothing hidden.
- **Permission policies with an audit trail** — `ask` everything, auto-allow
  read-only kinds, or YOLO behind an explicit confirmation. Every decision —
  human or automatic — lands in the transcript. Policies can auto-*allow*;
  rejection is always a human.
- **Session resume** — archived sessions carry the agent-side id; one click
  replays them via ACP `session/load`.
- **Workbench** — git diff viewer with accept (stage) / reject (revert,
  audited), a PTY terminal (xterm.js) with "attach output to prompt", image
  and text attachments as native ACP content blocks.
- **Agent handoff** — switch agents mid-conversation; the summary lands in
  the new composer for *you* to review and send, never auto-sent.
- **The value axis** — quality scores from CopilotScope shown inline next to
  the session. Cost meters exist everywhere; this is the "was it worth it".

## Honest limitations

The Copilot SDK adapter ships as a gated skeleton (the SDK is in preview and
has already broken compatibility once — ACP covers Copilot today). The
Scope↔Helm match is time-based best-effort until telemetry tagging lands.
Windows terminal is a pipe, not ConPTY. All tracked openly in the README.

## Try it

Grab the release zip, `./run.sh`, and talk to the built-in echo agent in two
minutes — no real agent required to see the whole loop, permissions included.
MIT licensed; 41 tests; issues and disagreements welcome.
