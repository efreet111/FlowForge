# Instalador OpenCode — FlowForge

## Arquitectura y SSOT
- Las plantillas canon están en `ide/opencode/templates/` (opencode.json.tpl, model-assignments.md.tpl, agents, manifest, managed-paths).
- El generador C# (`OpenCodeConfigGenerator`) y el script bash (`ide/opencode/generate-config.sh`) consumen el mismo manifiesto `agent-models.json` y la lista `managed-paths.json`.
- El merge es no destructivo: `.flowforge-managed.json` registra los JSON-paths que FlowForge controla para no sobrescribir customizaciones.

## Flujo de instalación
1. `flowforge install --ide opencode --yes` o `bash ide/opencode/generate-config.sh <FLOWFORGE_REPO>` crea backups en `~/.flowforge-backups/`.
2. Antes de escribir, se escanea el JSON con `PiiScanner` para evitar patrones prohibidos.
3. Se genera/mergea `opencode.json` (config, provider opencode-zen, agentes con prompts), `model-assignments.md` y `.flowforge-managed.json`.
4. Se copian `agents/*.md` desde las plantillas, parcheando `model: opencode-zen/...` y se sincronizan los `commands`.

## Comandos relevantes
- `flowforge install --ide opencode --yes` — flujo principal C# (acepta `--force-free`, `--dry-run`, `--json-only`, `--allow-symlink`).
- `flowforge doctor` — valida schema, mcp, PII, model-assignments y agents.
- `bash ide/opencode/generate-config.sh <FLOWFORGE_REPO>` — alternativa bash (requiere `jq`, `envsubst`).

## Troubleshooting
- **Falta sección `agent`** → corre `flowforge install --ide opencode --yes` para regenerar.
- **model-assignments desactualizado (claude-/gpt-/opencode-go/)** → vuelve a instalar y genera el archivo desde el provider opencode-zen.
- **PII detectada** → actualiza las plantillas o elimina commits con `git filter-repo`; no se escribe el config.

## Migraciones y compatibilidad
- Instalaciones viejas solo tenían `mcp.engram`. El nuevo flujo escribe `instructions`, `agent`, `permission`, `provider` y `mcp` seguras.
- `flowforge doctor` detecta si el sidecar falta o si `mcp.engram` ya fue actualizado y recomienda reinstall.
- Las antiguas plantillas `.example` se movieron a `ide/opencode/templates/`; ya no se usan en `.config/opencode/`.

## Dependencias
- **C#**: .NET 8 (FlowForge installer) para `OpenCodeConfigGenerator` y `DoctorCommand`.
- **Bash**: `jq`, `envsubst` para `generate-config.sh`, y `bash` libs (`pii-scan`, `sudo-guard`, `atomic-write`, `install-log`).
