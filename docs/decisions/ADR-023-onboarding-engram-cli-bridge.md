# ADR-023 — `flowforge onboard`: subprocess bridge al CLI de engram + formatos duales Go/.NET

> **Status**: Accepted
> **Date**: 2026-09-08
> **Feature**: `hu-029-onboarding-flow` (FF-003)
> **Deciders**: FlowForge methodology team
> **Links**: [HU-029](../../docs/tasks/HU-001-HU-099/HU-029-ff-003-onboarding-flow.md) · [spec](../../.ai-work/hu-029-onboarding-flow/spec.md) · [plan](../../.ai-work/hu-029-onboarding-flow/plan.md) · [verify-report](../../.ai-work/hu-029-onboarding-flow/verify-report.md) · [ADR-001](ADR-001-memory-curation-protocol.md) · [ADR-004](ADR-004-flowdoc-integration.md)

---

## Contexto

Un nuevo miembro del equipo necesitaba un comando único (`flowforge onboard`) que detecte el proyecto actual, recupere memorias relevantes de engram y muestre un briefing de 5 minutos con 3+ decisiones pasadas antes de escribir código. El `flowforge` CLI es un binario .NET AOT (`src/FlowForge.Installer`) con restricción dura de no agregar dependencias (ADR-001, NFR-002 del spec).

El problema técnico central: **cómo integrar el CLI de FlowForge con la base de memoria engram sin romper AOT y sin duplicar la lógica de acceso a datos**. Dos binarios engram coexisten (Go ≥1.20 y .NET ≥1.3) con **formatos de texto de salida distintos** para `context` y `search`. La integración debía ser tolerante a ambos y al banner "Update available" del binario Go.

## Decision drivers

- **AOT-safe (ADR-001)**: cero dependencias nuevas; solo `System.Diagnostics.Process`, `System.Text.Json` (source-gen / `JsonNode`) y `Spectre.Console` ya referenciado. Nada de `Spectre.Console.Cli`, nada de librerías de parsing externas.
- **Un solo punto de verdad**: el binario engram instalado (`PathHelper.EngramBinary`) es la fuente de datos — nunca PATH lookup, nunca re-implementación de la DB engram en C#.
- **Robustez ante drift de formato**: el parser debe tolerar banner, `Found N memories:`, `No memories found for: "<q>"`, líneas en blanco, truncación con elipsis `…`, y el formato `[N] #ID (type) — title` con línea de fecha al pie.
- **Seguridad**: `--project`/`--output`/`--user` pasados como argv (`ProcessStartInfo.ArgumentList`), no interpolados en shell (NFR-006).
- **Testabilidad**: parser como función pura `string → IReadOnlyList<T>` con fixtures capturados, sin requerir binario engram en CI (NFR-005).

## Options considered

### Option A — Llamar a la API/DB de engram directamente desde C#

**Pros**: Sin subproceso; acceso tipado.
**Cons**: Duplica el modelo de datos de engram, acopla versiones, rompe AOT si se agrega un cliente HTTP/ORM. **Rejected** — viola ADR-001 y el principio de un solo punto de verdad.

### Option B — Subprocess bridge al CLI de engram con parser tolerante (ELEGIDA)

Invoca `PathHelper.EngramBinary` con `ArgumentList` y un query set fijo (`context <p>` + `search "<kw>" --type decision --project <p> --limit 10` + `search "<kw>" --type pattern --project <p> --limit 10`), en paralelo (`Task.WhenAll`, timeout 15s por subproceso), y parsea stdout.

**Pros**: Cero deps nuevas, un solo punto de verdad, compatible con ambos binarios (Go y .NET), el CLI engram ya está verificado como bridge (session de discovery HU-029).
**Cons**: Acoplamiento al formato de texto (mitigado con parser estricto + fixtures), overhead de subproceso (aceptable: ≤10s wall, NFR-001). **Accepted.**

### Option C — Query set completo con `mem_timeline` y `convention`

**Pros**: Briefing más rico.
**Cons**: `convention` no es un tipo real de engram (verificado: `--type convention` devuelve 0) y `mem_timeline` requiere `obs_id` por decisión. **Deferred** (OQ-5, OQ-7) — fuera de alcance v1.

## Decision

1. **Subprocess bridge**: `EngramSubprocess` invoca `PathHelper.EngramBinary` (nunca PATH) con `ProcessStartInfo.ArgumentList` (sin interpolación shell), captura stdout/stderr separados, timeout 15s por subproceso, ejecuta los 3 queries en paralelo. Sigue el patrón `HealthCheckRunner.CheckBinaryAsync` existente.
2. **Parser tolerante a formatos duales**: `EngramOutputParser` es una función pura que (a) descarta banner `Update available*` / `Please update:*`, (b) descarta `Found N memories` / `No memories found*`, (c) ignora líneas en blanco y elipsis de truncación, (d) parsea el formato real `[N] #ID (type) — title` con línea de fecha al pie (verificado contra Go 1.20+ y .NET 1.3+). Fixture-based tests, sin binario en CI.
3. **Detección determinística de proyecto** (`ProjectDetector`): `--project` → `.flowforge.json` `engram.project` → top-level `project` → git remote basename → `basename(CWD)`. El nombre resuelto se muestra en el header del briefing.
4. **Interfaz interactiva sin markup conflictivo**: el menú usa `SelectionPrompt<T>` de Spectre.Console, pero las listas de decisiones/patrones/sesiones se imprimen con **`Console.WriteLine` plano** (no `AnsiConsole` markup) porque los títulos engram contienen corchetes (`[N] #ID`) y `**bold**` markdown que Spectre interpreta como su propio markup y lanza "malformed markup tag". `Markup.Escape()` se usa donde Spectre sí renderiza.
5. **ONBOARDING.md en la raíz del proyecto** (nunca `.ai-work/`, reafirma ADR-004): `MarkdownExporter.ResolveOutputPath` lanza `InvalidOperationException` si el path resuelve dentro de `.ai-work/`; default = raíz del proyecto.
6. **Salida determinística de errores**: exit 0 = éxito (incl. empty-memory con mensaje amigable), exit 1 = no-engram/error con instrucciones `flowforge install`. Ctrl+C = exit 0 sin stack trace.

## Consecuencias

**Positivas:**

- Nuevo comando `flowforge onboard` funcional con drill-down interactivo (3+ decisiones, sesiones, patrones, export MD).
- Compatibilidad verificada con ambos binarios engram (Go y .NET) vía Docker dual (`Dockerfile.pm-tests`).
- 43/43 tests unitarios de onboarding verdes; PM-1..PM-12 verificados por el developer.
- Cero dependencias nuevas; AOT intacto; parser reutilizable para futuras features CLI↔engram.

**Negativas / aceptadas:**

- F-1 (advisory, verify-report §9): `InteractiveMenuTests` solo cubre construcción/DTOs, no transiciones de estado máquina — cubierto por PM-* manuales (límite de alcance reconocido en plan §4 Phase 5).
- F-2 (advisory): keyword de búsqueda hard-coded a `"project"` vs sugerencia triple del plan (`project`+`onboarding`+`architecture`) — clasificado como `ai_reasoning` en la capability matrix del spec, delegado; spec-compliant.
- Los 8 tests pre-existentes de la suite completa siguen fallando (rutas de infra + red flaky) — no relacionados con HU-029.

**Riesgos:**

- **Riesgo**: drift futuro del formato de texto engram → mitigado por parser estricto + fixtures; un cambio de formato romperá tests antes que producción.
- **Riesgo**: el CLI engram cambia nombres de subcomandos → mitigado por el query set fijo documentado en spec §0 (determinístico).

## Status History

| Date | Change |
|------|--------|
| 2026-09-08 | Accepted — subprocess bridge al CLI de engram + parser de formatos duales para `flowforge onboard` (HU-029). Feature cerrada con PM-1..PM-12 verificados. |