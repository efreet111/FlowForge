# ADR-017 — Installer Protection Policy (baseline + regression tests)

> **Status**: **Accepted — applied** (2026-08-12) in `flowforge-update-mechanism`; established as standing policy for any change to `src/FlowForge.Installer/*`
> **Date**: 2026-08-12
> **Feature**: `flowforge-update-mechanism` (added as §6.1 "Installer Protection Policy" criterion during arch)
> **Deciders**: Engineering (FlowForge methodology team)
> **Links**: [`installer-baseline.md`](../../.ai-work/flowforge-update-mechanism/installer-baseline.md) · [`spec §6.1`](../../.ai-work/flowforge-update-mechanism/spec.md) · [ADR-001](ADR-001-memory-curation-protocol.md) · [ADR-002](ADR-002-scaffold-doc-policy.md) · [ADR-008](ADR-008-ide-installer-path-matrix.md) · [ADR-010](ADR-010-installer-prompt-for-server-url.md)

---

## Context

The installer (`src/FlowForge.Installer/`) has suffered **regressions in previous features** — most notably the MCP data-loss bugs (S6, two occurrences) where regenerating config files wiped user MCP servers, and the `flowforge status` command being de-registered during implementation (caught in rework cycle 2 only because a baseline existed).

Because the installer **mutates user machines** (binaries, IDE configs, MCP configs, git caches), a regression is not a compile error — it silently corrupts user data or breaks an existing install. The cost of a regression is paid by every existing user, not just new code paths.

**Desired behavior**: any change to `src/FlowForge.Installer/*` must prove it does not break existing functionality before it ships.

---

## Decision drivers

- **User-machine mutations**: installer writes to `~/.local/bin`, `~/.engram`, `~/.cursor`, `~/.gemini`, etc. — a bug is a data-loss event, not a crash.
- **Historic regressions**: prior features broke MCP configs and command registration.
- **Composition over replacement**: new capabilities must reuse existing modules, not rewrite them.
- **Traceability**: "paint" the installer before touching it, so regressions are verifiable.

---

## Decision

**Standing policy for any feature touching `src/FlowForge.Installer/*`:**

1. **Baseline documentation (mandatory)**: produce an `installer-baseline.md` in the feature artifact that documents, for every command (`install`, `update`, `uninstall`, `config`, `status`, `doctor`, `init`):
   - flags, expected behavior, and side effects
   - files read/written
   - ADR references (001, 002, 008, 010, …)
   - This file is the regression reference for the whole feature.

2. **Regression tests (mandatory)**: before implementing, validate the current installer works: `flowforge install --yes` (fresh), `flowforge status`, `flowforge doctor`, `flowforge uninstall`. **If any test fails → ABORT implementation** and resolve the regression first.

3. **Non-regression rule**: no change to `src/FlowForge.Installer/*` may break existing functionality without explicit approval. New orchestrators (e.g. `UpdateOrchestrator`) must **compose** existing modules (`EngramModule`, `FlowForgeModule`, `OpenCodeConfigGenerator`), never replace them. Existing commands must behave exactly as before.

4. **Post-implementation validation (mandatory)**: re-run the regression tests after implementation; validate original commands still work and the new feature does not interfere with them (e.g. `flowforge update --component all` must not break `flowforge install`).

---

## Consequences

### Positive

- Regressions in installer behavior are caught early (Phase 0 baseline before code, Phase 12 re-run after).
- The `flowforge status` de-registration bug was caught precisely because the baseline existed.
- New capabilities are built on verified modules → lower defect density.

### Negative / Costs

- Extra phase-0 work per installer feature (baseline doc + regression runs).
- Baseline doc must be kept current as commands evolve (maintenance cost).

### Applied evidence

- `flowforge-update-mechanism` produced `.ai-work/flowforge-update-mechanism/installer-baseline.md` (86 lines) and ran Phase 0 / Phase 12 regression checks.
- Result: `flowforge status` de-registration detected in rework cycle 2 and fixed; 0 regressions in existing commands at close (PM-1, PM-3, PM-4, PM-5 ✅).
