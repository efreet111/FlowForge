# HU-048: `mem_retention_prune` — Fix de Filtrado y Gate de Dry-Run

## Metadata

| Campo | Valor |
|-------|-------|
| **HU** | HU-048 |
| **Titulo** | Fix de `mem_retention_prune` en `forge-memory` — filtrado por tipo y gate de dry-run |
| **Estado** | Done |
| **Fecha de creacion** | 2026-10-16 |
| **Fecha de cierre** | 2026-10-16 |
| **Prioridad** | Alta |
| **Skills relacionadas** | `forge-memory` |
| **Tags** | `engram`, `memory`, `bugfix`, `data-safety` |

---

## 1. Resumen

Esta HU describe la correccion del skill `forge-memory` para `mem_retention_prune`: actualmente la herramienta borra **todo** tipo de observacion (bugfix, pattern, discovery, etc.) en lugar de solo las **temporales** (`tool_use`, `command`, `file_change`) como promete la documentacion. Esto destruye conocimiento valioso del equipo sin posibilidad de recover.

---

## 2. Motivacion

Durante el cierre de sesion de HU-047 (`fforge-init`), el agente `forge-memory` ejecuto `mem_retention_stats` para previsualizar el pruning:

| Tipo | Count |
|------|-------|
| bugfix | 98 |
| pattern | 59 |
| discovery | 71 |
| learning | 4 |
| **TOTAL** | **232** |

La skill `forge-memory/SKILL.md` L199 dice:

> "invoke `mem_retention_prune` to safely delete **temporary observations** (`tool_use`, `command`, `file_change`)"

Sin embargo, el dry-run mostro que se borrarian observaciones de tipo **bugfix, pattern, discovery, learning** — conocimiento durable del equipo.

El agente **eligio no ejecutar** `mem_retention_prune` para no destruir conocimiento valioso. Esto demuestra que la skill estaba inspecionando conocimiento.

**Impacto:** Si un agente sigue la skill literalmente, destruye 232 piezas de conocimiento durable sin opcion de recover.

---

## 3. Problema Especifico

### 3.1 Contradicion Documentacion vs Comportamiento

| Fuente | Dice que hace | Lo que realmente hace |
|--------|---------------|----------------------|
| `forge-memory/SKILL.md` L199 | Borra solo `tool_use`, `command`, `file_change` | Borra **todos** los tipos |
| `engram MCP` | TTL-based cleanup | El parametro `type` no filtra correctamente |

### 3.2 Falta de Gate de Dry-Run

La skill dice:

> "run `mem_retention_stats` first to see what will be pruned"

Pero no dice que hacer con esa informacion. No hay un gate que pause si el output muestra tipos que **no deberian** borrarse.

### 3.3 Comportamiento Riesgoso

```
mem_retention_prune sin filtro
  ↓
Borra 232 observaciones (bugfix, pattern, discovery, learning)
  ↓
Conocimiento valioso del equipo destruido
  ↓
No hay opcion de recover
```

---

## 4. Solucion Implementada

### 4.1 Fix en `forge-memory/SKILL.md` (v1.4.0)

**Paso 1:** Ejecutar `mem_retention_stats` para previsualizar

**Paso 2:** Validar que SOLO hay tipos temporales en la lista de a borrar:
- `tool_use` ✅ OK para borrar
- `command` ✅ OK para borrar
- `file_change` ✅ OK para borrar
- `bugfix` ❌ NO borrar
- `pattern` ❌ NO borrar
- `discovery` ❌ NO borrar
- `learning` ❌ NO borrar
- Cualquier otro tipo ❌ NO borrar

**Paso 3:** Si hay tipos no-temporales:
- Emitir WARNING con lista de tipos problematicos
- **NO ejecutar** `mem_retention_prune` automaticamente
- Ofrecer al usuario la opcion de:
  - [A] Continuar de todas formas (con confirmacion explicita)
  - [B] Cancelar (comportamiento seguro)

**Paso 4:** Si solo hay tipos temporales: ejecutar `mem_retention_prune`

### 4.2 Respuesta OQ-1 (RESUELTA)

`mem_retention_prune` **NO soporta** parametro `type` para filtrado. La limitacion esta documentada en la skill como "Limitation note" con sugerencia de feature request.

### 4.3 Verificacion Post-Fix

El gate de validacion ahora impide que se borren tipos no-temporales. La unica forma de resolver esto seria modificar `mem_retention_prune` en engram-dotnet para soportar filtrado por tipo (Fase 2, opcional).

---

## 5. Criterios de Aceptacion

### 5.1 Funcionales

- [x] La skill ya no destruye conocimiento durable (bugfix, pattern, discovery)
- [x] El gate de dry-run esta implementado
- [x] El warning se emite cuando hay tipos no-temporales
- [x] El usuario tiene opcion de continuar o cancelar

### 5.2 No Funcionales

- [x] El comportamiento seguro es el default (no se borra nada automaticamente si hay riesgo)
- [x] Mensajes de error claros y accionables

### 5.3 Testeable

- [x] `mem_retention_stats` muestra tipos no-temporales → WARNING emitido
- [x] `mem_retention_stats` muestra solo tipos temporales → se ejecuta sin warning
- [x] El usuario puede elegir continuar o cancelar

---

## 6. Dependencias

| Dependencia | Tipo | Notas |
|-------------|------|-------|
| `engram-dotnet` | MCP | Verificar si `mem_retention_prune` soporta filtrado `type` |
| `forge-memory` | Skill | Unico archivo a modificar |

---

## 7. Roadmap

### Fase 1 — Fix Inmediato ✅
- [x] Investigar si `mem_retention_prune` soporta parametro `type` → **NO soporta**
- [x] Actualizar `forge-memory/SKILL.md` con el gate de validacion → **v1.4.0**
- [x] Testear con `mem_retention_stats` real → **Gate implementado**

### Fase 2 — Mejora (Opcional)
- [ ] Proponer modificacion a `mem_retention_prune` en engram-dotnet para soportar filtrado por tipo

---

## 8. Preguntas Abiertas

| ID | Pregunta | Prioridad | Estado |
|----|----------|----------|--------|
| OQ-1 | `mem_retention_prune` soporta parametro `type` para filtrar? | Alta | **RESUELTO: NO** |
| OQ-2 | Cual es el TTL de cada tipo de observacion? | Media | Abierta |
| OQ-3 | Hay way de recover observaciones borradas? | Media | Abierta |

---

## 9. Memoria de Decisiones

| Decision | Justificacion |
|----------|---------------|
| Gate de validacion antes de prune | Comportamiento seguro es el default |
| Warning + opcion de usuario | El usuario decide si el riesgo es aceptable |
| No auto-borrar tipos no-temporales | Preserva conocimiento valioso del equipo |
| Sin parametro `type` en `mem_retention_prune` | Limitacion documentada; gate es unworkaround |

---

## 10. Resumen de Cambios

| Archivo | Cambio |
|---------|--------|
| `forge-memory/SKILL.md` | Agregado DRY-RUN GATE (HU-048), version 1.3.0 → 1.4.0 |
