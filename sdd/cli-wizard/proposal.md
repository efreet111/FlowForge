# Proposal: FlowForge CLI Wizard (`forge`) in C# Native AOT

## 1. Objective & Context

The goal is to build a high-performance, standalone, zero-dependency command-line interface (CLI) named `forge` (or `flowforge`) using **C# .NET 10 Native AOT**. 

This CLI will serve as the setup wizard and rules compiler for the FlowForge methodology, replacing manual skill copying and `.cursorrules` pasting with a single-command installation experience. By using Native AOT, we achieve sub-millisecond startup times and single-binary packaging (no .NET Runtime or Node.js required on the host system), providing a premium developer experience aligned with the team's C# expertise.

---

## 2. Scope

### In Scope:
- **Interactive CLI Wizard (`forge init`)**: A console UI featuring an ASCII-Art banner, interactive prompts (using keyboard arrow-key navigation) to configure database engines, LLM models, and the agent's custom Persona.
- **Rules Compiler (`forge compile` / `forge sync`)**: Dynamically parses the 7 physical skills (`skills/forge-*/*.md`), strips their YAML frontmatter, injects the user's configuration at the top, and outputs a compiled `.cursorrules` or `.clinerules` file.
- **Zero-Dependency Native Binary**: Configured with `<PublishAot>true</PublishAot>` and AOT-safe JSON serialization using System.Text.Json Source Generators.
- **Portability**: Cross-compilation targets for Linux x64, macOS (ARM64/x64), and Windows x64.

### Out of Scope:
- Modifying the core database engine of `engram-dotnet` (the CLI will only read/write `.flowforge.json` configuration blocks that connect to it).
- Web-based graphic interfaces (the wizard is 100% terminal-native).

---

## 3. Technical Design

### Project Structure (C# .NET 10)
```
src/FlowForge.Cli/
├── FlowForge.Cli.csproj         ← Configurado con <PublishAot>true</PublishAot>
├── Program.cs                  ← Punto de entrada, parser de comandos CLI
├── Commands/
│   ├── InitCommand.cs          ← Lógica del CLI Wizard interactivo
│   ├── CompileCommand.cs       ← Lógica de lectura y compilación de skills
│   └── DoctorCommand.cs        ← Diagnóstico de conexión de base de datos
├── Models/
│   └── ForgeConfig.cs          ← Modelo de datos (.flowforge.json)
└── Utils/
    ├── AnsiConsole.cs          ← Colores de consola, ASCII Art, Dwarf Forge
    └── AotJsonContext.cs       ← System.Text.Json Source Generator
```

### Native AOT Optimization: Zero-Reflection JSON
Standard JSON serializers use heavy reflection at runtime, which breaks Native AOT compilation or inflates binary sizes. We will utilize **System.Text.Json Source Generators**:

```csharp
[JsonSerializable(typeof(ForgeConfig))]
internal partial class ForgeJsonContext : JsonSerializerContext
{
}
```
This forces the C# compiler to generate serialization code *at compile time*, making it 100% AOT-safe and incredibly fast.

### Interactive Terminal UI (arrow keys support)
Instead of forcing the user to type numbers, we will write a lightweight custom console loop that intercepts arrow keys (`Console.ReadKey(true)`) to render beautiful, interactive selection lists directly in the terminal:

```
⚒️ Seleccioná el motor de base de datos de engram-dotnet:
  > [ ] SQLite Local (.db local)
    [*] SQLite Cloud (Turso/Nube)
    [ ] PostgreSQL Remoto (Nube/Staging)
```

---

## 4. The Compilation Engine (`forge compile`)

When the compiler runs, it executes the following steps:
1.  **Reads Config**: Loads `.flowforge.json`.
2.  **Generates Header**: Renders a dynamic header block containing the configured **Persona** and the active database context:
    ```markdown
    # SOP de Desarrollo FlowForge — INSTRUCCIÓN OBLIGATORIA
    
    ## 👤 Configuración de Persona Inyectada
    Actúas bajo la siguiente personalidad y tono:
    > "{config.Persona}"
    
    ## 🔌 Conexión de Memoria
    Estás conectado al motor engram-dotnet en modo: {config.DatabaseEngine}
    ```
3.  **Parses Skills**: Loops through the 7 directories in `skills/forge-*`, reads `SKILL.md`, strips YAML frontmatter, and appends the instructions neatly.
4.  **Writes Output**: Saves the resulting file as `.cursorrules` or `.clinerules` in the root workspace.

---

## 5. Alternatives & Tradeoffs

| Approach | Pros | Cons | Effort |
|----------|------|------|--------|
| **C# Native AOT (Chosen)** | Native execution, no runtime needed, sub-ms startup, fits team tech stack. | Requires separate builds for macOS, Linux, and Windows in CI/CD. | Medium |
| **Node.js Script** | Single script file runs everywhere Node is present. | Requires Node.js installed, parsing JSON at runtime, slower startup. | Low |

---

## 6. Verification Plan

### Manual Verification:
- Validate the C# program compiles correctly with AOT warnings treated as errors: `dotnet publish -r linux-x64 -c Release`.
- Test command parsing: `forge init` and `forge compile`.
- Verify the generated `.cursorrules` file correctly contains the concatenated skills and the custom persona header.
