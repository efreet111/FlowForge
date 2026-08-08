---
title: "Bug: jq def func($var) doesn't bind variables as expected — inline the filter"
type: "bugfix"
topic_key: "architecture/model-config/bugs"
date: "2026-07-18"
scope: "team"
project: "flowforge"
significance: "medium"
---

## What

When writing the CI validator (`scripts/validate-agent-models.sh`), using `jq`'s `def` function with a variable parameter (`def check_model($model; $list): ...`) produced incorrect results. Variables passed as arguments to `def` don't bind to the outer scope as expected when called inside a complex pipeline.

## Why

jq's `def` creates a function that has its own scope. Variables passed by value can be shadowed by other bindings in nested contexts (e.g., inside `to_entries[] | ...`). The fix was to inline the entire filter logic rather than factor it into a `def`.

## Where

- `scripts/validate-agent-models.sh` — single jq expression with `[$pm[] | select(. == $obj.model)] | length == 0` inline pattern

## Learned

When writing jq validators, keep filters as inline expressions. `def` functions in jq behave differently than functions in most programming languages — they cannot reliably access parent-scope variables in nested pipelines. A single monolithic `jq -r '[...] | .[]'` expression is more predictable.
