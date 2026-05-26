---
name: forge-plan-rollback
description: >
  Specialized Plan Agent skill for deployment and rollback strategies — 
  blue-green, canary, feature flags. Trigger: plan includes API changes, 
  schema modifications, contract changes, or any breaking changes.
---

# forge-plan-rollback — Deploy & Rollback Strategy

You are the **ROLLBACK PLAN AGENT**. When this skill is loaded, you MUST document the deploy strategy and rollback plan for every task that modifies contracts, APIs, or schemas.

## 🚀 Deployment Strategy Selection

| Strategy | Downtime | Rollback speed | Best for |
|----------|----------|---------------|----------|
| **Rolling Update** | Zero | Slow (per-instance) | Stateless services, no breaking changes |
| **Blue-Green** | Zero | Instant (traffic switch) | Breaking changes, API changes |
| **Canary** | Zero | Gradual (traffic %) | Risk reduction, data-sensitive changes |
| **Feature Flag** | Zero | Instant (toggle) | Any change, without redeploy |

### Decision Tree

```
Is this a breaking change? (API contract, schema, behavior)
  ├── NO  → Rolling Update
  └── YES → Is rollback speed critical?
              ├── YES → Blue-Green
              └── NO  → Canary + Feature Flag
```

## 📋 Rollback Plan Template

Every plan task that modifies production MUST include a rollback plan:

```markdown
### Rollback Plan: [Task name / Ticket]

#### Risk Level: [LOW / MEDIUM / HIGH / CRITICAL]

#### Deployment Plan
- Strategy: [Blue-Green / Canary / Rolling / Feature Flag]
- Traffic shift: [% per step, if canary]
- Feature flag name: [if applicable]

#### Rollback Steps
1. [Step 1: e.g., switch DNS back to blue environment]
2. [Step 2: e.g., run rollback migration script]
3. [Step 3: e.g., verify with smoke test]

#### Rollback Criteria (trigger immediately if any)
- Error rate increases by > 1%
- p99 latency increases by > 200ms
- Business metric [e.g., order completion] drops > 5%
- Any 5xx errors from new code path

#### Estimated Recovery Time
- Rollback decision to full recovery: [N] minutes
- Data loss on rollback: [YES/NO + details]
```

## 🔄 Feature Flag Integration

For high-risk changes, add feature flags to the plan:

```
[TASK] 3.1 Add feature flag: NEW_CHECKOUT_FLOW
         - Default: OFF
         - Target: 10% → 50% → 100%
         - Owner: [team/individual]
         - Removal: 2 weeks after 100% rollout + monitoring

[TASK] 3.2 Implement new flow behind flag (code)
[TASK] 3.3 Canary: 10% → monitor 1h → 50% → monitor 1h → 100%
[TASK] 3.4 If rollback: toggle flag OFF (instant, no deploy needed)
[TASK] 3.5 After 2 weeks stable: remove flag code
```

## 🐳 Database Rollback

For schema changes, the rollback plan MUST include:

```
#### Database Rollback
- Migration rollback script: [path to .sql file]
- Data recovery on rollback: [YES/NO]
  - If YES: how?
  - If NO: what's lost?

#### Migration rollback rules:
- Add column → DROP column (data loss if anything was written)
- Add table → DROP table (data loss)
- Add index → DROP index (no data loss)
- Rename column → Reverse rename (requires dual-write during deployment)
```

## 📝 Rollback Plan in plan.md

```markdown
## 5. Deployment & Rollback

### Feature: Checkout Redesign
- Risk level: HIGH (touches payment flow)
- Deploy strategy: Feature flag (NEW_CHECKOUT) + Canary (10% → 50% → 100%)
- Rollback: Toggle flag OFF → instant recovery (no deploy)
- Maximum impact: 10% of users if something goes wrong during canary
```

## 🚫 Anti-Patterns

| Anti-Pattern | Why it's dangerous | Fix |
|-------------|-------------------|-----|
| **"We'll fix forward"** | No rollback means every deploy is a crisis | Write rollback steps before deploying |
| **"Schema changes are backward compatible"** | Old code may create invalid data | Write dual-write code or use phases |
| **"Canary with auth"** | Auth tokens persist; user might be served by different version mid-session | Use sticky sessions or degrade gracefully |
| **"One person knows the rollback"** | Bus factor | Document every step; test rollback in staging |
| **"No smoke test on rollback"** | Partial rollback leaves system in inconsistent state | Test the rollback target after rolling back |

## ⚠️ Risk Thresholds

| Risk | Characteristics | Action |
|------|----------------|--------|
| **LOW** | Additive only, stateless, no schema change | Rolling update, no formal rollback plan needed |
| **MEDIUM** | New API endpoint, additive schema, feature-flagged | Document rollback steps in ticket |
| **HIGH** | Breaking API change, schema migration, payment/auth | Full rollback plan with tested scripts |
| **CRITICAL** | Multi-service change, data migration with backfill, public API change | Blue-green + feature flag + tested rollback + change approval |
