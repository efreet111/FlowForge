# Context Map — flowforge-update-mechanism

Fecha: 2026-08-10
Feature-slug: `flowforge-update-mechanism`
Agente: forge-discovery (Fase 0)
Status: **CLEAR** — contexto suficiente para forge-arch (CKP-1)

---

## 1) Resumen ejecutivo

FlowForge + Engram se instalan como un stack completo vía el binario AOT `flowforge`
(`src/FlowForge.Installer/`) orquestado por los bootstraps `install/install.sh` /
`install/install.ps1`. **Hoy no existe un mecanismo de actualización por componente**:

- `flowforge update` solo actualiza el **binario engram-dotnet** (swap in-place, sin backup ni rollback).
- **No existe camino de update para FlowForge skills/agents** (los packs IDE solo se
  refrescan re-ejecutando `flowforge install`, que re-copia TODO con overwrite).
- El re-install **sobrescribe configuraciones** del usuario en varios IDEs (Cursor y
  Antigravity pierden `mcp.json`/`mcp_config.json` completos) y **re-copia agentes**
  modificados por el usuario sin merge ni resolución de conflictos.
- La data de memoria de engram (`~/.engram/engram.db` + `local_memory/`) **sí** está
  a salvo hoy (el installer no la toca) — es el activo crítico a proteger.

Objetivo del feature: actualizar componentes individuales (engram-dotnet, FlowForge
skills por IDE, flowdoc, y el propio installer), preservando config de usuario,
con tolerancia a fallo (rollback) y granularidad por componente.

---

## 2) Mapa de componentes instalables (qué se instala y dónde)

### 2.1 Componentes declarados en `install/manifest.yaml`

| Componente | Repo | Default | Qué instala |
|-----------|------|---------|-------------|
| `engram-dotnet` | `efreet111/engram-dotnet` | true | Binario `engram` + lib nativa SQLite + MCP en IDEs + `~/.engram/` |
| `flowforge` | `efreet111/FlowForge` | true | Skills + agents para IDEs + paridad compartida |
| `flowdoc` | (via `flowforge init`) | false | Scaffold `docs/` + `AGENTS.md` por proyecto |

Compatibilidad: `requires.engram-dotnet: ">=0.3.0"`, `requires.installer: ">=0.1.0-alpha.1"`.
El manifest se descarga en runtime con timeout de 5s y degrada a defaults offline (ADR-002 / ManifestClient).

### 2.2 Artefactos del mecanismo de instalación

| Artefacto | Rol |
|-----------|-----|
| `install/install.sh`, `install/install.ps1` | Bootstrap: detecta OS/arch, descarga binario `flowforge` desde GitHub Releases, verifica SHA-256 (best-effort), instala a `~/.local/bin/flowforge`, ejecuta `flowforge install` |
| `install/manifest.yaml` | Manifest remoto de compatibilidad (channels, requires, components) |
| `src/FlowForge.Installer/` (C# .NET AOT) | El binario `flowforge`: comandos `install`, `update`, `uninstall`, `config`, `status`, `doctor`, `init` |
| `ide/install.sh` + `ide/install.ps1` | Instaladores shell paralelos (global + por proyecto) — **duplican** lógica del C# (riesgo de drift, ya documentado en `fix-opencode-installer-config-gen`) |
| `flow-init.sh` | Init de proyecto (legacy, 183 líneas) |
| `install-skills.sh` | Instalador de skills (stale: referencias EngramFlow, `.cursorrules` — ADR-011) |

### 2.3 Rutas globales escritas por el installer (ADR-008 matrix)

| Destino global | Contenido | Escritura |
|----------------|-----------|-----------|
| `~/.local/bin/flowforge`, `~/.local/bin/engram`, `~/.local/bin/libe_sqlite3.so` | Binarios | Overwrite |
| `~/.engram/config.json` | Config del stack (channel, sync, components) | Atomic RMW (ConfigStore) |
| `~/.engram/engram.db` (+ `-wal`/`-shm`), `~/.engram/local_memory/`, `~/.engram/install.log` | **Data de memoria del usuario** | **NO la toca el installer** ✅ |
| `~/.cursor/` — `rules/*.mdc`, `agents/forge-*.md`, `commands/*.md`, `mcp.json` | Pack Cursor | Copy overwrite; **mcp.json overwrite completo ⚠️** |
| `~/.config/opencode/` — `opencode.json(c)`, `agents/*.md`, `commands/*.md`, `.agents/rules/model-assignments.md`, `.flowforge-managed.json` (sidecar) | Pack OpenCode | Merge quirúrgico (managed-paths) ✅ |
| `~/.copilot/agents/*.agent.md` + `instructions/flowforge.instructions.md` | Pack VS Code Copilot | Copy overwrite |
| `~/.config/kilo/agents/*.md` | Pack Kilo (duplica opencode agents) | Copy overwrite |
| `~/.gemini/config/` — `AGENTS.md`, `rules/`, `global_workflows/`, `skills/`, `.agents/`, `mcp_config.json` | Pack Antigravity | Copy overwrite; **mcp_config.json overwrite completo ⚠️** |
| `~/.flowforge/shared/workflow-orchestrator-parity.md` | Paridad compartida | Copy overwrite |
| `~/.flowforge/cache/FlowForge` | Clone git (`--depth 1`) del repo | Clone; **nunca hace pull** ⚠️ |
| `~/.flowforge-backups/{nombre}-{timestamp}/` | Backups pre-overwrite | Creados por FlowForgeModule/InstallOpenCode |

### 2.4 Rutas por proyecto (vía `flowforge init <ruta>` / `ide/install.sh <ruta>`)

| Ruta | Contenido |
|------|-----------|
| `.cursor/` (rules, agents, commands) | Pack Cursor proyecto |
| `.opencode/agents/`, `.opencode/commands/`, `.kilo/agents/` | Pack OpenCode + Kilo proyecto |
| `.github/agents/*.agent.md` + `copilot-instructions.md` | Pack Copilot proyecto |
| `.agents/` (rules, workflows, skills, `AGENTS.md`) | Pack Antigravity proyecto |
| `AGENTS.md`, `.flowforge.json`, `.ai-work/`, `docs/` | Scaffold FlowDoc / FlowForge |

---

## 3) Archivos de configuración del usuario (qué debe preservarse)

**Clasificación propuesta: "del sistema" (managed / regenerable) vs "del usuario" (debe preservarse).**

| Archivo | Dueño | Estado actual en update/reinstall | Riesgo |
|---------|-------|-----------------------------------|--------|
| `~/.engram/config.json` (sync.mode, remote_url, user, components.version) | Usuario + sistema | Preservado ✅ (ConfigStore RMW + prefill ADR-010/ENG-453) | Bajo |
| `~/.engram/engram.db` + `-wal`/`-shm` — **todas las memorias** | Usuario | Intacto ✅ | **CRÍTICO: nunca tocar** |
| `~/.engram/local_memory/*.md` | Usuario | Intacto ✅ | **CRÍTICO: nunca tocar** |
| `~/.config/opencode/opencode.json(c)` — provider/agent/permission/instructions propias | Usuario | Merge quirúrgico ✅ (OpenCodeConfigGenerator + sidecar; preserva provider `opencode-go` pagado y su model) | Medio |
| `~/.config/opencode/.flowforge-managed.json` | Sistema (sidecar) | Regenerado ✅ | Bajo |
| `~/.cursor/mcp.json` — **otros servers MCP del usuario** | Usuario | **Overwrite completo ⚠️** (`EngramModule.WriteMcpJson`) | **ALTO: pérdida de datos** |
| `~/.gemini/config/mcp_config.json` — **otros servers MCP** | Usuario | **Overwrite completo ⚠️** (`WriteMcpJson`) | **ALTO: pérdida de datos** |
| `~/.config/opencode/agents/forge-*.md`, `~/.cursor/agents/forge-*.md`, `~/.copilot/agents/`, `~/.config/kilo/agents/`, `~/.gemini/config/rules/` — **agentes modificados por el usuario** | Usuario | Overwrite con warning (FU-5/FU-6: interacción diferida) | Medio-Alto |
| `~/.copilot/instructions/flowforge.instructions.md` | Sistema | Overwrite | Bajo |
| `~/.flowforge/shared/` | Sistema | Overwrite | Bajo |
| `~/.flowforge/cache/FlowForge` | Sistema | Clone — **stale tras updates** | Medio (ver §5-8) |
| `~/.flowforge-backups/` | Sistema | Solo crece (sin retención/poda) | Bajo |
| `docs/`, `.ai-work/`, `AGENTS.md`, `.flowforge.json` (proyectos) | Usuario + sistema | Solo `flowforge init` | Bajo |

**Conclusión:** los únicos archivos 100% "del usuario" que hoy se preservan bien son
los de engram (`config.json` vía RMW + data dir intacto). El feature debe extender esa
garantía al MCP de Cursor/Antigravity (merge, no overwrite) y a los agentes por IDE
(detección de modificaciones del usuario + backup/merge/confirmación).

---

## 4) Estado actual del comando `update` (gap principal)

`src/FlowForge.Installer/Commands/UpdateCommand.cs` — `flowforge update [--check] [--yes]`:

1. Compara `latestEngram` (GitHub) vs `cfg.Components.EngramDotnet.Version` (config.json).
2. Verifica compatibilidad con el manifest (`requires.engram-dotnet`).
3. `EngramModule.UpdateAsync(version)` → descarga y **sobrescribe el binario in-place**
   (sin backup del binario anterior, sin health-check post-swap).
4. Actualiza `config.json` con la nueva versión.
5. `--check` también compara la versión del installer, pero contra una **constante
   hardcodeada** `InstallerVersion = "0.1.0-alpha.6"` (drift, ver §7-2).

**Lo que NO hace hoy:**
- ❌ No actualiza FlowForge skills/agents por IDE (no existe `UpdateAsync` para FlowForgeModule).
- ❌ No actualiza el propio binario `flowforge` (FU-3 lo difirió "por diseño": los bootstraps
  deberían reemplazar el binario; nunca se implementó).
- ❌ No actualiza el cache git (`FlowForgeRepoLocator` clona `--depth 1` una vez y no hace `git pull`;
  re-installs posteriores usan el cache **stale** salvo que falten `AGENTS.md` o templates).
- ❌ No hace backup del binario previo → sin rollback automático.
- ❌ No valida post-update (`engram --version` / `flowforge doctor`).
- ❌ No distingue componentes: `update` = solo engram.
- ❌ No maneja el caso "release upstream sin binarios" en update (el install sí lo hace desde
  alpha.12: `GetLatestEngramVersionAsync` saltea releases sin assets — incidente v1.3.0).

---

## 5) Dependencias entre componentes

| Dependencia | Naturaleza | Implicación para update |
|-------------|-----------|--------------------------|
| Pack IDE de FlowForge → repo FlowForge (git) | `FlowForgeRepoLocator` clona/cachea `efreet111/FlowForge` | Update de skills requiere refrescar el cache (pull o clone fresh) ANTES de copiar |
| `opencode.json` (template) → `$FLOWFORGE_ENGRAM_BIN`, `$FLOWFORGE_REPO` | Plantilla resuelve rutas del binario engram y repo | Si el binario cambia de ruta, la config MCP queda rota (hoy ruta estable `~/.local/bin/engram`) |
| MCP de IDEs → binario `engram` presente | `mcp.engram.command` apunta al binario | Update de engram debe verificar que el binario nuevo arranca ANTES de dejar la config apuntando a él |
| `~/.engram/config.json` (components.*.version) → lógica de `update`/`status` | Version tracking via ConfigStore | La granularidad por componente exige entradas por componente (ya existe `engram_dotnet` + `flowforge` pero flowforge.version = versión del **installer**, no del pack) |
| FlowDoc → `flowforge init` | Scaffold por proyecto | Independiente del update global (por proyecto) |
| Manifest remoto → compatibilidad cross-component | `requires.*` en manifest.yaml | El update por componente debe evaluar compatibilidad con TODOS los componentes instalados (no solo engram) |
| `install/install.sh` (bootstrap) → binario flowforge | `update --self` diferido (FU-3) | Si se quiere auto-update del installer, el bootstrap debe ser re-ejecutable (curl\|bash) o el binario debe auto-reemplazarse |

**Orden de update sugerido (topológico):** engram-dotnet primero (binario + lib nativa),
luego regenerar MCP configs (merge preservando otros servers), luego packs FlowForge por IDE,
luego flowdoc (por proyecto), y por último el installer mismo.

---

## 6) Riesgos identificados

### 6.1 Seguridad (triggers: forge-discovery-security)

| # | Riesgo | Severidad | Detalle |
|---|--------|-----------|---------|
| S1 | **Manifest no firmado** (FU-1 diferido, OQ-1 aceptado en ADR-002) | 🟡 Alto | Un MITM que sirva un `manifest.yaml` modificado puede bajar constraints de compatibilidad. El update por componente AMPLÍA esta superficie (más lógica guiada por manifest). |
| S2 | **Checksum best-effort en bootstrap** | 🟡 Medio | `install/install.sh` continúa si el `.sha256` no se puede descargar (`if [[ -n "$EXPECTED_SHA" ]]`). El binario interno sí verifica (log: "SHA-256 OK"). |
| S3 | **Clone git sin pinning** (`--depth 1` sobre HTTPS, sin verificación de commit/tag) | 🟡 Medio | El source de los packs FlowForge es el repo `main` — un repo comprometido serviría agentes maliciosos. Considerar pins por tag en updates. |
| S4 | **Sin rollback** — binario sobrescrito in-place sin copia previa | 🟠 Alto | Un release roto (tipo engram v1.3.0 sin binarios, 2026-07-14) deja al usuario sin forma automática de volver atrás. |
| S5 | **PII en data de engram** | 🟢 Bajo | `~/.engram/` contiene memorias (posiblemente datos personales). El update NO debe subirlas ni exponerlas; el installer ya tiene `PiiScanner` para configs. |
| S6 | **Overwrite de MCP ajeno** | 🟠 Alto | `WriteMcpJson` para Cursor/Antigravity destruye servers MCP del usuario (datos de config perdidos). Es pérdida de datos, no solo inconveniencia. |
| S7 | Dependencias | 🟢 Bajo | El stack es C# AOT + shell + git. Sin dependencias nuevas previstas; las existentes (Spectre, ConsoleAppFramework, System.Text.Json) no tienen CVEs conocidos en el audit previo (CKP-3 de ENG-301: 0 vulnerables). |

### 6.2 Operativos

| # | Riesgo | Severidad | Detalle |
|---|--------|-----------|---------|
| O1 | **Cache git stale** | 🟠 Alto | `FlowForgeRepoLocator.TryClone` solo borra el cache si faltan `AGENTS.md` o templates. Un re-install/update días después de un cambio en `main` usa packs viejos silenciosamente. |
| O2 | **Fallo de descarga no aborta** | 🟡 Medio | Log del 2026-07-23: `DownloadAndVerify timeout` → `flowforge install` **continuó** e instaló FlowForgeModule igualmente, dejando engram sin actualizar sin error duro. |
| O3 | **Drift de versiones hardcodeadas** | 🟡 Medio | `UpdateCommand.InstallerVersion="0.1.0-alpha.6"`, `FlowForgeModule.InstallerVersion="0.1.0-alpha.6"`, manifest `0.1.0-alpha.7`, `install.sh` log `alpha.2`. El self-check de update miente. |
| O4 | **Uninstall destructivo** | 🟡 Medio | `UninstallCommand` borra `~/.engram/` recursivo (memorias). Tiene confirmación explícita, pero sin backup. |
| O5 | **Sin tests de update** | 🟠 Alto | FU-7 (test suite) quedó diferido; `tests/FlowForge.Installer.Tests/` existe (InstallCommandSourceTests, InstallerAsksForSyncUrlTests, GitHubReleasesClientTests, PathHelperTests) pero **no hay tests de UpdateCommand ni de merge MCP**. |
| O6 | **Retención de backups** | 🟢 Bajo | `~/.flowforge-backups/` crece sin límite/poda. Con rollback formalizado, definir retención. |

### 6.3 Compliance & costos

- **Compliance (forge-discovery-compliance):** sin regulación nueva activada. Los datos
  personales son memorias locales del usuario en `~/.engram/` (GDPR no aplica a software
  self-hosted sin procesamiento externo). Recomendación: documentar que el update no
  transmite memorias (data minimización) y mantener el `PiiScanner` en configs generadas.
  Sin bloqueos.
- **Costos (forge-discovery-cost):** impacto de infraestructura **despreciable**. Updates
  = descargas de GitHub Releases (binarios ~100 MB engram, packs IDE < 1 MB) + un clone
  shallow del repo FlowForge por refresh de cache. Sin storage nuevo, sin servicios cloud.
  Único costo: CI (test-installer.yml) si se agregan tests de update — menor.

---

## 7) Requisitos implícitos descubiertos

1. **Granularidad por componente**: engram-dotnet, flowforge skills (por IDE), flowdoc, installer.
   `flowforge update` debe aceptar selector de componente (p.ej. `--component engram|flowforge|all`).
2. **Preservación de config del usuario**: `~/.engram/config.json` (sync URL, user, channel),
   `engram.db`/`local_memory/` (intocables), MCP configs de TODOS los IDEs (merge, no overwrite),
   agentes/rules modificados por el usuario (detección de drift → backup + confirmación o merge).
3. **Rollback**: backup del binario/config previo antes del swap; health-check post-update
   (`engram --version`, parse MCP, `flowforge doctor`); restauración automática si falla.
4. **Version tracking por componente** en `config.json` (ya hay `components.*.version`;
   flowforge.version hoy = versión del installer — separar versión del pack/skills).
5. **Actualización del cache git** (`git pull` o clone fresh con pin por tag) antes de copiar packs.
6. **Compatibilidad multi-componente** vía manifest (`requires.*`) evaluada contra el conjunto instalado.
7. **Idempotencia**: re-ejecutar update no debe duplicar ni romper (ya hay AtomicWriter y ConfigStore atómico).
8. **Actualización del propio installer** (re-abrir FU-3): o el bootstrap se re-ejecuta
   (`curl | bash` idempotente) o el binario se auto-reemplaza con backup.
9. **Verificación post-update**: `flowforge doctor` debe validar que la config MCP resultante
   es consistente (pattern ya existe: `AntigravityPackValidator`, T-034 OpenCode validation).
10. **Clasificación managed vs user** por archivo/ruta (generalizar el patrón sidecar
    `managed-paths.json` de OpenCode a todos los destinos).
11. **Shutdown seguro de engram antes de swap binario** (el MCP puede estar corriendo;
    verificar que no haya procesos usando el binario antes de reemplazarlo — hoy se asume).
12. **Mensajería bilingüe**: mantener consistencia ES/EN (NFR-ERR-001 sigue sin resolverse).

---

## 8) Reusable Patterns Found (step 5 — OBLIGATORIO)

| Patrón | Ubicación | Qué resuelve → reusar para |
|--------|-----------|-----------------------------|
| **Update binario + version tracking** | `EngramModule.UpdateAsync` (`src/FlowForge.Installer/Modules/EngramModule.cs:116-145`) | Plantilla de update por componente: download → swap → version update. Extender con backup + health-check. |
| **Merge con sidecar managed-paths** | `Modules/OpenCode/OpenCodeConfigGenerator.cs` (`MergeManagedPaths`, preserva `opencode-go` pagado), `ManagedPathsSidecar.cs`, `AtomicWriter.cs` | **EL patrón a generalizar**: merge quirúrgico de configs preservando bloques del usuario → aplicarlo a `mcp.json` de Cursor y `mcp_config.json` de Antigravity (hoy overwrite total). |
| **Config atómica** | `Infrastructure/ConfigStore.cs` (read-modify-write + rename atómico) | Persistencia de versiones por componente. |
| **Manifest remoto + compatibilidad** | `Infrastructure/ManifestClient.cs` (`CheckEngramCompatibility`, `CheckInstallerCompatibility`) | Negociación de compatibilidad multi-componente en update. |
| **Releases client con skip de releases sin assets** | `Infrastructure/GitHubReleasesClient.cs` (`GetLatestEngramVersionAsync`) | Resolución robusta de "última versión instalable" (lección incidente v1.3.0). |
| **Repo locator + cache** | `Infrastructure/FlowForgeRepoLocator.cs` (clone `--depth 1` a `~/.flowforge/cache/FlowForge`) | Fuente de packs FlowForge; requiere añadir refresh (`git pull`) para updates. |
| **Backup pre-overwrite** | `Modules/FlowForgeModule.cs` (`BackupDirectory` → `~/.flowforge-backups/{name}-{ts}`) | Base del mecanismo de rollback. |
| **Validación post-install** | `Infrastructure/AntigravityPackValidator.cs`, `DoctorCommand` (T-034 OpenCode validation) | Health-check post-update. |
| **Auditoría de cambios** | `Modules/OpenCode/InstallLogger.cs` + hashes SHA-256 pre/post (`FlowForgeModule.ComputeSha256`) | Trazabilidad de qué cambió en cada update (para verify + rollback selectivo). |

Resultado: **NO es greenfield**. ~80% de los bloques necesarios ya existen en
`src/FlowForge.Installer/`. La tarea es componerlos bajo un `UpdateOrchestrator` por
componente + generalizar el merge sidecar a todos los destinos + añadir backup/rollback.

---

## 9) Memorias relevantes (step 3 — búsqueda local fallback)

⚠️ `mem_search` MCP no disponible en este entorno; se usó fallback local sobre
`.engram/local_memory/` y `.ai-work/`.

### Observaciones en `.engram/local_memory/`
| Obs | topic_key | Relevancia |
|-----|-----------|------------|
| `obs-20260722-policy-installer-protection.md` | `policy/installer-protection` | El installer es dominio separado con constraints propias (AOT, seguridad, pipeline) |
| `obs-20260722-pattern-installer-protection-policy.md` | `development/workflow/installer-protection` | `src/FlowForge.Installer/*` protegido: cambios de installer = feature propia con flow completo |
| `obs-20260715-opencode-installer-config-decision.md` | `architecture/opencode-installer-config` | SSOT de config OpenCode: sidecar managed-paths, PII scan, modelos free |
| `obs-20260715-session-close-fix-opencode-installer-config-gen.md` | — | Cierre del fix de config-gen (contexto del merge quirúrgico) |
| `obs-20260715-pii-scanner-json-aware.md` | — | PII scanner JSON-aware (preservar en configs generadas) |
| `obs-2026-05-30-config-mcp-binary-path.md` | — | MCP path del binario engram (config depende de la ruta del binario) |

### Features previas en `.ai-work/` (epics relacionados)
| Feature | Qué aporta |
|---------|-----------|
| `stack-installer/` (ENG-301) | Origen del installer; ADR-001/002; FU-1..FU-8 (incl. FU-3 update --self, FU-5/FU-6 conflict detection, FU-7 tests) |
| `fix-installer/` | TTY/headless; timeouts de descarga; diagnóstico |
| `fix-ide-installer-packs/` | Matriz de rutas por IDE (ADR-008); advertencia pre-overwrite; backups |
| `fix-opencode-installer-config-gen/` | Config canónica OpenCode sin PII; sidecar; **BLOCKER pendiente: lista oficial de modelos free** |
| `eng-453-installer-server-url/` | ADR-010 (Accepted): persistencia de sync URL en config.json; prefill en re-install |
| `incident-engram-v130-missing-binaries/` | Releases upstream sin binarios rompen install fresh; lección: skip releases sin assets; CI cron sugerido |

### ADRs relevantes
| ADR | Estado | Contenido |
|-----|--------|-----------|
| ADR-001 | Accepted | Stack C# .NET AOT + ConsoleAppFramework + bootstrap scripts |
| ADR-002 | Accepted | Manifest remoto; **trade-off: manifest sin firma (OQ-1)** |
| ADR-005 | Accepted | Instalación headless y libs nativas |
| ADR-008 | Accepted | **Matriz canónica de rutas por IDE** (global vs proyecto) |
| ADR-009 | Proposed | `flowforge sync connect <url>` (complementario) |
| ADR-010 | Accepted | Prompt + persistencia de `ENGRAM_SERVER_URL` (ENG-453) |
| ADR-011 (ide-pack-parity) | Proposed | Paridad de packs; `flowforge skills update` mencionado como post-MVP |
| ADR-011 (opencode-antigravity-customizations) | Accepted | Antigravity 2.x global customizations |
| ADR-012 | Accepted | Config de modelos por IDE |
| ADR-013 (engram-mcp) | Accepted | MCP engram sin dependencia Anthropic |

---

## 10) FlowDoc context

- PRD: `docs/PRD.md` (read: **sí** — sección 1 "Problem Statement"; producto: configurador de ecosistema).
- HU referenciado: **none** (no se pasó HU-NNN; backlog usa `docs/backlog/NS-*.md`).
- HU flowforge_slug: unset.
- Nota: `.flowforge.json` tiene `docs_framework: "flowdoc"` v2.0 → el feature debería
  documentarse siguiendo FlowDoc si forge-arch lo considera (ADRs, changelog).

---

## 11) Open questions para forge-arch (no bloquean, pero ayudan a decidir)

1. ¿`flowforge update` debe soportar update del propio binario (re-abrir FU-3) o se delega
   al bootstrap re-ejecutable? (decisión de alcance)
2. ¿La fuente de los packs FlowForge en update debe ser `main` (con pin por tag) o un tag
   de release semver? (afecta S3 y O1)
3. ¿Rollback automático solo de binario engram o también de configs MCP/agentes?
4. ¿Se generaliza el sidecar `managed-paths.json` a otros destinos (Cursor/Antigravity)
   o se usa otra estrategia de diff-and-merge?
5. ¿Los agentes/rules modificados por el usuario deben detectarse por hash (como
   `model-assignments.md` / `AntigravityPackValidator`) y ofrecer 3 opciones
   (sobrescribir/backup/omitir) — reactivando FU-5/FU-6?

---

## 12) Constraints / reglas operativas a respetar

- **Installer Protection Policy** (obs-20260722): los cambios a `src/FlowForge.Installer/*`
  e `ide/install.sh`/`install.ps1` son de dominio exclusivo de este feature — no mezclar
  con cambios de texto de agentes.
- AOT-safe: sin reflexión dinámica en el binario; System.Text.Json con source-gen
  (`JsonSerializerContext`); no romper TrimmerRoots.
- No escribir rutas absolutas de usuario ni API keys en plantillas (PiiScanner).
- Backups antes de overwrite; writes atómicos (ConfigStore/AtomicWriter).
- El manifest se sirve vía HTTPS sin firma (aceptado) — no empeorar la superficie.
- `~/.engram/engram.db` y `~/.engram/local_memory/` son **intocables** por el update.
- Idempotencia y re-ejecutabilidad de todos los comandos.
- Bilingüe ES/EN según decisión de UX pendiente (NFR-ERR-001).

---
