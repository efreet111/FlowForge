---
title: "ADR-001: Orchestrator Memory Curation Protocol"
type: decision
topic_key: architecture/memory-curation-protocol
date: "2026-05-30"
scope: team
project: team/flowforge
significance: high
---

## What
Protocolo IDE-agnóstico para persistencia proactiva de conocimiento en Engram durante
flujos FlowForge. Solo forge-arch y forge-dev emiten un Memory Signal (3 campos) al
final de su handoff. El orquestador aplica 3 pasos: tipo elegible → fricción
(revision_cycle/rework_count) → dedup (mem_search). mem_session_summary obligatorio
en /flow-close. Fallback a .engram/local_memory/ cuando MCP no responde.

## Why
Sesiones se cerraban sin ningún save a Engram porque: (a) no había criterios claros
de qué guardar, (b) lenguaje soft en los skills ("on hard bugs, mem_save"), (c)
mem_session_summary era opcional. Análisis del reporte offline-first 2026-05-30.

## Where
- skills/forge-orchestrator/SKILL.md (Memory Curation Protocol — nuevo)
- skills/forge-arch/SKILL.md (Memory Signal en output)
- skills/forge-dev/SKILL.md (reemplaza mem_save directo por Signal)
- ide/shared/workflow-orchestrator-parity.md (contrato cross-IDE)
- docs/decisions/ADR-001-memory-curation-protocol.md

## Learned
Tres opciones evaluadas: distribuida (sin contexto cross-fase, rechazada), global rule
(no resuelve dedup ni fricción, rechazada como primaria), orchestrator curation
(elegida: usa revision_cycle y rework_count que solo el orquestador conoce).
Solo 2 agentes emiten signal porque solo forge-arch y forge-dev generan conocimiento
nuevo persistible. forge-plan, forge-verify, forge-discovery producen artefactos
en .ai-work/ que ya son su propia evidencia.
