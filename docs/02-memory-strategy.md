# EngramFlow — Estrategia de Memoria (2 Niveles)

> **Versión**: 0.2
> **Última actualización**: 2026-05-13
> **Depende de**: engram-dotnet (motor de persistencia)

---

## 1. El Problema

Las memorias de agentes de IA tienen un problema fundamental: **todo parece importante en el momento**.

Un agente que debuggea un error raro durante 2 horas va a guardar 20 observaciones sobre ese error. A la semana siguiente, el error está resuelto y esas 20 observaciones son ruido. Pero siguen ocupando espacio y contaminando búsquedas.

Si no hay un mecanismo de limpieza, después de 3 meses de desarrollo tienes:

- 2000+ observaciones
- 40% es ruido (debugging temporal, experimentos fallidos, comandos efímeros)
- El FTS5 empieza a devolver resultados irrelevantes
- El agente pierde tiempo leyendo contexto basura

---

## 2. La Solución: Dos Niveles de Memoria

```
┌──────────────────────────────────────────────────────────────┐
│                   ESTRATIFICACIÓN DE MEMORIA                   │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  NIVEL 1: OPERATIVA (automática, efímera)                    │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ • Qué contiene: sesiones, prompts, outputs intermedios, │  │
│  │   debugging, tool_use, file_change, command              │  │
│  │ • Dónde: engram-dotnet DB (SQLite/Postgres)             │  │
│  │ • TTL: 30-90 días según tipo (auto-prune)               │  │
│  │ • Quién escribe: automático (el agente llama mem_save)  │  │
│  │ • Quién lee: el agente para contexto inmediato           │  │
│  │ • Propósito: "¿qué estábamos haciendo ayer?"             │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
│  NIVEL 2: ESTRUCTURADA (deliberada, permanente)              │
│  ┌────────────────────────────────────────────────────────┐  │
│  │ • Qué contiene: decisiones arquitectónicas, RFCs,      │  │
│  │   specs, lecciones aprendidas, patrones, convenciones   │  │
│  │ • Dónde: .md versionados en el repo + metadatos         │  │
│  │   en engram-dotnet (para FTS5 + link bidireccional)     │  │
│  │ • TTL: permanente (se borra con PR, como cualquier      │  │
│  │   código)                                               │  │
│  │ • Quién escribe: Memory Agent (Fase 4) + humano         │  │
│  │ • Quién lee: el agente y el humano como "constitución"  │  │
│  │   del proyecto                                           │  │
│  │ • Propósito: "¿por qué tomamos esta decisión?"          │  │
│  └────────────────────────────────────────────────────────┘  │
│                                                              │
└──────────────────────────────────────────────────────────────┘
```

---

## 3. Flujo entre Niveles

```
AGENTE TRABAJANDO
       │
       ▼
┌──────────────────┐
│  mem_save()      │  ← El agente guarda automáticamente
│  → Nivel 1 (DB)  │    (tool_use, discoveries, etc.)
└──────┬───────────┘
       │
       ▼
┌──────────────────────────────────────────────────────┐
│                  MEMORY AGENT (Fase 4)                 │
│                                                       │
│  1. Toma observaciones del ciclo actual               │
│  2. Decide cuáles merecen Nivel 2:                    │
│     - ¿Es una decisión arquitectónica? → .md          │
│     - ¿Es un patrón reusable? → .md                  │
│     - ¿Es debugging transitorio? → queda en N1        │
│     - ¿Es un error conocido con fix? → N1 + maybe N2 │
│  3. Para las que suben a N2:                          │
│     a. Genera .md estructurado en docs/decisions/     │
│     b. Guarda metadatos en DB con link al .md         │
│     c. Actualiza CLAUDE.md / AGENTS.md si aplica      │
│                                                       │
└──────────────────────────────────────────────────────┘
       │
       ▼
┌──────────────────────────────────────────────────────┐
│                  MEMORY JANITOR (background)           │
│                                                       │
│  Diario (cron determinístico, $0 de LLM):             │
│  - Borra observaciones N1 con TTL vencido             │
│  - Soft-delete: SET deleted_at = NOW()                │
│  - Log: "Pruned 42 observations"                      │
│                                                       │
│  Semanal (Haiku batch, barato):                       │
│  - Escanea observaciones próximas a vencer             │
│  - Pregunta: "¿alguna merece N2 antes de borrarse?"   │
│  - Si sí: genera .md y actualiza la observación       │
│  - Si no: deja que expire                             │
│                                                       │
└──────────────────────────────────────────────────────┘
```

---

## 4. TTL por Tipo de Observación

| Tipo | TTL sugerido | Comportamiento |
|------|-------------|----------------|
| `tool_use` | 30 días | Expira automáticamente |
| `file_change` | 30 días | Expira automáticamente |
| `command` | 30 días | Expira automáticamente |
| `bugfix` | 90 días | Expira (a menos que se promueva a N2) |
| `pattern` | 90 días | Expira (a menos que se promueva a N2) |
| `learning` | 60 días | Expira (a menos que se promueva a N2) |
| `discovery` | 60 días | Expira (a menos que se promueva a N2) |
| `decision` | Nunca | No expira (es N2 nativo) |
| `architecture` | Nunca | No expira (es N2 nativo) |
| `session_summary` | Nunca | No expira |

**Reglas**:
- Observaciones con `topic_key` NO expiran (son conocimiento estructurado deliberadamente)
- Si una observación se promueve a Nivel 2, su TTL se renueva o se elimina
- El TTL es configurable via `ENGRAM_TTL_{TYPE}` env vars

---

## 5. Esquema de Promoción a Nivel 2

Cuando una observación sube a Nivel 2, se crea un archivo .md en el repo con estructura canónica:

```
docs/decisions/
├── YYYY-MM-DD-short-title.md     ← Una decisión por archivo
├── index.md                        ← Índice de todas las decisiones
└── templates/
    └── decision.md                 ← Template para nuevas decisiones
```

### Ejemplo de .md de Nivel 2

```markdown
# ADR-001: Reemplazar sesiones con JWT

**Fecha**: 2026-05-13
**Tipo**: Architecture Decision Record
**Estado**: Aceptada

## Contexto
Necesitábamos escalar la autenticación a múltiples instancias.
Las sesiones en memoria no funcionaban con round-robin.

## Decisión
Usar JWT con refresh tokens. El token de acceso expira en 15 min,
el refresh token en 7 días.

## Consecuencias
- +: Stateless, escala horizontalmente
- +: No requiere Redis compartido
- -: Revocación de tokens es más compleja
- -: Payload de JWT no debe contener datos sensibles

## Referencias
- Observación engram-dotnet: #42
- spec.md relacionado: /specs/auth-service.md
```

### Link bidireccional

La observación en la DB guarda:
```
md_path: "docs/decisions/2026-05-13-reemplazar-sesiones-con-jwt.md"
```
Y el .md guarda:
```
observation_id: 42
```

Esto permite:
- Desde la DB: saber qué archivo .md corresponde
- Desde el .md: saber qué observación engram lo generó
- Buscar por FTS5 (DB) y encontrar el .md relacionado

---

## 6. Memory Janitor

**No es un agente. Es dos procesos background:**

### 6.1 Pruning diario (determinístico, $0)

```bash
#!/bin/bash
# Memory Janitor — daily prune
engram retention prune --type tool_use --older-than 30d --apply
engram retention prune --type file_change --older-than 30d --apply
engram retention prune --type command --older-than 30d --apply
engram retention prune --type bugfix --older-than 90d --apply
engram retention prune --type pattern --older-than 90d --apply
engram retention prune --type learning --older-than 60d --apply
engram retention prune --type discovery --older-than 60d --apply
```

### 6.2 Promoción semanal (Haiku batch, barato)

```bash
#!/bin/bash
# Memory Janitor — weekly promotion scan
engram retention scan --for-promotion
# Escanea observaciones próximas a vencer (>75% TTL)
# Para cada una, pregunta (via Haiku):
#   "¿Es una decisión arquitectónica, patrón reusable o lección aprendida?"
# Si sí: genera .md y actualiza observation.md_path
```

---

## 7. Integración con engram-dotnet

La estrategia de 2 niveles requiere cambios en engram-dotnet:

| Feature | Estado | Prioridad |
|---------|--------|-----------|
| TTL configurable por tipo (env vars) | ✅ Proposal escrita, no implementada | Alta — Fase 1 |
| `PruneOldObservationsAsync()` | ❌ No existe | Alta — Fase 1 |
| `mem_retention_prune` (MCP tool) | ❌ No existe | Alta — Fase 1 |
| `mem_retention_stats` (MCP tool) | ❌ No existe | Alta — Fase 1 |
| Campo `md_path` en observation | ❌ No existe | Alta — Fase 2 |
| `mem_promote_to_md` (MCP tool) | ❌ No existe | Alta — Fase 2 |
| Template engine para .md | ❌ No existe | Media — Fase 2 |
| `mem_verify_artifact` (MCP tool) | ❌ No existe | Alta — Fase 3 |
| `mem_traceability` (MCP tool) | ❌ No existe | Alta — Fase 3 |

Ver [03-engram-dotnet-gaps.md](03-engram-dotnet-gaps.md) para el detalle técnico.
