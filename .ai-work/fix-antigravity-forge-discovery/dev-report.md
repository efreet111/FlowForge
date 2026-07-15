# Dev Report — fix-antigravity-forge-discovery

**Date:** 2026-07-15  
**Phase:** forge-dev rework P0 (global_workflows path)

## Verdict

**REWORK RESOLVED** — Destino global de workflows migrado a `~/.gemini/config/global_workflows/` (Antigravity 2.1+). Migración legacy desde `config/workflows/` en C#, `install.sh` y `install.ps1`.

---

## Rework fix (P0)

### Root cause
Antigravity IDE 2.1 lee workflows globales desde `config/global_workflows/`, no `config/workflows/`. Instalador escribía en ruta obsoleta → picker `/` vacío.

### Implemented
- `PathHelper.AntigravityWorkflows` → `{AntigravityConfigDir}/global_workflows`
- `PathHelper.AntigravityLegacyWorkflowsDir` → `{AntigravityConfigDir}/workflows` (solo migración/doctor)
- `MigrateLegacyWorkflowsDir()` en C# + equivalente bash/PowerShell
- Doctor: nuevo check `Antigravity: legacy workflows dir`
- CI (`test-installer.yml`), `docker-pm1-test.sh`: asserts en `global_workflows`
- Docs: ADR-009, `ide/antigravity/AGENTS.md`, QUICKSTART*, `ide/README.md`
- `rework_ticket.md` → `status: resolved`

### Path property final value
`Path.Combine(AntigravityConfigDir, "global_workflows")`  
→ `~/.gemini/config/global_workflows/` (Linux) / `%USERPROFILE%\.gemini\config\global_workflows\` (Windows)

---

## Test results

| Test | Result |
|------|--------|
| `scripts/validate-antigravity-pack.sh` | **PASS** |
| `dotnet test --filter AntigravityPackValidator` | **PASS** (7/7) |
| `dotnet test --filter PathHelperTests` | **PASS** (incl. FR_016) |
| PM-1..PM-5 | **PENDING HUMAN** (reload Antigravity + picker) |

---

## Files changed (rework)

- `src/FlowForge.Installer/Infrastructure/PathHelper.cs`
- `src/FlowForge.Installer/Infrastructure/AntigravityPackValidator.cs`
- `src/FlowForge.Installer/Modules/FlowForgeModule.cs`
- `src/FlowForge.Installer/Commands/DoctorCommand.cs`
- `ide/install.sh`, `ide/install.ps1`
- `tests/FlowForge.Installer.Tests/PathHelperTests.cs`
- `tests/FlowForge.Installer.Tests/AntigravityPackValidatorTests.cs`
- `.github/workflows/test-installer.yml`
- `scripts/docker-pm1-test.sh`
- `docs/decisions/ADR-009-opencode-antigravity-customizations.md`
- `ide/antigravity/AGENTS.md`, `ide/README.md`, `QUICKSTART.md`, `QUICKSTART.es.md`
- `.ai-work/fix-antigravity-forge-discovery/plan.md`
- `.ai-work/fix-antigravity-forge-discovery/rework_ticket.md`

---

## Memory Signal

- type: bugfix
- significance: high
- summary: "Antigravity 2.1 reads global workflows from config/global_workflows not config/workflows; installer path fix + legacy migration on install"
