---
capability_matrix:
  ai_reasoning:
    - "Summary content: which FRs to highlight, which risks to emphasize"
    - "Summary phrasing: concise language for Objective, Scope, Key Decisions"
  deterministic:
    - "Summary is always section 0, before Objective (section 1)"
    - "Summary length: 15-20 lines maximum"
    - "Summary format: Objective, Scope, FRs table, Key Decisions, Risks"
    - "Summary is mandatory for every spec.md, regardless of spec length"
---
# Spec: Executive Summary in spec.md

> **Feature slug:** `executive-summary-in-spec`
> **Sources:** [`NS-09`](../../docs/backlog/NS-09-executive-summary-in-spec.md) · [`ADR-014`](../../docs/decisions/ADR-014-executive-summary-in-spec.md)

## 0. Executive Summary

**Objective**: forge-arch currently generates spec.md files exceeding 300 lines without a high-level overview, forcing users to read the entire document before understanding the feature. This change adds a mandatory executive summary as section 0 of every spec.md.

**Scope**:
- In: Executive summary format, mandatory generation, placement as section 0
- Out: Conversational mode, state management, orchestrator changes, IDE parity changes

**Functional Requirements**:
| ID | Name | Description |
|----|------|-------------|
| FR-001 | Mandatory summary | forge-arch always generates executive summary as section 0 |
| FR-002 | Summary format | 15-20 lines: Objective, Scope, FRs table, Key Decisions, Risks |
| FR-003 | Summary placement | Section 0, before Objective (section 1) |
| FR-004 | Summary accuracy | Summary reflects full spec content accurately |

**Key Decisions**: Summary is always generated (not conditional on spec length). No orchestrator changes needed. No state management required.

**Risks**: forge-arch may generate inaccurate summaries (mitigated by testing). Summary adds 15-20 lines to every spec (negligible impact).

---

## 1. Objective and scope

**What**: Users report that spec.md files exceed 300 lines, requiring 15-20 minutes to understand before making modifications or approving at CKP-1.

**Solution**: forge-arch always generates an executive summary (15-20 lines) as section 0 of every spec.md. Users read the summary first (2-3 minutes), then decide whether to read the full spec.

**Scope (in)**:
- Executive summary format: Objective, Scope, FRs table, Key Decisions, Risks
- Summary is always section 0, before Objective (section 1)
- Summary is mandatory for every spec.md, regardless of spec length
- Summary length: 15-20 lines maximum

**Scope (out)**:
- Conversational mode (rejected as over-engineering per ADR-014)
- State management (_spec_wip.md)
- Orchestrator changes
- Configurable summary length
- IDE parity changes (summary is part of spec.md, which all IDEs already handle)

## 2. Functional requirements (FR)

### FR-001: Mandatory executive summary
**forge-arch always generates an executive summary as section 0 of spec.md, regardless of spec length.**

- **Scenario A — Large spec gets summary**
  - Given a feature with 6+ FRs and expected spec length >300 lines
  - When forge-arch generates spec.md
  - Then spec.md starts with "## 0. Executive Summary" containing 15-20 lines
  - And the summary covers: Objective, Scope, FRs table, Key Decisions, Risks

- **Scenario B — Small spec also gets summary**
  - Given a feature with 2 FRs and expected spec length <100 lines
  - When forge-arch generates spec.md
  - Then spec.md still starts with "## 0. Executive Summary"
  - And the summary is shorter but still present

### FR-002: Summary format
**The executive summary follows a strict 15-20 line format with 5 subsections.**

- **Scenario A — Summary has all 5 subsections**
  - Given forge-arch is generating a spec.md
  - When it writes the executive summary
  - Then the summary contains: Objective (1 line), Scope (3 lines), FRs table (6 lines), Key Decisions (3 lines), Risks (2 lines)

- **Scenario B — Summary is concise**
  - Given a spec.md with 400 lines of detailed content
  - When forge-arch writes the executive summary
  - Then the summary is no more than 20 lines
  - And each subsection uses concise language (no GWT scenarios in summary)

### FR-003: Summary placement
**The executive summary is always section 0, placed before the Objective section.**

- **Scenario A — Correct placement**
  - Given forge-arch is generating spec.md
  - When it writes the document structure
  - Then section 0 is "## 0. Executive Summary"
  - And section 1 is "## 1. Objective and scope" (existing)
  - And sections 2-5 follow as before

- **Scenario B — Capability matrix unaffected**
  - Given spec.md has a YAML frontmatter with capability_matrix
  - When forge-arch adds the executive summary
  - Then the capability_matrix remains in the frontmatter
  - And the executive summary is the first section after frontmatter

### FR-004: Summary accuracy
**The executive summary accurately reflects the full spec content.**

- **Scenario A — Summary matches spec**
  - Given forge-arch has written all sections of spec.md
  - When it generates the executive summary
  - Then the summary's FRs table matches the FRs in section 2
  - And the summary's Key Decisions match the architectural decisions in the spec
  - And the summary's Risks match the identified risks in the spec

- **Scenario B — Summary is generated last**
  - Given forge-arch is generating spec.md
  - When it writes the document
  - Then it writes sections 1-5 first
  - And then generates the executive summary based on the completed content
  - And the summary accurately reflects what was written

## 3. Non-functional requirements (NFR)

- **NFR-001 — Summary length**: Executive summary must be 15-20 lines maximum. If the summary exceeds 20 lines, forge-arch must condense it.

- **NFR-002 — Summary conciseness**: Summary uses concise language. No GWT scenarios, no detailed descriptions, no implementation details. Only high-level overview.

- **NFR-003 — Backward compatibility**: Existing spec.md files without executive summaries continue to work. No validation changes required for forge-verify or forge-plan.

- **NFR-004 — No orchestrator changes**: This feature does not require changes to forge-orchestrator, workflow-orchestrator-parity.md, or any workflow files.

- **NFR-005 — IDE parity**: The executive summary is part of spec.md format, which all IDEs already handle. No IDE-specific changes required beyond updating agent files to reflect the new template.

## 4. Developer manual tests (PM-*)

| ID | Case / flow | Steps (summary) | Expected result | [x] |
|----|-------------|-----------------|-----------------|-----|
| PM-1 | Large spec has summary | 1. Invoke forge-arch for feature with 6+ FRs<br>2. Read generated spec.md | spec.md starts with "## 0. Executive Summary" containing 15-20 lines with Objective, Scope, FRs table, Key Decisions, Risks | [ ] |
| PM-2 | Small spec has summary | 1. Invoke forge-arch for feature with 2 FRs<br>2. Read generated spec.md | spec.md still starts with "## 0. Executive Summary" (shorter but present) | [ ] |
| PM-3 | Summary accuracy | 1. Generate spec.md for any feature<br>2. Compare summary FRs table with section 2 FRs<br>3. Compare summary Key Decisions with spec decisions | Summary accurately reflects full spec content | [ ] |
| PM-4 | CKP-1 review with summary | 1. Generate spec.md<br>2. Read only the executive summary (15-20 lines)<br>3. Decide whether to approve or dig deeper | User can make informed decision without reading full 300+ line spec | [ ] |

## 5. Open questions for human (OQ-*)

| ID | Tag | Question | Default / assumption |
|----|-----|---------|---------------------|
| OQ-1 | [OPTIONAL] | Should the executive summary include a "Next Steps" subsection (e.g., what needs to happen after spec approval)? | **Assumed: No** for v1. Summary focuses on what the feature IS, not what comes next. |
| OQ-2 | [OPTIONAL] | Should forge-arch generate the summary BEFORE or AFTER writing sections 1-5? | **Assumed: AFTER** — summary must accurately reflect the full spec, so it's generated last. |
| OQ-3 | [FOLLOW-UP] | Should the orchestrator present only the executive summary at CKP-1 (instead of the full spec)? | Out of v1 scope. Orchestrator changes are explicitly out of scope. |

---

## Memory Signal
- type: decision
- significance: low
- summary: "Mandatory executive summary as section 0 of spec.md: 15-20 lines covering Objective, Scope, FRs table, Key Decisions, Risks. Always generated, regardless of spec length. No orchestrator changes, no state management. Rejected conversational mode as over-engineering."
