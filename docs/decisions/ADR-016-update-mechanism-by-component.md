# ADR-016 — Update Mechanism por Componente (`flowforge update --component`)

> **Status**: **Accepted — shipped** (2026-08-12) in `flowforge-update-mechanism` (commits `654ddc1`, `786fd78`, `984daa3`); PM-2 (rollback manual test) deferred as minor technical debt
> **Date**: 2026-08-12
> **Feature**: `flowforge-update-mechanism`
> **Deciders**: Engineering (FlowForge methodology team)
> **Links**: [`spec`](../../.ai-work/flowforge-update-mechanism/spec.md) · [`plan`](../../.ai-work/flowforge-update-mechanism/plan.md) · [`verify-report`](../../.ai-work/flowforge-update-mechanism/verify-report.md) · [`summary`](../../.ai-work/flowforge-update-mechanism/summary.md) · [`installer-baseline`](../../.ai-work/flowforge-update-mechanism/installer-baseline.md) · [ADR-002](ADR-002-scaffold-doc-policy.md) · [ADR-008](ADR-008-ide-installer-path-matrix.md)

---

## Context

`flowforge update` (v0.1.0-alpha.6) solo actualizaba el binario `engram` — swap in-place **sin backup, sin health-check y sin granularidad**. Los packs de FlowForge (skills, agents, rules) solo se refrescaban re-ejecutando `flowforge install`, que **sobrescribía configuraciones críticas del usuario**:

- **Bug de pérdida de datos (S6, 2 ocurrencias)**: `mcp.json` de Cursor y `mcp_config.json` de Antigravity perdían todos los servers MCP ajenos a FlowForge al regenerar el archivo completo.
- El cache git (`~/.flowforge/cache/FlowForge`) se clonaba `--depth 1` una vez y **nunca se refrescaba** (skills/agents stale).
- Versiones hardcodeadas (`InstallerVersion = "0.1.0-alpha.6"`) en **drift** con el manifest real (`0.1.0-alpha.7`).
- Sin tests de update. Sin rollback. Sin detección de archivos modificados por el usuario.

**Requisito adicional (Installer Protection Policy)**: el installer (`src/FlowForge.Installer/`) ha sufrido regresiones en features anteriores; este feature **no debe romper funcionalidad existente** (ver ADR-017).

---

## Decision drivers

- **No-regresión**: el installer existente no debe romperse; el nuevo mecanismo debe *componer* con los módulos actuales, no reemplazarlos.
- **Preservación de config de usuario**: ningún update puede destruir servers MCP, config de modelos o agentes editados manualmente.
- **Tolerancia a fallo**: un binario roto nunca debe dejar la instalación inutilizable → backup + rollback.
- **Granularidad**: poder actualizar solo engram o solo los skills.
- **Trazabilidad**: versiones por componente + logging estructurado + `flowforge status`.

---

## Options considered

### Option A — UpdateOrchestrator que compone módulos existentes (✅ Accepted)

Nueva clase `UpdateOrchestrator` que orquesta los módulos ya probados (`EngramModule.UpdateAsync`, `ConfigStore`, `ManifestClient`, `FlowForgeModule`, `GitHubReleasesClient`), añadiendo las nuevas capacidades como unidades independientes (`BackupManager`, `HealthCheckRunner`, `McpConfigMerger`, `UserModifiedAgentDetector`, `CacheRefresher`, `EngramProcessChecker`, `ManagedPathsSidecarFactory`, `ComponentRegistry`).

**Pros**:
- ~80% de reuso del código existente (se probó en la práctica: 1.8k líneas nuevas para 12 FRs)
- Cada capacidad nueva es testeable en aislamiento (9 archivos de tests)
- Aprovecha garantías ya verificadas: SHA-256 obligatorio, write atómico de `ConfigStore`, merge quirúrgico de OpenCode
- No toca los comandos existentes (`install`, `uninstall`, `doctor`)

**Cons**:
- Orquestación compleja (fases 0-12) → riesgo de integración (se gestionó con 2 ciclos de rework)

**Decision**: ✅ **Accepted**.

### Option B — Reescribir el pipeline de update desde cero (❌ Rejected)

Pipeline monolítico nuevo que absorbe download/swap/merge/config en una sola clase.

**Pros**: Control total del flujo.

**Cons**:
- Duplica lógica ya probada (download con SHA-256, atomic writes)
- Riesgo alto de regresión en paths existentes
- Tests duplicados o reescritura de garantías ya verificadas

**Decision**: ❌ **Rejected** — viola la política de no-regresión (ADR-017) y duplica garantías ya cubiertas.

---

## Decision

**Actualización granular por componente orquestada por `UpdateOrchestrator`, componiendo módulos existentes (~80% reuso).**

### Key design choices

| Tema | Decisión | Racional |
|------|----------|----------|
| **MCP merge** | Patrón OpenCode (`JsonNode` quirúrgico read-merge-write) generalizado a Cursor `mcp.json` y Antigravity `mcp_config.json` | Elimina los 2 bugs de pérdida de datos (S6); los servers del usuario se preservan byte-for-byte |
| **Backup/rollback** | `~/.flowforge-backups/{component}-{timestamp}`, retención máx. 5; secuencia backup → download → health-check → move atómico → update version; rollback en cualquier fallo previo al move | Nunca dejar una instalación rota |
| **Agentes modificados** | SHA-256 compare; opciones Skip / Backup+overwrite / Overwrite; `--yes` → backup+overwrite (no destructivo), `--force` → overwrite sin backup | Clasificación managed-vs-user; nunca pisar ediciones del usuario sin consentimiento |
| **Sidecar** | `managed-paths.json` generalizado a todos los destinos IDE (Cursor, Antigravity, VS Code Copilot, Kilo) vía `ManagedPathsSidecarFactory` | Consistencia cross-IDE (OQ-3); ADR-008 como fuente de verdad de paths |
| **Cache git** | `git pull` sobre `~/.flowforge/cache/FlowForge`; fallback a `git clone --fresh` si pull falla | Cache stale/corrupto no bloquea updates (OQ-2: desde `main`) |
| **Proceso check** | `EngramProcessChecker` detecta engram corriendo antes del swap; warn/abort salvo `--force` | Evita binarios en uso / corrupción |
| **Fuente de versión** | Versiones derivadas del manifest/`config.json`, no constantes hardcodeadas | Elimina drift de versiones (bug de status en rework cycle 2) |
| **OQ-1 `--self`** | OUT (diferido) | El bootstrap `curl \| bash` sigue siendo el camino de update del installer; evita auto-reemplazo cross-platform (Windows) |
| **Channel** | Global (mismo que `config.json.channel`) para v1 | Channels por componente es post-MVP |

---

## Consequences

### Positive

- El update nunca más destruye config de usuario (MCP servers, modelos, agentes editados).
- Rollback automático garantiza instalación siempre funcional.
- Granularidad + `flowforge status` → trazabilidad completa por componente.
- Base de código del installer protegida: los comandos existentes siguen intactos.

### Negative / Accepted debt

- **OQ-1**: `flowforge update --self` no disponible en v1 (bootstrap required).
- **PM-2**: test manual de rollback con binario roto diferido (requiere simulación compleja; cobertura unitaria de `BackupManager`/`UpdateOrchestrator` como salvaguarda).
- **Versiones por channel**: solo channel global en v1.
- **1 función MCC > 10** reportada en verify (sin cambio entre ciclos).

### Risks

| Risk | Mitigation |
|------|-----------|
| Regresión en comandos existentes | Baseline + tests de regresión (Phase 0/12, ADR-017) |
| Merge MCP rompe servers | Pattern quirúrgico + tests unitarios con servers existentes (PM-3 ✅) |
| Rollback no validado end-to-end | Tests unitarios; PM-2 diferido con seguimiento |
