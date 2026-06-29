# Context Map — fix-installer

## Resumen ejecutivo
- Usuario: reporta que el instalador "se queda pegado" al ejecutar:
  `curl -fsSL https://raw.githubusercontent.com/efreet111/FlowForge/main/install/install.sh | bash`
- Objetivo: identificar causa probable, qué verificar y si las modificaciones locales corrigen el bloqueo.

## Hipótesis de causa raíz (ordenadas por probabilidad)
1. Interacción bloqueada por el wizard del binario `flowforge` / `engram` (TTY / modo headless): el instalador intenta reconnectar stdin y después invoca el binario que puede quedarse esperando input o una llamada HTTP que no responde. (Alta)
2. Asset/release ausente o nombre de binario distinto en GitHub Releases → `curl`/descarga queda bloqueada o `flowforge` instalado no coincide con lo esperado. (Media)
3. Lógica de detección de TTY o reconexión insuficiente al usar `curl | bash` hace que el instalador lance el modo interactivo en vez de headless y quede esperando. (Media)
4. MCP (engram) no está corriendo en el host o `engram` no está instalado en `~/.local/bin/engram`, por lo que las llamadas `mem_*` fallan con "An error occurred". Esto explicaría por qué la memoria no responde, pero no necesariamente el "pegado" del instalador. (Media‑baja)
5. Problemas de red / GitHub API rate‑limit → llamadas a GitHub (GetLatestVersionAsync / descarga) tardan o fallan silenciosamente. (Baja)

## Archivos relevantes (evidencia)
- `install/install.sh` — descarga el binario y reconecta stdin; decision TTY → invoca `"${INSTALL_DIR}/flowforge" install` o `--yes`.

```116:133:install/install.sh
if [ -t 1 ] && [ -e /dev/tty ]; then
  exec </dev/tty 2>/dev/null || true
fi
...
if [ -t 0 ]; then
  "${INSTALL_DIR}/flowforge" install
else
  "${INSTALL_DIR}/flowforge" install --yes
fi
```

- `src/FlowForge.Installer/Modules/EngramModule.cs` — el wizard de .NET llama `GetLatestVersionAsync` y `DownloadEngramAsync`; si `GetLatestVersionAsync` devuelve null devuelve early (no instala), pero si la descarga queda en `DownloadEngramAsync` puede bloquearse por `http.SendAsync` o durante la copia del stream.

```18:26:src/FlowForge.Installer/Modules/EngramModule.cs
version = await ctx.GitHub.GetLatestVersionAsync("efreet111/engram-dotnet", cfg.Channel);
...
ok = await ctx.GitHub.DownloadEngramAsync(version, PathHelper.EngramBinary);
```

- `src/FlowForge.Installer/Infrastructure/GitHubReleasesClient.cs` — construye URLs de releases y nombre de asset `engram-linux-x64`; si el release no existe, `GetLatestVersionAsync` devuelve `null` (capture y log).
- `src/FlowForge.Installer/Infrastructure/PathHelper.cs` — rutas usadas para `flowforge` y `engram` (`~/.local/bin/flowforge`, `~/.local/bin/engram`).
- `.cursor/mcps/user-engram` descriptors — los JSON de herramientas existen, pero el server no está respondiendo (calls retornaron "An error occurred").

## Reusable patterns encontrados (step 5 — obligatorio)
- `src/FlowForge.Installer/Modules/EngramModule.cs` (handle de descarga + MCP merge) → puede reutilizarse tal cual para instalar `engram` en otros proyectos; no requiere rediseño.
- `src/FlowForge.Installer/Infrastructure/GitHubReleasesClient.cs` (client para releases) → patrón reutilizable para resolver assets y checksums.
- Resultado negativo: no se encontró en el repo una implementación "drop‑in" que evite la necesidad de verificar la presencia de TTY cuando el instalador se ejecuta como `curl | bash` — la lógica actual intenta reconectar `/dev/tty` y usar `--yes` si no hay TTY.

## Qué verificar (pasos reproductibles)
1. Verificar releases en GitHub:
   - GET https://api.github.com/repos/efreet111/FlowForge/releases (¿existe tag y asset `flowforge-linux-x64`?)
   - GET https://github.com/efreet111/FlowForge/releases/download/<tag>/flowforge-linux-x64 (HTTP 200)
2. Ejecutar localmente (no piped) para observar logs:
   - `bash -x install/install.sh --version <tag>` o `bash install/install.sh --version vX.Y.Z` desde repo clonado.
3. Si ya instalaste `~/.local/bin/flowforge`, ejecutar:
   - `~/.local/bin/flowforge install --yes` y revisar `~/.engram/install.log` y salidas.
4. Revisar si `~/.local/bin/engram` existe y es ejecutable; intentar `~/.local/bin/engram mcp mem_stats` (si el binario lo permite) o ejecutar `engram doctor` si existe.
5. Reproducir el caso que reporta el usuario (curl | bash) en entorno con red monitorizada; si "se queda pegado", pausar con Ctrl+Z / strace para ver en qué syscalls está esperando (si el usuario lo autoriza).

## Cambios locales sin commitear (impacto)
- Hay modificaciones en `src/FlowForge.Installer/*` (PathHelper, EngramModule, csproj) y `ide/install.sh` sin commit. Estas correcciones (p. ej. "skip confirmation prompt in headless mode") podrían solucionar situaciones de bloqueo al instalar en entornos no interactivos; sin embargo, los binarios publicados en GitHub no incluirán estos fixes hasta que se haga un release.

## Sugerencias de mitigación / fixes
- Inmediato (workaround):
  - Clonar repo y ejecutar `bash install/install.sh --version <known-good-tag>` o ejecutar el binario local `dotnet run --project src/FlowForge.Installer -- install --yes`.
  - Instalar manualmente `flowforge` y `engram` descargando los assets conocidos.
- Corto plazo:
  - Commit + tag + release que incluya las correcciones del installer (skip headless confirmation, lectura de versión por metadata).
  - Añadir logging más verboso alrededor de las llamadas HTTP (GetLatestVersionAsync / DownloadEngramAsync) y timeouts configurables.
  - Robustecer la reconexión de TTY: comprobar `/dev/tty` y, en alternativa, usar `script -q -c` o `setsid` para garantizar un descriptor válido cuando sea necesario.
- Largo plazo:
  - Añadir un modo `--diagnose` que ejecute checks offline (GitHub reachable, binarios disponibles, MCP ejecutable) y produzca un `install.diagnose.json`.

## Open questions
- BLOCKER: ¿Querés que yo intente reproducir la instalación desde este entorno (requiere acceso de red outbound y permisos)?  
- OPTIONAL: ¿Subimos/commitamos los cambios locales y hacemos un release para validar que el problema desaparece en binario?  
- OPTIONAL: ¿Querés que intente arrancar el MCP localmente (si el binario `engram` está presente) para listar memorias?

## Relación con la memoria (engram)
- Si el MCP estuviera funcionando, podríamos usar `mem_stats` / `mem_search` para localizar observaciones relacionadas con el instalador o previous incidents. Los descriptors MCP existen en el workspace, pero el servidor no responde — dato de acción: arrancar `engram` o verificar `~/.engram` para logs.

---
Generated by forge-discovery — contexto listo para `forge-arch`.

