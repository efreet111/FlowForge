# ADR-005 — Stack Installer: Headless mode + Native libs + MCP config parity

> **Status**: Proposed
> **Date**: 2026-06-27
> **Feature**: `stack-installer` (ENG-301) + cross-cutting (`engram-dotnet` releases)
> **Deciders**: Engineering (FlowForge methodology team)
> **Links**: [`spec.md`](../.ai-work/stack-installer/spec.md) ·
> [`verify-report.md`](../.ai-work/stack-installer/verify-report.md) ·
> [`EngramModule.cs`](../../src/FlowForge.Installer/Modules/EngramModule.cs) ·
> [`InstallCommand.cs`](../../src/FlowForge.Installer/Commands/InstallCommand.cs) ·
> [engram-dotnet `release.yml`](https://github.com/efreet111/engram-dotnet/blob/main/.github/workflows/release.yml)

---

## Context

Durante la configuración de sync en una máquina CachyOS Linux, el stack installer
falló en 3 puntos que impidieron una instalación headless completa. El análisis
reveló 5 bugs cross-repo que deben resolverse para que el flujo `curl | bash`
funcione en entornos no-interactivos (CI/CD, scripts, OpenCode).

### Síntomas observados (2026-06-27)

1. `flowforge install --yes` → `System.NotSupportedException: Cannot show selection prompt since the current terminal isn't interactive`
2. Binario `engram` descargado manualmente → `DllNotFoundException: Unable to load shared library 'e_sqlite3'`
3. `engram mcp --tools=agent` → `Unrecognized command or argument`
4. Sync no funcional tras configurar `ENGRAM_SYNC_ENABLED=true` porque faltaba `ENGRAM_SERVER_URL`
5. Sin `.dotnet SDK` instalado → necesario solo para build, no para runtime (AOT)

---

## Bug catalogue

### B1 — `--yes` no evita los prompts de Spectre.Console (P0)

**Archivo**: `src/FlowForge.Installer/Commands/InstallCommand.cs:44-81`

El flag `--yes` solo se evalúa en la confirmación final (línea 91). Los 3 prompts
previos se ejecutan incondicionalmente:

- `MultiSelectionPrompt` (componentes: engram-dotnet / FlowForge) — línea 44
- `SelectionPrompt` (modo engram: local / sync) — línea 63
- `MultiSelectionPrompt` (IDEs: Cursor / OpenCode / VS Code) — línea 77

Si no hay TTY (stdin no es terminal), Spectre.Console lanza excepción antes de
llegar a la confirmación.

### B2 — `e_sqlite3.so` no incluida en el release AOT de engram-dotnet (P0)

**Archivo**: `engram-dotnet/.github/workflows/release.yml`

`Engram.Store` usa `Microsoft.Data.Sqlite` v9.0.*, que a su vez depende de
`SQLitePCLRaw.bundle_e_sqlite3`. Esta librería nativa (`e_sqlite3.so` en Linux,
`e_sqlite3.dll` en Windows) es parte del bundle SQLitePCL y DEBE estar en el
mismo directorio que el binario.

El release actual (v1.2.0) empaqueta solo `engram-linux-x64` y `engram-win-x64.exe`
— los archivos de librería nativa no están presentes como assets separados ni
embebidos en el binario (`PublishSingleFile=true` sin `PublishAot=true`).

### B3 — `--tools=agent` no reconocido por engram v1.2.0 (P1)

**Archivo**: `src/FlowForge.Installer/Modules/EngramModule.cs:154` +
`ide/opencode/opencode.flowforge.json`

El comando generado por `WriteMcpJson` y el template de OpenCode incluyen
`"args": ["mcp", "--tools=agent"]`, pero la versión actual del binario solo
acepta `engram mcp` sin subcomandos.

### B4 — `ENGRAM_SERVER_URL` ausente en la config MCP generada (P1)

**Archivo**: `src/FlowForge.Installer/Modules/EngramModule.cs:137-142`

`WriteMcpJson` configura `ENGRAM_DATA_DIR`, `ENGRAM_USER`, y `ENGRAM_SYNC_ENABLED`,
pero omite `ENGRAM_SERVER_URL`. Sin esta variable, el SyncManager no sabe a qué
servidor conectarse, incluso con `ENGRAM_SYNC_ENABLED=true`.

### B5 — `.NET SDK` requerido solo para build, no para runtime (P2)

El binario es self-contained (runtime embebido). El instalador descarga binarios
pre-compilados desde GitHub Releases — correcto. Pero si un usuario necesita
compilar desde fuente, el wizard no verifica que `dotnet` esté instalado antes de
intentar `dotnet publish`.

---

## Decision drivers

- El flujo de instalación debe funcionar 100% headless (`--yes`) para CI/CD y scripts.
- Los binarios self-contained deben incluir TODAS las dependencias nativas necesarias.
- La configuración MCP generada debe ser completa y funcional sin edición manual.
- Los cambios cross-repo (FlowForge + engram-dotnet) deben versionarse
  consistentemente.

---

## Options considered

### Para B1 (headless mode):

| Opción | Descripción | Pros | Contras |
|--------|-------------|------|---------|
| **A) Defaults on `--yes`** | Cuando `yes=true`, usar defaults para todos los prompts: ambos componentes, modo sync, IDEs auto-detectados | Simple, no rompe el wizard interactivo | Los defaults pueden no coincidir con lo que el usuario quiere |
| **B) CLI flags individuales** | Agregar `--engram`, `--no-engram`, `--flowforge`, `--no-flowforge`, `--ides cursor,opencode` | Control granular | Explosión de flags, complejidad en ConsoleAppFramework |
| **C) Abort on no-TTY** | Detectar `!Console.IsInputRedirected` y abortar con mensaje claro | Transparente | No soluciona el problema — sigue sin funcionar headless |

**Decisión preliminar**: **Opción A** — defaults razonables + flag `--yes`.

### Para B2 (e_sqlite3.so):

| Opción | Descripción | Pros | Contra |
|--------|-------------|------|--------|
| **A) Incluir en release** | Agregar `e_sqlite3.so`/`e_sqlite3.dll` como asset en el release de engram-dotnet | Canónico, el binario es verdaderamente self-contained | Requiere cambio en `release.yml` de engram-dotnet |
| **B) Symlink en instalador** | `EngramModule` crea `ln -s /usr/lib/libsqlite3.so ~/.local/bin/e_sqlite3.so` | No requiere cambio en engram-dotnet | Frágil — depende de que el sistema tenga `libsqlite3.so`; no funciona en Windows |
| **C) PublishAot=true** | Cambiar el release a AOT nativo completo | La librería se embebería en el binario | Puede ser incompatible con Microsoft.Data.Sqlite (reflection) |

**Decisión preliminar**: **Opción A + B combinadas** — engram-dotnet incluye la lib
en su release, y FlowForge la verifica/crea symlink como fallback.

### Para B3 (`--tools=agent`):

**Decisión**: Eliminar el flag del template. Es solo `engram mcp`.

### Para B4 (`ENGRAM_SERVER_URL`):

**Decisión**: Agregar `ENGRAM_SERVER_URL` cuando `syncEnabled == true`. Leer de
variable de entorno `ENGRAM_SERVER_URL` si existe; si no, preguntar en modo
interactivo o usar default en modo `--yes`.

---

## Decision

1. **InstallCommand.cs**: Cuando `yes == true`, saltar todos los prompts de
   Spectre.Console y usar defaults. Detectar automáticamente IDEs del sistema.
2. **engram-dotnet release.yml**: Agregar step que copie `e_sqlite3.so` y
   `e_sqlite3.dll` como assets del release (extraídos del NuGet cache tras
   restore).
3. **EngramModule.cs**: 
   - Verificar existencia de `e_sqlite3.so` post-descarga, crear symlink si falta
   - Eliminar `--tools=agent` del comando MCP
   - Agregar `ENGRAM_SERVER_URL` al bloque `env` cuando `syncEnabled == true`
4. **opencode.flowforge.json template**: Eliminar `--tools=agent` de los args.

---

## Consequences

### Positivas
- Instalación 100% headless funcional con `flowforge install --yes`
- Binario engram verdaderamente self-contained (sin pasos manuales)
- Configuración MCP lista para usar sin edición post-instalación
- Compatible con CI/CD y scripts de automatización

### Negativas
- Requiere cambios en 2 repositorios (FlowForge + engram-dotnet)
- El symlink fallback solo funciona en Linux (Windows necesita la DLL en el release)
- Los defaults headless pueden no ser óptimos para todos los usuarios

### Neutral
- El wizard interactivo no cambia su comportamiento
- `--yes` ahora es semánticamente "usar defaults + no preguntar"

---

## Files affected

| Repo | Archivo | Cambio |
|------|---------|--------|
| FlowForge | `src/FlowForge.Installer/Commands/InstallCommand.cs` | Saltar prompts cuando `yes=true` |
| FlowForge | `src/FlowForge.Installer/Modules/EngramModule.cs` | Symlink `e_sqlite3.so`, `ENGRAM_SERVER_URL`, quitar `--tools=agent` |
| FlowForge | `ide/opencode/opencode.flowforge.json` | Quitar `--tools=agent` |
| FlowForge | `~/.config/opencode/opencode.jsonc` | Quitar `--tools=agent` (manual merge) |
| engram-dotnet | `.github/workflows/release.yml` | Incluir `e_sqlite3.so` y `e_sqlite3.dll` como assets |

---

## Verification

- [ ] `flowforge install --yes` completa sin errores en terminal no-interactivo
- [ ] `~/.local/bin/engram serve` arranca sin `DllNotFoundException`
- [ ] `engram mcp` inicia correctamente desde OpenCode
- [ ] `mem_search` retorna resultados del servidor sync
- [ ] `mem_sync_status` muestra sync healthy con `192.168.0.178:7437`
- [ ] Release de engram-dotnet incluye `e_sqlite3.so` y `e_sqlite3.dll`
