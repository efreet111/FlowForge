---
title: "VS Code RF/RNF vs FR/NFR Naming Inconsistency"
type: "bugfix"
topic_key: "bugs/naming/rf-rnf-vs-fr-nfr"
date: "2026-07-22"
scope: "team"
---

## What

VS Code agent forge-arch was generating requirements with `RF-001` (Functional Requirement) and `RNF-001` (Non-Functional Requirement) prefixes instead of the spec-standard `FR-001` and `NFR-001`.

## Why

This breaks traceability: forge-dev generates tests from FR-001 references, forge-verify cross-references against FR-001, and spec.md uses FR/NFR format everywhere. The VS Code agents were out of sync with the project convention.

## Where

- `ide/vscode/agents/forge-arch.agent.md` — spec generation prompt
- `ide/vscode/agents/forge-dev.agent.md` — test generation prompt
- `ide/vscode/agents/forge-discovery.agent.md` — discovery prompt

## Learned

1. The root cause was the VS Code agents being originally written in Spanish (where RF = Requisito Funcional, RNF = Requisito No Funcional)
2. When translating to English, the acronyms must also translate: RF→FR, RNF→NFR
3. Traceability chain: spec.md FR-001 → forge-dev tests `[FR-001]` → forge-verify cross-reference
4. Any deviation in naming breaks the automated verification pipeline
5. To detect: `rg 'RF-\d' ide/ skills/` should return zero matches in agent instruction blocks
