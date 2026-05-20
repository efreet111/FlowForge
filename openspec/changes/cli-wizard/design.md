# Design: FlowForge CLI Wizard (`forge`)

## Technical Approach

We will build the `forge` CLI as a standalone, Compiled Native AOT application targeting **.NET 10**. 

The overall technical strategy prioritizes:
1.  **Zero Runtime Dependency**: Native AOT compiles the C# codebase directly to platform-specific machine code (ELF on Linux, Mach-O on macOS, PE on Windows).
2.  **No Dynamic Reflection**: All serialization, command routing, and prompt parsing will be executed via statically resolved compile-time logic or Source Generators to guarantee 100% Native AOT compatibility and minimize binary size.
3.  **Visual Excellence**: A custom AOT-safe terminal wrapper that controls ANSI coloring and renders interactive arrow-key selection loops.

---

## Architecture Decisions

| Option | Tradeoff | Decision & Rationale |
|--------|----------|----------------------|
| **Native AOT (`PublishAot: true`)** | Requires platform-specific compiles in CI/CD pipeline. | **CHOSEN**: Instantaneous startup (<1ms), ~10MB footprint, no runtime required, leverages existing .NET 10 skills. |
| **System.Text.Json Source Gen** | Slightly more boilerplate code to declare contexts. | **CHOSEN**: Default System.Text.Json uses runtime reflection which causes warnings and bugs under Native AOT. Source Generators emit serialization code at compile-time. |
| **Manual CLI Routing (`args[0]`)** | More basic than complex CLI libraries. | **CHOSEN**: Eliminates reflection-heavy Command Parser libraries, ensuring guaranteed AOT-safety and maintaining a zero-allocation footprint. |
| **Console `ReadKey` Interactive UI** | Needs manual tracking of console cursor position. | **CHOSEN**: Allows interactive arrow-key selection of DB engines and IDE preferences natively without bloating dependencies. |

---

## Data Flow

### 1. Interactive Config Generation (`forge init`)
```
[User Input] ──→ (Interactive Console UI Loop) ──→ [ForgeConfig Object]
                                                           │
                                                           ▼ (ForgeJsonContext Source Gen)
                                                    [.flowforge.json File] ──→ Trigger `forge compile`
```

### 2. Rules Compiler (`forge compile`)
```
[.flowforge.json] ──→ [Read Config]
                            │
                            ▼
[skills/forge-*/*.md] ──→ [Strip YAML Frontmatter] ──→ [Concatenate prompts] ──→ [Write .cursorrules / .clinerules]
                                                               ↑
[ForgeConfig] ─────────→ [Format Persona & DB Header] ────────┘
```

---

## File Changes

| File | Action | Description |
|------|--------|-------------|
| `src/FlowForge.Cli/FlowForge.Cli.csproj` | **Create** | Project configuration with `<PublishAot>true</PublishAot>` and treated warnings as errors. |
| `src/FlowForge.Cli/Program.cs` | **Create** | Root entry point. Directs routing for `init`, `compile`, and `doctor`. |
| `src/FlowForge.Cli/Commands/InitCommand.cs` | **Create** | Interactive configuration wizard (Arrow keys selection, text input, saves JSON). |
| `src/FlowForge.Cli/Commands/CompileCommand.cs` | **Create** | Opens skills directory, strips frontmatter, inserts persona metadata, compiles target file. |
| `src/FlowForge.Cli/Commands/DoctorCommand.cs` | **Create** | Validates database endpoints and logs diagnostics. |
| `src/FlowForge.Cli/Models/ForgeConfig.cs` | **Create** | Schema matching `.flowforge.json`. |
| `src/FlowForge.Cli/Models/ForgeJsonContext.cs` | **Create** | System.Text.Json Source Generator Context. |
| `src/FlowForge.Cli/Utils/AnsiConsole.cs` | **Create** | Console color helpers and Pixel Dwarf ASCII Art renderer. |

---

## Interfaces / Contracts

### Configuration Schema (`.flowforge.json`)
```json
{
  "Ide": "cursor",
  "DatabaseEngine": "sqlite-cloud",
  "DatabaseConnection": "sqlite://my-turso-db.turso.io",
  "ActiveModels": {
    "orchestrator": "claude-3-5-sonnet",
    "discovery": "claude-3-haiku"
  },
  "Persona": "Senior Architect, 15+ years experience, Rioplatense direct style"
}
```

### AOT JSON Context (`ForgeJsonContext.cs`)
```csharp
using System.Text.Json.Serialization;
using FlowForge.Cli.Models;

namespace FlowForge.Cli.Models;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ForgeConfig))]
internal partial class ForgeJsonContext : JsonSerializerContext
{
}
```

---

## Testing Strategy

| Layer | What to Test | Approach |
|-------|-------------|----------|
| **Unit** | Markdown Metadata Stripping | Verify the regex/parser removes `---` frontmatter blocks cleanly from any string. |
| **Unit** | JSON Serialization (AOT) | Verify `ForgeJsonContext` serializes and deserializes configurations without throwing reflection errors. |
| **Integration** | Rules Compilation | Supply a mock `./skills/` folder structure, run compile logic, and assert the output matches expected headers and prompts. |
| **E2E** | Compiled Binary Executions | Build the Native AOT binary on the CI/CD runner and verify `forge --help` exits with code 0. |

---

## Migration / Rollout

No data migration required. FlowForge CLI is a brand-new component.

---

## Open Questions

- [ ] **Cross-compilation**: Should we supply pre-built shell scripts (`install.sh`) to download the native binaries from Github Releases automatically? (Highly Recommended)
