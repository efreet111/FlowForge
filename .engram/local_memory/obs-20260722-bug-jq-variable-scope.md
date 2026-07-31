---
title: "jq variable scope: def func($var) doesn't localize"
type: bugfix
topic_key: "bugs/jq-variable-scope"
date: "2026-07-18"
scope: team
---

## What
In jq, defining a function with `def func($var): ...` does NOT scope the variable to the function body. The `$var` leaks into the outer scope, causing unexpected behavior when multiple functions use the same parameter name.

## Why
The CI validator script (`scripts/validate-agent-models.sh`) used `def` to factor out model reference validation. Parameters passed via `def func($var)` collided with other functions' parameters.

## Where
- `scripts/validate-agent-models.sh` (fixed in commit `2637562`)
- Fix: Use inline filter logic (`select(.model | IN(...))`) instead of `def func($var)`

## Learned
- jq's `def` is NOT a proper function scoping mechanism — parameters are global
- Use inline `select` + `map` pipelines instead of named functions with parameters
- If you must use `def`, avoid parameter names that could collide with input object keys
