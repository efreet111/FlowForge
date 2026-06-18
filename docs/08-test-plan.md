# Test Plan: EngramFlow Validation in VS Code (OpenSpec Isolation)

> **Objective**: Empirically, objectively, and non-subjectively validate the behavior of the 5 Core EngramFlow Skills in Visual Studio Code using the change-isolation structure of **FlowForge (Agentic SDLC)**. This ensures phase rules, the Capability Matrix, the Ralph Wiggum Loop, and the Sentinel Judge work cleanly before applying them in production.

---

## 1. Practice Project: `practice-todo-cli`

To avoid dependency noise, we'll use a small CLI **Python** application for task management with JSON file persistence.

### 1.1 Practice Project Structure
Create a `practice-todo-cli` directory in your workspace root with this structure:

```text
practice-todo-cli/
├── todo.py          # CLI application logic and JSON persistence
├── test_todo.py     # Unit test suite (using pytest or unittest)
├── tasks.json       # Simulated JSON database
└── .ai-work/
    └── prioridades-tareas/   <-- CHANGE ISOLATION FOLDER
```

### 1.2 Starter Code
#### `todo.py`
```python
import json
import os

DB_FILE = "tasks.json"

def load_tasks():
    if not os.path.exists(DB_FILE):
        return []
    with open(DB_FILE, "r") as f:
        try:
            return json.load(f)
        except json.JSONDecodeError:
            return []

def save_tasks(tasks):
    with open(DB_FILE, "w") as f:
        json.dump(tasks, f, indent=4)

def add_task(title):
    if not title.strip():
        raise ValueError("Title cannot be empty")
    tasks = load_tasks()
    new_task = {
        "id": len(tasks) + 1,
        "title": title,
        "completed": False
    }
    tasks.append(new_task)
    save_tasks(tasks)
    return new_task
```

#### `test_todo.py`
```python
import os
import pytest
from todo import add_task, load_tasks, DB_FILE

@pytest.fixture(autouse=True)
def cleanup():
    if os.path.exists(DB_FILE):
        os.remove(DB_FILE)
    yield
    if os.path.exists(DB_FILE):
        os.remove(DB_FILE)

def test_add_task_success():
    task = add_task("Buy milk")
    assert task["title"] == "Buy milk"
    assert task["completed"] is False
    
    tasks = load_tasks()
    assert len(tasks) == 1

def test_add_task_empty_title():
    with pytest.raises(ValueError, match="Title cannot be empty"):
        add_task("")
```

---

## 2. Experiment Task (The Change)

**User Requirement**:
> "I want to add priorities (HIGH, MEDIUM, LOW) to tasks when I create them, and be able to filter tasks by a specific priority."

---

## 3. Test Execution Guide Phase by Phase

> **Commands vs agents (v0.4):** You type **`/flow-*`** to the orchestrator; it delegates to **`forge-*`**.
> Don't use `/forge-memory` or `@forge-dev` as main commands — they're not official.
> See [`QUICKSTART.md`](../QUICKSTART.md) and [`14-flowforge-complete-reference.md`](14-flowforge-complete-reference.md).
>
> **Paths**: artifacts in `.ai-work/{feature-slug}/` (kebab-case), not `openspec/changes/`.

Execute the following instructions in the IDE chat in **Agent** mode, with FlowForge rules active.

### 3.0 Phase 0: Discovery + Spec (`/flow-start`)

* **Input prompt**:
  ```
  /flow-start Task Priorities — add HIGH, MEDIUM, LOW priorities when creating tasks and filter by priority. Default MEDIUM.
  ```
* **Orchestrator delegates**: `forge-discovery` → `forge-arch` → `spec.md` in `.ai-work/prioridades-tareas/`
* **CKP criteria (CKP-1)**: spec with capability_matrix, GWT, PM-*; human approves before `/flow-plan`

### 3.1 Phase 1: Spec (`forge-arch` via `/flow-start`)

* **Legacy alternative (not recommended)**: invoke `@forge-arch` directly — skips CKP-0 and discovery.
* **Acceptance criteria (what to verify in `spec.md`)**:
  - [ ] Did it create `spec.md` in `.ai-work/prioridades-tareas/spec.md`?
  - [ ] Does the `capability_matrix` classify HIGH/MEDIUM/LOW as `deterministic`?
  - [ ] At least 2 Given-When-Then scenarios?
  - [ ] No Python code proposed?

### 3.2 Phase 2: Plan (`/flow-plan` → `forge-plan`)

* **Input prompt**:
  ```
  /flow-plan
  ```
* **Acceptance criteria (what to verify in `plan.md`)**:
  - [ ] Is `plan.md` in `.ai-work/prioridades-tareas/plan.md`?
  - [ ] Proposed Changes only `todo.py` and `test_todo.py`?
  - [ ] Explicit JSON schema with `priority`?
  - [ ] Topological checklist (persistence before tests)?

### 3.3 Phase 3: Execution (`/flow-dev` → `forge-dev`)

* **Input prompt**:
  ```
  /flow-dev
  ```
* **Acceptance criteria**:
  - [ ] Respects plan without freelancing?
  - [ ] Ralph Wiggum: pytest green?
  - [ ] Tests with `[FR-XXX]` prefix?

### 3.4 Phase 4: Judgment (`/flow-verify` → `forge-verify`)

Inject a manual failure in `todo.py` before verifying.

* **Input prompt**:
  ```
  /flow-verify
  ```
* **Acceptance criteria**:
  - [ ] REJECTED or `rework_ticket.md` with `cycle_count: 1`?
  - [ ] Objective reason documented?
  - [ ] After fixing: second `/flow-verify` → PASS in `verify-report.md`

### 3.5 Phase 5: Closure (`/flow-close` → `forge-memory`)

* **Input prompt**:
  ```
  /flow-close
  ```
  (Don't use `/forge-memory` — the canonical command is **`/flow-close`**.)

* **Acceptance criteria**:
  - [ ] PM-* marked `[x]` in `spec.md`?
  - [ ] `summary.md` in `.ai-work/prioridades-tareas/`?
  - [ ] `mem_session_summary` called (or fallback in `.engram/local_memory/` if MCP falls)?
  - [ ] Key decisions persisted via Memory Curation Protocol (orchestrator + Engram)?

---

## 4. Experiment Evaluation Matrix

Use this table to grade the performance of your OpenRouter models in VS Code:

| Skill | Model Used | Hard Rules Met? (Y/N) | Execution Time | Output Quality (1-10) |
|-------|------------|----------------------|----------------|--------------------------|
| **forge-arch** | | | | |
| **forge-plan** | | | | |
| **forge-dev** | | | |
| **forge-verify** | | | | |
| **forge-memory** | | | |

If all phases receive a grade of **8/10** or higher and phase constraints (no-touch, no-coding in Arch, no-freelancing in Dev) are strictly respected, the methodology will be considered **Production Ready** for FlowForge.