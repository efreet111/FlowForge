---
name: forge-discovery
description: "Phase 0 (Discovery) of FlowForge. Explores memories, maps requirements, and produces context-map.md."
version: "1.2.0"
changelog:
  - date: "2026-09-08"
    note: "HU-026: Add step 3a-alt context-project fast-path (FR-007 read-if-exists, FR-008 alert-if-absent)"
  - date: "2026-08-28"
    note: "HU-023: Reorder flow — document-aware Engram search (step 3), gate of sufficiency replacing unconditional PRD+HUs read (step 3b), new diagnostic sections (ADR contradiction, broken refs, ADR conflict, ambiguous search, new requirement path), updated context-map output format"
  - date: "2026-08-27"
    note: "Initial tracked version"
---

# Forge Discovery Skill (English)

## Trigger / Context
When the orchestrator launches you to explore a new epic, investigate prior memories (both DB and local grep fallback), or map requirements.

---

## Why This Skill Exists (Provenance)

**Added**: 2026-06-18
**Author context**: while running a FlowForge-managed feature in `engram-dotnet` (the very backend FlowForge orchestrates against), the human asked: _"¿ese spike entra dentro del flow?"_ — referring to a 2h validation spike for ENG-404 (memory relations). The orchestrator ran the spike _outside_ the full FlowForge cycle (no `spec.md`, no `plan.md`, no PM-*) because the spike's deliverable is a learning, not a production feature.

During that spike, the discovery agent realized that `src/Engram.Verification/` already contains a complete **pattern library** (TraceRepository, LineageBuilder, RelationValidator) for requirement traceability. The spike cloned this pattern for general observations in **M effort (not XL)**. The realization that ENGRAM-DOTNET ITSELF was a pattern library the agent could have searched earlier is what motivated **step 5** below.

**This is why this step is non-negotiable**: we are not theorizing. We are documenting a method that, in production on 2026-06-18, reduced a real XL estimate to M in a 2h spike. Ignoring it means re-paying the discovery cost on every future greenfield-shaped request.

**Scope reminder**: FlowForge orchestrates _projects_ (engram-dotnet, FlowDocs, FlowForge itself, others). Step 5 must be run **inside the project under change** — not against the orchestrator codebase. The example grep commands below target the active project, not `FlowForge/`.

---

## Overview

1. **Receive User Request** – a new user story or change request arrives.
   - If a `.flowforge.json` exists in the project root, read the `paths` section to know where `PRD.md`, backlog HUs, and `features` (`.ai-work/`) live.
   - If the human references a specific HU (e.g. `HU-042`), locate it under `paths.backlog` and record its path in the Context Map.
2. **Keyword Extraction** – parse the prompt and extract 3‑5 highly specific technical/business terms (e.g., `auth`, `login`, `jwt`, `performance`, `sqlite`).
3. **Memory Search — Document-Aware Engram Search (FR-001, FR-009)**
    - **Pre-step**: Call `mem_current_project` (no parameters needed) to auto-detect the active project from CWD. Use the returned `project` value in all subsequent memory calls — do not hardcode project names.
    - **Precondition**: only run when `.flowforge.json` has `docs_framework` set to a non-empty value other than `"none"` (e.g. `"flowdoc"` with `docs_framework_version: "2.0"`). If `docs_framework` is absent, `null`, or `"none"`, skip to step 3-alt (Local Fallback).
    - **Document-aware query construction**: from the 3-5 keywords extracted in step 2, build queries using the `docs/` namespace. Execute in order; **stop as soon as a query returns results > 0**:
      1. `mem_search("docs/adr/ {keyword1} {keyword2}")` — ADRs first (most likely to constrain design)
      2. `mem_search("docs/prd {keyword1}")` — PRD
      3. `mem_search("docs/rfc/ {keyword1}")` — RFCs
      4. `mem_search("docs/api/ {keyword1}")` — API docs
      5. `mem_search("docs/db/ {keyword1}")` — DB docs
      6. `mem_search("{keyword1} {keyword2} docs/")` — broadest fallback
    - **CRITICAL**: Results are truncated. For each relevant observation, call `mem_get_observation(id)` to retrieve the full, uncut content. Record the observation `#id` for traceability.
    - **Fallback — Engram unavailable (FR-009)**: if `mem_search` errors or times out (MCP error), catch the failure and fall back to reading `docs/PRD.md` + relevant docs in `docs/` directly. If `mem_search("docs/")` returns 0 results (empty index), treat the index as absent and use `docs/` as the primary source.
    - **Local Fallback (Attempt B)**: if Engram is available but yields nothing, use `grep_search` over `./.engram/local_memory/` for the extracted keywords. For each matching file, read it fully to extract its YAML FrontMatter and structured content.

3a-alt. **Context-project fast-path (FR-007, FR-008)**:
     - Check if `docs/project-context.md` exists.
     - **IF exists**: read as base structural context (tech stack, architecture, key decisions).
       → Supplements CKP-0 context (reduces Engram queries for basic structure).
       → Record in context-map: `Context-project: read`.
     - **IF NOT exists**: emit alert `⚠️ context-project ausente — create docs/project-context.md to enable fast onboarding`.
       → Record in context-map diagnostics.
     - **IF exists but unfilled placeholders** (e.g. contains `{{...}}` hints): emit warning `⚠️ context-project incompleto — fill placeholders to improve onboarding quality`.

3b. **Gate of Sufficiency + Conditional Reading (FR-002, FR-003)**
    - **Precondition**: only when step 3 returned results from Engram.
    - **Deterministic checks** (ALL must pass):
      1. `results > 0` — at least one observation found
      2. `topic_key ∈ docs/*` — observation's topic_key matches the `docs/` namespace (e.g., `docs/adr/product/005`, `docs/prd`)
      3. `Where` path exists on filesystem — verify with a file existence check
    - **Semantic check (conservative)**:
      4. The observation's `What`/`Why`/`Learned` covers ≥2 of the requirement's keywords
      5. If ANY doubt about summary quality → mark INSUFFICIENT (read the file)
    - **Result**:
      - **SUFFICIENT**: use the index content as context. Do NOT read files. Record Engram source IDs.
      - **INSUFFICIENT**: proceed to conditional reading below.
    - **Conditional reading**: read ONLY the files referenced in the `Where` field of relevant observations. Do NOT read PRD + 3 HUs unconditionally. If a `Where` path does not exist → report as broken reference (see step 3d).

3c. **New Requirement Path (FR-004)**
    - If step 3 returned 0 results from Engram AND the fallback (local grep / direct docs/ scan) found nothing relevant → classify as **"new" requirement**.
    - Proceed directly to step 5 (Pattern Search) WITHOUT activating CKP-0.
    - **Distinction from CKP-0**:
      - Vague request with no context (e.g., "improve performance") → CKP-0 🔴 hard stop (step 7).
      - Specific request with no prior info in Engram/docs → "new requirement" path → proceed to step 5.
    - Record `Requirement classification: new` in the context-map.

3d. **ADR Contradiction Detection (FR-005)**
    - For each ADR found via Engram (topic_key ∈ `docs/adr/*`), evaluate if its decision **contradicts** the requirement's intent.
    - **Contradiction**: ADR says "do NOT use X" but requirement asks for X → report in context-map BEFORE proceeding to Phase 1: `ADR-NNN contradicts requirement: {brief description}`.
    - **Complement**: ADR provides context that supports or is neutral to the requirement → incorporate as context silently, no report needed.

3e. **Broken Reference Reporting (FR-007)**
    - When a `Where` field points to a non-existent file (detected lazily when attempting to read the path):
      - Report in context-map: `{path} not found — suggest re-index`
      - Do NOT auto-delete the observation. Do NOT auto-fix. This is diagnostic only (NFR-004).

3f. **ADR Conflict Detection (FR-008)**
    - When multiple ADRs on the same topic contradict each other:
      - Report in context-map: `ADR-NNN vs ADR-MMM on {topic} — manual resolution required`
    - **Distinction**: observations that are "related" (same topic, different granularity — per ADR-021 Learned) are NOT contradictions. Only report actual conflicting decisions.

3g. **Ambiguous Search Handling (FR-010)**
    - When `mem_search` returns multiple results from unrelated topics (no dominant `topic_key` cluster):
      - **Block**: `**BLOCKED: ambiguous results, clarification needed**` — request human clarification via CKP-0 mechanism.
    - When one dominant candidate exists by `topic_key` but other results are tangentially related:
      - **Proceed** with the dominant candidate and document the ambiguity in the context-map: `Ambiguity: {brief description}`.

4. **Association Mapping & Narrative Thread**
   - Determine if the new user story belongs to an existing Epic in memory, or inherits architectural constraints from an ongoing topic (check if observations share the same `topic_key`).
5. **Pattern Search (Codebase Cloning) — MANDATORY**
   - **Purpose**: discover _existing implementations of the same architectural shape_ inside the project under change. If a module solves 80%+ of the problem, the right move is to **clone / extend / re-use**, not to design from scratch.
   - **Concrete trigger evidence** (why this step exists):
     - ENG-404 (memory relations) was estimated XL because no one searched `Engram.Verification/`. After a 2h spike that cloned TraceRepository + LineageBuilder, the estimate was revised to **M**.
     - The original XL estimate was a **false estimate**: it priced a greenfield design that should never have been proposed. The pattern was sitting in the repo.
   - **How to execute** (in the _active project_, not in FlowForge/):
     - Extract 1-2 **architectural shape keywords** from the request (e.g., "graph", "BFS", "topic_key persistence", "validation set", "lineage", "cycle detection", "upsert by key", "diff-and-patch", "background job", "retry with backoff").
     - Run grep/code_search against the **active project**: e.g. `grep -r "BFS\|MaxHops\|HashSet<long>" src/`, `grep -rn "topic_key:" src/`, `grep -rn "CycleDetected\|MaxHops" src/`.
     - For each candidate, read the file fully and judge: _does it solve a structurally similar problem?_ Example: `LineageBuilder` on `string` RF-* IDs vs. `MemoryLineageBuilder` on `long` observation IDs is structurally identical — only the ID type changes. That is a clone, not a redesign.
     - Capture each finding in the Context Map under a mandatory section: `## Reusable Patterns Found`. If the search returned nothing, write the search terms and the (negative) result explicitly — _the absence of a finding is itself a finding, and is auditable_.
   - **Required Context Map additions** (effective for all Context Maps produced after 2026-06-18):

     ```markdown
     ## Reusable Patterns Found
     - `src/.../X.cs` (line N): <what it does> → can be cloned / extended / re-used for <this request>
     - Or: no patterns found. Search terms: ["BFS", "topic_key", "cycle detection"]. Result: negative.
     ```

   - **Anti-pattern (CKP-0 violation)**: proposing a greenfield design when an existing module solves 80%+ of the problem. The human reviewer (or `forge-verify`) MUST reject the design and send the agent back to step 5. This is a hard, mechanical check — not a stylistic preference.
   - **Why this step was added to the SKILL (not just the CHANGELOG)**: skills are loaded at agent runtime. CHANGELOG is read by humans months later. If the why lives only in the CHANGELOG, the next agent to run discovery will not see it. The provenance section at the top of this file is the _only_ place where the why is guaranteed to load with the skill.
6. **Hard Stop — CKP-0 🔴**
   - If the user request is too vague (e.g., "improve performance") and no prior context clarifies it, **STOP IMMEDIATELY**. Ask clarification questions before proceeding.
   - This checkpoint is **BINARY** — there is no "maybe". If context is insufficient, the entire flow halts here. The orchestrator MUST NOT proceed to Phase 1.

---

## Your Output (Context Map)

If valid context exists, produce a concise **Context Map (Discovery)** that serves as the mandatory preface for the Architecture Agent (Phase 1). The map must list:

- Relevant prior observations with Engram IDs (from step 3)
- Associated epics and topic_keys (from step 4)
- **Reusable Patterns Found** (from step 5) — _mandatory; missing this section is a CKP-0 violation_
- **FlowDoc context** (from steps 3b-3g) — HU referenced, PRD read status, Engram sources, diagnostics
- Any constraints that must be respected (including ADR contradictions)

### Context Map output format

```markdown
## FlowDoc context
- PRD: docs/PRD.md (read: yes/no)
- HU referenced: HU-NNN — [title] (path: docs/tasks/HU-NNN-*.md)
- HU flowforge_slug: [current value or "unset"]
- Engram sources: [#id1, #id2, ...] (topic_keys: docs/adr/product/005, docs/prd)
- ADR contradictions: [ADR-NNN contradicts requirement: {brief}] | none
- ADR conflicts: [ADR-NNN vs ADR-MMM on {topic}] | none
- Broken refs: [{path} not found — suggest re-index] | none
- Ambiguity: [{brief description}] | none
- Requirement classification: [existing | new]

## Reusable Patterns Found
- `src/.../X.cs` (line N): <what it does> → can be cloned / extended / re-used
- Or: no patterns found. Search terms: [...]. Result: negative.
```

**Mandatory**: write the Context Map to disk at `.ai-work/{feature-slug}/context-map.md`. Do not only output it inline — the file must exist on disk for the orchestrator and subsequent agents to reference.

**Final line of your response must be one of these exact tokens** (used by the orchestrator to route the flow):
- `**CLEAR**` — context is sufficient, advance to Phase 1 (forge-arch).
- `**BLOCKED: [reason]**` — context is insufficient (CKP-0) or ambiguous (step 3g); include a 1-line reason. Orchestrator halts until human clarifies.

---

## Cross-References

- Spike that motivated this change: `engram-dotnet/.ai-work/eng-404-spike/{spike.md, learnings.md}`
- Changelog entry: `CHANGELOG.md` → [Unreleased] → item 21
- Next agents in the flow (orchestrator delegates): `forge-arch` (reads the Context Map at CKP-1), `forge-verify` (rejects designs that skipped step 5)
