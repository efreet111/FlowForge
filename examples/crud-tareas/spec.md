---
capability_matrix:
  ai_reasoning:
    - Formato de timestamp para createdAt/updatedAt (ISO 8601 vs Unix timestamp)
    - Mensajes de error específicos por campo de validación
    - Estructura de paginación si se implementa en GET /tasks
    - Orden por defecto de tareas en listado (createdAt desc vs updatedAt desc)
  deterministic:
    - title es campo obligatorio, máximo 255 caracteres
    - description máximo 1000 caracteres si presente
    - status debe ser enum: "pending", "in-progress", "completed"
    - priority debe ser enum: "low", "medium", "high"
    - HTTP 400 para validaciones fallidas, 404 para recursos no encontrados
    - HTTP 201 para creación exitosa, 200 para actualización/consulta, 204 para eliminación
    - Respuesta JSON siempre con Content-Type application/json
    - Mensajes de error en español
---
# Spec: CRUD de tareas

## 1. Objetivo y Alcance

Implementar una API REST para gestión básica de tareas (Task Manager) que permita operaciones CRUD completas sobre un modelo de tarea simple. La API servirá como backend JSON para demostrar el flujo FlowForge, sin autenticación ni interfaz de usuario en V1.

**Alcance incluido:**
- Endpoints REST para crear, leer, actualizar y eliminar tareas
- Validaciones de entrada y manejo de errores
- Persistencia en SQLite local
- Tests unitarios e integración

**Fuera de alcance V1:**
- Autenticación/autorización
- Interfaz de usuario (frontend)
- Filtros avanzados o búsqueda de texto completo
- Migración a PostgreSQL o bases de datos en la nube
- Paginación compleja
- Rate limiting

## 2. Requerimientos Funcionales (RF)

### RF-001: Crear tarea
Permitir la creación de nuevas tareas mediante POST /tasks con validación de campos requeridos.

**Escenario A**: Given un payload válido con title "Comprar leche" When POST /tasks Then respuesta 201 con la tarea creada incluyendo id generado y timestamps.

**Escenario B**: Given un payload sin title When POST /tasks Then respuesta 400 con mensaje "El campo title es obligatorio".

### RF-002: Listar todas las tareas
Obtener lista completa de tareas existentes mediante GET /tasks.

**Escenario A**: Given 3 tareas en la base de datos When GET /tasks Then respuesta 200 con array de 3 tareas en formato JSON.

**Escenario B**: Given base de datos vacía When GET /tasks Then respuesta 200 con array vacío [].

### RF-003: Obtener tarea por ID
Consultar una tarea específica mediante GET /tasks/:id.

**Escenario A**: Given una tarea existente con id "123" When GET /tasks/123 Then respuesta 200 con los datos completos de la tarea.

**Escenario B**: Given id inexistente "999" When GET /tasks/999 Then respuesta 404 con mensaje "Tarea no encontrada".

### RF-004: Actualizar tarea
Modificar campos de una tarea existente mediante PUT /tasks/:id.

**Escenario A**: Given tarea existente When PUT /tasks/:id con {"title": "Nuevo título"} Then respuesta 200 con tarea actualizada y updatedAt modificado.

**Escenario B**: Given id inexistente When PUT /tasks/999 Then respuesta 404 con mensaje "Tarea no encontrada".

### RF-005: Eliminar tarea
Remover una tarea del sistema mediante DELETE /tasks/:id.

**Escenario A**: Given tarea existente con id "123" When DELETE /tasks/123 Then respuesta 204 sin contenido y tarea eliminada de BD.

**Escenario B**: Given id inexistente When DELETE /tasks/999 Then respuesta 404 con mensaje "Tarea no encontrada".

## 3. Requerimientos No Funcionales (RNF)

### RNF-001: Performance
- Tiempo de respuesta < 200ms para operaciones CRUD en datasets < 1000 tareas
- SQLite debe manejar operaciones concurrentes básicas sin bloqueos prolongados

### RNF-002: Estructura de datos
- Campo id: UUID v4 o auto-increment integer (decisión del desarrollador)
- Timestamps: ISO 8601 format (YYYY-MM-DDTHH:mm:ssZ)
- Todos los campos de tarea deben ser JSON-serializables

### RNF-003: Manejo de errores
- Errores de validación (400) deben incluir campo específico y motivo
- Errores internos (500) no deben exponer detalles de implementación
- Mensajes de error orientados al usuario en español
- Logs internos pueden estar en inglés para debugging

### RNF-004: Compatibilidad
- Node.js LTS (mínimo v18)
- TypeScript strict mode
- SQLite 3.x via better-sqlite3 o similar
- Express.js o framework HTTP compatible

## 4. Modelo de datos SQLite

### Tabla: tasks
```sql
CREATE TABLE tasks (
  id INTEGER PRIMARY KEY AUTOINCREMENT,  -- o TEXT si usa UUID
  title TEXT NOT NULL CHECK(length(title) <= 255 AND length(title) > 0),
  description TEXT CHECK(description IS NULL OR length(description) <= 1000),
  status TEXT DEFAULT 'pending' CHECK(status IN ('pending', 'in-progress', 'completed')),
  priority TEXT DEFAULT 'medium' CHECK(priority IN ('low', 'medium', 'high')),
  created_at TEXT NOT NULL DEFAULT (datetime('now')),
  updated_at TEXT NOT NULL DEFAULT (datetime('now'))
);
```

### Índices recomendados
- Index en created_at para ordenamiento por defecto
- Index en status para filtros futuros (si se implementan)

## 5. Especificación de Endpoints

### POST /tasks
**Request Body:**
```json
{
  "title": "string (required, 1-255 chars)",
  "description": "string (optional, max 1000 chars)", 
  "status": "enum (optional: pending|in-progress|completed)",
  "priority": "enum (optional: low|medium|high)"
}
```

**Response 201:**
```json
{
  "id": "1",
  "title": "Comprar leche",
  "description": "Leche entera del supermercado",
  "status": "pending",
  "priority": "medium",
  "createdAt": "2026-05-27T12:00:00Z",
  "updatedAt": "2026-05-27T12:00:00Z"
}
```

### GET /tasks
**Response 200:**
```json
[
  {
    "id": "1",
    "title": "Comprar leche", 
    "description": "Leche entera del supermercado",
    "status": "pending",
    "priority": "medium",
    "createdAt": "2026-05-27T12:00:00Z",
    "updatedAt": "2026-05-27T12:00:00Z"
  }
]
```

### GET /tasks/:id
**Response 200:** Misma estructura que POST /tasks
**Response 404:**
```json
{
  "error": "Tarea no encontrada"
}
```

### PUT /tasks/:id
**Request Body:** Mismo que POST /tasks (todos los campos opcionales)
**Response 200:** Tarea actualizada con updatedAt modificado
**Response 404:** Mismo que GET

### DELETE /tasks/:id  
**Response 204:** Sin contenido
**Response 404:** Mismo que GET

## 6. Validaciones y Casos de Error

### Errores de validación (400)
```json
{
  "error": "Error de validación",
  "details": {
    "title": "El campo title es obligatorio",
    "status": "El valor debe ser: pending, in-progress o completed"
  }
}
```

### Error interno (500)
```json
{
  "error": "Error interno del servidor"
}
```

## 7. Pruebas Manuales del Desarrollador (OBLIGATORIO — marcar [x] antes de /flow-close)

| ID | Caso / Flujo | Pasos resumidos | Resultado esperado | [x] |
|----|-------------|-----------------|-------------------|-----|
| PM-1 | Happy path CRUD completo | 1. POST /tasks con tarea válida<br>2. GET /tasks verificar aparece<br>3. PUT /tasks/:id modificar título<br>4. DELETE /tasks/:id eliminar | Cada operación responde con código HTTP correcto y datos esperados | [ ] |
| PM-2 | Validaciones de error | 1. POST /tasks sin title<br>2. GET /tasks/999 (inexistente)<br>3. PUT /tasks/999 (inexistente) | Respuestas 400/404 con mensajes en español | [ ] |
| PM-3 | Persistencia SQLite | 1. Crear 2 tareas via API<br>2. Reiniciar servidor<br>3. GET /tasks verificar datos | Las tareas persisten después del reinicio | [ ] |
| PM-4 | Tests automatizados | 1. npm test<br>2. Verificar coverage reports | Todos los tests pasan, coverage > 80% en endpoints CRUD | [ ] |