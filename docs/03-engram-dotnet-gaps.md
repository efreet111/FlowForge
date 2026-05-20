# EngramFlow — Gaps Analysis: engram-dotnet

> **Versión**: 0.2
> **Última actualización**: 2026-05-13
> **Repositorio analizado**: [efreet111/engram-dotnet](https://github.com/efreet111/engram-dotnet)
> **Ruta local**: `/media/gantz/300extra/Proyectos/engram-dotnet/`

---

## 1. Estado Actual de engram-dotnet

### Stack técnico

| Capa | Tecnología |
|------|-----------|
| Lenguaje | C# / .NET 10 LTS |
| HTTP | ASP.NET Core Minimal API (Kestrel) |
| Base de datos | SQLite via Microsoft.Data.Sqlite + FTS5 **o** PostgreSQL via Npgsql + tsvector |
| MCP | ModelContextProtocol NuGet (Microsoft oficial) |
| CLI | System.CommandLine |
| Auth | Microsoft.AspNetCore.Authentication.JwtBearer (opcional) |
| Tests | xUnit + WebApplicationFactory + Testcontainers.PostgreSql |

### Estructura del proyecto

```
engram-dotnet/
├── src/
│   ├── Engram.Store/        ← Motor central: IStore + 3 implementaciones
│   ├── Engram.Server/       ← HTTP REST API (ASP.NET Core)
│   ├── Engram.Mcp/          ← Servidor MCP (transporte stdio, 15 herramientas)
│   ├── Engram.Sync/         ← Sync git-friendly (gzip + JSONL)
│   └── Engram.Cli/          ← Entry point CLI + DI wiring
├── tests/
│   ├── Engram.Store.Tests/       ← 110 tests
│   ├── Engram.Postgres.Tests/    ← 26 tests
│   ├── Engram.Server.Tests/      ← 19 tests
│   ├── Engram.Mcp.Tests/         ← 34 tests
│   └── Engram.HttpStore.Tests/   ← 30 tests
└── config/                      ← MCP configs para Cursor, VS Code
```

### Lo que ya funciona ✅

| Componente | Estado |
|-----------|--------|
| IStore con 22 métodos | ✅ Implementado con Strategy Pattern (SqliteStore, PostgresStore, HttpStore) |
| Deduplicación por hash (15 min window) | ✅ Funcional |
| topic_key upsert (temas evolutivos) | ✅ Funcional |
| Sesiones con inicio/fin/summary | ✅ Funcional |
| Búsqueda FTS5 (title, content, type, project, topic_key) | ✅ Funcional |
| namespacing multi-usuario (X-Engram-User) | ✅ Implementado en server |
| Export/Import JSON | ✅ Funcional |
| Obsidian Export | ✅ Funcional |
| CLI: search, save, context, stats, export, import | ✅ Funcional |
| Offline-First Sync (PR #14) | ⏳ En progreso |
| Schema con expires_at, review_after, embedding | ✅ Columnas existen pero no se usan |

---

## 2. Matriz de Gaps vs EngramFlow

### GAP 1: TTL + Pruning (Fase 1)

| Item | Estado en engram-dotnet | Lo que EngramFlow necesita |
|------|------------------------|---------------------------|
| `PruneOldObservationsAsync` | ✅ Implementado | Método en IStore que marca observaciones como deleted por TTL |
| TTL configurable por tipo | ✅ Implementado (`sdd/ttl-configurable/`) | Env vars `ENGRAM_TTL_TOOL_USE=30d`, etc. |
| Retention Stats endpoint | ✅ Implementado | `GET /retention/stats` + CLI + MCP tool |
| `mem_retention_prune` tool | ✅ Implementado | MCP tool para pruning manual o batch |
| `mem_retention_stats` tool | ✅ Implementado | MCP tool con distribución por edad |
| Observaciones con topic_key preservadas | ✅ Implementado | Lógica en PruneOldObservationsAsync: saltar si topic_key != null |
| Diario automático (cron) | ✅ Implementado | Servicio background o cron externo |

**¿Qué hay que tocar?**
- `IStore.cs` → nuevos métodos: `PruneOldObservationsAsync`, `GetRetentionStatsAsync`
- `SqliteStore.cs` + `PostgresStore.cs` → implementación de pruning
- `HttpStore.cs` → proxy de los nuevos endpoints
- `Models.cs` → `RetentionStats`, `AgeBucket`, `InactiveProject`
- `EngramServer.cs` → `GET /retention/stats`
- `Engram.Cli/Program.cs` → `engram retention check`, `engram retention prune`
- `Engram.Mcp/EngramTools.cs` → `mem_retention_stats`, `mem_retention_prune`
- `Engram.Store/RetentionConfig.cs` → nuevo archivo, parsing de TTL desde env vars

**Esfuerzo estimado**: 7-10h (Capa 1: 2-3h + Capa 2: 3-4h + integración: 2-3h)

---

### GAP 2: Promoción a Nivel 2 — .md (Fase 2)

| Item | Estado | Lo que EngramFlow necesita |
|------|--------|---------------------------|
| Campo `md_path` en Observation | ❌ No existe | Nuevo campo opcional que linkea observation → .md file |
| Template engine para .md | ❌ No existe | Generador de archivos .md estructurados desde observaciones |
| `mem_promote_to_md` tool | ❌ No existe | Tool MCP que: (1) genera .md, (2) lo escribe en repo, (3) actualiza observation.md_path |
| `mem_sync_md_to_repo` | ❌ No existe | Para estado inicial: escanea observaciones existentes y genera .md |
| Bidirectional link | ❌ No existe | observation.md_path → .md, .md frontmatter → observation_id |
| Índice de decisiones | ❌ No existe | `docs/decisions/index.md` autogenerado |
| `CLAUDE.md` / `AGENTS.md` updater | ❌ No existe | Actualización de archivos de "constitución" del proyecto |

**¿Qué hay que tocar?**
- `Models.cs` → nuevo campo opcional `md_path` en Observation
- `AddObservationParams` / `UpdateObservationParams` → nuevos campos
- `EngramTools.cs` → `mem_promote_to_md`
- Nuevo: `Engram.MdGeneration/` → template engine + writer
- Nuevo: `Engram.MdPromotion/` → lógica de promoción con Haiku
- Server endpoints para promoción batch
- CLI: `engram promote --id 42` o `engram promote --batch`

**Esfuerzo estimado**: 7-9h

---

### GAP 3: Verification Tools (Fase 3)

| Item | Estado | Lo que EngramFlow necesita |
|------|--------|---------------------------|
| `mem_verify_artifact` tool | ❌ No existe | Toma spec.md + plan.md + cambios → verifica compliance |
| `mem_traceability` tool | ❌ No existe | Verifica RF/RNF listados en spec.md vs código generado |
| Rework ticket protocol | ❌ No existe | Formato canónico de `rework_ticket.md` + cycle_count |
| Verification report format | ❌ No existe | Output estructurado del Verify Agent |

**¿Qué hay que tocar?**
- `EngramTools.cs` → `mem_verify_artifact`, `mem_traceability`
- Nuevo: `Engram.Verification/` → lógica de verificación contra spec.md
- No toca IStore (es lógica de aplicación, no de persistencia)
- Puede vivir en un proyecto separado dentro de engram-dotnet o como tool externa

**Esfuerzo estimado**: 8-10h

---

### GAP 4: Memory Janitor (transversal)

| Item | Estado | Lo que EngramFlow necesita |
|------|--------|---------------------------|
| Pruning automático diario | ❌ No existe | Cron + script o BackgroundService |
| Promoción semanal batch | ❌ No existe | Batch con Haiku que escanea observaciones próximas a vencer |
| Deduplicación avanzada | ⚠️ Existe hash window (15 min) | Necesita consolidación de duplicados viejos |

**Nota**: El Memory Janitor **no necesita ser un agente**. Es:
- Un cron (`engram retention prune --apply`)
- Un script batch semanal que usa Haiku para clasificar
- BackgroundService opcional dentro de engram-dotnet

**Esfuerzo estimado**: 3-4h

---

### Lo que NO toca engram-dotnet

| Feature | Responsabilidad | Por qué no toca engram-dotnet |
|---------|----------------|------------------------------|
| Orquestador AI opcional | EngramFlow | Es configuración JSON + lógica de ruteo, no persistencia |
| Model routing | EngramFlow + MCP | El dispatcher vive en el agente o en un MCP proxy |
| Workflow runner (Makefile/bash) | EngramFlow | Orquesta las 4 fases con artefactos |
| Checkpoints humanos | EngramFlow | Son procesos humanos, no tools |
| spec.md / plan.md / rework_ticket.md format | EngramFlow | Son formatos de archivo, no responsabilidad de engram |

---

## 3. Plan de Implementación

### ✅ Completado: Verification Tools + Memory Level 2 + Traceability

**Verification Tools** (13 tasks) — `Engram.Verification/`
- `mem_verify_artifact`: spec.md + diff → compliance report con cycle tracking
- `mem_traceability`: matriz de trazabilidad RF/RNF con source validity
- SpecParser: bilingüe EN/ES con detección de secciones canónicas
- CycleTracker: persistencia con topic_key y escalation a humano

**Memory Level 2 — .md Promotion** (21 tasks) — `Engram.MdGeneration/`
- `mem_promote_to_md`: observación → .md con frontmatter YAML
- `mem_sync_md_to_repo`: batch sync con dry-run
- Link bidireccional: observation.md_path ↔ .md frontmatter
- CLI: `engram promote --id <n>` o `--sync --dry-run`

**Requirement Traceability** (18 tasks) — `Engram.Verification/`
- `mem_trace_source`: origen de RF/RNF con Source/Author/Rationale
- `mem_lineage`: BFS lineage tree con cycle detection (max 10 hops)
- TraceRepository: persistencia con topic_key `trace/{project}/{rf-id}`
- SpecParser extendido: sección `## Traceability` en spec.md

### ✅ Completado: TTL + Pruning
**Objetivo lograd**: engram-dotnet ya puede expirar observaciones viejas automáticamente, las tools de retención y la configuración TTL están implementadas y verificadas.

1. Implementar `PruneOldObservationsAsync` en IStore + SqliteStore + PostgresStore
2. Agregar `RetentionConfig.cs` para parsing de TTL desde env vars
3. Implementar `GetRetentionStatsAsync` (age buckets, proyectos inactivos)
4. Agregar endpoints HTTP: `GET /retention/stats`
5. Agregar CLI: `engram retention check`, `engram retention prune`
6. Agregar MCP tools: `mem_retention_stats`, `mem_retention_prune`
7. Implementar HttpStore proxy para los nuevos métodos
8. Memory Janitor v1: cron script que ejecuta prune diario

### Fase 2: Promoción a Nivel 2 — .md (7-9h)
**Objetivo**: Que las observaciones importantes puedan promoverse a .md estructurados.

1. Agregar campo `md_path` a Observation model
2. Diseñar template engine para .md (ADR template + frontmatter)
3. Implementar `mem_promote_to_md` tool MCP
4. Implementar link bidireccional: observation.md_path ↔ .md frontmatter
5. Implementar `mem_sync_md_to_repo` para estado inicial
6. Generación de `docs/decisions/index.md`

### Fase 3: Verification Tools (8-10h)
**Objetivo**: Darle al Verify Agent las herramientas para validar spec compliance.

1. Implementar `mem_verify_artifact`: spec.md + plan.md + code → structured report
2. Implementar `mem_traceability`: RF/RNF list vs code coverage
3. Definir formato canónico de `rework_ticket.md`
4. Implementar cycle_count tracking automático

**Total estimado**: ~22-29h
