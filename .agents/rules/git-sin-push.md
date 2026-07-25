---
alwaysApply: true
description: Prevents accidental git push without explicit human request. Enforces PR workflow.
---

# Git Safety — No Push Without Explicit Request + PR Required

## Rule 1: No Push Without Explicit Permission

You may commit changes freely, but you MUST NOT push to any remote
until the user explicitly requests it (e.g. "push", "subir los cambios",
"push to origin").

Before pushing:
1. Confirm the user explicitly said "push", "subir", "upload", "subilo", or similar direct command.
2. Do NOT interpret "está listo", "dale", "commit and go" as push authorization.
3. If unsure, ask: "¿Querés que suba los commits a origin?"

## Rule 2: NEVER Push Directly to Main

The `main` branch is **protected**. Direct pushes to `main` will fail.

**Always use the PR workflow:**
```bash
# Create feature branch
git checkout -b fix/my-fix

# Commit changes
git add .
git commit -m "fix: description"

# Push the branch (NOT main)
git push -u origin fix/my-fix

# Create Pull Request
gh pr create --title "fix: description" --body "Details"
```

See [`pr-workflow.md`](./pr-workflow.md) for complete PR workflow documentation.