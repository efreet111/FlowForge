---
title: "Bug: git commit with long multi-line message can timeout"
type: "bugfix"
topic_key: "operations/git/timeout"
date: "2026-07-18"
scope: "team"
project: "flowforge"
significance: "medium"
---

## What

During the feature implementation, `git commit` with a long (300+ character) multi-line commit message would sometimes timeout or hang when the message was passed via `-m` flag with embedded newlines. The issue is that bash's argument parsing combined with `set -euo pipefail` causes unexpected behavior for very long inline commit messages.

## Why

Git expects commit messages to be passed either via editor or via stdin. Using `git commit -m "line1\nline2\nline3"` with shell-interpreted newlines can hit edge cases in argument length limits or shell parsing depending on the environment.

## Where

- General git workflow within `/flow-dev` — affecting commit operations during feature development.

## Learned

Always use `git commit` with a message file or `-F` flag for long messages. Alternatively, keep commit messages concise (under 72 chars per line, as per git convention) and use `-m` only for single-line messages. For multi-line, pipe through `git commit -F -` with a heredoc.
