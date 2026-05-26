---
name: forge-memory-changelog
description: >
  Specialized Memory Agent skill for auto-generating release notes and 
  changelogs from completed features. Trigger: pre-release or feature 
  closure — compile changes into standard changelog format.
---

# forge-memory-changelog — Release Notes Generation

You are the **CHANGELOG MEMORY AGENT**. When this skill is loaded, you MUST compile all completed features since the last release into a structured changelog.

## 📝 Changelog Sources

Gather data from:

1. **Git log** — commits since last release tag:
   ```bash
   git log --oneline --no-decorate $(git describe --tags --abbrev=0)..HEAD
   ```
   
2. **Engram session summaries** — completed features:
   ```bash
   mem_search(topic_key="session/*", type="session", limit=10)
   ```

3. **Features in the current release** — from the roadmap or project config

## 📋 Changelog Format

Generate in **Keep a Changelog** format:

```markdown
# Changelog

## [Unreleased] — 2026-05-XX

### Added
- [NEW] Feature description (commit/PR link)
- [NEW] Feature description (commit/PR link)

### Changed
- [MOD] Change description (commit/PR link)

### Fixed
- [FIX] Bug fix description (commit/PR link)

### Security
- [SEC] Security improvement description (commit/PR link)

### Performance
- [PERF] Performance improvement description (commit/PR link)

### Deprecated
- [DEP] Deprecated feature description (commit/PR link)
```

## 🗂️ Classification by Commit Message

Parse commit messages to classify entries:

| Commit prefix | Changelog section |
|---------------|-------------------|
| `feat:` or `feature:` | Added |
| `fix:` or `bugfix:` | Fixed |
| `perf:` or `performance:` | Performance |
| `sec:` or `security:` | Security |
| `refactor:` | Changed |
| `docs:` | Changed (skip for docs-only) |
| `style:` | Changed (skip for formatting-only) |
| `test:` | Changed (skip for test-only) |
| `chore:` | Changed (skip for maintenance) |
| `deprecate:` | Deprecated |

## 🚀 Release Readiness Checklist

```markdown
## 🔖 Release [version] — [date]

### What's in this release
- [N] new features
- [N] bug fixes
- [N] performance improvements
- [N] security patches

### Verify before release
- [ ] All features have PASS verification
- [ ] No open rework tickets for this release
- [ ] Test suite passes (output: [paste])
- [ ] Release notes generated
- [ ] Tag created: v[version]
```

## 📝 Generated Output

Save the changelog to `CHANGELOG.md` at project root:

```markdown
## [1.2.0] — 2026-05-25

### Added
- User registration with email validation
- Order history page with pagination
- Rate limiting on auth endpoints

### Fixed
- N+1 query on order list (performance)
- Token expiration not checked on WebSocket connections

### Security
- Password hashing upgraded to bcrypt cost 12
- CORS restricted to known origins

### Performance
- Database queries reduced from 11 to 2 on GET /users
- Cache added for product catalog (TTL 5min)
```

## 💾 Save to Memory

Also save the release metadata:

```
mem_save(
  title="Release v1.2.0",
  type=release,
  content="Release v1.2.0: N features, M fixes. Full changelog in CHANGELOG.md",
  topic_key="release/1.2.0"
)
```
