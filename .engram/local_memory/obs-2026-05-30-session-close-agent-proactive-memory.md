---
title: "Session summary: Orchestrator Memory Curation Protocol (item 20)"
type: session_summary
topic_key: session/2026-05-30-agent-proactive-memory
date: "2026-05-30"
scope: team
project: team/flowforge
session_id: manual-save-team/flowforge
---

## Goal
Implement the Orchestrator Memory Curation Protocol (roadmap item 20): IDE-agnostic
proactive knowledge persistence via Memory Signal from forge-arch/forge-dev and
3-step curation by the orchestrator.

## Instructions
- Logic lives in `skills/` (source of truth); IDE adapters are thin propagation only.
- Only forge-arch and forge-dev emit Memory Signal; orchestrator decides what to save
  using revision_cycle and rework_count.
- `mem_session_summary` is mandatory at `/flow-close` — closing the IDE does not auto-save.
- MCP should use fixed binary `dist/win-x64-fixed/engram.exe`, not `dotnet run --no-build`.
- Accept partial knowledge loss; session summary is the safety net.

## Discoveries
- Offline-first design is correct; failure was agents not calling Engram tools, not sync.
- Orchestrator curation beats distributed `mem_save` — only orchestrator has cross-phase context.
- Global Cursor rules are not portable; skills + shared parity is the right pattern.
- forge-memory needed explicit Session close protocol (FR-004 rework after verify).
- MCP may fail; local fallback to `.engram/local_memory/` works and should be ingested at close.

## Accomplished
- ✅ Analysis of offline-first failure report; ADR-001 with options A/B/C
- ✅ spec.md, plan.md, verify PASS after rework cycle 1
- ✅ Memory Curation Protocol + Memory Signal across 12 files, 4 IDEs
- ✅ docs/10 offline-first lifecycle; context-project MCP binary path
- ✅ docs/decisions/README.md — ADR guide for newcomers
- ✅ Roadmap item 20 + CHANGELOG Unreleased
- ✅ Session close protocol in forge-memory skill + Cursor/VSCode agents
- ✅ PM-1 through PM-5 marked complete in spec.md

## Next Steps
- Update `~/.cursor/mcp.json` to fixed engram binary (see ia-work/context-project.md)
- Enroll `team/flowforge` in sync when server is healthy
- Optional: promote ADR-001 via `mem_promote_to_md` when MCP is stable
- Ingest pending `.engram/local_memory/obs-2026-05-30-*.md` into Engram when MCP works

## Relevant Files
- skills/forge-orchestrator/SKILL.md — Memory Curation Protocol
- skills/forge-arch/SKILL.md, skills/forge-dev/SKILL.md — Memory Signal
- skills/forge-memory/SKILL.md — Session close protocol
- ide/shared/workflow-orchestrator-parity.md — cross-IDE contract
- docs/decisions/ADR-001-memory-curation-protocol.md
- docs/decisions/README.md
- .ai-work/agent-proactive-memory/summary.md
