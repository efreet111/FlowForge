# Context Map — Methodology Audit 2026-06-22

## Epic
**Slug**: `methodology-audit-2026-06-22`  
**Type**: Internal audit (not a product feature)  
**Trigger**: Human-initiated adversarial review to surface latent contradictions before broader adoption  
**Phase**: Verify (Judgment Day) → Dev (fixes) → Close

---

## Problem Statement

FlowForge v0.5.0 added significant new content (CKP-1 BLOCKER gate, FlowDoc integration, Pattern Search, Memory Curation Protocol) across 4 IDEs and 8+ skills. No cross-file consistency check existed. Symptoms:

- The CKP-3 inner loop (dev↔verify) was suspected to be fragile
- `verify-report.md` referenced everywhere but no one had confirmed it was actually written
- `workflow.mdc` and skills had been updated independently without synchronization review

---

## Method

Judgment Day adversarial triple-review:
- Judge A: Sonnet 4.6
- Judge B: GPT-5.5 (different model family for independence)
- Judge C: Opus 4 (different model family)

Three families chosen to minimize correlated false positives.

---

## Key Discoveries

- `skills/forge-verify/SKILL.md` was never updated to match the `.ai-work/{slug}/` convention — it still writes to repo root and uses a different schema
- `verify-report.md` is promised by 3+ files but never instructed to be written
- The verify skill defines 3 verdict states (PASS, PASS DEGRADADO, PENDING) but the orchestrator only handles 2
- CKP-1 BLOCKER logic exists in the orchestrator skill but was never propagated to `workflow.mdc`
- No CI check enforces skill↔agent parity, which is how drift accumulated undetected

---

## Reusable Patterns Found

- Judgment Day with 3 different model families gives high-confidence signal: any finding confirmed by all 3 has very low false-positive rate
- Using 2 instances of the same model inflates apparent agreement — always use distinct model families
- The root-cause pattern here ("skill updated, adapter not") is a systemic risk for all future skill changes

---

## Files Audited

20 files: 8 core skills, 6 Cursor agents, workflow.mdc, parity contract, 3 slash commands, AGENTS.md

## Artifacts

| File | Status |
|------|--------|
| `context-map.md` | ✅ This file |
| `verify-report.md` | ✅ Full findings + fix plan |
| `rework_ticket.md` | Pending — will be created after human approval of fix plan |
