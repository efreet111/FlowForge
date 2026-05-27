# Discovery: Context Map — CRUD de tareas

**Feature slug**: `crud-tareas`  
**User request**: `/flow-start CRUD de tareas`  
**Date**: 2026-05-27  
**Status**: 🟢 **CLEAR** (CKP-0 passed)

---

## 1. Feature Intent (User Story)

Build a **Task Manager API** — REST JSON over Node.js + TypeScript + SQLite:

- **CRUD operations**: Create, Read (list + get by id), Update, Delete tasks
- **Required field**: `title` (string)
- **Optional fields**: `description`, `status`, `priority`
- **Validations**: HTTP 400 for bad input, 404 if not found
- **Response format**: JSON with consistent structure across endpoints
- **Error messages**: Spanish (product layer)

---

## 2. Stack Alignment (Non-negotiable V1)

| Layer | Choice | Notes |
|-------|--------|-------|
| Runtime | Node.js LTS | ✅ Standard, long-term support |
| Language | TypeScript | ✅ Type safety for API contracts |
| Persistence | SQLite (file-based) | ✅ No external DB; suitable for demo/MVP |
| API | HTTP REST (JSON) | ✅ Standard, testable, no WebSocket complexity |
| Tests | vitest + supertest | ✅ Modern, fast, integrates well |
| **Out of scope V1** | **Auth, UI** | ✅ Deferred; not blocking core CRUD |

**Source**: `.cursor/rules/project-context.mdc` ✓

---

## 3. Prior Context & Memory Search

### Search Keywords Extracted
- `task`, `crud`, `sqlite`, `rest api`, `node.js`, `typescript`, `demo`

### Memory Findings
- **No prior epics in engram** (demo starts fresh)
- **No prior observations** in `.engram/local_memory/`
- **No CVEs** related to CRUD + SQLite (schema-level CRUD is safe by design)
- **No compliance needs** in V1 (no HIPAA, GDPR, SOC2 triggered; auth is out of scope)

### Cost & Infra
- **Estimate**: Low
  - SQLite is local, zero hosting cost
  - No external APIs
  - No managed DB fees
  - Node.js LTS is free/OSS

---

## 4. Associated Decisions & Constraints

### Architectural Constraints (Inherited from project-context)
1. **SQLite schema**: Must support tasks with at least: `id`, `title`, `description`, `status`, `priority`, timestamps
2. **No migration framework required**: Simple schema init on app startup is acceptable for demo
3. **Validation layer**: Input sanitization; HTTP status codes must be correct (400, 404, 500)
4. **File structure**: Code in `src/`, tests in `tests/`, database in `data/` (gitignored)
5. **No user/tenant isolation**: Single-tenant; all tasks in one namespace

### Testing Strategy (Already Defined)
- **Unit tests**: vitest (business logic)
- **Integration tests**: supertest (HTTP layer)
- **Minimum coverage**: All CRUD endpoints + error cases
- **Manual test cases**: PM-* gates in `spec.md` (will be written by `forge-arch`)

---

## 5. Functional Scope (Detailed Interpretation)

### Endpoints (Anticipated)

| Method | Endpoint | Purpose | Status Code |
|--------|----------|---------|-------------|
| `POST` | `/tasks` | Create a task | 201 (created) or 400 (validation error) |
| `GET` | `/tasks` | List all tasks (+ optional filters) | 200 |
| `GET` | `/tasks/:id` | Get single task | 200 or 404 |
| `PUT` | `/tasks/:id` | Update task | 200 or 404 |
| `DELETE` | `/tasks/:id` | Delete task | 204 (no content) or 404 |

### Typical Task Payload

```json
{
  "id": "uuid or auto-increment",
  "title": "Buy milk",
  "description": "Whole milk from grocery store",
  "status": "pending",
  "priority": "high",
  "createdAt": "2026-05-27T12:00:00Z",
  "updatedAt": "2026-05-27T12:00:00Z"
}
```

### Validation Rules (Baseline)
- **title**: Required, non-empty string, max 255 chars (typical)
- **description**: Optional, max 1000 chars
- **status**: Optional enum (e.g., `pending`, `in-progress`, `completed`)
- **priority**: Optional enum (e.g., `low`, `medium`, `high`)

---

## 6. Risk Assessment

### Known Risks (Low)
1. **SQLite scalability**: Not a risk for demo/MVP; if prod → migrate to PostgreSQL later
2. **Concurrent writes**: SQLite has file-lock limitations; acceptable for single-dev demo
3. **Error message localization**: Ensure Spanish messages are clear; no i18n framework needed (hardcoded strings OK for demo)

### Mitigations
- Document SQLite assumptions in `spec.md`
- Include upgrade path note in `summary.md` (post-verify)
- Simple error message map in code (no i18n library overhead)

---

## 7. Interdependencies & Blockers

### None (CKP-0 ✅)
- Project context is complete and unambiguous
- Stack is pre-approved (no negotiation needed)
- No external dependencies block discovery
- Scope is clear, realistic, and testable

---

## 8. Checkpoint CKP-0 Verdict

| Check | Result | Evidence |
|-------|--------|----------|
| **Requirement clarity** | ✅ CLEAR | "CRUD de tareas" maps to standard REST CRUD pattern; scope defined in project-context.mdc |
| **Stack alignment** | ✅ CONFIRMED | Node.js + TS + SQLite + vitest non-negotiable; no conflicts |
| **Prior context** | ✅ NONE NEEDED | Fresh demo; no prior CVEs, compliance, or architectural debt |
| **Scope creep risk** | ✅ LOW | Auth and UI explicitly excluded; feature boundary is tight |
| **Feasibility** | ✅ HIGH | CRUD on SQLite is a solved problem; standard patterns apply |

### **Decision: 🟢 CLEAR — Proceed to forge-arch (CKP-1 gate)**

---

## 9. Handoff to forge-arch

### Files Created
- `.ai-work/crud-tareas/context-map.md` ← this document

### Artifacts Expected from forge-arch
- `.ai-work/crud-tareas/spec.md` with:
  - Requirements (RF-001, RF-002, RF-003, etc.)
  - Non-functional requirements (response times, error handling)
  - Capability matrix (endpoints, status codes, validations)
  - Manual test cases (PM-* gates)
  - Approval template

### Key Inputs for arch Phase
- Stack is locked: Node.js LTS, TypeScript, SQLite, REST, vitest + supertest
- No auth, no UI, no external APIs
- Error messages in Spanish
- Artifacts only under `.ai-work/crud-tareas/`

---

## 10. Metadata

| Field | Value |
|-------|-------|
| **Discovery phase** | Complete |
| **CKP-0 status** | ✅ PASSED |
| **Next phase** | forge-arch (`spec.md`) |
| **Checkpoint gate** | CKP-1 (human approval of spec) |
| **Created** | 2026-05-27 12:05 UTC |
| **Agent** | forge-discovery |
