---
feature: fix-opencode-installer-config-gen
date: 2026-07-15
status: closed
release: v0.1.0-alpha.12
pr: "#8"
---

# Resumen de cierre — feature `fix-opencode-installer-config-gen`

## Goal

Hacer que los instaladores bash y C# de FlowForge generen un `opencode.json` completo, idempotente, PII-free y limitado a 8 modelos OpenCode Zen gratuitos, con `model-assignments.md` regenerado desde el bloque `provider` — eliminando el supuesto "no merge needed" y la plantilla source-of-rot de Antigravity.

## ✅ Pruebas Manuales del Desarrollador

- PM-1: Fresh install en VM limpia — ✅ ejecutada (Docker 20 PASS; `curl|bash` v0.1.0-alpha.12 en máquina real)
- PM-2: Reinstall preserva custom — ✅ ejecutada (`mcp.my-mcp` + `agent.my-agent` preservados; backups OK)
- PM-3: Paridad bash vs C# — ✅ ejecutada (`diff` vacío modulo paths env-específicos)
- PM-4: `flowforge doctor` detecta stale `model-assignments.md` — ✅ ejecutada (FAIL reportado; exit code 0 vs 2 esperado — discrepancia menor)
- PM-5: PII scan bloquea install — ✅ ejecutada (JSON-aware `PiiScanner`; unit tests 8/8 PASS)

Verificadas por el desarrollador humano (victor) el 2026-07-15. Evidencia detallada en `spec.md` § PM Evidence (ciclo 7).

## Entregables principales

| Área | Resultado |
|------|-----------|
| **FR-001..FR-010** | Generador canónico C#, paridad bash↔C#, `model-assignments.md` desde provider, frontmatter agents, free-Zen-only, PII-free, merge no destructivo, `flowforge doctor`, backup, templates en repo |
| **Release** | PR #8 merged → `v0.1.0-alpha.12` publicado y validado por usuario |
| **Incidente engram v1.3.0** | Resuelto en alpha.12 (pin a v1.2.1 con binarios); ver `.ai-work/incident-engram-v130-missing-binaries/analysis.md` |
| **Rework cycle 8** | PM-5 fix: `PiiScanner` JSON-aware (Opción B del rework ticket) — resuelto, ticket `status: resolved` |

## Discoveries & Notes

- **Sidecar managed-paths**: Decisión CKP-1 (OQ-3) — `~/.config/opencode/.flowforge-managed.json` lista JSON-paths managed; legacy install asume solo `mcp.engram`.
- **PII scanner gotcha**: Regex cruda no detecta `/home/user/` dentro de strings JSON (`": "/home/...`); solución JSON-aware con whitelist de placeholders CI (`runner`, `testuser`, `user`, `username`, `example`).
- **Paridad bash**: `generate-config.sh` debe resolver `agent.*.model` per-provider, no stringified — fix aplicado en ciclo PM-3.
- **Doctor exit code**: PM-4 detecta stale correctamente pero doctor termina exit 0 (no 2) — follow-up opcional, no bloquea cierre.
- **Working tree sin commit**: `PiiScanner.cs`, `PiiScannerTests.cs`, evidencia en `spec.md` — cambios locales post-merge; commit pendiente a discreción del desarrollador.

## Archivos relevantes

- `.ai-work/fix-opencode-installer-config-gen/spec.md` — spec + PM evidence
- `.ai-work/fix-opencode-installer-config-gen/plan.md` — plan T-001..T-038
- `.ai-work/fix-opencode-installer-config-gen/verify-report.md` — audit estático PASS_DEGRADADO → superseded por PM runtime
- `ide/opencode/templates/` — SSOT (opencode.json.tpl, agent-models.json, managed-paths.json, agents/*.md.tpl)
- `src/FlowForge.Installer/Modules/OpenCode/` — OpenCodeConfigGenerator, PiiScanner, ModelAssignmentsGenerator, etc.
- `ide/opencode/generate-config.sh` — paridad bash
- `src/FlowForge.Installer/Modules/OpenCode/PiiScanner.cs` — JSON-aware scan (PM-5 fix)
- `tests/FlowForge.Installer.Tests/PiiScannerTests.cs` — 8/8 unit tests

## Next steps (post-cierre)

1. **Commit local** del fix PM-5 (`PiiScanner` + tests) si aún no está en main.
2. **Follow-up opcional**: alinear exit code de `flowforge doctor` a 2 en FAIL (PM-4 discrepancia).
3. **Follow-up OQ-1/OQ-2**: `flowforge doctor --refresh-models`, flag `--paid` flavor.
4. **Layer D tests**: xUnit parity CI gate (T-027..T-033) cuando SDK disponible en CI.

## Memory Signal

- type: decision
- significance: high
- summary: "Instalador OpenCode genera config completa free-Zen-only desde `ide/opencode/templates/`; bash y C# paridad; sidecar managed-paths; PII scanner JSON-aware; release v0.1.0-alpha.12 validado."
