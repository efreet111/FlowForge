---
capability_matrix:
  ai_reasoning:
    - Wording quality and tone consistency across translated docs
    - Whether optional docs/15 Part 1 table translation is worth the effort (recommendation in plan phase)
  deterministic:
    - All targeted files must be English per I18N policy
    - CKP terminology (CKP-0→4, verify-report, .ai-work/{slug}/) must match project standard
    - docs/08 must not reference openspec/changes/ as primary artifact path (use .ai-work/{slug}/)
    - docs/03 must state implemented vs historical gap status without reopening engram backlog
    - I18N.md and 04-roadmap.md updated when items complete
---

# Spec: CKP-coherent docs & public i18n (Item 15)

## 1. Objective and scope

**Objective:** Close the **partial** release-gate items **CKP-coherent docs** and **Public language** by finishing the documented i18n backlog, so FlowForge is publish-ready for English public docs without contradictory or legacy-only Spanish content in priority files.

**In scope:**

- Translate and modernize [`docs/08-test-plan.md`](../../docs/08-test-plan.md) to English.
- Refactor [`docs/03-engram-dotnet-gaps.md`](../../docs/03-engram-dotnet-gaps.md) body to English with a short “implemented features / reference only” posture.
- Update [`docs/I18N.md`](../../docs/I18N.md) and [`docs/04-roadmap.md`](../../docs/04-roadmap.md) statuses when work completes.
- Optional (separate phase): English section titles for `docs/15` Part 2–3; optional Part 1 table localization.

**Out of scope:**

- Rewriting all 31 specialized skills (spot-check only if gaps found).
- New engram-dotnet features or MCP tool implementation.
- Translating `README.es.md` / `QUICKSTART.es.md` (already Spanish entry points by policy).
- OpenCode install script changes (unrelated; tracked separately in CHANGELOG Unreleased).

## 2. Functional requirements (FR)

### FR-001 — docs/08 English + path alignment

Bring `docs/08-test-plan.md` to English and align the practice flow with current FlowForge conventions.

* **Scenario A:** Given a reader opens `docs/08-test-plan.md`, When they read the experiment guide, Then all narrative sections are in English and artifact paths use `.ai-work/{feature-slug}/` (not `openspec/changes/` as the primary location).
* **Scenario B:** Given the translated doc describes the five core skills test, When compared to `docs/14` and QUICKSTART, Then phase names, agent names (`forge-arch`, etc.), and CKP references are consistent.

### FR-002 — docs/03 English reference doc

Convert `docs/03-engram-dotnet-gaps.md` into an English reference that does not read as an open implementation backlog.

* **Scenario A:** Given a contributor reads `docs/03`, When they check gap sections marked completed, Then the doc clearly states those features are **implemented in engram-dotnet** and points to `docs/12` for MCP tools.
* **Scenario B:** Given the release gate checklist, When item 15 is audited, Then `docs/03` is listed as **Done (EN)** in `I18N.md` with no misleading “blocking gap” language.

### FR-003 — Tracker and roadmap sync

Keep i18n status authoritative in tracker docs.

* **Scenario A:** Given FR-001 and FR-002 are merged, When `I18N.md` is opened, Then **Partial** no longer lists `08` or `03` under required next steps (or marks them done with date).
* **Scenario B:** Given release gate table in `04-roadmap.md`, When item 15 minimum scope is done, Then **CKP-coherent docs** moves to ✅ or documents explicit accepted debt for optional `docs/15` only.

### FR-004 — Optional docs/15 polish (P2)

Reduce Spanish surface in `docs/15` without breaking the legacy catalog note.

* **Scenario A:** Given the team chooses the optional track, When Part 2 and Part 3 section headings are updated, Then headings are English while the legacy note in Part 1 remains.
* **Scenario B:** Given the team **defers** Part 1 tables, When `I18N.md` is updated, Then `docs/15` is marked “EN summary + runtime skills; Part 1 tables legacy (accepted)”.

## 3. Non-functional requirements (NFR)

- **NFR-001:** No personal machine paths (`/media/...`, `/home/...`) in edited files.
- **NFR-002:** Terminology lock: `verify-report.md`, `rework_ticket.md`, CKP-0→4, `.ai-work/{slug}/`.
- **NFR-003:** Translations preserve technical accuracy (pytest, MCP, skill names unchanged).
- **NFR-004:** Minimum scope (FR-001–003) completable in one focused session (~2–4 h).

## 4. Developer manual tests (required — mark [x] before /flow-close)

| ID | Case / flow | Steps (summary) | Expected result | [x] |
|----|-------------|-----------------|-----------------|-----|
| PM-1 | I18N tracker accuracy | 1. Open `docs/I18N.md`<br>2. Compare Partial/Next vs actual file language | `08` and `03` reflect completed work; no stale “translate next” for done files | [ ] |
| PM-2 | docs/08 path sanity | 1. Read translated `docs/08`<br>2. Search for `openspec/changes` | No primary artifact instructions using legacy openspec paths only; `.ai-work/` documented | [ ] |
| PM-3 | Release gate read-through | 1. Open `docs/04-roadmap.md` release gate<br>2. Confirm item 15 + public language | Status matches reality (✅ or explicit debt note for optional 15 Part 1) | [ ] |
| PM-4 | Cross-link smoke | 1. From README or QUICKSTART, follow links to `08`, `03`, `15`<br>2. Skim first screen | No jarring Spanish blocks in priority docs (08, 03); 15 optional debt documented if deferred | [ ] |

## 5. Acceptance summary (for CKP-1)

| Tier | Deliverables | Closes gate? |
|------|--------------|--------------|
| **Minimum (recommended)** | FR-001, FR-002, FR-003 | ✅ Item 15 + public language for priority docs |
| **Optional** | FR-004 | 🟡 Improves 15; not required if debt accepted |

## 6. Open decisions (human @ CKP-1)

1. **Resume minimum only** (08 + 03 + trackers) vs **include docs/15 Part 2–3 headers** in same epic?
2. Accept **legacy Spanish Part 1 tables** in `docs/15` as permanent catalog debt?

---

*Next step after approval:* `/flow-plan` → ordered checklist (08 first, then 03, then I18N/roadmap, optional 15).
