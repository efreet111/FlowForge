# FlowForge — Complete Reference and Test Cases

> **Version**: 1.1 (post-OLA 1–4, parity v0.4)
> **Skills**: 31 total (7 core + 23 specialized + 1 teacher)
> **Checkpoints**: 5 (CKP-0 → CKP-4)
> **Agents**: 7 roles
> **Artifacts**: `spec.md` → `plan.md` → code/tests → `verify-report.md` → `summary.md`

🇪🇸 Spanish overview: [`README.es.md`](../README.es.md)

---

## PART 1: Flow architecture

### Checkpoint system

| CKP | Phase | Color | Type | What happens | Who decides |
|-----|-------|-------|------|--------------|-------------|
| **CKP-0** | Discovery | 🔴 HARD STOP | Binary | Vague requirement → stop and clarify | Agent (no advance without clarity) |
| **CKP-1** | Arch (`spec.md`) | 🟡 YELLOW | Flexible | spec ready → “Approve or adjust?” | Human |
| **CKP-2** | Plan (`plan.md`) | 🟡 YELLOW | Flexible | plan ready → “Green light to code?” | Human |
| **CKP-3** | Verify (inner loop) | 🔴 EMERGENCY | Mechanical (3 cycles) | 3 failed reworks → escalate | Mechanical |
| **CKP-4** | Memory (close) | 🟢 DEPLOY | Flexible | Feature complete → “Deploy?” | Human |

### Five phases

```
PHASE 0 — DISCOVERY ——— CKP-0 🔴
  Agent:   forge-discovery
  Skills:  core | security | compliance | cost
  Output:  context-map.md

PHASE 1 — INTENT ————— CKP-1 🟡
  Agent:   forge-arch
  Skills:  core | security | performance | a11y | domain
  Output:  spec.md + Capability Matrix + PM-*

PHASE 2 — PLAN ——————— CKP-2 🟡
  Agent:   forge-plan
  Skills:  core | security | patterns | migrations | rollback
  Output:  plan.md (ordered checklist)

PHASE 3 — EXECUTION ——— Inner loop (CKP-3 🔴 on failure)
  Agents:  forge-dev ↔ forge-verify
  Output:  code + tests + PASS or rework_ticket.md

PHASE 4 — CLOSE ——————— CKP-4 🟢
  Agent:   forge-memory
  Skills:  core | metrics | changelog | knowledge
  Output:  summary.md (if PM-* complete)
```

### Artifact protocol

```
[context-map] → [spec.md] → [plan.md] → [code + tests] → [deploy decision]
     ↑               ↑            ↑              ↑
  discovery        arch          plan      dev ↔ verify

On verify failure:
  rework_ticket.md → dev fixes → verify re-audits (max 3 cycles)
```

**Paths:** `.ai-work/{feature-slug}/` (kebab-case). Use `verify-report.md`, not `cert-report.md`.

### Commands (IDE conventions)

| Command | Phase |
|---------|--------|
| `/flow-start <feature>` | Discovery → Spec |
| `/flow-plan` | Plan |
| `/flow-dev` | Implementation |
| `/flow-verify` | Audit |
| `/flow-rework` | Bug → ticket → dev |
| `/flow-close` | Memory + CKP-4 |
| `/flow-status` | Read `.ai-work/` only |

---

## PART 2: Agents and skills catalog

### forge-orchestrator (traffic light)

- **Skills**: 1 core
- **Phase**: All (coordinates only)
- **Does not**: write spec, plan, product code, or verify reports inline

### forge-discovery (phase 0)

| Skill | Trigger | Role |
|-------|---------|------|
| core | Always | Past memories, keywords, epic mapping |
| security | Auth/data/APIs | CVEs, dependency risk |
| compliance | Personal data | GDPR/SOC2/HIPAA/PCI-DSS |
| cost | New infra impact | Cost estimate |

**Output:** `context-map.md`

### forge-arch (phase 1)

| Skill | Trigger | Role |
|-------|---------|------|
| core | Always | `spec.md`, RF/RNF, Given-When-Then, PM-* |
| security | Auth/data/APIs | STRIDE, security RNFs |
| performance | Critical paths | SLAs/SLOs |
| a11y | UI | WCAG 2.1 AA |
| domain | Multiple contexts | DDD boundaries |

**Output:** `spec.md` + Capability Matrix

### forge-plan (phase 2)

| Skill | Trigger | Role |
|-------|---------|------|
| core | Always | Topological task breakdown |
| security | User input | OWASP ASVS, secure-by-design |
| patterns | Structure | GoF / enterprise patterns |
| migrations | DB schema | Zero-downtime strategies |
| rollback | Contract changes | Rollback plan |

**Output:** `plan.md`

### forge-dev (phase 3a)

| Skill | Trigger | Role |
|-------|---------|------|
| core | Always | Code + Ralph Wiggum loop; marks plan checklist `[x]` |
| security | Input/DB/auth | OWASP prevention |
| solid | Production code | SOLID self-audit |
| testing | Complex logic | Property-based / fuzzing |
| performance | DB/APIs | N+1, caching |
| refactor | Smells in loop | Fowler transforms |

**Output:** code + tests (`[RF-XXX]` traceability)

### forge-verify (phase 3b)

| Skill | Trigger | Role |
|-------|---------|------|
| core | Always | Spec compliance, tests, PASS/rework |
| security | Always | SAST-style review, OWASP |
| complexity | Dense logic | MCC, nesting, cognitive load |
| performance | Perf RNFs | N+1, memory, Big-O |
| a11y | UI | WCAG audit |

**Output:** `verify-report.md` or `rework_ticket.md` (does not grade PM-*)

### forge-memory (phase 4)

| Skill | Trigger | Role |
|-------|---------|------|
| core | Always | Synthesis, engrams, ADRs |
| metrics | Feature close | Coverage, cycle time |
| changelog | Pre-release | Release notes |
| knowledge | Multi-repo | Cross-project links |

**Output:** `summary.md` (blocked if PM-* pending)

### Skills summary by role

| Role | Core | OLA 1 | OLA 2 | OLA 3 | OLA 4 | Total |
|------|------|-------|-------|-------|-------|-------|
| Orchestrator | 1 | — | — | — | — | **1** |
| Discovery | 1 | — | — | 2 | 1 | **4** |
| Arch | 1 | 1 | — | 3 | — | **5** |
| Plan | 1 | 1 | 1 | 2 | — | **5** |
| Dev | 1 | 2 | 2 | 1 | — | **6** |
| Verify | 1 | 1 | 2 | — | 1 | **5** |
| Memory | 1 | — | — | — | 3 | **4** |
| **TOTAL** | **7** | **5** | **5** | **8** | **5** | **31** |

---

## PART 3: Hands-on test cases

### Suggested test project

**Task Manager API** — REST CRUD for tasks.  
**Minimal stack for first run:** Node.js + TypeScript + SQLite (see [`docs/18-replicable-demo-definition.md`](18-replicable-demo-definition.md)).  
**Team:** one person playing all human gates.

### Case 1: Simple feature — “Task CRUD”

| Phase | Expected | Skills |
|-------|----------|--------|
| Discovery | Prior art / context map | discovery/core |
| CKP-0 | Clear requirement → proceed | — |
| Arch | RF-001… + GWT + PM-* | arch/core |
| CKP-1 | Human approves spec | — |
| Plan | Ordered checklist | plan/core |
| CKP-2 | Human green-lights | — |
| Dev | Code + tests; marks plan | dev/core, dev/solid |
| Verify | PASS | verify/core |
| Memory | summary if PM-* done | memory/core |

**Success:** fewer than 3 rework cycles, plan checklist marked, tests green.

### Case 2: Security — “JWT authentication”

Extra skills: discovery/security, arch/security (STRIDE), plan/security (OWASP ASVS), dev/security, verify/security.

**Success:** RNF-SEC-* in spec, `[SEC]` tasks in plan, verify PASS with security notes.

### Case 3: Performance — “Real-time dashboard”

Extra: discovery/cost, arch/performance (SLOs), plan/patterns, dev/performance, verify/performance.

**Success:** Measurable SLOs, no N+1, caching documented.

### Case 4: UI — “Admin panel”

Extra: arch/a11y, verify/a11y.

**Success:** RNF-A11Y-* in spec; a11y issues found and fixed before PASS.

### Case 5: Migration — “Add user field”

Extra: plan/migrations, plan/rollback, dev/testing (fuzzing).

**Success:** Phased migration + rollback script + edge-case tests.

### Case 6: Refactor — “Repository pattern”

Extra: arch/domain, plan/patterns, dev/refactor, verify/complexity.

**Success:** Behavior unchanged, tests still pass, complexity reduced.

### Case 7: Multi-repo — “Notification service”

Extra: discovery/compliance, memory/knowledge, memory/changelog.

**Success:** Compliance noted, ADRs cross-linked, changelog updated.

### Flow validation checklist

```
Before start:
  [ ] Clear feature name and measurable goal

CKP-0:
  [ ] context-map.md (or explicit “no prior context”)
  [ ] security/compliance/cost skills if applicable

CKP-1 (spec.md):
  [ ] RF-* and RNF-* IDs
  [ ] Given-When-Then per RF
  [ ] Capability Matrix (ai_reasoning vs deterministic)
  [ ] PM-* manual tests section

CKP-2 (plan.md):
  [ ] Topological task order
  [ ] Contracts / file list for dev
  [ ] [SEC] / [PATTERN] tags if applicable
  [ ] Migration / rollback sections if applicable

Phase 3:
  [ ] forge-dev marked completed plan items [x]
  [ ] Tests named with RF-XXX traceability
  [ ] Tests green (Ralph Wiggum)
  [ ] rework_ticket resolved if any

CKP-3:
  [ ] verify-report.md PASS
  [ ] cycle_count < 3 on rework_ticket

CKP-4:
  [ ] All PM-* [x] in spec.md
  [ ] summary.md (not preview) if closing
```

---

## PART 4: Next steps

### Planned tooling

| Tool | Purpose |
|------|---------|
| Rules generator | Compile skills into IDE rule bundles |
| `forge init` CLI | Interactive `.flowforge.json` |
| Metrics dashboard | Optional project health UI |
| Engram config file | File-based engram-dotnet config |

### Incubator ideas

See [`13-edge-cases-and-risks.md`](13-edge-cases-and-risks.md) — context poisoning guardrail, conflict resolution, drift health check, lineage at CKP-3, etc.

---

> **Last updated**: 2026-05-27  
> **Checkpoints**: 5 | **Phases**: 5 | **Agents**: 7 | **Skills**: 31
