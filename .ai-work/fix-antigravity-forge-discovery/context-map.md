# Context Map (Discovery) — fix-antigravity-forge-discovery (+ workflows `/flow-*`)

**Feature slug:** `fix-antigravity-forge-discovery`  
**Date:** 2026-07-15 (actualizado con síntoma workflows)  
**Analyst:** forge-discovery  
**Verdict:** **CLEAR** (suficiente contexto para Phase 1; ver preguntas abiertas al humano)

---

## Problem statement

El usuario reporta **dos síntomas co-primarios** en Antigravity (Windows y Linux):

1. **`forge-discovery` falta** al iniciar el flujo (p. ej. `/flow-start` no delega a discovery).
2. **Los comandos `/flow-*` no aparecen** en el picker de Antigravity (Customizations → Workflows vacío o `/` sin resultados).

Ambos apuntan a gaps en el pipeline de instalación Antigravity: skills no expuestos en el workspace activo **y** workflows instalados sin el frontmatter YAML que el parser de Antigravity exige.

En Antigravity, `forge-discovery` **no es un agente `.md` independiente** (como en Cursor/OpenCode). Es un **skill** bajo `skills/forge-discovery/SKILL.md`, expuesto vía symlinks en:

- Global: `~/.gemini/config/skills/forge-discovery/` (+ espejo `config/.agents/skills/`)
- Proyecto: `{repo}/.agents/skills/forge-discovery/`

El orquestador delega vía `workflow.md` / `flow-start.md` (“Delegate to **forge-discovery**”), pero Antigravity solo puede invocarlo si el skill está instalado y visible en el workspace activo.

---

## FlowDoc context

- **PRD:** `docs/PRD.md` (read: sí — sección 1 Problem Statement; instalador multi-IDE para SMB)
- **HU referenced:** ninguna HU explícita del humano
- **HU flowforge_slug:** unset
- **docs_framework:** `flowdoc` v2.0 en `.flowforge.json` (sin `paths` custom)

---

## Relevant prior observations

| Fuente | Hallazgo |
|--------|----------|
| `docs/decisions/ADR-009-opencode-antigravity-customizations.md` | Antigravity 2.0 lee `~/.gemini/config/`, no `~/.gemini/antigravity/`. Skills = directorios `config/skills/forge-*/`, **no** `skills.json`. |
| `.ai-work/fix-ide-installer-packs/antigravity-workflows-closure.md` | Fix jul-2026: C# + `ide/install.sh` migrados a `config/`; **pendiente** paridad `install.ps1` y verificación de skills en CI Windows. |
| `.engram/local_memory/obs-20260704-ide-opencode-antigravity-session.md` | Misma causa raíz documentada; symlinks a `skills/forge-*`; workflows requieren frontmatter YAML. |
| `.engram/local_memory/obs-20260704-opencode-antigravity-install.md` | Verificación manual: `ls ~/.gemini/config/skills/forge-dev`. |
| `mem_current_project` (engram MCP) | **Falló** en esta sesión — se usó fallback grep en `.engram/local_memory/`. |

**topic_key asociado:** `architecture/ide-opencode-antigravity` (épico `fix-ide-installer-packs`, parcialmente resuelto; regresión/paridad incompleta).

---

## Reusable Patterns Found

- `src/FlowForge.Installer/Modules/FlowForgeModule.cs` (L346–369) `InstallAntigravitySkills()` — itera `skills/forge-*`, crea symlink o copia recursiva en fallo → **patrón canónico a replicar en `install.ps1` y en verificación doctor/CI**.
- `ide/install.sh` (L114–129) `install_antigravity_skills()` — symlink `ln -sfn`; copia si repo en `/tmp/*` (install remoto) → **paridad shell Linux correcta**.
- `ide/install.sh` (L138–154) `install_antigravity_global()` — destino `~/.gemini/config/` + espejo `.agents/` → **referencia para Windows**.
- `src/FlowForge.Installer/Infrastructure/FlowForgeRepoLocator.cs` — cache `~/.flowforge/cache/FlowForge` para symlinks tras `curl | bash` → **relevante si symlinks rotos**.
- `ide/cursor/compile-agents-from-skills.py` — contraste: Cursor **compila** skills en agents; Antigravity **no** tiene paso equivalente.
- **Negativo en Windows:** `ide/install.ps1` (L254–270) **no** usa `InstallAntigravitySkills` ni `config/` — divergencia confirmada.

---

## Inventario esperado vs actual — Antigravity completo

| Artefacto | Fuente repo | Global Linux | Global Windows (C#) | Global Windows (`ide/install.ps1`) | Proyecto (`.agents/`) |
|-----------|-------------|--------------|---------------------|-----------------------------------|-------------------------|
| `AGENTS.md` | `ide/antigravity/AGENTS.md` | `~/.gemini/config/` ✅ | `%USERPROFILE%\.gemini\config\` ✅ | `%LOCALAPPDATA%\Google\Gemini\antigravity\` ❌ legacy | `.agents/AGENTS.md` ✅ |
| `rules/*.md` | `ide/antigravity/rules/` | `config/rules/` ✅ | idem ✅ | `antigravity/rules/` ❌ | `.agents/rules/` ✅ |
| `workflows/flow-*.md` | `ide/antigravity/workflows/` | `config/workflows/` ⚠️ **6/7 sin frontmatter** → `/` roto | idem | `antigravity/workflows/` ❌ ruta no escaneada | `.agents/workflows/` ✅ (7/7 con frontmatter en repo; installer sobrescribiría sin FM) |
| `skills/forge-discovery/` | `skills/forge-discovery/` | `config/skills/` symlink ✅ | idem (symlink/copy) | **NO INSTALADO** ❌ | `.agents/skills/` — **vacío en máquina usuario** ❌ |
| `GEMINI.md` (always-on) | `rules/workflow.md` | `~/.gemini/GEMINI.md` ✅ (sin frontmatter `alwaysApply`) | idem | **NO** ❌ | N/A |
| MCP Engram | `EngramModule` | `config/mcp_config.json` ✅ | idem | **NO** ❌ | `.agents/mcp_config.json` (manual) |
| `skills.json` | — | eliminado por installer ✅ | idem | N/A | `.agents/skills.json` presente (no oficial) ⚠️ |

**Evidencia en máquina del usuario (Linux):**

```text
~/.gemini/config/skills/forge-discovery → ~/.flowforge/cache/FlowForge/skills/forge-discovery  ✅
~/.gemini/config/.agents/skills/forge-discovery → (mismo)  ✅
{FlowForge}/.agents/skills/  → VACÍO  ❌
{FlowForge}/.agents/skills.json → entries path a skills/ (mecanismo no documentado en ADR-009)  ⚠️
~/.gemini/config/workflows/flow-start.md → SIN frontmatter YAML  ⚠️
{FlowForge}/.agents/workflows/flow-start.md → CON frontmatter  ✅
```

---

## Por qué `/flow-*` no aparece en Antigravity (investigación dedicada)

### Reglas de descubrimiento Antigravity (ADR-009 + memoria local)

| Requisito | Fuente | Consecuencia si falta |
|-----------|--------|----------------------|
| Ruta global | `~/.gemini/config/workflows/*.md` | Workflows en `~/.gemini/antigravity/` → **invisibles** |
| Ruta proyecto | `{repo}/.agents/workflows/*.md` | Sin `.agents/workflows/` instalado → depende solo de global |
| **Frontmatter YAML** con `description:` en **una línea** | ADR-009 L89–99; [forum Google](https://discuss.ai.google.dev/t/antigravity-ide-slash-commands-workflows-disappear-entirely-no-results-due-to-4-fatal-parser-exceptions/135370) | **Parser `/` devuelve 0 resultados** |
| Nombre de archivo | `flow-start.md` → comando `/flow-start` | Nombre correcto en todos los packs |
| Reinicio IDE | `antigravity-workflows-closure.md` | Cambios no visibles hasta reload |
| MCP no vacío | ADR-009 L111 | `mcp_config.json` 0 bytes puede romper parser — **en máquina usuario: 203 bytes, OK** |

El instalador **no valida ni inyecta** frontmatter: hace `CopyGlob` / `cp` tal cual desde `ide/antigravity/workflows/`.

### Inventario fuente vs instalado vs proyecto (esta máquina, 2026-07-15)

#### Pack fuente del instalador — `ide/antigravity/workflows/`

| Archivo | Frontmatter `description:` | Visible como `/flow-*` |
|---------|---------------------------|------------------------|
| `flow-start.md` | ❌ | ❌ |
| `flow-plan.md` | ❌ | ❌ |
| `flow-dev.md` | ❌ | ❌ |
| `flow-verify.md` | ❌ | ❌ |
| `flow-close.md` | ❌ | ❌ |
| `flow-rework.md` | ❌ | ❌ |
| `flow-status.md` | ✅ | ✅ (único) |

#### Copia manual / no-canónica — `.agents/workflows/` (repo FlowForge)

| Archivo | Frontmatter | Notas |
|---------|-------------|-------|
| Los 7 `flow-*.md` | ✅ todos | **Diverge** del pack instalador; no es lo que copia `flowforge init` |

Ejemplo proyecto (`flow-start.md`):
```yaml
---
description: Iniciar feature FlowForge (Discovery a Spec, CKP-0 y CKP-1)
---
```

Ejemplo fuente instalador (`ide/antigravity/workflows/flow-start.md`):
```markdown
# /flow-start — New feature
1. Derive `feature-slug` ...
```
(sin bloque `---`)

#### Instalado global — `~/.gemini/config/workflows/` (y espejo `config/.agents/workflows/`)

**Idéntico al pack fuente** — confirmado por `head` en los 7 archivos:

- 6 archivos empiezan con `# /flow-...` (sin YAML)
- Solo `flow-status.md` tiene frontmatter
- **Predicción:** picker `/` muestra como mucho `/flow-status`; el resto **ausente**

#### Visibilidad según workspace abierto

| Workspace en Antigravity | Workflows leídos | `/flow-*` esperado |
|--------------------------|------------------|-------------------|
| `~/.gemini/config/` | `config/workflows/` o `config/.agents/workflows/` | **6/7 rotos** (sin FM) |
| Repo FlowForge (con `.agents/workflows/` actual) | `.agents/workflows/` del proyecto | **7/7 OK** si Antigravity escanea `.agents/` del workspace |
| Repo sin `flowforge init` | Solo global | **6/7 rotos** |
| Windows + `ide/install.ps1` | `%LOCALAPPDATA%\Google\Gemini\antigravity\workflows\` | **0/7** (ruta no escaneada por Antigravity 2.0) |

### Pipeline instalador — workflows (Linux + Windows)

| Canal | Origen copiado | Destino global | Destino proyecto | ¿Preserva frontmatter? |
|-------|----------------|----------------|------------------|------------------------|
| C# `InstallAntigravity()` | `ide/antigravity/workflows/` | `~/.gemini/config/workflows/` + `config/.agents/workflows/` | — | Copia literal; **no añade FM** |
| C# `InstallAntigravityProject()` | `ide/antigravity/workflows/` | — | `{repo}/.agents/workflows/` | Idem — **sobrescribe** `.agents/` con versión sin FM |
| `ide/install.sh` global | `ide/antigravity/workflows/` | `~/.gemini/config/workflows/` | — | Idem |
| `ide/install.sh` proyecto | `ide/antigravity/workflows/` | — | `{repo}/.agents/workflows/` | Idem |
| `ide/install.ps1` global | `ide/antigravity/workflows/` | `%LOCALAPPDATA%\Google\Gemini\antigravity\workflows\` ❌ | — | Ruta incorrecta + sin FM |
| `ide/install.ps1` proyecto | `ide/antigravity/workflows/` | — | `{repo}/.agents/workflows/` (solo rules/wf/AGENTS) | Sin skills; copia sin FM |

**Conclusión mecánica:** cualquier install canónico (C# o `install.sh`) deja workflows **sin frontmatter** salvo `flow-status.md`. Eso explica el síntoma “no aparecen `/flow-*`” cuando el usuario depende del pack global o reinstala sobre el proyecto.

### Matriz Win vs Linux — workflows específicamente

| Escenario | Linux | Windows |
|-----------|-------|---------|
| Ruta escaneada por Antigravity 2.0 | `~/.gemini/config/workflows/` | `%USERPROFILE%\.gemini\config\workflows\` (C#) |
| Ruta escrita por `install.ps1` | N/A | `%LOCALAPPDATA%\Google\Gemini\antigravity\workflows\` ❌ |
| Archivos post-`flowforge install` | 7 `.md`, **6 sin FM** | Misma lógica C# |
| Archivos post-`ide/install.sh` | Idem | N/A |
| Archivos post-`ide/install.ps1` | N/A | En ruta legacy; FM igualmente ausente en 6/7 |
| CI verifica frontmatter | ❌ solo cuenta archivos | ❌ sin check Antigravity |
| Comando visible si FM OK | `/flow-start` desde `flow-start.md` | Idem |

---

## Mapa de código / archivos

### Instaladores

| Archivo | Rol Antigravity | Estado |
|---------|-----------------|--------|
| `src/FlowForge.Installer/Modules/FlowForgeModule.cs` | `InstallAntigravity()`, `InstallAntigravitySkills()`, `InstallAntigravityProject()` | ✅ Correcto (`config/` + skills) |
| `src/FlowForge.Installer/Infrastructure/PathHelper.cs` | `AntigravityConfigDir` = `~/.gemini/config` | ✅ |
| `src/FlowForge.Installer/Commands/InitCommand.cs` | Proyecto: `.agents/` + skills | ✅ código OK; **no aplicado** en repo usuario |
| `src/FlowForge.Installer/Commands/DoctorCommand.cs` | Solo verifica que exista `~/.gemini/config/` | ⚠️ No valida `skills/forge-discovery` |
| `ide/install.sh` | Global + proyecto con skills | ✅ |
| `ide/install.ps1` | Global → legacy `antigravity/`, **sin skills** | ❌ **Desactualizado** |
| `install/install.sh` | Bootstrap binario C# | N/A (delega a `flowforge install`) |

### Packs fuente

| Archivo | Nota |
|---------|------|
| `ide/antigravity/workflows/*.md` | 6/7 **sin** frontmatter YAML (solo `flow-status.md` lo tiene) |
| `.agents/workflows/*.md` | 7/7 **con** frontmatter — **diverge** del pack instalador |
| `ide/antigravity/rules/workflow.md` | **Sin** `alwaysApply: true` en frontmatter |
| `.agents/rules/workflow.md` | **Con** `alwaysApply: true` |
| `ide/antigravity/AGENTS.md` | Documentación **obsoleta** (`~/.gemini/antigravity/`) |
| `skills/forge-discovery/SKILL.md` | Fuente canónica del agente fase 0 |

### CI / verificación

| Archivo | Gap |
|---------|-----|
| `.github/workflows/test-installer.yml` (Linux) | Verifica AGENTS/rules/workflows; **no** `config/skills/forge-discovery` |
| `.github/workflows/test-installer.yml` (Windows) | **Sin** verificación Antigravity |
| `scripts/docker-pm1-test.sh` | Idem — sin assert de skills |

---

## Matriz Win vs Linux — comportamiento del instalador

| Escenario | Linux | Windows |
|-----------|-------|---------|
| `flowforge install` (C#) | `~/.gemini/config/` + skills symlink/copy | `%USERPROFILE%\.gemini\config\` + skills |
| `bash ide/install.sh` | Paridad con C# ✅ | N/A |
| `ide/install.ps1` | N/A | Legacy `%LOCALAPPDATA%\Google\Gemini\antigravity\`, **sin skills** ❌ |
| `flowforge init <proyecto>` | `.agents/skills/forge-*` symlinks | idem |
| `ide/install.ps1 -ProjectPath` | N/A | Copia rules/workflows/AGENTS; **sin `.agents/skills`** ❌ |
| Detección Antigravity | `~/.gemini` existe | C#: `~/.gemini`; PS1: `%LOCALAPPDATA%\Google\Gemini` primero |
| Symlink sin privilegios | `ln -sfn` OK | C#: `CreateSymbolicLink` → fallback copy |
| Install remoto (`curl \| bash`) | Copia skills si repo en `/tmp` | C#: cache `~/.flowforge/cache/FlowForge` |
| Ruta legacy | Cleanup elimina `~/.gemini/antigravity/` pack | PS1 **recrea** legacy ❌ |

---

## Hipótesis rankeadas (por evidencia) — actualizado

### H1 — Workflows instalados sin frontmatter YAML (causa #1 de `/flow-*` ausentes) ⭐

**Evidencia directa en esta máquina:** `~/.gemini/config/workflows/` tiene 7 archivos; **6 empiezan con `# /flow-` sin bloque `---`**. Solo `flow-status.md` tiene `description:`. ADR-009 documenta que sin frontmatter el parser `/` devuelve **0 resultados**. El instalador copia desde `ide/antigravity/workflows/` (mismo defecto en fuente).

**Afecta:** `flowforge install`, `ide/install.sh`, y `flowforge init` (todos usan `ide/antigravity/` como origen).

**Síntoma usuario:** Customizations vacío o `/` sin `flow-start`, `flow-plan`, etc.

### H2 — `ide/install.ps1` escribe workflows en ruta legacy (causa #1 en Windows) ⭐

**Evidencia:** L264–268 → `%LOCALAPPDATA%\Google\Gemini\antigravity\workflows\`. Antigravity 2.0 escanea `config/workflows/`, no `antigravity/workflows/`. Además 6/7 archivos sin frontmatter.

**Síntoma usuario:** 0 workflows visibles tras `install.ps1` en Windows.

### H3 — Proyecto sin skills en `.agents/skills/` (causa #1 de `forge-discovery` ausente)

**Evidencia:** `{FlowForge}/.agents/skills/` vacío; global `config/skills/forge-discovery` OK. Delegación post-`/flow-start` falla si el skill no es visible en el workspace.

**Nota:** Independiente de H1 — el usuario puede no ver `/flow-*` (H1) **y** no tener discovery aunque un workflow se ejecute manualmente.

### H4 — Pack fuente `ide/antigravity/` desincronizado de `.agents/` (causa raíz de H1)

**Evidencia:** `.agents/workflows/` tiene 7/7 frontmatter; `ide/antigravity/workflows/` tiene 1/7. El instalador **nunca** lee `.agents/` como fuente. Reinstalar destruye el frontmatter del proyecto.

**Fix arquitectónico:** sincronizar `ide/antigravity/workflows/` ← `.agents/workflows/` (o generar FM en build).

### H5 — Workspace incorrecto o scope mixto

**Evidencia:** Si workspace = `~/.gemini/config/` → workflows globales sin FM (H1). Si workspace = repo con `.agents/workflows/` corregido manualmente → deberían verse, **pero** skills siguen vacíos (H3). Si workspace = otro repo sin init → sin `.agents/` → H1.

**Pregunta al humano:** ¿qué carpeta tenés abierta como workspace?

### H6 — Divergencia rutas Windows (`%USERPROFILE%\.gemini` vs `%LOCALAPPDATA%\Google\Gemini`)

**Evidencia:** C# usa `UserProfile\.gemini\config`; PS1 prioriza `LOCALAPPDATA\Google\Gemini`. Riesgo de pack en ubicación que Antigravity no lee.

### H7 — `skills.json` en proyecto / MCP roto

**Evidencia menor:** `.agents/skills.json` no estándar; MCP en máquina usuario OK (203 B). Descartado como causa primaria de workflows.

---

## Gaps / CKP-0

**No hay BLOCKER de requisitos.** Problema acotado: paridad instalador Antigravity + frontmatter workflows + skills en workspace.

**Preguntas abiertas para el humano** (no bloquean arch):

1. ¿Qué comando de instalación usaste? (`flowforge install`, `bash ide/install.sh`, `ide/install.ps1`, `flowforge init`)
2. ¿OS? (Linux, Windows, WSL)
3. ¿Workspace abierto en Antigravity? (ruta exacta — repo vs `~/.gemini/config/`)
4. Al escribir `/` en el chat, ¿0 resultados o solo aparece `flow-status`?
5. ¿Reiniciaste Antigravity tras instalar?

---

## Recomendación para forge-arch

Proceder a **spec.md** enfocado en:

1. **Frontmatter obligatorio** en los 7 `ide/antigravity/workflows/flow-*.md` (sincronizar desde `.agents/workflows/`).
2. **Paridad `ide/install.ps1`** con C# / `install.sh` (`config/workflows/`, skills, cleanup legacy).
3. **Cambiar fuente canónica o validar post-copia**: installer debe fallar/avizar si workflow carece de `description:`.
4. **Doctor + CI**: assert frontmatter en cada `flow-*.md` + `skills/forge-discovery/SKILL.md`.
5. **Documentación**: `ide/antigravity/AGENTS.md`, ADR-008 (rutas `config/`).
6. **Política `skills.json`**: no escribir en proyecto; alinear con ADR-009.
7. **Windows path matrix**: unificar detección y destino `config/`.

---

## Memory Signal

- **type:** decision
- **significance:** critical
- **summary:** "Antigravity /flow-* requires YAML frontmatter (description:) in config/workflows/ or .agents/workflows/; installer copies ide/antigravity/workflows/ where 6/7 lack frontmatter — parser returns 0 results. Global install on user machine confirmed broken. install.ps1 writes to unscanned legacy path on Windows. forge-discovery is a separate skill symlink gap (.agents/skills/ empty). Fix: sync frontmatter to ide/antigravity pack + installer parity."
