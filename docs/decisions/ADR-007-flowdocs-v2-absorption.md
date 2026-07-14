# ADR-007 — Absorb `FlowDocsv2Adoption` branch (FlowDoc v1.1 → v2.0 migration)

> **Status**: Accepted
> **Date**: 2026-07-08
> **Feature**: `absorb/flowdocs-v2-adoption`
> **Deciders**: FlowForge methodology team
> **Trigger**: Branch `FlowDocsv2Adoption` by Crhistian Mendoza (2026-07-08)
> **Links**: [`FlowDocsv2Adoption` branch](https://github.com/efreet111/FlowForge/tree/FlowDocsv2Adoption) ·
> [`ADR-004` FlowDoc integration](ADR-004-flowdoc-integration.md) ·
> [`docs/20-flowdoc-ecosystem.md`](../20-flowdoc-ecosystem.md) ·
> [FlowDoc upstream](https://github.com/crhistianmdz/FlowDocs) (private)

---

## Context

On 2026-07-08, Crhistian Mendoza (creator of FlowDoc) opened a branch in our repo (`FlowDocsv2Adoption`) that migrates FlowForge's project templates from FlowDoc **v1.1** to **FlowDoc v2.0**. The branch contains a single commit (`d6a2288`) that updates 7 files (330 insertions, 130 deletions).

The motivation is upstream-driven: Crhistian released FlowDoc v2.0 with a richer HU template (GWT scenarios, Owner & Timeline, Technical Debt section, 🧪 Ref test links) and deprecated `flowdoc-ciclo.md` (which FlowForge never adopted). Continuing to ship v1.1 templates means our adopters miss v2.0's improvements and pin to an outdated upstream.

We reviewed the branch and found it **substantively correct but incomplete**:

| Aspect | Branch state | Issue |
|--------|--------------|-------|
| `.flowforge.json` (root) | Updated to split keys | ✅ Correct |
| `templates/project/.flowforge.json.template` | **Not updated** (still `flowdoc@1.1`) | ❌ This is what `flow-init` actually emits |
| `src/FlowForge.Installer/Commands/InitCommand.cs` | **Not updated** (still `flowdoc@1.1` literal) | ❌ C# installer still emits v1.1 |
| `flow-init.sh` + `flow-init.ps1` | **Not updated** (user-facing strings say `flowdoc@1.1`) | ❌ Confusing for adopters |
| `QUICKSTART.es.md` | **Not updated** (4 v1.1 references remain) | ❌ Spanish docs drift from EN |
| `skills/forge-discovery/SKILL.md` | Not updated (example still `"flowdoc@1.1"`) | ⚠️ Cosmetic |
| `ide/cursor/agents/forge-discovery.md` | Not updated (same example) | ⚠️ Cosmetic |
| Example HU `status: done` + `flowforge_slug: "project-onboarding"` | As shipped | ❌ Adopters copy-paste and inherit "done" state |

If we merge as-is, the next time someone runs `flowforge init`, they get a project with `docs_framework: "flowdoc@1.1"` while the docs and ecosystem guide say v2.0. The C# installer, shell scripts, and Spanish QUICKSTART are all out of sync. This is the kind of internal drift that makes methodology projects feel "vapor" to new adopters.

---

## Decision drivers

1. **Internal consistency is a release blocker** — we plan to publish FlowForge publicly this month (per the user's instruction). An adopter running `flowforge init` should see what the docs describe.
2. **Only-internal users today** — no public adopters yet, so breaking changes (e.g., split keys in `.flowforge.json`) have no migration cost.
3. **The branch is mostly correct** — the template content is good. The gap is mechanical: missing parallel updates to the C# installer, shell scripts, and Spanish docs.
4. **Adopters copy example files** — the example HU must be a *template*, not a *completed story*. `status: draft` and `flowforge_slug: ""` are correct defaults.

---

## Options considered

### Option A — Full absorb + finish the gaps (CHOSEN)

Take all of Crhistian's 7 file changes as the base, then apply the 5 missing parallel updates (template, C# installer, shell scripts, Spanish QUICKSTART, agents) plus 2 UX fixes (example HU resets).

**Pros:**
- Single coherent migration in one PR
- All consumer surfaces (template + C# + shell + docs) emit and document v2.0
- Spanish-speaking adopters see the same info as English ones
- The example HU becomes a safe template to copy

**Cons:**
- Bigger diff (~10 files instead of 7)
- Touches C# code (compiled by CI but not locally — relies on `test-installer.yml` to validate)

### Option B — Partial absorb

Take only the template + docs changes from the branch. Defer the split-keys change to a follow-up ticket.

**Pros:** Smaller, lower-risk diff.

**Cons:** Adopters running `flowforge init` after this PR would still get v1.1 config. The migration is incomplete; we'd revisit it next month.

### Option C — Reject and ask Crhistian to complete

Send the branch back with the gap list.

**Pros:** No work for us.

**Cons:** Adds a round-trip with Crhistian. We already have all the info to fix it. Slower path to a coherent migration.

**Decision: Option A.** We're the only user, the gaps are mechanical, and shipping a partial migration creates the same drift we're trying to fix.

---

## Decision

Adopt **Option A**. Specifically:

1. Take all 7 changes from `origin/FlowDocsv2Adoption` as the base of the new branch `absorb/flowdocs-v2-adoption`.
2. Update the **5 parallel surfaces** that Cristian's branch missed:
   - `templates/project/.flowforge.json.template` — split keys (`docs_framework` + `docs_framework_version` + `upstream`)
   - `src/FlowForge.Installer/Commands/InitCommand.cs::BuildFlowDocEnabledJson` — same split keys
   - `flow-init.sh` — `flowdoc@1.1` → `flowdoc@2.0`
   - `flow-init.ps1` — `flowdoc@1.1` → `flowdoc@2.0`
   - `QUICKSTART.es.md` — translate the v2.0 changes (split keys + adoption levels L1/L2/L3 table)
3. Update the **2 cosmetic surfaces**:
   - `skills/forge-discovery/SKILL.md` — change example `"flowdoc@1.1"` → `"flowdoc"` + `docs_framework_version: "2.0"`
   - `ide/cursor/agents/forge-discovery.md` — same
4. Apply **2 UX fixes** to the example HU:
   - `status: done` → `status: draft`
   - `flowforge_slug: "project-onboarding"` → `flowforge_slug: ""`
   - Replace committed-story content with `[placeholder]` so the example is a safe template
5. **Do not** touch the archived `docs/archive/21-flowdoc-integration-proposal.archived.md` — it's a historical artifact of the v1.1 era and should stay as-is.
6. **Do not** delete the v1.1 supersession note in `ADR-004` line 159 — it documents the migration history.

---

## Consequences

### Positive

- One PR produces a fully-consistent v2.0 migration across all surfaces
- Public launch (planned this month) ships v2.0 templates with no half-state
- The example HU is now a safe template to copy without inheriting "done" status
- Spanish and English docs describe the same config format

### Negative / accepted

- The diff is larger than Cristian's original (10 files vs 7) but all changes are mechanical
- Existing adopters (currently zero) would need to migrate their `.flowforge.json` from `"flowdoc@1.1"` → split keys. Documented in ADR-004 §"Version pin strategy".
- C# code change requires CI compilation to validate (no `dotnet` SDK locally). The existing `test-installer.yml` workflow will catch any compile error.

### Neutral

- ADR-004 gains a status history entry (2026-07-08)
- CHANGELOG [Unreleased] gains an entry for this migration

---

## Validation

- **Static grep**: `rg "flowdoc@1\.1" .` should return only intentional historical references (ADR-004 line 159 + archived proposal). Verified locally: 4 hits, all historical.
- **JSON validity**: both `.flowforge.json` (project) and `.flowforge.json.template` (after placeholder substitution) parse as valid JSON with `docs_framework: "flowdoc"` + `docs_framework_version: "2.0"`. Verified via Python.
- **JSON validity**: the C# `BuildFlowDocEnabledJson` raw string literal, after C# template substitution, also parses as valid JSON. Verified via Python regex extraction.
- **Shell syntax**: `bash -n flow-init.sh` exits 0. Verified locally.
- **CI**: the existing `.github/workflows/test-installer.yml` (Linux + Windows, .NET 10) will compile the C# installer. We don't have a CI step that runs `flowforge init` end-to-end (only `install`); that's a known gap, not introduced by this PR.

If any of these fail on the PR, the CKP-3 rework loop kicks in.

---

## Related future work (out of scope for this PR)

The conversation that triggered this PR surfaced 4 engram-dotnet enhancements that mirror FlowDoc v2.0's structured philosophy:

| Enhancement | Why |
|---|---|
| `hu_id` field on observations | Closes HU ↔ observation traceability loop |
| `tech_debt` observation type | Mirrors Technical Debt section in v2.0 HUs |
| `test_ref` / `code_ref` fields on observations | Mirrors 🧪 Ref concept |
| Observation lifecycle (`active` / `superseded` / `archived`) | Mirrors HU status lifecycle |

Saved to engram memory: `topic_key: engram-dotnet/flowdoc-v2-inspired-enhancements`. Recommended implementation order (after this PR merges): `hu_id` first (highest value, nullable field, no migration) → `tech_debt` type → `test_ref`/`code_ref` → lifecycle status (last; needs migration).

These are engram-dotnet issues, not FlowForge issues. They will live in the engram-dotnet repo as a future EN (Engineering Note).

---

## References

- Branch: https://github.com/efreet111/FlowForge/tree/FlowDocsv2Adoption
- Commit: `d6a2288 feat: migrate to FlowDoc v2.0, update templates and documentation` (Crhistian Mendoza, 2026-07-08)
- Related ADRs: [ADR-004](ADR-004-flowdoc-integration.md) (the v1.1 acceptance; this ADR-007 is the v2.0 migration that builds on it)
- Docs: [docs/20-flowdoc-ecosystem.md](../20-flowdoc-ecosystem.md) (adopter guide)
- Memory observation: `engram-dotnet/flowdoc-v2-inspired-enhancements`