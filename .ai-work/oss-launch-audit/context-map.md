# Context Map — OSS Launch Audit (FlowForge + engram-dotnet)

**Feature slug**: `oss-launch-audit`  
**Date**: 2026-06-23  
**Objective**: Critical analysis of both projects before open-source community launch this week.  
**Analyst**: forge-discovery  

---

## FlowDoc context

- PRD: n/a (audit request, not a feature)
- HU referenced: none
- HU flowforge_slug: unset (this is a meta-request, not a product feature)

---

## Scope

Two projects analyzed:
1. **FlowForge** (`/media/gantz/300extra/Proyectos/FlowForge`) — v0.5.0 (CHANGELOG), v0.4.0 (README), stack installer v0.1.0-alpha.2
2. **engram-dotnet** (`/media/gantz/300extra/Proyectos/engram-dotnet`) — v0.3.0 (last tag), huge [Unreleased] in CHANGELOG

---

## 🔴 BLOCKERS — Must fix before any OSS launch

### B-1: engram-dotnet — ENG-435 data corruption bugs (rework_ticket.md is OPEN)

Two critical bugs confirmed in `rework_ticket.md` (cycle_count: 1, status: "rejected"):

- **PostgresStore transaction is empty** (`PostgresStore.cs:1610-1626`): three `NpgsqlCommand` objects do NOT set `cmd.Transaction = tx`. The transaction commits nothing. If sessions UPDATE fails after observations UPDATE succeeds, observations are already committed — no rollback possible. Violates REQ-435-004.
- **`--dry-run` executes the actual migration** (`Program.cs:641`): calls `MigrateProjectAsync()` which runs all UPDATEs, then prints "Would migrate". Developer acknowledged it: "dry-run still migrates but we could enhance this later." Violates REQ-435-003.

**Risk**: Users running `engram project migrate` will silently corrupt their data.

**Fix spec** (already in rework_ticket.md):
1. Add `cmdObs.Transaction = tx`, `cmdSess.Transaction = tx`, `cmdPrompt.Transaction = tx`
2. Replace dry-run `MigrateProjectAsync` call with SELECT COUNT(*) queries only

---

### B-2: engram-dotnet — TD-013: ApplyPulledMutationAsync is a stub (sync silently broken)

`SqliteStore.cs:1910-1916` — `ApplyPulledMutationAsync` returns `Task.CompletedTask` without applying mutations. `SyncManager.PullAsync` (L271) calls this; the sync cursor advances but pulled data is never persisted locally.

**Impact**: Every user with `ENGRAM_SYNC_ENABLED` and SQLite local backend silently loses pulled data. No error, no warning. This is P0 for sync users.

---

### B-3: FlowForge — Version mismatch in README (first impression broken)

- `VERSION.md` → `0.5.0`
- `CHANGELOG.md` → `[0.5.0]` with significant additions (CKP-1 BLOCKER gate, FlowDoc, Pattern Search, Stack Installer)
- `README.md` → still says **`v0.4.0`**

A new OSS visitor sees v0.4.0 and clicks the CHANGELOG link which says 0.5.0. Credibility hit on arrival.

---

### B-4: engram-dotnet — No release tag for [Unreleased] work

The [Unreleased] section in `CHANGELOG.md` contains:
- ENG-410 (Project identity fingerprint)
- ENG-411 (Polly resilience)
- ENG-208 (Structured MCP errors + Obsidian export)
- ENG-428, 429, 430, 431, 432, 433, 435 (sync fixes, doctor, CLI tools)
- Full logging infrastructure
- Hub MCP multi-editor
- Setup wizard

Last git tag is `v0.3.0`. Users cloning `main` get all of this as unversioned, unreleased code. For OSS communities, "is this stable?" is answered by releases — there are none for all this work.

---

## 🟡 IMPORTANT — Fix this week if possible

### I-1: rework_ticket.md in engram-dotnet root

The file `rework_ticket.md` sits at the repo root. First-time contributors and OSS visitors will see an open "Failure Reason" document with "CRITICAL-1" language as the first artifact after README. Move to `.ai-work/eng-435/` or create a `.gitignore` entry — either way, it should not be visible at root.

---

### I-2: FlowForge — Uncommitted roadmap change

`docs/04-roadmap.md` has an unstaged change (confirmed via `git diff`): the public launch notice ("The repo is public as of ENG-301 release") has been written but not committed. This needs to be committed to `main`.

---

### I-3: engram-dotnet — Version string chaos

`git log` shows commit `84e0712 fix: update version string from 0.1.0 to 1.2.0 across all files` but CHANGELOG declares 0.3.0 as the last release, with [Unreleased] work that logically follows. There's a mismatch between what `engram --version` prints (likely 1.2.0) and what CHANGELOG/README say. New users cannot determine what version they have.

---

### I-4: FlowForge — Three install paths with no clear "start here"

README presents:
1. Public one-liner install (IDE skills)
2. Stack installer (AOT binary)
3. Local clone

No decision guide exists. A new user doesn't know whether to use the Stack installer or the IDE install. The Stack installer is v0.1.0-alpha.2 (alpha!) while methodology is 0.5.0 — the alpha label may scare off users who should actually use it.

---

### I-5: engram-dotnet README tool count inconsistencies

- Architecture diagram says "24 MCP tools"
- Features table says "28 tools"
- A commit message says "correct count to 26"

New users checking "how many MCP tools" get three different answers in the same README.

---

### I-6: FlowForge — No demo GIF or screenshot in README

For OSS discovery, a 30-second GIF showing `/flow-start → spec.md → CKP-1 → plan.md → code` is worth more than 500 words. There is none. The `examples/crud-tareas/` artifacts exist but they require manual navigation. GitHub stars correlate strongly with visual proof of concept.

---

### I-7: engram-dotnet — God classes are contributor-hostile

- `SqliteStore.cs`: 2397 lines
- `PostgresStore.cs`: 2136 lines  
- `EngramTools.cs`: 1034 lines

TD-001, TD-002, TD-009 document this. For OSS, a 2400-line file with no decomposition is the single biggest barrier to "I want to contribute a fix." This is not a blocker for launch but will kill contribution momentum on day 2.

---

## 🟢 STRENGTHS — What's already solid for OSS

### FlowForge:
- Complete OSS files: LICENSE (MIT), CONTRIBUTING.md, SECURITY.md, CODE_OF_CONDUCT.md
- EN + ES documentation (README, QUICKSTART, GLOSSARY)
- 4-IDE support with parity doc
- CI with Layer 1 structural linting (opencode-smoke.yml)
- Real example: `examples/crud-tareas/` with 20/20 validation
- ADR pattern established (ADR-001 through ADR-004)
- Stack installer with SHA-256 verification and GitHub Releases

### engram-dotnet:
- 11 source modules + 11 test projects — well-structured test coverage
- PostgreSQL + SQLite dual backends
- Docker support with real multi-client test scripts
- 258+ tests
- CI badge in README
- `docs/TECHNICAL-DEBT.md` is honest and detailed — great for contributor trust
- Setup wizard scripts for multi-editor MCP config
- Pragmatic "no MediatR, no CQRS" philosophy is differentiated and appealing to community

---

## Reusable Patterns Found

- `rework_ticket.md` pattern in FlowForge (`.ai-work/eng-301/`) shows how to properly scope rework — can be applied to model the ENG-435 fix cycle already underway in engram-dotnet.
- `examples/crud-tareas/CASE-1-VALIDATION.md` is the golden template for "here's a worked example of our methodology" — should be referenced more prominently in README.

---

## Priority matrix for "this week" (Mon–Sun)

| Priority | Item | Project | Effort | Unblocks |
|----------|------|---------|--------|---------|
| P0 | Fix ENG-435 transaction + dry-run bugs | engram-dotnet | M (2h) | Data safety |
| P0 | Fix version in FlowForge README (0.4.0 → 0.5.0) | FlowForge | XS (5min) | First impression |
| P0 | Commit uncommitted roadmap change | FlowForge | XS (2min) | Roadmap accuracy |
| P1 | Release engram-dotnet v0.4.0 tag | engram-dotnet | S (1h) | OSS credibility |
| P1 | Fix TD-013 ApplyPulledMutationAsync stub | engram-dotnet | M (3h) | Sync users |
| P1 | Move rework_ticket.md out of root | engram-dotnet | XS (5min) | First impression |
| P1 | Fix MCP tool count in README | engram-dotnet | XS (10min) | README accuracy |
| P2 | Add decision guide for install paths | FlowForge | S (30min) | Onboarding |
| P2 | Add demo GIF to FlowForge README | FlowForge | L (4h) | OSS discovery |
| P3 | Split God classes (TD-001, TD-002) | engram-dotnet | XL (2d+) | Contribution |

---

**CLEAR**
