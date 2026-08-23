# ADR-019 — HTTP-First Integration with CLI-Subprocess Fallback (installer ↔ engram)

> **Status**: **Accepted — applied** (2026-08-23) in `ff-003-onboarding-flow`
> **Date**: 2026-08-23
> **Feature**: `ff-003-onboarding-flow` (`flowforge onboard`)
> **Deciders**: Engineering (FlowForge methodology team)
> **Links**: [Engram obs #116](https://engram/observation/116) · [spec §1.3 AD-2](../../.ai-work/ff-003-onboarding-flow/spec.md) · [ADR-018](ADR-018-ff003-eng485-onboarding-boundary.md) · [ADR-017](ADR-017-installer-protection-policy.md)

---

## Context

The FlowForge installer needs to read engram memories for the onboarding briefing. Three candidate
integration paths existed (context-map §1 finding 5): (a) engram **CLI subprocess** (`engram
search/context/stats`), (b) **HTTP API** of the sync server (`/search`, `/context`, `/stats`), (c)
**MCP stdio** (inappropriate — the installer is a CLI, not an MCP host).

Key constraint discovered: `engram search/context/stats` have **no `--json` flag** — their output is
plain text with a stable format (`[i] #id (type) — title`). The sync server (verified live:
postgres v1.1.0) returns structured JSON.

**Desired behavior**: prefer structured JSON when available; degrade gracefully to local SQLite via
CLI subprocess when the server is unreachable or `sync.mode=local`.

---

## Decision drivers

- **Reliability**: structured JSON parsing beats fragile text parsing (R5 in context-map, prob Medium).
- **Offline/local mode**: `sync.mode=local` must still produce a briefing from local SQLite.
- **Resilience**: server-down must not crash the command (NFR-006, STRIDE T8).
- **AOT**: `System.Diagnostics.Process` is AOT-safe and already used (`EngramProcessChecker`).
- **Multi-user isolation**: HTTP supports `X-Engram-User` header; CLI relies on local user identity.

---

## Decision

**Primary path**: engram sync server **HTTP API** (`/search`, `/context`, `/stats`) returning
structured JSON, with `X-Engram-User` header set from config identity.

**Fallback path**: when the server is unreachable (bounded timeout, default 30 s, configurable via
`FLOWFORGE_API_TIMEOUT_SECONDS`) or `sync.mode=local`, invoke the **engram CLI as a subprocess**
(`engram search/context/stats`) against local SQLite, parsing the stable text format.

**Explicitly not used**: MCP stdio (the installer is a CLI, not an MCP host).

The path selection is a standing pattern for any future installer↔engram memory integration.

---

## Consequences

### Positive

- Structured JSON primary path → robust parsing, less fragile than text scraping.
- Local/offline resilience → briefing works without the server (prints "usando memorias locales").
- Reuses existing `EngramProcessChecker` Process pattern (AOT-safe).

### Negative / Costs

- Two code paths to maintain (`HttpEngramClient` + `CliEngramClient` behind `IEngramClient`).
- CLI fallback text parsing is a maintenance risk if engram changes its output format (mitigated by
  unit tests on the stable format).

### Applied evidence

- `.ai-work/ff-003-onboarding-flow/`: FR-013 implemented both paths; integration tests cover HTTP
  happy path + CLI fallback; NFR-001 perf verified (< 5 s HTTP, < 2 s local).