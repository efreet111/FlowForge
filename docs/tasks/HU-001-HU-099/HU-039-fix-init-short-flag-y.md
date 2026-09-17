---
hu_id: HU-039
title: "BUG: flowforge init -y flag not recognized"
status: draft
category: bugfix
flowforge_slug: "fix-init-short-flag-y"
priority: P2
severity: minor
created_date: 2026-09-16
---

# HU-039 — BUG: flowforge init -y flag not recognized

## User Story

As a FlowForge user running commands in non-interactive mode,
I want the `-y` short flag to work as documented in `--help`,
so that I can use either `-y` or `--yes` interchangeably without errors.

## Problem Description

The `flowforge init --help` output shows:

```
Options:
  --no-flowdoc: omitir creación de estructura docs/, --no-flow-doc    
  -y / --yes: omitir confirmaciones (non-interactive), --yes          
  -v, --verbose                                                       Enable verbose output
```

But running `flowforge init . -y` fails with:

```
Argument '-y' is not recognized.
```

Only the long form `--yes` works.

## Root Cause (hypothesis)

ConsoleAppFramework (CAF) requires short flags to be explicitly registered via the `[Option]` attribute or command naming conventions. The `InitCommand.Run()` method likely declares `bool yes` without a short-name mapping, but the help text was manually written (or auto-generated incorrectly) to suggest `-y` exists.

## Evidence

From user PM-* test log (2026-09-16):

```
$ flowforge init . -y 2>&1
Argument '-y' is not recognized.

Command exited with code 1.

$ flowforge init . --yes 2>&1
(works correctly)
```

## Acceptance Criteria (business-level)

- [ ] AC-1: `flowforge init . -y` behaves identically to `flowforge init . --yes`
- [ ] AC-2: `--help` output accurately reflects which flags exist
- [ ] AC-3: No regression in `--yes` behavior
- [ ] AC-4: Same fix applied to any other command with the same short-flag mismatch (e.g., `flowforge install`, `flowforge update` if affected)

## Scenarios (SDD Spec)

### Happy Path

- [ ] **Short flag works**
  **GIVEN** a project directory
  **WHEN** user runs `flowforge init . -y`
  **THEN** init proceeds non-interactively with defaults (same as `--yes`)

### Edge Cases

- [ ] **Long flag still works**
  **GIVEN** a project directory
  **WHEN** user runs `flowforge init . --yes`
  **THEN** init proceeds non-interactively (no regression)

- [ ] **Help text accuracy**
  **GIVEN** user runs `flowforge init --help`
  **WHEN** help is displayed
  **THEN** flags listed match the flags actually accepted by the CLI

## Files to Investigate

| File | Purpose |
|------|---------|
| `src/FlowForge.Installer/Commands/InitCommand.cs` | `Run(String, Boolean, Boolean)` — check CAF option attributes for `yes` param |
| Possibly other `Commands/*.cs` | Same pattern check (install, update, config, doctor) |

## Owner & Timeline

- **Owner**: Unassigned
- **Priority**: P2 (minor — workaround exists: use `--yes`)
- **Target milestone**: v0.1.0-alpha.13 (bundle with HU-038 if quick) or next release
- **Dependencies**: None

## Definition of Done

- [ ] `-y` flag registered in `InitCommand` (and any other affected commands)
- [ ] Unit test verifying `-y` parses correctly
- [ ] `--help` output verified accurate
- [ ] CHANGELOG entry added

## FlowForge

> This section is managed by FlowForge agents. Do not edit manually.

To implement this HU:

```bash
/flow-start fix-init-short-flag-y
# HU file: HU-039-fix-init-short-flag-y.md
```

- `flowforge_slug`: `fix-init-short-flag-y`
- HU status lifecycle: `draft` → `in-progress` → `done`
