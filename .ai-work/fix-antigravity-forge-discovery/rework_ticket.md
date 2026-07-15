---
feature_slug: fix-antigravity-forge-discovery
status: resolved
cycle_count: 1
severity: P0
created: 2026-07-15
source: human-pm after manual install
---

# Rework ticket — Antigravity global workflows path wrong

## Expected

Tras `flowforge install` / `ide/install.sh` / `ide/install.ps1`, los 7 `/flow-*` aparecen en Antigravity IDE 2.1 al escribir `/` o en Customizations → Workflows, **sin copia manual**.

## Actual

Los workflows se instalaban en `~/.gemini/config/workflows/`. Antigravity 2.1 (confirmado en máquina del usuario + [docs Atamel 2026-07-13](https://atamel.dev/posts/2026/07-13_where_agy_rules_workflows/)) lee globales desde:

```text
~/.gemini/config/global_workflows/
```

Resultado: frontmatter correcto pero picker vacío. Workaround manual: copiar a `global_workflows/` → flujos visibles.

## Steps to reproduce

1. Instalar FlowForge Antigravity pack (escribe en `config/workflows/`).
2. Abrir Antigravity IDE en un workspace **sin** `.agents/workflows/` (p. ej. otro proyecto).
3. Escribir `/` → no aparecen `flow-*`.
4. Copiar los mismos `.md` a `~/.gemini/config/global_workflows/` → aparecen.

## Evidence

- Instalados (previo): `~/.gemini/config/workflows/flow-*.md` con FM, no listados.
- Canónico actual: `~/.gemini/config/global_workflows/`.
- Workspace: project workflows siguen en `{repo}/.agents/workflows/` (eso no cambia).
- ADR-009 documenta ruta obsoleta `config/workflows/`.

## Environment

- OS: Linux (CachyOS)
- Antigravity IDE 2.1.x (`user-data-dir=~/.config/Antigravity IDE`)
- Branch: `feat/fix-antigravity-forge-discovery` / PR #10

## Fix scope (dev)

1. Cambiar destino global de workflows a `PathHelper` → `AntigravityConfigDir/global_workflows` (C#, `install.sh`, `install.ps1`).
2. Migración: al instalar, copiar/mover desde `config/workflows/` legacy → `global_workflows/` y documentar cleanup opcional de legacy.
3. Actualizar ADR-009, AGENTS.md, doctor/CI/`validate-antigravity-pack` asserts de destino instalado si aplica.
4. Mantener proyecto en `.agents/workflows/` (sin cambio).
5. Tests unitarios del path / doctor checks.
