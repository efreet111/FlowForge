<!--
  Adapted from FlowDoc v2.0 (github.com/crhistianmdz/FlowDocs) — MIT License
  Modified by FlowForge: additions documented in ADR-004.
-->

---
hu_id: HU-041
title: "Paridad entre ide/install.sh y el actualizador C# (flowforge update)"
status: draft
category: hotfix
flowforge_slug: "hu-041-installer-updater-parity"
priority: P1
related_adrs: [ADR-008, ADR-011, ADR-026]
related_hu: [HU-040, HU-038, HU-027]
---

# HU-041 — Paridad entre `ide/install.sh` y `flowforge update` (C#)

> **Tipo:** hotfix de integridad · **Severidad:** P1 · **Origen:** hallazgos de la validación en máquina real de HU-040

## User Story

**As a** desarrollador que trabaja en el working tree de FlowForge con packs IDE instalados,
**I want** que `ide/install.sh`, `flowforge install` y `flowforge update` appliquen **el mismo contenido** a los directorios de cada IDE, y que ninguno degrade lo que ya está funcionando,
**so that** pueda confiar en que cualquier operación de instalación o actualización es segura e idempotente.

---

## Background

Durante la validación de [HU-040](HU-040-vscode-slash-commands-missing.md) en la máquina del desarrollador (`gantz`, Linux + VS Code Flatpak + OpenCode `opencode-go`), se compilaron ambos pathways (`ide/install.sh` y el binario C# `flowforge`) y se ejecutaron con snapshots `sha256` antes/después. El test reveló **4 defectos independientes** que comparten una causa raíz común: la ausencia de un contrato canónico compartido entre ambos installers.

| # | Defecto | Evidencia |
|---|---------|-----------|
| **D1** | `ide/install.sh` L~303 — `sed` aborta con `orden 's' sin terminar` porque `agent-models.json` migró de schema en commit `2637562` y el `jq` no se actualizó. Bloquea el script **antes** del bloque VS Code. | `bash -n` OK pero ejecución real aborta en OpenCode |
| **D2** | `UpdateOrchestrator.GetAgentDirsForIde()` **no incluye `prompts/`** para ningún IDE. `flowforge update` nunca sincroniza los slash commands de HU-040. | Lectura del código, línea ~395 |
| **D3** | `CacheRefresher.RefreshCache()` usa `git clone` (no el working tree). Un dev con commits locales corre `flowforge update` y pierde features: `forge-orchestrator` perdió Decision Extraction (HU-030), `forge-verify` perdió Drift Health Check (HU-031). | 6/40 archivos degradados, verificado por hash |
| **D4** | `FLOWFORGE_PROVIDER` se lee en `install.sh` pero el C# no lo respeta; el shell default `opencode-zen` pisa modelos `opencode-go` (suscripción) y deja `flowforge.md` con `__FLOWFORGE_MODEL__` sin sustituir. | 8/8 agentes OpenCode degradados |

**Causa raíz común**: `install.sh` (bash) y `UpdateOrchestrator.cs` (C#) implementan la instalación de forma **duplicada e independiente**. Ninguno lee el otro. La matriz de rutas vive sólo en [ADR-008](../decisions/ADR-008-ide-installer-path-matrix.md) como documento — ningún código la consume.

---

## Acceptance Criteria (business-level)

- [ ] **AC-1**: `bash ide/install.sh` completa la cadena de instalación sin errores en un sistema limpio (con y sin binario `flowforge` presente).
- [ ] **AC-2**: `ide/install.sh` no aborta ante ninguno de los tres schemas de `agent-models.json` (per-provider, tiers legacy, flat legacy).
- [ ] **AC-3**: Si el schema no se reconoce, el installer **preserva** el `model:` original del template (no deja placeholders sin sustituir).
- [ ] **AC-4**: `FLOWFORGE_PROVIDER` se respeta tanto en el shell installer como en el path C#; el default nunca degrada un provider de suscripción activo.
- [ ] **AC-5**: `flowforge update --component flowforge-skills` sincroniza `prompts/` para VS Code (y el path equivalente para cada IDE que declare comandos).
- [ ] **AC-6**: `flowforge update` detecta drift entre el working tree y la fuente y **aborta con mensaje claro** en vez de degradar contenido.
- [ ] **AC-7**: Existe un único manifest declarativo de destinos (ruta × IDE × asset) consumido por **ambos** installers.
- [ ] **AC-8**: `240/240` tests siguen pasando; se agregan tests para los 4 defectos.

---

## Scenarios (SDD Spec)

### Happy Path

- [ ] **[D1: schema per-provider]**
  **GIVEN** `ide/opencode/config/agent-models.json` con schema `{"agents":{"forge-dev":{"opencode-zen":"big-pickle"}}}`
  **WHEN** corre `bash ide/install.sh`
  **THEN** el installer escribe `model: <provider>/big-pickle` y la cadena continúa hasta el bloque VS Code.
  **🧪 Ref**: `ide/install.sh` bloque OpenCode, T-D2/T-D3

- [ ] **[D1: schema legacy tiers]**
  **GIVEN** el mismo archivo con schema `{"active_tier":"budget","tiers":{"budget":{"agents":{"forge-dev":{"model":"qwen3.7-plus"}}}}}`
  **WHEN** corre `bash ide/install.sh`
  **THEN** el installer resuelve el modelo sin fallar (compatibilidad retroactiva con cachés viejas).
  **🧪 Ref**: observación HU-041, cache `~/.flowforge/cache/FlowForge/` (26-sep)

- [ ] **[D5: update sincroniza prompts]**
  **GIVEN** VS Code instalado con `~/.copilot/agents/` presente y el repo con `ide/vscode/prompts/`
  **WHEN** corre `flowforge update --component flowforge-skills`
  **THEN** los 7 `flow-*.prompt.md` aparecen en el directorio de prompts detectado (nativo o Flatpak).
  **🧪 Ref**: `UpdateOrchestrator.GetAgentDirsForIde()` + `PathHelper.CopilotPromptsNative/Flatpak`

### Edge Cases

- [ ] **[Schema irreconocible]**
  **GIVEN** un `agent-models.json` con schema desconocido
  **WHEN** corre el installer
  **THEN** el `model:` del template queda intacto y se emite un aviso `!` en el log; no se inyecta un objeto multi-línea.
  **🧪 Ref**: guard `case "$model" in ''|*[[:space:]]*|*'{'*...)`

- [ ] **[Provider de suscripción activo]**
  **GIVEN** agentes OpenCode con `model: opencode-go/*` y el repo declara `opencode-zen`
  **WHEN** corre `bash ide/install.sh` **sin** `FLOWFORGE_PROVIDER`
  **THEN** el installer detecta el provider activo desde los archivos instalados y lo preserva, o aborta pidiendo confirmación explícita.
  **🧪 Ref**: observación máquina real 2026-10-20, 8/8 agentes degradados

- [ ] **[Working tree más nuevo que el origin]**
  **GIVEN** el dev tiene commits locales no pusheados que agregan features a los skills
  **WHEN** corre `flowforge update --component flowforge-skills`
  **THEN** el installer detecta que el working tree diverge y **aborta** con: "hay cambios locales; usá `--force-local` o hacé pull primero".
  **🧪 Ref**: D3, pérdida de HU-030/HU-031

### Error Cases

- [ ] **[Placeholder sin sustituir]**
  **GIVEN** un template con `model: __FLOWFORGE_MODEL__` y un schema que no resuelve
  **WHEN** el installer copia el template
  **THEN** el post-condition check detecta el placeholder y falla ruidosamente (no deja el archivo en disco con el placeholder).
  **🧪 Ref**: observado en `flowforge.md` tras el install del 2026-10-20

- [ ] **[Directorio de prompts no escribible]**
  **GIVEN** el directorio de prompts existe pero es read-only
  **WHEN** corre el installer
  **THEN** el backup preflight (T-B7 de HU-040) se ejecuta antes del `cp`, y el error reporta el path exacto.
  **🧪 Ref**: NFR-2 de HU-040

---

## Functional Requirements (FR-)

| ID | Descripción | Verificable por |
|----|-------------|-----------------|
| FR-1 | El installer shell resuelve `model:` con un parser tolerante a ≥3 schemas y valida que el resultado sea un token single-line antes de escribir. | Test con 3 fixtures de schema |
| FR-2 | Un post-condition check falla ruidosamente si algún archivo instalado contiene `__FLOWFORGE_*__` sin sustituir. | `grep -r '__FLOWFORGE_' ~/.config/opencode/agents/` → vacío |
| FR-3 | `GetAgentDirsForIde()` incluye un destino `prompts` por IDE, con el mismo patrón de detección Flatpak que el shell. | `flowforge update` sincroniza los 7 prompts |
| FR-4 | `CacheRefresher` reporta drift entre working tree y origin; aborta si el working tree tiene commits no pusheados que tocan `ide/` o `skills/`. | Test con repo clonado + commit local |
| FR-5 | `FLOWFORGE_PROVIDER` se lee del env en ambos installers; si no está, se infiere de los modelos ya instalados antes de sobreescribir. | Test con modelos `opencode-go/*` instalados |
| FR-6 | Existe un manifest declarativo único (JSON o YAML) con la matriz destino × IDE × asset, consumido por ambos installers. | Lectura del manifest por ambos |

## Non-Functional Requirements (NFR-)

| ID | Tipo | Descripción | Métrica |
|----|------|-------------|---------|
| NFR-1 | Compatibilidad | `install.sh` sigue siendo bash 3.2+ (macOS). | `bash -n` + CI `test-installer.yml` |
| NFR-2 | Atomicidad | Todo write tiene backup preflight + post-condition check. | Backup dir existe tras cada write |
| NFR-3 | Observabilidad | Cada degradación potencial emite un `!` visible, no un warning silencioso. | Log review |
| NFR-4 | Idempotencia | 3 runs consecutivos de cualquiera de los dos installers → mismo hash final. | Snapshot sha256 antes/después |

## Security & STRIDE

| Amenaza | Mitigación |
|---------|------------|
| **T**ampering | `UserModifiedAgentDetector` (FR-006 existente) + backup preflight obligatorio antes de cualquier overwrite |
| **R**epudiation | Cada operación deja log con timestamp + paths + hashes |
| **D**enial of Service | Un fallo en un bloque no debe abortar la cadena completa (best-effort por bloque, como ya hace Kilo/Copilot) |
| **I**nfo Disclosure | El manifest no contiene credenciales; los prompts no llevan paths absolutos |

---

## Manual Tests (PM-*)

> forge-verify NO califica los PM-*. forge-memory exige `[x]` antes del close.

- [ ] **PM-041-1**: `bash ide/install.sh` en sandbox con HOME limpio **completa la cadena completa** (Cursor → OpenCode → VS Code → Antigravity → Kilo) sin abortar. **Ejecutable por humano o CI.**
- [ ] **PM-041-2**: `grep -r '__FLOWFORGE_' ~/.config/opencode/agents/` devuelve **vacío** tras el install (ningún placeholder sin sustituir).
- [ ] **PM-041-3**: Los 8 agentes OpenCode mantienen sus modelos `opencode-go/*` tras correr el installer sin `FLOWFORGE_PROVIDER` (verificar con `grep '^model:'`).
- [ ] **PM-041-4**: `flowforge update --component flowforge-skills --yes` sincroniza los 7 `flow-*.prompt.md` al directorio de prompts de VS Code.
- [ ] **PM-041-5**: Con un commit local no pusheado que toca `skills/`, `flowforge update` **aborta** con mensaje de drift (no degrada).
- [ ] **PM-041-6**: 3 runs consecutivos de `bash ide/install.sh` producen el mismo hash final (idempotencia).
- [ ] **PM-041-7**: `dotnet test` reporta **≥ 240** tests pasando (baseline actual) + los nuevos de esta HU.

---

## ADR candidate

**Sí.** Requiere **ADR-026 — Manifest único de destinos IDE compartido por shell y C#**, que modifica ADR-008 (matriz de rutas) y ADR-011 (paridad VS Code).

---

## Related Refs

- **HU-040** — originador del hallazgo (prompts pack VS Code)
- **HU-038** — precedente: paridad OpenCode commands
- **HU-027** — precedente: path mismatch
- **ADR-008** — matriz canónica de rutas (aeijoextends con el manifest)
- **ADR-011** — VS Code prompts pack
- **`src/FlowForge.Installer/Update/UpdateOrchestrator.cs`** — `GetAgentDirsForIde()` línea ~395
- **`src/FlowForge.Installer/Update/CacheRefresher.cs`** — fuente del downgrade
- **`ide/install.sh`** línea ~303 — el `sed` frágil
- **Engram #68** — observación del downgrade
- **Engram #69** — bug de `engram stats` (relacionado por ser el mismo tipo de defecto: null-safety)

---

## Owner & Timeline

- **Owner**: TBD (installer crew)
- **Target milestone**: v0.4.3
- **Effort**: 2–3 días (FR-1/2/5 en shell ≈ 4h; FR-3/4 en C# ≈ 8h; FR-6 manifest ≈ 4h; tests ≈ 4h)

## Definition of Done

- [ ] FR-1..FR-6 implementados
- [ ] ADR-026 en `docs/decisions/` y aceptado
- [ ] `ide/install.sh` + `ide/install.ps1` + `UpdateOrchestrator.cs` consumen el manifest
- [ ] Tests nuevos cubren D1–D4
- [ ] `dotnet test` ≥ 240 + nuevos, todos verdes
- [ ] PM-041-1..7 verificados
- [ ] forge-verify emite PASS
- [ ] forge-memory cierra con PM-041-* en `[x]`

## Memory Signal

- **Tipo**: `pattern`
- **Título**: "Installer duplicado = drift garantizado. El shell y el C# deben compartir un manifest."
- **Regla**: "Si dos binarios escriben en los mismos paths, la tabla de destinos debe ser **datos**, no código. Un manifest declarativo consumido por ambos elimina la clase entera de bugs (D1–D4 son instancias del mismo patrón)."
- **Tags**: `installer`, `parity`, `drift`, `single-source-of-truth`, `manifest`, `downgrade`