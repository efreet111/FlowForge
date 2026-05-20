# Apply Progress: Doctor Diagnostic

> **Última actualización**: 2026-05-14
> **Estimación total**: 4-6h
> **Estado general**: 🟡 En progreso (7/14 tasks completados)

---

## Phase 1: Foundation / Infrastructure

**Estado**: ✅ Completo
**Inicio**: 2026-05-14
**Fin**: 2026-05-14
**Duración estimada**: 1-1.5h
**Duración real**: ~20 min

- [x] **1.1** — Create `Engram.Diagnostics` project (`dotnet new classlib`) and add to solution
- [x] **1.2** — Create `src/Engram.Diagnostics/Models/DiagnosticResult.cs` with `DiagnosticResult` and `ComponentHealth` DTOs
- [x] **1.3** — Create `src/Engram.Diagnostics/IDiagnosticService.cs` with `Task<DiagnosticResult> RunDiagnosticsAsync(CancellationToken ct)`

### Notas de implementación
- Proyecto creado con .NET 10.0
- Agregado a `engram-dotnet.slnx`
- DTOs creados en `Models/DiagnosticResult.cs` con `ComponentHealth` y `DiagnosticResult`
- Interfaz `IDiagnosticService` definida en la raíz del proyecto
- Eliminado archivo `Class1.cs` por defecto

### Bloqueos
—

---

## Phase 2: Core Implementation

**Estado**: ✅ Completo
**Inicio**: 2026-05-14
**Fin**: 2026-05-14
**Duración estimada**: 1.5-2h
**Duración real**: ~40 min

- [x] **2.1** — Create `src/Engram.Diagnostics/DiagnosticService.cs` implementing `IDiagnosticService`
- [x] **2.2** — Implement Database Check using `IStore` (`StatsAsync()` with 2s timeout)
- [x] **2.3** — Implement HTTP Server Check using `HttpClient` to ping `/health` endpoint
- [x] **2.4** — Implement MCP Server Check verifying process/socket binding (avoid stdio contention)

### Notas de implementación
- `DiagnosticService` implementa los 3 checks en paralelo con `Task.WhenAll`
- **DB Check**: Usa `StatsAsync()` como operación liviana (IStore no expone SQL directo)
- **HTTP Check**: Ping a `/health` con timeout de 2s, requiere `ENGRAM_SERVER_URL`
- **MCP Check**: Verifica configuración sin causar stdio contention (chequea backend disponible)
- Todos los checks usan `Stopwatch` para medir latencia
- Manejo de `OperationCanceledException` para timeouts vs cancellation
- Build exitoso: 0 errores, 0 warnings

### Bloqueos
—

---

## Phase 3: Integration / Wiring

**Estado**: ⬜ Pendiente
**Inicio**: —
**Fin**: —
**Duración estimada**: 1-1.5h

- [ ] **3.1** — Add project references to `Engram.Diagnostics` in `Engram.Cli.csproj` and `Engram.Mcp.csproj`
- [ ] **3.2** — Register `engram doctor` command in `Engram.Cli/Program.cs` and configure DI
- [ ] **3.3** — Register `mem_doctor` tool in `Engram.Mcp/EngramTools.cs`

### Notas de implementación
—

### Bloqueos
—

---

## Phase 4: Testing / Verification

**Estado**: ⬜ Pendiente
**Inicio**: —
**Fin**: —
**Duración estimada**: 1-1.5h

- [ ] **4.1** — Create `tests/Engram.Diagnostics.Tests` project and add to solution
- [ ] **4.2** — Unit tests: mock `IStore` for DB success/timeout scenarios
- [ ] **4.3** — Unit tests: mock `HttpMessageHandler` for HTTP `/health` success/failure
- [ ] **4.4** — Integration test: CLI `engram doctor` returns exit code 0 when all components healthy

### Notas de implementación
—

### Bloqueos
—

---

## Progreso General

| Fase | Tasks | Completados | Estado |
|------|-------|-------------|--------|
| 1 — Foundation | 1.1-1.3 | 3/3 | ✅ Completo |
| 2 — Core | 2.1-2.4 | 4/4 | ✅ Completo |
| 3 — Integration | 3.1-3.3 | 0/3 | ⬜ Pendiente |
| 4 — Testing | 4.1-4.4 | 0/4 | ⬜ Pendiente |
| **Total** | **14** | **7/14** | **🟡 En progreso (50%)** |
