# FF-007: Drift Health Check (Code vs plan.md)

**Status**: 🟢 Ready (lite version in verify agent)  
**Priority**: P3  
**Effort**: M (lite) / L (full)  
**Origin**: FF-IDEA-007  

---

## Problem Statement

Over time, the actual code drifts from the `plan.md` that originally described it. New devs add features not in the plan; refactors change the architecture; decisions get lost. The plan becomes a historical artifact instead of a living document.

## Proposed Solution

### Lite Version (integrate into forge-verify)

A periodic check that:
- Compares current code structure (files, key symbols) against the plan's expected structure
- Flags tasks in the plan that haven't been marked as `[x]` after N days
- Flags files in the code that aren't mentioned in the plan
- Optionally, prompts: "this code looks like it's drifting from plan.md, should we update the plan or fix the drift?"

### Full Version (standalone command)

A `flowforge drift` command that:
- Runs the lite checks
- Additionally, uses LLM-as-Judge to compare spec.md requirements vs actual implementation
- Generates a drift report with recommendations (update plan, fix code, or both)

## Success Criteria

- A monthly check identifies drift with low false-positive rate
- Drift is resolved within the same iteration (plan updated OR code corrected)
- Plans remain living documents

## Dependencies

### FlowForge (no blockers)

- plan.md artifact format (already exists)
- spec.md artifact format (already exists)
- forge-verify agent (already exists, can extend)

### Optional (for full version)

- LLM-as-Judge (already exists in engram-dotnet: `mem_verify_artifact`)

## Effort Breakdown

### Lite Version (M)

| Component | Effort | Notes |
|-----------|--------|-------|
| Parse plan.md for tasks | S (2-4 hours) | Extract checklist items |
| Check task completion (marked `[x]`) | S (2-4 hours) | Simple regex |
| Check task age (not marked after N days) | S (2-4 hours) | Compare file timestamps |
| Integration into forge-verify | M (4-8 hours) | Add drift check to verify workflow |
| **Total Lite** | **M (1 day)** | Implementable today |

### Full Version (L)

| Component | Effort | Notes |
|-----------|--------|-------|
| Lite version | M (1 day) | Prerequisite |
| File structure comparison (code vs plan) | M (4-8 hours) | Parse plan for file mentions, compare with actual files |
| LLM-as-Judge integration (spec vs code) | M (4-8 hours) | Use `mem_verify_artifact` or similar |
| Drift report generation | M (4-8 hours) | Markdown report with recommendations |
| CLI command (`flowforge drift`) | S (2-4 hours) | Command scaffolding |
| **Total Full** | **L (2-3 days)** | Implementable today |

## Open Questions

### Lite Version

1. What's the threshold for "old" tasks? (7 days? 14 days? 30 days?)
2. Should we distinguish between "not started" and "in progress" tasks?
3. How to handle tasks that are intentionally deferred (marked `[ ]` with a note)?
4. Should we alert on every drift check, or only when drift exceeds a threshold?

### Full Version

1. How to compare code structure vs plan? (file paths? symbol names? both?)
2. Should we use LLM-as-Judge for every drift check, or only when lite checks find issues?
3. How to generate recommendations (update plan vs fix code)?
4. Should drift reports be stored (for trend analysis) or ephemeral?

## Implementation Plan

### Lite Version: Integrate into forge-verify

#### Phase 1: Parse plan.md for Tasks (S)

```python
# Pseudocode
def extract_tasks_from_plan(plan_md):
    tasks = []
    for line in plan_md.lines:
        if line.startswith("- [ ]") or line.startswith("- [x]"):
            tasks.append({
                "content": line.replace("- [ ]", "").replace("- [x]", "").strip(),
                "completed": line.startswith("- [x]"),
                "line_number": line.number
            })
    return tasks
```

#### Phase 2: Check Task Age (S)

```python
# Pseudocode
def check_task_age(tasks, plan_path, threshold_days=14):
    plan_mtime = os.path.getmtime(plan_path)
    age_days = (time.now() - plan_mtime) / 86400
    
    old_incomplete_tasks = []
    for task in tasks:
        if not task["completed"] and age_days > threshold_days:
            old_incomplete_tasks.append(task)
    
    return old_incomplete_tasks
```

#### Phase 3: Integration into forge-verify (M)

```python
# Pseudocode
class ForgeVerifyAgent:
    def execute(self, spec_path, plan_path):
        # Existing verify logic
        verify_report = self.verify_spec_vs_code(spec_path)
        
        # New drift check
        tasks = extract_tasks_from_plan(plan_path)
        old_tasks = check_task_age(tasks, plan_path)
        
        if old_tasks:
            verify_report.warnings.append(
                f"⚠️  {len(old_tasks)} tasks in plan.md are incomplete after 14 days:\n" +
                "\n".join(f"  - {t['content']}" for t in old_tasks)
            )
        
        return verify_report
```

### Full Version: Standalone Command

#### Phase 4: File Structure Comparison (M)

```python
# Pseudocode
def extract_file_mentions_from_plan(plan_md):
    # Look for file paths in plan
    file_mentions = set()
    for line in plan_md.lines:
        # Match patterns like "src/FlowForge.Installer/Update/UpdateOrchestrator.cs"
        matches = re.findall(r'[a-zA-Z0-9_\-./]+\.[a-zA-Z]{1,5}', line)
        file_mentions.update(matches)
    return file_mentions

def compare_file_structure(plan_files, actual_files):
    missing_files = plan_files - actual_files  # In plan but not in code
    extra_files = actual_files - plan_files    # In code but not in plan
    
    return {
        "missing": missing_files,
        "extra": extra_files
    }
```

#### Phase 5: LLM-as-Judge Integration (M)

```python
# Pseudocode
def verify_spec_vs_code(spec_path, code_files):
    spec_content = read_file(spec_path)
    code_content = "\n\n".join(read_file(f) for f in code_files)
    
    prompt = f"""
    Compare the following spec with the actual code implementation.
    
    ## Spec
    {spec_content}
    
    ## Code
    {code_content}
    
    ## Instructions
    Identify:
    1. Requirements in the spec that are NOT implemented in the code
    2. Code functionality that is NOT in the spec (drift)
    
    Return a JSON report with:
    - missing_requirements: list of spec requirements not implemented
    - extra_functionality: list of code features not in spec
    - drift_score: 0-100 (0 = no drift, 100 = complete drift)
    """
    
    response = llm.generate(prompt)
    return parse_json(response)
```

#### Phase 6: Drift Report Generation (M)

```markdown
# Drift Report: flowforge-update-mechanism

> Generated: 2026-08-14
> Plan age: 23 days

## Summary
- Drift score: 35/100 (moderate drift)
- 3 incomplete tasks after 14 days
- 2 files in plan but not in code
- 5 files in code but not in plan

## Incomplete Tasks (>14 days)
- [ ] Task 1: Implement backup manager
- [ ] Task 2: Add health-check logic
- [ ] Task 3: Write unit tests

## File Structure Drift

### Files in plan but not in code
- `src/FlowForge.Installer/Update/RollbackManager.cs` (planned but not implemented)

### Files in code but not in plan
- `src/FlowForge.Installer/Update/CacheRefresher.cs` (implemented but not in plan)
- `src/FlowForge.Installer/Update/EngramProcessChecker.cs` (implemented but not in plan)
- ...

## Recommendations

1. **Update plan.md** — add CacheRefresher, EngramProcessChecker to plan
2. **Implement missing files** — RollbackManager is planned but not implemented
3. **Complete old tasks** — 3 tasks are incomplete after 14 days

## Next Steps
- [ ] Review drift report with team
- [ ] Decide: update plan or fix code?
- [ ] Re-run drift check in 7 days
```

#### Phase 7: CLI Command (S)

```bash
$ flowforge drift --epic flowforge-update-mechanism

╭──────────────────────────────────────────────────────────╮
│  Drift Check: flowforge-update-mechanism                 │
╰──────────────────────────────────────────────────────────╯

Drift score: 35/100 (moderate drift)

## Warnings
⚠️  3 incomplete tasks after 14 days
⚠️  2 files in plan but not in code
⚠️  5 files in code but not in plan

## Recommendations
1. Update plan.md — add CacheRefresher, EngramProcessChecker
2. Implement missing files — RollbackManager
3. Complete old tasks — 3 tasks incomplete

Run `flowforge drift --report` for full report.
```

## Risk Assessment

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| High false-positive rate (legitimate drift flagged) | Medium | Medium | Allow user to mark drift as "intentional" |
| Users ignore drift warnings | Medium | Medium | Make drift check mandatory before `/flow-close` |
| LLM-as-Judge is expensive (full version) | Medium | Low | Run full check weekly, lite check daily |
| Plan format changes (breaks parser) | Low | Medium | Document required format, validate in forge-plan |

## Recommended Path

1. **Implement lite version now** — integrate into forge-verify (M effort, no blockers)
2. **Monitor usage** — if teams find it useful, implement full version
3. **Full version** — implement as `flowforge drift` command (L effort)

## Related Features

- **FF-006** (Cost dashboard) — could share instrumentation layer
- **FF-005** (Contradiction detection) — drift check could detect contradictory decisions
- **FF-001** (Dev agent code-aware) — Dev agent could check drift before implementing

---

**Next step**: Implement lite version in forge-verify. Create `/flow-start ff-007-lite-drift-check` when ready.
