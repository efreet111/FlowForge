---
title: "Session summary: FlowForge v0.4.x — i18n, Case 1, backlog item 19"
type: session_summary
topic_key: session/2026-05-27-flowforge
date: "2026-05-27"
scope: team
project: team/flowforge
session_id: manual-save-team/flowforge
---

## Goal
Continuar el plan de mejoras FlowForge v0.4.x: i18n, validación Case 1 CRUD, cierre ítem 2, spec backlog ítem 19 (asociación memoria proyecto), y push a GitHub.

## Instructions
- Responder en español al usuario.
- Offline-first en engram: **guardar en SQLite local primero**; sync al servidor solo cuando esté sano (no forzar push si HTTP falla).
- Proyecto engram canónico: `team/flowforge` (convendría actualizar `ENGRAM_PROJECT` en mcp.json desde `engram-dotnet`).
- Demo CRUD vive en `flowforge-demo-task-manager` (local, sin git); artefactos limpios en `examples/crud-tareas/`.

## Discoveries
- `mem_save` en engram-dotnet con proyecto nuevo: guarda igual y solo advierte si hay proyecto similar (`ProjectDetector.FindSimilar`); no hay gate interactivo team/personal (backlog ítem 19).
- MCP engram puede colgar si sync/servidor no responde; CLI con `ENGRAM_SYNC_ENABLED=false` escribe local sin bloquear.
- `git push` en PowerShell puede tardar; `GIT_TERMINAL_PROMPT=0` evita cuelgue por credenciales.
- Push exitoso commit `941200d` (backlog ítem 19).

## Accomplished
- ✅ v0.4.0 / v0.4.1: i18n README, QUICKSTART EN+ES, docs core, skills core EN, agentes Cursor recompilados
- ✅ Ítem 2 cerrado: `examples/crud-tareas/CASE-1-VALIDATION.md`, PM/plan/summary en examples
- ✅ OpenCode install: placeholder `__FLOWFORGE_REPO__` en `ide/install.ps1` y `install.sh` (commit `f3ee881`)
- ✅ Ítem 19 spec: `docs/19-project-memory-association-backlog.md` + roadmap + I18N (commit `941200d`, pushed)
- ✅ Sync manual probado previamente a servidor `192.168.0.178:7437` (seq ~1124); rutina `mem_doctor` pendiente con servidor sano
- 🔲 Ítem 15: traducir `docs/08-test-plan.md`
- 🔲 Ítem 1: smoke OpenCode Linux post-parche
- 🔲 Ítems 5–6, 8–9: template, schema, smoke IDEs, engram MCP rutina
- 🔲 Implementación engram ítem 19 (`mem_resolve_project`, dry-run)

## Relevant Files
- docs/19-project-memory-association-backlog.md — spec backlog asociación proyecto al primer save
- docs/04-roadmap.md — ítem 19 en post-release
- docs/I18N.md — tracker product backlog
- examples/crud-tareas/CASE-1-VALIDATION.md — Case 1 PASS
- ide/install.ps1, ide/install.sh — parche OpenCode repo path
- VERSION.md — v0.4.1
