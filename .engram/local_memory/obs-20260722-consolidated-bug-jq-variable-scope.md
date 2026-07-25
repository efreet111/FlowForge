---
title: "[CONSOLIDATED] jq variable scope: def func($var) does not localize"
type: bugfix
topic_key: "bugs/jq-variable-scope"
date: "2026-07-22"
scope: team
consolidates:
  - "obs-20260718-bug-jq-variable-scope.md"
  - "obs-20260722-bug-jq-variable-scope.md"
---

## What

Using `jq`'s `def` function with variable parameters (`def check_model($model; $list): ...`) produces incorrect results because jq's `def` does NOT properly scope variable parameters. Variables passed as arguments to `def` leak into the outer scope and can be shadowed by other bindings in nested contexts (e.g., inside `to_entries[] | ...`).

This was discovered when writing the CI validator (`scripts/validate-agent-models.sh`), where a `def` was used to factor out model reference validation logic.

## Why

jq's function model is unconventional: parameters declared with `$` are not truly local to the function body. When multiple functions use the same parameter names, or when the function is called inside a complex pipeline with nested bindings, jq resolves the variable reference to the wrong scope.

## Where

- `scripts/validate-agent-models.sh` (fixed in commit `2637562`)
- Fix: Use inline filter logic (`select(.model | IN(...))`) instead of `def func($var)`

## Learned

1. When writing jq validators, keep filters as inline expressions — avoid `def` with parameters
2. A single monolithic `jq -r '[...] | .[]'` expression is more predictable than factored `def` functions
3. jq's `def` is NOT a proper function scoping mechanism — parameters behave as global variables
4. If you must use `def`, avoid parameter names that could collide with input object keys
5. Use inline `select` + `map` pipelines instead of named functions with parameters
6. Test pattern: `jq -n 'def f($x): $x; f(1) as $x | f(2)'` — the `$x` binding leaks, producing unexpected results
