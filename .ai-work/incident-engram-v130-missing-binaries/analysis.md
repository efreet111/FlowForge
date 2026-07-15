# Incidente — engram-dotnet v1.3.0 publicado sin binarios

**Fecha**: 2026-07-14
**Severidad**: P1 (bloquea instalación fresh de FlowForge via `curl|bash`)
**Estado**: Resuelto por `v0.1.0-alpha.12` (publicado 2026-07-15T02:18:01Z)
**Reportado por**: Usuario final al correr `curl -fsSL .../install/install.sh | bash`

---

## 1. Síntoma

El usuario ejecutó la instalación one-liner y vio:

```
Instalando engram-dotnet...
Buscando última versión...
Error al descargar engram-dotnet. Ver ~/.engram/install.log para detalles.
```

El `~/.engram/install.log` muestra:

```
[2026-07-14 21:12:57] [INFO] Download: https://github.com/efreet111/engram-dotnet/releases/download/v1.3.0/engram-linux-x64
[2026-07-14 21:12:57] [ERROR] DownloadAndVerify error: Response status code does not indicate success: 404 (Not Found).
```

El installer intenta descargar `engram-linux-x64` del release `v1.3.0` y recibe 404.

---

## 2. Root cause — dos problemas encadenados

### Problema A: engram-dotnet v1.3.0 se publicó SIN binarios

El release `v1.3.0` de `efreet111/engram-dotnet` (publicado 2026-07-11T03:35:08Z) tiene **0 assets**:

```
Tag: v1.3.0
Name: v1.3.0 — Sync recovery + Self-loop detection
Prerelease: False
Assets count: 0
```

El body del release dice:

> Notes extraídas del `CHANGELOG.md` sección `## [1.3.0] — 2026-07-06` para `gh release create v1.3.0 --notes-file ...`.
> Generado por session FlowForge Orchestrator (cierre de MUST existentes).

Eso indica que el release se creó **manualmente** con `gh release create v1.3.0 --notes-file ...` — **sin disparar el workflow `Release`** que compila los binarios AOT y los sube como assets.

El repo `engram-dotnet` tiene el workflow `.github/workflows/release.yml` que se dispara con `push: tags: v*` y compila `engram-linux-x64` + `engram-win-x64.exe` + `e_sqlite3.dll` + `libe_sqlite3.so`. Pero los runs recientes en `gh run list --repo efreet111/engram-dotnet` son todos del workflow `CI`, **ninguno del workflow `Release`**. El tag `v1.3.0` se creó directamente (probablemente via `gh release create` que hace push del tag implícito), pero el workflow `Release` no se disparó o falló silenciosamente.

El release anterior `v1.2.1` (2026-06-28) SÍ tiene los 8 assets esperados:

```
engram-linux-x64       (104541 KB)
engram-win-x64.exe      (107116 KB)
e_sqlite3.dll           (1718 KB)
libe_sqlite3.so         (1299 KB)
+ 4 archivos .sha256
```

### Problema B: el installer NO salteaba releases sin assets (hasta alpha.12)

El manifest de FlowForge (`install/manifest.yaml`) ya documentaba la posibilidad:

```yaml
# Latest GitHub tag may be notes-only (e.g. v1.3.0); installer skips releases without binaries.
requires:
  engram-dotnet: ">=0.3.0"
```

Y el código en `src/FlowForge.Installer/Infrastructure/GitHubReleasesClient.cs` tiene la lógica `GetLatestEngramVersionAsync` que itera releases y saltea los que no tienen el asset esperado:

```csharp
/// engram-dotnet v1.3.0+ may publish release notes without assets; skip empty releases.
async Task<string?> GetLatestEngramVersionAsync(string channel, CancellationToken ct)
{
    var releases = await FetchReleasesPageAsync(EngramRepo, 20, ct);
    var assetName = GetEngramAssetName();
    foreach (var release in releases)
    {
        if (release.Draft) continue;
        if (await ReleaseAssetExistsAsync(release.TagName, assetName, ct))
        {
            _log.Info($"engram-dotnet: usando {release.TagName} ({assetName} disponible)");
            return release.TagName;
        }
        _log.Warn($"engram-dotnet: omitiendo {release.TagName} — asset {assetName} no publicado");
    }
    return null;
}
```

**Pero esa lógica fue añadida en el commit `6fc63c2 fix(installer): skip engram releases without published binaries`**, que **solo está incluido en el release `v0.1.0-alpha.12`**:

```
$ git tag --contains 6fc63c2
v0.1.0-alpha.12
```

Los releases `alpha.7` a `alpha.11` (todos del 2026-06-29) **NO tienen** esa lógica. El binary que el usuario descargó (reportado como alpha.9 en su output, alpha.7 en el manifest log) usa la lógica vieja que simplemente agarra el primer release (el más nuevo = v1.3.0) sin verificar si tiene assets → 404.

---

## 3. Cadena causal

```
engram-dotnet v1.3.0 publicado manualmente sin binarios (2026-07-11)
        │
        ▼
FlowForge installer alpha.7..alpha.11 no saltea releases sin assets
        │
        ▼
flowforge install intenta descargar v1.3.0/engram-linux-x64
        │
        ▼
GitHub devuelve 404 (asset no existe)
        │
        ▼
EngramModule.InstallAsync reporta "Error al descargar engram-dotnet"
        │
        ▼
Instalación fresh de FlowForge falla en el componente engram-dotnet
(FlowForge skills sí se instalan, pero el backend de memoria queda ausente)
```

---

## 4. Impacto

- **Usuarios nuevos** que corren `curl|bash` con el binary alpha.7..alpha.11: engram-dotnet NO se instala. FlowForge skills sí (porque ese módulo no depende de engram). Pero el MCP de engram queda sin configurar, y `flowforge doctor` reporta FAIL en `engram binary` y `engram en PATH`.
- **Usuarios existentes** con engram ya instalado (v1.2.1 o anterior): no afectados, porque el installer saltea la descarga si el binario ya existe.
- **CI de FlowForge**: el job `Happy Path Install (Linux/Windows)` en `test-installer.yml` puede fallar si depende de descargar engram fresh. (En el momento del incidente, el CI de alpha.12 pasó porque el nuevo binary saltea v1.3.0.)

---

## 5. Resolución

### Resolución aplicada (FlowForge lado)

Publicación del release `v0.1.0-alpha.12` (2026-07-15T02:18:01Z) que incluye el commit `6fc63c2` con la lógica de saltear releases sin assets. El nuevo binary:

1. Lista los últimos 20 releases de `efreet111/engram-dotnet`.
2. Itera desde el más nuevo al más viejo.
3. Para cada release, verifica si el asset `engram-linux-x64` (o `engram-win-x64.exe` en Windows) existe via HTTP HEAD.
4. Saltea releases sin assets (como v1.3.0) con un warning.
5. Descarga el primer release que SÍ tiene el asset (v1.2.1 en este caso).

Validación post-alpha.12:

```
$ curl -fsSL https://raw.githubusercontent.com/efreet111/FlowForge/main/install/install.sh | bash
Instalando FlowForge v0.1.0-alpha.12...
...
Instalando engram-dotnet...
  ✓ engram-dotnet v1.2.1 instalado en /home/<user>/.local/bin/engram
```

### Resolución pendiente (engram-dotnet lado)

El release `v1.3.0` de `efreet111/engram-dotnet` sigue sin binarios. Acciones recomendadas en ese repo:

1. **Verificar por qué el workflow `Release` no se disparó** para el tag `v1.3.0`. Posibles causas:
   - El tag se creó via `gh release create` que hace push del tag, pero el workflow tenía un typo o filtro que impidió dispararse.
   - El workflow se disparó pero falló silenciosamente (verificar `gh run list --workflow=release.yml --repo efreet111/engram-dotnet`).
2. **Republicar el release v1.3.0 con binarios**:
   - Opción A: borrar el release existente y re-crear el tag para disparar el workflow.
   - Opción B: subir los assets manualmente al release existente via `gh release upload v1.3.0 out/engram-linux-x64 out/engram-win-x64.exe ...`.
3. **O bien**: dejar v1.3.0 como notes-only y documentar que el installer saltea releases sin binarios (que es justo lo que hace alpha.12).

Mientras tanto, el installer alpha.12 saltea v1.3.0 y usa v1.2.1, que tiene los binarios. Funcionalmente correcto, pero los usuarios no reciben las features de v1.3.0 (sync recovery + self-loop detection) hasta que se republicen los binarios.

---

## 6. Lecciones

1. **Releases manuales vs automatizados**: crear releases via `gh release create` sin disparar el workflow de build deja releases sin binarios. Siempre que exista un workflow `Release` que compile assets, los tags deben dispararlo. Verificar con `gh run list --workflow=release.yml` después de crear un tag.

2. **Defensa en profundidad en el installer**: el installer no puede asumir que el release más reciente de una dependencia tiene assets. La lógica de saltear releases sin assets (commit `6fc63c2`) es una salvaguarda necesaria. Sin ella, un release notes-only upstream rompe la instalación fresh de todos los usuarios.

3. **Versionado del manifest**: el comment en `install/manifest.yaml` decía "installer skips releases without binaries" **antes** de que el installer realmente lo hiciera. El manifest documentaba comportamiento aspiracional, no real. El commit `6fc63c2` alineó el código con el manifest. Lección: no documentar comportamiento futuro en archivos que se publican como contract.

4. **Detección via CI**: el job `Happy Path Install` del CI de FlowForge debería fallar si engram-dotnet no se instala. En el momento del incidente, el CI de alpha.12 pasó porque el nuevo binary saltea v1.3.0. Pero si el CI hubiera corrideado contra alpha.7..alpha.11 después del 2026-07-11 (cuando se publicó v1.3.0 sin binarios), habría fallado y detectado el problema antes. Recomendación: agregar un cron job que corra `test-installer.yml` semanalmente contra el release más reciente, no solo en PRs.

---

## 7. Referencias

- **Release engram-dotnet v1.3.0** (sin assets): https://github.com/efreet111/engram-dotnet/releases/tag/v1.3.0
- **Release engram-dotnet v1.2.1** (con assets): https://github.com/efreet111/engram-dotnet/releases/tag/v1.2.1
- **Release FlowForge v0.1.0-alpha.12** (con fix): https://github.com/efreet111/FlowForge/releases/tag/v0.1.0-alpha.12
- **Commit fix**: `6fc63c2 fix(installer): skip engram releases without published binaries`
- **PR merge**: `85fef06 Merge pull request #8` (FlowForge)
- **Log del incidente**: `~/.engram/install.log` (línea 2026-07-14 21:12:57)
- **Manifest FlowForge**: `install/manifest.yaml` (campo `requires.engram-dotnet`)
- **Código del fix**: `src/FlowForge.Installer/Infrastructure/GitHubReleasesClient.cs` método `GetLatestEngramVersionAsync`

---

## 8. Estado de los releases FlowForge

| Tag | Fecha | ¿Tiene fix skip-empty-releases? | ¿Funciona instalación fresh? |
|-----|-------|-------------------------------|---------------------------|
| v0.1.0-alpha.7..alpha.11 | 2026-06-29 | No | No (404 en engram v1.3.0) |
| v0.1.0-alpha.12 | 2026-07-15 | Sí | Sí (saltea v1.3.0, usa v1.2.1) |

Los releases alpha.7..alpha.11 quedan rotos para instalación fresh. No se recomienda su uso. El release vigente es `v0.1.0-alpha.12`.
