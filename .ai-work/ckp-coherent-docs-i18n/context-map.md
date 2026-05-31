# Context Map — CKP-coherent docs & i18n (Item 15)

> **Phase 0 (Discovery)** — 2026-05-28  
> **CKP-0:** ✅ Requirement is clear (audit partial i18n backlog and decide what to resume).

## Request summary

Verify what remains **partial** for:

1. **Item 15** — Doc audit / CKP-coherent docs (`docs/08`, `docs/03`, optional `docs/15` legacy tables)
2. **Public language** — General i18n per [`docs/I18N.md`](../../docs/I18N.md)

## Prior work (in-repo)

| Source | Finding |
|--------|---------|
| [`docs/I18N.md`](../../docs/I18N.md) | Tracker lists **Done**, **Partial**, **Next**; doc-audit checklist mostly ✅ |
| [`docs/04-roadmap.md`](../../docs/04-roadmap.md) | Release gate: CKP-coherent docs 🟡, Public language 🟡 |
| [`.engram/local_memory/obs-2026-05-27-session-close.md`](../../.engram/local_memory/obs-2026-05-27-session-close.md) | Notes item 15 + `docs/08` still open |

## What is already ✅ (no rework needed)

Per `I18N.md` **Doc audit (item 15)**:

- 7 agents / 5 CKPs documented consistently
- `verify-report` naming (not `cert-report`)
- `.ai-work/{slug}/` convention
- `examples/crud-tareas/` proof artifacts
- `VERSION.md` / `CHANGELOG.md` present

**English already done:** README/QUICKSTART (+ ES), core docs `01,02,05,06,07,09,10,11,13,14,16,18`, `04-roadmap`, all 7 core skills, teacher, IDE parity, examples.

## What is still 🟡 Partial

| File | Lines (approx) | Issue | Priority |
|------|----------------|-------|----------|
| [`docs/08-test-plan.md`](../../docs/08-test-plan.md) | ~170 | **100% Spanish**; VS Code + `openspec/changes/` paths (legacy); manual experiment for 5 skills | **P0 — next in I18N.md** |
| [`docs/03-engram-dotnet-gaps.md`](../../docs/03-engram-dotnet-gaps.md) | ~215 | EN header + status summary; **body mostly Spanish**; historical “gaps” mostly implemented | **P1 — trim + EN “implemented features” summary** |
| [`docs/15-agent-skills-technical-spec.md`](../../docs/15-agent-skills-technical-spec.md) | ~876 | Summary + Part 1 headers EN; **Part 2–3 section titles Spanish**; Part 1 tables use legacy Spanish function names by design | **P2 — optional** (large) |

## Risks / constraints

- **`docs/08`** references `openspec/changes/` isolation; current FlowForge standard is `.ai-work/{slug}/`. Translation should **modernize paths** in the same pass (deterministic).
- **`docs/15` Part 1** legacy Spanish identifiers are **documented as intentional**; full table EN pass is high effort, low runtime impact (skills are source of truth).
- **`docs/03`** must not re-open engram-dotnet implementation backlog; keep as **reference + MCP mapping** only.

## Recommendation (resume or defer)

| Track | Action | Effort |
|-------|--------|--------|
| **Minimum to close item 15 gate** | Translate + modernize `docs/08`; shorten `docs/03` body to EN | ~2–4 h |
| **Full public-language polish** | Above + EN headers for `docs/15` Part 2–3; optional Part 1 table pass | +4–8 h |
| **Defer** | Leave `docs/15` legacy catalog as-is; update `I18N.md` + roadmap to “accepted debt” | 15 min |

## Delegation

→ **forge-arch:** `spec.md` with scoped FR/NFR, PM-* for human verification, and explicit in/out of scope for optional `docs/15` work.
