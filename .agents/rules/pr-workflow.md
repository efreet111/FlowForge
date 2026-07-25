---
alwaysApply: true
description: Enforces Pull Request workflow for all changes to main branch.
---

# Pull Request Workflow — Required for All Changes

The `main` branch is protected. **Direct push to main is not allowed.** All changes must go through Pull Requests.

## Branch Protection Rules

- **Required PRs**: All changes must be submitted via Pull Request
- **Required status checks** (5 checks must pass):
  - `Build & Smoke (ubuntu-latest)` — builds installer on Linux
  - `Build & Smoke (windows-latest)` — builds installer on Windows
  - `Happy Path Install (Linux)` — full installer test in Docker
  - `Happy Path Install (Windows)` — full installer test on Windows
  - `smoke` — OpenCode configuration validation
- **Enforce admins**: Even repository admins must use PRs
- **Strict mode**: Branch must be up-to-date before merging

## Correct Workflow

### 1. Create a feature/fix branch
```bash
git checkout -b fix/descriptive-name
# or
git checkout -b feat/descriptive-name
```

### 2. Make commits on the branch
```bash
git add .
git commit -m "fix(component): description"
```

### 3. Push the branch to origin
```bash
git push -u origin fix/descriptive-name
```

### 4. Create a Pull Request
```bash
gh pr create --title "fix: description" --body "Detailed description of changes"
```

Or use the interactive mode:
```bash
gh pr create
```

### 5. Wait for CI checks to pass
The PR will automatically run:
- `OpenCode Smoke` (~10s)
- `Test Installer` (~2-3min)

### 6. Merge the PR
Once checks pass, merge via GitHub UI or:
```bash
gh pr merge --squash --delete-branch
```

## Branch Naming Conventions

| Type | Format | Example |
|------|--------|---------|
| Bug fix | `fix/short-description` | `fix/installer-timeout` |
| Feature | `feat/short-description` | `feat/add-dark-mode` |
| Documentation | `docs/short-description` | `docs/update-readme` |
| Refactor | `refactor/short-description` | `refactor/extract-service` |
| Hotfix | `hotfix/short-description` | `hotfix/critical-bug` |

## Commit Message Format

Use [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

**Types**: `fix`, `feat`, `docs`, `style`, `refactor`, `test`, `chore`

**Examples**:
- `fix(installer): separate API and download timeouts`
- `feat(auth): add OAuth2 support`
- `docs(readme): update installation instructions`

## PR Title Format

Same as commit messages, but can be more descriptive:

```
fix(installer): resolve timeout issues with large downloads

- Separate API timeout (30s) from download timeout (300s)
- Add per-operation CancellationTokenSource
- Verified in Docker environment (23 PASS / 0 FAIL)
```

## Emergency Hotfixes

For critical production issues that require immediate merge:

1. Create branch from `main`
2. Make minimal fix
3. Create PR with `[HOTFIX]` prefix in title
4. Request expedited review
5. Merge after CI passes (no exceptions)

## What NOT to Do

❌ **Never do this**:
```bash
git checkout main
git push origin main  # This will FAIL — branch is protected
```

❌ **Never force push to main**:
```bash
git push --force origin main  # This will FAIL — force pushes disabled
```

✅ **Always use PRs**:
```bash
git checkout -b fix/my-fix
# make changes
git push -u origin fix/my-fix
gh pr create
```

## Benefits of PR Workflow

1. **Code review**: Changes are visible before merging
2. **CI validation**: Automated tests run on every PR
3. **Audit trail**: Git history shows what was merged and when
4. **Rollback capability**: Easy to revert specific PRs
5. **Discussion**: Team can discuss changes in PR comments

## Related Rules

- [`git-sin-push.md`](./git-sin-push.md) — Don't push without explicit request
- [`workflow.md`](./workflow.md) — FlowForge workflow phases
