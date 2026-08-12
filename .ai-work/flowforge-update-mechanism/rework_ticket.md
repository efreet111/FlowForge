---
cycle_count: 1
max_cycles: 3
status: "closed"
severity: P1
---
# Rework ticket — flowforge-update-mechanism

## 1. Failure Reason

**Classification**: CRITICAL stub + missing integration

El `UpdateOrchestrator.UpdateSkillsAsync` es un stub que NO copia archivos reales. Reporta SUCCESS sin transferir skills/agents a los destinos IDE. FR-010 está completamente roto. Adicionalmente, `InstallerLogger.UpdateOperation()` (NFR-LOG-001) nunca se invoca.

## 2. Affected Files

### Must Fix (P1):

- `src/FlowForge.Installer/Update/UpdateOrchestrator.cs` — `UpdateSkillsAsync` (lines 239-289)
- `src/FlowForge.Installer/Modules/FlowForgeModule.cs` — Extraer/reusar lógica de copia de archivos por IDE

### Should Fix (P2):

- `src/FlowForge.Installer/Update/UpdateOrchestrator.cs` — `UpdateEngramAsync` debe llamar `_ctx.Log.UpdateOperation(...)` en success/failure/rollback
- `src/FlowForge.Installer/Modules/EngramModule.cs` — Agregar `DownloadEngramToTempAsync` y `HealthCheckBinaryAsync` (tarea 10.2 del plan)
- `src/FlowForge.Installer/Modules/OpenCode/ManagedPathsSidecar.cs` — Hacer constructor aceptar path custom (tarea 8.2 del plan)

## 3. Correction Instruction

### 3.1 Fix `UpdateSkillsAsync` (CRITICAL — FR-010)

El método debe:
1. ✅ Refrescar el cache git (ya lo hace)
2. ✅ Detectar IDEs instalados (ya lo hace)
3. 🔴 **COPIAR archivos desde el cache a cada destino IDE**:
   - Cursor: copiar `ide/cursor/rules/*.mdc`, `ide/cursor/agents/forge-*.md`, `ide/cursor/commands/*.md`
   - OpenCode: copiar `ide/opencode/agents/*.md`, `ide/opencode/commands/*.md`
   - Antigravity: copiar `.agents/rules/*.md`, `.agents/skills/*.md`, `.agents/workflows/*.md`
   - VS Code Copilot: copiar `.github/agents/*.agent.md`, `copilot-instructions.md`
   - Kilo: copiar duplicados de opencode agents
4. 🔴 **Ejecutar `UserModifiedAgentDetector` antes de sobrescribir** (FR-006):
   - Si `--yes`: backup automático + overwrite
   - Sin `--yes`: prompt interactivo (Skip/Backup+overwrite/Overwrite)
   - Si `--force`: overwrite sin backup
5. 🔴 **Actualizar sidecar `ManagedPathsSidecarFactory` por cada IDE** (FR-011)
6. ✅ Actualizar version tracking (ya lo hace)

### 3.2 Fix NFR-LOG-001

En `UpdateOrchestrator.UpdateEngramAsync`, reemplazar `_ctx.Log.Info($"UpdateEngram: success {currentVersion} → {latestVersion}")` (line 211) por:

```csharp
_ctx.Log.UpdateOperation("engram", currentVersion, latestVersion, sha256Pre, sha256Post, "success");
```

Hacer lo mismo en los casos de `Failed`, `RolledBack`, y `SkippedAlreadyLatest`.

Para `UpdateSkillsAsync`, hacer equivalente tras la corrección.

### 3.3 Reusar FlowForgeModule existente (tarea 10.3)

`FlowForgeModule.Install` ya implementa la copia de archivos por IDE. Extraer un método `CopySkillsForIdeAsync(string ide, string homeDir, string cacheRepo)` que reutilice la lógica existente. No duplicar código de copia de archivos.

### 3.4 Modificar EngramModule.cs (tarea 10.2)

Agregar:
```csharp
/// <summary>Downloads engram binary to a temp path without overwriting the installed binary.</summary>
public async Task<bool> DownloadEngramToTempAsync(string version, string tempPath, CancellationToken ct = default)
{
    return await ctx.GitHub.DownloadEngramAsync(version, tempPath, ct);
}
```

Esto formaliza la separación de responsabilidades que el plan requiere.

## 4. Close Criteria

- [x] `UpdateSkillsAsync` copia archivos reales desde cache a cada IDE detectado (FR-010)
- [x] `UserModifiedAgentDetector` se ejecuta antes de sobrescribir agentes (FR-006)
- [x] `ManagedPathsSidecarFactory.WriteSidecar()` se llama por cada IDE actualizado (FR-011)
- [x] `_ctx.Log.UpdateOperation()` se invoca en success/failure/rollback de cada componente (NFR-LOG-001)
- [x] `EngramModule.DownloadEngramToTempAsync` agregado (tarea 10.2)
- [x] `FlowForgeModule` tiene método reutilizable `CopySkillsForIde` (tarea 10.3)
- [x] `ManagedPathsSidecar` soporta path custom vía constructor (tarea 8.2)
- [x] `UpdateOrchestrator.UpdateEngramAsync` usa structured logging (MCC > 10)
- [x] Tests actualizados: test de integración para skills update, test de logging estructurado
- [x] `dotnet test` — 100% green en Update suite (61/61 tests pass)
