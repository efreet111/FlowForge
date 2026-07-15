# ADR-008: IDE installer path matrix

**Estado:** Accepted  
**Fecha:** 2026-07-03  
**Autores:** FlowForge Installer crew  

## Contexto

La feature `fix-ide-installer-packs` identificó ambigüedades en los destinos donde FlowForge copia los packs de agents. Los usuarios confunden rutas globales (por IDE) con estructuras heredadas y con Claude Desktop (Anthropic), lo que genera instalaciones fallidas y diagnósticos incorrectos del comando `flowforge doctor`. Necesitamos un mapa canónico que describa claramente la ruta global, la ruta de proyecto y cómo detectar cada IDE.

## Decisión

Adoptamos la siguiente matriz canónica en la que cada IDE tiene rutas globales, rutas locales y criterios de detección únicos. `FlowForgeModule` y los scripts `install.sh`/`install.ps1` deben seguirla y cualquier documentación (README, ADR, scripts) debe reflejarla.

| IDE | Global agents | Project agents | Detección |
|-----|---------------|----------------|-----------|
| Cursor | `~/.cursor/agents/` + `rules/` + `commands/` | `.cursor/agents/` + `rules/` + `commands/` | Presencia de `~/.cursor` |
| OpenCode | `~/.config/opencode/agents/` + `commands/` | `.opencode/agents/` | `~/.config/opencode` |
| GitHub Copilot | `~/.copilot/agents/*.agent.md` + `instructions/flowforge.instructions.md` | `.github/agents/*.agent.md` + `copilot-instructions.md` | Extensión `github.copilot*` en `~/.vscode/extensions/` |
| Kilo Code | `~/.config/kilo/agents/*.md` (replicado desde OpenCode) | `.kilo/agents/*.md` (duplicado desde `.opencode/agents/`) | Extensión `kilocode.*` en `~/.vscode/extensions/` |
| Antigravity (Google) | `~/.gemini/config/` con `AGENTS.md`, `rules/`, `workflows/`, `skills/` y `mcp_config.json` | `.agents/rules/`, `.agents/workflows/`, `.agents/skills/`, `AGENTS.md` | `~/.gemini` — ver [ADR-009](ADR-009-opencode-antigravity-customizations.md) para Antigravity 2.0 |
| Claude Desktop (Anthropic) | `~/.config/Claude/claude_desktop_config.json` (MCP manual) | — | Presencia de `%APPDATA%\Claude` o `~/.config/Claude/` |

## Consecuencias

- `FlowForgeModule`, los scripts de instalación y `flowforge doctor` usan esta matriz para decidir dónde escribir y qué reportar.
- Antigravity ya no se documenta como `Claude Desktop`; el instalador distingue ambos y ofrece guía para Claude Desktop en el MCP manual.
- Cualquier nueva ruta debe reflejarse aquí antes de tocar código o documentación.
