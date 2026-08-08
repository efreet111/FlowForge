---
title: "CHANGELOG merge conflict resolution — keep both entries when branches append to [Unreleased]"
type: "pattern"
topic_key: "git/changelog-merge-conflict"
date: "2026-08-07"
scope: "team"
project: "team/flowforge"
---

## What
When two feature branches both append entries to `CHANGELOG.md` under `[Unreleased] → Added`, the merge conflict must be resolved by **keeping BOTH entries**, in any order, rather than dropping one. Applied in the NS-10 (Test Quality Gates) merge: the NS-09 (Executive Summary) entry and the NS-10 (Test Quality Gates) entry both survived under `[Unreleased] → Added` after manual conflict resolution.

## Why
Each feature's changelog record is a durable artifact: it documents shipped work for users and future audits. Dropping one entry during conflict resolution silently erases a feature from the release notes — a false-close-class bug in documentation. The cost of keeping both is negligible; the cost of dropping one is a permanent, hard-to-detect gap in the changelog.

## Where
- `CHANGELOG.md` — resolved keeping both NS-09 and NS-10 entries (commit `775c678`, PR #22).

## Learned
1. **Resolution rule**: for `[Unreleased] → Added` conflicts, concatenate — keep both entries, do not overwrite.
2. **Reduce conflict surface with pre-merge integration**: merge `main` into the feature branch *before* opening the final PR (`9812eb4 Merge branch 'main' into feat/test-quality-gates`). This moved the CHANGELOG conflict into the feature branch and left the final PR merge conflict-light (1 file instead of several).
3. **Sanity check after resolution**: `git diff` on the merged file and confirm both feature headings are present before committing the merge.
