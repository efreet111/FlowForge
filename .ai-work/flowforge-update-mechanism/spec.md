---
capability_matrix:
  ai_reasoning:
    - "UI confirmation prompts: whether to show diff of modified agents before overwrite or auto-backup silently"
    - "Message wording for user-modified agent detection (ES/EN bilingual — see NFR-ERR-001)"
    - "Order of IDE pack updates when user has multiple IDEs (parallel or sequential, and which first)"
    - "Whether health-check failure should auto-rollback or prompt the user for confirmation"
    - "Cache git refresh strategy: `git pull` (fast, carries forward any corrupted cache) vs. `git clone --fresh` (safe but slow)"
  deterministic:
    - "~/.engram/engram.db, -wal, -shm, and local_memory/ MUST never be touched by any update operation (write-protected at the filesystem level in code)"
    - "ConfigStore.Update MUST remain atomic: write to .tmp, then File.Move (no partial writes)"
    - "Binary swap MUST follow the sequence: backup old → download new → health-check new → atomically move new to target → update version. Any failure before the move step triggers rollback to backup."
    - "MCP config merge for Cursor and Antigravity MUST use read-merge-write (never overwrite entire file) — same pattern as OpenCode merge via JsonNode surgical replacement"
    - "SHA-256 verification for all binary downloads is MANDATORY (hard error if checksum fails, no best-effort fallback)"
    - "All download operations MUST have a configurable timeout (env: FLOWFORGE_API_TIMEOUT_SECONDS, default 30s) and MUST NOT proceed on timeout (hard stop)"
    - "Component compatibility MUST be validated via manifest.yaml `requires.*` before any component update proceeds"
    - "Backup retention: maximum 5 backups per component, oldest pruned on new backup"
---

# Spec: Update Mechanism por Componente (`flowforge update --component`)

> **Context map**: `.ai-work/flowforge-update-mechanism/context-map.md` — 319 líneas de discovery de forge-discovery (Fase 0)
>
> HU source: none (backlog `docs/backlog/NS-*.md`; PRD `docs/PRD.md` v1.0.0-draft)

---

## 0. Executive Summary

**Objective**: Extender `flowforge update` de un swap in-place solo para engram a un mecanismo de actualización granular por componente (engram-dotnet, FlowForge skills/agents por IDE, flowdoc, installer) que preserva configuración del usuario, tiene tolerancia a fallo con rollback, y expone trazabilidad completa.

**Scope**:
- In: Update granular por componente (`--component`), backup antes de swap binario con rollback automático, merge quirúrgico de MCP configs (Cursor + Antigravity) preservando servers del usuario, refresh de cache git, health-check post-update, version tracking por componente, detección de agentes modificados por el usuario con opción de backup/omitir, clasificación managed vs user con sidecar generalizado a todos los destinos IDE
- Out: Actualización de flowdoc por proyecto (es `flowforge init` por proyecto, no update global), migración de `engram.db` entre versiones de esquema (es dominio de engram-dotnet), update de dependencias de sistema (git, curl, sqlite3)

**Functional Requirements**:
| ID | Name | Description |
|----|------|-------------|
| FR-001 | Granular component selection | `--component` flag: engram, flowforge-skills, flowdoc, installer, all |
| FR-002 | Backup + rollback for binary swap | Backup old binary before download; health-check new binary; auto-restore on failure |
| FR-003 | MCP config merge (Cursor & Antigravity) | Surgical merge of engram MCP entry into existing mcp.json/mcp_config.json preserving user servers |
| FR-004 | Cache git refresh | `git pull` on `~/.flowforge/cache/FlowForge` before copying skills/agents |
| FR-005 | Health-check post-update | Run `engram --version`, validate MCP config parse, run `flowforge doctor` subset |
| FR-006 | User-modified agent detection | Compare SHA-256 of installed agents vs. new versions; offer skip/backup/overwrite |
| FR-007 | Idempotent update | Re-running update with same target version is a no-op; no duplicates, no corruption |
| FR-008 | Version tracking per component | `config.json` tracks version per component; `flowforge status` shows all |
| FR-009 | Self-update of flowforge binary | `--self` flag downloads new flowforge binary, swaps with backup + rollback |
| FR-010 | Skills/agents update by IDE | Update FlowForge packs for Cursor, OpenCode, Antigravity, VS Code Copilot, Kilo per ADR-008 matrix |
| FR-011 | Managed-vs-user sidecar generalization | Extend OpenCode `managed-paths.json` pattern to Cursor, Antigravity, VS Code, Kilo |
| FR-012 | Pre-update engram process check | Detect running engram processes before binary swap; warn or abort |

**Key Decisions**:
1. Update orchestration via a new `UpdateOrchestrator` class composing existing modules (80% reuse of `EngramModule.UpdateAsync`, `ConfigStore`, `ManifestClient`, `FlowForgeModule`)
2. MCP merge uses the OpenCode pattern (`JsonNode` surgical replacement) generalized to Cursor/Antigravity — eliminates the 2 data-loss bugs (S6)
3. Backup directory `~/.flowforge-backups/{component}-{timestamp}` with retention capped at 5 per component

**Risks**:
| Risk | Mitigation |
|------|-----------|
| Manifest without signature (S1) | Accepted trade-off from ADR-002; update adds compatibility cross-check across ALL installed components |
| Rollback untested (O5) | Spec mandates PM-2 (binary rollback), PM-3 (MCP merge integrity), and unit tests for UpdateOrchestrator |
| Cache git stale (O1) | `git pull` before every skills update; if pull fails, fallback to clone fresh |

---

## 1. Objective and Scope

### Problem Statement

El comando `flowforge update` (v0.1.0-alpha.6) solo actualiza el binario `engram` — lo hace con swap in-place sin backup, sin health-check, y sin granularidad. Los packs de FlowForge (skills, agents, rules, commands para IDEs) solo se refrescan re-ejecutando `flowforge install`, que sobrescribe configuraciones críticas del usuario: los `mcp.json` de Cursor y `mcp_config.json` de Antigravity pierden todos los servers MCP ajenos a FlowForge (2 bugs de pérdida de datos). El cache git (`~/.flowforge/cache/FlowForge`) se clona `--depth 1` una vez y nunca se refresca. Las versiones hardcodeadas (`InstallerVersion = "0.1.0-alpha.6"`) están en drift con el manifest real (`0.1.0-alpha.7`). Sin tests de update. Sin rollback.

### Objective

Transformar `flowforge update` en un mecanismo de actualización orquestada por componente que:
- Permita seleccionar qué actualizar (`--component`)
- Preserve TODA la configuración del usuario (MCP, agentes modificados, sync URL, channel)
- Tenga tolerancia a fallo (backup → swap → health-check → rollback si falla)
- Sea trazable (logging, version tracking, hashes pre/post)
- Sea idempotente

### Out of Scope (v1)

- Auto-update periódico (cron/systemd timer) — es conveniencia post-MVP
- Rollback de agentes modificados por el usuario (solo backup + detección, no auto-merge 3-way)
- Update de flowdoc por proyecto (`flowforge init` es el mecanismo, no `update`)
- Firmado criptográfico del manifest (diferido desde ADR-002 — no empeorar la superficie)
- Migración de esquema de `engram.db` entre versiones (dominio de engram-dotnet, no del installer)

---

## 2. Functional Requirements (FR)

### FR-001: Granular Component Selection

**Description**: `flowforge update` acepta un flag `--component <name>` para seleccionar qué actualizar. Sin flag, muestra el estado de todos e invita al usuario a elegir.

- **Scenario A — Happy path (single component)**
  - **Given** el usuario tiene instalado engram v0.3.0 y FlowForge skills v0.1.0-alpha.6
  - **When** ejecuta `flowforge update --component engram`
  - **Then** solo se actualiza engram-dotnet a la última versión compatible; los skills/agents NO se tocan; `config.json` refleja la nueva versión de engram

- **Scenario B — All components**
  - **Given** múltiples componentes tienen updates disponibles
  - **When** ejecuta `flowforge update --component all` (o `--yes` sin `--component`)
  - **Then** se actualizan en orden topológico: engram-dotnet → MCP configs (merge) → FlowForge skills por IDE → flowdoc → installer; si un componente falla, los siguientes no se ejecutan y se reporta el error

### FR-002: Backup Before Binary Swap + Rollback

**Description**: Antes de sobrescribir cualquier binario (engram o flowforge), se crea una copia de backup con timestamp. Tras descargar el nuevo binario, se ejecuta health-check. Si falla, se restaura el backup automáticamente.

- **Scenario A — Successful update with health-check**
  - **Given** engram v0.3.0 está instalado en `~/.local/bin/engram`
  - **When** se ejecuta update a v0.4.0
  - **Then** el binario v0.3.0 se copia a `~/.flowforge-backups/engram-{timestamp}/engram`; se descarga v0.4.0 a un path temporal; se ejecuta `engram --version` contra el binario temporal (sin sobreescribir el original); si devuelve `v0.4.0`, se mueve atómicamente a `~/.local/bin/engram`; `config.json` se actualiza a v0.4.0

- **Scenario B — Failed health-check triggers rollback**
  - **Given** engram v0.3.0 está instalado y se descarga v0.4.0
  - **When** el health-check del binario temporal falla (exit code != 0 o versión no coincide)
  - **Then** el binario temporal se elimina; `config.json` NO se modifica; el backup NO se restaura (el original sigue en su lugar); se muestra error: `[red]✗ Health-check falló — engram v0.3.0 preservado[/]`

- **Scenario C — Download failure during update**
  - **Given** se inicia update de engram
  - **When** la descarga falla (timeout, 404, checksum mismatch)
  - **Then** el binario original permanece intacto; no se toca `config.json`; se limpia cualquier archivo temporal; se muestra error específico

### FR-003: MCP Config Merge (Cursor & Antigravity)

**Description**: Los `mcp.json` de Cursor y `mcp_config.json` de Antigravity se actualizan con merge quirúrgico (agregar/quitar entrada `engram`, preservar el resto del JSON). Mismo patrón que `MergeOpenCodeMcp` en `EngramModule.cs`.

- **Scenario A — First MCP entry (both IDEs)**
  - **Given** Cursor tiene un `mcp.json` con servers existentes: `{"mcpServers": {"my-tool": {...}}}`
  - **When** se ejecuta `flowforge update --component engram`
  - **Then** el `mcp.json` resultante contiene `"my-tool"` intacto Y `"engram"` agregado con path al nuevo binario; Antigravity recibe el mismo tratamiento en `mcp_config.json`

- **Scenario B — Update existing MCP entry**
  - **Given** Cursor ya tiene `"engram"` en `mcpServers` apuntando a `engram` v0.3.0
  - **When** engram se actualiza a v0.4.0 (mismo path de binario, solo cambia el binario)
  - **Then** la entrada `"engram"` en `mcp.json` se mantiene intacta (apunta al mismo path); NO se tocan otros servers; el merge detecta que el path del comando no cambió y es no-op para MCP configs

### FR-004: Cache Git Refresh

**Description**: Antes de copiar skills/agents desde el repo FlowForge, se ejecuta `git pull` en `~/.flowforge/cache/FlowForge`. Si el pull falla (sin conexión, repo corrupto), se intenta clone fresh.

- **Scenario A — Cache exists and is updatable**
  - **Given** `~/.flowforge/cache/FlowForge` existe (clone previo)
  - **When** se ejecuta `flowforge update --component flowforge-skills`
  - **Then** se ejecuta `git pull` en el cache; si tiene éxito, se usan los archivos actualizados para copiar skills/agents

- **Scenario B — Stale or corrupt cache**
  - **Given** el cache existe pero `git pull` falla (network error o repo corrupto)
  - **When** se ejecuta update de skills
  - **Then** se borra el cache y se hace `git clone --depth 1` fresh; si el clone también falla, el update ABORTA con error claro (no usa cache stale)

### FR-005: Health-Check Post-Update

**Description**: Después de actualizar cualquier componente, se ejecuta una verificación mínima para confirmar que el sistema está en estado consistente.

- **Scenario A — Binary health-check**
  - **Given** se acaba de hacer swap del binario engram
  - **When** el health-check se ejecuta
  - **Then** se verifica: (1) `engram --version` devuelve la versión esperada y exit code 0, (2) el binario tiene permisos de ejecución, (3) `flowforge doctor --strict` reporta MCP configurado y binario presente

- **Scenario B — MCP config parse check**
  - **Given** se acaba de hacer merge de MCP configs
  - **When** el health-check valida las configs
  - **Then** `mcp.json` de Cursor y `mcp_config.json` de Antigravity parsean como JSON válido y contienen la entrada `engram` con el comando apuntando al binario correcto

### FR-006: User-Modified Agent Detection

**Description**: Antes de sobrescribir agentes/rules en los destinos IDE, se compara el SHA-256 del archivo instalado contra el SHA-256 del archivo fuente en el repo. Si difieren, el usuario modificó el agente y se le ofrecen opciones.

- **Scenario A — Unmodified agent (fast path)**
  - **Given** el archivo `~/.cursor/agents/forge-arch.md` tiene SHA-256 idéntico a `ide/cursor/agents/forge-arch.md` en el repo
  - **When** se ejecuta update de skills
  - **Then** el archivo se sobrescribe silenciosamente con la nueva versión del repo

- **Scenario B — User-modified agent detected**
  - **Given** el usuario modificó `~/.cursor/agents/forge-arch.md` (SHA-256 difiere del repo)
  - **When** se ejecuta update y se detecta la divergencia
  - **Then** se muestra: `[yellow]⚠ forge-arch.md fue modificado[/]` con opciones: `[S]kip / [B]ackup + overwrite / [O]verwrite sin backup`; si el usuario eligió `--yes`, el default es `Backup + overwrite`

### FR-007: Idempotent Update

**Description**: Ejecutar `flowforge update` múltiples veces con la misma versión objetivo no produce efectos secundarios.

- **Scenario A — Already at latest**
  - **Given** engram v0.4.0 instalado y es la última versión disponible
  - **When** ejecuta `flowforge update --component engram`
  - **Then** muestra `[green]✓[/] engram-dotnet v0.4.0 (ya es la última versión)` y no descarga nada

- **Scenario B — Partial re-run after interruption**
  - **Given** un update previo fue interrumpido después de swap binario pero antes de update de skills
  - **When** ejecuta `flowforge update --component all`
  - **Then** engram detecta que ya está en la versión target (no-op); skills se actualizan normalmente; no hay duplicados ni archivos corruptos

### FR-008: Version Tracking Per Component

**Description**: `~/.engram/config.json` registra la versión instalada de cada componente individualmente.

- **Scenario A — Initial state**
  - **Given** instalación fresh con engram v0.3.0 y FlowForge v0.1.0-alpha.7
  - **When** se consulta `flowforge status`
  - **Then** muestra tabla con `engram-dotnet: v0.3.0`, `flowforge-skills: v0.1.0-alpha.7`, `flowdoc: (no instalado)`, `installer: v0.1.0-alpha.7`

- **Scenario B — After selective update**
  - **Given** se actualiza solo engram a v0.4.0
  - **When** se consulta `flowforge status`
  - **Then** `engram-dotnet` muestra v0.4.0; `flowforge-skills` mantiene v0.1.0-alpha.7; `config.json` tiene entradas separadas por componente

### FR-009: Self-Update of FlowForge Binary

**Description**: `flowforge update --self` descarga y reemplaza el propio binario `flowforge` (ubicado en `~/.local/bin/flowforge`). Usa el mismo mecanismo de backup + health-check + rollback que FR-002.

> **Nota**: Este FR es condicional. Ver OQ-1 `[BLOCKER]` en §5.

- **Scenario A — Binary self-replace**
  - **Given** flowforge v0.1.0-alpha.6 instalado y v0.2.0 disponible en GitHub Releases
  - **When** ejecuta `flowforge update --self`
  - **Then** se hace backup del binario actual; se descarga v0.2.0; se ejecuta `flowforge --version` contra el binario temporal; si OK, se reemplaza atómicamente; el propio proceso de update sobrevive porque el binario ya está cargado en memoria en Linux/macOS (exec self-replace vía `mv` atómico)

### FR-010: Skills/Agents Update by IDE

**Description**: `--component flowforge-skills` actualiza los packs de FlowForge para los IDEs detectados en el sistema. Soporta los 5 IDEs de la matriz ADR-008: Cursor, OpenCode, Antigravity, VS Code Copilot, Kilo.

- **Scenario A — Single IDE update**
  - **Given** solo Cursor está instalado
  - **When** ejecuta `flowforge update --component flowforge-skills`
  - **Then** se refresca el cache git; se copian `ide/cursor/rules/*.mdc`, `ide/cursor/agents/forge-*.md`, `ide/cursor/commands/*.md`; se aplica FR-006 para agentes modificados; OpenCode/Antigravity/VS Code/Kilo NO se tocan

- **Scenario B — All IDEs detectadas**
  - **Given** Cursor, OpenCode y Antigravity instalados
  - **When** ejecuta `flowforge update --component flowforge-skills`
  - **Then** los tres IDEs reciben sus packs actualizados; cada uno con su propia lógica de merge/config (OpenCode con sidecar, Antigravity con `mcp_config.json` merge, Cursor con copia directa + MCP merge)

### FR-011: Managed-vs-User Sidecar Generalization

**Description**: El patrón `managed-paths.json` (sidecar que declara qué paths son gestionados por FlowForge) se generaliza a todos los destinos IDE, no solo OpenCode.

- **Scenario A — New IDE integration**
  - **Given** se instala/actualiza FlowForge en un IDE
  - **When** el sistema escribe archivos en `~/.cursor/` o `~/.gemini/config/`
  - **Then** se crea/actualiza un sidecar `~/.cursor/.flowforge-managed.json` o `~/.gemini/config/.flowforge-managed.json` listando los paths gestionados; futuros updates usan este sidecar para decidir si sobrescribir o merge

- **Scenario B — User adds custom agents**
  - **Given** el sidecar de Cursor lista `agents/forge-*.md` como managed
  - **When** el usuario crea `~/.cursor/agents/my-custom-agent.md`
  - **Then** el update NO toca `my-custom-agent.md` (no está en el sidecar); el uninstall puede limpiar solo los managed paths

### FR-012: Pre-Update Engram Process Check

**Description**: Antes de hacer swap del binario engram, se verifica que no haya procesos `engram` corriendo (MCP server activo). Si los hay, se advierte o se aborta.

- **Scenario A — Engram running**
  - **Given** hay un proceso `engram mcp` corriendo (usado por Cursor/OpenCode)
  - **When** se intenta `flowforge update --component engram`
  - **Then** se muestra: `[yellow]⚠ engram está corriendo (PID 12345). Cerrá tu IDE antes de continuar.[/]`; el update ABORTA a menos que el usuario pase `--force`

- **Scenario B — Engram not running**
  - **Given** no hay procesos `engram` activos
  - **When** se ejecuta update de engram
  - **Then** el update procede sin advertencia

---

## 3. Non-Functional Requirements (NFR)

### 3.1 Performance & Reliability

- **NFR-PERF-001**: Download timeout configurable vía `FLOWFORGE_API_TIMEOUT_SECONDS` (default 30s, mismo que el installer actual). Hard stop en timeout — no continuar con datos parciales.
- **NFR-PERF-002**: `git pull` en el cache no debe exceder 10s en condiciones normales de red. Si el repo está corrupto, pasar a clone fresh (hasta 60s).
- **NFR-REL-001**: Atomic writes para TODOS los archivos de configuración (`ConfigStore.Save` — patrón `.tmp → rename`).
- **NFR-REL-002**: Backup retention: máximo 5 backups por componente en `~/.flowforge-backups/`. El más antiguo se elimina al crear el sexto.
- **NFR-REL-003**: Todo update debe ser reanudable: si falla a mitad de camino, re-ejecutar no corrompe ni duplica (ver FR-007).
- **NFR-REL-004**: `engram.db`, `-wal`, `-shm`, y `~/.engram/local_memory/` son read-only para el updater. El código NO debe tener paths que escriban en estos directorios.

### 3.2 Logging & Audit Trail

- **NFR-LOG-001**: Cada operación de update genera entrada en `~/.engram/install.log` con: timestamp, componente, versión anterior, versión nueva, SHA-256 pre y post del binario, resultado (success/failure/rollback).
- **NFR-LOG-002**: Los diffs de MCP config (antes/después del merge) se loguean a nivel debug.
- **NFR-LOG-003**: Errores de red (timeout, DNS, 403/404) se loguean con detalle suficiente para diagnóstico sin exponer tokens.

### 3.3 Compatibility

- **NFR-COMP-001**: Compatibilidad entre componentes evaluada vía `manifest.yaml` (`requires.engram-dotnet`, `requires.installer`). Un update de engram que incumpla `requires.installer` se rechaza con mensaje claro.
- **NFR-COMP-002**: La verificación de compatibilidad se hace contra el CONJUNTO completo de componentes instalados (no solo el que se está actualizando).
- **NFR-COMP-003**: Releases de GitHub sin assets binarios (tipo incidente engram v1.3.0) se saltean automáticamente (`GetLatestVersionAsync` ya implementa esto).

### 3.4 Bilingual Messaging

- **NFR-ERR-001** (carry-over, sin resolver): Mensajes de usuario en ES/EN. Mantener consistencia con el patrón actual del installer (mayoritariamente ES con términos técnicos en EN). Las tablas de estado (`flowforge status`) usan EN para nombres de componentes, ES para descripciones.

---

## 4. Security Requirements (STRIDE Analysis)

### Threat Model

> **Surface**: GitHub Releases API (HTTPS), GitHub git clone (HTTPS), `manifest.yaml` remoto (HTTPS, sin firma), sistema de archivos local (`~/.engram/`, `~/.flowforge-backups/`, destinos IDE).

| Threat | Finding | RNF |
|--------|---------|-----|
| **S**poofing | Manifest no firmado (ADR-002 trade-off). Un MITM que sirva un `manifest.yaml` malicioso puede bajar constraints de compatibilidad. | **RNF-SEC-001**: El update debe validar que la versión descargada de GitHub Releases coincide con la versión reportada por el manifest. Si hay discrepancia, warning explícito y confirmación del usuario. |
| **T**ampering | Binarios descargados vía HTTPS, pero el SHA-256 es best-effort en bootstrap. Un release comprometido en GitHub serviría binarios maliciosos. | **RNF-SEC-002**: SHA-256 verification es MANDATORY para TODOS los binarios descargados. Si el checksum no coincide, el binario se descarta y el update ABORTA. No continuar con "best-effort". |
| **T**ampering | `git clone --depth 1` clona `main` sin verificación de commit/tag. Un force-push malicioso a `main` serviría agentes comprometidos. | **RNF-SEC-003**: El update de skills debe soportar pin por tag (`--tag v0.2.0`). Por defecto usa `main`, pero advierte si `main` cambió de HEAD desde el último update (compara commit hash). |
| **R**epudiation | Sin log estructurado, un usuario no puede probar qué versión instaló ni cuándo. | **RNF-SEC-004**: `install.log` registra timestamp, actor (usuario OS), versión anterior, versión nueva, SHA-256, y resultado para CADA operación de update. |
| **I**nformation Disclosure | `~/.engram/` contiene memorias del usuario (posiblemente PII). Un update no debe leerlas ni transmitirlas. | **RNF-SEC-005**: El código del updater NO lee archivos dentro de `~/.engram/engram.db` ni `~/.engram/local_memory/`. Si `PiiScanner` detecta PII en configs generadas (MCP env vars), se limpia antes de escribir. |
| **I**nformation Disclosure | Los MCP configs del usuario pueden contener API keys de otros servers. | **RNF-SEC-006**: El merge de MCP configs (FR-003) solo toca la entrada `engram`. Todo otro contenido del JSON se preserva byte a byte. Se loguea diff del merge a nivel debug SIN incluir valores de variables de entorno. |
| **D**enial of Service | Descargas de binarios grandes (~100 MB) pueden saturar ancho de banda en entornos restringidos. | **RNF-SEC-007**: Timeout configurable; sin reintentos infinitos (máx 2 reintentos con exponential backoff 2s/4s). |
| **E**levation of Privilege | El installer corre con los permisos del usuario. Si el binario descargado tiene setuid o capacidades inesperadas, escala privilegios. | **RNF-SEC-008**: Después de copiar el binario, verificar que los permisos son `755` (no setuid, no setgid). Si tiene permisos inesperados, warning y `chmod 755`. |

### Mandatory Security RNFs for AOT Binary

- **RNF-SEC-AOT-001**: No usar reflexión dinámica (AOT-safe). System.Text.Json con source-gen (`InstallerJsonContext`, `McpJsonContext`).
- **RNF-SEC-AOT-002**: No hardcodear tokens, API keys, ni URLs internas en el binario AOT (el manifest URL ya es configurable vía `FLOWFORGE_MANIFEST_URL`).
- **RNF-SEC-AOT-003**: `PiiScanner.EnsureClean` se ejecuta sobre todo archivo de configuración generado antes de escribirlo a disco.

---

## 5. Developer Manual Tests (PM-*)

| ID | Case / flow | Steps (summary) | Expected result | [x] |
|----|-------------|-----------------|-----------------|-----|
| PM-1 | **Happy path: update all components** | 1. Instalar FlowForge fresh con `flowforge install --yes`<br>2. Publicar un release nuevo de engram-dotnet (simulado con tag local o release de prueba)<br>3. Ejecutar `flowforge update --component all --yes`<br>4. Verificar `flowforge status` | Todos los componentes muestran la nueva versión. `engram --version` devuelve versión actualizada. MCP configs de Cursor/Antigravity preservan servers existentes. `install.log` tiene entradas para cada componente. | [x] |
| PM-2 | **Rollback: binario roto** | 1. Instalar engram v0.3.0<br>2. Simular un release "roto": modificar temporalmente `GitHubReleasesClient` para que descargue un binario que devuelve exit code 1 en health-check<br>3. Ejecutar `flowforge update --component engram`<br>4. Verificar `engram --version` y `config.json` | El update falla con mensaje de health-check. `engram --version` sigue devolviendo v0.3.0. `config.json` NO fue modificado. El backup en `~/.flowforge-backups/engram-*/` contiene el binario v0.3.0. | [~] deferido (2026-08-12) |

> **PM-2 — DIFERIDO como deuda técnica menor** (decisión del desarrollador humano en `/flow-close`, 2026-08-12): requiere una simulación compleja (release "roto" con health-check exit code 1). Riesgo bajo porque la lógica de rollback está cubierta por tests unitarios (`BackupManagerTests`, `UpdateOrchestratorTests`) y el flujo fallido de health-check tiene cobertura. No bloquea CKP-4. Ver `summary.md` §PM-2 (Deferred).
| PM-3 | **MCP merge con servers existentes** | 1. Agregar manualmente un server MCP falso en `~/.cursor/mcp.json`: `{"mcpServers": {"fake-server": {"command": "echo", "args": ["hello"]}}}`<br>2. Ejecutar `flowforge update --component engram`<br>3. Inspeccionar `~/.cursor/mcp.json` y `~/.gemini/config/mcp_config.json` | Ambos archivos contienen `fake-server` INTACTO y `engram` agregado/actualizado. El orden de las keys puede variar pero el contenido de `fake-server` es idéntico. | [x] |
| PM-4 | **Detección de agente modificado** | 1. Modificar `~/.cursor/agents/forge-arch.md` (agregar una línea)<br>2. Ejecutar `flowforge update --component flowforge-skills` (sin `--yes`)<br>3. Observar prompt interactivo | El sistema detecta la modificación y ofrece las 3 opciones (Skip/Backup+overwrite/Overwrite). Elegir Backup+overwrite: el archivo modificado se copia a `~/.flowforge-backups/` y se instala la versión nueva. | [x] |
| PM-5 | **Cache git stale / refresh** | 1. Instalar FlowForge normalmente (crea cache `~/.flowforge/cache/FlowForge`)<br>2. Hacer un cambio en el repo remoto (nuevo agente en `ide/cursor/agents/`)<br>3. Esperar > 1 hora (o forzar timestamp del cache)<br>4. Ejecutar `flowforge update --component flowforge-skills`<br>5. Verificar que el nuevo agente aparece en `~/.cursor/agents/` | `git pull` se ejecuta en el cache (visible en output). El nuevo agente del repo remoto se instala localmente. | [x] |

---

## 6. Open Questions for Human (OQ-*) — **RESUELTAS**

| ID | Decisión | Racional |
|----|----------|----------|
| OQ-1 | **OUT (diferido)** — `flowforge update --self` NO se implementa en v1 | El bootstrap `curl \| bash` sigue siendo el camino de update del installer. Simplifica el diseño (no hay que manejar auto-reemplazo cross-platform, especialmente Windows). Actualizaciones del installer son raras y el bootstrap ya funciona. |
| OQ-2 | **Desde `main`** con `git pull` | Los packs FlowForge vienen de `main`. Más simple operacionalmente, y el repo es controlado por el equipo. Tags pueden ser post-MVP si se necesita más estabilidad. |
| OQ-3 | **Generalizar a todos los destinos IDE** (Cursor, Antigravity, VS Code Copilot; Kilo hereda de OpenCode) | Consistencia en todos los destinos. |
| OQ-4 | **`--yes` → backup+overwrite automático** (no destructivo) | El usuario puede forzar overwrite sin backup con `--force`. |
| OQ-5 | **Channel global** (mismo que `config.json.channel`) para v1 | Channels por componente es post-MVP. |

---

## 6.1. Installer Protection Policy (CRITERIO AGREGADO)

**Contexto**: El installer actual (`src/FlowForge.Installer/`) ha sufrido regresiones en features anteriores. Este feature **NO debe romper funcionalidad existente**.

### Requisitos de protección

1. **"Pintura" del instalador actual** (baseline):
   - Documentar el estado actual de todos los comandos: `install`, `update`, `uninstall`, `config`, `status`, `doctor`, `init`
   - Registrar comportamiento esperado de cada comando (flags, outputs, side effects)
   - Este documento sirve como referencia para verificar no-regresión durante la implementación

2. **Tests de regresión obligatorios**:
   - Antes de implementar el nuevo mecanismo de update, validar que el installer actual funciona correctamente
   - Tests mínimos: `flowforge install --yes` (fresh install), `flowforge status`, `flowforge doctor`, `flowforge uninstall`
   - Si algún test falla, **ABORTAR** la implementación del feature y resolver la regresión primero

3. **Política de no-regresión**:
   - Ningún cambio a `src/FlowForge.Installer/*` puede romper funcionalidad existente sin aprobación explícita
   - El nuevo `UpdateOrchestrator` debe **componer** con los módulos existentes (`EngramModule`, `FlowForgeModule`, `OpenCodeConfigGenerator`), no reemplazarlos
   - Los comandos existentes (`install`, `uninstall`, etc.) deben seguir funcionando exactamente como antes

4. **Validación post-implementación**:
   - Después de implementar el feature, re-ejecutar los tests de regresión
   - Validar que `flowforge install` (el comando original) sigue funcionando igual
   - Validar que `flowforge update --component all` no interfiere con `flowforge install`

### Artefacto requerido

Generar `.ai-work/flowforge-update-mechanism/installer-baseline.md` con:
- Lista de comandos y flags actuales
- Comportamiento esperado de cada comando
- Tests de regresión a ejecutar antes/después de la implementación
- Referencia a ADRs relevantes (ADR-001, ADR-002, ADR-008, ADR-010)

---

## 7. Architecture Decision Records (ADR) — Impact Assessment

| ADR | Impact on this feature |
|-----|----------------------|
| **ADR-002** (Manifest remoto sin firma) | Aceptado. La superficie de ataque crece con update multi-componente. Mitigación: compatibilidad cross-component validada en cada update; SHA-256 mandatorio en binarios. |
| **ADR-008** (Matriz de rutas IDE) | Es la fuente de verdad para FR-010 (skills/agents por IDE). Los paths de instalación son los mismos que `flowforge install`. |
| **ADR-010** (Persistencia sync URL) | El update preserva `config.json.sync.mode` y `config.json.user`. No debe resetearlos. |
| **ADR-011** (ide-pack-parity) | Menciona `flowforge skills update` como post-MVP. Este feature lo implementa. |
| **ADR-012** (Model config por IDE) | El update de OpenCode debe preservar la config de modelos del usuario (provider, model asignments). El sidecar `managed-paths.json` cubre esto. |

---

## Memory Signal

- type: decision
- significance: high
- summary: "Update mechanism por componente con merge quirúrgico de MCP configs, backup/rollback, y sidecar managed-paths generalizado — basado en ~80% de código reusable del installer existente"
- topics: [installer, update-mechanism, mcp-merge, rollback, sidecar, managed-vs-user, component-granularity]
