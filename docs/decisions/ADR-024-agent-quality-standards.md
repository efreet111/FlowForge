# ADR-024 — Estándares de calidad de agentes: lenguaje, self-containment y drift detection

> **Status**: Accepted
> **Date**: 2026-09-08
> **Feature**: `agent-quality-improvement` (HU-027 / NS-08)
> **Deciders**: FlowForge methodology team
> **Links**: [HU-027](../../docs/tasks/HU-001-HU-099/HU-027-ns-08-agent-quality-improvement.md) · [spec](../../.ai-work/agent-quality-improvement/spec.md) · [summary](../../.ai-work/agent-quality-improvement/summary.md) · [ADR-009](ADR-009-flowforge-sync-connect.md) · [ADR-019](ADR-019-opencode-antigravity-customizations.md)

---

## Contexto

La auditoría del 2026-07-18 detectó 50+ instancias de texto en español, 20+ gaps de paridad, 15+ protocolos faltantes y 5+ inconsistencias de naming en los agentes FlowForge distribuidos en 4 IDEs (Cursor, VS Code, OpenCode, Antigravity). Los agentes daban guía inconsistente, la cadena de trazabilidad FR/NFR estaba rota en VS Code, y los agentes de OpenCode (stubs de 19-30 líneas) eran no-funcionales fuera del repo FlowForge.

Tres decisiones arquitectónicas durables emergieron de esta feature. Ningún ADR existente las documenta: ADR-019 cubre rutas de instalación de OpenCode/Antigravity, y ADR-009 cubre por qué el installer no copia skills — pero no la política de lenguaje, ni la estrategia embed-inline, ni el patrón de drift detection.

## Decision drivers

- **Paridad cross-IDE**: un agente del mismo rol debe producir artefactos estructuralmente idénticos en los 4 IDEs (NFR-001).
- **Self-containment**: ningún agente puede requerir cargar archivos fuera de su propio paquete IDE (NFR-003) — los agents OpenCode no tienen acceso a `skills/` fuera del repo (ADR-009).
- **Mantenibilidad**: la duplicación de protocolos entre IDEs es inevitable (readability), pero el drift debe ser detectable mecánicamente (NFR-002).
- **Trazabilidad**: los IDs de requisitos FR/NFR deben ser consistentes de spec → tests → verify (FR-003).

## Decisiones

### Decisión 1 — Política de lenguaje: English-only para instrucciones de agentes

**Decisión:** Todo texto de instrucción operativa, descripciones YAML y plantillas de salida de agentes debe estar en inglés. Los triggers bilingües de intención natural de usuario (ej. `"reporté un error"`) se conservan únicamente en `ide/shared/workflow-orchestrator-parity.md`.

**Racional:** Las instrucciones las leen LLMs, no humanos — el lenguaje mixto confunde la interpretación de tokens. Los triggers bilingües son señales de entrada del usuario, no instrucciones de agente.

**Rechazado:** Traducción completa de todo incluyendo triggers de intención (rompería detección para usuarios hispanohablantes).

### Decisión 2 — Estrategia OpenCode: embed inline, no referenciar

**Decisión:** Los agentes OpenCode embeben instrucciones críticas inline (80-120 líneas), siguiendo el patrón de self-containment de Cursor. Las skills avanzadas (security, complexity, performance, a11y) se referencian por path con fallback graceful ("si el skill no existe, saltear el check especializado y anotarlo en el reporte").

**Racional:** El installer NO copia skills a `~/.config/opencode/skills/` (ADR-009). Un agente stub que dice "load skill on-demand" es no-funcional en cualquier proyecto ajeno al repo FlowForge. Embedding garantiza operación en cualquier workspace.

**Rechazado:** Option B del ADR-019 original (referencia relativa desde el repo) — funcionaba solo dentro del clone FlowForge.

### Decisión 3 — Gestión de duplicación: drift comments, no eliminación

**Decisión:** Los bloques de protocolo duplicados entre IDEs se marcan con comentarios HTML `<!-- sync: path/to/canonical -->` inmediatamente antes del bloque. Los protocolos largos (Memory Curation Protocol) se reemplazan por una referencia al archivo canónico `ide/shared/workflow-orchestrator-parity.md` en lugar de copiarse.

**Racional:** Self-containment exige que los orquestadores sean legibles sin cross-referencing (los CKP tables y verdicts permanecen inline). Pero el drift es real — 4 copias del CKP table ya habían divergido. Los comentarios `sync:` dan un objetivo grep mecánico (`rg '<!-- sync:'`) sin sacrificar legibilidad.

## Consecuencias

**Positivas:**

- Agentes self-contained y paritarios en 4 IDEs; OpenCode funcional fuera del repo.
- Cadena de trazabilidad FR/NFR restaurada en VS Code (spec → tests `[FR-XXX]` → verify).
- Cero español en instrucciones de agentes; triggers bilingües preservados para usuarios.
- Drift detectable por grep; protocolo largo centralizado en un solo archivo canónico.
- 24 archivos modificados (+719/−171), cero archivos protegidos de installer tocados (ADR-017).

**Negativas / aceptadas:**

- Los agentes OpenCode ahora duplican contenido inline de las SKILL.md — hay que mantener ambas fuentes sincronizadas manualmente (el `<!-- sync: -->` no es sincronización automática, solo detección).
- El ítem 1 del summary (VS Code orchestrator handoff aún dice "RF/RNF" en línea 15) queda como deuda menor fuera de alcance — fix en un futuro quality pass.

**Verificación:**

- PM-1..PM-5 ejecutados por el developer humano: 5/5 ✅
- `rg '[áéíóúñü]'` en agentes → cero matches (NFR-004)
- Todos los agentes OpenCode entre 81-117 líneas (NFR-005)
- `rg '<!-- sync:'` → presente en los 4 orquestadores + SKILL.md source
- Rework Cycle 2 confirmado vía `git diff HEAD` (lección: verificar fixes por diff, no por status del ticket)