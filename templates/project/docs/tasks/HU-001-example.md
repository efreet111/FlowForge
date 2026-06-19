<!-- Adapted from FlowDoc v1.1 (github.com/crhistianmdz/FlowDocs) — MIT License -->
---
hu_id: HU-001
title: "Example User Story"
status: draft
priority: medium
flowforge_slug: ""
created: __DATE__
---

# HU-001 — [Feature name]

## User Story

**As a** [user role],  
**I want** [action or capability],  
**so that** [benefit or outcome].

---

## Acceptance criteria

- [ ] AC-1: [Criterion — specific and testable]
- [ ] AC-2: [Criterion]
- [ ] AC-3: [Criterion]

---

## Context / notes

[Optional: background, constraints, design references, mockup links]

---

## Definition of Done

- [ ] Code reviewed and merged
- [ ] Unit tests passing (coverage ≥ threshold)
- [ ] Manual tests (PM-*) executed by developer
- [ ] Documentation updated (if applicable)
- [ ] ADR created (if architecture decision was made)

---

## FlowForge

To implement this User Story:

```
/flow-start HU-001-[feature-slug]
```

The agent will read this file to populate `spec.md` (Phase 1).  
Update `flowforge_slug` in the frontmatter above when you run `/flow-start`.

**Status lifecycle:**  
`draft` → `in-progress` (set when `/flow-start` runs) → `done` (set by forge-memory at `/flow-close`)

---

*Template from FlowDoc v1.1 · Modified by FlowForge for `flowforge_slug` traceability.*
