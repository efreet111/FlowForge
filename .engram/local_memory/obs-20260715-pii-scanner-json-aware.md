---
title: "PiiScanner JSON-aware home path detection"
type: bugfix
topic_key: installer/pii-scanner-json
date: 2026-07-15
scope: team
project: flowforge
---

## What

PiiScanner parsea template como JSON y escanea valores string para `/home/<user>/`; whitelist placeholders CI. 8/8 unit tests PASS.

## Why

PM-5 FAIL: regex `[=:\s]\s*/home/` no matcheaba `"pii_test": "/home/victor/secret"`.

## Where

`src/FlowForge.Installer/Modules/OpenCode/PiiScanner.cs`, `tests/FlowForge.Installer.Tests/PiiScannerTests.cs`

## Learned

Opción B (JSON-aware) > regex ampliada; iterar JsonObject con `foreach (var child in obj)`.
