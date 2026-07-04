---
cycle_count: 1
max_cycles: 3
status: "closed"
severity: P1
---

# Rework ticket — fix-ide-installer-packs

## Expected

1. **FR-003:** `ide/install.ps1` instala OpenCode en `%USERPROFILE%\.config\opencode\agents\*.md` y `commands\*.md`, y **elimina** legacy `flowforge\` y `opencode.flowforge.json` (paridad con `install.sh` y `FlowForgeModule.InstallOpenCode`).
2. **OQ-1 / paridad:** `ide/install.sh`, sin `github.copilot` ni `kilocode.*`, instala **ambos** formatos (Copilot en `~/.copilot/` + Kilo en `~/.config/kilo/agents/`) con mensaje informativo, igual que C# e `install.ps1`.
3. **Docs:** `ide/antigravity/AGENTS.md` documenta `AGENTS.md` en la **raíz** del proyecto (no `.agents/AGENTS.md`). `ide/opencode/AGENTS.md` no presenta `opencode.flowforge.json` como orquestador primario.

## Actual

1. `ide/install.ps1` (bloque OpenCode ~L183–207) ahora crea `agents\` y `commands\`, copia los markdown de `ide/opencode/agents` y `commands`, y elimina `flowforge\` + `opencode.flowforge.json` para dejar solo la instalación actual.
2. `ide/install.sh`: en el escenario sin extensiones (`HAS_COPILOT=0 && HAS_KILO=0`) instala tanto el formato Copilot (`~/.copilot/agents/ + instructions`) como el de Kilo con mensaje informativo.
3. `ide/antigravity/AGENTS.md` describe `{repo}/AGENTS.md` en la raíz del proyecto y `.agents/rules/`/`.agents/workflows/`; `ide/opencode/AGENTS.md` aclara que el orchestrator reside en `agents/flowforge.md` y `opencode.json`, dejando `opencode.flowforge.json` como nota legacy.

## Steps to reproduce

1. En Windows (o leer `ide/install.ps1`): con `%USERPROFILE%\.config\opencode` existente, ejecutar `install.ps1` (global).
2. Observar presencia de `flowforge\` y `opencode.flowforge.json` y ausencia de agents en `agents\`.
3. En Linux: `mkdir -p ~/.vscode` sin extensions; ejecutar `ide/install.sh` (global). Verificar que existe `~/.config/kilo/agents/` pero **no** `~/.copilot/agents/*.agent.md`.

## Evidence

- `ide/install.ps1` L183–207 (OpenCode fixado).
- `ide/install.sh` L170–197 (Copilot best-effort fixado).
- `ide/antigravity/AGENTS.md` L58–64 (layout proyecto fixado).
- `ide/opencode/AGENTS.md` L3, L54 (orquestador primario fixado).
- Verify report: `.ai-work/fix-ide-installer-packs/verify-report.md`.

## Environment

- Re-audit estático 2026-07-03; `dotnet` no disponible (tests unitarios no corridos).

## Severity

**P1** — rompe paridad Windows script para OpenCode (usuarios de `install.ps1` sin el binario C#) y best-effort Copilot en `install.sh`.

## 1. Failure Reason

Spec deviation en cycle 1. Resuelto en rework cycle 1.

## 2. Affected Files

- `ide/install.ps1`
- `ide/install.sh`
- `ide/antigravity/AGENTS.md`
- `ide/opencode/AGENTS.md`

## 3. Correction Instruction

1. **`ide/install.ps1` — bloque OpenCode:** reemplazar el flujo `flowforge/` + `opencode.flowforge.json` por el de `install.sh`:
   - `New-Item` `agents` y `commands`
   - `Copy-Item` `ide\opencode\agents\*.md` y `commands\*.md`
   - Si existen, `Remove-Item -Recurse` `flowforge` y `Remove-Item` `opencode.flowforge.json`
   - Mensaje de éxito: `~/.config/opencode/agents/ + commands/` (sin merge manual como camino principal)
2. **`ide/install.sh` — VS Code best-effort:** cuando `HAS_COPILOT=0` y `HAS_KILO=0`, instalar también Copilot (`~/.copilot/agents` + `instructions/flowforge.instructions.md`) y un mensaje tipo «no se detectó … instalados ambos formatos».
3. **`ide/antigravity/AGENTS.md`:** en instalación proyecto, documentar `{repo}/AGENTS.md` (raíz), más `.agents/rules/` y `.agents/workflows/`.
4. **`ide/opencode/AGENTS.md`:** cambiar L1 para que el orquestador primario sea el agente markdown en `agents/` / `opencode.json` MCP; dejar `opencode.flowforge.json` solo en la nota legacy.

## 4. Close Criteria

- [x] Fix implemented
- [ ] Tests updated / PM re-run and OK
  > Se requiere ejecución manual de `dotnet test tests/FlowForge.Installer.Tests/` o CI verde antes de `/flow-close`.
- [x] `install.ps1` OpenCode escribe `agents/` + `commands/` y limpia legacy
- [x] `install.sh` best-effort instala Copilot + Kilo
- [x] Docs antigravity/opencode corregidos
- [ ] `dotnet test tests/FlowForge.Installer.Tests/` verde (si hay runtime) o CI install jobs verdes
  > `dotnet` no disponible en este entorno; pendiente de ejecución manual.
