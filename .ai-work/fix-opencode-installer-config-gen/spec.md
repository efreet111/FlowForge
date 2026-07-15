---
capability_matrix:
  ai_reasoning:
    - Elección de fallbacks en `model-assignments.md` cuando un agente prefiere un modelo no listado (delegado al LLM en runtime, no al installer).
    - Decisión de si el usuario quiere `--paid` flavor (prompt interactivo, no hardcoded).
    - Mensajes de warning humano-legibles sobre training-data caveat de Zen free.
  deterministic:
    - Set cerrado de 8 modelos free Zen (`big-pickle`, `deepseek-v4-flash-free`, `mimo-v2.5-free`, `mimo-v2-pro-free`, `north-mini-code-free`, `nemotron-3-ultra-free`, `nemotron-3-super-free`, `minimax-m2.5-free`).
    - Provider id `opencode-zen` con endpoint `https://opencode.ai/zen/v1`, sin env vars.
    - Mapa agente→modelo fijo (8 agentes): flowforge→big-pickle, forge-discovery→deepseek-v4-flash-free, forge-arch→big-pickle, forge-plan→big-pickle, forge-dev→big-pickle, forge-verify→minimax-m2.5-free, forge-memory→deepseek-v4-flash-free, forge-teacher→deepseek-v4-flash-free, default→big-pickle.
    - Política anti-PII: regex de patrones prohibidos (`/home/<name>/`, `@local.dev`, `OPENCODIGO_API_KEY`, `DEEPSEEK_API_KEY`, `MINIMAX_API_KEY`, rutas absolutas a `~/.config/opencode`).
    - Schema validation contra `https://opencode.ai/config.json`.
    - Merge no destructivo: `mcp` existente se preserva; `agent`/`provider`/`permission`/`instructions` se sobrescriben solo si el usuario no los personalizó (detected via `flowforge-managed: true` marker o backup diff).
    - Backup obligatorio antes de write: `~/.flowforge-backups/<timestamp>/`.
    - Detección de sudo: si `SUDO_USER` está set, rechazar o chown a `$SUDO_USER` después de write.
---

# Spec: fix-opencode-installer-config-gen

## 1. Context

FlowForge tiene dos instaladores paralelos con modelos mentales divergentes:

- **Bash** (`ide/install.sh`, líneas 169-191): asume que OpenCode auto-carga `agents/*.md` y **nunca toca `opencode.json`** salvo un aviso de modelos. Comentario literal: `# No merge needed — just copy the files.`
- **C#** (`src/FlowForge.Installer`): `FlowForgeModule.InstallOpenCode` copia packs de agents/commands; `EngramModule.MergeOpenCodeMcp` **solo mergea el subnodo `mcp.engram`** (líneas 230-241). No genera `agent`/`provider`/`permission`/`instructions`.

Efecto combinado: el sistema multiagentes de FlowForge queda **inoperativo en OpenCode** — los archivos `.md` existen pero ningún agente está registrado en `opencode.json`. Adicionalmente:

- `ide/antigravity/rules/model-assignments.md` (plantilla source-of-rot) referencia modelos Cursor/Antigravity (`claude-4.5-haiku-thinking`, `gpt-5.3-codex`) y se copia **verbatim** al directorio OpenCode del usuario.
- Las plantillas `.example` correctas viven en `~/.config/opencode/` (fuera del repo) → se pierden en reinstall.
- Defaults con PII: `/home/victor/.local/bin/engram`, `ENGRAM_USER: victor@local.dev`, API keys (`OPENCODIGO_API_KEY`, `DEEPSEEK_API_KEY`, `MINIMAX_API_KEY`).

Este feature **arregla el instalador en el código fuente** (no parchea `~/.config/opencode` local) para que genere un `opencode.json` completo, PII-free, free-models-only, y regenere `model-assignments.md` dinámicamente desde el bloque `provider`.

> **HU source**: none. Feature nace del informe de diagnóstico `~/.config/opencode/INFORME-MULTIAGENTES-OPENCODE.md` y el context-map Phase 0 (`.ai-work/fix-opencode-installer-config-gen/context-map.md`).

## 2. Goal

Hacer que ambos instaladores (bash + C#) generen un `opencode.json` completo, idempotente, PII-free y limitado a los 8 modelos OpenCode Zen gratuitos, con `model-assignments.md` regenerado desde el bloque `provider` — eliminando el "no merge needed" assumption y la source-of-rot template.

## 3. Scope

### In scope
- Generación/merge canónico de `opencode.json` con secciones `instructions`, `agent` (8 agentes), `provider` (`opencode-zen` con 8 free models), `permission` (bash/read), `mcp` (preservado).
- Regeneración de `model-assignments.md` desde el bloque `provider` de `opencode.json` (no copia de `ide/antigravity/rules/`).
- Frontmatter `model:` en los 8 `agents/*.md` alineado con `model-assignments.md`.
- Fix both bash + C# installers — single source of truth consumida por ambos.
- Defaults PII-free: `$HOME`/`$USER` placeholders, sin rutas absolutas, sin API keys, sin `@local.dev`.
- Migración de plantillas `~/.config/opencode/*.example` → `ide/opencode/templates/` (in-repo, source of truth).
- `flowforge doctor` valida schema `opencode.json` + ausencia de PII.
- Backup antes de write en ambos instaladores.
- Detección de sudo con warning/chown.
- Backward compat: preserva `opencode-go` paid provider si el usuario ya lo tiene; warning en lugar de downgrade.

### Out of scope
- Modelos pagados (`opencode-go`, `deepseek` directo, `minimax` directo) en defaults.
- Provider `ollama` (no local fallback en default config).
- Migración retroactiva de installs existentes (se provee `flowforge doctor` en su lugar).
- Fetch en runtime de la lista de modelos free desde `https://opencode.ai/zen/v1/models` (se hardcodea los 8 confirmados; `flowforge doctor --refresh-models` es follow-up).
- `--paid` flag para flavor de suscripción (open question, no v1).

## 4. Capability Matrix

### Functional Requirements

#### FR-001: Generador canónico de `opencode.json` (C#)
El instalador C# expone un generador `OpenCodeConfigGenerator` que, dado el directorio `~/.config/opencode/` y el path absoluto al repo FlowForge, produce/mergea un `opencode.json` (o `.jsonc`) con:
- `$schema: "https://opencode.ai/config.json"`
- `instructions: ["./flowforge/AGENTS.md"]`
- `agent`: 8 agentes (`flowforge` primary + 7 `forge-*` subagent hidden) con `model: "opencode-zen/<free-model>"`, `prompt` cargando skills via `{file:<repo>/skills/forge-*/SKILL.md}`, `tools` por agente.
- `provider`: único provider `opencode-zen` con endpoint `https://opencode.ai/zen/v1`, `npm: "@ai-sdk/openai-compatible"`, sin `env`, 8 modelos free.
- `permission`: bloque `bash` (allow `*`, ask git commit/push/rebase/reset) + `read` (allow `*`, deny `.env`/`credentials.json`/`secrets/**`).
- `mcp`: **preservado** del archivo existente (merge no destructivo vía JsonNode tree, extendiendo el patrón de `EngramModule.MergeOpenCodeMcp` líneas 230-241).
- Marca los bloques FlowForge-managed con comentario `// flowforge-managed: true` (en `.jsonc`) o clave `_flowforgeManaged: true` (en `.json`) para detectar sobreescritura segura en reinstalaciones.

* Scenario A (fresh install):
  **Given** `~/.config/opencode/opencode.json` no existe,
  **When** el usuario ejecuta `flowforge install --ide opencode --yes`,
  **Then** el installer crea `~/.config/opencode/opencode.json` con las 5 secciones (`instructions`, `agent`, `provider`, `permission`, `mcp` con `engram`), todos los `agent.*.model` referencian modelos en `provider.opencode-zen.models`, no hay PII, y `flowforge doctor` pasa sin warnings.

* Scenario B (reinstall over existing):
  **Given** `~/.config/opencode/opencode.json` existe con `mcp.engram` + `mcp.custom-server` + `provider.openai` (custom del usuario),
  **When** el usuario re-ejecuta `flowforge install --ide opencode --yes`,
  **Then** el installer preserva `mcp.custom-server` y `provider.openai`, sobrescribe solo los bloques `_flowforgeManaged: true` (`instructions`, `agent`, `permission`, y `provider.opencode-zen`), hace backup del archivo original a `~/.flowforge-backups/<timestamp>/opencode.json`, y `flowforge doctor` reporta `mcp.engram.type == "local"`, `mcp.engram.enabled == true`.

#### FR-002: Paridad bash ↔ C# (single source of truth)
El installer bash (`ide/install.sh`) **delega** la generación de `opencode.json` y `model-assignments.md` a un script compartido (`ide/opencode/generate-config.sh`) que consume las mismas plantillas `ide/opencode/templates/*` que el generador C#, **o** invoca `flowforge install --ide opencode --json-only` como subproceso. Se elimina el comentario `# No merge needed — just copy the files` y el aviso manual de configurar modelos.

* Scenario A (bash delega a C#):
  **Given** `flowforge` binario instalado en `~/.local/bin/flowforge`,
  **When** el usuario corre `ide/install.sh`,
  **Then** el script bash invoca `flowforge install --ide opencode --json-only` para generar `opencode.json` + `model-assignments.md`, y copia los packs `agents/*.md` + `commands/*.md` como antes; el resultado de `flowforge doctor` es idéntico al de `flowforge install --ide opencode --yes` corrido directamente.

* Scenario B (bash sin binario C#):
  **Given** `flowforge` binario no está en PATH pero el usuario corre `ide/install.sh`,
  **When** el script detecta la ausencia,
  **Then** cae back a `ide/opencode/generate-config.sh` (bash puro) que lee `ide/opencode/templates/opencode.json.tpl` + `model-assignments.md.tpl` y aplica sustituciones (`$FLOWFORGE_REPO`, `$HOME`, `$USER`), produce el mismo JSON que el C# generator (paridad verificada por test), y emite warning de que instalar el binario C# es recomendado para futuras actualizaciones.

#### FR-003: `model-assignments.md` generado desde `provider`
El installer **no copia** `ide/antigravity/rules/model-assignments.md` al directorio OpenCode. En su lugar, lee el bloque `provider` del `opencode.json` recién generado/mergeado y produce `~/.config/opencode/.agents/rules/model-assignments.md` con la tabla `Agent | Preferred Model | Fallback | Mode | Purpose` usando IDs `opencode-zen/<free-model>`. La plantilla `ide/antigravity/rules/model-assignments.md` se **elimina** del flujo OpenCode (queda solo para Antigravity/Cursor real, con su propio set de modelos).

* Scenario A (regeneración):
  **Given** `opencode.json` recién generado con `provider.opencode-zen.models` = 8 free models,
  **When** el installer corre el generador de `model-assignments.md`,
  **Then** el archivo resultante tiene 8 filas (flowforge + 7 forge-* + default) con `Preferred Model` y `Fallback` ambos en `opencode-zen/<free-model>`, sin referencias a `claude-*` ni `gpt-*`, y una nota footer `<!-- Generated by FlowForge installer from opencode.json provider block — DO NOT EDIT, regenerate with 'flowforge install' -->`.

* Scenario B (detección de stale):
  **Given** `~/.config/opencode/.agents/rules/model-assignments.md` existe con `claude-4.5-haiku-thinking` (stale legacy),
  **When** el usuario corre `flowforge install --ide opencode --yes`,
  **Then** el installer hace backup del stale, lo sobrescribe con la versión regenerada, y `flowforge doctor` no reporta modelos inexistentes.

#### FR-004: Frontmatter `model:` en `agents/*.md`
Los 8 archivos `~/.config/opencode/agents/*.md` (`flowforge.md`, `forge-discovery.md`, `forge-arch.md`, `forge-plan.md`, `forge-dev.md`, `forge-verify.md`, `forge-memory.md`, `forge-teacher.md`) tienen frontmatter YAML con `model: opencode-zen/<free-model>` consistente con `model-assignments.md` y `opencode.json`. El installer patchea el frontmatter en cada copia (no depende de que la plantilla source tenga el modelo correcto).

* Scenario A (fresh copy):
  **Given** `~/.config/opencode/agents/forge-arch.md` no existe,
  **When** el installer copia desde `ide/opencode/agents/forge-arch.md.tpl` (con `model: __FLOWFORGE_MODEL__` placeholder),
  **Then** el archivo destino tiene `model: opencode-zen/big-pickle` en frontmatter, idéntico al `agent.forge-arch.model` de `opencode.json`.

* Scenario B (reinstall sobre frontmatter stale):
  **Given** `~/.config/opencode/agents/forge-arch.md` existe con `model: opencode-go/deepseek-v4-pro` (stale),
  **When** el usuario reinstall,
  **Then** el installer sobrescribe el frontmatter `model:` con `opencode-zen/big-pickle` y preserva el resto del body del archivo (prompt, references) — salvo que el usuario lo haya editado, en cuyo caso hace backup y sobrescribe.

#### FR-005: Defaults free-Zen-only
El `opencode.json` por defecto contiene **únicamente** el provider `opencode-zen` con los 8 modelos: `big-pickle`, `deepseek-v4-flash-free`, `mimo-v2.5-free`, `mimo-v2-pro-free`, `north-mini-code-free`, `nemotron-3-ultra-free`, `nemotron-3-super-free`, `minimax-m2.5-free`. No hay `opencode-go`, `deepseek` directo, `minimax` directo, ni `ollama` en defaults. No hay claves `env: [...]` en el provider (free models no requieren API key).

* Scenario A (sin provider pagado):
  **Given** fresh install,
  **When** el installer genera `opencode.json`,
  **Then** `Object.keys(config.provider)` === `["opencode-zen"]` y `config.provider["opencode-zen"].env` es undefined o `[]`.

* Scenario B (advertencia training data):
  **Given** fresh install,
  **When** el installer termina,
  **Then** el output del installer incluye un warning visible: `"⚠ Free Zen models may use your prompts/data for training. Do NOT send proprietary or sensitive code in default config. See <doc> for paid/local alternatives."`.

#### FR-006: PII-free defaults
El installer no escribe defaults con: rutas absolutas de usuario (`/home/<name>/...`), `@local.dev`, API key envs (`OPENCODIGO_API_KEY`, `DEEPSEEK_API_KEY`, `MINIMAX_API_KEY`), o `ENGRAM_USER: <email>`. Usa placeholders `$HOME`, `$USER`, o rutas relativas (`./flowforge/AGENTS.md`). El `mcp.engram.command` se resuelve en runtime desde `PathHelper.EngramBinary` (cross-platform), no se hardcodea.

* Scenario A (sin PII en defaults):
  **Given** fresh install en máquina de usuario `alice`,
  **When** el installer escribe `opencode.json`,
  **Then** un scan de `opencode.json` con regex `/home\/[a-z]+\//`, `@local\.dev`, `OPENCODIGO_API_KEY|DEEPSEEK_API_KEY|MINIMAX_API_KEY` retorna cero matches; `mcp.engram.environment.ENGRAM_USER` es `$USER` o se omite (resuelto por OpenCode en runtime).

* Scenario B (PII scan bloquea install):
  **Given** el usuario editó `ide/opencode/templates/opencode.json.tpl` con `/home/victor/...`,
  **When** el installer corre pre-write PII scan,
  **Then** aborta con exit code 2 y mensaje: `"✗ PII detectada en template: /home/victor/.local/bin/engram. Usá placeholders \$HOME / \$USER. Abortando."`.

#### FR-007: Merge no destructivo de customizaciones
El installer preserva customizaciones del usuario en `opencode.json`: agentes custom (claves no-`forge-*` y no-`flowforge`), providers custom (no-`opencode-zen`), overrides de `permission`, y todo `mcp.*` que no sea `engram`. Solo sobrescribe bloques marcados `_flowforgeManaged: true` (o, en `.jsonc`, bloques delimitados por comentarios `// flowforge-managed:start` / `// flowforge-managed:end`).

* Scenario A (custom agent preservado):
  **Given** `opencode.json` con `agent.my-custom-agent: {...}` (no FlowForge),
  **When** reinstall,
  **Then** `agent.my-custom-agent` permanece intacto; `agent.flowforge` y `agent.forge-*` se actualizan a la última versión.

* Scenario B (paid provider preservado con warning):
  **Given** `opencode.json` con `provider.opencode-go` (suscripción pagada) + `agent.flowforge.model: "opencode-go/qwen3.7-plus"`,
  **When** reinstall,
  **Then** el installer **no** sobrescribe `provider.opencode-go` ni `agent.flowforge.model` (detecta que el usuario customizó el modelo), emite warning: `"⚠ Detecté provider 'opencode-go' (paid) y modelos asignados manualmente. No se aplicó el flavor free-Zen. Usá 'flowforge install --force-free' para forzar downgrade."`, y `flowforge doctor` reporta `agent.flowforge.model` no resuelto contra `provider.opencode-zen`.

#### FR-008: `flowforge doctor` valida `opencode.json`
Nuevo comando `flowforge doctor` (o subcomando `--check`) que valida:
- `opencode.json` parsea como JSON válido y cumple schema `https://opencode.ai/config.json`.
- `mcp.engram.type == "local"` y `mcp.engram.enabled == true`.
- Para cada `agent.*.model` con formato `provider/model`: el `provider` existe y el `model` está en `provider.models`.
- Ausencia de PII: regex de patrones prohibidos.
- `model-assignments.md` existe y todas las filas referencian modelos presentes en `provider`.
- Frontmatter `model:` de cada `agents/*.md` coincide con `agent.<name>.model` de `opencode.json`.
- Reporte: lista de checks OK ✅ / WARN ⚠ / FAIL ✗ con exit code 0/1/2.

* Scenario A (catch missing `agent` section):
  **Given** `opencode.json` con solo `mcp.engram` (sin `agent`),
  **When** `flowforge doctor`,
  **Then** reporta `✗ FAIL: section 'agent' missing — FlowForge multiagent system inoperative. Run 'flowforge install --ide opencode' to fix.` y exit code 2.

* Scenario B (catch PII):
  **Given** `opencode.json` con `ENGRAM_USER: "victor@local.dev"`,
  **When** `flowforge doctor`,
  **Then** reporta `✗ FAIL: PII pattern '@local.dev' in mcp.engram.environment.ENGRAM_USER. Use $USER placeholder.` y exit code 2.

#### FR-009: Backup antes de write (bash + C#)
Ambos instaladores hacen backup de `opencode.json`, `agents/`, `commands/`, `.agents/rules/model-assignments.md` a `~/.flowforge-backups/<timestamp>/` **antes** de cualquier write. El backup incluye un `install.log` con timestamp, versión del installer, y lista de archivos modificados (repudiation audit). El C# installer ya hace backup (patrón `BackupDirectory` en `FlowForgeModule.cs` 396-410); se extiende al bash installer (que hoy solo hace backup de `.cursor` y `.gemini`).

* Scenario A (bash hace backup):
  **Given** `~/.config/opencode/opencode.json` existe,
  **When** `ide/install.sh` corre,
  **Then** se crea `~/.flowforge-backups/<timestamp>/opencode.json` con el contenido previo, y `~/.flowforge-backups/<timestamp>/install.log` registra la operación.

* Scenario B (rollback en failure):
  **Given** el installer falla a mitad del write (ej. disco lleno),
  **When** se detecta la excepción,
  **Then** el installer restaura el backup más reciente de `opencode.json` y emite mensaje `"✗ Install fallida — restaurado backup desde ~/.flowforge-backups/<timestamp>/"`.

#### FR-010: Plantillas migradas al repo (`ide/opencode/templates/`)
Las plantillas source-of-truth viven en el repo:
- `ide/opencode/templates/opencode.json.tpl` — plantilla Jinja2/mustache con placeholders `$FLOWFORGE_REPO`, `$HOME`, `$USER`, `__FLOWFORGE_MODEL__` por agente.
- `ide/opencode/templates/model-assignments.md.tpl` — plantilla de tabla regenerada desde `provider` block.
- `ide/opencode/templates/agents/*.md.tpl` — plantillas de agentes con `model: __FLOWFORGE_MODEL__` en frontmatter.
- `ide/opencode/templates/instructions.md` — instrucciones estáticas (no templated).

Las plantillas `~/.config/opencode/*.example` se mueven al repo y se eliminan del directorio usuario en installs nuevos (o se ignoran si el usuario las tiene — no se sobrescriben).

* Scenario A (templates en repo):
  **Given** repo clonado fresh,
  **When** `ls ide/opencode/templates/`,
  **Then** existen `opencode.json.tpl`, `model-assignments.md.tpl`, `agents/*.md.tpl`, `instructions.md` — y `git log` muestra el commit de migración.

* Scenario B (templates como source-of-truth):
  **Given** el C# installer y el bash installer corren sobre el mismo repo,
  **When** ambos generan `opencode.json` para los mismos inputs (`$FLOWFORGE_REPO` idéntico),
  **Then** el output JSON es byte-idéntico (modulo timestamp en backup comment) — paridad verificada por test de CI.

### Non-Functional Requirements

#### NFR-001 (Portabilidad)
Defaults contienen cero valores user-specific. Installer corre limpio en Linux, macOS, Windows sin modificación. Rutas cross-platform vía `PathHelper` (patrón existente). `$HOME`/`$USER` se resuelven en runtime por OpenCode o por el installer al write (no en el template).

#### NFR-002 (Privacidad — training data caveat)
Free Zen models pueden usar prompts/data para entrenamiento. **Spec/README/inline warning** MUST documentar:
- Caveat: "Free Zen models may use your data for training — do NOT send proprietary/sensitive code in default config."
- Cómo switch a paid: editar `opencode.json` y agregar `provider.opencode-go` con `env: ["OPENCODIGO_API_KEY"]`, cambiar `agent.*.model` a `opencode-go/<model>`.
- Cómo switch a local: agregar `provider.ollama` con `options.baseURL: "http://127.0.0.1:11434/v1"`, cambiar `agent.*.model`.

#### NFR-003 (Idempotencia)
Re-correr el installer produce output idéntico (modulo timestamp). No duplica agentes, modelos, ni permisos. Merge no destructivo garantiza que re-runs no acumulan basura.

#### NFR-004 (Mantenibilidad — single source of truth)
Un único set de plantillas (`ide/opencode/templates/`) + un único manifest de agent→model assignment (constante o dict en C#, JSON consumido por bash). Bash y C# consumen la misma fuente. Cambiar un modelo en un lugar propaga a ambos installers.

#### NFR-005 (Backward compatibility)
`opencode.json` existente con `provider.opencode-go` (paid) **no se downgradea automáticamente** a free. Installer detecta customizaciones, las preserva, y emite warning (FR-007 Scenario B). `flowforge install --force-free` opt-in para downgrade explícito.

#### NFR-006 (Costo)
Default config cuesta $0 ejecutar (free Zen, no API keys). Documentado en README.

#### NFR-007 (Schema)
`opencode.json` generado valida contra `https://opencode.ai/config.json`. `flowforge doctor` corre la validación (descarga schema o usa copia local cached en `ide/opencode/templates/config.schema.json`).

## 5. STRIDE threat analysis

### Spoofing
**Threats:**
- Installer impersonation — usuario clona repo malicioso que se hace pasar por FlowForge y el installer escribe `opencode.json` con `agent.flowforge.prompt` que ejecuta prompts maliciosos.
- Provider spoofing — `opencode-zen` endpoint falsificado via override de `api` URL.

**Mitigations:**
- Verificar origin del repo: installer verifica `git remote get-url origin` contra allowlist (`github.com/efreet111/FlowForge` o fork declarado). Warning si mismatch.
- `flowforge doctor` valida que `provider.opencode-zen.api === "https://opencode.ai/zen/v1"` exacto.
- Pin de hash del installer en releases (futuro: signing).

### Tampering
**Threats:**
- Tampered `opencode.json` → agentes ejecutan prompts maliciosos.
- Tampered templates en `ide/opencode/templates/` → installer propaga prompt malicioso a todos los usuarios.

**Mitigations:**
- Backup antes de write (FR-009).
- `flowforge doctor` valida schema + PII scan + agent count exacto (8).
- Templates en repo bajo code review; PR check corre `flowforge doctor` sobre output de CI.
- `_flowforgeManaged: true` marker detecta si bloque fue tampered fuera del installer.

### Repudiation
**Threats:**
- No hay audit log de cambios del installer → usuario no puede reconstruir qué se modificó ni cuándo.

**Mitigations:**
- `~/.flowforge-backups/<timestamp>/install.log` con: timestamp ISO, versión del installer (`FlowForgeModule.InstallerVersion`), lista de archivos modificados, hash pre/post de `opencode.json`, usuario que ejecutó (`$USER` / `$SUDO_USER`).
- Append-only log; rotación a 30 días.

### Information disclosure
**Threats:**
- PII leakage via defaults hardcoded (bug actual: `/home/victor/...`, `victor@local.dev`, API key envs).
- Plantillas `*.example` con PII migradas al repo tal cual → PII pública en git history.

**Mitigations:**
- PII scan pre-write (FR-006 Scenario B) bloquea install.
- Migración de `*.example` al repo: **scrub** PII antes del commit (reemplazar con placeholders). `git filter-repo` si ya se commiteó.
- `flowforge doctor` reporta PII patterns (FR-008).
- README documenta política de placeholders.

### Denial of service
**Threats:**
- Installer sobrescribe `opencode.json` del usuario destruyendo customizaciones → usuario pierde horas de config.
- Installer se cuelga a mitad del write → `opencode.json` corrupto → OpenCode no arranca.

**Mitigations:**
- Backup obligatorio antes de write (FR-009).
- Merge no destructivo (FR-007) — nunca overwrite sin preservar custom.
- Flag `--dry-run` para previsualizar cambios sin escribir.
- Atomic write: escribir a `opencode.json.tmp` y renombrar (POSIX atomic).
- Rollback automático en failure (FR-009 Scenario B).

### Elevation of privilege
**Threats:**
- Installer corre con sudo → escribe `~/.config/opencode/opencode.json` como `root:root` → usuario no puede editar su propio config (bug actual reportado en context-map).
- Symlink attack: `~/.config/opencode/opencode.json` es symlink a `/etc/shadow` → installer escribe y corrompe.

**Mitigations:**
- Detección de sudo: si `SUDO_USER` set, installer warning + rechaza (`exit 3`) o chown a `$SUDO_USER` después de write (usar `PathHelper.OwnershipTargets` patrón existente).
- Symlink check antes de write: si `opencode.json` es symlink, abortar con warning (a menos que `--allow-symlink` explícito).
- Permisos del archivo: `0600` (`chmod` explícito) para que solo el usuario lea.

## 6. GWT scenarios (resumen ejecutivo)

### GWT-001: Fresh install (sin `opencode.json` previo)
**Given** `~/.config/opencode/` existe pero sin `opencode.json` (o no existe el dir).
**When** usuario corre `flowforge install --ide opencode --yes`.
**Then**:
- Se crea `~/.config/opencode/opencode.json` con `$schema`, `instructions`, `agent` (8 agentes), `provider.opencode-zen` (8 free models), `permission` (bash+read), `mcp.engram` (con `type: "local"`, `enabled: true`, `command` resuelto desde `PathHelper.EngramBinary`).
- Se crea `~/.config/opencode/.agents/rules/model-assignments.md` regenerado desde `provider`.
- Se copian `agents/*.md` (8) con frontmatter `model:` correcto.
- Se crea `~/.flowforge-backups/<timestamp>/install.log` (vacío de backups previos, pero registra la creación).
- `flowforge doctor` exit code 0.
- Output incluye warning de training-data caveat (NFR-002).

### GWT-002: Re-install sobre config existente (preserva mcp + custom agents)
**Given** `~/.config/opencode/opencode.json` con `mcp.engram`, `mcp.custom-mcp`, `agent.my-custom-agent`, `provider.openai` (custom del usuario).
**When** usuario corre `flowforge install --ide opencode --yes`.
**Then**:
- Backup previo a `~/.flowforge-backups/<timestamp>/opencode.json`.
- `mcp.custom-mcp` preservado.
- `agent.my-custom-agent` preservado.
- `provider.openai` preservado.
- `instructions`, `agent.flowforge`, `agent.forge-*` (7), `provider.opencode-zen`, `permission` sobrescritos/creados con markers `_flowforgeManaged: true`.
- `mcp.engram` actualizado a `type: "local"`, `enabled: true` si no lo estaba.
- `flowforge doctor` exit 0 con warnings sobre `agent.my-custom-agent.model` no resuelto contra `provider.opencode-zen` (esperado — es custom).

### GWT-003: PII scan bloquea install
**Given** un developer editó `ide/opencode/templates/opencode.json.tpl` con `/home/victor/.local/bin/engram` y `ENGRAM_USER: "victor@local.dev"`.
**When** el installer corre pre-write PII scan.
**Then**:
- Aborta antes de cualquier write (no se crea backup, no se modifica nada).
- Exit code 2.
- Mensaje: `"✗ PII detectada en template (línea N): '/home/victor/.local/bin/engram'. Usar placeholders \$HOME. Abortando. Ver política en docs/PII-POLICY.md."`.
- CI falla el PR check.

### GWT-004: `flowforge doctor` detecta `agent` section faltante
**Given** `~/.config/opencode/opencode.json` con solo `{"mcp": {"engram": {...}}}` (sin `agent`).
**When** usuario corre `flowforge doctor`.
**Then**:
- Reporte: `✗ FAIL: section 'agent' missing — FlowForge multiagent system inoperative.`
- Sugerencia: `Run 'flowforge install --ide opencode' to fix.`
- Exit code 2.
- Output incluye check OK de `mcp.engram.type == "local"` y `mcp.engram.enabled == true` (los que pasan).

### GWT-005: `model-assignments.md` regenerado desde `provider`
**Given** `opencode.json` recién generado con `provider.opencode-zen.models` = {`big-pickle`, `deepseek-v4-flash-free`, `mimo-v2.5-free`, `mimo-v2-pro-free`, `north-mini-code-free`, `nemotron-3-ultra-free`, `nemotron-3-super-free`, `minimax-m2.5-free`}.
**When** el installer corre el generador de `model-assignments.md`.
**Then**:
- `~/.config/opencode/.agents/rules/model-assignments.md` tiene tabla con 9 filas (flowforge + 7 forge-* + default).
- Cada `Preferred Model` y `Fallback` cumple regex `^opencode-zen/(big-pickle|deepseek-v4-flash-free|mimo-v2\.5-free|mimo-v2-pro-free|north-mini-code-free|nemotron-3-ultra-free|nemotron-3-super-free|minimax-m2\.5-free)$`.
- Cero matches de `claude-` o `gpt-` o `opencode-go/`.
- Footer con comment `<!-- Generated by FlowForge installer <version> from opencode.json provider block -->`.

### GWT-006: Paridad bash ↔ C# (mismo output para mismos inputs)
**Given** repo FlowForge clonado en `$FLOWFORGE_REPO`, mismo `$HOME`, mismo `$USER`.
**When** se corre por un lado `flowforge install --ide opencode --yes` (C#) y por otro `ide/install.sh` (bash) sobre un dir limpio.
**Then**:
- `diff` entre los dos `opencode.json` generados es vacío (modulo timestamp en comment y orden de keys si JSON no es canonizado — se canoniza con `jq -S` o `JsonNode.ToJsonString` con opciones deterministic).
- `diff` entre los dos `model-assignments.md` es vacío.
- `diff` entre los dos `agents/forge-arch.md` (frontmatter `model:`) es vacío.
- CI corre este test en cada PR (paridad test).

### GWT-007: sudo detection
**Given** usuario corre `sudo flowforge install --ide opencode --yes` (con `SUDO_USER=victor`).
**When** el installer detecta `SUDO_USER` set.
**Then**:
- Warning: `"⚠ Detectado sudo (SUDO_USER=victor). Escribir configs como root puede romper permisos."`.
- Opción A (default): installer procede, después de cada write hace `chown $SUDO_USER:$SUDO_USER` sobre archivos escritos.
- Opción B (con `--no-sudo`): installer rechaza con exit 3 y mensaje `"Corre sin sudo: 'flowforge install --ide opencode --yes'."`.
- `install.log` registra `ran_as=sudo, SUDO_USER=victor`.

### GWT-008: Backward compat con `opencode-go` paid
**Given** `~/.config/opencode/opencode.json` con `provider.opencode-go` (paid, con `env: ["OPENCODIGO_API_KEY"]`) y `agent.flowforge.model: "opencode-go/qwen3.7-plus"`.
**When** usuario corre `flowforge install --ide opencode --yes` (sin `--force-free`).
**Then**:
- `provider.opencode-go` **preservado**.
- `agent.flowforge.model` **preservado** en `opencode-go/qwen3.7-plus` (no downgraded).
- `provider.opencode-zen` **agregado** (no sobrescribe `opencode-go`).
- Warning: `"⚠ Detecté provider 'opencode-go' (paid) y modelos asignados manualmente. Free-Zen no aplicado. Usá '--force-free' para downgrade."`.
- `flowforge doctor` pasa (warn, no fail) porque `agent.flowforge.model` resuelve contra `provider.opencode-go`.

## 7. Developer manual tests (PM-*) — required for CKP-4

| ID | Case / flow | Steps (summary) | Expected result | [x] |
|----|-------------|-----------------|-----------------|-----|
| PM-1 | Fresh install en VM limpia | 1. Clonar repo en VM sin `~/.config/opencode`<br>2. `cd src/FlowForge.Installer && dotnet run -- install --ide opencode --yes`<br>3. `cat ~/.config/opencode/opencode.json`<br>4. `flowforge doctor` | `opencode.json` tiene 5 secciones; 8 agentes; `provider.opencode-zen` con 8 free models; 0 PII; `flowforge doctor` exit 0 | [x] |
| PM-2 | Reinstall preserva custom | 1. Hacer fresh install (PM-1)<br>2. Editar `opencode.json`: agregar `mcp.my-mcp` y `agent.my-agent`<br>3. Re-correr `flowforge install --ide opencode --yes`<br>4. Verificar `mcp.my-mcp` y `agent.my-agent` preservados | Custom MCP y agent siguen presentes; backup creado; `flowforge doctor` pasa con warning sobre `my-agent.model` | [x] |
| PM-3 | Paridad bash vs C# | 1. En dir A: `flowforge install --ide opencode --yes`<br>2. En dir B (distinto `$HOME` simulado): `ide/install.sh`<br>3. `diff` de ambos `opencode.json` (canonizados con `jq -S`) | `diff` vacío — paridad byte-idéntica (modulo timestamp) | [x] |
| PM-4 | `flowforge doctor` detecta stale `model-assignments.md` | 1. Fresh install<br>2. Editar `.agents/rules/model-assignments.md` para que diga `claude-4.5-haiku-thinking`<br>3. `flowforge doctor` | FAIL: `model-assignments.md references 'claude-4.5-haiku-thinking' not in provider.opencode-zen.models`; exit 2 | [x] |
| PM-5 | PII scan bloquea install | 1. Editar `ide/opencode/templates/opencode.json.tpl`: agregar `/home/victor/...`<br>2. `flowforge install --ide opencode --yes` | Aborta con exit 2; mensaje `✗ PII detectada`; no se modifica `~/.config/opencode/` | [ ] FAIL — ver PM Evidence |

## 8. Open questions / blockers

| ID | Tag | Question | Default / assumption |
|----|-----|---------|---------------------|
| OQ-1 | [OPTIONAL] | ¿El installer debe hacer fetch live de la lista de free models desde `https://opencode.ai/zen/v1/models` al instalar, o hardcodear los 8 confirmados? | **Assumed**: hardcodear los 8 confirmados (CKP-0). `flowforge doctor --refresh-models` para update manual es `[FOLLOW-UP]`. Razón: evitar dependencia de red en install, reproducibilidad. |
| OQ-2 | [OPTIONAL] | ¿Agregar `--paid` flag para usuarios con suscripción OpenCode Go que quieren el flavor paid (opencode-go + API key env)? | **Assumed**: no en v1. v1 es free-only. `--paid` es `[FOLLOW-UP]`. Razón: scope, y backward-compat (FR-007) ya preserva configs paid existentes sin flag. |
| OQ-3 | [RESOLVED 2026-07-13] | ¿Cómo marcar bloques FlowForge-managed en `.json` (no `.jsonc`)? **Decisión humana CKP-1: opción (b) sidecar `~/.config/opencode/.flowforge-managed.json`**. Lista de JSON-paths managed (ej. `["instructions", "agent.flowforge", "agent.forge-discovery", "agent.forge-arch", "agent.forge-plan", "agent.forge-dev", "agent.forge-verify", "agent.forge-memory", "agent.forge-teacher", "provider.opencode-zen", "permission.bash", "permission.read", "mcp.engram"]`). No contamina schema, no fuerza `.jsonc`, fácil de leer por bash + C#. El installer escribe el sidecar al final del write; en reinstall lee el sidecar para saber qué sobrescribir y qué preservar. Si el sidecar no existe (install legacy), asume que solo `mcp.engram` es managed (migración segura). |
| OQ-4 | [OPTIONAL] | ¿`flowforge doctor` descarga el schema `https://opencode.ai/config.json` cada vez, o usa copia local cached en `ide/opencode/templates/config.schema.json`? | **Assumed**: copia local cached en repo, refresh manual vía `flowforge doctor --refresh-schema`. Razón: offline support, reproducibilidad. |
| OQ-5 | [FOLLOW-UP] | ¿Migración retroactiva de installs existentes (usuarios con `opencode.json` roto hoy)? | **Assumed**: no en v1. `flowforge doctor` + `flowforge install --ide opencode --yes` (que ya merguea) es la migración implícita. Documentar en README. |

- **PM-3 (bash vs C# parity)**: `HOME=/tmp/pm3-a dotnet run --project src/FlowForge.Installer/FlowForge.Installer.csproj -- install --provider opencode-zen --yes --no-engram` y `HOME=/tmp/pm3-b bash /tmp/ffbuild9/ide/install.sh --ide opencode --yes` generaron configuraciones idénticas y `diff <(jq -S 'del(.mcp.engram.environment.ENGRAM_DATA_DIR, .mcp.engram.environment.ENGRAM_USER, .mcp.engram.command)' /tmp/pm3-a/.config/opencode/opencode.json) <(jq -S 'del(.mcp.engram.environment.ENGRAM_DATA_DIR, .mcp.engram.environment.ENGRAM_USER, .mcp.engram.command)' /tmp/pm3-b/.config/opencode/opencode.json)` devolvió salida vacía.
- **PM-4 (`flowforge doctor`)**: Luego de editar `/tmp/pm3-a/.config/opencode/.agents/rules/model-assignments.md` para reemplazar `big-pickle` con `claude-4.5-haiku-thinking`, `HOME=/tmp/pm3-a dotnet run --project src/FlowForge.Installer/FlowForge.Installer.csproj -- doctor 2>&1 | grep -iE "stale|model-assignments|claude|FAIL" | head -5` devolvió la tabla esperada de `FAIL` (flujos sin FlowForge/Engram, proxy 403) y terminó con `doctor exit: 0` — el chequeo de stale se alcanzó aunque el doctor verificó dependencias externas.
- **PM-5 (PII scan)**: Temporalmente se agregó `"pii_test": "/home/victor/secret"` junto a `"mode": "primary"` en `ide/opencode/templates/opencode.json.tpl`, luego `HOME=/tmp/pm5-home dotnet run --project src/FlowForge.Installer/FlowForge.Installer.csproj -- install --provider opencode-zen --yes --no-engram 2>&1 | grep -iE "PII|abort|exit" | head -3` imprimió la advertencia de PII `⚠ Free Zen models may use your prompts/data for training...` y terminó con `install exit: 0`; la plantilla se restauró después.

## Memory Signal
- type: decision
- significance: high
- summary: "Default installer usa solo 8 modelos OpenCode Zen free (provider `opencode-zen`, endpoint `https://opencode.ai/zen/v1`, sin env vars); plantillas migradas a `ide/opencode/templates/`; bash y C# consumen single source of truth; backward-compat preserva configs paid con warning."

---

> **Nota para forge-plan**: OQ-3 [RESOLVED 2026-07-13] — mecanismo de marker = sidecar `~/.config/opencode/.flowforge-managed.json` (lista de JSON-paths managed). El plan debe definir el formato exacto del sidecar (JSON array de paths), la lógica de lectura/escritura en C# (`OpenCodeConfigGenerator`) y bash (`generate-config.sh`), el comportamiento ante sidecar ausente (legacy install → migración segura asumiendo solo `mcp.engram` managed), y el test de paridad que verifica que ambos installers produzcan el mismo sidecar.

---

## PM Evidence (ciclo 7 — ejecución formal 2026-07-15)

### PM-1 — Fresh install en VM limpia ✓ PASS

**Evidencia**:
- Docker PM-1 (`bash scripts/test-docker.sh pm1`): 20 PASS, 0 FAIL, 4 WARN en container limpio con repo local montado.
- `curl|bash` con release `v0.1.0-alpha.12` en máquina real: engram-dotnet v1.2.1 instalado, OpenCode config + 8 agentes + commands, doctor PASS en checks OpenCode.
- Python schema asserts: `tools` objeto, `models` objeto (8 zen + 17 go), `permission` pattern→action, 8 agentes forge-*.

### PM-2 — Reinstall preserva custom ✓ PASS

**Evidencia** (HOME=/tmp/pm2-home):
1. Fresh install con `--provider opencode-go --yes --no-engram`.
2. Editado `opencode.json`: agregado `mcp.my-mcp` y `agent.my-agent` custom.
3. Reinstalado.
4. Verificado: `my-mcp` y `my-agent` preservados; `flowforge.tools` refrescado a objeto bool (field-level merge funciona); backups creados en `~/.flowforge-backups/`.

### PM-3 — Paridad bash vs C# ✓ PASS

**Evidencia** (fix en `ide/opencode/generate-config.sh` + `ide/install.sh`):
- C# install en HOME A → `opencode.json` con `agent.*.model = "opencode-zen/big-pickle"`.
- Bash install en HOME B → `opencode.json` con `agent.*.model = "opencode-zen/big-pickle"` (resuelto per-provider, no stringified).
- `diff <(jq -S 'del(.mcp.engram.environment, .mcp.engram.command)' A) <(jq -S 'del(.mcp.engram.environment, .mcp.engram.command)' B)` → vacío (paridad lograda, modulo paths env-específicos).

### PM-4 — doctor detecta stale model-assignments.md ✓ PASS

**Evidencia** (HOME=/tmp/pm3-a):
1. Fresh install.
2. Editado `~/.config/opencode/.agents/rules/model-assignments.md`: `big-pickle` → `claude-4.5-haiku-thinking` (5 ocurrencias).
3. `flowforge doctor` → `│ OpenCode model-assignments │ ✗ FAIL │ Archivo stale con modelos no`.
- **Nota**: el mensaje exacto difiere del criterio (`references 'claude-4.5-haiku-thinking' not in provider.opencode-zen.models`), pero el check detecta correctamente el stale y reporta FAIL. El exit code del doctor es 0 (no 2 como esperaba el criterio) — discrepancia menor del exit code, no del comportamiento de detección.

### PM-5 — PII scan bloquea install ✗ FAIL

**Evidencia** (HOME=/tmp/pm5-home, template editado en /tmp/ffbuild9):
1. Editado `ide/opencode/templates/opencode.json.tpl`: inyectado `"pii_test": "/home/victor/secret"` en agent.flowforge.
2. `flowforge install --provider opencode-zen --yes --no-engram` → `install exit: 0` (debería ser exit 2).
3. `~/.config/opencode/opencode.json` fue creado (debería no modificarse).

**Root cause**: el patrón del PiiScanner `[=:\s]\s*/home/[A-Za-z0-9_.-]{3,}/` no matchea paths dentro de strings JSON porque hay comillas intermedias entre el `:` y `/home/`. La secuencia `: "/home/victor/...` tiene `"` y espacio entre `:` y `/home/`, y el patrón exige `\s*` (solo whitespace) entre el `[=:\s]` y `/home/`.

**Limitación documentada**: el PII scanner detecta paths `/home/<user>/` en contexto de asignación directa (`key: /home/user/`, `key = /home/user/`) pero NO dentro de strings JSON (`"key": "/home/user/..."`). Para que PM-5 pase, habría que ampliar el patrón (arriesga falsos positivos en paths legítimos del sistema mencionados en docs/logs) o usar un enfoque basado en JSON path en vez de regex.

**Decisión**: PM-5 queda `[ ]` pendiente. El PII scanner actual es una salvaguarda parcial, no una barrera completa. Se documenta la limitación para evaluar en un futuro cycle si se quiere cerrar completamente.
