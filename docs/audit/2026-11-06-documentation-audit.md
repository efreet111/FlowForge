# Documentation Audit — 2026-11-06

> **Scope**: all versioned documentation under `docs/`, plus root `README.md`, `README.es.md`,
> `AGENTS.md` and `CHANGELOG.md`.
> **Method**: FlowDoc review criteria + automated inventory, link checker, ADR index/file diff,
> HU structure scan, skill count comparison.
> **Branch policy**: documentation only. No product code, no HU implementation.

---

## 1. Summary

| Metric | Result |
|--------|--------|
| Markdown files scanned | 123 under `docs/` + 5 root docs |
| Internal links checked | 477 (excluding `docs/archive/`) |
| Broken links found initially | 56 |
| Broken links remaining | 20 — all point to `.ai-work/` session artifacts (not versioned, ADR-002) |
| Duplicate ADR numbers | 4 numbers / 8 files → **fixed** (renumbered to ADR-030…033) |
| Missing base docs | `docs/README.md`, `docs/endpoints.md`, `docs/schema.md`, `docs/architecture/rfc/INDEX.md` → **created** |
| Skill count mismatch | 31 claimed vs 35 actual → **fixed** |
| Skill index gap | `forge-memory-contradiction` missing from `AGENTS.md` → **fixed** |
| Historical plans presented as current | `docs/04`, `docs/08`, `docs/17` → **banners added** |
| HU format deviations | 6 HUs use alternative formats (content present, headings differ) → **mitigated with template-compliance notes** |
| PRD status stale | `docs/PRD.md` still `Draft` → **open warning** |

---

## 2. Fixed findings

### 2.1 Duplicate ADR numbers — ERROR → fixed

Four ADR numbers had two files each. The canonical index already chose one file per number; the
onboarding/fforge-init branch files were unindexed duplicates.

| Original | New | File |
|----------|-----|------|
| ADR-018 | **ADR-030** | `docs/decisions/ADR-030-ff003-eng485-onboarding-boundary.md` |
| ADR-019 | **ADR-031** | `docs/decisions/ADR-031-http-first-cli-fallback-integration.md` |
| ADR-020 | **ADR-032** | `docs/decisions/ADR-032-user-identity-from-config.md` |
| ADR-026 | **ADR-033** | `docs/decisions/ADR-033-fforge-init-hybrid-skill-architecture.md` |

Updated: headers, internal ADR links, `docs/decisions/INDEX.md` (table + renumbered table),
`docs/project-context.md`, `CHANGELOG.md`.

**Verification**: 33 ADR files, 33 index rows, numbers 001–033 unique and sequential.

### 2.2 Missing base docs — WARNING → fixed

Created:

- `docs/README.md` — canonical documentation map (types, paths, status, validation rules).
- `docs/endpoints.md` — N/A with rationale (FlowForge has no HTTP API).
- `docs/schema.md` — N/A with rationale (Engram owns persistence; link to its contract).
- `docs/architecture/rfc/INDEX.md` — RFC index placeholder (no RFCs yet).

### 2.3 Broken relative links — ERROR → fixed (active docs)

Fixed in:

- `docs/16-ide-integration-plan.md` — `docs/decisions/...` → `decisions/...`; missing
  `referencias-stack.md` marked as session artifact.
- `docs/project-context.md` — `../decisions/...` → `decisions/...`.
- `docs/04-roadmap.md` — `VERSION.md` → `../VERSION.md`; `.ai-work` links marked as session
  artifacts; historical banner added.
- `docs/20-flowdoc-ecosystem.md` — live proposal path → `archive/...archived.md`.
- `docs/07-core-skills.md` — `forge-orchestrator` → `flowforge`.
- `README.es.md` — `../examples/crud-tareas/` → `examples/crud-tareas/`.
- `docs/decisions/ADR-010` — `../../decisions/ADR-009...` → `ADR-009...`.
- `docs/decisions/ADR-006` — `docs/tasks/...` → `../tasks/...`; `POST-INSTALL.md` →
  `../../POST-INSTALL.md`.
- `docs/tasks/HU-021` — `docs/decisions/...` → `../../decisions/...`.
- `docs/tasks/HU-041` — `../decisions/...` → `../../decisions/...`.
- `docs/tasks/HU-042` — `docs/decisions/...` → `../../decisions/...`.
- `docs/tasks/HU-050` — `../../docs/decisions/...` → `../../decisions/...`.
- `CHANGELOG.md` — links to removed/renumbered HU files replaced with plain-text references.

### 2.4 Skill count and index — ERROR → fixed

Actual `skills/**/SKILL.md` count: **35** (7 core + 27 specialized + 1 teacher).

Updated:

- `README.md`: `31 specialized skills` → `35 skills`; skills table `31` → `35`.
- `README.es.md`: `31 skills` → `35 skills`.
- `docs/14-flowforge-complete-reference.md`: header 31 → 35, version 1.1 → 1.2.
- `docs/15-agent-skills-technical-spec.md`: 31/30 → 35; specialized 31 → 27.
- `AGENTS.md`: added `forge-memory-contradiction` to the cross-cutting skill table.

### 2.5 Historical documents presented as current — WARNING → fixed

Added explicit status banners:

- `docs/04-roadmap.md` — historical snapshot; current status in `docs/backlog/BACKLOG-MASTER.md`.
- `docs/08-test-plan.md` — historical test plan; current tests in `tests/`.
- `docs/17-improvement-plan-specs.md` — historical plan; current status in the backlog.

### 2.6 Engram tool reference stale count — WARNING → fixed

- `docs/12-engram-tool-reference.md`: 25 → **34** MCP tools.
- Removed the stale “HU-036 Phase 1 — STUB” note for `mem_check_contradictions`.
- Added a table for the 10 tools missing from the historical 6-area map
  (code-aware recall, decision tree, project management, stats).
- Added the MCP/CLI code-metadata asymmetry note (HU-034 Part B).

### 2.7 `docs/14` content refresh — WARNING → fixed

- Header/version updated to **1.3 (FlowForge v0.5.0, FlowDoc v2.0)**.
- `forge-orchestrator` → **`flowforge`** canonical orchestrator name.
- `flowforge status` sample de-hardcoded (current v0.5.0 / engram v1.4.0 placeholders).
- Skills summary table: 31 → **35**, with an explicit on-demand/cross-cutting row.
- Legacy `RF`/`RNF` notation → **`FR`/`NFR`** throughout Parts 1–3.
- Added `/fforge-init` to the command map and an on-demand skills section.
- Part 4 “Planned tooling” → **“Current tooling”** with implemented/incubator status.

### 2.8 `docs/15` content refresh — WARNING → fixed

- Header/version updated to **1.3 (FlowForge v0.5.0)**.
- `forge-orchestrator` → **`flowforge`** canonical name (with historical note).
- Legacy `RF`/`RNF` IDs → **`FR`/`NFR`**, including conceptual function names.
- Skills-per-agent table: 31 → **35**, with on-demand/cross-cutting and teacher rows.
- Gap statuses refreshed: rollback, verify-without-runtime, CKP timestamps and cycle-time
  tracking marked resolved; mutation testing marked P0-shipped/P1-pending (HU-028); traceability
  marked partial.
- `forge-memory` retention row now documents the **mandatory dry-run + human confirmation** gate (HU-048).
- Conclusion/footer updated to v0.5.0 and 2026-11-06.

### 2.9 `docs/16` content refresh — WARNING → fixed

- Header/version updated to **1.2 (FlowForge v0.5.0)** and title now includes Copilot/Kilo.
- Canonical orchestrator `flowforge` updated; legacy `forge-orchestrator` retained only where it
  is an actual filename (`forge-orchestrator.md` / `.agent.md` / prompt agent id).
- Closed-gaps table expanded with VS Code prompts, Antigravity contract, OpenCode V2,
  Copilot/Kilo destinations and context7 MCP.
- Remaining-improvements table now marks natural-language intent signals as done, stack
  references as partial, and adds the compiled-Cursor-agents drift as an explicit gap.
- Canonical skill count 31 → **35**; OpenCode file tree and `agent-models.json` entries added.
- Part 4 now documents the current `flowforge install/update/doctor` path alongside the
  legacy shell installers.
- Part 5 split into “Done (v0.4–v0.5)” and refreshed short/medium-term items.

### 2.10 `docs/17` status refresh — WARNING → fixed

- Title corrected from “14 items” to **15 items**; version bumped to 1.1 with a 2026-11-06
  status refresh.
- Added a **Resultado actual** table mapping all 15 items to `Done` / `Partial` / `Open`,
  with evidence links to `examples/crud-tareas/`, `templates/project/`, QUICKSTART, VERSION and
  the documentation audit.
- Item 1 now describes the current OpenCode pack (`agents/` + `commands/`) and marks
  `opencode.flowforge.json` as a legacy path.
- Replaced legacy `RF`/`RNF` with `FR`/`NFR`, updated the IDE scope to include Copilot/Kilo,
  and refreshed the release tags to `v0.4.x` / `v0.1.0-alpha.x`.

---

## 3. Open findings

| ID | Severity | Document | Finding | Recommendation |
|----|----------|----------|---------|----------------|
| D-01 | suggestion | `docs/decisions/ADR-001, 005, 016, 017, 024, 027`; `CHANGELOG.md`; `docs/backlog/NS-09` | 20 links point to `.ai-work/` session artifacts that are not versioned (ADR-002). They are broken in a clean clone. | Keep as session references or replace with plain text. `docs/README.md` now documents the rule. |
| D-02 | warning | `docs/PRD.md` | Status is still `Draft` (last updated 2026-05-13). | Human decision: move to `In Review` / `Accepted`, or mark as a historical PRD. |
| D-03 | warning | HUs 042, 043, 046, 047, 048, 050 | They do not use the standard HU template headings (`User Story`, `Acceptance Criteria`, `Scenarios`, `Definition of Done`). They use equivalent sections (`Requirements`, `Acceptance Criteria`, `Implementation Tasks`, `Criterios de Aceptación`). | **Mitigated 2026-11-06**: a “Template compliance note” was added to each HU; the content is present under equivalent headings. Normalize headings only if a future FlowDoc tool requires literal section names. |
| D-04 | suggestion | `docs/14-flowforge-complete-reference.md` | Header now says 1.3/35 skills, but some Part 1–4 sections still reflect v0.4-era commands (e.g. `forge-orchestrator` naming). | **Fixed 2026-11-06**: header/version, `forge-orchestrator` → `flowforge`, CLI status sample, 35-skill table, FR/NFR notation, `/fforge-init` command, on-demand skills section, and current-tooling table. |
| D-05 | suggestion | `docs/15-agent-skills-technical-spec.md` | Header now says 35 skills, but Part 1 tables still use legacy conceptual identifiers and some Spanish labels. | **Mitigated 2026-11-06**: header/version, `flowforge` naming, FR/NFR IDs, 35-skill table, gap statuses, and retention dry-run gate updated. Legacy conceptual function names remain by design (Part 1 note). |
| D-06 | suggestion | `docs/04-roadmap.md` | Many checkboxes for completed items remain `[ ]`. The historical banner explains this, but the checkboxes are still misleading. | Leave as historical, or convert the checklists to a “Done” summary table. |

---

## 4. Verification commands

```bash
# ADR files vs index
python3 - <<'PY'
from pathlib import Path
import re, collections
files = sorted(Path('docs/decisions').glob('ADR-*.md'))
by = collections.defaultdict(list)
for f in files:
    m = re.match(r'ADR-(\d{3})', f.name)
    if m: by[m.group(1)].append(f.name)
print('duplicates:', {k:v for k,v in by.items() if len(v)>1} or 'none')
print('missing 001..033:', [f'{n:03d}' for n in range(1,34) if f'{n:03d}' not in by] or 'none')
PY

# Skill count
find skills -name SKILL.md | wc -l

# Broken links (excluding archive)
# Re-run the link checker used in this audit; expected remaining = 20 `.ai-work` links.
```

---

## 5. Conclusion

The documentation set is now **structurally complete and internally consistent** for the
FlowDoc base types: index, PRD, ADRs, RFC placeholder, API/DB N/A, HUs, backlog and archive.
The remaining findings are either **intentional local references** (`.ai-work`) or **human
decisions** (PRD status, HU format normalization). No product code was modified in this branch.
