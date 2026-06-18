# Plan: CRUD de tareas

## 1. Análisis de Impacto y Dependencias

**Proyecto nuevo**: No hay código existente que modificar. Se construirá la aplicación completa desde cero.

**Dependencias nuevas requeridas**:
- Node.js LTS (runtime)
- TypeScript + tipos (@types/node)
- Express.js (framework HTTP)
- better-sqlite3 (driver SQLite)
- vitest (testing framework)
- supertest + @types/supertest (tests de integración HTTP)
- cors, helmet (middlewares básicos de seguridad)

**Estructura de directorios a crear**:
- `src/` — código fuente TypeScript
- `src/models/` — definiciones de tipos y DTOs
- `src/database/` — conexión SQLite y esquema
- `src/routes/` — endpoints REST
- `src/services/` — lógica de negocio
- `src/middleware/` — validaciones y manejo de errores
- `tests/` — tests unitarios e integración
- `data/` — archivo SQLite (gitignored)

## 2. Modificaciones de Archivos (Proposed Changes)

### Archivos base del proyecto
- [NEW] `package.json` — Dependencias, scripts npm, configuración TypeScript
- [NEW] `tsconfig.json` — Configuración TypeScript strict mode
- [NEW] `vitest.config.ts` — Configuración del framework de testing
- [NEW] `.gitignore` — Excluir node_modules, data/, build/
- [NEW] `README.md` — Documentación del API con ejemplos de uso

### Estructura del código fuente
- [NEW] `src/app.ts` — Aplicación Express principal, configuración middlewares
- [NEW] `src/server.ts` — Punto de entrada, inicialización de base de datos
- [NEW] `src/models/Task.ts` — Interface Task, enums Status/Priority
- [NEW] `src/database/connection.ts` — Conexión SQLite con better-sqlite3
- [NEW] `src/database/schema.ts` — Creación tabla tasks, índices
- [NEW] `src/services/TaskService.ts` — Lógica CRUD, validaciones
- [NEW] `src/routes/taskRoutes.ts` — Endpoints REST /tasks
- [NEW] `src/middleware/validation.ts` — Validador de entrada JSON
- [NEW] `src/middleware/errorHandler.ts` — Manejo centralizado de errores

### Tests
- [NEW] `tests/unit/TaskService.test.ts` — Tests unitarios lógica de negocio
- [NEW] `tests/integration/taskRoutes.test.ts` — Tests supertest endpoints
- [NEW] `tests/helpers/testDatabase.ts` — Utilidad base de datos en memoria para tests

## 3. Contratos y Estructuras (Esquemas)

### Interface Task (TypeScript)
```typescript
interface Task {
  id: number;  // auto-increment SQLite
  title: string;  // required, 1-255 chars
  description?: string;  // optional, max 1000 chars
  status: TaskStatus;  // enum
  priority: TaskPriority;  // enum
  createdAt: string;  // ISO 8601
  updatedAt: string;  // ISO 8601
}

enum TaskStatus {
  PENDING = 'pending',
  IN_PROGRESS = 'in-progress',
  COMPLETED = 'completed'
}

enum TaskPriority {
  LOW = 'low',
  MEDIUM = 'medium',
  HIGH = 'high'
}
```

### Esquema SQLite
```sql
CREATE TABLE tasks (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  title TEXT NOT NULL CHECK(length(title) <= 255 AND length(title) > 0),
  description TEXT CHECK(description IS NULL OR length(description) <= 1000),
  status TEXT DEFAULT 'pending' CHECK(status IN ('pending', 'in-progress', 'completed')),
  priority TEXT DEFAULT 'medium' CHECK(priority IN ('low', 'medium', 'high')),
  created_at TEXT NOT NULL DEFAULT (datetime('now')),
  updated_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX idx_tasks_created_at ON tasks(created_at);
CREATE INDEX idx_tasks_status ON tasks(status);
```

### API Endpoints Signature
```typescript
// POST /tasks
createTask(body: CreateTaskRequest): Promise<TaskResponse>

// GET /tasks  
getAllTasks(): Promise<TaskResponse[]>

// GET /tasks/:id
getTaskById(id: number): Promise<TaskResponse>

// PUT /tasks/:id
updateTask(id: number, body: UpdateTaskRequest): Promise<TaskResponse>

// DELETE /tasks/:id
deleteTask(id: number): Promise<void>
```

### Error Response Structure
```typescript
interface ErrorResponse {
  error: string;
  details?: Record<string, string>;  // campo -> mensaje validación
}
```

## 4. Checklist de Implementación

### Fase 1: Infraestructura Base
- [x] 1.1 Crear package.json con dependencias y scripts npm
- [x] 1.2 Configurar TypeScript (tsconfig.json)
- [x] 1.3 Configurar vitest para testing
- [x] 1.4 Crear .gitignore básico

### Fase 2: Modelos y Base de Datos
- [x] 2.1 Definir interfaces Task, enums Status/Priority en src/models/Task.ts
- [x] 2.2 Crear conexión SQLite en src/database/connection.ts
- [x] 2.3 Crear schema.ts con tabla tasks e índices
- [x] 2.4 Implementar inicialización de DB en arranque

### Fase 3: Lógica de Negocio
- [x] 3.1 Implementar TaskService.ts con métodos CRUD
- [x] 3.2 Agregar validaciones de entrada (title obligatorio, longitudes, enums)
- [x] 3.3 Implementar manejo de timestamps (createdAt/updatedAt)
- [x] 3.4 Tests unitarios de TaskService

### Fase 4: Capa HTTP
- [x] 4.1 Crear middleware de validación JSON
- [x] 4.2 Crear middleware de manejo de errores centralizado
- [x] 4.3 Implementar rutas REST en taskRoutes.ts
- [x] 4.4 Configurar app Express con middlewares
- [x] 4.5 Crear server.ts como punto de entrada

### Fase 5: Testing e Integración
- [x] 5.1 Tests de integración con supertest (todos los endpoints)
- [x] 5.2 Tests casos de error (404, 400, validaciones)
- [x] 5.3 Verificar persistencia SQLite entre reinicios
- [x] 5.4 Ejecutar suite completa de tests

### Fase 6: Documentación
- [x] 6.1 Escribir README.md con ejemplos de API
- [x] 6.2 Documentar scripts npm (dev, test, build)
- [x] 6.3 Verificar cumplimiento de casos PM-* del spec

## 5. Definición de Completitud por Tarea

### Scripts npm requeridos
```json
{
  "scripts": {
    "dev": "tsx watch src/server.ts",
    "start": "node dist/server.js", 
    "build": "tsc",
    "test": "vitest run",
    "test:watch": "vitest"
  }
}
```

### Criterio de Done por fase
- **Fase 1**: package.json válido, `npm install` exitoso
- **Fase 2**: Schema SQLite creado, conexión funcional
- **Fase 3**: Tests unitarios TaskService al 100%
- **Fase 4**: Todos los endpoints responden correctamente
- **Fase 5**: Suite de tests pasa, coverage > 80%
- **Fase 6**: README con ejemplos funcionales, casos PM-* verificados

### Dependencias críticas
- Fase 2 requiere Fase 1 (package.json)
- Fase 3 requiere Fase 2 (modelos y DB)
- Fase 4 requiere Fase 3 (lógica de negocio)
- Fase 5 requiere Fase 4 (endpoints implementados)
- Fase 6 puede ejecutarse en paralelo con Fase 5

## 6. Estrategia de Testing

### Tests unitarios (vitest)
- TaskService.createTask() con validaciones
- TaskService.getAllTasks() con datos vacíos/poblados
- TaskService.getById() con IDs existentes/inexistentes
- TaskService.updateTask() con campos válidos/inválidos
- TaskService.deleteTask() con confirmación de eliminación

### Tests de integración (supertest)
- POST /tasks: casos happy path y validaciones (400)
- GET /tasks: respuesta array vacío/poblado
- GET /tasks/:id: 200 vs 404
- PUT /tasks/:id: actualización y 404
- DELETE /tasks/:id: 204 vs 404
- Verificar Content-Type: application/json en todas las respuestas

### Base de datos de test
- SQLite en memoria (:memory:) para tests rápidos
- Reset completo entre tests para aislamiento
- Fixtures con datos de prueba consistentes

## 7. Consideraciones de Arquitectura

### Separación de responsabilidades
- **Models**: Definiciones de tipos, sin lógica
- **Database**: Conexión y schema, sin business logic
- **Services**: Lógica CRUD pura, independiente de HTTP
- **Routes**: Adaptadores HTTP, delegación a services
- **Middleware**: Cross-cutting concerns (validación, errores)

### Error handling
- Errores de validación → 400 con detalles específicos
- Recursos no encontrados → 404 con mensaje en español
- Errores internos → 500 sin exposición de detalles
- Logging interno en inglés para debug

### Performance considerations
- Índices en created_at para orden por defecto
- Índice en status para filtros futuros
- Connection pooling no requerido (SQLite file-based)
- Límites de validación razonables (255/1000 chars)

---

**Resumen**: 6 fases, 24 tareas totales, dependencias claras. Ruta crítica: Infraestructura → DB → Business Logic → HTTP → Testing. Tiempo estimado de implementación: 4-6 horas de desarrollo enfocado.
<!-- checklist auto-mark: 2026-05-27T18:15:15.697Z (22 items by mark-plan-checklist) -->
