# Doctor Diagnostic — SDD

## ¿Qué es?

Herramienta CLI (`engram doctor`) y MCP tool (`mem_doctor`) que verifica el estado operativo del ecosistema `engram-dotnet`: conectividad a DB (SQLite/PostgreSQL), disponibilidad del HTTP Server, y estado del MCP Server.

Diagnóstico **read-only** — no muta datos.

## Estado Actual

| Fase | Estado |
|------|--------|
| Exploration | ✅ |
| Proposal | ✅ |
| Spec | ✅ |
| Design | ✅ |
| Tasks | ✅ (14 tasks, 4 fases) |
| Apply | ⬜ Pendiente |
| Verify | ⬜ Pendiente |
| Archive | ⬜ Pendiente |

**Estimación de implementación**: 4-6h

## Documentos

| Documento | Descripción |
|-----------|-------------|
| [Exploration](./exploration.md) | Investigación inicial y alternativas |
| [Proposal](./proposal.md) | Intención, alcance, criterios de éxito |
| [Spec](./specs/doctor-diagnostic/spec.md) | Requerimientos detallados con escenarios |
| [Design](./design.md) | Decisiones arquitectónicas, data flow, interfaces |
| [Tasks](./tasks.md) | Breakdown en 14 tasks (4 fases) |
| [Apply Progress](./apply-progress.md) | Tracking de implementación |
| [Verify Report](./verify-report.md) | Resultados de verificación |

## Cómo Contribuir

1. Leer [Design](./design.md) para entender las decisiones arquitectónicas
2. Revisar [Tasks](./tasks.md) para ver el breakdown de implementación
3. Seguir [Apply Progress](./apply-progress.md) para saber qué está en progreso
4. Ejecutar `engram doctor` después de implementar y reportar resultados en [Verify Report](./verify-report.md)

## Dependencias

- **Ninguna** — usa infraestructura existente (`IStore`, `HttpClient`, MCP bindings)

## Stack

- .NET (class library: `Engram.Diagnostics`)
- CLI integration via `Engram.Cli`
- MCP integration via `Engram.Mcp`
- Tests via xUnit + Moq
