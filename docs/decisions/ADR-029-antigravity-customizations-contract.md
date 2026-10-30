# ADR-029: Antigravity customizations contract (rules `trigger:`, `/flow-*` como skills, subagentes)

> **Status**: Accepted  
> **Date**: 2026-10-29  
> **Feature**: `hu-050-antigravity-customizations-contract`  
> **Links**: [ADR-008](ADR-008-ide-installer-path-matrix.md) · [ADR-011](ADR-011-ide-pack-parity-and-delivery.md) · [ADR-017](ADR-017-installer-protection-policy.md) · [ADR-019](ADR-019-opencode-antigravity-customizations.md)  
> **Docs oficiales**: [rules](https://antigravity.google/docs/rules) · [skills](https://antigravity.google/docs/skills) · [subagents](https://antigravity.google/docs/subagents) · [migration: workflows → skills](https://antigravity.google/docs/migration/workflows-to-skills)  
> **Supersede**: el **contrato de customizations de Antigravity de [ADR-019](ADR-019-opencode-antigravity-customizations.md)**. Las **rutas** que fija ADR-019 (raíz global `~/.gemini/config/` vs. workspace `.agents/`) **siguen vigentes**.

---

## Contexto

FlowForge publicaba un pack de Antigravity (`ide/antigravity/`) que **Antigravity descartaba en silencio**. La integración estaba 100% rota: el agente nunca vio las reglas, nunca tuvo el orquestador `flowforge` y nunca pudo invocar `/flow-*`. Cuatro fallos independientes, medidos contra la documentación oficial y contra el CLI real (`agy` 1.4.10, mismo workspace, `agy -p`, sin tools):

1. **Rules con la clave de Cursor.** El pack escribía frontmatter `alwaysApply: true`. Antigravity exige `trigger:` ∈ {`always_on`, `model_decision`, `glob`, `manual`} en `.agents/rules/*.md` y `~/.gemini/config/rules/*.md`. Sin `trigger:` válido la regla se **descarta en silencio**. Evidencia: `E rules.go:482] Invalid rule trigger: CORTEX_MEMORY_TRIGGER_UNSPECIFIED` **× 4** (una por archivo de regla), y las preguntas de control “¿las instrucciones mencionan CKP-0?” / “¿la regla prohíbe `git push` sin permiso?” respondían **NO**.
2. **`/flow-*` entregados como workflows legacy.** Los workflows legacy dejaron de indexarse y de ejecutarse con `/` el **2026-10-19** ([migración oficial](https://antigravity.google/docs/migration/workflows-to-skills)). El pack seguía escribiendo `global_workflows/` y `.agents/workflows/`; `/flow-start` simplemente **no existía** para el agente.
3. **Ni skills ni subagentes.** El instalador copiaba sólo `skills/forge-*`, así que la skill orquestadora `flowforge` **nunca** se instalaba, y no se escribía ningún subagente nativo. Medido: **1** skill visible y **0** subagentes invocables.
4. **`GEMINI.md` con frontmatter.** Se copiaba el cuerpo de una regla con su YAML; `AGENTS.md`/`GEMINI.md` no llevan frontmatter (el archivo completo es `always_on`), por lo que el bloque se interpretaba como contenido.

Además, el baseline de instalación (medido sobre `5c384ae`) mostró que el camino shell estaba **roto antes de llegar a Antigravity**: `bash ide/install.sh` salía con **EXIT 1** por `FileNotFoundError: skills/forge-orchestrator/SKILL.md` en `ide/cursor/compile-agents-from-skills.py` y, con `set -e`, abortaba antes de instalar OpenCode, Antigravity, VS Code y Kilo. El camino C# (`flowforge install --yes`) salía 0 pero instalaba un pack inerte.

---

## Decision drivers

- **Verificabilidad**: el contrato debe poder comprobarse contra la documentación oficial y contra el binario real, no contra suposiciones.
- **Fail-loud en el repo, fail-safe en el usuario**: los validadores deben detectar un pack inválido antes de publicarlo; el instalador nunca debe abortar a mitad de una migración.
- **Paridad entre IDEs** (ADR-011): los cuatro packs exponen los mismos `/flow-*`, reglas y agentes; cambia el mecanismo, no la funcionalidad.
- **Compatibilidad de actualización** (ADR-017): una instalación previa no puede perder artefactos ni archivos de usuario.
- **Una sola fuente de contenido**: `skills/` y `ide/antigravity/workflows/*.md` siguen siendo la fuente; Antigravity recibe artefactos derivados.

---

## Decisión

### 1. Rules: `trigger:` obligatorio, nunca la clave inerte de Cursor

Todo `.md` en `ide/antigravity/rules/`, `.agents/rules/` y `~/.gemini/config/rules/` lleva frontmatter YAML con `trigger:` ∈ {`always_on`, `model_decision`, `glob`, `manual`}. `model_decision` exige además `description`. La clave `alwaysApply` es **inerte en Antigravity** y provoca el descarte silencioso de la regla; queda prohibida en el pack y en los validadores.

```yaml
---
trigger: always_on
description: Una sola línea
---
```

`AGENTS.md` y `GEMINI.md` **no llevan frontmatter**: el archivo completo es `always_on`. `GEMINI.md` se genera desde el cuerpo de `rules/workflow.md` con el frontmatter eliminado.

### 2. `/flow-*`: skills generadas, no workflows legacy

Los slash commands se entregan como **skills**:

- global: `~/.gemini/config/skills/<name>/SKILL.md`
- workspace: `.agents/skills/<name>/SKILL.md`

Se generan desde `ide/antigravity/workflows/flow-*.md` (que sigue siendo la fuente de contenido, con `description` en una sola línea) a un `SKILL.md` con `name:` + `description:` — el picker de `/` no encuentra la skill si falta el `name:`. Las skills tienen **precedencia** sobre workflows legacy homónimos.

El instalador migra `global_workflows/flow-*.md` y `.agents/workflows/flow-*.md` a skills y elimina los `.md` migrados; un workflow **custom** del usuario se preserva como skill (no se borra). No se escribe `global_workflows/` en ninguna instalación nueva.

### 3. Subagentes nativos

Se entregan **7** subagentes: `flowforge`, `forge-discovery`, `forge-arch`, `forge-plan`, `forge-dev`, `forge-verify`, `forge-memory`.

- global: `~/.gemini/config/agents/<name>.md`
- workspace: `.agents/agents/<name>.md`

Frontmatter mínimo `name` + `description` (obligatorios), opcionales `subagent`, `mainAgent` (sólo en `flowforge`) y `model` ∈ {`inherit`, `flash`, `pro`}. **Nunca** se declara `tools:`: un nombre de tool no mapeado **cuelga** el subagente.

### 4. Artefactos del instalador

Ambos caminos (`ide/install.sh` / `ide/install.ps1` y `flowforge install`) escriben el mismo pack:

| Destino | Contenido |
|---------|-----------|
| `~/.gemini/config/` | `AGENTS.md`, `rules/`, `agents/` (7), `skills/` (todas las de metodología + 7 `flow-*`), `mcp_config.json` |
| `~/.gemini/config/.agents/` | espejo para cuando el usuario abre Antigravity con workspace en `~/.gemini/config/` |
| `~/.gemini/GEMINI.md` | cuerpo de `rules/workflow.md` sin frontmatter |
| `<proyecto>/.agents/` | `rules/`, `agents/`, `skills/`, `AGENTS.md` |
| `<proyecto>/AGENTS.md` | copia del orquestador (si no existe otro) |

**No** se escribe `global_workflows/` ni `~/.gemini/antigravity/` (pack legacy, eliminado al reinstalar junto con `config/skills.json`).

### 5. Validadores

- `src/FlowForge.Installer/Infrastructure/AntigravityPackValidator.cs` + `flowforge doctor`: verifican `trigger:` (nunca la clave de Cursor), las 7 command skills, los 7 subagentes y la ausencia de restos legacy. Doctor reporta `rules trigger ✓`, `command skills ✓`, `subagents ✓`, `legacy ✓`.
- `scripts/validate-antigravity-pack.sh` afirma el mismo contrato sobre el **pack fuente** (CI).
- `scripts/compat-antigravity-upgrade.sh` verifica la actualización desde instalaciones previas (CI), con fingerprint de todo el `$HOME`.

---

## Alternativas rechazadas

| Alternativa | Por qué se rechaza |
|-------------|--------------------|
| **Seguir entregando `/flow-*` como workflows** | Muertos desde 2026-10-19: no se indexan ni se ejecutan. Mantenerlos deja el `/flow-start` inexistente y añade ruido de diagnóstico. |
| **Mantener `alwaysApply` “por compatibilidad”** | No hay compatibilidad que preservar: Antigravity no lo lee y descarta la regla en silencio (`CORTEX_MEMORY_TRIGGER_UNSPECIFIED`). Escribirlo es indistinguible de no instalar la regla. |
| **Usar `skills.json` como registry** | La documentación oficial de skills usa **directorios** `skills/<name>/SKILL.md`; el registry no es el mecanismo. |
| **Declarar `tools:` en los subagentes** | Un nombre de tool no mapeado **cuelga** el subagente (fallo observado). Se omite por diseño; los permisos viven en las reglas. |
| **Copiar sólo skills `forge-*`** | Deja fuera la skill orquestadora `flowforge`; el orquestador nunca existía para el agente. |

---

## Consecuencias

### Positivas

- La integración de Antigravity **funciona de verdad**: reglas aplicadas, 19 skills visibles, `/flow-*` disponibles y 7 subagentes invocables (medido antes/después en la sección Verificación).
- El fallo es **detectable en CI** (`validate-antigravity-pack.sh`) y en el usuario (`flowforge doctor`), en vez de silencioso.
- La actualización preserva el trabajo previo: 0 cambios fuera de `~/.gemini`, 0 skills `forge-*` borradas, workflows custom conservados.
- Se elimina deuda colateral: `ide/install.sh` ya no aborta en Cursor, `install-skills.sh` recupera su sintaxis y apunta a la ruta correcta de Antigravity.

### Negativas / aceptadas

- `ide/antigravity/workflows/` sigue existiendo como **fuente** de `/flow-*`; el nombre es histórico y no implica que Antigravity los lea como workflows.
- El número de skills instaladas crece (19 visibles) porque ahora se copian todas las de metodología, no sólo `forge-*`.
- `flowforge uninstall` sigue sin limpiar `~/.gemini/config/`: gap **preexistente** (`AntigravitySidecarPath` no lo escribe ningún camino de instalación), fuera de alcance.
- `mcp_config.json` de 0 bytes puede romper el parser de Antigravity: fuera de alcance, se documenta como pendiente.

---

## Verificación

Medición empírica con `agy` 1.4.10 en el mismo workspace (`agy -p`, sin tools), antes → después:

| Señal | Antes | Después |
|-------|-------|---------|
| `Invalid rule trigger` | **4** | **0** |
| “instrucciones mencionan CKP-0” | NO | **SÍ** |
| “la regla prohíbe `git push` sin permiso” | NO | **SÍ** |
| Skills visibles | **1** | **19** (12 de metodología, incl. `flowforge`, + 7 `flow-*`) |
| `/flow-start` | no disponible | **disponible** |
| Subagentes invocables | **0** | **7** |

Compatibilidad old → new (fingerprint = tipo + sha256/`readlink` de todo el `$HOME` temporal): **0 cambios fuera de `~/.gemini`** (Cursor, OpenCode, VS Code, Kilo y `.flowforge/shared` byte-idénticos), 0 skills `forge-*` preexistentes borradas, `global_workflows/flow-*.md` migrados a skills, workflow custom preservado, pack legacy `~/.gemini/antigravity/` + `skills.json` eliminados, symlinks colgantes reemplazados, 2ª corrida idempotente (0 cambios), archivos de usuario intactos.

Regresión encontrada y corregida durante esa prueba: un symlink colgante en `~/.gemini/config/skills/<name>` hacía fallar `mkdir -p` con `EEXIST` y abortaba el instalador a mitad de la migración. Corregido en `ide/install.sh`, `ide/install.ps1` y `FlowForgeModule.WriteCommandSkill`, con test `CopySkillsForIde_Antigravity_ReplacesDanglingSkillSymlink`.

Comandos reproducibles:

```bash
bash scripts/validate-antigravity-pack.sh
bash scripts/compat-antigravity-upgrade.sh
dotnet test tests/FlowForge.Installer.Tests -c Release --nologo   # 288 pasando
```

---

## Referencias

- [ADR-019](ADR-019-opencode-antigravity-customizations.md) — rutas reales de OpenCode y Antigravity (vigentes); su contrato de customizations queda superseded por este ADR.
- [ADR-008](ADR-008-ide-installer-path-matrix.md) — matriz de rutas por IDE.
- [ADR-011](ADR-011-ide-pack-parity-and-delivery.md) — paridad de packs y entrega de skills.
- [ADR-017](ADR-017-installer-protection-policy.md) — política de protección del instalador (baseline + tests antes de tocar el instalador).
- Baseline y ciclo de vida medidos: [`.ai-work/hu-050-antigravity-customizations-contract/installer-baseline.md`](../../.ai-work/hu-050-antigravity-customizations-contract/installer-baseline.md)
- Docs oficiales: [rules](https://antigravity.google/docs/rules) · [skills](https://antigravity.google/docs/skills) · [subagents](https://antigravity.google/docs/subagents) · [workflows → skills](https://antigravity.google/docs/migration/workflows-to-skills)
