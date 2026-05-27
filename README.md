# FlowForge

> **Forja tu flujo de desarrollo con agentes de IA.**

FlowForge es una **metodología Agentic SDLC** diseñada para equipos pequeños y medianos (SMB, 2-20 personas). Define cómo integrar agentes de IA en el ciclo de desarrollo de software con 5 checkpoints formales, 7 agentes, 31 skills especializadas, y un protocolo de artefactos versionados.

## Repositorios

| Proyecto | Descripción |
|----------|-------------|
| **FlowForge** (este) | Metodología de desarrollo de software asistida por agentes |
| **[engram-dotnet](https://github.com/efreet111/engram-dotnet)** | Motor de memoria persistente para agentes de IA (.NET 10) |

## Metodología FlowForge

```
FASE 0: DISCOVERY ──── CKP-0 🔴 HARD STOP
FASE 1: INTENCIÓN ──── CKP-1 🟡 spec.md (humano aprueba)
FASE 2: ARQUITECTURA ─ CKP-2 🟡 plan.md (humano aprueba)
FASE 3: EJECUCIÓN ──── Inner Loop autónomo + CKP-3 🔴 (3 ciclos máx)
FASE 4: CIERRE ─────── CKP-4 🟢 deploy gate (humano decide)
```

- **5 fases**, **5 checkpoints (CKP-0 → CKP-4)**, **7 agentes** (Orchestrator, Discovery, Arch, Plan, Dev, Verify, Memory)
- **31 skills**: 7 core + 23 especializadas (seguridad, SOLID, performance, a11y, patrones, DDD, migraciones, métricas) + 1 teacher toggleable
- **Checkpoints con semántica de colores**: 🔴 binario e inapelable, 🟡 flexible con autoridad humana, 🟢 decisión de release
- **Orquestador AI nativo (Semáforo)** — inyectado mediante reglas de IDE
- **Model routing** óptimo por tipo de tarea
- **Memory Janitor** — pruning automático con TTL configurable

## Skills (31 total)

| OLA | Cantidad | Enfoque |
|-----|----------|---------|
| Core | 7 | Flujo base (orchestrator, discovery, arch, plan, dev, verify, memory) |
| OLA 1 | 5 | Seguridad + SOLID (STRIDE, OWASP, SAST) |
| OLA 2 | 5 | Calidad + Patrones (GoF, testing, performance, complejidad) |
| OLA 3 | 8 | Infraestructura (CVE, compliance, DDD, migraciones, rollback, refactor) |
| OLA 4 | 5 | Métricas (costos, a11y verify, project health, changelog, knowledge graph) |
| Cross | 1 | forge-teacher (modo profesor toggleable) |

## IDE Integration

Archivos listos para usar en `ide/`:

| IDE | Archivos |
|-----|----------|
| **OpenCode** | `opencode.flowforge.json` — 7 subagentes con `{file:...}` a skills |
| **Cursor** | `cursor/rules/*.mdc` + `cursor/agents/*.md` |
| **Antigravity** | `antigravity/rules/*.md` + `antigravity/workflows/*.md` |
| **VS Code** | `vscode/copilot-instructions.md` |

### Instalación rápida

```bash
# Linux/macOS
curl -sSL https://raw.githubusercontent.com/efreet111/FlowForge/main/ide/install.sh | bash

# Windows (PowerShell)
iex ((New-Object System.Net.WebClient).DownloadString('https://raw.githubusercontent.com/efreet111/FlowForge/main/ide/install.ps1'))
```

Después de instalar, reiniciá el IDE, seleccioná el agente `flowforge` y probá:
```
/flow-start CRUD de tareas — endpoints REST para crear, listar, actualizar y eliminar tareas
```

## Estado de implementación

**engram-dotnet** — 7 features implementadas, **258 tests**:

| Feature | SDD | Tests |
|---------|-----|-------|
| ✅ verification-tools | Archivado | 16 + 52 MCP |
| ✅ promotion-level2 (.md) | Archivado | 17 + 139 Store |
| ✅ traceability (lineage) | Archivado | 28 + 52 MCP |
| ✅ ttl-configurable | Archivado | 22 + 139 Store |
| ✅ doctor-diagnostic | Archivado | 27 tests |
| ✅ offline-first-sync | Archivado | 84 tests |
| ✅ advanced-engram-integration | Archivado | Doc & Skills |

## Documentación

| Documento | Descripción |
|-----------|-------------|
| [`01-engramflow-architecture.md`](docs/01-engramflow-architecture.md) | Diseño completo de la metodología |
| [`04-roadmap.md`](docs/04-roadmap.md) | Roadmap v0.3 con 4 OLAS |
| [`14-flowforge-complete-reference.md`](docs/14-flowforge-complete-reference.md) | Referencia completa + 7 casos de prueba |
| [`15-agent-skills-technical-spec.md`](docs/15-agent-skills-technical-spec.md) | Especificación técnica de agentes |
| [`16-ide-integration-plan.md`](docs/16-ide-integration-plan.md) | Integración con 4 IDEs |

## Licencia

MIT
