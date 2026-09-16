# ADR-020 — User Identity from Config (CLI flags are display-only, never auth)

> **Status**: **Accepted — applied** (2026-08-23) in `ff-003-onboarding-flow`
> **Date**: 2026-08-23
> **Feature**: `ff-003-onboarding-flow` (`flowforge onboard`)
> **Deciders**: Engineering (FlowForge methodology team)
> **Links**: [Engram obs #118](https://engram/observation/118) · [spec §1.3 AD-4 + FR-014](../../.ai-work/ff-003-onboarding-flow/spec.md) · [ADR-019](ADR-019-http-first-cli-fallback-integration.md) · [ADR-001](ADR-001-memory-curation-protocol.md)

---

## Context

The onboarding command reads memories from engram, which isolates data per-user (team scope +
`X-Engram-User` header / `ENGRAM_USER`). The preliminary spec proposed `--user <handle>` as a way
for a team lead to prepare onboarding for a new member.

Discovery found (context-map §9): engram isolates by **who runs the command**, not by the
`--user` flag value. Allowing a CLI flag to control identity would let any user read another user's
team memories — a **spoofing** vulnerability (STRIDE T1).

**Desired behavior**: identity always from the local, trusted config; the `--user` flag only changes
what the briefing header displays.

---

## Decision drivers

- **Security**: prevent identity spoofing (T1) — `--user other@team.dev` must not read other users' data.
- **Consistency**: match `EngramModule` identity resolution already used by the installer.
- **Least surprise**: the user who runs the command is the identity; the flag is a label.

---

## Decision

**Identity source of truth** (for `X-Engram-User` header / `ENGRAM_USER`):

1. `~/.engram/config.json` → `sync.user`
2. fallback → `ENGRAM_USER` env var
3. fallback → `Environment.UserName`

**The `--user` flag is display-only** (briefing header) and is **never** used to filter or
authenticate memory access.

This rule is a standing security pattern for any installer command that reads engram data.

---

## Consequences

### Positive

- Identity spoofing is structurally impossible via CLI flags (T1 verified PASS).
- Consistent identity behavior across installer commands (`EngramModule` parity).
- Team-lead onboarding flow (`--user nuevo@team.dev`) still works as a **label** — the exported
  ONBOARDING.md is attributed correctly, but data access is the runner's own.

### Negative / Costs

- A team lead cannot generate a briefing scoped to a *different* user's memories via `--user`
  (would need server-side identity switching — out of scope, not a v1 need).
- UX ambiguity: users may expect `--user` to filter data; mitigated by docs and the display-only
  behavior being explicit in the briefing header.

### Applied evidence

- `.ai-work/ff-003-onboarding-flow/`: FR-014 tests assert `X-Engram-User` header = `sync.user`
  regardless of `--user`; STRIDE T1 verified PASS in verify-report.