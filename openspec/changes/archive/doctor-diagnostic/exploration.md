## Exploration: Understanding EngramFlow and Roadmap

### Current State
- **FlowForge** is a documented methodology for Agentic SDLC (v0.2).
- **engram-dotnet** is the storage engine and is quite advanced (236+ tests).
- Key features already implemented in `engram-dotnet`:
  - `verification-tools`: `mem_verify_artifact`, traceability matrix, cycle tracking.
  - `promotion-level2`: Promoting observations to structured `.md` files.
  - `traceability`: Lineage and requirement tracing.
  - `ttl-configurable`: Automatic pruning of old observations.
- The methodology defines 4 phases and 3 human checkpoints.

### Affected Areas
- `docs/` — Documentation of the methodology.
- `engram-dotnet/` (external repo) — Implementation of the storage engine.

### Approaches
1. **Doctor Diagnostic** — Ensure the 258+ tests pass and the environment is sane.
   - Pros: Quick win, ensures stability.
   - Cons: Doesn't add new methodology features.
   - Effort: Low (4-6h).

2. **Orchestrator Design** — Start building the actual tool that "runs" the methodology.
   - Pros: Core to the FlowForge methodology.
   - Cons: High complexity.
   - Effort: High.

3. **Offline-First Sync** — Complete remaining engram-dotnet gaps.
   - Pros: Key for distributed teams.
   - Cons: Deep backend work.
   - Effort: Medium.

### Recommendation
Start with the **Doctor Diagnostic**. It is marked as an active sprint in the roadmap and provides a solid foundation before building more complex features.

### Risks
- The methodology is v0.2 and might evolve.
- Dependencies on external backends (SQLite/Postgres).

### Ready for Proposal
Yes. Proceeding with Doctor Diagnostic.
