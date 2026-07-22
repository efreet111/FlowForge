---
description: Fase Memory y cierre (CKP-4 deploy gate)
---

# /flow-close — Memory + deploy gate

1. Delegate to **forge-memory**.
2. Memory blocks if any PM-* in `spec.md` still `[ ]` or rework ticket open.
3. If blocked: instruct human to run PM-*, mark `[x]`, retry `/flow-close`.
4. Preview only if human says **"preview de cierre"** → `summary.preview.md`, not `summary.md`.
5. CKP-4 🟢: ask human about deploy/merge.

Orchestrator does not generate `summary.md` inline when PM-* are pending.
