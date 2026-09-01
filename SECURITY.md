# Security policy

AgentHelm drives AI coding agents that execute tools on your machine. That is
the product, and it is also the whole of its risk: the Bridge is a local
process with your files, your credentials and a shell. This document says how
to report a hole in the boundaries that contain it, and — just as important —
which powerful behaviours are deliberate and therefore not holes.

## Supported versions

| Version | Supported |
|---|---|
| Latest release | ✅ |
| Everything earlier | ❌ |

This is a single-maintainer project. Fixes land on `master` and go out in the
next release; there are no backports and no long-term support branch. Stating
anything else would be a promise the project cannot keep.

## Reporting a vulnerability

**Please do not open a public issue for a security problem.**

Use GitHub's private vulnerability reporting:
[**Report a vulnerability**](https://github.com/konradcinkusz/AgentHelm/security/advisories/new)
— Security → Advisories → Report a vulnerability. It is private between you
and the maintainer, and it gives us somewhere to co-ordinate a fix and a
credit before anything is public.

If that link does not work, private reporting has not been switched on yet:
say hello through the contact details on
[the maintainer's GitHub profile](https://github.com/konradcinkusz) and ask
for a private channel, rather than filing the details publicly.

Helpful in a report, roughly in order of usefulness:

- what boundary you crossed, and which of the ones below it was;
- the smallest reproduction you have — a path, a request, a config;
- the version or commit, and how AgentHelm was started (Aspire, `dotnet run`,
  the release zip, containers);
- what an attacker gets out of it.

You will get an acknowledgement as quickly as one person reasonably can, and
an honest answer about a timeline rather than an invented one. If a report
turns out to describe designed behaviour, you will get the reasoning, not
silence.

## What is in scope

These are the boundaries AgentHelm claims to hold. A way through any of them
is a vulnerability:

- **The working-directory path guard.** Agents may touch files only under the
  session's working directory. ACP `fs/read_text_file` and `fs/write_text_file`
  are checked in the Bridge, and the git endpoints enforce the same guard. Any
  traversal, symlink, or encoding trick that reads or writes outside it counts.
- **The permission gateway.** Every tool call passes a policy decision that is
  recorded. A tool call that reaches execution without one — or an audit entry
  that can be suppressed or forged — counts. Policies may only auto-*allow*;
  anything that makes an automatic *rejection* look like a human one, or the
  reverse, counts.
- **The loopback bind.** The Bridge listens on `127.0.0.1` by default. Any web
  page open in the user's browser can send requests to localhost, so anything
  a hostile page can achieve against a default install — starting a session,
  driving the terminal, reading a transcript — counts, including CSRF-shaped
  attacks against the `x-helm-token` gate.
- **Secret handling.** Tokens, API keys or credentials leaking into
  transcripts, logs, the Postgres snapshot, or the UI counts.
- **Escalation beyond the user.** Anything that gets more privilege than the
  person running AgentHelm already has.

## What is not a vulnerability

These are documented, deliberate, and cost real capability to remove. Reports
about them will be answered with this section:

- **The Terminal tab runs shell commands as you.** It is exactly as powerful
  as your own terminal, because it *is* your terminal. That is the feature.
- **YOLO mode auto-allows tool calls.** It is never a default and never
  silent: it takes an explicit per-session confirmation, and every automatic
  decision is still audited.
- **Agents read and write inside the session working directory.** That is the
  job. Escaping it is in scope; doing it is not.
- **Exposing the Bridge beyond loopback.** `AgentHelm:Urls` is a deliberate
  configuration change. If you publish it to a network without a reverse proxy
  and authentication in front, that is a deployment decision, not a defect —
  and enabling `AgentHelm:ApiToken` is worth it even on loopback.
- **Vulnerabilities in the agent CLIs themselves** (Copilot CLI, Claude Code,
  Gemini CLI, or any other ACP agent). Please report those to their own
  projects. If AgentHelm *amplifies* one — turns something local into
  something remote, say — that part is ours and we want to hear about it.

## Hardening a deployment

Briefly, since the questions recur: keep the loopback bind; set
`AgentHelm:ApiToken` even so; leave the permission policy at `ask` unless you
have a reason; treat the Terminal tab as a terminal. The
[Security model](README.md#security-model) section of the README covers the
design behind each of these.
