# Installer Baseline (ADR-017)

This document is the canonical reference for all `flowforge` CLI commands.
Per ADR-017, any new command must update this baseline.

Last updated: 2026-08-19 (FF-003 onboarding flow)

---

## Commands

| Command | Description | Side effects | Exit codes |
|---------|-------------|--------------|------------|
| `flowforge` | Alias for `status` | None | 0 success, 1 error |
| `flowforge install` | Install engram-dotnet + IDE integrations | Writes binaries, config, MCP | 0 success, 1 error |
| `flowforge install --yes` | Non-interactive install | Same as above | 0 success, 1 error |
| `flowforge status` | Show installed components and sync state | None | 0 success, 1 error |
| `flowforge doctor` | Diagnostic checks for the FlowForge stack | None | 0 all OK, 2 check failures |
| `flowforge init [path]` | Initialize FlowForge in a project | Writes .flowforge.json, .agents/ | 0 success, 1 error |
| `flowforge update [--check]` | Update installed components | Overwrites binaries | 0 success, 1 error |
| `flowforge uninstall` | Remove FlowForge components | Deletes binaries, config | 0 success, 1 error |
| `flowforge config get <key>` | Read a config value | None | 0 success, 1 error |
| `flowforge config set <key> <value>` | Write a config value | Modifies ~/.engram/config.json | 0 success, 1 error |
| `flowforge onboard` | First-day onboarding briefing from engram memories | None (read-only) | 0 success, 1 runtime error, 2 pre-check failure |

---

## `flowforge onboard` — detailed reference

### Synopsis

```
flowforge onboard [--project <name>] [--user <handle>] [--scope team|personal]
                  [--output <path>] [--limit <n>] [--no-interactive]
```

### Flags

| Flag | Type | Default | Description |
|------|------|---------|-------------|
| `--project <name>` | string | auto-detected | Override project name (bypasses detection) |
| `--user <handle>` | string | from config | Display-only user name in briefing header |
| `--scope team\|personal` | string | team | Memory scope (team = shared, personal = user-only) |
| `--output <path>` | string | none | Export briefing to ONBOARDING.md (team scope only) |
| `--limit <n>` | int | 10 | Max items per section (clamped 1..20) |
| `--no-interactive` | bool | false | CI-safe mode: no drill-down prompts |

### Exit codes

| Code | Meaning |
|------|---------|
| 0 | Success (briefing rendered or empty-memory guidance shown) |
| 1 | Runtime error (ambiguous project in non-interactive mode, unexpected failure) |
| 2 | Pre-check failure (engram binary missing, config unreadable) |

### Side effects

**None.** The command is read-only against engram. The only disk write is the optional `--output` file (user-controlled path, team scope only).

### Pre-flight checks

Before any memory query, the command verifies:
1. `engram` binary exists at `~/.local/bin/engram`
2. `~/.engram/config.json` is readable and parseable

If either fails, the command exits 2 with actionable hints.

### Integration path

- **HTTP-first**: If `sync.mode=sync` and the server responds to `/health`, uses the HTTP API (`/context`, `/search`, `/stats`).
- **CLI fallback**: If the server is unreachable or `sync.mode=local`, falls back to `engram` CLI subprocess against local SQLite.

### Security

- All memory text is escaped before terminal rendering (no markup/ANSI injection).
- `--user` is display-only; identity for API calls always comes from config (`sync.user` → `ENGRAM_USER`).
- `--output` defaults to team scope; personal scope is never written to disk.
- Drill-down is opt-in; default previews are truncated to 300 chars.

### Example

```bash
# Interactive briefing (detects project from .flowforge.json or git)
flowforge onboard

# Specify project and export to markdown
flowforge onboard --project flowforge --output ONBOARDING.md

# CI-safe mode (no interactive prompts)
flowforge onboard --no-interactive --limit 5
```

---

## Regression test matrix (ADR-017)

After adding or modifying any command, verify:

| Command | Test | Expected |
|---------|------|----------|
| `flowforge install --yes` | Non-interactive install | Exit 0, binaries present |
| `flowforge status` | Show components | Exit 0, shows installed versions |
| `flowforge doctor` | Diagnostic checks | Exit 0 or 2 (with hints) |
| `flowforge uninstall` | Remove components | Exit 0, binaries removed |
| `flowforge onboard --help` | Show usage | Exit 0, shows all flags |
| `flowforge --help` | List commands | Shows `onboard` in command list |
