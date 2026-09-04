---
hu_id: HU-037
title: "Cost Dashboard"
status: draft
category: feature
flowforge_slug: "ff-006-cost-dashboard"
---

# HU-037 — Cost Dashboard

## User Story
As a **team lead**, I want to **see a breakdown of AI costs per phase and epic** so that I **can track spending and validate ROI before committing to larger AI-driven development investments**.

## Acceptance Criteria (business-level)
- [ ] AC-1: Team lead can view total AI spend grouped by phase (Plan, Execution, Review, etc.) in a single command
- [ ] AC-2: Team lead can see AI costs broken down per epic within a project
- [ ] AC-3: Cost alerts fire when a phase exceeds a configurable USD threshold
- [ ] AC-4: Dashboard shows cost trends over time (daily/weekly/monthly)

## Scenarios (SDD Spec)
### Happy Path
- [ ] **[View cost breakdown by phase]**
  **GIVEN** the user has configured LLM provider API keys with billing access
  **WHEN** they run `flowforge cost breakdown --phase`
  **THEN** they see a table listing each phase with total input tokens, output tokens, and USD cost
  **🧪 Ref**: ...

- [ ] **[View cost breakdown by epic]**
  **GIVEN** artifacts are tagged with epic metadata
  **WHEN** they run `flowforge cost breakdown --epic`
  **THEN** they see costs attributed to each epic in the current project
  **🧪 Ref**: ...

- [ ] **[Cost alert fires on threshold breach]**
  **GIVEN** a per-phase cost threshold is configured (e.g., Phase Plan = $50)
  **WHEN** cumulative costs for that phase exceed the threshold
  **THEN** a warning is displayed before the next LLM call completes
  **🧪 Ref**: ...

### Edge Cases
- **[No API keys configured]**
  **GIVEN** the user has not configured LLM provider billing API keys
  **WHEN** they attempt to view cost data
  **THEN** they receive a clear error message explaining the requirement with setup instructions
  **🧪 Ref**: ...

- **[Zero usage]**
  **GIVEN** a project has no LLM calls recorded yet
  **WHEN** the user views the cost dashboard
  **THEN** they see $0.00 with a message "No AI usage recorded yet"
  **🧪 Ref**: ...

- **[Multiple providers]**
  **GIVEN** the user has configured both OpenAI and Anthropic API keys
  **WHEN** they view cost breakdown
  **THEN** costs are aggregated across providers with a column showing per-provider breakdown
  **🧪 Ref**: ...

## Context / Notes
From the spec - note ROI is doubtful. This feature should not be prioritized until demand is validated. Alternative path: implement `flowforge stats` (session count, artifact count, time-per-phase) as a simpler, lower-risk first iteration that still provides value without requiring billing API access.

## Owner & Timeline
- **Owner**: @equipo-FlowForge
- **Target milestone**: v0.9.0 (ROI doubtful - validate first)
- **Dependencies**: LLM provider APIs (medium impact - requires API keys with billing permissions), FlowForge artifact metadata (exists)

## Definition of Done
- [ ] LLM calls are tagged with phase + epic metadata on every invocation
- [ ] Usage data (input tokens, output tokens, cost in USD) is persisted per call
- [ ] CLI command `flowforge cost breakdown` displays phase and epic groupings
- [ ] Configurable cost threshold alerts are implemented
- [ ] Documentation for setup and usage is provided
- [ ] Demand validation survey sent to 10 FlowForge users before development starts

## FlowForge
> This section is managed by FlowForge agents. Do not edit manually.
- HU status lifecycle: `draft` → `in-progress` → `done`
- Status is updated by forge-memory at `/flow-close`

---

## Technical Debt (if applicable)
- Cost calculation relies on provider pricing (OpenAI, Anthropic) which may change; consider caching pricing at time of call
- No data retention policy defined yet; costs could grow unbounded in local storage
