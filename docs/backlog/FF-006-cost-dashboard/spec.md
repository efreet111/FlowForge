# FF-006: Cost Dashboard per Phase/Epic

**Status**: 🟢 Ready (but ROI doubtful)  
**Priority**: P4  
**Effort**: L (1-2 days)  
**Origin**: FF-IDEA-006  

---

## Problem Statement

Teams using FlowForge for AI-driven development have no easy way to see: how much are we spending on AI calls? Which phase/epic is most expensive? Are we getting ROI? Today, cost data lives in LLM provider dashboards, not in FlowForge.

## Proposed Solution

A cost-tracking layer:

- Tag every LLM call with phase + epic metadata
- Persist usage data (input tokens, output tokens, cost in USD)
- Surface in a dashboard (CLI for now, web later)
- Alert on per-phase cost thresholds

## Success Criteria

- A team lead can see "Phase Plan cost $X for this epic, Phase Execution cost $Y" in one command
- Costs are predictable enough to budget
- Cost alerts fire before overruns

## Dependencies

### External (potential blockers)

| Dependency | Status | Impact |
|------------|--------|--------|
| LLM provider usage APIs | Varies by provider | Medium — OpenAI, Anthropic have usage APIs, but they require API keys with billing permissions |
| FlowForge's existing artifact metadata | Exists | Low — can tag artifacts with phase/epic |
| Integration with billing systems | Not exists | Low — optional, can start with simple tracking |

### FlowForge (no blockers)

- Orchestrator agent — integration point for tagging LLM calls
- Existing artifact metadata (spec.md, plan.md, etc.)

## Effort Breakdown

| Component | Effort | Notes |
|-----------|--------|-------|
| LLM call instrumentation (tagging phase/epic) | M (4-8 hours) | Hook into Orchestrator, extract metadata |
| Usage data persistence | M (4-8 hours) | Store token counts, costs in local DB or file |
| Cost calculation logic | S (2-4 hours) | Map token counts to USD using provider pricing |
| CLI dashboard (`flowforge costs`) | M (4-8 hours) | Aggregate by phase/epic, format output |
| Alert system (thresholds) | S (2-4 hours) | Check costs against thresholds, warn user |
| Testing + documentation | S (2-4 hours) | Unit tests, user guide |
| **Total** | **L (1-2 days)** | Implementable today |

## Open Questions

1. **Where do cost data come from?**
   - Option A: LLM provider APIs (OpenAI, Anthropic) — requires API keys with billing permissions
   - Option B: Local estimation (token count × provider pricing) — less accurate, but no external dependencies
   - Option C: IDE-level tracking (Cursor, OpenCode expose usage) — depends on IDE support

2. **Who is the user?**
   - Team lead (manages 2-20 developers) — wants to see team-wide costs
   - Individual developer — wants to see their own costs
   - Engineering manager — wants to see ROI (cost vs. velocity)

3. **What's the granularity?**
   - Per session (each FlowForge session)
   - Per phase (Discovery, Arch, Plan, Dev, Verify, Memory)
   - Per epic (feature-slug)
   - Per developer (user handle)

4. **How to handle multiple LLM providers?**
   - Some teams use OpenAI for some tasks, Anthropic for others
   - Pricing varies by model (GPT-4 vs GPT-3.5, Claude 3 vs Claude 2)

5. **Should we integrate with existing billing dashboards?**
   - OpenAI: platform.openai.com/usage
   - Anthropic: console.anthropic.com/usage
   - Or build our own?

## Implementation Plan

### Phase 1: LLM Call Instrumentation (M)

```python
# Pseudocode
class Orchestrator:
    def call_llm(self, prompt, phase, epic):
        # Before call
        start_time = time.now()
        
        # Make LLM call
        response = llm.generate(prompt)
        
        # After call
        token_count = response.usage.total_tokens
        cost = calculate_cost(token_count, model=response.model)
        
        # Persist usage
        usage_db.insert({
            "timestamp": start_time,
            "phase": phase,
            "epic": epic,
            "model": response.model,
            "tokens": token_count,
            "cost_usd": cost,
            "user": current_user
        })
```

### Phase 2: Cost Calculation (S)

```python
# Pseudocode
PRICING = {
    "gpt-4": {"input": 0.03, "output": 0.06},  # per 1K tokens
    "gpt-3.5-turbo": {"input": 0.0015, "output": 0.002},
    "claude-3-opus": {"input": 0.015, "output": 0.075},
    "claude-3-sonnet": {"input": 0.003, "output": 0.015}
}

def calculate_cost(tokens, model):
    pricing = PRICING[model]
    input_cost = tokens.input * pricing["input"] / 1000
    output_cost = tokens.output * pricing["output"] / 1000
    return input_cost + output_cost
```

### Phase 3: CLI Dashboard (M)

```
$ flowforge costs --epic flowforge-update-mechanism

╭──────────────────────────────────────────────────────────╮
│  Cost Breakdown: flowforge-update-mechanism              │
╰──────────────────────────────────────────────────────────╯

Phase          | Tokens   | Cost (USD)
---------------|----------|-----------
Discovery      | 45,230   | $1.36
Arch           | 78,450   | $2.35
Plan           | 62,180   | $1.87
Dev            | 234,560  | $7.04
Verify         | 89,340   | $2.68
Memory         | 23,450   | $0.70
---------------|----------|-----------
Total          | 533,210  | $16.00

## Alerts
⚠️  Dev phase exceeded threshold ($5.00)
✅  Total cost within budget ($20.00)
```

### Phase 4: Alert System (S)

```python
# Pseudocode
THRESHOLDS = {
    "phase": {"Dev": 5.00},  # $5 max per Dev phase
    "epic": 20.00  # $20 max per epic
}

def check_alerts(epic):
    costs = aggregate_costs(epic)
    
    for phase, cost in costs.by_phase.items():
        if cost > THRESHOLDS["phase"].get(phase, float("inf")):
            alert(f"⚠️  {phase} phase exceeded threshold (${cost:.2f})")
    
    if costs.total > THRESHOLDS["epic"]:
        alert(f"⚠️  Total cost exceeded budget (${costs.total:.2f})")
```

## Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| LLM provider APIs are unreliable | Medium | Medium | Fallback to local estimation |
| Pricing changes frequently | High | Low | Make pricing configurable, update quarterly |
| Users don't care about costs | Medium | High | Validate with user interviews before implementing |
| Cost estimation is inaccurate | High | Medium | Show confidence interval, warn if estimation |
| Privacy concerns (tracking developer usage) | Low | High | Make tracking opt-in, allow per-developer opt-out |

## ROI Analysis

### Costs of Implementation
- Development: L (1-2 days) = ~$1,000 (assuming $500/day rate)
- Maintenance: S per quarter (pricing updates) = ~$250/year

### Benefits
- **Tangible**: Teams can budget AI costs, avoid surprises
- **Intangible**: Visibility into AI ROI, better decision-making

### Break-Even
- If this feature helps 1 team avoid a $2,000 cost overrun, it pays for itself
- If it helps 10 teams budget better, it's worth $10,000 in value

### Verdict
**ROI is doubtful** unless:
1. Teams are spending >$100/month on AI calls (small teams may not)
2. Team leads are actively managing AI budgets (most don't)
3. There's a regulatory requirement to track AI costs (unlikely)

## Recommended Path

1. **Validate demand first** — survey 10 FlowForge users: "Do you care about AI cost tracking?"
2. **If demand is low** — skip this feature, focus on FF-003 (Onboarding) or FF-001 (Code-aware Dev)
3. **If demand is high** — implement as planned
4. **Alternative (simpler)**: `flowforge stats` — show session count, artifact count, time-per-phase. No costs, but still useful metrics.

## Related Features

- **FF-007** (Drift health check) — could share instrumentation layer
- **FF-003** (Onboarding) — higher priority, lower effort

---

**Next step**: Validate demand with user interviews. If demand is low, skip. If high, implement.

**Alternative**: Implement `flowforge stats` (session count, artifact count) instead — simpler, still useful.
