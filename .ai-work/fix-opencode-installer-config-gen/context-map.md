# Context Map — fix-opencode-installer-config-gen

Fecha: 2026-07-13
Feature-slug: fix-opencode-installer-config-gen

## 1) Resumen ejecutivo
- Causa raíz: el instalador está fragmentado en dos flujos (scripts Bash/PS y el instalador C#). Uno copia los *packs* de agentes/plantillas; el otro sólo parchea la sección `mcp`. Ninguno genera o mantiene de forma canónica los bloques `agent` / `provider` / `permission` / `instructions` ni regenera `model-assignments.md` con modelos seguros. Esto produce rot (fixes que "re-rotten") cada vez que se actúa sólo en uno de los dos instaladores.

## 2) Confirmación de causa raíz (evidencia)
- El instalador Shell copia agentes y limpia legacy, pero no toca `opencode.json` salvo aviso de modelos:
```169:179:/home/victor/Documentos/Proyectos/Desarrollo Personal/FlowForge/ide/install.sh
# --- OpenCode ---
# OpenCode auto-loads agents from ~/.config/opencode/agents/*.md and commands
# from ~/.config/opencode/commands/*.md. No merge needed — just copy the files.
if [ -d "${HOME}/.config/opencode" ]; then
  ...
  cp "$IDE_DIR/opencode/agents/"*.md "${HOME}/.config/opencode/agents/" 2>/dev/null || true
```

- El módulo C# `EngramModule` únicamente mergea el bloque `mcp.engram` (no crea `agent`/`provider`/`permission`/`instructions`):
```162:172:/home/victor/Documentos/Proyectos/Desarrollo Personal/FlowForge/src/FlowForge.Installer/Modules/EngramModule.cs
    // OpenCode: merge into existing opencode.jsonc (or create opencode.json)
    if (Directory.Exists(Path.Combine(home, ".config", "opencode")))
    {
        MergeOpenCodeMcp(Path.Combine(home, ".config", "opencode"),
                         user, dataDir, syncEnabled);
    }
```

- `MergeOpenCodeMcp` implementa reemplazo/merge exclusivo del nodo `mcp` (líneas 230-241 muestran que sólo escribe `mcp`):
```230:241:/home/victor/Documentos/Proyectos/Desarrollo Personal/FlowForge/src/FlowForge.Installer/Modules/EngramModule.cs
            // Get or create mcp section as JsonObject
            if (node["mcp"] is not System.Text.Json.Nodes.JsonObject mcpNode)
            {
                mcpNode = new System.Text.Json.Nodes.JsonObject();
                node["mcp"] = mcpNode;
            }
            mcpNode["engram"] = engramNode;
            // Serialize back
            File.WriteAllText(configPath, json + Environment.NewLine);
```

- El módulo C# que instala packs copia archivos `ide/opencode/agents` y `ide/antigravity/rules` tal cual (no generan `model-assignments.md` dinámicamente):
```145:149:/home/victor/Documentos/Proyectos/Desarrollo Personal/FlowForge/src/FlowForge.Installer/Modules/FlowForgeModule.cs
        var ideAgentsSrc = Path.Combine(ffRepo, "ide", "opencode", "agents");
        CopyGlob(ideAgentsSrc, agentsDest, "*.md");

188:193:/home/victor/Documentos/Proyectos/Desarrollo Personal/FlowForge/src/FlowForge.Installer/Modules/FlowForgeModule.cs
        CopyGlob(Path.Combine(ideDir, "rules"), PathHelper.AntigravityRules, "*.md");
        CopyGlob(Path.Combine(ideDir, "workflows"), PathHelper.AntigravityWorkflows, "*.md");
```

## 3) Inventario — ¿qué escribe cada instalador VS qué omite?
- Bash installer (`ide/install.sh`)
  - Escribe: copia `ide/opencode/agents/*.md` → `~/.config/opencode/agents/` y `ide/opencode/commands/*.md` → `~/.config/opencode/commands/`. (véase 169–179, 181–183).  
  - También copia `ide/antigravity/rules/*.md` → `~/.gemini/config/rules/` / `~/.gemini/config/.agents/rules/` (líneas 110–113).
  - Omite: no genera ni actualiza bloques `agent`/`provider`/`permission`/`instructions` en `opencode.json`; sólo deja un aviso (línea 192).

- C# installer (`src/FlowForge.Installer`)
  - Escribe: copia idéntica de packs por IDE (funciones `InstallOpenCode`, `InstallAntigravity`, `CopyGlob`) — crea/respeta backups antes de overwrite. (véase 137–150 y 165–196 en `FlowForgeModule.cs`).
  - Parchea: `EngramModule.MergeOpenCodeMcp` sólo mergea/reescribe el subnodo `mcp` (no toca `agent`/`provider`/`permission`/`instructions`).
  - Omite: no genera un `opencode.json` con secciones completas de `agent`/`provider`/`permission`/`instructions`, ni regenera `model-assignments.md` con modelos distintos a la plantilla.

Efecto combinado: las plantillas (p. ej. `ide/opencode/opencode.json.example` y `ide/antigravity/rules/model-assignments.md`) se copian o se dejan en el sistema del usuario pero no hay una fuente única y canonical que el instalador actual escriba/regenere en `~/.config/opencode/opencode.json` ni en `.agents/rules/model-assignments.md`.

## 4) Plantilla / flujo de `model-assignments.md` (por qué filtran modelos Cursor/Antigravity)
- Origen plantilla: `ide/antigravity/rules/model-assignments.md` (repo), que contiene modelos de la familia Claude / GPT heredados de plantillas Cursor/Antigravity:
```10:18:/home/victor/Documentos/Proyectos/Desarrollo Personal/FlowForge/ide/antigravity/rules/model-assignments.md
| forge-discovery | `claude-4.5-haiku-thinking` | `gpt-5-mini` | pinned | Fast search, cheap |
| forge-arch | `claude-4.5-sonnet-thinking` | `gpt-5.2` | pinned | Reasoning + spec writing |
...
```
- Flujo de copia:
  - `install.sh` copia `ide/antigravity/rules/*.md` → `~/.gemini/...` y también a `~/.config/opencode/.agents/rules/` (líneas 110–113 y 176–179).  
  - `FlowForgeModule.InstallAntigravity()` copia las mismas reglas a `PathHelper.AntigravityRules` (líneas 188–193).
  - Resultado: la plantilla con referencias a `claude-*` y `gpt-5.*` se instala tal cual en el directorio del usuario y en OpenCode/Antigravity, sembrando referencias a modelos no disponibles en entornos sin claves.

## 5) Trabajo previo en `.ai-work/fix-ide-installer-packs/` — qué se intentó
- Artefactos leídos:
  - `.ai-work/fix-ide-installer-packs/context-map.md` (análisis de rutas y patrón, concluye reutilizar `FlowForgeModule` y `PathHelper`).
  - `.ai-work/fix-ide-installer-packs/spec.md` (especificación de FRs: FR-003, FR-009, FR-010) — se decidió no migrar modelos automáticamente por riesgo.
  - `summary.preview.md` (resumen): añade `ide/opencode/commands/` y corrige copy en `install.sh`/`install.ps1`; dejó pendientes tests CI y validación schema MCP.
- Conclusión del trabajo previo: se corrigieron rutas y añadieron `commands/` pero no se abordó la generación canónica de `opencode.json` (`agent/provider/permission/instructions`) ni la regeneración segura de `model-assignments.md` con un set limitado a modelos libres.

## 6) Auditoría PII / valores sensibles en artefactos (lo que NO debe incluir el instalador por defecto)
- Entradas detectadas que no deben baked-in:
  - Rutas absolutas de usuario:
    - `/home/victor/.local/bin/engram` (opencode.example: `mcp.engram.command`) — no incluir rutas de usuario.
    - `/home/victor/.engram` (ENGRAM_DATA_DIR)
    - entradas en `.agents/skills.json`: `"/home/victor/Documentos/Proyectos/Desarrollo Personal/FlowForge/skills"`
  - Identificadores/usuario local:
    - `ENGRAM_USER`: `victor@local.dev` (opencode.example)
  - Variables de entorno/API keys referenciadas en plantillas:
    - `OPENCODIGO_API_KEY` (opencode provider `opencode-go`).
    - `DEEPSEEK_API_KEY`, `MINIMAX_API_KEY` (presentes en provider blocks).
  - Hardcoded provider URLs / descriptions que sugieren suscripción pagada (`opencode-go` descripción `$10/mes`).
- Recomendación: el instalador por defecto debe escribir sólo plantillas sin valores concretos (usar placeholders `$HOME`, evitar rutas absolutas, omitir envs con nombres de usuario, NO escribir claves ni valores de cuentas).

## 7) Identificación de modelos OpenCode Zen gratuitos — estado
- He revisado las ADRs relevantes (ADR-006, ADR-008, ADR-009) y la documentación del repo (`docs/09-open-source-integration.md`) y los ejemplos (`ide/opencode/...`, `opencode.json.example`). El repositorio lista los modelos disponibles en `opencode-go` (p. ej. `qwen3.7-plus`, `deepseek-v4-flash`, `deepseek-v4-pro`, `minimax-*`, `kimi-*`, `glm-5.1` en `opencode.json.example`), pero NO hay una indicación canonizada en el repo sobre cuáles de esos son "gratuitos" en la oferta OpenCode Zen.

-> Estado: [BLOCKER] — para cumplir la restricción "solo modelos OpenCode Zen gratuitos" necesitamos la lista oficial de modelos gratuitos (o una policy interna que marque permitidos). Sin la referencia del proveedor OpenCode no podemos inferir con seguridad qué modelos son gratuitos sin arriesgar incluir modelos de pago o que requieran API keys.

## 8) Constraints / Stakeholders
- Requisito expreso del stakeholder: arreglar el instalador en el código fuente (no solo parchear ~/.config/opencode local) y garantizar que los artefactos por defecto usen únicamente modelos OpenCode Zen gratuitos y NO contengan PII ni claves.
- Reglas operativas: no escribir rutas absolutas de usuario, no incluir claves en plantillas, respetar backups antes de overwrite.

## 9) Acción recomendada (alta prioridad)
1. Decidir lista de modelos OpenCode Zen permitidos (input humano o referencia oficial OpenCode). (Necesario para desbloquear).
2. Implementar en C# Installer (FlowForgeModule / EngramModule):
   - a) Un generador canónico `opencode.json` / `opencode.jsonc` que, además de `mcp`, escriba/regenere `agent`/`provider`/`permission`/`instructions` usando plantillas sin PII y con los modelos permitidos.
   - b) Un generador/regenerador para `.agents/rules/model-assignments.md` que sustituya la plantilla `ide/antigravity/rules/model-assignments.md` por una versión filtrada solo con modelos permitidos (y con comentarios sobre cómo personalizar).
3. Hacer paridad con `install.sh`/`install.ps1` para que ambos caminos utilicen la misma función central (extraer lógica a `PathHelper`/manifest generator) o invocar el instalador C# para la parte de configuración JSON.
4. Añadir validación schema en `flowforge doctor` para `opencode.json` (asegurar `mcp.engram.type == "local"` y `mcp.engram.enabled == true`) y validar ausencia de PII/keys en `agent`/`provider` defaults.

## 10) Reusable Patterns Found (MANDATORY)
- `src/FlowForge.Installer/Modules/FlowForgeModule.cs` — patrón "Instalador por IDE": `InstallOpenCode`, `InstallAntigravity`, `CopyGlob` (reusar; parche, no rediseño). (véase: FlowForgeModule.cs 137–149 / 188–193).
- `src/FlowForge.Installer/Modules/EngramModule.cs` — patrón de merge `mcp` (reutilizable para un merge-canónico más amplio). (véase: EngramModule.cs 162–172 / 230–241).
- `ide/install.sh` / `ide/install.ps1` — orquestadores shell/ps que ya implementan la copia de packs (evitar duplicar lógica).

## 11) FlowDoc / HU context
- PRD: docs/PRD.md (no presente en repo) — no leído.
- HU referenciado: none.

## Memory Signal
- decision: Centralizar generación de `opencode.json` y `model-assignments.md` en el instalador C# y exponer opción de plantilla sin PII ni keys.
- constraint: No incluir rutas absolutas de usuario ni API keys en defaults; usar solo modelos OpenCode Zen que el proyecto defina como "gratuitos" (se requiere lista).
- blocker: Falta lista autorizada de modelos OpenCode Zen gratuitos (prov. OpenCode or responsable de producto).

---  
Archivo escrito en: `.ai-work/fix-opencode-installer-config-gen/context-map.md`

