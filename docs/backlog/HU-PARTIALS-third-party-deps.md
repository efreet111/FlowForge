# HU Partial Results — Third-Party Dependencies

> **Purpose**: Track PM tests that returned PARTIAL results due to third-party dependency limitations.
> **Scope**: HU features with partial implementations where the root cause is an external dependency (engram-dotnet, context7, etc.), not FlowForge implementation.
> **Maintenance**: Update when dependencies are upgraded or issues are resolved.

---

## HU-036: Contradiction Detection (Rework v2)

**Date**: 2026-10-14  
**Dependency**: `engram-dotnet 1.4`  
**Feature**: Resolution workflow — keep both

### Partial Item

| Field | Value |
|-------|-------|
| **PM Test** | PM-3: Resolution — keep both |
| **Expected** | `mem_update` accepts `resolution: "kept_both"` field |
| **Actual** | `mem_update` only accepts `status`, `topic_key`, `title`, `content`, `superseded_by` |
| **Field Missing** | `resolution`, `justification` |
| **Workaround** | Save memory signal documenting the resolution choice |
| **Impact** | Low — audit trail preserved via memory signal |
| **Status** | ⚠️ PARTIAL — workaround functional |

### Evidence

```
# Expected (per spec)
mem_update(
  id: "<relation-id>",
  resolution: "kept_both",
  justification: "<user-provided justification>"
)

# Actual (engram-dotnet 1.4)
mem_update(
  id: "<relation-id>",
  status: "active"  # only this field works for relations
)
```

### Resolution Path

1. **Option A**: Request `resolution` and `justification` fields in `mem_update` for engram-dotnet
2. **Option B**: Use memory signal as permanent workaround (current approach)
3. **Option C**: Deprecate `mem_update` for relations; use dedicated `mem_resolve_conflict` tool

---

## Template for New Entries

```markdown
## HU-XXX: Feature Name

**Date**: YYYY-MM-DD  
**Dependency**: `<dependency-name> <version>`  
**Feature**: <brief description>

### Partial Item

| Field | Value |
|-------|-------|
| **PM Test** | PM-N: <test name> |
| **Expected** | <what spec says> |
| **Actual** | <what actually happens> |
| **Field Missing** | <missing fields> |
| **Workaround** | <current workaround> |
| **Impact** | Low/Medium/High |
| **Status** | ⚠️ PARTIAL |

### Evidence

```
# Expected
<code>

# Actual
<code>
```

### Resolution Path

1. **Option A**: ...
2. **Option B**: ...
3. **Option C**: ...
```

---

## Summary

| HU | PM | Dependency | Status | Impact |
|----|----|------------|--------|--------|
| HU-036 | PM-3 | engram-dotnet 1.4 | ⚠️ PARTIAL | Low |

---

## Resolution Tracking

| HU | Issue | Resolution | Date Resolved |
|----|-------|------------|---------------|
| — | — | — | — |
```

