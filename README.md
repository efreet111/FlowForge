# FlowForge

> **Forja tu flujo de desarrollo con agentes de IA.**

FlowForge es una **metodología Agentic SDLC** diseñada para equipos pequeños y medianos (SMB, 2-20 personas). Define cómo integrar agentes de IA en el ciclo de desarrollo de software con 4 checkpoints humanos, 7 agentes, y un protocolo de artefactos versionados.

## Repositorios

| Proyecto | Descripción |
|----------|-------------|
| **FlowForge** (este) | Metodología de desarrollo de software asistida por agentes |
| **[engram-dotnet](https://github.com/efreet111/engram-dotnet)** | Motor de memoria persistente para agentes de IA (.NET 10) |

## Metodología FlowForge

```
FASE 0: DISCOVERY     → Mapeo de épicas y engramas pasados (Haiku)
FASE 1: INTENCIÓN     → Checkpoint ① (Humano)  → spec.md (Sonnet)
FASE 2: ARQUITECTURA  → Checkpoint ② (Humano)  → plan.md (Sonnet)
FASE 3: EJECUCIÓN     → Inner Loop autónomo   ➔ código + tests (Sonnet/Flash)
FASE 4: CIERRE        → Checkpoint ③ (Humano)  → memoria + markdown (Haiku)
```

- **5 fases**, **4 checkpoints humanos + 1 deploy gate** (CKP-0 → CKP-4), **7 agentes** (Orchestrator, Discovery, Arch, Plan, Dev, Verify, Memory)
- **Orquestador AI nativo (Semáforo)** — inyectado mediante reglas de entorno (`.cursorrules`, `.clinerules`, etc.)
- **Model routing** óptimo por tipo de tarea (Sonnet para razonamiento denso, Haiku/Flash para lectura o clasificación barata)
- **Memory Janitor** — pruning automático con TTL configurable (por tipo: tool_use=30d, bugfix=90d, etc.)

## Estado de implementación

**engram-dotnet** — 4 features implementadas, **258 tests**:

| Feature | SDD | Tests |
|---------|-----|-------|
| ✅ verification-tools | Archivado | 16 + 52 MCP |
| ✅ promotion-level2 (.md) | Archivado | 17 + 139 Store |
| ✅ traceability (lineage) | Archivado | 28 + 52 MCP |
| ✅ ttl-configurable | Archivado | 22 + 139 Store |

## Documentación

| Documento | Descripción |
|-----------|-------------|
| [`01-engramflow-architecture.md`](docs/01-engramflow-architecture.md) | Diseño completo de la metodología |
| [`02-memory-strategy.md`](docs/02-memory-strategy.md) | Estrategia de 2 niveles de memoria |
| [`03-engram-dotnet-gaps.md`](docs/03-engram-dotnet-gaps.md) | Análisis de gaps con engram-dotnet |
| [`04-roadmap.md`](docs/04-roadmap.md) | Roadmap conjunto |
| [`05-comparison-methodologies.md`](docs/05-comparison-methodologies.md) | Investigación de metodologías Agentic SDLC |
| [`09-open-source-integration.md`](docs/09-open-source-integration.md) | Integración con Cursor, Cline y Estrategia Open Source |
| [`test-matrix.md`](docs/test-matrix.md) | 258 tests documentados |

## Licencia

MIT
