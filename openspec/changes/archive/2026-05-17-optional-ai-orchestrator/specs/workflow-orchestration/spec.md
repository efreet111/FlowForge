# Workflow Orchestration Specification

## Purpose

The Workflow Orchestration capability defines how the optional AI Orchestrator integrates into the EngramFlow methodology. It acts strictly as an Escalation Manager to recover from blocked states, ensuring the SDD workflow remains deterministic and cost-effective by default.

## Requirements

### Requirement: Orchestrator Configuration

The system MUST support configuring the Orchestrator via the `.engram.json` configuration file, including an explicit toggle and defined escalation thresholds.

#### Scenario: Orchestrator disabled by default

- GIVEN an `.engram.json` file without an `orchestrator` block
- WHEN the workflow runner executes a cycle
- THEN the system MUST assume `orchestrator.enabled: false`
- AND the AI Orchestrator MUST NOT be invoked under any circumstances

#### Scenario: Custom escalation threshold

- GIVEN an `.engram.json` file with `orchestrator.enabled: true`
- AND `orchestrator.max_retry_cycles: 2`
- WHEN the Verify Agent fails its verification twice (writing the second `rework_ticket.md`)
- THEN the system MUST recognize the threshold has been reached

### Requirement: Deterministic Base Routing (Token Bleed Prevention)

The system MUST NOT invoke the AI Orchestrator during the standard, unblocked execution of the Inner Loop.

#### Scenario: Happy path execution

- GIVEN the AI Orchestrator is enabled
- AND a valid `plan.md` exists
- AND the Dev Agent successfully writes code
- AND the Verify Agent passes verification without generating a `rework_ticket.md`
- WHEN the workflow transitions between these states
- THEN the workflow runner MUST route control deterministically based on the artifacts
- AND the AI Orchestrator MUST NOT consume any tokens

### Requirement: Escalation Trigger on Rework Limit

The system MUST invoke the AI Orchestrator when the deterministic retry limit is exhausted.

#### Scenario: Rework loop exhaustion

- GIVEN the AI Orchestrator is enabled with `max_retry_cycles: 3`
- AND the Verify Agent generates a `rework_ticket.md` indicating "Cycle 3/3"
- WHEN the workflow runner detects the cycle count meets the maximum limit
- THEN the workflow runner MUST suspend the standard Dev Agent loop
- AND it MUST invoke the AI Orchestrator, passing the `rework_ticket.md`, `spec.md`, and `plan.md` as context

### Requirement: Orchestrator Resolution

Upon invocation, the AI Orchestrator MUST determine the path forward to resolve the deadlock.

#### Scenario: Orchestrator decides to re-plan

- GIVEN the AI Orchestrator has been invoked due to a rework loop
- WHEN it analyzes the context and determines the `plan.md` contradicts the `spec.md`
- THEN it SHOULD modify the `plan.md` to resolve the architectural conflict
- AND it MUST reset the rework cycle counter
- AND it MUST re-invoke the Dev Agent

#### Scenario: Orchestrator escalates to Human Checkpoint

- GIVEN the AI Orchestrator has been invoked due to a rework loop
- WHEN it analyzes the context and cannot resolve the conflict autonomously
- THEN it MUST halt the automated execution
- AND it MUST emit an Escalation Report detailing the impasse
- AND it MUST trigger a Human Checkpoint for manual intervention
