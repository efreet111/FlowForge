# ADR-011: OpenCode commands y Antigravity customizations (rutas reales)

> **Status**: Accepted  
> **Date**: 2026-07-04  
> **Feature**: `fix-ide-installer-packs`  
> **Links**: [ADR-008](ADR-008-ide-installer-path-matrix.md) · [ide/opencode/AGENTS.md](../../ide/opencode/AGENTS.md) · [ide/antigravity/AGENTS.md](../../ide/antigravity/AGENTS.md)

---

## Contexto

Durante pruebas manuales (jul 2026) los usuarios veían **agents** de FlowForge en OpenCode y Antigravity, pero **no** los slash commands `/flow-*` ni customizations completas. El instalador copiaba archivos a rutas que el código asumía correctas (ADR-008 v1), pero:

1. **OpenCode:** existía `ide/opencode/agents/` pero **no** `ide/opencode/commands/` → `~/.config/opencode/commands/` quedaba vacío.
2. **Antigravity:** el pack se escribía en `~/.gemini/antigravity/`, ruta que **Antigravity 2.0 no escanea** para Customizations. La raíz real es `~/.gemini/config/` ([docs oficiales](https://antigravity.google/docs/skills)).
3. **Skills:** confusión entre *agents IDE* (markdown por herramienta) y *skills* (metodología en `skills/forge-*/SKILL.md`). OpenCode no copia skills al pack; Antigravity sí debe enlazarlos en `config/skills/`.

Validación cruzada: spec generado por el propio Antigravity IDE (`antigravity_installer_spec.md`) + documentación en [antigravity.google/docs/mcp](https://antigravity.google/docs/mcp).

---

## Decision drivers

- Los slash commands deben aparecer al escribir `/` en OpenCode y Antigravity sin pasos manuales extra.
- Rutas deben alinearse con documentación oficial Antigravity 2.0 (`~/.gemini/config/`).
- No confundir `~/.gemini/antigravity/` (legacy / built-ins IDE) con customizations globales.
- Skills de metodología viven en **un solo lugar** en el repo (`skills/`); cada IDE los expone distinto.
- Limpiar instalaciones legacy para no diagnosticar carpetas obsoletas.

---

## OpenCode — decisión

### Qué instala FlowForge

| Artefacto | Fuente repo | Global | Proyecto |
|-----------|-------------|--------|----------|
| Agents | `ide/opencode/agents/*.md` | `~/.config/opencode/agents/` | `.opencode/agents/` |
| Commands `/flow-*` | `ide/opencode/commands/*.md` | `~/.config/opencode/commands/` | `.opencode/commands/` |
| Skills metodología | `skills/forge-*/SKILL.md` | **No se copian** | **No se copian** (quedan en raíz del repo) |

### Formato commands

Cada `flow-*.md` en `ide/opencode/commands/`:

```markdown
---
description: Texto corto para el picker
agent: flowforge
---

Prompt enviado al agente orquestador. Usar $ARGUMENTS donde aplique.
```

El nombre de archivo define el comando: `flow-start.md` → `/flow-start`.

### Skills — por qué no están en `.opencode/`

Los agents OpenCode **referencian** skills por ruta relativa (`skills/forge-dev/SKILL.md`). Eso funciona cuando OpenCode arranca **en la raíz del clone FlowForge**. A diferencia de Cursor (skills embebidos en agents compilados) o Antigravity (symlinks en `config/skills/`), OpenCode **no incluye** carpeta `skills/` en su pack IDE.

**Implicación:** en proyectos ajenos a FlowForge, los subagentes pueden carecer de SKILL.md a menos que el workspace incluya el clone o se configuren rutas absolutas en `opencode.json`.

**Follow-up opcional:** symlink `skills/forge-*` → `.opencode/skills/` o global equivalente (pendiente verificar API OpenCode).

### Uso

1. Elegir agente **`flowforge`** (no `build`).
2. Escribir **`/`** → listar `flow-start`, etc.
3. MCP Engram: `opencode.json` con `mcp.engram.type = "local"` (ver ADR-006).

---

## Antigravity — decisión

### Rutas canónicas (Antigravity 2.0)

| Artefacto | Global | Workspace (proyecto) |
|-----------|--------|---------------------|
| Orquestador | `~/.gemini/config/AGENTS.md` | `.agents/AGENTS.md` (o `AGENTS.md` raíz si no existe otro) |
| Rules | `~/.gemini/config/rules/` + `~/.gemini/GEMINI.md` (always-on) | `.agents/rules/` |
| Workflows `/flow-*` | `~/.gemini/config/global_workflows/` | `.agents/workflows/` |
| Skills | `~/.gemini/config/skills/forge-*/` (symlink → repo) | `.agents/skills/forge-*/` |
| MCP Engram | `~/.gemini/config/mcp_config.json` | `.agents/mcp_config.json` |

**Rechazado:** `~/.gemini/antigravity/` como destino de customizations FlowForge. El instalador **elimina** el pack legacy (`AGENTS.md`, `rules/`, `workflows/` bajo `antigravity/`) al reinstalar.

**Obsoleto (Antigravity 2.0):** `~/.gemini/config/workflows/` — Antigravity 2.1+ escanea `config/global_workflows/`. El instalador migra `flow-*.md` desde la ruta legacy al instalar; `flowforge doctor` advierte si queda contenido en `config/workflows/`.

**Rechazado:** `skills.json` con `"entries": [{ "path": "..." }]`. La [documentación oficial de skills](https://antigravity.google/docs/skills) usa **directorios** `config/skills/<nombre>/SKILL.md`, no un registry JSON.

### Formato workflows

Frontmatter YAML obligatorio (description en **una sola línea**, sin `>` multilínea):

```yaml
---
description: Iniciar feature FlowForge (Discovery a Spec)
---
```

Sin frontmatter, el parser de `/` puede devolver 0 resultados ([forum Google](https://discuss.ai.google.dev/t/antigravity-ide-slash-commands-workflows-disappear-entirely-no-results-due-to-4-fatal-parser-exceptions/135370)).

`rules/workflow.md` lleva `alwaysApply: true` en frontmatter.

### Workspace = `~/.gemini/config/`

Si el usuario abre Antigravity con workspace en `~/.gemini/config/` (común), el instalador también escribe espejo en `config/.agents/{rules,workflows,skills}/`.

### Uso

1. Reiniciar Antigravity tras instalar.
2. Customizations → Workflows debe listar `flow-start`, etc.
3. MCP vacío (`mcp_config.json` 0 bytes) puede romper el parser — ejecutar `flowforge install` para Engram o configurar MCP manualmente.

---

## Matriz comparativa skills vs agents

| Capa | Ubicación en repo | Cursor | OpenCode | Antigravity |
|------|-------------------|--------|----------|-------------|
| **Skills** (metodología) | `skills/forge-*/SKILL.md` | Embebidos en agents compilados | Referencia relativa desde repo | Symlink en `config/skills/` |
| **Agents IDE** | `ide/*/agents/` | `~/.cursor/agents/` | `~/.config/opencode/agents/` | N/A (usa AGENTS.md + rules) |
| **Slash commands** | `ide/*/commands/` o `workflows/` | `~/.cursor/commands/` | `~/.config/opencode/commands/` | `config/global_workflows/` o `.agents/workflows/` |

---

## Consecuencias

**Positivas:**

- OpenCode y Antigravity muestran `/flow-*` tras `ide/install.sh` o `flowforge install`.
- ADR-008 matriz actualizada; doctor y CI verifican `~/.gemini/config/`.
- Documentación evita repetir el error `antigravity/` vs `config/`.

**Negativas / aceptadas:**

- OpenCode fuera del repo FlowForge no tiene skills locales (gap conocido).
- Instalación remota `curl | bash` necesita cache persistente para symlinks Antigravity (skills apuntan al clone).
- ADR-008 fila Antigravity queda resumida; detalle en este ADR.

**Archivos tocados:**

- `ide/opencode/commands/` (nuevo pack)
- `ide/antigravity/workflows/` (frontmatter)
- `src/FlowForge.Installer/Modules/FlowForgeModule.cs`, `PathHelper.cs`, `EngramModule.cs`
- `ide/install.sh`, `ide/install.ps1`
- `.github/workflows/test-installer.yml`, `scripts/docker-pm1-test.sh`

---

## Verificación rápida

```bash
# OpenCode
ls ~/.config/opencode/commands/flow-start.md
opencode agent list | grep flowforge

# Antigravity
ls ~/.gemini/config/global_workflows/flow-start.md
ls ~/.gemini/config/skills/forge-dev
test ! -f ~/.gemini/antigravity/AGENTS.md && echo "legacy OK"
```
