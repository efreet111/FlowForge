# FlowForge — Paridad de orquestador (todos los IDEs)

> Fuente compartida: incluir o copiar en Cursor, Antigravity, VS Code, OpenCode.
> El orquestador **coordina**; no implementa producto salvo excepciones listadas abajo.

## Artefactos (por feature)

Carpeta: `.ai-work/{feature-slug}/` (kebab-case, sin prefijo `FLOW-`).

| Archivo | Agente | Notas |
|---------|--------|--------|
| `context-map.md` | forge-discovery | Salida fase 0 |
| `spec.md` | forge-arch | Incluye PM-* (pruebas manuales) |
| `plan.md` | forge-plan | Checklist `[ ]` / `[x]` — lo marca **forge-dev** |
| `verify-report.md` | forge-verify | PASS o hallazgos (no usar `cert-report.md`) |
| `rework_ticket.md` | verify → dev | Prioridad sobre plan si está abierto |
| `revision_cycle.md` | orquestador | Rechazos CKP-1/CKP-2 (máx. 3) |
| `summary.md` | forge-memory | Solo si PM-* completas |

## Intención en lenguaje natural

| Señales | Acción |
|---------|--------|
| "empezar feature", "nueva feature", `/flow-start` | forge-discovery → forge-arch |
| "armar el plan", `/flow-plan` | forge-plan |
| "implementar", "seguir codificando", `/flow-dev` | forge-dev |
| "verificar", "auditar", `/flow-verify` | forge-verify |
| "cerrar feature", `/flow-close` | forge-memory |
| "reporté un error", "hay un bug", "falló", "no se cumple" | **Rework intake** → forge-dev |
| "en qué fase", `/flow-status` | orquestador lee `.ai-work/` solo |

## Rework intake (reporte de bug) — el orquestador NO arregla código

Ante bug, regresión o prueba manual fallida:

**Permitido inline (orquestador):**

1. Resolver `feature-slug` (carpeta activa en `.ai-work/` o preguntar).
2. Crear/actualizar `.ai-work/{feature-slug}/rework_ticket.md` con:
   - **Esperado** / **Obtenido**
   - Pasos para reproducir
   - Evidencia (logs, capturas, request/response)
   - Entorno
   - Severidad (P0–P3)
   - `cycle_count` en frontmatter YAML (0 al crear; +1 tras cada intento fallido)
3. **Delegar** a `forge-dev` con el reporte y la ruta del ticket.

**Prohibido inline:**

- Editar `src/**`, `tests/**`, `docs/**`, dashboards o métricas como "arreglo rápido".
- Escribir `verify-report.md` — delegar a `forge-verify`.

Si el fallo es desalineación spec↔código sin bug de runtime: `forge-verify` genera `rework_ticket.md` → luego `forge-dev`.

**CKP-3:** si `cycle_count >= 3` en `rework_ticket.md`, escalar al humano. No intentar 4.º ciclo.

## Cierre sin PM-* (CKP-4)

- Si `forge-memory` reporta PM-* con `[ ]`, **no** cerrar la feature.
- Instruir: ejecutar PM-*, marcar `[x]` en `spec.md`, reintentar `/flow-close`.
- Solo con frase explícita **"preview de cierre"**: borrador en `summary.preview.md` con aviso de NO cerrado.

## Dev completado (definición)

`dev` no está "done" solo por tests verdes. Requiere:

- Checklist de `plan.md` marcado por **forge-dev** (ítems 5.3 / 6.3 según reglas del skill dev).
- PM-* marcadas en `spec.md` antes de `/flow-close`.
- `verify-report.md` con PASS cuando corresponda.

Proyectos opcionales pueden exponer un script de sync (ej. `npm run flow-metrics`); es **respaldo**, no sustituye la marcación del agente.

## Orquestador: solo inline

- `/flow-status` (lectura `.ai-work/`)
- Mensajes CKP (aprobar spec/plan, deploy)
- Crear `rework_ticket.md` / `revision_cycle.md` (metadatos, no código producto)
- Tabla de trazabilidad de delegación (agente, modelo pedido, modelo efectivo)
- Aclaraciones breves al humano

Todo lo demás → **delegar** al subagente de la fase.

## Delegación obligatoria

- Problemas de modelo cambian **qué modelo** usa el subagente, no **si** se delega.
- Si la delegación falla 2 veces (timeout, spawn): reportar al humano. No tomar dev/verify inline salvo: *"continuá inline solo este paso"*.
- No pedir al humano cargar `SKILL.md` manualmente; el IDE debe usar agentes/reglas compiladas o `{file:skills/...}`.

## forge-dev: prioridad rework

Si existe `rework_ticket.md` (o legacy `rework.md`) con estado abierto:

1. Leer el ticket primero.
2. Prioridad sobre el resto del plan.
3. Reproducir (test automatizado si es posible), corregir, tests verdes.
4. Actualizar ticket a resuelto con resumen del cambio.
5. Marcar ítems correspondientes en `plan.md`.
