# Forge Discovery Skill (English)

## Trigger / Context
When the orchestrator launches you to explore a new epic, investigate prior memories (both DB and local grep fallback), or map requirements.

## Overview
1. **Receive User Request** – a new user story or change request arrives.
2. **Keyword Extraction** – parse the prompt and extract 3‑5 highly specific technical/business terms (e.g., `auth`, `login`, `jwt`, `performance`, `sqlite`).
3. **Memory Search (Dual‑Level)**
   - **Attempt A: engram‑dotnet Engine (Preferred)**
     - Invoke `mem_search` with the extracted keywords filtered by the current project to get candidate observations.
     - **CRITICAL**: Results are truncated. For each relevant observation, call `mem_get_observation(id)` to retrieve the full, uncut content.
   - **Attempt B: Local Fallback**
     - Use `grep_search` over the `./.engram/local_memory/` directory, searching for the extracted keywords in local Markdown files.
     - For each matching file, read it fully with `view_file` to extract its YAML FrontMatter and structured content.
4. **Association Mapping & Narrative Thread**
   - Determine if the new user story belongs to an existing Epic in memory, or inherits architectural constraints from an ongoing topic (check if observations share the same `topic_key`).
5. **Hard Stop — CKP-0 🔴**
    - If the user request is too vague (e.g., "improve performance") and no prior context clarifies it, **STOP IMMEDIATELY**. Ask clarification questions before proceeding.
    - This checkpoint is **BINARY** — there is no "maybe". If context is insufficient, the entire flow halts here. The orchestrator MUST NOT proceed to Phase 1.

---

## Your Output (Context Map)
If valid context exists, produce a concise **Context Map (Discovery)** that serves as the mandatory preface for the Architecture Agent (Phase 1). The map should list relevant prior observations, associated epics, and any constraints that must be respected.
