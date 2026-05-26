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

### Cycle Time (Automático desde Timestamps)

El orquestador guarda automáticamente un timestamp en cada checkpoint. Vos los leés:

```bash
# Buscar timestamps de esta sesión
mem_search(topic_key="metrics/timestamp/*", limit=10)

# Los timestamps tienen este formato:
# timestamp/ckp0-pass — "Discovery completado a las HH:MM"
# timestamp/ckp1-pass — "Spec aprobado a las HH:MM"  
# timestamp/ckp2-pass — "Plan aprobado a las HH:MM"
# timestamp/ckp3-pass — "Verify PASS a las HH:MM"
# timestamp/ckp4-pass — "Deploy decidido a las HH:MM"
```

**Cálculo automático de cycle time**:
```
Cycle time = timestamp[ckp4] - timestamp[ckp0] = total
Discovery  = timestamp[ckp1] - timestamp[ckp0]
Spec+Plan  = timestamp[ckp2] - timestamp[ckp1]
Ejecución  = timestamp[ckp3] - timestamp[ckp2]
Cierre     = timestamp[ckp4] - timestamp[ckp3]
```

**Si faltan timestamps** (orquestador no los guardó):
1. Intentá estimar desde el session summary actual (si tiene fechas)
2. Si no hay datos → guardá "cycle_time: unknown — timestamps no disponibles"
3. Reportá el gap: "⏱️ Para medir cycle time automáticamente, el orquestador debe guardar timestamps en cada CKP"
```

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
