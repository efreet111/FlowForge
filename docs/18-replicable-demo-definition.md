# FlowForge — Definición de Demo Replicable (Release Gate)

## Objetivo
Una **demo replicable** es evidencia mínima y reproducible de que FlowForge funciona en un entorno real (idealmente **Cursor**) y que un tercero puede repetir el flujo sin “telepatía” del autor.

Esta demo es un **requisito previo** para publicar FlowForge como open source.

---

## Forma recomendada

### Opción A (recomendada): repo demo separado
Crear un repo independiente, por ejemplo: `flowforge-demo-task-manager`.

**Ventajas**:
- Permite versionar artefactos `spec.md`/`plan.md`/reworks junto con el código.
- Evita contaminar el repo de metodología con un proyecto app.
- Es ideal para que terceros “clonen y prueben” sin contexto.

### Opción B: carpeta `examples/` dentro de FlowForge
Crear `examples/task-manager/` dentro de este repo.

**Ventaja**: una sola URL.\n+**Desventaja**: agranda el repo y mezcla “metodología” con “producto demo”.

---

## Proyecto de demo (scope fijo)

### Nombre
**Task Manager API**

### Stack (Cursor-first, low friction)
- **Node.js + TypeScript**
- **SQLite** (para evitar dependencia de PostgreSQL/Redis en la primera demo)
- **Tests** con `vitest` (o `jest`) + `supertest`

### Reglas de scope (para que sea replicable)
- Sin auth en V1 (auth se deja como “Caso 2” en docs/14).\n+- Sin UI.\n+- CRUD simple con validaciones.

---

## Artefactos obligatorios (lo que hay que “subir”)

En el proyecto demo debe existir, versionado en git:

### 1) `docs/spec.md`
- RFs con IDs (ej: `RF-001`..)\n+- RNFs mínimos (logging, error handling)\n+- Escenarios Given-When-Then por RF\n+- Capability Matrix (ai_reasoning vs deterministic)

### 2) `docs/plan.md`
- “Proposed Changes” con lista explícita de archivos a tocar\n+- Checklist topológico (tareas ordenadas)\n+- Plan de tests (qué ejecutar, y qué constituye PASS)

### 3) Evidencia de Verify
Una de estas dos (idealmente ambas):
- `docs/verify-report.md` con:\n  - Resultado final (PASS)\n  - Resumen de auditoría (seguridad básica, complejidad, performance mínima)\n  - “Manual test steps”
- y/o un historial de reworks:\n  - `docs/rework_ticket.md` (si hubo) con `cycle_count` y motivo

### 4) `README.md` del demo
Debe incluir:\n+- Prerrequisitos\n+- Cómo correr tests\n+- Cómo correr API\n+- Cómo ejecutar el flujo FlowForge en Cursor (pasos exactos)

---

## Criterios de aceptación (Definition of Done)

La demo se considera “replicable” si se cumplen TODOS:

1. **Clonado en limpio**: un tercero puede clonar el repo y correr `npm test` con éxito.\n+2. **No hay pasos ocultos**: todo prerequisito está en el README.\n+3. **FlowForge trazable**: `spec.md` y `plan.md` están completos y coherentes.\n+4. **Verify auditable**: existe `verify-report.md` o evidencia equivalente.\n+5. **Cursor probado**: hay una sección “Cursor runbook” con pasos y resultados esperados.

---

## Runbook mínimo para Cursor (pasos reproducibles)

En una máquina nueva (o ambiente limpio):

1. Instalar FlowForge en Cursor (modo local si FlowForge es privado).\n+2. Abrir el repo demo en Cursor.\n+3. Ejecutar:\n+   - `/flow-start CRUD de tareas`\n+4. Revisar el `spec.md` generado y aprobar CKP-1.\n+5. Ejecutar:\n+   - `/flow-dev`\n+6. Revisar/ejecutar tests (los comandos exactos deben estar en el plan).\n+7. Ejecutar:\n+   - `/flow-verify`\n+8. Confirmar PASS.\n+9. Ejecutar:\n+   - `/flow-close` (si aplica) y decidir deploy gate.

**Resultado esperado**: el repo queda con los artefactos `spec.md`, `plan.md`, verify PASS y tests verdes.

---

## Qué NO cuenta como demo replicable (anti-criterios)
- “Funciona en mi máquina” sin instrucciones.\n+- Requiere servicios externos sin alternativa (por ejemplo Postgres + Redis) en la primera demo.\n+- No hay tests automatizados.\n+- `spec.md`/`plan.md` están incompletos o contradictorios con el código.

