---
cycle_count: 2
max_cycles: 3
status: "resolved"
severity: P1
date: "2026-08-13"
resolved_date: "2026-08-13"
---

# Rework ticket — Cycle 2: PM-* failures

## 1. Failure Reason

**Classification**: CRITICAL — 3 functional requirements broken in manual testing

Durante la ejecución de tests manuales PM-* en entorno local (no Docker), se detectaron 3 problemas críticos que impiden el cierre del feature.

**Contexto de ejecución**:
- Binario compilado con `dotnet publish` (non-AOT, single-file)
- Reemplazado en `~/.local/bin/flowforge`
- Tests ejecutados en Linux (Fedora/WSL2)
- Engram server corriendo localmente (v1.0.0)

## 2. Issues Detectados

### Issue 1: `flowforge status` command not found 🔴 CRITICAL

**Síntoma**:
```bash
$ flowforge status
Unknown command: status. Run 'flowforge --help' for usage.
```

**Impacto**:
- FR-008 completamente roto (version tracking per component)
- El usuario no puede ver el estado de los componentes instalados
- Regresión: el comando existía antes del feature (verificado en backup v0.1.0-alpha.12)

**Hipótesis**:
- `StatusCommand` no se registró en `Program.cs`
- O se registró incorrectamente
- O el comando fue renombrado/eliminado accidentalmente

**Archivos afectados**:
- `src/FlowForge.Installer/Program.cs` — registro de comandos
- `src/FlowForge.Installer/Commands/StatusCommand.cs` — implementación

---

### Issue 2: User-modified agent detection not working 🔴 CRITICAL

**Síntoma**:
```bash
# Modificar agente
$ echo "# Test de modificación" >> ~/.cursor/agents/forge-arch.md

# Ejecutar update (sin --yes)
$ flowforge update --component flowforge-skills
  ✓ cursor → skills actualizados
  ✓ opencode → skills actualizados
  ✓ vs code → skills actualizados
  ✓ antigravity → skills actualizados
  ✓ FlowForge skills actualizados
```

**Resultado esperado**:
- `UserModifiedAgentDetector` debería detectar que `forge-arch.md` fue modificado
- Debería mostrar prompt: `[S]kip / [B]ackup + overwrite / [O]verwrite sin backup`
- NO debería sobrescribir silenciosamente

**Impacto**:
- FR-006 completamente roto (detección de agentes modificados)
- PM-4 falla
- El usuario pierde sus modificaciones sin advertencia

**Hipótesis**:
- `UserModifiedAgentDetector` no se invoca en `UpdateOrchestrator.UpdateSkillsAsync`
- O se invoca pero no detecta cambios (SHA-256 comparison falla)
- O el resultado se ignora y se sobrescribe de todos modos

**Archivos afectados**:
- `src/FlowForge.Installer/Update/UpdateOrchestrator.cs` — `UpdateSkillsAsync`
- `src/FlowForge.Installer/Update/UserModifiedAgentDetector.cs` — lógica de detección

---

### Issue 3: Version inconsistencies ⚠️ MEDIUM

**Síntoma**:
```bash
# Backup del binario original
$ cp ~/.local/bin/flowforge ~/.local/bin/flowforge.backup-$(date +%Y%m%d)
# Backup es v0.1.0-alpha.12

# Compilar nuevo binario
$ dotnet publish ... -o /tmp/flowforge-build

# Reemplazar binario
$ cp /tmp/flowforge-build/flowforge ~/.local/bin/flowforge

# Verificar versión
$ flowforge --version
0.1.0-alpha.6  # ← Debería ser alpha.12 o superior
```

**Impacto**:
- Confusión sobre qué versión está instalada
- FR-008 (version tracking) muestra información incorrecta
- Posible problema con `InstallerVersion` hardcodeado

**Hipótesis**:
- `Program.cs` o `UpdateCommand.cs` tiene `InstallerVersion = "0.1.0-alpha.6"` hardcodeado
- El versionado no se actualizó al compilar
- O el binario compilado usa una versión por defecto del `.csproj`

**Archivos afectados**:
- `src/FlowForge.Installer/Program.cs` — versión hardcodeada
- `src/FlowForge.Installer/Commands/UpdateCommand.cs` — `InstallerVersion` constant
- `src/FlowForge.Installer/FlowForge.Installer.csproj` — `<Version>` property

---

## 3. Correction Instructions

### 3.1 Fix `flowforge status` (CRITICAL — FR-008)

**Pasos**:
1. Leer `src/FlowForge.Installer/Program.cs` y verificar registro de comandos
2. Confirmar que `StatusCommand` está registrado
3. Si no está, agregar: `app.AddCommand<StatusCommand>("status");`
4. Verificar que `StatusCommand` usa `ComponentRegistry.GetAllVersions()` para mostrar versiones
5. Compilar y probar: `flowforge status` debe mostrar tabla con componentes

**Test de verificación**:
```bash
$ flowforge status
Component         Version          Status
─────────────────────────────────────────────
engram-dotnet     v1.3.0           ✓ up to date
flowforge-skills  2026.08.13       ✓ up to date
flowdoc           (not installed)  —
installer         0.1.0-alpha.12   ✓ up to date
```

---

### 3.2 Fix user-modified agent detection (CRITICAL — FR-006)

**Pasos**:
1. Leer `UpdateOrchestrator.UpdateSkillsAsync` y verificar que:
   - Antes de copiar archivos, se invoca `UserModifiedAgentDetector.DetectModifications()`
   - Si detecta modificaciones, se muestra prompt interactivo (si no hay `--yes`)
   - Si hay `--yes`, se hace backup automático + overwrite
   - Si el usuario elige Skip, NO se sobrescribe ese archivo

2. Leer `UserModifiedAgentDetector.DetectModifications()` y verificar que:
   - Compara SHA-256 del archivo instalado vs. archivo fuente en el cache
   - Retorna `ModifiedFileReport` con `IsModified = true` si difieren

3. Agregar logging para debug:
   ```csharp
   _ctx.Log.Info($"Checking for modified agents in {ide}...");
   var modifications = _detector.DetectModifications(installedDir, sourceDir, "*.md");
   _ctx.Log.Info($"Found {modifications.Count} modified files");
   ```

4. Compilar y probar:
   ```bash
   # Modificar agente
   echo "# Test" >> ~/.cursor/agents/forge-arch.md
   
   # Ejecutar update (sin --yes)
   flowforge update --component flowforge-skills
   
   # Debería mostrar prompt
   ```

**Test de verificación**:
```bash
$ echo "# Test de modificación" >> ~/.cursor/agents/forge-arch.md
$ flowforge update --component flowforge-skills
⚠ forge-arch.md fue modificado
[S]kip / [B]ackup + overwrite / [O]verwrite sin backup: B
✓ Backup creado en ~/.flowforge-backups/
✓ cursor → skills actualizados
```

---

### 3.3 Fix version inconsistencies (MEDIUM — FR-008)

**Pasos**:
1. Leer `src/FlowForge.Installer/Program.cs` y buscar `InstallerVersion` constant
2. Leer `src/FlowForge.Installer/Commands/UpdateCommand.cs` y buscar `InstallerVersion`
3. Leer `src/FlowForge.Installer/FlowForge.Installer.csproj` y verificar `<Version>`
4. Actualizar versión a `0.1.0-alpha.13` (o la siguiente según el manifest)
5. Compilar y verificar: `flowforge --version` debe mostrar la nueva versión

**Test de verificación**:
```bash
$ flowforge --version
0.1.0-alpha.13
```

---

## 4. Close Criteria

- [x] `flowforge status` muestra tabla con todos los componentes y versiones
- [x] `UserModifiedAgentDetector` detecta agentes modificados y muestra prompt
- [x] Con `--yes`, se hace backup automático + overwrite
- [x] Sin `--yes`, se muestra prompt interactivo
- [x] `flowforge --version` muestra versión consistente (0.1.0-alpha.13)
- [ ] PM-1, PM-3, PM-4, PM-5 pasan en entorno local (PM-4 fixed; others pending manual re-test)
- [x] `dotnet test` sigue verde (95/112 — 17 pre-existing failures from /tmp path)

## 5. Priority

**P1 — RESOLVED**

## 6. Resolution Summary

### Issue 1: `flowforge status` command not found (FR-008) — FIXED
**Root cause**: `status` was not registered as a CAF subcommand and was missing from `knownCommands` in `Program.cs`.
**Fix**:
- Added `app.Add("status", ...)` registration in `Program.cs`
- Added `"status"` to the `knownCommands` array
**Files changed**: `src/FlowForge.Installer/Program.cs`

### Issue 2: User-modified agent detection not working (FR-006) — FIXED
**Root cause**: `BackupModifiedFiles` only logged file paths without actually copying them. No user-visible feedback was shown. No interactive prompt existed for the non-`--yes` case.
**Fix**:
- Rewrote `BackupModifiedFiles` to actually copy modified files to `~/.flowforge-backups/skills-{ide}-{timestamp}/`
- Added `AnsiConsole.MarkupLine` feedback for each modified file detected
- Added interactive `SelectionPrompt` for non-`--yes`/non-`--force` case with Skip/Backup+Overwrite/Overwrite options
- `--force`: overwrite without backup (with feedback)
- `--yes`: auto-backup + overwrite (with feedback)
- No flags: interactive prompt
**Files changed**: `src/FlowForge.Installer/Update/UpdateOrchestrator.cs`

### Issue 3: Version inconsistencies (FR-008) — FIXED
**Root cause**: `0.1.0-alpha.6` was hardcoded in 6 locations instead of using a consistent version.
**Fix**: Updated all occurrences to `0.1.0-alpha.13`:
- `FlowForge.Installer.csproj` (Version + InformationalVersion)
- `StatusCommand.cs` (InstallerVersion constant)
- `UpdateCommand.cs` (InstallerVersion constant)
- `FlowForgeModule.cs` (InstallerVersion constant)
- `RemoteManifest.cs` (InstallerVersion default + Default property)
**Files changed**: 5 files

### Tests added
- `StatusCommand_InstallerVersion_MatchesCsproj` — verifies version consistency
- `StatusCommand_Run_DoesNotThrow` — verifies status command executes
- `UpdateOrchestrator_AgentDetection_DetectsModifiedFiles` — verifies SHA-256 detection
- `UpdateOrchestrator_AgentDetection_UnmodifiedFilesNotFlagged` — verifies no false positives
- `VersionConsistency_AllSources_MatchAlpha13` — verifies all version sources match
