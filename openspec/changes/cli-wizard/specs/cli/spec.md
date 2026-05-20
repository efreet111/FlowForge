# FlowForge CLI Specification

## Purpose

The FlowForge CLI (`forge` executable) provides a zero-dependency, compiled Native AOT command-line terminal utility for bootstrapping workspace configurations, installing agentic skills, compiling IDE-native instructions, and diagnosing environment integrity.

---

## Requirements

### Requirement: Interactive Workspace Initialization (`forge init`)

The CLI MUST guide the developer through an interactive, keyboard-driven terminal wizard to establish the project configurations, saving them to `.flowforge.json` in the root workspace.

#### Scenario: Clean initialization
- GIVEN a workspace without a `.flowforge.json` configuration file
- WHEN the user runs the command `forge init`
- THEN the system MUST render a stylized ASCII Dwarf Forge banner in ANSI colors
- AND it MUST prompt the user to select their database engine (`sqlite-local`, `sqlite-cloud`, `postgres`) using keyboard arrow keys
- AND it MUST prompt the user for API credentials and LLM model selections per skill
- AND it MUST prompt the user for their custom Agentic Persona
- AND it MUST write the validated parameters into `.flowforge.json`
- AND it MUST automatically execute the `forge compile` command upon completion

#### Scenario: Prevent accidental override
- GIVEN an already existing `.flowforge.json` in the root directory
- WHEN the user runs `forge init`
- THEN the system MUST prompt the user for explicit overwrite confirmation `[y/N]`
- AND if the user cancels `[N]`, it MUST exit without modifying the existing configuration

---

### Requirement: Automated Rules Compilation (`forge compile`)

The CLI MUST parse all 7 physical `@forge-` skills from `./skills/`, strip their YAML metadata, inject the runtime configuration variables, and compile them into a unified rules file for the IDE.

#### Scenario: Compiling Cursor rules
- GIVEN a valid `.flowforge.json` with `ide: "cursor"`
- AND a local `./skills/` directory containing the 7 `@forge-` skills folders with their `SKILL.md` prompts
- WHEN the user runs `forge compile`
- THEN the system MUST read each `SKILL.md` file, stripping out its YAML frontmatter block
- AND it MUST construct a dynamic header block containing the configured **Persona** and **DB Engine** settings
- AND it MUST concatenate the header and all 7 stripped prompts into a single `.cursorrules` file in the workspace root
- AND it MUST print a success message detailing the compiled size in bytes

#### Scenario: Compiling Cline rules
- GIVEN a valid `.flowforge.json` with `ide: "cline"`
- WHEN the user runs `forge compile`
- THEN the system MUST output the compiled instructions to `.clinerules` in the workspace root

---

### Requirement: Integration Diagnostics (`forge doctor`)

The CLI MUST verify database connectivity to ensure the active workspace is correctly integrated with the `engram-dotnet` neuronal storage engine.

#### Scenario: Successful Postgres ping
- GIVEN a `.flowforge.json` configured to use a PostgreSQL database cloud instance
- WHEN the user runs `forge doctor`
- THEN the system MUST attempt to establish a network handshake/connection to the endpoint
- AND if successful, it MUST print `[PASS] Database connection reachable` in green text

#### Scenario: Offline fallback notification
- GIVEN a configured database connection that is offline or throws a network timeout
- WHEN the user runs `forge doctor`
- THEN the system MUST NOT crash
- AND it MUST print `[WARN] Database offline. Degrading gracefully to local files in .engram/local_memory/` in yellow text
