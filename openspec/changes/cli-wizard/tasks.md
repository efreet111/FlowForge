# Tasks: FlowForge CLI Wizard (`forge`)

## Phase 1: Foundation & Models
- [ ] 1.1 Create `src/FlowForge.Cli/FlowForge.Cli.csproj` with `<PublishAot>true</PublishAot>`, treated warnings as errors, and treat JSON source gen enabled.
- [ ] 1.2 Create `src/FlowForge.Cli/Models/ForgeConfig.cs` with the Schema for `.flowforge.json`.
- [ ] 1.3 Create `src/FlowForge.Cli/Models/ForgeJsonContext.cs` as a partial class extending `JsonSerializerContext` for AOT-safe serialization.
- [ ] 1.4 Create `src/FlowForge.Cli/Utils/AnsiConsole.cs` containing the Dwarf Forge ASCII Art banner and helper methods for ANSI colors.

## Phase 2: Rules Compilation (`forge compile`)
- [ ] 2.1 Create `src/FlowForge.Cli/Commands/CompileCommand.cs` with the method `Execute()`.
- [ ] 2.2 Add YAML frontmatter parser and stripper using regex/substring logic inside `CompileCommand.cs`.
- [ ] 2.3 Add dynamic rules assembler (formats active Persona and DB Engine header, concatenates stripped skills prompts).
- [ ] 2.4 Add file writer logic to save results in `.cursorrules` or `.clinerules` in the workspace root.

## Phase 3: Interactive CLI Wizard (`forge init`)
- [ ] 3.1 Create `src/FlowForge.Cli/Commands/InitCommand.cs` with the method `Execute()`.
- [ ] 3.2 Add terminal-based overwrite protection check (asks user confirmation `[y/N]` if `.flowforge.json` already exists).
- [ ] 3.3 Add Console arrow-keys intercept loop (`Console.ReadKey(true)`) to render interactive database engine selection list.
- [ ] 3.4 Add interactive text prompt collection for API keys, LLM models, and Persona details.
- [ ] 3.5 Add config saver (.flowforge.json writer) and automatic trigger of `CompileCommand.Execute()`.

## Phase 4: Diagnostics & Routing (`forge doctor`)
- [ ] 4.1 Create `src/FlowForge.Cli/Commands/DoctorCommand.cs` with a connection handshake ping (`HttpClient` or socket check).
- [ ] 4.2 Add console report printing: `[PASS]` in green or `[WARN]` in yellow (signaling fallback to local memory) without crashing.
- [ ] 4.3 Create `src/FlowForge.Cli/Program.cs` as the entry point using manual switch-case routing over `args` parameter (`init`, `compile`, `doctor`, `--help`).

## Phase 5: Verification & Compilation Test
- [ ] 5.1 Add unit tests for frontmatter stripping logic and AOT JSON serialization/deserialization.
- [ ] 5.2 Compile AOT binary locally: `dotnet publish -r linux-x64 -c Release`.
- [ ] 5.3 Verify all specs scenarios manually against the compiled native binary outputs.
