# Plan: Agent Proactive Memory — Orchestrator Memory Curation Protocol

> **Feature slug**: `agent-proactive-memory`
> **Phase**: 2 (Plan) — CKP-2 🟡
> **Design**: Opción B simplificada — Orchestrator Curation al mínimo viable
> **Decisión de diseño**: El orquestador decide qué se guarda usando contexto cross-fase
> (revision_cycle, rework_count) que los subagentes no tienen.

---

## 0. Pre-dev tracking artifacts (completados antes de /flow-dev)

| Artefacto | Ruta | Estado |
|-----------|------|--------|
| spec.md | `.ai-work/agent-proactive-memory/spec.md` | ✅ Creado |
| ADR-001 | `docs/decisions/ADR-001-memory-curation-protocol.md` | ✅ Creado |
| Roadmap ítem 20 | `docs/04-roadmap.md` — sección Post-release | ✅ Agregado |
| CHANGELOG Unreleased | `CHANGELOG.md` — sección Added | ✅ Agregado |

---

## 1. Impact and dependencies

### Principio rector

Solo 2 agentes producen conocimiento nuevo persistible: `forge-arch` (decisiones de diseño)
y `forge-dev` (bugs/gotchas de implementación). El resto produce artefactos en `.ai-work/`
que ya son su propia evidencia.

El orquestador ya mantiene un traceability log inline (agent/model/fallback). Memory Curation
es una adición al mismo patrón — coordinación inline, no product work.

`forge-memory` no cambia: maneja `mem_session_summary` en `/flow-close` como siempre.

### Mapa de cambios

```
skills/forge-orchestrator/SKILL.md    ← Sección "Memory Curation Protocol" (nuevo)
skills/forge-arch/SKILL.md            ← Memory Signal al final del handoff
skills/forge-dev/SKILL.md             ← Reemplaza soft "on hard bugs" por Memory Signal
ide/shared/workflow-orchestrator-parity.md  ← Contrato cross-IDE del protocolo
docs/10-memory-mapping-fallback.md    ← Sección offline-first lifecycle
ia-work/context-project.md            ← MCP binary path + enroll flow
--- propagación thin (sin lógica) ---
ide/cursor/agents/forge-arch.md
ide/cursor/agents/forge-dev.md
ide/cursor/rules/workflow.mdc
ide/antigravity/rules/workflow.md
ide/opencode/AGENTS.md
ide/vscode/copilot-instructions.md
```

### No cambia

| Archivo | Motivo |
|---------|--------|
| `skills/forge-memory/SKILL.md` | Maneja session_summary en /flow-close — correcto como está |
| `skills/forge-plan/SKILL.md` | No genera conocimiento nuevo; plan.md es su artefacto |
| `skills/forge-verify/SKILL.md` | PASS/rework_ticket ya son los artefactos; no persiste conocimiento nuevo |
| `skills/forge-discovery/SKILL.md` | Lee memoria, rara vez crea conocimiento nuevo |

---

## 2. File changes (Proposed Changes)

### Capa 1 — Skills fuente de verdad

- [MODIFY] `skills/forge-orchestrator/SKILL.md`
  Reemplazar la sección actual "Automatic timestamps (cycle-time metrics)" por
  "Memory Curation Protocol" con:
  - Cuándo se activa (al recibir handoff de forge-arch o forge-dev)
  - El proceso de 3 pasos (tipo → fricción → deduplicación)
  - Fallback a `.engram/local_memory/` cuando MCP no está disponible
  - mem_session_summary obligatorio en `/flow-close` (no opcional)

- [MODIFY] `skills/forge-arch/SKILL.md`
  En la sección "Memory protocol" existente, agregar el formato Memory Signal
  al final del handoff output. Reemplazar el actual "Run mem_search for prior
  decisions / on conflict STOP" (que solo lee) por: leer + signal de salida.

- [MODIFY] `skills/forge-dev/SKILL.md`
  Reemplazar "On hard bugs or framework gotchas, mem_save as bugfix or discovery
  before handing off" por el formato Memory Signal. El agente ya no llama
  mem_save directamente — emite la señal; el orquestador decide.

### Capa 2 — Shared contract cross-IDE

- [MODIFY] `ide/shared/workflow-orchestrator-parity.md`
  Agregar sección "Memory Curation Protocol" documentando:
  - Qué agentes emiten Memory Signal (forge-arch, forge-dev)
  - El formato del signal (3 campos)
  - El proceso de 3 pasos del orquestador
  - Que forge-memory maneja session_summary en /flow-close

### Capa 3 — IDE adapters (propagación thin, sin lógica nueva)

- [MODIFY] `ide/cursor/agents/forge-arch.md`
  Propagar la nueva sección Memory Signal del skill

- [MODIFY] `ide/cursor/agents/forge-dev.md`
  Propagar el reemplazo de mem_save directo por Memory Signal

- [MODIFY] `ide/cursor/rules/workflow.mdc`
  Agregar en la sección de traceability log: referencia al Memory Curation Protocol

- [MODIFY] `ide/antigravity/rules/workflow.md`
  Mismo thin signal que cursor

- [MODIFY] `ide/opencode/AGENTS.md`
  Agregar nota del Memory Curation Protocol al bloque de orquestación

- [MODIFY] `ide/vscode/copilot-instructions.md`
  Agregar línea en sección Orchestrator role sobre memory curation

### Capa 4 — Documentación

- [MODIFY] `docs/10-memory-mapping-fallback.md`
  Agregar sección "3. Offline-first lifecycle":
  - "Cerrar el IDE no dispara ningún save en Engram"
  - Diagrama del flujo: signal → orquestador → mem_save → sync cola → push
  - Enroll: tras primer mem_save en proyecto nuevo → POST /sync/enroll/{project}

- [MODIFY] `ia-work/context-project.md`
  - Actualizar bloque MCP Cursor: command → `dist/win-x64-fixed/engram.exe`, args → `["mcp"]`
  - Agregar nota: por qué binario fijo en vez de `dotnet run --no-build`

---

## 3. Contracts and schemas

### Memory Signal — formato del handoff de forge-arch y forge-dev

```markdown
## Memory Signal
- type: decision | bugfix | config | pattern | none
- significance: high | low
- summary: "Una sola línea describiendo qué ocurrió"
```

Reglas para el agente emisor:
- `type: none` → si no ocurrió nada relevante, usar none (no inventar señales)
- `significance: high` → decisión que define patrones futuros, o bug que requirió 3+ intentos
- `significance: low` → todo lo demás que igualmente vale mencionar
- El agente **describe**, no decide si se guarda — esa decisión es del orquestador

### Memory Curation Protocol — proceso del orquestador (3 pasos)

```
Al recibir handoff de forge-arch o forge-dev:

PASO 1 — Tipo elegible?
  Si type == none → SKIP
  Si type en {decision, bugfix, config, pattern} → continuar

PASO 2 — Hubo fricción? (el orquestador ya conoce esto)
  Si significance == high → continuar
  Si revision_cycle >= 1 (spec fue rechazado) → continuar
  Si rework_count >= 2 (dev falló 2+ veces) → continuar
  Si ninguno de los anteriores → SKIP

PASO 3 — ¿Ya existe en Engram?
  Llamar mem_search(query=summary, limit=1)
  Si resultado reciente y similar existe → SKIP (evitar duplicado)
  Si no existe → mem_save(title, type, content=summary, topic_key derivado)
  Si MCP no responde → escribir .engram/local_memory/obs-<timestamp>.md con
                        el mismo contenido y YAML frontmatter
```

### mem_session_summary — obligatorio en /flow-close

```
Antes de CKP-4:
  El orquestador verifica que forge-memory haya llamado mem_session_summary.
  Si no → forge-memory debe llamarla antes de reportar cierre.
  Si MCP no responde → escribir .engram/local_memory/obs-<timestamp>-session-close.md
```

### Offline fallback YAML structure

```markdown
---
title: "[descripción en una línea]"
type: "decision | bugfix | config | pattern | session_summary"
topic_key: "area/tema"
date: "YYYY-MM-DD"
scope: "team | personal"
project: "[nombre-proyecto]"
significance: "high | low"
---

## What
## Why (solo si aplica)
## Learned
```

---

## 4. Implementation checklist

### Capa 1: Skills fuente de verdad
- [x] 1.1 `skills/forge-orchestrator/SKILL.md`: Reemplazar "Automatic timestamps" por
      sección "Memory Curation Protocol" con el proceso de 3 pasos y fallback
- [x] 1.2 `skills/forge-arch/SKILL.md`: Agregar Memory Signal al output format de la
      sección "Memory protocol"
- [x] 1.3 `skills/forge-dev/SKILL.md`: Reemplazar mem_save directo por Memory Signal
      en la sección "Memory protocol"

### Capa 2: Shared contract
- [x] 2.1 `ide/shared/workflow-orchestrator-parity.md`: Agregar sección "Memory Curation
      Protocol" con el contrato cross-IDE (signal format + 3 pasos + forge-memory role)

### Capa 3: IDE adapters (propagación thin)
- [x] 3.1 `ide/cursor/agents/forge-arch.md`: Propagar Memory Signal del skill
- [x] 3.2 `ide/cursor/agents/forge-dev.md`: Propagar reemplazo de mem_save directo
- [x] 3.3 `ide/cursor/rules/workflow.mdc`: Agregar referencia al Memory Curation Protocol
      en la sección de traceability
- [x] 3.4 `ide/antigravity/rules/workflow.md`: Agregar thin signal equivalente
- [x] 3.5 `ide/opencode/AGENTS.md`: Agregar nota del protocolo en bloque de orquestación
- [x] 3.6 `ide/vscode/copilot-instructions.md`: Agregar línea en sección Orchestrator role

### Capa 4: Documentación
- [x] 4.1 `docs/10-memory-mapping-fallback.md`: Agregar sección "Offline-first lifecycle"
      con diagrama + enroll flow
- [x] 4.2 `ia-work/context-project.md`: Actualizar MCP binary path + nota de enroll
