# Plan de Pruebas: Validación de EngramFlow en VS Code (Aislamiento OpenSpec)

> **Objetivo**: Validar de manera empírica, objetiva y no subjetiva el comportamiento de las 5 Skills Core de EngramFlow en Visual Studio Code utilizando la estructura de aislamiento por cambios de **FlowForge (Agentic SDLC)**. Esto asegura que las reglas de fase, la Capability Matrix, el Ralph Wiggum Loop y el Sentinel Judge funcionen de forma prolija antes de aplicarlos en producción.

---

## 1. El Proyecto de Práctica: `practice-todo-cli`

Para evitar ruido de dependencias, utilizaremos una pequeña aplicación CLI en **Python** para gestionar tareas con persistencia en un archivo JSON.

### 1.1 Estructura del Proyecto de Práctica
Crea un directorio `practice-todo-cli` en la raíz de tu workspace con la siguiente estructura:

```text
practice-todo-cli/
├── todo.py          # Lógica de la aplicación CLI y persistencia JSON
├── test_todo.py     # Suite de tests unitarios (usando pytest o unittest)
├── tasks.json       # Base de datos simulada en JSON
└── openspec/
    └── changes/
        └── 001-prioridades-tareas/   <-- CARPETA DE AISLAMIENTO DEL CAMBIO
```

### 1.2 Código Base de Partida
#### `todo.py`
```python
import json
import os

DB_FILE = "tasks.json"

def load_tasks():
    if not os.path.exists(DB_FILE):
        return []
    with open(DB_FILE, "r") as f:
        try:
            return json.load(f)
        except json.JSONDecodeError:
            return []

def save_tasks(tasks):
    with open(DB_FILE, "w") as f:
        json.dump(tasks, f, indent=4)

def add_task(title):
    if not title.strip():
        raise ValueError("El título no puede estar vacío")
    tasks = load_tasks()
    new_task = {
        "id": len(tasks) + 1,
        "title": title,
        "completed": False
    }
    tasks.append(new_task)
    save_tasks(tasks)
    return new_task
```

#### `test_todo.py`
```python
import os
import pytest
from todo import add_task, load_tasks, DB_FILE

@pytest.fixture(autouse=True)
def cleanup():
    if os.path.exists(DB_FILE):
        os.remove(DB_FILE)
    yield
    if os.path.exists(DB_FILE):
        os.remove(DB_FILE)

def test_add_task_success():
    task = add_task("Comprar leche")
    assert task["title"] == "Comprar leche"
    assert task["completed"] is False
    
    tasks = load_tasks()
    assert len(tasks) == 1

def test_add_task_empty_title():
    with pytest.raises(ValueError, match="El título no puede estar vacío"):
        add_task("")
```

---

## 2. La Tarea del Experimento (El Cambio)

**Requerimiento del Usuario**: 
> "Quiero poder agregar prioridades (ALTA, MEDIA, BAJA) a las tareas cuando las creo, y poder listar las tareas filtradas por una prioridad específica."

---

## 3. Guía de Ejecución de Pruebas Fase por Fase

> **Comandos vs agentes (v0.4):** Escribís **`/flow-*`** al orquestador; él delega a **`forge-*`**.
> No uses `/forge-memory` ni `@forge-dev` como comando principal — no son oficiales.
> Ver [`QUICKSTART.md`](../QUICKSTART.md) y [`14-flowforge-complete-reference.md`](14-flowforge-complete-reference.md).
>
> **Rutas:** artefactos en `.ai-work/{feature-slug}/` (kebab-case), no `openspec/changes/`.

Ejecuta las siguientes instrucciones en el chat del IDE en **modo Agent**, con reglas FlowForge activas:

### 3.0 Fase 0: Discovery + Spec (`/flow-start`)

* **Prompt de entrada**:
  ```text
  /flow-start Prioridades en tareas — agregar prioridades ALTA, MEDIA, BAJA al crear tareas y filtrar por prioridad. Default MEDIA.
  ```
* **Qué delega el orquestador:** `forge-discovery` → `forge-arch` → `spec.md` en `.ai-work/prioridades-tareas/`
* **Criterios (CKP-1):** spec con capability_matrix, GWT, PM-*; humano aprueba antes de `/flow-plan`

### 3.1 Fase 1: Spec (`forge-arch` vía `/flow-start`)

* **Alternativa legacy (no recomendada):** invocar `@forge-arch` directo — salta CKP-0 y discovery.
* **Criterios de aceptación (qué verificar en `spec.md`)**:
  - [ ] ¿Creó `spec.md` en `.ai-work/prioridades-tareas/spec.md`?
  - [ ] ¿La `capability_matrix` clasifica ALTA/MEDIA/BAJA como `deterministic`?
  - [ ] ¿Al menos 2 escenarios Given-When-Then?
  - [ ] ¿Sin código Python propuesto?

### 3.2 Fase 2: Plan (`/flow-plan` → `forge-plan`)

* **Prompt de entrada**:
  ```text
  /flow-plan
  ```
* **Criterios de aceptación (qué verificar en `plan.md`)**:
  - [ ] ¿`plan.md` en `.ai-work/prioridades-tareas/plan.md`?
  - [ ] ¿Proposed Changes solo `todo.py` y `test_todo.py`?
  - [ ] ¿Esquema JSON explícito con `priority`?
  - [ ] ¿Checklist topológico (persistencia antes de tests)?

### 3.3 Fase 3: Ejecución (`/flow-dev` → `forge-dev`)

* **Prompt de entrada**:
  ```text
  /flow-dev
  ```
* **Criterios de aceptación**:
  - [ ] ¿Respeta el plan sin freelancing?
  - [ ] ¿Ralph Wiggum: pytest verde?
  - [ ] ¿Tests con prefijo `[RF-XXX]`?

### 3.4 Fase 4: Juicio (`/flow-verify` → `forge-verify`)

Inyectá una falla manual en `todo.py` antes de verificar.

* **Prompt de entrada**:
  ```text
  /flow-verify
  ```
* **Criterios de aceptación**:
  - [ ] ¿REJECTED o `rework_ticket.md` con `cycle_count: 1`?
  - [ ] ¿Motivo objetivo documentado?
  - [ ] Tras corregir: segundo `/flow-verify` → PASS en `verify-report.md`

### 3.5 Fase 5: Cierre (`/flow-close` → `forge-memory`)

* **Prompt de entrada**:
  ```text
  /flow-close
  ```
  (No uses `/forge-memory` — el comando canónico es **`/flow-close`**.)

* **Criterios de aceptación**:
  - [ ] ¿PM-* marcados `[x]` en `spec.md`?
  - [ ] ¿`summary.md` en `.ai-work/prioridades-tareas/`?
  - [ ] ¿`mem_session_summary` llamado (o fallback en `.engram/local_memory/` si MCP cae)?
  - [ ] ¿Decisiones clave persistidas vía Memory Curation Protocol (orquestador + Engram)?

---

## 4. Matriz de Evaluación del Experimento

Usa esta tabla para calificar el desempeño de tus modelos de OpenRouter en VS Code:

| Skill | Modelo Utilizado | ¿Cumplió Reglas Duras? (S/N) | Tiempo de Ejecución | Calidad del Output (1-10) |
|---|---|---|---|---|
| **forge-arch** | | | | |
| **forge-plan** | | | | |
| **forge-dev** | | | | |
| **forge-verify** | | | | |
| **forge-memory** | | | | |

Si todas las fases obtienen una calificación mayor o igual a **8/10** y las restricciones de fase (no-touch, no-coding en Arch, no-freelancing en Dev) se respetan estrictamente, la metodología se considerará **Apta para Producción** en FlowForge.
