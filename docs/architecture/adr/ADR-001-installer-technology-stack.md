# ADR-001: Stack tecnológico del FlowForge Installer

**Estado:** Aceptado  
**Fecha:** 2026-06-23  
**Autores:** equipo engram / FlowForge  
**Contexto de trabajo:** ENG-301 — Stack Installer

---

## Contexto

El proyecto necesita un instalador unificado que cubra los tres componentes del stack:

- **engram-dotnet** — binario self-contained (backend de memoria, .NET AOT)
- **FlowForge** — skills e integración con IDEs (Cursor, OpenCode, VS Code, Claude Desktop)
- **FlowDoc** — estructura de documentación (`docs/` + `AGENTS.md`)

El instalador debe correr en Linux/macOS y Windows sin requerir que el usuario instale runtimes adicionales (Node, Python, .NET SDK, etc.).

Las opciones evaluadas fueron:

| Opción | Pros | Contras |
|--------|------|---------|
| **Bash + PowerShell** | Rápido de prototipar | Dos lenguajes, wizard limitado, difícil de mantener |
| **Python script** | Muy portable | Requiere Python 3.x en el host; no siempre está disponible |
| **Go binary** | Un binario, AOT-nativo | Fuera del stack del proyecto (C#/.NET) |
| **C# AOT binary** | Mismo stack; self-contained sin runtime; Spectre.Console; tipado fuerte | Compilación más lenta; AOT constraints en algunas librerías |

---

## Decisión

**Usar C# .NET 10 con PublishAot=true** para compilar el installer como binario self-contained.

Librerías seleccionadas:

| Rol | Librería | Motivo |
|-----|----------|--------|
| CLI routing / subcomandos | `ConsoleAppFramework` | AOT-compatible; source generator based |
| UI rica (prompts, colores, progress) | `Spectre.Console` core | Compatible con AOT cuando se usa sin `Spectre.Console.Cli` |
| HTTP / GitHub API | `System.Net.Http` (built-in) | AOT nativo, sin dependencias externas |
| JSON | `System.Text.Json` con `JsonSerializerContext` | AOT-safe con source generators |

> **Nota sobre Spectre.Console.Cli**: la librería CLI de Spectre (`Spectre.Console.Cli`) NO es AOT-compatible (usa reflection para el command tree). Por eso usamos `ConsoleAppFramework` para routing y solo `Spectre.Console` core para rendering.

---

## Distribución

El binario se distribuye mediante:

1. **Bootstrap scripts minimalistas** (`install.sh` / `install.ps1`) que:
   - Detectan OS y arquitectura
   - Descargan el binario AOT desde GitHub Releases
   - Verifican SHA-256
   - Ejecutan `flowforge install`

2. **GitHub Actions release pipeline** (`FlowForge/.github/workflows/release.yml`) que publica binarios `linux-x64` y `win-x64` al tagear `v*`.

3. **Semver pre-releases**: tags con `-alpha`, `-beta`, `-rc` se publican como pre-releases en GitHub y el workflow los marca automáticamente.

---

## Consecuencias

**Positivas:**
- Un solo codebase (.NET) para lógica de instalación — consistente con el ecosistema del proyecto.
- Binarios self-contained — el usuario final no necesita instalar nada antes de correr el installer.
- Wizard interactivo de alta calidad con `Spectre.Console` (multi-select, progress bars, colores).
- Error handling tipado, logs estructurados, fácil de testear unitariamente.

**Restricciones asumidas:**
- El binario debe compilar con `dotnet publish -p:PublishAot=true`. Toda dependencia nueva debe ser AOT-compatible.
- Los source generators (`JsonSerializerContext`, `ConsoleAppFramework`) deben usarse para código que en otro contexto dependería de reflection.
- `Spectre.Console.Cli` está explícitamente excluido. Usar `ConsoleAppFramework` para routing.

---

## Referencias

- [ConsoleAppFramework — AOT support](https://github.com/Cysharp/ConsoleAppFramework)
- [Spectre.Console — AOT compatibility notes](https://spectreconsole.net/)
- `FlowForge/src/FlowForge.Installer/FlowForge.Installer.csproj`
- `FlowForge/src/FlowForge.Installer/TrimmerRoots.xml`
