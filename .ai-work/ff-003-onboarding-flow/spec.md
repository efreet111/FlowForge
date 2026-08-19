---
capability_matrix:
  ai_reasoning:
    - Briefing section ordering and visual hierarchy (Spectre rendering / markdown layout)
    - Keyword set derivation for convention search (e.g. "convention", "naming", "style", "workflow")
    - Ranking of surfaced "blockers/gotchas" items by relevance (no ranking logic in installer — delegated to engram search rank)
    - "Where to start" next-steps section content (derived from surfaced decisions/patterns)
  deterministic:
    - Architectural decisions are `type=decision`; conventions AND reusable patterns are `type=pattern` (never `type=convention` — that type does not exist)
    - Recent activity = `mem_context` (last 5 sessions / 20 observations). `mem_timeline` is used ONLY as drill-down around an `observation_id`, never for recent activity
    - Project namespacing: team scope resolves `team/{project}`; personal scope resolves `{user}/{project}`. Bare `--project <name>` must resolve the scope prefix before querying
    - Exit codes: 0 = success, 1 = runtime error, 2 = pre-check failure (consistent with DoctorCommand)
    - `--limit` clamped to 1..20 (matches mem_search clamp); default 10
    - `--user` flag is display-only (briefing header); the `X-Engram-User` identity header always comes from config (`sync.user` → `ENGRAM_USER`), never from the flag
    - Pre-check order: engram binary present → config readable → (optional) server reachable; fail fast with actionable hint
    - Search results are previews truncated to 300 chars; full content is opt-in via `mem_get_observation(id)`
    - ONBOARDING.md written atomically (temp file + rename); never a partial write
    - `--output` defaults to `scope=team` (never writes personal-scope memories to disk)
---

# Spec: Onboarding Flow (`flowforge onboard`)

- Feature slug: `ff-003-onboarding-flow`
- Origin: `docs/backlog/FF-003-onboarding-flow/spec.md` (FF-IDEA-003)
- HU source: none — FlowForge backlog uses FF-* (no HU-* under docs/tasks)
- Status: in-progress (forge-arch, CKP-1)

## 0. Executive Summary

`flowforge onboard` adds a first-day briefing command to the FlowForge installer. A new
developer (or a team lead preparing one) runs a single command that (1) detects the current
project, (2) pre-checks the engram memory stack, (3) pulls the project's memories from
engram-dotnet via its existing primitives (`mem_context`, `mem_search`, `mem_stats`,
`mem_get_observation`), and (4) renders a 5-minute interactive briefing (optionally written to
`ONBOARDING.md`).

This is **not greenfield**: the installer already has ≥6 reusable patterns (DoctorCommand
pre-checks, ConfigStore, EngramProcessChecker/`Process`, source-gen JSON contexts,
`FormatContextAsync`/`mem_context` briefing format, ProjectDetector/`mem_current_project`).
The command orchestrates existing engram primitives — it does **not** reimplement memory
aggregation or ranking.

Three design errors in the preliminary spec are corrected here (verified against engram-dotnet
source): (1) `mem_timeline` is drill-down-only — recent activity is `mem_context`;
(2) `type:convention` does not exist — conventions are stored as `type=pattern`;
(3) `project:{name}` query syntax is not how `mem_search` filters — the `--project`/`--type`
parameters are used instead.

A boundary decision with the twin feature ENG-485/HU-055 (`engram onboard` in engram-dotnet)
is made in §1.3: FF-003 owns CLI orchestration + project detection + briefing rendering; engram
owns memory aggregation/ranking. No runtime dependency on ENG-485 in v1.

## 1. Objective and scope

### 1.1 Problem statement

New team members joining a FlowForge-using team have a disjointed onboarding experience: they
install FlowForge, set up IDEs, learn the methodology, **and** must absorb project knowledge
(architectural decisions, conventions, patterns, recent activity) from scattered sources. No
single command surfaces the team's accumulated memory in their first session.

### 1.2 Goals / Non-Goals

**Goals**
- G1: A single `flowforge onboard` command produces a briefing from engram memories (recent activity, top architectural decisions, team conventions, reusable patterns).
- G2: Correctly detect the current project (including `team/{project}` namespacing and ambiguity).
- G3: Fail with actionable diagnostics when engram is missing or the project has no memories (no silent empty output).
- G4: Optional `ONBOARDING.md` export that is safe to commit (team scope only).

**Non-Goals (out of scope for v1)**
- NG1: No Orchestrator skill `/flow-onboard` (future iteration — see OQ-4).
- NG2: No memory ranking/aggregation logic in the installer — always delegated to engram primitives.
- NG3: No web UI, no per-role personalization, no personal-scope memory leak into exported files.
- NG4: No write of new memories — `onboard` is read-only against engram.
- NG5: No implementation of `engram onboard` itself (that is ENG-485/HU-055, owned by engram-dotnet).

### 1.3 Architecture decisions

**AD-1 — FF-003 ↔ ENG-485 boundary (the twin feature).**
FF-003 owns: command registration, project detection, pre-flight checks, briefing rendering
(CLI interactive + ONBOARDING.md), and orchestration of existing engram primitives
(`mem_current_project`, `mem_context`, `mem_search`, `mem_stats`, `mem_get_observation`,
`mem_timeline` for drill-down only). FF-003 does **not** own memory aggregation/ranking, and does
**not** implement `engram onboard`. When ENG-485/HU-055 ships, `flowforge onboard` may delegate to
`engram onboard` as an optimization (follow-up), but v1 has no runtime dependency on it. This
prevents duplicated ranking logic across the two repos.

**AD-2 — Integration path: HTTP-first, CLI-subprocess fallback.**
Preferred source of memories is the engram sync server HTTP API (`/search`, `/context`, `/stats`),
which returns structured JSON (verified live: postgres v1.1.0). If the server is unreachable or
`sync.mode` is `local`, fall back to invoking the `engram` CLI as a subprocess (`engram
search/context/stats`) against local SQLite. `search/context/stats` have **no** `--json` flag;
their stable text format is parsed only in the fallback path. MCP stdio is explicitly not used
(the installer is a CLI, not an MCP host).

**AD-3 — `flowforge onboard` is a CLI command, not an Orchestrator flow (v1).**
The preliminary spec's recommended path (CLI first, `/flow-onboard` later) is adopted. The
Orchestrator skill is a follow-up that would call the same briefing renderer.

**AD-4 — User identity source of truth.**
The identity used for engram isolation (`X-Engram-User` header / `ENGRAM_USER`) is read from
`~/.engram/config.json` `sync.user`, falling back to `ENGRAM_USER`, then `Environment.UserName`
(matching `EngramModule`). The `--user` flag is **display-only** (briefing header) and is never
used to filter or authenticate memory access.

### 1.4 Reusable assets (verified)

| Asset | Reused for |
|-------|------------|
| `DoctorCommand` check-list pattern (`(Name, Func<Task<(bool, string?)>>)` + Spectre table + exit 0/1/2) | Onboarding pre-checks (FR-002) |
| `ConfigStore` (atomic read-modify-write of `~/.engram/config.json`) | Reading `sync.{mode, remote_url, user}` (FR-013, FR-014) |
| `EngramProcessChecker` (`System.Diagnostics.Process`) | CLI-subprocess fallback (FR-013) |
| `InstallerJsonContext` / `McpJsonContext` (source-gen JSON) | Deserializing config + HTTP JSON responses under AOT (NFR-002) |
| `Program.cs` CAF routing + `InstallerContext` DI | Registering `onboard` (FR-001) |
| engram `ProjectDetector` / `mem_current_project` | Project detection without reimplementing (FR-003) |
| engram `FormatContextAsync` / `mem_context` | Recent-activity briefing aggregate (FR-004) |

## 2. Functional requirements (FR)

### FR-001 — Register `onboard` command
`flowforge onboard` is registered in the CAF router (`app.Add<OnboardCommand>("onboard")`) and
added to the `knownCommands` validation array in `Program.cs`, so it is reachable from
`flowforge --help` and does not trigger the "Unknown command" guard.

- Scenario A: Given a user runs `flowforge onboard --help`, When CAF dispatches, Then the command usage text is shown with all flags (`--project`, `--user`, `--scope`, `--output`, `--limit`, `--no-interactive`).
- Scenario B: Given a user runs `flowforge onboard` with no flags in a detected FlowForge project, When the command starts, Then it does not hit the "Unknown command" guard and proceeds to pre-checks.

### FR-002 — Pre-flight checks (fail fast with hints)
Before any memory query, the command runs pre-checks in the DoctorCommand pattern and returns
exit code 2 with actionable hints on failure.

- Scenario A: Given the engram binary is missing (`PathHelper.EngramBinary` does not exist), When `flowforge onboard` runs, Then it prints "engram binary — ✗ FAIL" with hint "Instalá con `flowforge install`" and exits 2.
- Scenario B: Given `~/.engram/config.json` is missing or unparseable, When `flowforge onboard` runs, Then it prints "engram config — ✗ FAIL" with hint to run `flowforge install` / `flowforge config`, and exits 2.

### FR-003 — Project detection with ambiguity handling
The command resolves the target project via `mem_current_project` (canonical ProjectDetector
algorithm), falls back to `.flowforge.json` → `engram.project`, and honors a `--project` override.
If the detector returns `available_projects` with a warning (ambiguous), the command prompts for
selection interactively (or errors in `--no-interactive` mode).

- Scenario A: Given the CWD is the FlowForge repo (git remote `FlowForge`) and no `--project` flag, When `flowforge onboard` resolves the project, Then it uses `flowforge` and maps it to `team/flowforge` when scope is team.
- Scenario B: Given the CWD contains two child git repos and `--project` is omitted, When detection returns `available_projects=[a,b]`, Then an interactive `SelectionPrompt` lists `a` and `b`; in `--no-interactive` mode it exits 1 with "ambiguous project: pass --project".

### FR-004 — Recent activity via `mem_context` (not `mem_timeline`)
Recent project activity is retrieved with `mem_context(project, scope)` (last 5 sessions, up to 20
observations, 10 prompts). `mem_timeline` is never used for this purpose.

- Scenario A: Given project `team/flowforge` has 14 observations, When `flowforge onboard --scope team` runs, Then the "Recent Activity" section lists the last sessions/observations from `mem_context`, each with project, date and type.
- Scenario B: Given the project has 0 sessions, When `mem_context` returns "No previous session memories found", Then the command enters the empty-memory path (FR-012) instead of rendering an empty section.

### FR-005 — Top architectural decisions (`type=decision`)
The briefing includes the top N architectural decisions via `mem_search(type=decision, project, limit=N)`.

- Scenario A: Given `team/flowforge` contains `decision` observations (e.g. #97, #112), When onboarding queries `type=decision`, Then the "Key Architectural Decisions" section lists them by title/date, truncated to 300-char previews.
- Scenario B: Given the project has no `decision` observations, When the search returns "No memories found", Then the section is omitted (or shows "— no decisions captured yet"), never a fabricated list.

### FR-006 — Conventions and patterns (`type=pattern`)
Team conventions and reusable patterns are retrieved with `mem_search(type=pattern, ...)` plus a
convention keyword query (`convention`, `naming`, `style`, `workflow`). The type `type=convention`
is never used (it does not exist).

- Scenario A: Given `team/flowforge` stores conventions as `pattern` observations (e.g. #32 "EN linked"), When onboarding queries `type=pattern` + convention keywords, Then the "Conventions" section lists those patterns.
- Scenario B: Given a search with `type=convention` is (incorrectly) attempted, Then the implementation never emits that type — all convention/pattern queries use `type=pattern` only (unit-tested guard).

### FR-007 — Known blockers / gotchas (keyword search)
The briefing surfaces known blockers/gotchas by keyword search over `type=bugfix` and `type=manual`
(keywords: `blocker`, `gotcha`, `issue`, `bug`, `workaround`). No canonical `type=blocker`/`type=gotcha`
is assumed.

- Scenario A: Given a `bugfix` observation mentions a known installer regression, When onboarding searches blocker keywords, Then the "Known Blockers / Gotchas" section lists it with date and preview.
- Scenario B: Given no blockers are found, When the keyword search returns empty, Then the section is omitted without error.

### FR-008 — Project stats header
The briefing header includes project statistics from `mem_stats` (sessions, observations, prompts,
projects).

- Scenario A: Given `engram stats` returns 45 sessions / 65 observations, When onboarding renders the header, Then the counts are shown as "Memory stats: 45 sessions, 65 observations".
- Scenario B: Given `mem_stats` fails (e.g. DB locked), When onboarding runs, Then it degrades to the briefing without stats and logs a warning, instead of crashing.

### FR-009 — Interactive CLI briefing
The command renders a structured briefing (Recent Activity, Key Decisions, Conventions, Patterns,
Blockers) with Spectre.Console, and supports an interactive drill-down prompt.

- Scenario A: Given a successful retrieval, When the briefing renders, Then all sections appear with timestamps and truncated previews, and a prompt "type a number to drill down, Enter to exit" is shown.
- Scenario B: Given `--no-interactive` is passed, When onboarding runs, Then it prints the full briefing (previews only) and exits without waiting for input (CI-safe).

### FR-010 — Drill-down into an observation
Selecting an item shows its full content via `mem_get_observation(id)`; optionally `mem_timeline(id)`
shows temporal context. This is the only correct use of `mem_timeline`.

- Scenario A: Given the briefing lists `[1] #97 decision`, When the user types `1`, Then `mem_get_observation(97)` renders the full content + session/scope/project metadata.
- Scenario B: Given the user types `1t` (timeline), When `mem_timeline(97, before=5, after=5)` is invoked, Then the chronological context (Before / focus / After) is shown around observation #97.

### FR-011 — ONBOARDING.md export (`--output`)
`--output <path>` writes a markdown briefing (Recent Activity, Key Decisions, Conventions, Patterns,
Next Steps) with a generated timestamp, team scope only, atomically.

- Scenario A: Given `flowforge onboard --output ONBOARDING.md --project flowforge`, When the command completes, Then `ONBOARDING.md` exists with all sections, a `> Generated: <UTC timestamp>` header, and no personal-scope memories.
- Scenario B: Given the target directory is not writable, When `--output` runs, Then it exits 1 with a clear error and no partial file remains (atomic temp-file + rename).

### FR-012 — Empty-memory / no-engram handling
If the pre-checks pass but the project has no memories (or engram is not configured for the team),
the command prints a clear diagnostic and a first-steps guide instead of an empty briefing.

- Scenario A: Given `mem_context` returns 0 sessions and `mem_search` returns 0 results, When onboarding runs, Then it prints "No memories found for <project>" plus guidance ("run `flowforge install`, capture decisions with the methodology").
- Scenario B: Given engram is installed but the team does not use engram sync, When `sync.mode` is absent/`local` and no local data exists, Then the guidance includes how to enable team sync.

### FR-013 — Integration path selection (HTTP → CLI fallback)
The command reads `sync.mode`/`sync.remote_url` from config and selects the HTTP API when
`sync.mode=sync` and `/health` responds within the timeout; otherwise it falls back to the `engram`
CLI subprocess against local SQLite.

- Scenario A: Given `sync.mode=sync` and the server responds `{"status":"ok"}`, When onboarding fetches memories, Then it uses `GET /search`, `GET /context`, `GET /stats` with header `X-Engram-User` and parses structured JSON.
- Scenario B: Given the server is unreachable (timeout), When onboarding runs, Then it falls back to `engram search/context/stats` subprocess, prints "usando memorias locales", and still produces a briefing.

### FR-014 — User identity resolution
The engram identity is resolved as `sync.user` → `ENGRAM_USER` → `Environment.UserName`. The
`--user` flag only changes the displayed name in the briefing header.

- Scenario A: Given `sync.user=victor@local.dev`, When onboarding calls the HTTP API, Then the `X-Engram-User` header is `victor@local.dev` regardless of any `--user` flag.
- Scenario B: Given `--user nuevo@team.dev` is passed, When onboarding renders, Then the briefing header shows `nuevo@team.dev` but no memory query uses that value as an identity filter.

### FR-015 — Project namespacing (`team/{project}`)
When scope is team, the resolved project name is namespaced to `team/{project}` before querying
(and `{user}/{project}` is included when reading both scopes, matching engram's wide-read behavior).

- Scenario A: Given project `flowforge` and scope `team`, When onboarding queries memories, Then it resolves `team/flowforge` (never the bare `flowforge`, which would miss team-scoped data).
- Scenario B: Given scope is omitted and a user is configured, When `mem_context`/`mem_search` run, Then both `team/{project}` and `{user}/{project}` are searched (cross-namespace), matching engram semantics.

## 3. Non-functional requirements (NFR)

- **NFR-001 — Performance.** Briefing renders in < 5 s via HTTP API and < 2 s via local CLI subprocess (excluding server latency beyond timeout). Timeouts are bounded and configurable (`FLOWFORGE_API_TIMEOUT_SECONDS`, default 30 s).
- **NFR-002 — AOT compatibility.** The installer remains `PublishAot=true`, `TrimMode=full`, `InvariantGlobalization=true`. All JSON (config read + HTTP responses) is deserialized via source-generated `JsonSerializerContext` (new contexts as needed); no reflection; only BCL + already-used packages (ConsoleAppFramework, Spectre.Console, DI); `System.Diagnostics.Process` is used, not any non-AOT-safe interop.
- **NFR-003 — ADR-017 installer protection.** The change composes over existing commands (no replacement of `install`/`status`/`doctor`/`uninstall`); an updated `installer-baseline.md` (commands, flags, side effects) is produced; regression tests for `install --yes`/`status`/`doctor`/`uninstall` run green before and after.
- **NFR-004 — Security (untrusted memory content).** Memory text is untrusted data: it is escaped before Spectre markup rendering (no `[markup]`/ANSI injection), truncated to 300-char previews by default, and full content is only shown on explicit drill-down.
- **NFR-005 — Privacy (scope discipline).** The default scope for `--output` is `team`; personal-scope memories are never written to `ONBOARDING.md`. Interactive mode warns when `--scope personal` is combined with `--output`.
- **NFR-006 — Resilience.** No partial `ONBOARDING.md` writes (atomic temp+rename); HTTP and subprocess calls have timeouts; server-down and DB-locked cases degrade gracefully (FR-008, FR-013) instead of crashing.
- **NFR-007 — Deterministic exit codes.** `0` success, `1` runtime/usage error, `2` pre-check failure — consistent with `DoctorCommand` (scriptable in CI).

### 3.1 STRIDE threat analysis

| # | Threat | Category | Scenario | Mitigation | Trace |
|---|--------|----------|----------|------------|-------|
| T1 | Identity spoofing | Spoofing | A user runs `flowforge onboard --user other@team.dev` to read another user's team memories | `--user` is display-only; `X-Engram-User`/`ENGRAM_USER` always from local config (`sync.user`), never from the flag | FR-014 |
| T2 | MITM on sync server | Tampering | `remote_url` is plain HTTP; responses intercepted and modified | Recommend HTTPS `remote_url` in production; treat all memory content as untrusted (NFR-004); no credentials written to disk | NFR-004 |
| T3 | Terminal injection | Tampering | A memory contains `[red]`/ANSI escapes that Spectre interprets as markup | Escape memory text (`Markup.Escape`) before rendering; never pass raw memory to `MarkupLine` | NFR-004 |
| T4 | Malicious markdown in export | Tampering | A memory contains malicious links/commands that land in `ONBOARDING.md` | Content is truncated data, never executed; no auto-run hooks; generated-timestamp header marks provenance | FR-011 |
| T5 | No audit trail | Repudiation | Onboarding generation cannot be attributed | Log generation (timestamp, project, user) to installer log; optional future engram `command` observation | NFR-006 |
| T6 | Leak of personal/team memory into VCS | Information Disclosure | `--output` writes team decisions or (worse) personal memories into a committed file | Default `scope=team` for export; personal scope excluded; warn before writing into a git repo | FR-011, NFR-005 |
| T7 | Full-content secrets via drill-down | Information Disclosure | `mem_get_observation` reveals a memory with secrets (past sessions contained sensitive-data cleanup) | Drill-down is opt-in; default previews truncated to 300 chars; no bulk full-content dump | FR-010 |
| T8 | Server-down / slow query | Denial of Service | Sync server unreachable or a query hangs | Bounded timeouts; fallback to local SQLite CLI; `--limit` clamp bounds output size | FR-013, NFR-001 |
| T9 | Malicious binary path | Elevation of Privilege | Subprocess invokes an attacker-controlled `engram` binary | Binary path from `PathHelper.EngramBinary` (installer-managed), never from user input or env override | FR-013 |

### 3.2 Capability Matrix — FR/NFR traceability

| FR | Deterministic rule / AI-reasoning | NFR(s) |
|----|----------------------------------|--------|
| FR-001 | Command registration + knownCommands | NFR-003 |
| FR-002 | Pre-check order + exit 2 | NFR-007 |
| FR-003 | Project resolution + namespacing (deterministic) | — |
| FR-004 | `mem_context` for recent activity (deterministic) | NFR-001 |
| FR-005 | `type=decision` (deterministic) | — |
| FR-006 | `type=pattern` + keywords (deterministic type; AI-reasoning keyword set) | — |
| FR-007 | keyword search over bugfix/manual; relevance ranking delegated to engram (AI-reasoning) | — |
| FR-008 | stats header (deterministic) | NFR-006 |
| FR-009 | section ordering/hierarchy (AI-reasoning) | NFR-004 |
| FR-010 | drill-down full content (deterministic, opt-in) | NFR-004 |
| FR-011 | atomic write, team scope (deterministic) | NFR-005, NFR-006 |
| FR-012 | empty-memory diagnostic (deterministic) | — |
| FR-013 | HTTP-first → CLI fallback (deterministic) | NFR-001, NFR-006 |
| FR-014 | identity from config, flag display-only (deterministic) | NFR-004 |
| FR-015 | `team/{project}` namespacing (deterministic) | — |

## 4. Developer manual tests (required — mark [x] before /flow-close)

| ID | Case / flow | Steps (summary) | Expected result | [x] |
|----|-------------|-----------------|-----------------|-----|
| PM-1 | Happy path (team sync) | 1. `flowforge onboard --project flowforge` in the FlowForge repo with engram sync configured<br>2. Read the interactive briefing | Briefing shows Recent Activity, Key Decisions (type=decision), Conventions/Patterns (type=pattern) from `team/flowforge` (14 obs), with timestamps; exit 0 | [ ] |
| PM-2 | Error path (no engram) | 1. Temporarily rename `~/.local/bin/engram`<br>2. Run `flowforge onboard`<br>3. Restore the binary | Pre-check "engram binary ✗ FAIL" with hint "Instalá con `flowforge install`"; exit code 2 | [ ] |
| PM-3 | Edge case (ambiguous/no memories) | 1. `cd` into a dir with ≥2 child git repos (or a fresh empty project)<br>2. Run `flowforge onboard` | Ambiguity prompt lists `available_projects` (or empty-memory guidance if no data); no crash; exit 0/1 per FR-003/FR-012 | [ ] |
| PM-4 | Export + drill-down | 1. `flowforge onboard --output ONBOARDING.md --project flowforge`<br>2. Run again without `--output` and type an item number | `ONBOARDING.md` written atomically with generated timestamp, team scope only; drill-down shows full observation content via `mem_get_observation` | [ ] |

## 5. Open questions for human (OQ-*)

| ID | Tag | Question | Default / assumption |
|----|-----|---------|---------------------|
| OQ-1 | [OPTIONAL] | Confirm the FF-003 ↔ ENG-485 boundary: FF-003 owns CLI orchestration + detection + rendering; engram owns aggregation/ranking. When ENG-485 ships, should `flowforge onboard` delegate to `engram onboard`? | Assumed: v1 has no dependency on ENG-485; delegation is a follow-up (AD-1). |
| OQ-2 | [OPTIONAL] | Should the briefing include a "Known Blockers / Gotchas" section sourced by keyword search over `bugfix`/`manual` (no canonical `type=blocker` exists)? | Assumed: yes, via keyword search; omitted if empty (FR-007). |
| OQ-3 | [OPTIONAL] | Default output mode: interactive briefing only, `ONBOARDING.md` only with `--output`? | Assumed: interactive by default; export only via `--output` (FR-009/FR-011). |
| OQ-4 | [FOLLOW-UP] | Add an Orchestrator skill `/flow-onboard` that invokes the same briefing renderer for agent-driven onboarding? | Out of v1 scope (NG-1); does not change v1 design. |

## Memory Signal

- type: decision
- significance: high
- summary: "FF-003 `flowforge onboard` owns CLI orchestration + project detection + briefing rendering and delegates memory aggregation/ranking to engram primitives (mem_context/mem_search/mem_stats/mem_get_observation); boundary with ENG-485 defined; corrected two spec errors (mem_timeline→mem_context for recent activity, type:convention→type=pattern); integration path HTTP-first with CLI-subprocess fallback."
