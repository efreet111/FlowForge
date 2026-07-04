# Context Map — fix-ide-installer-packs

Fecha: 2026-07-03
Feature-slug: fix-ide-installer-packs

## 1) Problem statement
- El instalador multi-IDE (`flowforge install` / `flowforge init`) no garantiza que los "packs" de agentes queden ubicados donde cada IDE realmente los lee. Resultado: usuarios con Kilo Code, GitHub Copilot y OpenCode ven distintos comportamientos; algunos agentes quedan instalados en rutas que las extensiones no usan.
- Casos concretos ya identificados:
  1. Copilot: instalador copia agentes a `~/.vscode/agents/` pero Copilot usa `~/.copilot/agents/` (global) y `.github/agents/` (por proyecto).
  2. Kilo Code: extensión instalada es `kilocode.kilo-code` y Kilo lee `~/.config/kilo/agents/`, `.kilo/agents/` o `.opencode/agents/`, no `.github/agents/*.agent.md`.
  3. OpenCode: global `~/.config/opencode/agents/` funciona, pero `flowforge init` no siempre instala `.opencode/agents/` a nivel de proyecto o no parchea `opencode.flowforge.json` correctamente.
  4. Cursor: funciona correctamente (`~/.cursor/agents/`) — es la referencia de paridad.


## 2) IDE path matrix (expected vs actual)
- Cursor
  - Expected (and implemented): Global: `~/.cursor/agents/` ; Project: `.cursor/agents/`
  - Current: OK (install copies `ide/cursor/*` → `~/.cursor/`)

- OpenCode
  - Expected: Global: `~/.config/opencode/agents/` ; Project: `.opencode/agents/`
  - Current: installer copies to `~/.config/opencode/agents/` and (InstallOpenCodeProject) to project `.opencode/agents/` and duplicates to `.kilo/agents/` for compatibility.
  - Known gap: some opencode flowforge JSON patching required for `__FLOWFORGE_REPO__`.

- VS Code / GitHub Copilot
  - Expected: Global Copilot agent dir: `~/.copilot/agents/` and instructions in `~/.copilot/instructions/`. Project-level agents: `.github/agents/` and `.github/copilot-instructions.md`.
  - Current: installer previously copied to `~/.vscode/...` in some flows; current code (`FlowForgeModule.InstallVsCode` + `install.sh`/`install.ps1`) targets both `~/.copilot/agents/` (global) and `.github/agents/` (project). Detection logic depends on presence of extension folders under `~/.vscode/extensions` and may miss Kilo vs Copilot distinctions.

- Kilo Code (VS Code extension)
  - Expected: Global: `~/.config/kilo/agents/` ; Project: `.kilo/agents/`
  - Current: installer writes to `~/.config/kilo/agents/` and duplicates project `.kilo/agents/`. Detection logic looks for `kilocode.*` under `.vscode/extensions`.

- Antigravity / Claude desktop (notes)
  - Expected: `~/.gemini/antigravity/` (rules/workflows)
  - Current: installer supports copying AGENTS.md + rules/workflows.


## 3) Files found (touched / to touch)
- Source files implementing installer behavior (found/modified):
  - `src/FlowForge.Installer/Modules/FlowForgeModule.cs` (core IDE install logic: InstallCursor, InstallOpenCode, InstallVsCode, InstallKiloGlobal, InstallVsCodeProject, InstallOpenCodeProject)
  - `src/FlowForge.Installer/Commands/InitCommand.cs` (calls InstallVsCodeProjectPack on init; creates .flowforge.json, .ai-work, AGENTS.md)
  - `src/FlowForge.Installer/Commands/InstallCommand.cs` (installer command entry — review for CLI flags)
  - `src/FlowForge.Installer/Infrastructure/PathHelper.cs` (IDE path resolution helper)
  - `ide/install.sh` (Unix installer orchestration)
  - `ide/install.ps1` (Windows installer orchestration)
  - `ide/opencode/agents/*.md` (agent definitions; `qwen3.7-plus` updated)
  - `.github/agents/*.agent.md` (project-level agents present in repo)
  - `.opencode/agents/*`, `.kilo/agents/*`, `.cursor/agents/*` (repo-level copies exist)
  - `.ai-work/fix-installer/*` and `.ai-work/stack-installer/*` (existing feature artifacts)

- Files to update (recommended)
  - CI: `.github/workflows/test-installer.yml`, `scripts/docker-pm1-test.sh` — update tests for new paths and model changes.
  - `ide/install.ps1` — ensure parity for Kilo detection on Windows.
  - `src/FlowForge.Installer/Modules/FlowForgeModule.cs` — tighten detection for VS Code extensions and non-invasive defaults; add explicit handling to avoid writing to `.vscode` unless required.
  - `src/FlowForge.Installer/Commands/DoctorCommand.cs` — extend `flowforge doctor` to report which VS Code extension is installed (`kilocode` vs `github.copilot`) and presence of global agent dirs.
  - Documentation: `CHANGELOG.md`, `ide/README.md`, `docs/16-ide-integration-plan.md`


## 4) Reusable Patterns Found (MANDATORY)
- `src/FlowForge.Installer/Modules/FlowForgeModule.cs`
  - Implementa patrón "Instalador por IDE" con:
    - función InstallForIde + InstallCursor/InstallOpenCode/InstallVsCode
    - helpers CopyGlob / WriteUserCopilotInstructions / PatchOpenCodeFlowforgeJson
  - Recomendación: reutilizar/extend er estas funciones (no rediseñar) — son el patrón a ajustar.

- `ide/install.sh` and `ide/install.ps1`
  - Orquestadores shell/PowerShell con detección IDE, copia global y por-project.
  - Recomendación: unificar la lógica de detección (actualmente duplicada entre C# y scripts) o extraer pruebas de detección a PathHelper para consistencia.

- `src/FlowForge.Installer/Infrastructure/PathHelper.cs`
  - Central para resolver rutas (CopilotAgents, Kilo, OpenCode). Prioridad: verificar y consolidar.

- `.opencode/agents/*`, `ide/opencode/agents/*`, `.kilo/agents/*`, `.github/agents/*`
  - Agentes de ejemplo que ya existen y permiten pruebas de instalación por proyecto.

- Resultado: existe código que cubre ~80% del comportamiento esperado — esto permite PATCH/REFACTOR en lugar de rediseño (no es greenfield).


## 5) Risks & Dependencies
- CI risk: `test-installer.yml` and docker smoke tests expect old paths/models (e.g., qwen3.5) — must update CI to avoid regressions.
- Backwards compatibility: some users may have hand-customized `~/.copilot/agents/` or `~/.config/opencode/agents/`; installer must avoid destructive deletes — use safe overwrite and backups (scripts already write backups).
- Model provider compatibility: user machines may have old models in their global dirs (qwen3.5) — decide whether installer should auto-upgrade or only install new copies and log deprecation warnings.
- OS parity: Windows PowerShell script must match Linux behavior for Kilo detection and project-level copies.
- Permissions: copying to user config dirs may require additional permissions on some locked environments.


## 6) FlowDoc context
- PRD: docs/PRD.md (read: no — not present)
- HU referenced: none
- HU flowforge_slug: unset


## 7) Open questions
- [BLOCKER] Confirm desired installer behavior for VS Code when both Kilo and Copilot are absent: should installer write BOTH global Copilot (`~/.copilot/agents/`) and Kilo (`~/.config/kilo/agents/`) formats by default? Current code does a best-effort install of both — confirm.
- [BLOCKER] CI acceptance criteria: which test matrix should be updated to validate new target paths (Linux, Windows, project-level `.github/agents`, `.opencode/agents`, `.kilo/agents`)? Need explicit test cases to unblock implementation.
- [BLOCKER] Should the installer automatically remove or migrate old opencode model refs (qwen3.5 → qwen3.7), or only place updated agent definitions and leave global user config untouched?
- [OPTIONAL] Decide if `flowforge doctor` should offer an "auto-fix" flag to repair detected mismatches (safer: propose changes and require user confirmation).
- [OPTIONAL] Add telemetry/log suggestion: record which IDEs were detected and which agent destinations were written (for easier triage).


## 8) Next steps (summary for forge-arch)
- Context sufficient to design spec.md:
  1. Spec should require precise installer semantics for each IDE (global vs project), explicit non-destructive behavior, and CI test cases.
  2. Arch decisions: extend `PathHelper` and centralize detection logic; avoid duplicating OS logic between scripts and C#; define upgrade policy for models (qwen3.5 → qwen3.7).
  3. Plan should include updating CI, adding Windows Kilo parity, and updating `flowforge doctor`.

Archivo escrito en: `.ai-work/fix-ide-installer-packs/context-map.md`

