# Session Summary — Orchestrator Memory Curation Protocol

> **Feature slug**: `agent-proactive-memory`  
> **Date**: 2026-05-30  
> **CKP-4**: Ready for deploy decision  
> **Verify**: PASS (rework cycle 1 resolved)

---

## Goal

Establish an IDE-agnostic, model-agnostic protocol so FlowForge sessions persist
relevant knowledge to Engram consistently: Memory Signal from `forge-arch` and
`forge-dev`, 3-step curation by the orchestrator, mandatory `mem_session_summary`
at `/flow-close`.

Origin: offline-first failure report (2026-05-30) — sessions closed with zero Engram writes.

---

## Discoveries

- Offline-first (local → queue → sync) works; the gap was agent discipline + MCP stability.
- Orchestrator curation is preferable to distributed saves — it uses `revision_cycle` and `rework_count` subagents lack.
- Skills are the source of truth; IDE files are thin adapters (Cursor `alwaysApply` is not portable).
- FR-004 required explicit forge-memory instructions, not only orchestrator verification.
- MCP unavailable during close → fallback to `.engram/local_memory/` (used this session).

---

## Accomplished

- ✅ **ADR-001** — decision record + `docs/decisions/README.md` (ADR workflow guide)
- ✅ **spec.md** — FR-001–004, NFR-001–005, PM-1–5 complete
- ✅ **plan.md** — 12 files implemented across 4 layers
- ✅ **Implementation** — Memory Signal, Memory Curation Protocol, cross-IDE parity
- ✅ **Docs** — `docs/10` offline-first lifecycle; MCP binary in `context-project.md`
- ✅ **Tracking** — roadmap item 20, CHANGELOG Unreleased
- ✅ **Verify** — PASS after rework (forge-memory session close protocol)
- ✅ **Local memory** — ADR + MCP config observations saved to `.engram/local_memory/`

---

## ✅ Pruebas Manuales del Desarrollador

- PM-1: Signal en forge-arch — ✅ ejecutada
- PM-2: Signal en forge-dev — ✅ ejecutada
- PM-3: 3 pasos en orquestador — ✅ ejecutada
- PM-4: Cross-IDE parity — ✅ ejecutada
- PM-5: Fallback documentado — ✅ ejecutada

Verificadas por el desarrollador humano (marcadas en `spec.md`).

---

## Next Steps

| Priority | Action |
|----------|--------|
| P0 | Update `~/.cursor/mcp.json` → `dist/win-x64-fixed/engram.exe` |
| P1 | When server healthy: sync enroll `team/flowforge` + ingest local buffer |
| P2 | Optional: `mem_promote_to_md` for ADR-001 when MCP stable |
| P2 | Reload Cursor after agent file changes |

---

## Relevant Files

| Path | Role |
|------|------|
| `skills/forge-orchestrator/SKILL.md` | Memory Curation Protocol |
| `skills/forge-arch/SKILL.md` | Memory Signal (arch) |
| `skills/forge-dev/SKILL.md` | Memory Signal (dev) |
| `skills/forge-memory/SKILL.md` | Session close protocol |
| `ide/shared/workflow-orchestrator-parity.md` | Cross-IDE contract |
| `docs/decisions/ADR-001-memory-curation-protocol.md` | Why this design |
| `docs/decisions/README.md` | How to write ADRs |
| `docs/10-memory-mapping-fallback.md` | Offline-first lifecycle |
| `.ai-work/agent-proactive-memory/verify-report.md` | PASS verdict |

---

## Memory persistence (this close)

- `mem_session_summary` via MCP: **failed** (MCP unavailable)
- Fallback: `.engram/local_memory/obs-2026-05-30-session-close-agent-proactive-memory.md`
- Pending ingest when MCP healthy: `obs-2026-05-30-adr-001-*.md`, `obs-2026-05-30-config-mcp-*.md`
