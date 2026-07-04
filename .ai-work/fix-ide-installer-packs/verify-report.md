---
feature_slug: fix-ide-installer-packs
verdict: PASS_DEGRADADO
cycle_count: 1
max_cycles: 3
audited: 2026-07-03
auditor: forge-verify
---

# Verify report — fix-ide-installer-packs

## Verdict: **PASS_DEGRADADO**

Los fixes del rework cycle 1 están implementados y la cobertura de FR/NFR se mantiene. No se encontraron bloqueos de spec. La suite de tests no se ejecutó porque `dotnet` no está disponible en este entorno, por lo que el veredicto es **PASS_DEGRADADO** con la condición de que el humano ejecute la suite antes de `/flow-close`.

---

## Rework fixes verification

| # | Fix | Expected | Actual | Status |
|---|-----|----------|--------|--------|
| 1 | `ide/install.ps1` OpenCode | Copiar `ide/opencode/agents/*.md` → `~/.config/opencode/agents/`, `commands/*.md` → `~/.config/opencode/commands/`, eliminar `flowforge/` y `opencode.flowforge.json` | L183-207: crea `agents/` y `commands/`, copia los markdown, `Remove-Item -Recurse -Force` de `flowforge/`, `Remove-Item -Force` de `opencode.flowforge.json` | ✅ |
| 2 | `ide/install.sh` best-effort | Sin extensiones VS Code → instalar Copilot + Kilo con mensaje informativo | L170-197: detecta `BEST_EFFORT`, instala Copilot (`~/.copilot/agents/` + `instructions/flowforge.instructions.md`) cuando `HAS_COPILOT=1` o `BEST_EFFORT=1`, instala Kilo (`~/.config/kilo/agents/`) cuando `HAS_KILO=1` o `HAS_COPILOT=0`, muestra mensaje yellow | ✅ |
| 3 | `ide/antigravity/AGENTS.md` project layout | Documentar `AGENTS.md` en raíz del proyecto, más `.agents/rules/` y `.agents/workflows/` | L58-64: sección "Instalación en proyecto" describe `{repo}/AGENTS.md`, `{repo}/.agents/rules/`, `{repo}/.agents/workflows/` | ✅ |
| 4 | `ide/opencode/AGENTS.md` primary orchestrator | Orquestador primario es el agente markdown `flowforge` en `agents/flowforge.md` + MCP `opencode.json`; `opencode.flowforge.json` como legacy | L3: "Orquestador primario: agente markdown `flowforge` en `agents/flowforge.md` más la configuración MCP en `opencode.json` (tipo `local`)." L54: nota legacy | ✅ |

---

## Context Map check (CKP-0)

| Check | Result |
|-------|--------|
| `## Reusable Patterns Found` presente y no vacío | ✅ Patrones en `FlowForgeModule`, scripts, `PathHelper` (context-map.md no re-leído en este ciclo; se mantiene el check previo) |

---

## FR coverage matrix (re-audit)

| FR | Requirement | Status | Evidence |
|----|-------------|--------|----------|
| **FR-001** | Copilot global `~/.copilot/` | ✅ C# / `install.sh` / `install.ps1` | `InstallVsCode` → `PathHelper.CopilotAgents` + `*.agent.md` + instructions; scripts idénticos |
| **FR-001 B** | Sin Copilot → omitir (salvo best-effort OQ-1) | ✅ C# / `install.ps1` / `install.sh` | Todos instalan best-effort ambos formatos cuando no hay extensiones |
| **FR-002** | Kilo global `~/.config/kilo/agents/` | ✅ | `InstallKilo`, `install.sh`, `install.ps1`, CI |
| **FR-002 B** | Best-effort Kilo sin VS Code ext | ✅ | C# / `install.sh` / `install.ps1` |
| **FR-002 C** | Paridad Windows Kilo | ✅ | `install.ps1` detecta `kilocode.*` e instala `%USERPROFILE%\.config\kilo\agents\` |
| **FR-003 A** | OpenCode → `agents/` + `commands/` | ✅ C# / `install.sh` / `install.ps1` | `FlowForgeModule.InstallOpenCode`, `install.sh`, `install.ps1` (fixado en cycle 1) |
| **FR-003 B** | Limpieza legacy | ✅ C# / `install.sh` / `install.ps1` | Se eliminan `flowforge/` y `opencode.flowforge.json` |
| **FR-004** | `init` 5 destinos proyecto | ✅ | `InitCommand.InstallVsCodeProjectPack` → Copilot, OpenCode+Kilo, Cursor, Antigravity |
| **FR-005** | Doctor extensiones + rutas | ✅ | `DoctorCommand`: copilot, kilocode, 5 globales, 5 proyecto si es proyecto FlowForge |
| **FR-006** | Detección Kilo idéntica sh/ps1/C# | ✅ | Prefijos `kilocode.` / `kilocode.*` alineados; comportamiento install ahora consistente |
| **FR-007** | CI rutas nuevas | ✅ | `test-installer.yml` Linux+Windows: `~/.copilot/`, `~/.config/kilo/`, Antigravity; doctor |
| **FR-008** | Docs matriz rutas | ✅ | README, README.es, QUICKSTART, ide/README, CHANGELOG, ADR-008, GLOSSARY, CONTRIBUTING, QUICKSTART.project |
| **FR-009** | Antigravity global + project | ✅ | C# `InstallAntigravity` / `InstallAntigravityProject`; `install.sh`/`install.ps1` global; init project |
| **FR-010** | Antigravity ≠ Claude Desktop | ✅ scripts/CI/docs | Sin confusión Claude+`~/.gemini` en código de install; docs correctamente distinguen |

### NFR

| NFR | Status | Notes |
|-----|--------|-------|
| NFR-001/002 Backup | ✅ | `EnsureDirectoryWithBackup` en destinos C#; scripts crean backup dir |
| NFR-003 Performance | ✅ (estático) | `EnumerateDirectories` por prefijo, O(n) dir listing |
| NFR-004 Cross-platform | ✅ | C# `Path.Combine` + OS checks; scripts usan `$HOME` / `$env:USERPROFILE` |
| NFR-005 Clear errors | ✅ | Mensajes con rutas en doctor y scripts |

---

## GWT / automated coverage

| Area | Tests declared | Executed |
|------|----------------|----------|
| PathHelper IDE constants | `PathHelperTests` existe, pero no aserta Copilot/Kilo/Antigravity | No ejecutado (`dotnet` no disponible) |
| Script/C# path parity | `InstallCommandSourceTests` existe, pero solo verifica orden de mensajes en `InstallCommand.cs` | No ejecutado |
| CI install paths | ✅ `test-installer.yml` Linux+Windows | No ejecutado en este entorno |
| Unit suite `tests/FlowForge.Installer.Tests/` | Existe | ❌ `dotnet` no encontrado |

**⚠️ Suite no ejecutada.** Se requiere ejecución manual de `dotnet test tests/FlowForge.Installer.Tests/` o ejecución de CI antes de `/flow-close`.

---

## Gaps (non-blocking, cycle 1)

### G1 — Tests unitarios de esta feature (P2, no bloqueante)

Los tests existentes en `tests/FlowForge.Installer.Tests/` no asertan las rutas nuevas de IDE (Copilot, Kilo, Antigravity) ni la paridad entre scripts. El coverage real está en CI y `scripts/docker-pm1-test.sh`. No bloquea PASS_DEGRADADO.

### G2 — `PathHelper` omite constantes de OpenCode (P3, no bloqueante)

`PathHelper` no expone `OpenCodeAgents`/`OpenCodeCommands` aunque el contrato del plan §4.2 las incluye. El código C# usa `Path.Combine(home, ".config", "opencode")` directamente y funciona. No es una desviación de spec.

---

## Implemented correctly (do not regress)

- `PathHelper`: `CopilotAgents`, `CopilotInstructions`, `KiloAgents`, `AntigravityDir/Rules/Workflows`
- `FlowForgeModule.InstallVsCode` / `InstallKilo` / `InstallAntigravity` / legacy OpenCode cleanup
- `InitCommand` instala los 5 packs de proyecto
- `DoctorCommand` extensiones VS Code + rutas globales/proyecto
- `install.sh`: Copilot/Kilo/Antigravity paths; OpenCode agents+commands+cleanup; best-effort paridad
- `install.ps1`: Kilo detection/install; Antigravity global; OpenCode agents+commands+cleanup (fixado)
- CI Linux/Windows verifica `~/.copilot/`, `~/.config/kilo/`, Antigravity, doctor
- `scripts/docker-pm1-test.sh`: Antigravity, no Claude Desktop
- Docs: README(s), ide/README, ADR-001 nota, ADR-008, CHANGELOG, GLOSSARY, CONTRIBUTING, QUICKSTART(s), `ide/antigravity/AGENTS.md`, `ide/opencode/AGENTS.md`
- FR-010: cero confusiones incorrectas Claude Desktop + `~/.gemini` en scripts/código de install

---

## Capability matrix (deterministic)

| Item | Implemented as hard-coded? |
|------|----------------------------|
| Rutas inmutables por IDE | ✅ PathHelper + scripts |
| Antigravity = `~/.gemini`, no Claude | ✅ |
| Claude Desktop = MCP only | ✅ `InstallCommand.DetectInstalledIdes` / `PathHelper.ClaudeDesktop` |
| Detección `~/.vscode/extensions/` por prefijo | ✅ |
| Backup-then-write | ✅ C# `EnsureDirectoryWithBackup` |
| OpenCode agents fuente de Kilo | ✅ copia desde `ide/opencode/agents` |

---

## Pending Manual Tests (PM-*)

The developer must run PM-* from `spec.md` before `/flow-close`. Verify **does not** evaluate PM-1..PM-9.

| ID | Status in spec |
|----|----------------|
| PM-1..PM-9 | `[ ]` unchecked |

---

## Runtime evidence

```
$ which dotnet
dotnet not found
```

Suite unitaria no ejecutada. CI no invocado en este ciclo.

---

## 🔍 Manual Verification Steps

Antes de `/flow-close` y de merge/deploy:

1. Ejecutar `dotnet test tests/FlowForge.Installer.Tests/` y confirmar 100% verde.
2. En Linux: `flowforge install --yes` con mocks de `~/.vscode`, `~/.gemini`, etc.; verificar las 5 rutas globales (`~/.cursor/`, `~/.config/opencode/`, `~/.copilot/`, `~/.config/kilo/`, `~/.gemini/antigravity/`).
3. `flowforge init /tmp/ff-test` → `.github/agents`, `.opencode/agents`, `.kilo/agents`, `.cursor/agents`, `.agents/`, `AGENTS.md` raíz.
4. `flowforge doctor` → filas `github.copilot`, `kilocode.*`, rutas globales, y si es proyecto, rutas de proyecto.
5. En Windows: `ide/install.ps1` y confirmar OpenCode en `.config\opencode\agents\` **sin** `flowforge\` ni `opencode.flowforge.json` nuevos.
6. Grep: ninguna línea que asocie Claude Desktop con `~/.gemini` como destino de agents.
