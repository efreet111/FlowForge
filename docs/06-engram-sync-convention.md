# Engram Sync Convention — Colaboración en Equipo

> **Versión**: 1.0
> **Feature**: Offline-First Sync (Phases 1-4 ✅)
> **Requiere**: engram-dotnet v1.1.0+, servidor PostgreSQL en `192.168.0.178:7437`

---

## ⚡ Resumen para Agentes

Engram soporta **sync bidireccional** entre el SQLite local y un servidor PostgreSQL compartido. Esto permite que **N desarrolladores** compartan memorias de equipo sin pisarse entre sí.

---

## 🔧 Herramientas MCP Disponibles

| Herramienta | Qué hace | Cuándo usarla |
|-------------|----------|---------------|
| `mem_save` | Guarda una observación en el servidor (via sync) | Cuando termines una tarea, descubras un patrón, o decidas algo |
| `mem_search` | Busca en TODAS las memorias (locales + sincronizadas) | Cuando necesites contexto de sesiones anteriores, tuyas o del equipo |
| `mem_sync_status` | Verifica estado del sync (health, pendientes, errores) | Antes de cerrar sesión o si notas que falta data |
| `mem_doctor` | Diagnóstico completo del sistema (DB, HTTP, MCP) | Cuando algo no funciona y no sabés por qué |

---

## 📋 Endpoints HTTP (para admin/configuración)

| Endpoint | Método | Propósito |
|----------|--------|-----------|
| `/sync/enroll` | POST | Inscribir un proyecto en el sync |
| `/sync/enroll` | GET | Listar proyectos inscritos |
| `/sync/enroll?project=X` | DELETE | Desinscribir un proyecto |
| `/sync/pause` | POST | Pausar sync (mantenimiento) |
| `/sync/pause?project=X` | DELETE | Reanudar sync |
| `/sync/status` | GET | Estado completo del sync |
| `/sync/mutations/push` | POST | Forzar push de mutaciones |
| `/sync/mutations/pull` | GET | Forzar pull de mutaciones |

---

## 👥 Flujo para Equipos Multi-Usuario

### Escenario: 5 desarrolladores en FlowForge

```
         ┌──────────────────────────────┐
         │  PostgreSQL Server           │
         │  192.168.0.178:7437          │
         │  Memorias de TODO el equipo  │
         └──────┬───────┬───────┬───────┘
                │       │       │
     ┌──────────┤   ┌───┤   ┌───┤──────────┐
     ▼          │   ▼   │   ▼              ▼
┌─────────┐     │┌─────┐│ ┌─────┐     ┌─────────┐
│ Dev 1   │     ││Dev 2││ │Dev 3│ ... │ Dev 5   │
│ victor  │     ││juan ││ │ana  │     │ maria   │
│ SQLite  │     ││SQLite│ │SQLite│     │ SQLite  │
└─────────┘     └─────┘ └─────┘     └─────────┘
     │                              │
     └────── SyncManager ──────────┘
           (push/pull automático)
```

### Cómo funciona:

1. **Cada developer** corre `engram serve` o `engram mcp` localmente
2. **Memorias personales** (`scope: personal`) → se aíslan automáticamente como `personal:{user}`
3. **Memorias de equipo** (`scope: team` o `project: team/{proyecto}`) → se sincronizan al servidor
4. **SyncManager** hace push/pull automático cada 30s (configurable)

---

## 🧪 Protocolo para Agentes

### Al guardar una memoria:

```markdown
mem_save(
  title="Decisión: usar PostgreSQL para sync",
  content="**What**: ... **Why**: ... **Where**: ... **Learned**: ...",
  type="decision",
  project="team/flowforge",
  scope="team"          // ← IMPORTANTE: "team" para compartir, "personal" para privado
)
```

### Al buscar contexto del equipo:

```markdown
// Buscar en TODOS los proyectos del equipo
mem_search(query="arquitectura sync", project="team/flowforge")

// Buscar en decisiones pasadas
mem_search(query="TTL config", type="decision")

// Verificar sync antes de cerrar sesión
mem_sync_status()
```

### Al iniciar sesión:

```markdown
// Verificar que el servidor está accesible
curl http://192.168.0.178:7437/health

// Verificar sync status
mem_sync_status()
```

---

## 🔐 Aislamiento Multi-Usuario (RFC-002)

| Usuario | memoria personal | memoria de equipo |
|---------|------------------|-------------------|
| `victor.silgado` | `personal:victor.silgado/...` | `team/flowforge/...` |
| `juan.perez` | `personal:juan.perez/...` | `team/flowforge/...` |
| `ana.gomez` | `personal:ana.gomez/...` | `team/flowforge/...` |

**Los usuarios NO ven las memorias personales de otros.** Solo comparten lo marcado como `team/`.

---

## 🔧 Configuración del Entorno

Cada developer necesita en su `.env` o `opencode.json`:

```json
{
  "command": "engram",
  "args": ["mcp", "--tools=agent"],
  "env": {
    "ENGRAM_DATA_DIR": "~/.engram",
    "ENGRAM_USER": "tu.email@ejemplo.com",   // ← OPCIONAL pero RECOMENDADO
    "ENGRAM_SYNC_ENABLED": "true",
    "ENGRAM_SERVER_URL": "http://192.168.0.178:7437"
  }
}
```

> **Importante**: Para sync offline-first usá `ENGRAM_SERVER_URL`, **no** `ENGRAM_URL`.  
> `ENGRAM_URL` activa modo remoto puro (HttpStore) y desactiva el journal local de sync.

### ¿Es obligatorio `ENGRAM_USER`?

**No, es opcional.** Pero tiene ventajas importantes en modo equipo:

| Variable | ¿Obligatoria? | ¿Qué pasa si no la ponés? |
|----------|---------------|--------------------------|
| `ENGRAM_SERVER_URL` | ✅ Sí (para sync) | Sin servidor, no hay sync |
| `ENGRAM_SYNC_ENABLED` | ✅ Sí (para sync) | El cliente no sincroniza |
| `ENGRAM_DATA_DIR` | ❌ No (usa `~/.engram` por defecto) | SQLite en otro lado |
| **`ENGRAM_USER`** | ❌ **Opcional** | **Funciona igual**, pero perdés las ventajas de abajo |

### Ventajas de configurar `ENGRAM_USER`

| Ventaja | Con `ENGRAM_USER` | Sin `ENGRAM_USER` |
|---------|-------------------|-------------------|
| **Memorias personales** | Se guardan como `personal:{user}/project` | Van a un namespace genérico |
| **Auditoría** | El servidor registra quién hizo cada save | No hay trazabilidad de autor |
| **Aislamiento** | Cada user tiene su propio espacio personal | Todos comparten el mismo espacio "anon" |
| **Scope personal** | `scope: "personal"` → `personal:victor.silgado/...` | Puede ir a `null/...` o fallar |
| **Scope team** | `scope: "team"` → `team/...` (igual) | `team/...` (igual, funciona) |

### ¿Cuándo podés omitirlo?

- **Desarrollo individual**: Si sos el único usuario, no hace falta.
- **Pruebas locales**: Para validar que el sync funciona, podés probar sin user.
- **Memorias 100% team**: Si todo lo que guardás es `scope: "team"`, no hay diferencia.

### ¿Cuándo deberías ponerlo?

- **Equipos de 2+ personas**: Para que cada uno tenga su namespace personal.
- **Múltiples máquinas**: Si usás `ENGRAM_USER` consistente, tus memorias personales te siguen.
- **Auditoría**: Si querés saber "quién guardó qué" en el equipo.

**Recomendación**: Usá tu email o username de GitHub. Ej: `victor.silgado`, `juan@empresa.com`, `gantz`.

---

## 🐛 Troubleshooting

| Síntoma | Causa probable | Solución |
|---------|---------------|----------|
| Sync no arranca | `ENGRAM_SYNC_ENABLED=false` | Setear `true` |
| Push falla 409 | Sync paused por admin | `DELETE /sync/pause?project=X` |
| Pull no trae data | Proyecto no enrolled | `POST /sync/enroll` |
| 500 sin body | Error no logueado | Ver logs del servidor |
| Memorias no aparecen | Filtro por `X-Engram-User` | Verificar header coincide |
| `scope: personal` va a namespace raro | `ENGRAM_USER` no seteado | Setear `ENGRAM_USER` (opcional pero recomendado) |

---

## 📚 Comandos Rápidos

```bash
# Ver estado
engram sync status

# Ver enrollemos
curl -H "X-Engram-User: victor.silgado" http://192.168.0.178:7437/sync/enroll

# Ver salud
engram doctor --server http://192.168.0.178:7437
```
