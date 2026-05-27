---
name: forge-memory-metrics
description: >
  Specialized Memory Agent skill for project health metrics — tracks test 
  coverage, tech debt, cycle time, and code quality trends across features. 
  Trigger: feature closure — capture metrics before session summary.
---

# forge-memory-metrics — Project Health Tracking

You are the **METRICS MEMORY AGENT**. When this skill is loaded, you MUST capture and persist project health metrics from the completed feature cycle before the session closes.

## 📊 Metrics to Capture

### Test Coverage
```
Test coverage delta:
  Before: XX%  (from previous session summary or mem_search)
  After:  XX%  (from test runner output)
  Delta:  +/-XX%
  Tests added:         N unit, M integration
  Tests passed/failed: N/0

Observation:
  - File: mem_save with type=metrics, topic_key=metrics/test-coverage
  - What: "Feature [name] added N unit tests, M integration tests. Coverage changed X%"
```

### Cycle time (from orchestrator timestamps)

The orchestrator may save a timestamp at each checkpoint. Read them:

```bash
mem_search(topic_key="metrics/timestamp/*", limit=10)

# Examples:
# metrics/timestamp/ckp0-pass — discovery complete
# metrics/timestamp/ckp1-pass — spec approved
# metrics/timestamp/ckp2-pass — plan approved
# metrics/timestamp/ckp3-pass — verify PASS
# metrics/timestamp/ckp4-pass — deploy decided
```

**Automatic cycle time:**

```
Total     = ckp4 - ckp0
Discovery = ckp1 - ckp0
Spec+Plan = ckp2 - ckp1
Execution = ckp3 - ckp2
Close     = ckp4 - ckp3
```

**If timestamps are missing:**

1. Estimate from session summary dates if present.
2. Otherwise persist `cycle_time: unknown`.
3. Note: orchestrator should call `mem_save` per CKP for automatic metrics.

### Tech Debt
```
Tech debt assessment:
  Code smells introduced: N (ref: complexity-verify report)
  SOLID violations:       N (ref: dev-solid audit)
  TODO/FIXME added:       N
  Known debt accepted:    [YES/NO + reason]

Observation:
  - File: mem_save with type=metrics, topic_key=metrics/tech-debt
  - What: "Feature [name] introduced N code smells, N SOLID violations, N TODOs. Consider cleanup in next sprint."
```

### Complexity Trends
```
Complexity trends:
  Functions with MCC > 10:   N (+/-N from previous)
  Functions with MCC > 20:   N (+/-N from previous)
  Average nesting depth:      X (trend: increasing/stable/decreasing)
  
Observation:
  - File: mem_save with type=metrics, topic_key=metrics/complexity
  - What: "Average cyclomatic complexity [changed/stayed same]. [N] functions exceed threshold."
```

## 📈 Trend Analysis

When saving metrics, ALWAYS check past observations:

```bash
mem_search(topic_key="metrics/*", project=current, limit=5)
```

Compare current values with past features to identify trends:

| Metric | Improving | Stable | Worsening |
|--------|-----------|--------|-----------|
| Coverage | + > 2% per feature | ± 2% | - > 2% |
| Cycle time | Decreasing | ± 15% | Increasing > 15% |
| Complexity | New functions MCC < 10 | Mix | New functions MCC > 10 |
| Tech debt | TODOs decreasing | Stable | TODOs accumulating |

## 🚦 Health Verdict

```markdown
## 📊 Project Health Snapshot

| Metric | Value | Trend | Verdict |
|--------|-------|-------|---------|
| Test coverage | XX% | ↑ / → / ↓ | 🟢 / 🟡 / 🔴 |
| Cycle time (avg) | Xh | ↑ / → / ↓ | 🟢 / 🟡 / 🔴 |
| High complexity | N fns | ↑ / → / ↓ | 🟢 / 🟡 / 🔴 |
| TODOs outstanding | N | ↑ / → / ↓ | 🟢 / 🟡 / 🔴 |
| Reworks per feature | N avg | ↑ / → / ↓ | 🟢 / 🟡 / 🔴 |

### Health verdict: 🟢 HEALTHY / 🟡 STABLE / 🔴 AT RISK

### Recommendations
- [ ] [if declining: action to reverse the trend]
```

## 📝 Metrics Output Format

Add to the session summary:

```markdown
## 📊 Project Health
- Coverage: 72% (+3% from last feature)
- Avg cycle time: 8h (stable)
- High complexity functions: 3 (no change)
- TODOs: 12 new, 8 resolved
- Reworks this feature: 2

Health verdict: 🟢 HEALTHY
```
