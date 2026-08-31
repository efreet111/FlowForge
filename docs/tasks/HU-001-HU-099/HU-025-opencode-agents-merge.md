---
hu_id: HU-025
title: "OpenCode installer debe actualizar AGENTS.md global sin perder datos"
status: done
flowforge_slug: "hu-025"
---

# HU-025 — OpenCode installer debe actualizar AGENTS.md global sin perder datos

## User Story

**As a** FlowForge user,
**I want** que el instalador de FlowForge para OpenCode actualice `~/.config/opencode/AGENTS.md` sin sobrescribir ni perder datos existentes,
**so that** FlowForge y las reglas existentes del usuario (como Engram Protocol) coexistan sin conflictos de precedencia.

---

## Acceptance Criteria (business-level)

- [ ] AC-1: El instalador detecta si existe `~/.config/opencode/AGENTS.md`
- [ ] AC-2: El instalador preserva todo el contenido pre-existente del AGENTS.md
- [ ] AC-3: El instalador inserta las reglas de FlowForge (pre-flight AGENTS.md-first) en el lugar correcto
- [ ] AC-4: Las reglas de Engram Protocol existentes se mantienen intactas
- [ ] AC-5: Si el usuario ya tiene rules conflicting con FlowForge, el instalador lo reporta sin modificar

---

## Scenarios (SDD Spec)

### Happy Path

- [ ] **[OpenCode sin AGENTS.md previo]**
  **GIVEN** `~/.config/opencode/AGENTS.md` no existe
  **WHEN** el usuario ejecuta `flowforge install --ide opencode`
  **THEN** se crea AGENTS.md con el contenido completo de FlowForge + Engram Protocol
  **🧪 Ref**: `install/install.sh` → "creates AGENTS.md from template"

- [ ] **[OpenCode con AGENTS.md existente]**
  **GIVEN** `~/.config/opencode/AGENTS.md` existe y contiene Engram Protocol
  **WHEN** el usuario ejecuta `flowforge install --ide opencode`
  **THEN** el instalador hace merge preserving existing content + injects FlowForge rules
  **🧪 Ref**: `install/install.sh` → "preserves existing AGENTS.md + appends FlowForge section"

### Edge Cases

- [ ] **[Conflicto de reglas]**
  **GIVEN** AGENTS.md tiene una regla que contradice "AGENTS.md first" de FlowForge
  **WHEN** el instalador detecta el conflicto
  **THEN** reporta el conflicto al usuario y no modifica el archivo sin autorización
  **🧪 Ref**: `install/install.sh` → "reports conflict without auto-modifying"

- [ ] **[AGENTS.md vacío o corrupto]**
  **GIVEN** AGENTS.md existe pero está vacío o malformado
  **WHEN** el instalador lo detecta
  **THEN** pregunta al usuario si desea recrearlo o preservarlo
  **🧪 Ref**: `install/install.sh` → "handles malformed AGENTS.md gracefully"

---

## Context / Notes

### Problema actual

El `install-skills.sh` copia skills a `~/.config/opencode/skills/` pero **no modifica** `~/.config/opencode/AGENTS.md`. Esto genera un conflicto de precedencia:

| Fuente | Regla |
|--------|-------|
| `~/.config/opencode/AGENTS.md` (existente) | `mem_context` para "what did we do" (Engram Protocol) |
| Skills FlowForge en `~/.config/opencode/skills/` | "AGENTS.md first" pre-flight rule |

Cuando FlowForge se instala, ambas reglas coexisten. El agente (yo) aplica la del Engram Protocol por ser el comportamiento base, ignorando la de FlowForge.

### Solución propuesta

El instalador debe hacer **merge no destructivo** en `~/.config/opencode/AGENTS.md`:
1. Detectar contenido existente (Engram Protocol, Persona, etc.)
2. Insertar las reglas de FlowForge (forge-orchestrator pre-flight) preservando el orden
3. Resolver conflictos de precedencia explícitamente (FlowForge pre-flight > Engram mem_context para recall)

### Referencias técnicas

- ADR-004: FlowDoc integration
- install-skills.sh (línea 88): donde se copian skills a OpenCode
- `ide/opencode/generate-config.sh`: generador de config para OpenCode

---

## Owner & Timeline

- **Owner**: @kaito
- **Target milestone**: v0.5
- **Dependencies**: Ninguna — esta HU es independiente

---

## Definition of Done

- [ ] Code reviewed and merged
- [ ] Unit tests passing para el script de instalación
- [ ] Manual test: instalar en entorno con AGENTS.md pre-existente
- [ ] ADR creada si hay nueva decisión técnica de merge

---

## FlowForge

> This section is managed by FlowForge agents. Do not edit manually.

To implement this HU:

```bash
# Start the feature cycle
/flow-start opencode-agents-merge
# HU file: HU-025-opencode-agents-merge.md (range-binned)
```

- `flowforge_slug` is set by forge-arch when `.ai-work/{slug}/` is created
- HU status lifecycle: `draft` → `in-progress` → `done`
- Status is updated by forge-memory at `/flow-close`

---

## Technical Debt (if applicable)

- El script `install-skills.sh` actualmente solo copia skills — necesita lógica de merge para AGENTS.md. Ver línea 88.

## Additional Artifacts (out of scope, created for dev/testing)

- `install/dev/install-dev.sh` — script shell para instalación local sin .NET ni GitHub
- `Dockerfile.test` — contenedor Docker para tests automatizados y validación local
- ADR-022 — documenta el patrón de merge (marcadores gentle-ai + hash SHA-256)
