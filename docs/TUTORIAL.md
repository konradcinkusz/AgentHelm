# AgentHelm Tutorial

This tutorial walks you through AgentHelm from a fresh install to a real working session with a coding agent. No prior experience with ACP or .NET Aspire is needed.

**Time to complete:** ~20 minutes  
**What you'll learn:** install, first echo session, permission policies, connecting Claude Code, git diff review, terminal, session history

---

## 1. Install and start

### Prerequisites

- .NET SDK 8.0.303 or newer — check with `dotnet --version`
- Docker (optional) — only needed for persistent session history

### Quickest start: release zip

Download the latest zip from [Releases](https://github.com/konradcinkusz/AgentHelm/releases), unpack it, and run:

```bash
# macOS / Linux
./run.sh

# Windows
.\run.ps1
```

The Bridge starts on `http://127.0.0.1:5199` and the UI on `http://127.0.0.1:5200`. Open `http://127.0.0.1:5200` in your browser.

### From source (recommended for development)

```bash
git clone https://github.com/konradcinkusz/AgentHelm.git
cd AgentHelm

# With Aspire — starts Postgres + Bridge + Web
dotnet run --project src/AgentHelm.AppHost
```

Aspire prints a `web` URL (e.g. `http://localhost:18888`) — open that.

> If you see "Aspire Workload has been deprecated": update your .NET SDK to 8.0.303+ and optionally run `dotnet workload uninstall aspire`.

---

## 2. Your first session (no real agent needed)

AgentHelm ships with a built-in **echo agent** — it speaks the full ACP protocol and is perfect for learning the interface before you connect a real agent.

1. Click **＋ New** in the left rail.
2. Pick **Echo (built-in demo agent)** from the Agent dropdown.
3. Set any existing directory as the Working Directory (e.g. your home folder).
4. Click **Start session**.

You'll see the session open with the transcript panel on the right.

**Try a basic prompt:**
- Type `Hello!` and press Enter.
- The echo agent streams the response back. Notice the chunk-by-chunk rendering in the transcript.

**Try a tool call:**
- Type any message containing the word **tool** (e.g. `Please use a tool here`).
- The echo agent will request permission to run a tool.
- An **amber banner** appears at the top of the session. You have three choices:
  - **Allow** — the tool runs; an audit entry is added to the transcript.
  - **Allow once** — allows this specific tool one time.
  - **Reject** — the tool is denied; the rejection is also audited.

This is the core of AgentHelm's permission gateway. Every decision — human or automatic — lands in the transcript as a permanent record.

---

## 3. Permission policies

Approving every tool call manually can become tedious for long sessions. AgentHelm offers three policies you can switch per session:

| Policy | Behaviour |
|---|---|
| **Ask** (default) | Every tool call shows an approval prompt |
| **Auto-read** | Read-only tool kinds are auto-allowed; write operations and network `fetch` still require approval |
| **YOLO** | Everything is auto-allowed — requires an explicit click to enable per session |

To change the policy: look for the **policy selector** in the session header.

> Auto-read deliberately excludes `fetch` even though it is read-only: a fetch can exfiltrate what a file read just loaded. The exclusion is intentional.

YOLO is designed for trusted sessions where you want the agent to run uninterrupted. It always requires an explicit confirmation click — it can never be set as a global default.

---

## 4. Connecting a real agent

### Claude Code

Make sure Node.js is installed, then add this entry to `src/AgentHelm.Bridge/appsettings.json` under `AgentHelm:Agents` (it is pre-configured by default):

```json
{
  "Id": "claude",
  "Name": "Claude Code",
  "Command": "npx",
  "Args": ["@zed-industries/claude-code-acp"],
  "Type": "acp"
}
```

Restart the Bridge and Claude Code will appear in the **＋ New** dropdown. The first session will take a moment while npx downloads the adapter.

### GitHub Copilot CLI

Prerequisites: `copilot` on PATH and logged in (`copilot login`).

```json
{
  "Id": "copilot",
  "Name": "GitHub Copilot CLI",
  "Command": "copilot",
  "Args": ["--acp", "--stdio"],
  "Type": "acp"
}
```

### Gemini CLI

Prerequisites: `gemini` on PATH and authenticated.

```json
{
  "Id": "gemini",
  "Name": "Gemini CLI",
  "Command": "gemini",
  "Args": ["--acp"],
  "Type": "acp"
}
```

All three are pre-configured in the default `appsettings.json` — just make sure the CLI is installed.

---

## 5. The git diff viewer (Changes tab)

When an agent modifies files in the working directory, you can review the changes in the **Changes** tab of the session.

1. Open a session with the working directory set to a git repository.
2. Ask the agent to make a change (e.g. `Create a file called hello.txt with the text "hello world"`).
3. Switch to the **Changes** tab.
4. You'll see a list of modified files with `+` / `−` counts and a per-file diff.

For each file you can:
- **Accept** — stages the file (`git add`). The action is audited in the transcript.
- **Reject** — reverts the change (`git checkout HEAD --` for tracked files, delete for untracked). Also audited.

All file paths are hard-guarded to the session working directory — an agent cannot accept or reject files outside the cwd through this interface.

---

## 6. The integrated terminal (Terminal tab)

The **Terminal** tab gives you a shell inside the session's working directory, rendered with xterm.js.

1. Switch to the **Terminal** tab.
2. Type any shell command (e.g. `ls -la` or `git log --oneline`).
3. Click the **→ Prompt** button to attach the last few lines of terminal output to the chat composer. This lets you paste a test failure, a build error, or command output directly into your next agent prompt without copy-pasting.

> On Unix systems with `script` available, the terminal runs inside a real PTY — interactive programs, colors, and line editing work. On Windows it falls back to a pipe (interactive TUIs won't render, but running commands and reading output works).

---

## 7. Image and file attachments

You can attach files to any message:

1. Click the **paperclip** icon in the compose bar.
2. Select up to 4 files (max 2 MB each).
   - Images are sent as native ACP `image` content blocks — the agent sees the actual image.
   - Text files are embedded as `resource` blocks — the agent reads the content.
   - Binary non-image files are refused client-side.

The attach button shows a warning chip if the connected agent does not advertise attachment support in its ACP `initialize` response.

---

## 8. Agent handoff

If you want to continue a conversation with a *different* agent (e.g. switch from Copilot to Claude Code mid-session):

1. Click the **Handoff** button in the session header.
2. AgentHelm opens a new session picker.
3. Pick the target agent and working directory.
4. The new session's compose bar is pre-filled with a compact, attributed summary of the conversation so far.
5. **Review the summary**, then press **Send** when you are ready. The summary is never auto-sent.

The source session gets an audit entry recording that a handoff was initiated.

---

## 9. Session history and resume

With Postgres running (Option A / Aspire), every session is persisted automatically with a 1-second write-behind.

- Click **History** in the left rail to see all past sessions.
- Click any session to open a read-only transcript viewer.
- If the agent advertises the `loadSession` capability, a **Resume** button appears.
- Clicking Resume opens a new session in the same working directory and replays the conversation history via ACP `session/load`. The echo agent demonstrates this capability.

Without Postgres the Bridge runs in memory-only mode — sessions are lost when the Bridge restarts, but all other features work identically.

---

## 10. Security notes before you go further

AgentHelm is designed to run on your local machine. A few things to be aware of:

- The Bridge binds to `127.0.0.1` only by default. If you need remote access, set `AgentHelm:Urls` explicitly — it's never widened automatically.
- Enable the optional shared token (`AgentHelm:ApiToken` / `x-helm-token` header) even on loopback: any web page open in your browser can make requests to localhost without it.
- The **Terminal tab** executes shell commands as you, on your machine — it is exactly as powerful as your own terminal. Treat it accordingly.
- YOLO policy requires explicit per-session opt-in and is never a global default. Every auto-decision is still audited.

---

## What's next?

- Add your own ACP-compatible agent to `appsettings.json` — any command accepting `--acp --stdio` works.
- Browse the [roadmap in the README](../README.md#roadmap) to see what's coming beyond M3.
- Run the test suite: `dotnet test` — 41 tests, all green.
- Open an issue or PR if you find a bug or have a feature idea.
