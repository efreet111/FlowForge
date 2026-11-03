# HU-050: Contrato de customizations de Antigravity (rules `trigger:`, `/flow-*` como skills, subagentes)

## Metadata

| Campo | Valor |
|-------|-------|
| **HU** | HU-050 |
| **Titulo** | Reparar la integración de Antigravity: rules con `trigger:`, `/flow-*` como skills generadas, 7 subagentes nativos e instaladores verificables |
| **Estado** | Done |
| **Fecha de creacion** | 2026-10-29 |
| **Fecha de cierre** | 2026-10-29 |
| **PR** | [#38](https://github.com/efreet111/FlowForge/pull/38) |
| **Prioridad** | Crítica |
| **Skills relacionadas** | `flowforge`, `forge-dev`, `forge-verify`, `forge-memory` |
| **Tags** | `antigravity`, `installer`, `ide-pack`, `customizations`, `bugfix`, `contract` |

---

## 1. Resumen

La integración de Antigravity estaba **100% rota**: Antigravity descartaba las reglas en silencio, los `/flow-*` se entregaban como workflows muertos desde el 2026-10-19 y no se instalaba ni la skill orquestadora ni ningún subagente. Esta HU fija el **contrato real** (verificado contra la documentación oficial y el CLI `agy` 1.4.10) en el pack fuente, en los dos caminos de instalación, en los validadores y en `flowforge doctor`, y deja la evidencia en [ADR-029](../../docs/decisions/ADR-029-antigravity-customizations-contract.md).

---

## 2. Motivacion

### 2.1 Evidencia medida (`agy -p`, mismo workspace, sin tools)

| Fallo | Evidencia |
|-------|-----------|
| Rules con `alwaysApply` (clave inerte de Cursor) | `E rules.go:482] Invalid rule trigger: CORTEX_MEMORY_TRIGGER_UNSPECIFIED` **× 4** (una por archivo de regla) |
| `/flow-*` inexistentes | `/flow-start` **no disponible**; workflows legacy retirados el **2026-10-19** |
| Sin orquestador | **1** skill visible (faltaba `flowforge`, porque sólo se copiaban `forge-*`) |
| Sin subagentes | **0** subagentes invocables |

Las preguntas de control al agente respondían **NO** a “¿las instrucciones mencionan CKP-0?” y “¿la regla prohíbe `git push` sin permiso?”.

### 2.2 Impacto

**El orquestador nunca existió para el agente.** Antigravity no podía arrancar el flujo FlowForge: sin reglas, sin `/flow-*` y sin subagentes, el pack era decorativo. Peor: el fallo era **silencioso**, así que ni el usuario ni `flowforge doctor` lo detectaban.

### 2.3 Baseline del instalador (HEAD `5c384ae`)

| Comando | Resultado |
|---------|-----------|
| `bash ide/install.sh` | 🔴 **EXIT 1** — `FileNotFoundError: skills/forge-orchestrator/SKILL.md` en `ide/cursor/compile-agents-from-skills.py`; `set -e` abortaba **antes** de instalar OpenCode, Antigravity, VS Code y Kilo |
| `flowforge install --yes` | 🟡 EXIT 0 con pack inerte (`alwaysApply`, sin subagentes, `/flow-*` en `global_workflows/`) |
| `flowforge uninstall --yes` | 🟡 deja restos en `~/.gemini/config/` (gap preexistente) |

Detalle completo en `.ai-work/hu-050-antigravity-customizations-contract/installer-baseline.md` (artefacto de sesión, no versionado — ADR-002).

---

## 3. Alcance

**Incluido:**

- Pack fuente `ide/antigravity/` (rules, 7 subagentes, `AGENTS.md`, fuente de `/flow-*`).
- Instaladores `ide/install.sh`, `ide/install.ps1`, `FlowForgeModule` y `install-skills.sh`.
- Validadores: `AntigravityPackValidator`, `scripts/validate-antigravity-pack.sh`, `scripts/compat-antigravity-upgrade.sh`, `flowforge doctor`.
- Tests de regresión y CI.
- Documentación y ADRs.

**Excluido:**

- `flowforge uninstall` sin limpieza de `~/.gemini/config/` (preexistente).
- Contenido de `mcp_config.json` (0 bytes).
- Publicación del binario C# (clona `main`; el fix llega con merge/release).

---

## 4. Cambios

### 4.1 Pack (`ide/antigravity/`)

| Archivo | Cambio |
|---------|--------|
| `rules/*.md` | Frontmatter `alwaysApply` → `trigger:` válido |
| `rules/pr-workflow.md` | **Nuevo** (faltaba en el pack) |
| `agents/*.md` | **7 subagentes nuevos**: `flowforge`, `forge-discovery`, `forge-arch`, `forge-plan`, `forge-dev`, `forge-verify`, `forge-memory` (`name` + `description`, sin `tools:`) |
| `AGENTS.md` | Orquestador sin frontmatter |
| `workflows/flow-*.md` | Se mantienen como **fuente** de las command skills |

### 4.2 Instaladores

| Archivo | Cambio |
|---------|--------|
| `ide/install.sh` | Instala `AGENTS.md`, `rules/`, `agents/`, `skills/` (todas + 7 `flow-*`), `GEMINI.md` sin frontmatter; migra y elimina `global_workflows/`; limpia pack legacy; **fix de symlink colgante** |
| `ide/install.ps1` | Mismo contrato + mismo fix de symlink |
| `src/FlowForge.Installer/Modules/FlowForgeModule.cs` | `WriteCommandSkill` genera skills y reemplaza symlinks colgantes |
| `src/FlowForge.Installer/Infrastructure/PathHelper.cs` | Rutas global/workspace de Antigravity |
| `install-skills.sh` | Ruta legacy de Antigravity corregida + error de sintaxis preexistente |
| `install/dev/install-dev.sh` | Directorio de skills de Antigravity |

### 4.3 Validadores y doctor

| Archivo | Cambio |
|---------|--------|
| `Infrastructure/AntigravityPackValidator.cs` | Valida `trigger:`, 7 command skills, 7 subagentes, ausencia de legacy |
| `Commands/DoctorCommand.cs` | Checks `rules trigger`, `command skills`, `subagents`, `legacy` |
| `scripts/validate-antigravity-pack.sh` | Reescrito: prohíbe la clave de Cursor y exige el contrato completo |
| `scripts/compat-antigravity-upgrade.sh` | **Nuevo**: actualización desde instalaciones previas, idempotencia y no-regresión |
| `scripts/docker-pm1-test.sh` | Aserciones actualizadas al nuevo contrato |

### 4.4 Tests

- `tests/.../AntigravityPackValidatorTests.cs` — contrato del validador.
- `tests/.../Update/ReworkFixTests.cs` — `CopySkillsForIde_Antigravity_ReplacesDanglingSkillSymlink`.
- `tests/.../PathHelperTests.cs` — rutas globales/workspace.

### 4.5 Docs y dogfooding

- `docs/decisions/ADR-029-...md` (nuevo), ADR-008/011/019, `docs/decisions/INDEX.md`, README/README.es, `ide/README.md`, QUICKSTART (EN/ES).
- `.agents/rules/*` + `.agents/agents/*` actualizados (dogfood).
- `CHANGELOG.md` actualizado.

---

## 5. Criterios de aceptacion

1. **Dado** un pack instalado, **cuando** Antigravity carga `.agents/rules/*.md` o `~/.gemini/config/rules/*.md`, **entonces** toda regla declara `trigger:` ∈ {`always_on`, `model_decision`, `glob`, `manual`} y ninguna usa la clave inerte de Cursor.
2. **Dado** el agente en un workspace con FlowForge instalado, **cuando** se pregunta por CKP-0 o por la prohibición de `git push` sin permiso, **entonces** responde afirmativamente (las reglas se aplican).
3. **Dado** un workspace nuevo, **cuando** el usuario escribe `/flow-start`, **entonces** la skill existe en `~/.gemini/config/skills/flow-start/SKILL.md` y en `.agents/skills/flow-start/SKILL.md`, y los 7 `/flow-*` están disponibles.
4. **Dado** una instalación completa, **cuando** se inspecciona `~/.gemini/config/agents/`, **entonces** hay exactamente 7 subagentes invocables con frontmatter `name` + `description` y **sin** `tools:`.
5. **Dado** una instalación nueva, **cuando** termina, **entonces** no existe `global_workflows/` ni `~/.gemini/antigravity/`, y `GEMINI.md` no lleva frontmatter.
6. **Dado** un estado previo (reglas con la clave de Cursor, `global_workflows/`, pack legacy, `skills.json`, symlinks colgantes), **cuando** se actualiza, **entonces** no se pierde ningún artefacto ni archivo de usuario, se preservan workflows custom como skills y una segunda corrida no produce cambios.
7. **Dado** un pack inválido, **cuando** corre CI o `flowforge doctor`, **entonces** el fallo se reporta explícitamente (nunca en silencio).
8. **Dado** el árbol de trabajo, **cuando** corre la suite del instalador, **entonces** pasan 288 tests.

---

## 6. Verificacion

```bash
# Contrato medido con el CLI real
agy -p "<pregunta de control>"        # 4 errores -> 0; CKP-0 NO -> SÍ; git push NO -> SÍ
# skills visibles 1 -> 19 (12 metodología + 7 flow-*); subagentes 0 -> 7; /flow-start no -> sí

bash scripts/validate-antigravity-pack.sh
# Antigravity pack validation passed (rules with trigger:, 7 subagents, 7 workflow sources)

bash scripts/compat-antigravity-upgrade.sh
# ok x21 ... compat-antigravity-upgrade: all checks passed

dotnet test tests/FlowForge.Installer.Tests -c Release --nologo
# Superado: 288, Con error: 0, Omitido: 0, Total: 288
```

| Verificación | Resultado |
|--------------|-----------|
| `validate-antigravity-pack.sh` | ✅ EXIT 0 |
| `compat-antigravity-upgrade.sh` | ✅ EXIT 0, all checks passed |
| Suite del instalador | ✅ 288/288 (baseline 273) |
| `bash ide/install.sh` (global y proyecto) | ✅ EXIT 0 |
| `flowforge install --yes` (`FLOWFORGE_REPO` al árbol) | ✅ `trigger:` + 7 agentes + 19 skills, sin `global_workflows/` |
| `flowforge doctor` | ✅ `rules trigger ✓`, `command skills ✓`, `subagents ✓`, `legacy ✓` |

---

## 7. Compatibilidad con instalaciones previas

Medido con fingerprint (tipo + sha256/`readlink`) sobre todo el `$HOME` temporal, instalando con la versión anterior y actualizando con la nueva:

- **0 cambios** fuera de `~/.gemini` (Cursor, OpenCode, VS Code, Kilo y `.flowforge/shared` byte-idénticos).
- **0** skills `forge-*` preexistentes borradas.
- `global_workflows/flow-*.md` migrados a skills y eliminados; workflow **custom** (`flow-deploy-custom`) preservado como skill.
- Pack legacy `~/.gemini/antigravity/` + `skills.json` eliminados; symlinks colgantes reemplazados.
- 2ª corrida **idempotente** (0 cambios); `.cursor/rules/user.mdc` intacto.

Regresión detectada y corregida en este ciclo: un symlink colgante en `~/.gemini/config/skills/<name>` hacía fallar `mkdir -p` con `EEXIST` y abortaba el instalador a mitad de la migración.

Detalle: `.ai-work/hu-050-antigravity-customizations-contract/installer-baseline.md` (artefacto de sesión, no versionado — ADR-002).

---

## 8. Riesgos y pendientes

| Riesgo / pendiente | Estado |
|--------------------|--------|
| `ide/install.ps1` no se pudo parsear en este entorno (sin `pwsh`) | ⚠️ Validar en CI Windows |
| `flowforge uninstall` no limpia `~/.gemini/config/` (`AntigravitySidecarPath` nunca se escribe) | ⚠️ **Preexistente**, fuera de alcance |
| El binario C# clona `main`: el fix no llega a usuarios hasta merge/release | ℹ️ Probar con `FLOWFORGE_REPO` |
| `mcp_config.json` de 0 bytes puede romper el parser de Antigravity | ℹ️ Fuera de alcance |
| `ide/antigravity/workflows/` conserva el nombre histórico | ℹ️ Es fuente de `/flow-*`, no se lee como workflow |

---

## 9. Referencias

- [ADR-029](../../docs/decisions/ADR-029-antigravity-customizations-contract.md) — contrato de customizations (supersede el de ADR-019).
- [ADR-019](../../docs/decisions/ADR-019-opencode-antigravity-customizations.md) — rutas (vigentes).
- [ADR-008](../../docs/decisions/ADR-008-ide-installer-path-matrix.md) · [ADR-011](../../docs/decisions/ADR-011-ide-pack-parity-and-delivery.md) · [ADR-017](../../docs/decisions/ADR-017-installer-protection-policy.md)
- Docs oficiales: [rules](https://antigravity.google/docs/rules) · [skills](https://antigravity.google/docs/skills) · [subagents](https://antigravity.google/docs/subagents) · [workflows → skills](https://antigravity.google/docs/migration/workflows-to-skills)
- Spec / plan / verificación: `.ai-work/hu-050-antigravity-customizations-contract/` (artefacto de sesión, no versionado — ADR-002)
