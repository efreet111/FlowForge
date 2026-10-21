# ADR-011: VS Code prompts pack (slash commands `/flow-*`)

**Estado:** Accepted — *promovido 2026-10-20 por forge-memory tras verdict PASS*  
**Fecha:** 2026-10-20  
**Autores:** Orquestador FlowForge (a partir de reporte de usuario en Linux+Flatpak)  
**Relacionado con:** [ADR-008](ADR-008-ide-installer-path-matrix.md), [HU-040](../tasks/HU-001-HU-099/HU-040-vscode-slash-commands-missing.md), [HU-038](../tasks/HU-001-HU-099/HU-038-opencode-commands-missing.md)

## Contexto

Al armar la paridad IDE en el commit `cdd5c79` (Jun 2026) — `chore(ide): parity v0.4 + installers` — se crearon tres packs de slash commands:

- `ide/cursor/commands/` (Cursor lee de ahí)
- `ide/antigravity/workflows/` (Antigravity)
- `ide/opencode/commands/` (posteriormente, fix `HU-038`)

El cuarto IDE soportado — **Visual Studio Code + GitHub Copilot Chat** — recibe `agents/` (con sufijo `.agent.md`) e `instructions/` (frontmatter `applyTo`), pero **nunca** recibió su carpeta análoga de prompts. VS Code Copilot Chat busca slash commands en `~/.config/Code/User/prompts/*.prompt.md` (user-scope) o `<project>/.github/prompts/*.prompt.md` (workspace-scope).

Resultado: usuarios con VS Code (especialmente Flatpak) reportan que `/flow-start`, `/flow-plan`, etc. "no funcionan", aunque el orquestador sí responde por lenguaje natural (las instructions cargan, los agents no).

Este ADR **modifica** la matriz canónica de [ADR-008](ADR-008-ide-installer-path-matrix.md) agregando la fila de prompts faltante para GitHub Copilot y para los escenarios de detección (Linux nativo, Flatpak, workspace-scope).

## Decisión

### 1. Nueva fila en la matriz de ADR-008

| IDE | Global commands | Project commands | Detección |
|-----|-----------------|------------------|-----------|
| GitHub Copilot | `~/.config/Code/User/prompts/*.prompt.md` (Linux nativo) **o** `~/.var/app/com.visualstudio.code/config/Code/User/prompts/*.prompt.md` (Flatpak) | `<proyecto>/.github/prompts/*.prompt.md` | Extensión `github.copilot*` en `~/.vscode/extensions/` o en el sandbox Flatpak |

### 2. Pack fuente canónico

Crear `ide/vscode/prompts/` con 7 archivos `flow-*.prompt.md`. Cada archivo:

- Frontmatter YAML obligatorio: `description` (y `agent` cuando delegue a un agent `.agent.md` específico).
- Contenido: refactor del equivalente en `ide/cursor/commands/flow-*.md`, adaptando:
  - `$ARGUMENTS` para variables (compatibilidad cross-IDE).
  - `agent: forge-<rol>` en frontmatter para delegación al picker de VS Code.
  - Sin rutas absolutas (evitar Info Disclosure — STRIDE).
- Naming: `flow-start.prompt.md`, `flow-plan.prompt.md`, `flow-dev.prompt.md`, `flow-verify.prompt.md`, `flow-rework.prompt.md`, `flow-status.prompt.md`, `flow-close.prompt.md`.

### 3. Reglas del instalador (`ide/install.sh`)

- Detectar si `$HOME/.var/app/com.visualstudio.code` existe → tratar como Flatpak layout.
- En modo user-scope: copiar a la ruta detectada.
- En modo project-scope (`install.sh /path`): copiar adicionalmente a `/path/.github/prompts/`.
- Idempotencia + backup en `~/.flowforge-backups/<TS>/` (ya en patrón actual).
- Equivalente PowerShell en `ide/install.ps1` (paridad ya requerida por ADR-008).

### 4. Documentación

- Actualizar [docs/16-ide-integration-plan.md](../16-ide-integration-plan.md) marcando VS Code como 🟢 para slash commands.
- Actualizar `README.md` (sección "Quick Start / VS Code") indicando cómo invocar los `/flow-*`.
- Mencionar explícitamente el layout Flatpak y la dualidad `~/.config/Code/User` vs `~/.var/app/com.visualstudio.code/.../User`.

## Consecuencias

### Positivas

- Los usuarios de VS Code (incluido Flatpak, que es el caso de este reporte) obtienen paridad completa con Cursor/Antigravity/OpenCode.
- El instalador detecta automáticamente el layout nativo vs Flatpak sin requerir flags adicionales.
- El flujo `/flow-*` queda documentado formalmente con un HU trazable (HU-040).

### Negativas / Riesgos

- **Riesgo de edición post-install**: si un usuario edita manualmente `~/.config/Code/User/prompts/flow-*.prompt.md` el reinstall silencioso lo sobreescribe. Documentar en README y considerar `checksum` en una HU futura (no en esta).
- **Riesgo de drift**: si Cursor cambia su formato commands, los prompts VS Code podrían quedar rezagados. Mitigado por PM-VSC-* que exigen paridad.
- **Compatibilidad versiones VS Code**: el frontmatter `agent:` requiere VS Code ≥ 1.85 (Copilot Chat soporta agents desde esa versión). Documentar como prerrequisito.

### Trade-offs aceptados

- No generamos los 7 prompts en runtime: van en el repo (permite code-review, versionado, y trazabilidad — el mismo razonamiento que en [HU-038](../tasks/HU-001-HU-099/HU-038-opencode-commands-missing.md)).
- No usamos un wrapper symlink a los comandos Cursor: preferimos prompts nativos VS Code para evitar mantenimiento de un traductor cross-IDE.

## Alternativas consideradas

| Alternativa | Por qué se rechazó |
|-------------|---------------------|
| **A)** Inyectar `/flow-*` en chat vía un solo prompt orquestador (`flowforge.prompt.md`) que detecte intención | Pierde la UX de un comando dedicado por slash, complica el flujo de cierre, y acopla con el orquestador. |
| **B)** Reusar `ide/cursor/commands/*.md` directamente sin carpeta prompts dedicada | VS Code los ignora (no son `.prompt.md` y carecen de frontmatter). |
| **C)** Símbolo-enlace al repo de Cursor desde el instalador | Ensucía el sistema del usuario con symlinks invisibles; rompe atomicidad de backup. |
| **D)** Documentar workaround manual (lenguaje natural) y no hacer pack | El usuario explícitamente pidió que esto entrara en el flujo formal con HU, ADR y backlog — por eso vamos a la opción ADR. |

## Implementation plan

1. Crear los 7 `ide/vscode/prompts/flow-*.prompt.md` (FR-1, FR-2).
2. Modificar `ide/install.sh` bloque VS Code (L334–L364) para:
   - Detectar Flatpak.
   - Copiar prompts a la ruta correcta.
   - Idéntico para `ide/install.ps1`.
3. Actualizar `sdd/cli-wizard/doctor-diagnostic/` para separar checks `agents` vs `prompts` (FR-5, AC-6).
4. Actualizar `docs/16-ide-integration-plan.md` y `README.md`.
5. Ejecutar PM-VSC-* (forge-memory).

## Cierre

Este ADR queda **Proposed** hasta que `forge-verify` emita verdict `PASS` y todos los PM-VSC-* estén `[x]`. En ese momento, promoción automática a **Accepted** por `forge-memory` (regla del protocolo de promoción de ADRs).

---

## Registro de promoción

**Promovido a Accepted el 2026-10-20** por `forge-memory`.

Criterios cumplidos:
- Verdict de verificación: **PASS** (ver [`verify-report.md`](../../.ai-work/hotfix-vscode-slash-commands/verify-report.md))
- PM-VSC-1, 2, 5, 6, 7: `[x]` (verificados por sandbox + máquina real + build .NET)
- PM-VSC-3: `[x]` (el usuario confirmó "ya tengo los comandos" tras Reload Window)
- PM-VSC-4: `[x]` (el usuario verificó `/flow-status` sobre `.ai-work/`)

Evidencia de máquina real (Linux + VS Code Flatpak):
```
~/.var/app/com.visualstudio.code/config/Code/User/prompts/  →  7 flow-*.prompt.md
~/.copilot/agents/                                        →  8 *.agent.md
~/.copilot/instructions/                                  →  1 flowforge.instructions.md
```

Build verificado: `.NET SDK 10.0.402`, `dotnet build` 0 errores, `dotnet test` 240/240.
