# ADR-022 — Merge no destructivo de `~/.config/opencode/AGENTS.md` por secciones gestionadas

> **Status**: Accepted
> **Date**: 2026-08-31
> **Feature**: `hu-025` (opencode-agents-merge)
> **Deciders**: FlowForge methodology team
> **Links**: [HU-025](../../docs/tasks/HU-001-HU-099/HU-025-opencode-agents-merge.md) · [spec](../../.ai-work/hu-025/spec.md) · [plan](../../.ai-work/hu-025/plan.md) · [AgentsMdMerger.cs](../../src/FlowForge.Installer/Update/AgentsMdMerger.cs) · [ADR-006](ADR-006-opencode-mcp-config.md) · [ADR-016](ADR-016-update-mechanism-by-component.md) · [ADR-017](ADR-017-installer-protection-policy.md) · [ADR-019](ADR-019-opencode-antigravity-customizations.md)

---

## Contexto

El `install-skills.sh` copia skills a `~/.config/opencode/skills/` pero **no modifica** `~/.config/opencode/AGENTS.md`. La regla pre-flight "AGENTS.md first" de FlowForge vive en el agent file (`ide/opencode/agents/flowforge.md`, cargado después), mientras el Engram Protocol vive en el AGENTS.md global (cargado primero). Resultado: el agente aplica la regla del Engram Protocol por ser el comportamiento base, ignorando la de FlowForge — conflicto de precedencia documentado en la sesión 2026-07-08 (#3800).

El installer de producción es el binario C# (`src/FlowForge.Installer`), no los scripts shell. La ruta canónica del archivo es `~/.config/opencode/AGENTS.md` (ADR-019).

## Decision drivers

- **No pérdida de datos**: TODO el contenido ajeno al bloque gestionado debe quedar byte-for-byte idéntico (hash SHA-256 pre/post es requisito duro, no best-effort).
- **Precedencia explícita**: el bloque FlowForge debe insertarse ANTES del bloque `<!-- gentle-ai:engram-protocol -->` para que la regla pre-flight gane.
- **Seguridad por diseño**: backup completo del directorio con retención máx. 5 (ADR-016), escritura atómica tmp+rename, rechazo de symlinks que escapen de `~/.config/opencode/`.
- **Idempotencia**: re-ejecutar install no duplica bloques (no-op por hash canónico).
- **No auto-modificación ante conflicto**: drift o regla conflictiva → SOLO reporta (AC-5 hard stop).
- **Baseline de regresión**: ADR-017 exige `installer-baseline.md` + regression tests pre/post.

## Options considered

### Option A — Sobrescribir AGENTS.md completo con template de FlowForge

**Pros**: Simple, precedencia garantizada.
**Cons**: Destruye contenido del usuario (Engram Protocol, Persona, tokens). **Rejected** — viola la user story.

### Option B — Append del bloque FlowForge al final del archivo

**Pros**: No destructivo.
**Cons**: No resuelve la precedencia: el Engram Protocol (que sigue primero en el archivo) mantiene prioridad de carga; y mezcla secciones sin delimitación clara para futuras re-ejecuciones. **Rejected.**

### Option C — Secciones gestionadas por marcadores + hash canónico (ELEGIDA)

Adaptación del patrón `McpConfigMerger` (JsonNode → secciones markdown): bloques delimitados por `<!-- gentle-ai:* -->`, hash SHA-256 del bloque gestionado embebido como línea `<!-- hash: … -->`, inyección antes del bloque Engram Protocol.

**Pros**: Preservación byte-for-byte verificable, precedencia explícita, idempotencia por hash, drift detectable.
**Cons**: Complejidad media de parsing; el hash se calcula en runtime (no detecta source-spoofing del template — debilidad documentada, RNF-SEC-001); edge case: si el usuario borra solo la línea hash, el bloque se duplica (NFR-002). **Accepted trade-offs.**

## Decision

1. **Merge por secciones delimitadas por marcadores `<!-- gentle-ai:* -->`** como claves — adaptación del patrón `McpConfigMerger` (JsonNode → secciones markdown), implementado en una sola clase `AgentsMdMerger` (plan simplificado de 4 clases a 1).
2. **Inyectar solo la sección pre-flight** (`<!-- gentle-ai:flowforge -->` con el contenido de `ide/shared/workflow-orchestrator-parity.md` §'Pre-flight: AGENTS.md first (mandatory)') y colocarla **antes** del bloque Engram Protocol para que la precedencia FlowForge pre-flight gane explícitamente. Sin marcadores reconocibles → se inserta al inicio.
3. **Hash canónico SHA-256** embebido en el bloque gestionado para distinguir "gestionado intacto" vs "editado por el usuario" (drift → reportar, no sobrescribir), reusando el patrón `UserModifiedAgentDetector`. Ante conflicto o drift, el instalador SOLO reporta (AC-5).
4. **Seguridad**: backup completo de `~/.config/opencode/` en `~/.flowforge-backups/opencode-config-{timestamp}/` (retención máx. 5, ADR-016), escritura atómica (tmp + rename), canonicalización de path + rechazo de symlinks que escapen, guard de tamaño >1 MB, sin loggear contenido (solo metadata/hashes).
5. **Integración**: `FlowForgeModule.InstallOpenCode()` llama `merger.Merge()` y mapea `MergeResult` (`Created`/`Merged`/`IdempotentNoOp`/`ConflictReported`/`Error`) a log + reporte.

## Consecuencias

**Positivas:**

- Precedencia resuelta: la regla pre-flight FlowForge se carga antes que el Engram Protocol.
- Cero pérdida de datos: contenido ajeno byte-idéntico verificado por hash pre/post.
- Re-instalación idempotente (sin bloques duplicados).
- Drift/conflicto del usuario nunca se pisa automáticamente (reporte + decisión humana).
- 3 regression tests nuevos en `InstallerBaselineTests` (14/14) + `installer-baseline.md` (ADR-017).

**Negativas / aceptadas:**

- F-1 (integración): `autoConfirm: !dryRun` evaluado dentro de `if (!dryRun)` hace que el prompt interactivo recrear/preservar (FR-006/FR-007) nunca se dispare en la integración real; `--yes` no llega a `Install()`. Pendiente propagar `yes`/`isHeadless`.
- F-2 (edge): si el usuario borra solo la línea `<!-- hash: … -->` del bloque gestionado, la re-instalación duplica el bloque (viola NFR-002 en ese escenario estrecho). Pendiente tratar `managedBlock != null && hash == null` como Drift.
- F-3 (diseño): el hash canónico se calcula en runtime, no es una constante "embebida" — un template alterado en el repo no se detectaría como source-spoofing. Documentado para trazabilidad; el caso práctico (drift del archivo instalado) sí funciona.
- Los 8 tests pre-existentes de la suite siguen fallando (infra paths + flaky red) — no relacionados con HU-025.

**Riesgos:**

- **Riesgo**: otro installer/herramienta edita el bloque gestionado → mitigado por detección de drift (reporta, no sobrescribe).
- **Riesgo**: TOCTOU por edición concurrente → mitigado por escritura atómica.
- **Riesgo**: symlink attack escribiendo fuera de `~/.config/opencode/` → mitigado por canonicalización + rechazo.

## Status History

| Date | Change |
|------|--------|
| 2026-08-31 | Accepted — patrón de merge no destructivo por secciones gestionadas para AGENTS.md de OpenCode (HU-025). Feature en `in-progress` (PM-1..PM-6 pendientes). |