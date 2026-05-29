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

Ejecuta las siguientes instrucciones en el chat de **Visual Studio Code** utilizando los modelos de OpenRouter para evaluar cada skill:

### 3.1 Fase 1: Intención (`@forge-arch`)
* **Prompt de Entrada**:
  ```text
  @forge-arch Quiero implementar el requerimiento de prioridades en el proyecto practice-todo-cli. La prioridad por defecto al crear debe ser MEDIA. Guarda la especificación spec.md en el directorio de aislamiento: openspec/changes/001-prioridades-tareas/spec.md.
  ```
* **Criterios de Aceptación (Qué verificar en `spec.md`)**:
  - [ ] ¿Creó el archivo `spec.md` estrictamente en `openspec/changes/001-prioridades-tareas/spec.md`?
  - [ ] ¿La `capability_matrix` clasifica las prioridades exactas (ALTA, MEDIA, BAJA) como `deterministic` y los mensajes del CLI como `ai_reasoning`?
  - [ ] ¿Definió al menos 2 escenarios Given-When-Then para la creación y el filtrado por prioridad?
  - [ ] **Restricción**: ¿El agente se abstuvo de escribir o proponer código en Python? (Si propuso código, la skill falló la regla de no-touch).

### 3.2 Fase 2: Arquitectura (`@forge-plan`)
* **Prompt de Entrada**:
  ```text
  @forge-plan Diseña el plan técnico para implementar el spec.md generated en openspec/changes/001-prioridades-tareas/spec.md. Guarda el resultado en openspec/changes/001-prioridades-tareas/plan.md.
  ```
* **Criterios de Aceptación (Qué verificar en `plan.md`)**:
  - [ ] ¿Creó el archivo `plan.md` estrictamente en `openspec/changes/001-prioridades-tareas/plan.md`?
  - [ ] ¿Las modificaciones de archivos en "Proposed Changes" listan únicamente `todo.py` y `test_todo.py`?
  - [ ] ¿El esquema de datos JSON resultante está explícitamente detallado (ej: `{"id": int, "title": str, "completed": bool, "priority": str}`)?
  - [ ] ¿El checklist tiene orden topológico estricto (primero persistencia y constantes en `todo.py`, al final tests en `test_todo.py`)?
  - [ ] **Restricción**: ¿El plan es lo suficientemente cerrado para que el programador no tenga que tomar decisiones de diseño?

### 3.3 Fase 3: Ejecución (`@forge-dev`)
* **Prompt de Entrada**:
  ```text
  @forge-dev Implementa el plan.md ubicado en openspec/changes/001-prioridades-tareas/plan.md. Recuerda correr los tests usando pytest en la terminal de forma autónoma.
  ```
* **Criterios de Aceptación (Qué verificar en el código)**:
  - [ ] ¿El Dev Agent modificó `todo.py` y `test_todo.py` respetando estrictamente el alcance del plan?
  - [ ] ¿Corrió de forma autónoma `pytest` en la terminal (Ralph Wiggum Loop) para autocorregirse si algo falló?
  - [ ] ¿Creó tests unitarios específicos para cada escenario del spec con el prefijo `[RF-XXX]`?

### 3.4 Fase 4: Juicio (`@forge-verify` - Test de Inyección de Falla)
Para probar realmente al Sentinel Judge, inyectaremos una falla de manera intencionada antes de invocarlo.

* **Inyección de Falla (Hacer manualmente)**: Modifica `todo.py` agregando un print inútil (ej. `print("debug")`) o rompe la validación de prioridad (ej. permite guardar una prioridad "SUPER_ALTA" o cambia el default a "baja").
* **Prompt de Entrada**:
  ```text
  @forge-verify Audita el código entregado contra openspec/changes/001-prioridades-tareas/spec.md y openspec/changes/001-prioridades-tareas/plan.md. Copia y pega aquí el output de pytest si los tests fallan para darme evidencia.
  ```
* **Criterios de Aceptación (Qué verificar en el veredicto)**:
  - [ ] ¿El agente rechazó la entrega y generó un veredicto de REJECTED?
  - [ ] ¿Creó un archivo `rework_ticket.md` con `cycle_count: 1` estrictamente bajo `openspec/changes/001-prioridades-tareas/rework_ticket.md`?
  - [ ] ¿Identificó el motivo exacto del fallo de forma objetiva (detectando el print de debug, la falta de retorno, o la prioridad default alterada)?
  - [ ] **Segunda vuelta**: Corrige el error manual, corre `@forge-verify` pasándole el output de pytest en verde. ¿Emitió un "PASS" limpio?

### 3.5 Fase 5: Cierre (`@forge-memory`)
* **Prompt de Entrada**:
  ```text
  @forge-memory Cierra la sesión de desarrollo basándote en la carpeta de aislamiento openspec/changes/001-prioridades-tareas/ y extrae el conocimiento del ciclo.
  ```
* **Criterios de Aceptación (Qué verificar en los logs/Engram)**:
  - [ ] ¿Llamó a `mem_save` con una estructura formal *What/Why/Where/Learned*?
  - [ ] ¿Categorizó el cambio como `architecture` o `pattern`?
  - [ ] Si hubo rework en la fase 4, ¿quedó registrado el bugfix en la base de datos neuronal?

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
