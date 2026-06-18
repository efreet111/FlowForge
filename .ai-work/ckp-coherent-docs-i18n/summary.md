# Summary: CKP-coherent docs & public i18n (Item 15)

**Status:** ✅ CLOSED — CKP-4 gate unlocked

**Date:** 2026-06-01

**Tier delivered:** Minimum (FR-001, FR-002, FR-003). FR-004 (docs/15 polish) deferred as accepted debt.

---

## Goal

Cerrar los items parciales **CKP-coherent docs** y **Public language** del release gate, traduciendo la documentación prioritaria a inglés y actualizando los trackers de i18n, para que FlowForge esté publish-ready con docs en inglés sin bloques legacy en español en los archivos prioritarios.

## Discoveries

- Feature puramente documental — sin decisiones arquitectónicas, bugs, ni cambios de código.
- La verificación fue estática (análisis línea por línea) por no existir suite de tests para archivos `.md`.

## Accomplished

- ✅ **FR-001** — `docs/08-test-plan.md` traducido a inglés (185 líneas), paths normalizados a `.ai-work/{slug}/`, terminología CKP y agentes consistente con `docs/14` y QUICKSTART.
- ✅ **FR-002** — `docs/03-engram-dotnet-gaps.md` convertido a referencia en inglés con postura "implemented in engram-dotnet", apuntando a `docs/12` para el catálogo MCP. Sin lenguaje de "open gap" o "blocking".
- ✅ **FR-003** — `docs/I18N.md` actualizado: `docs/08` y `docs/03` marcados como Done (EN) con fecha. `docs/04-roadmap.md` actualizado: Item 15 marcado como ✅ con nota explícita de deuda aceptada para docs/15 Part 1.
- ✅ **FR-004** — Deferred según spec §5 (tier opcional). Deuda documentada en `docs/04-roadmap.md` línea 67.
- ✅ **PM-1 a PM-4** — Todos verificados y marcados [x] por el desarrollador humano.
- ✅ **Verify report** — PASS sin issues críticos ni warnings. 8/8 escenarios compliant.

## Next Steps

- **Opcional:** FR-004 (docs/15 Part 1 tables) si se decide abordar en un sprint futuro.
- **Opcional post-MVP:** Revisar skills especializadas que aún mencionan `openspec/changes/` si se encuentra durante spot-checks.
- La deuda por docs/15 Part 1 está documentada en `04-roadmap.md` línea 67 como "optional Part 1 tables in 15 deferred".

## Relevant Files

| File | Change | Lines |
|------|--------|-------|
| `docs/08-test-plan.md` | Traducción completa a inglés + path alignment | 185 |
| `docs/03-engram-dotnet-gaps.md` | Rewrite a inglés con postura "implemented features / reference only" | 176 |
| `docs/I18N.md` | Marcados 08 y 03 como Done (EN) con fecha | 45 |
| `docs/04-roadmap.md` | Item 15 → ✅, deuda docs/15 documentada | 182 |
