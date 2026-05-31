---
capability_matrix:
  ai_reasoning:
    - Significancia de un Memory Signal cuando revision_cycle y rework_count son ambiguos
    - Wording del summary en el signal (una línea descriptiva, no mecánica)
    - Criterio de "similar existe en Engram" al hacer mem_search
  deterministic:
    - Solo forge-arch y forge-dev emiten Memory Signal — no otros agentes
    - El orquestador aplica los 3 pasos en orden; no puede saltarlos
    - mem_session_summary es obligatorio en /flow-close — no opcional
    - Si MCP no responde, el fallback es .engram/local_memory/ — no silencio
    - El agente emisor describe; nunca llama mem_save directamente
---

# Spec: Orchestrator Memory Curation Protocol (Item 20)

## 1. Objective and scope

**Objective:** Establecer un protocolo IDE-agnóstico y model-agnóstico que garantice
que el conocimiento relevante generado durante un flujo FlowForge se persiste en Engram
de forma consistente, sin sobrecargar a los subagentes con decisiones de memoria y sin
crear acoplamiento a un IDE específico.

**Contexto de origen:** Análisis del reporte offline-first del 2026-05-30. Las sesiones
se cerraban sin ningún save a Engram porque: (a) no había criterios claros de qué guardar,
(b) los agentes tenían responsabilidad directa de mem_save pero con lenguaje soft, y
(c) no había `mem_session_summary` obligatorio al cerrar. Ver
`[docs/decisions/ADR-001-memory-curation-protocol.md](../../docs/decisions/ADR-001-memory-curation-protocol.md)`.

**In scope:**

- Protocolo Memory Signal en `forge-arch` y `forge-dev` (handoff output)
- Memory Curation Protocol en `forge-orchestrator` (proceso de 3 pasos)
- `mem_session_summary` obligatorio en `forge-memory` al `/flow-close`
- Fallback a `.engram/local_memory/` cuando MCP no está disponible
- Propagación thin a los 4 IDE adapters
- Documentación del offline-first lifecycle en `docs/10`
- Config MCP binary path en `ia-work/context-project.md`

**Out of scope:**

- Modificar `forge-plan`, `forge-verify`, `forge-discovery` (no generan conocimiento nuevo)
- Implementar nuevas herramientas MCP en engram-dotnet
- Cambiar el proceso de sincronización con el servidor remoto
- Agregar Memory Signal a agentes que no sean forge-arch o forge-dev

## 2. Functional requirements (FR)

### FR-001 — Memory Signal en forge-arch

El agente forge-arch debe incluir un bloque `## Memory Signal` al final de su handoff.

- **Scenario A:** Dado que forge-arch completó un spec con revision_cycle = 0,
Cuando el orquestador lee el handoff,
Entonces el Memory Signal tiene `type: decision` y `significance: low` o `type: none`.
- **Scenario B:** Dado que forge-arch completó un spec que fue rechazado y revisado
(revision_cycle >= 1),
Cuando el orquestador lee el Memory Signal,
Entonces `significance: high` y el orquestador ejecuta los 3 pasos de curation.

### FR-002 — Memory Signal en forge-dev

El agente forge-dev reemplaza el mem_save directo por un Memory Signal en su handoff.

- **Scenario A:** Dado que forge-dev resolvió una implementación rutinaria sin rework,
Cuando completa el handoff,
Entonces el Memory Signal tiene `type: none` y el orquestador hace skip.
- **Scenario B:** Dado que forge-dev resolvió un bug que generó un rework_ticket
con cycle_count >= 2,
Cuando el orquestador lee el Memory Signal,
Entonces `type: bugfix`, `significance: high`, y el orquestador ejecuta los 3 pasos.

### FR-003 — Memory Curation Protocol en el orquestador

El orquestador aplica un proceso de 3 pasos al recibir un Memory Signal.

- **Scenario A:** Dado un signal con `type: none`,
Cuando el orquestador procesa el handoff,
Entonces no llama mem_save ni mem_search (skip inmediato).
- **Scenario B:** Dado un signal con `type: decision` y `significance: high`,
Cuando el orquestador ejecuta el paso 3 (mem_search) y no encuentra resultado similar,
Entonces llama `mem_save` con el contenido del signal.
- **Scenario C:** Dado un signal con `type: bugfix` y `significance: low`
pero `rework_count >= 2` en el rework_ticket,
Cuando el orquestador evalúa el paso 2 (fricción),
Entonces el criterio de rework_count eleva la prioridad y continúa al paso 3.
- **Scenario D:** Dado que MCP no responde al intentar mem_save,
Cuando el orquestador detecta el timeout,
Entonces escribe el contenido en `.engram/local_memory/obs-<timestamp>.md`
con YAML frontmatter completo.

### FR-004 — mem_session_summary obligatorio en /flow-close

forge-memory debe llamar `mem_session_summary` antes de reportar CKP-4.

- **Scenario A:** Dado que `/flow-close` fue invocado y forge-memory terminó su trabajo,
Cuando el orquestador recibe el output de forge-memory,
Entonces verifica que `mem_session_summary` fue llamado antes de emitir CKP-4.
- **Scenario B:** Dado que MCP no responde durante `/flow-close`,
Cuando forge-memory no puede llamar `mem_session_summary`,
Entonces escribe el resumen en `.engram/local_memory/obs-<timestamp>-session-close.md`.

## 3. Non-functional requirements (NFR)

- **NFR-001:** El protocolo debe ser IDE-agnóstico — la lógica vive en `skills/`,
no en reglas de un IDE específico.
- **NFR-002:** El protocolo debe ser model-agnóstico — no asume capacidades específicas
de ningún LLM.
- **NFR-003:** El Memory Signal tiene máximo 3 campos — no debe crecer en complejidad.
- **NFR-004:** El paso de mem_search (deduplicación) es no-bloqueante si MCP tarda;
en timeout se skippea y se guarda igual.
- **NFR-005:** El fallback a `.engram/local_memory/` es siempre disponible sin
dependencias externas.

## 4. Developer manual tests (required — mark [x] before /flow-close)


| ID   | Case / flow            | Steps (summary)                                                                                                 | Expected result                                                      | [x] |
| ---- | ---------------------- | --------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------- | --- |
| PM-1 | Signal en forge-arch   | Leer `ide/cursor/agents/forge-arch.md` — buscar `## Memory Signal` en output format                             | Bloque de 3 campos presente al final del handoff format              | [X] |
| PM-2 | Signal en forge-dev    | Leer `ide/cursor/agents/forge-dev.md` — verificar que no hay `mem_save` directo                                 | Memory Signal presente; sin llamada directa a mem_save               | [X] |
| PM-3 | 3 pasos en orquestador | Leer `ide/cursor/rules/workflow.mdc` y `skills/forge-orchestrator/SKILL.md` — buscar "Memory Curation Protocol" | Sección con los 3 pasos: tipo → fricción → dedup                     | [X] |
| PM-4 | Cross-IDE parity       | Leer `ide/antigravity/rules/workflow.md`, `ide/opencode/AGENTS.md`, `ide/vscode/copilot-instructions.md`        | Referencia al Memory Curation Protocol presente en los 3             | [X] |
| PM-5 | Fallback documentado   | Leer `docs/10-memory-mapping-fallback.md` — buscar sección "Offline-first lifecycle"                            | Sección presente con diagrama del flujo y nota IDE close ≠ auto-save | [X] |


## 5. Acceptance summary (para CKP-1)


| Tier         | Deliverables                       | Cierra gate?                     |
| ------------ | ---------------------------------- | -------------------------------- |
| **Mínimo**   | FR-001, FR-002, FR-003, FR-004     | ✅ Protocolo funcional end-to-end |
| **Completo** | Mínimo + propagación 4 IDEs + docs | ✅ Parity cross-IDE verificada    |


## 6. Open decisions (resueltas en análisis pre-spec)

1. **¿Todos los agentes o solo 2?** → Solo forge-arch y forge-dev. Resto no genera
  conocimiento nuevo persistible. Ver ADR-001.
2. **¿Orquestador centralizado o distribuido?** → Orquestador. Tiene contexto
  cross-fase (revision_cycle, rework_count) que los subagentes no tienen. Ver ADR-001.
3. **¿Global rule IDE-específica?** → No. Skills son la fuente de verdad;
  IDE adapters son propagación thin. Ver ADR-001.

---

*Spec derivada del análisis del 2026-05-30. CKP-1 aprobado implícitamente por confirmación
del diseño en la sesión de planning. Plan: `.ai-work/agent-proactive-memory/plan.md`.*