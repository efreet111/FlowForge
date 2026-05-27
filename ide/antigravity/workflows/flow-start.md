# /flow-start — New feature

1. Derive `feature-slug` (kebab-case) from the description.
2. Delegate to **forge-discovery** with user text + slug.
3. If BLOCKED (CKP-0 🔴): STOP — ask human to clarify. Do not call forge-arch.
4. If CLEAR: delegate to **forge-arch** → `.ai-work/{feature-slug}/spec.md`.
5. CKP-1 🟡: stop and ask human to approve spec before `/flow-plan`.

Orchestrator does not write spec or code inline.
