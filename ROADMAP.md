# AgentHelm roadmap — post-M3 hardening

Milestones M0–M3 are complete: the feature work described in the
[README roadmap table](README.md#roadmap) shipped. This document covers what
comes after, and it exists because GitHub milestones carry *what* and *when*
but not *why* or *what must not break*.

**Tracker issue:** [#17](https://github.com/konradcinkusz/AgentHelm/issues/17) —
hosts the running log and the decision log.

---

## 1. What "complete" means for this repository

AgentHelm is a web cockpit for AI coding agents: one GUI driving any
ACP-speaking agent, with explicit tool permissions and an audit trail. It is
not a library and not a content collection, so a mature version is judged on
whether a stranger can run it and whether its riskiest surface is honest about
itself:

1. **Every advertised path actually works for someone following the README.**
   The container quick-start, the release zip, the landing page and the badges
   all describe things a reader will attempt. A path that silently stopped
   working is worse than one that was never offered.
2. **The delivery pipeline verifies what ships, not only what is proposed.**
   The default branch is the artifact; PR-only verification leaves it unguarded.
3. **The security boundary is documented and reportable.** This project runs
   arbitrary tools on the user's machine and says so plainly. Its own analysis
   rates that as its principal risk. A stated disclosure channel is part of the
   product, not paperwork.
4. **Claims in the docs are checkable, or they are not made.** A hand-maintained
   tally in prose decays silently; nothing in CI can see it.
5. **Deferred work names its blocker.** "Beyond" items stay explicitly out of
   scope with the reason attached, rather than drifting into implied debt.

Structural elements that do **not** belong on this list, deliberately: broader
test coverage (the suite is substantial and the seams that matter are covered),
a plugin system, and UI theming. None is a gap; each would be a new feature.

## 2. Phases

| Phase | Dates | Issues | Goal |
|---|---|---|---|
| **1 — Pipeline integrity** | 2026-09-01 → 2026-09-14 | [#11](https://github.com/konradcinkusz/AgentHelm/issues/11), [#12](https://github.com/konradcinkusz/AgentHelm/issues/12), [#13](https://github.com/konradcinkusz/AgentHelm/issues/13) | Make the build and deploy triggers fire on the branch that exists, and turn a hung job into a readable failure. |
| **2 — Security and supply chain** | 2026-09-15 → 2026-09-28 | [#14](https://github.com/konradcinkusz/AgentHelm/issues/14), [#15](https://github.com/konradcinkusz/AgentHelm/issues/15) | Give the project's riskiest surface a disclosure policy, and put its dependencies under observation. |
| **3 — Documentation truth** | 2026-09-29 → 2026-10-12 | [#16](https://github.com/konradcinkusz/AgentHelm/issues/16) | Remove the one claim in the docs that has already decayed, without replacing it with a claim that will. |

Phases are two weeks because the commit history does not support inferring a
cadence: 29 commits across four days in July, then six weeks dormant. A burst is
not a cadence, so the solo-maintainer default applies.

> **Note on milestones.** These phases are not GitHub milestones. They could not
> be created from the execution environment — see §6 and the decision log on
> #17. The phase table and the tracker's checklists carry the same information;
> promoting them to real milestones is a few clicks and loses nothing.

## 3. Sequencing rationale

**Phase 1 first, because the pipeline is currently lying.** Both `ci.yml` and
`pages.yml` declare `on: push: branches: [main]`, and the default branch is
`master`. Neither trigger has ever fired: all 11 recorded `ci` runs are
`event: pull_request`, and the `GitHub Pages` workflow has exactly one run in
its history, a manual `workflow_dispatch`. Nothing is red. A workflow that never
fires is indistinguishable from a workflow with nothing to do, which is why this
survived ten merged PRs. Everything downstream is verified by that same
pipeline, so fixing it first is what makes the later phases' green checks
worth anything.

**Phase 2 second, because it is additive and low-risk.** `SECURITY.md` and
`.github/dependabot.yml` are new files that cannot break an existing workflow.
Sequencing them after the pipeline work means their CI runs are actually
meaningful; sequencing them before would have been safe but uninformative.

**Phase 3 last, because it is the smallest real defect.** The test-count drift
misleads nobody materially. It is on the roadmap because the *class* of defect
matters — a claim nothing can check — and it is last because that class is
cheap.

## 4. Dependencies

Every declared `Blocked by` in one place:

- **#13** is blocked by **#11** and **#12**.

That is the only edge. #13 edits `ci.yml` and `pages.yml`, which #11 and #12
also edit; taking them in order avoids a conflict between two PRs from the same
author. The ordering is a convenience rather than a correctness requirement — if
either blocker is skipped, #13 can still proceed, and the PR should say so.

All other issues are independent and parallel-safe.

## 5. Protected paths

Files whose breakage would compromise every later PR. A change touching one of
these is **never** force-merged past a red CI run; the PR is left open and
labelled instead.

| Path | Why |
|---|---|
| `.github/workflows/**` | The verification mechanism itself. A broken workflow turns every later PR into three-retries-and-force-merge, which is CI ceasing to exist. |
| `AgentHelm.sln`, `**/*.csproj` | Break the build graph and nothing compiles, so no later change can be verified at all. |
| `src/AgentHelm.Bridge/Sessions/PermissionPolicy.cs` | The no-auto-reject invariant and the policy taxonomy. Policies may only auto-*allow*; rejection stays a human decision. |
| `src/AgentHelm.Bridge/Agents/Acp/AcpClient.cs` | The working-directory path guard — the boundary that keeps an agent inside the session's directory. |
| `src/AgentHelm.Bridge/Workbench/GitService.cs` | Enforces the same path guard for the git endpoints, and re-derives tracked-vs-untracked server-side rather than trusting the request. |

The three security files are listed even though no issue on this roadmap touches
them: the list describes the repository, not this batch of work.

## 6. Execution policy

- **One issue, one pull request.** Never batched.
- **Never merge without CI having actually run** on the pushed branch. This is
  not optional here: no .NET SDK exists in the execution environment, so CI is
  the *only* verification a change gets.
- **Retry cap: three fix attempts per PR**, fixed.
- **After three failures**, branch on the diff:
  - Touches no protected path → force-merge, say so explicitly, and open a
    `Fix CI: <title>` issue in the same phase labelled `tech-debt`.
  - Touches a protected path → do **not** merge. Leave the PR open, label it
    and the issue `blocked`, comment with the diagnosis and the three attempts.
- **Infrastructure failures** (auth, quota, outage, runner unavailable — no code
  path in the log) do not consume the retry cap. Re-run once, then proceed in
  degraded mode; the protected-path rule does not apply, because an infra
  failure says nothing about the code.
- **Merge convention: squash**, matching the repository's history.
- **Branch:** work is pushed to `claude/repo-roadmap-plan-execute-w6zob5`, which
  is reset from `origin/master` after each merge. The loop is strictly
  sequential, so one branch name carries one PR at a time.

## 7. Non-goals

Explicitly out of scope for this roadmap, each with its blocker named. These are
not abandoned — they are the README's "Beyond" row, and they remain the right
next work once the blocker lifts.

| Item | Blocker |
|---|---|
| **Copilot SDK adapter finish** (the four `TODO`s in `Agents/CopilotSdk/CopilotSdkAdapter.cs`) | Needs the GitHub Copilot SDK preview NuGet package *and* a .NET SDK to compile against it. Neither exists in the execution environment. Writing it blind against a preview API, unable to compile, would produce speculative code whose only verification is a CI run it would likely fail. |
| **Exact CopilotScope correlation via telemetry tagging** | Cross-repository: requires a matching change in [CopilotScope](https://github.com/konradcinkusz/copilotscope), which is outside this repository's scope. The current time-window correlation is best-effort and the UI already says so. |
| **ConPTY terminal for Windows** | Requires a Windows machine to develop and verify. Unix already has a real PTY via `script(1)`; Windows stays on the pipe until this can be tested where it runs. |
| **Renaming `master` → `main`** | Would resolve #11 and #12 by a different route, but breaks existing clones, the `raw.githubusercontent.com/.../master/…` URL the README tells users to `curl`, and external blob links. A deliberate maintainer decision, not a side effect of a CI fix. |
| **A `dotnet format` CI gate** | Would require a formatting pass over the existing codebase in the same change, mixing a large mechanical diff into a policy change. Worth doing as its own piece of work. |
| **Broader test coverage** | Not a gap. The suite covers the seams that carry risk — protocol framing, the policy engine's invariants, the path guard, the adapter factory. Adding tests for their own sake is not roadmap work. |
