# Context Map — ff-003-onboarding-flow

Fecha: 2026-08-14
Feature-slug: `ff-003-onboarding-flow`
Agente: forge-discovery (Fase 0)
Status: **CLEAR** — contexto suficiente para forge-arch (CKP-1)

---

## 1) Resumen ejecutivo

El feature agrega un comando `flowforge onboard` al installer (`src/FlowForge.Installer/`,
binario AOT C# .NET 10) que genera un briefing de onboarding para nuevos desarrolladores
a partir de las memorias de engram-dotnet del proyecto.

**Hallazgos críticos del discovery:**

1. **⚠️ DUPLICACIÓN CON ENG-485 (engram-dotnet)**: el repo engram-dotnet ya tiene ENG-485
   ("Onboarding flow para teams", P2, Idea, tamaño L) con HU-055 que especifica `engram onboard
   --user <handle>` con `--format markdown|json`, `--days`, `--output`. FF-003 propone lo mismo
   en el lado FlowForge. **El spec preliminar de FF-003 no menciona ENG-485.** El arquitecto DEBE
   decidir la frontera de responsabilidad: ¿quién genera el briefing? (ver §9 Recomendaciones).

2. **⚠️ `mem_timeline` NO sirve para "recent activity"**: en el código real de engram-dotnet,
   `mem_timeline(observation_id, before, after)` requiere un ID de observación como ancla — es
   drill-down de una observación específica, NO actividad reciente del proyecto. El spec
   preliminar lo usa incorrectamente. Para "actividad reciente" la herramienta correcta es
   `mem_context` (últimas 5 sesiones + 20 observaciones + 10 prompts).

3. **⚠️ `type:convention` NO existe como tipo de observación**: los types canónicos de mem_search
   son `tool_use, file_change, command, file_read, search, manual, decision, architecture, bugfix,
   pattern`. "convention" se normaliza a `pattern` (topic family) — las convenciones del equipo
   se guardan como `type=pattern` con contenido de convención, o como `manual`. El briefing debe
   buscar `type=pattern` + keyword "convention", no `type=convention`.

4. **Namespacing de proyectos**: en modo sync/team, los proyectos se guardan como `team/{nombre}`
   (ej. `team/flowforge`, `team/engram-dotnet`). El CLI `engram search --project flowforge` NO
   matchea `team/flowforge`. El comando debe resolver el nombre con prefijo de scope o usar
   `mem_current_project`/ProjectDetector.

5. **El installer NO tiene cliente HTTP hacia engram hoy**: solo configura MCP en los IDEs
   (EngramModule.ConfigureMcp). Para `onboard` hay que elegir vía de integración: CLI subprocess
   (`engram search/context/stats` — texto plano, sin --json en estos subcomandos), HTTP API del
   server de sync (JSON estructurado, requiere server corriendo), o MCP stdio (no apropiado para CLI).

6. **AOT constraint**: el installer compila con `PublishAot=true`, `TrimMode=full`,
   `InvariantGlobalization=true` — JSON con source-gen obligatorio, sin reflection, sin paquetes
   no AOT-safe. `System.Diagnostics.Process` SÍ es usable (ya se usa en EngramProcessChecker).

7. **ADR-017 Installer Protection Policy aplica**: cualquier cambio a `src/FlowForge.Installer/*`
   requiere `installer-baseline.md` + tests de regresión + composición sobre reemplazo.

---

## 2) Mapa de componentes involucrados

### 2.1 FlowForge installer (proyecto bajo cambio: FlowForge)

| Componente | Rol |
|-----------|-----|
| `src/FlowForge.Installer/Program.cs` | Routing CLI via ConsoleAppFramework 5.7.13. Registra comandos: `install`, `doctor`, `init`, `update`, `status`, `uninstall`, `config`. **Aquí se registra `onboard`.** |
| `src/FlowForge.Installer/Commands/` | Comandos CAF. `OnboardCommand.cs` sería el nuevo archivo (patrón: primary constructor con `InstallerContext`). |
| `InstallerContext` (Program.cs) | DI record: `Log`, `Store`, `GitHub`, `Manifest`. Para onboard se necesita además acceso al binario engram (`PathHelper.EngramBinary`) y a la config de sync (`ConfigStore`). |
| `src/FlowForge.Installer/Infrastructure/PathHelper.cs` | `EngramBinary` (ruta binario), `EngramDir`, `ConfigFile` (`~/.engram/config.json`). |
| `src/FlowForge.Installer/Infrastructure/ConfigStore.cs` | Lee/escribe `~/.engram/config.json` con read-modify-write atómico. Contiene `sync.{mode, remote_url, user, data_dir}`. **Fuente de la URL del server y user.** |
| `src/FlowForge.Installer/Models/InstallerConfig.cs` | `SyncConfig` (mode, remote_url, user, data_dir), `ComponentsConfig.EngramDotnet.Binary`. |
| `src/FlowForge.Installer/Update/EngramProcessChecker.cs` | Patrón existente de interacción con procesos engram (usa `Process`). |
| `src/FlowForge.Installer/Modules/EngramModule.cs` | Configura MCP por IDE (no invoca memorias). Referencia de cómo se resuelve user: `ENGRAM_USER` → `Environment.UserName`. |
| `.flowforge.json` (proyecto) | `engram.project` (ej. "flowforge") + `docs_framework: flowdoc`. Template de init crea `engram.enabled: true, engram.project: {projectName}`. |

### 2.2 engram-dotnet (dependencia externa, repo vecino)

| Componente | Rol |
|-----------|-----|
| `src/Engram.Mcp/EngramTools.cs` | 25 herramientas MCP. Signatures verificadas en §3. |
| `src/Engram.Cli/Program.cs` | CLI directa: `search`, `context`, `stats`, `projects list`, `project id`, `doctor`, `sync status --json`, `project id --json`. **search/context/stats NO tienen --json.** |
| `src/Engram.Store/SqliteStore.cs` | `FormatContextAsync` (sesiones+obs+prompts), `SearchAsync` (FTS5), `TimelineAsync`. |
| `src/Engram.Store/ProjectDetector.cs` | Algoritmo de 5 casos: git_remote → git_root → git_child → ambiguous → dir_basename. `DetectProjectFull` devuelve project + source + warning + available_projects. |
| `src/Engram.Store/Normalizers.cs` | `NormalizeProject` (lowercase, guiones), `SuggestTopicKey` (family: decision/pattern/bug/config/discovery/learning). |
| `src/Engram.Server/EngramServer.cs` | HTTP API JSON: `/health`, `/search`, `/context`, `/stats`, `/timeline`, `/sessions/recent`, `/observations/recent`, `/projects/list`. Header `X-Engram-User`. |
| `src/Engram.Store/Models.cs` | `Observation` (id, type, title, content, project, scope, topic_key, created_at...), `SearchResult { observation, rank }`. |

### 2.3 Config real del entorno (2026-08-14)

- Binario: `/home/victor/.local/bin/engram` v1.0.0, `/home/victor/.local/bin/flowforge`.
- `~/.engram/config.json`: `sync.mode=sync`, `sync.remote_url=http://192.168.0.178:7437`,
  `sync.user=victor@local.dev`, `data_dir=/home/victor/.engram`.
- Server de sync respondiendo: `{"status":"ok","service":"engram","version":"1.1.0","backend":"postgres"}`
  — stats: 265 sessions, 558 observations, 544 prompts, projects incl. `team/flowforge`, `team/engram-dotnet`.
- DB local SQLite: 45 sessions, 65 observations (proyectos: team/flowforge, team/engram-dotnet, dawilgestion, test/verify...).

---

## 3) Herramientas MCP disponibles (verificadas en código fuente de engram-dotnet)

### 3.1 `mem_context`
```csharp
[McpServerTool(Name = "mem_context")]
string MemContext(
    string? project = null,   // filtrar por proyecto (omitir = todos)
    string? scope = null,     // team | personal | ambos
    int limit = 20)           // nº observaciones
```
**Retorna**: texto `## Memory from Previous Sessions` con:
- `### Recent Sessions` — últimas 5 (proyecto, fecha, summary, nº obs)
- `### Recent User Prompts` — últimos 10
- `### Recent Observations` — últimas 20 (type, title, content truncado 300)
- Footer con stats: `Memory stats: N sessions, N observations across projects: ...`
- Wide-read: en modo team con user, lee `team/{project}` + `{user}/{project}` simultáneamente.
- Si no hay nada: `No previous session memories found.`

### 3.2 `mem_search`
```csharp
[McpServerTool(Name = "mem_search")]
string MemSearch(
    string query,                          // natural language o keywords
    string? type = null,                   // tool_use|file_change|command|file_read|search|manual|decision|architecture|bugfix|pattern
    string? project = null,                // nombre de proyecto (ojo: con scope team se resuelve team/{p})
    string? scope = null,                  // team | personal | omitir = ambos
    int limit = 10)                        // clamp: 1..20
```
**Retorna**: `Found N memories:` con `[i] #id (type) — title`, preview truncado a **300 chars** con
marca `[preview]`, `createdAt | project | scope`. **SIEMPRE llamar `mem_get_observation(id)` para
contenido completo** (previews truncados). Sin resultados: `No memories found for: "query"`.
- Search cross-namespace si scope=null y hay user: busca `team/{project}` + `{user}/{project}`.

### 3.3 `mem_timeline` ⚠️ (NO es "recent activity")
```csharp
[McpServerTool(Name = "mem_timeline")]
string MemTimeline(
    long observation_id,     // REQUERIDO — ancla (de mem_search results)
    int before = 5,
    int after = 5)
```
**Retorna**: timeline cronológico alrededor de una observación específica (Session info, Before,
focus `>>>`, After). **Requiere un ID existente** — no sirve como consulta de actividad reciente
del proyecto. Para "última actividad" usar `mem_context`.

### 3.4 `mem_stats`
```csharp
[McpServerTool(Name = "mem_stats")]
string MemStats()
```
**Retorna**: `Memory System Stats: Sessions, Observations, Prompts, Projects`.

### 3.5 `mem_get_observation`
```csharp
[McpServerTool(Name = "mem_get_observation")]
string MemGetObservation(long id)
```
**Retorna**: contenido COMPLETO + session, project, scope, topic_key, tool, duplicates, revisions, created.

### 3.6 `mem_current_project`
```csharp
[McpServerTool(Name = "mem_current_project")]
string MemCurrentProject(string? workingDir = null)
```
**Retorna JSON**: `{ project, project_source, project_id, project_path, cwd, available_projects, warning }`.
Fuente de detección: git_remote | git_root | git_child | ambiguous | dir_basename.
**Recomendado como paso 0 del onboarding** (equivale a ProjectDetector del installer, sin reimplementar).

### 3.7 CLI directa equivalente (sin MCP)

| Comando CLI | Options | Salida |
|------------|---------|--------|
| `engram search <query>` | `--type`, `--project`, `--scope`, `--limit` (default 10) | **Texto plano** (sin --json) |
| `engram context [project]` | `--scope` | **Texto plano** (mismo formato que mem_context) |
| `engram stats` | — | **Texto plano** |
| `engram projects list` | — | Texto tabla |
| `engram project id` | `--json` | JSON (GUID) |
| `engram sync status` | `--json` | JSON |
| `engram doctor` | — | Healthchecks |

### 3.8 HTTP API del server de sync (JSON estructurado)

| Endpoint | Params | Retorna |
|----------|--------|---------|
| `GET /health` | — | `{status, service, version, backend}` |
| `GET /search?q=&type=&project=&scope=&limit=` | q requerido | `[SearchResult{observation, rank}]` JSON completo |
| `GET /context?project=&scope=` | — | `{context: "<markdown>"}` |
| `GET /stats` | — | `{total_sessions, total_observations, total_prompts, projects, backend}` |
| `GET /timeline?observation_id=&before=&after=` | observation_id requerido | JSON TimelineResult |
| `GET /sessions/recent`, `GET /observations/recent` | project, limit | JSON |
| `GET /projects/list` | — | JSON |

Header de aislamiento multi-usuario: `X-Engram-User`.

---

## 4) Tipos de observación reales (verificado en código + datos)

**Types canónicos** (Description de mem_search / mem_save):
`tool_use, file_change, command, file_read, search, manual, decision, architecture, bugfix, pattern, config, discovery, learning`

**Types observados en datos reales** (`engram projects list` + searches):
`decision, pattern, metrics, session_summary, session, session_close, manual`

**Implicaciones para el spec**:
- `type:decision` ✅ existe — para "Top architectural decisions".
- `type:pattern` ✅ existe — para "Reusable patterns" Y "Team conventions" (las convenciones se guardan como pattern; `SuggestTopicKey` mapea `convention` → family `pattern`).
- `type:convention` ❌ NO es tipo de observación — el spec preliminar debe corregirlo.
- `type:architecture` ✅ existe (sin uso frecuente en datos actuales).
- `type:blocker` / `type:gotcha` / `type:insight` — **no canónicos**; HU-055 de ENG-485 los asume, pero en la práctica habría que buscarlos por keywords o guardarlos como `manual`/`bugfix`.

---

## 5) Detección de proyecto actual (verificado)

Cadena de resolución en engram-dotnet (`ProjectDetector.DetectProjectFull`):
1. `git remote get-url origin` → repo name (SSH/HTTPS, strip `.git`)
2. Git root basename
3. Único child git repo
4. **Ambiguous** (≥2 child repos) → `project=""`, `available_projects=[...]`, warning
5. Fallback: dir basename
+ Normalización: lowercase, `_`/espacios → `-`.

En FlowForge installer: `.flowforge.json` → `engram.project` (creado por `flowforge init`).
Nota: `.flowforge.json` del proyecto FlowForge tiene `engram.project: "flowforge"` — coincide con el
repo name de git. **Recomendación**: usar `mem_current_project` (o el algoritmo equivalente) y luego
permitir `--project` override. Para proyectos con scope team, resolver como `team/{project}`.

---

## 6) Memorias relacionadas (búsqueda en .engram/local_memory + engram search)

- **`obs-20260812-session-close-update-mechanism.md`**: cierre de `flowforge-update-mechanism`
  (PR #24 MERGED). Documenta: 2 bugs de pérdida de datos MCP (S6), stub traps, baseline ADR-017,
  version drift. **Relevante**: cualquier cambio al installer debe respetar ADR-016/017.
- **Memoria #112 (decision, personal)**: "Prioritized Feature Backlog" — FF-003 (Onboarding, P1),
  FF-002A (Decision extraction, P2), FF-007 (Drift check, P3). **4 features bloqueadas por
  engram-dotnet (ENG-416 schema evolution).** Guardada 2026-08-14.
- **Memoria #97 (decision, team/flowforge)**: update mechanism por componente.
- **Memoria #32 (pattern, team/flowforge)**: "EN linked: agent changes needed for auto-enroll" —
  ejemplo de cross-link FlowForge ↔ engram-dotnet.
- **No existen memorias previas de "onboarding" en local_memory** (grep: 0 resultados).
- **ENGRAM-485 / HU-055** (engram-dotnet/docs/tasks/HU-055-onboarding-flow.md): el feature gemelo
  del lado engram. Lee: Top 10 Architectural Decisions, Active Conventions, Known Blockers/Gotchas,
  Recent Insights (30 días), Where to Start. Fuera de alcance: UI web, personalización por rol.

### Features relacionados en backlog FlowForge

| Feature | Status | Relación |
|---------|--------|----------|
| FF-001 (code-aware dev agent) | 🔴 Blocked (ENG-484) | Onboarding podría surfacear memorias code-aware si ENG-484 está listo |
| FF-002 (decision extraction) | 🟡 Parcial / 🔴 Blocked | Onboarding se beneficia de decisiones capturadas (type:decision) |
| FF-005 (contradiction detection) | 🔴 Blocked | Onboarding podría advertir decisiones contradictorias (futuro) |
| FF-004 (code-context arch) | — | Ver en backlog |

### Dependencias engram-dotnet (opcionales)

- **ENG-480** (Quick-capture CLI) — Idea, S-M. Más memorias → mejor briefing.
- **ENG-481** (Git hooks) — Idea, S-M. Captura automática → briefing más rico.
- **ENG-485** (onboard en engram) — Idea, L. **Feature gemela — coordinar frontera.**

---

## 7) FlowDoc context

- PRD: `docs/PRD.md` (leído: sí, secciones 1) — FlowForge "One command. Any stack. Any team."
- Feature referenciado: FF-003 en `docs/backlog/FF-003-onboarding-flow/spec.md` (leído completo).
- HU: no aplica — el backlog FlowForge usa FF-* (FF-BACKLOG.md), no HU-* bajo docs/tasks (dir vacío).
- `.flowforge.json`: `docs_framework: "flowdoc"`, `docs_framework_version: "2.0"`, `engram.project: "flowforge"`.

---

## 8) Reusable Patterns Found (MANDATORIO — paso 5 del skill)

Búsqueda ejecutada en el proyecto bajo cambio (FlowForge installer) y engram-dotnet:

1. **`src/FlowForge.Installer/Commands/DoctorCommand.cs`** — patrón de lista de checks con
   `(Name, Func<Task<(bool, string?)>>)` + tabla Spectre + exit codes (0/1/2). **Reusable** para
   el pre-check "¿engram instalado? ¿MCP configurado? ¿server reachable?" del onboarding.
2. **`src/FlowForge.Installer/Update/EngramProcessChecker.cs`** — patrón `Process` cross-platform
   para detectar engram corriendo. **Reusable** (o extensible) para invocar `engram search/context`
   como subprocess con `RedirectStandardOutput`.
3. **`src/FlowForge.Installer/Infrastructure/ConfigStore.cs`** — read-modify-write atómico de
   `~/.engram/config.json`. **Reusable** para leer `sync.remote_url` + `sync.user` (target del onboard).
4. **`src/FlowForge.Installer/Models/InstallerConfig.cs`** — `SyncConfig` + `InstallerJsonContext`
   (source-gen JSON). **Reusable** para deserializar la config en el nuevo comando.
5. **`src/FlowForge.Installer/Program.cs`** — registro de comando CAF + `InstallerContext` DI.
   **Clonable**: `app.Add<OnboardCommand>("onboard")` + añadir el cliente al contexto.
6. **engram-dotnet `src/Engram.Store/ProjectDetector.cs`** — algoritmo de detección de proyecto
   (5 casos, normalize). **Reusable** vía `mem_current_project` (evita reimplementar en C#).
7. **engram-dotnet `src/Engram.Server/EngramServer.cs`** — HTTP API JSON `/search`, `/context`,
   `/stats`, `/projects/list`. **Reusable** como vía de integración JSON si el server corre.
8. **engram-dotnet `src/Engram.Store/SqliteStore.cs` `FormatContextAsync`** — el formato de briefing
   "Recent Sessions / Recent Observations" ya existe; el onboarding puede reutilizarlo vía
   `mem_context` en lugar de construir el agregado desde cero.

Resultado: **no es greenfield** — existen ≥6 patrones reutilizables directos. La lógica de briefing
puede componerse de `mem_context` + `mem_search(type=decision/pattern)` + `mem_stats`.

---

## 9) Dependencias identificadas

| Dependencia | Tipo | Detalle |
|-------------|------|---------|
| Binario engram instalado | Runtime | `PathHelper.EngramBinary`. Si falta → mensaje: "Instalá con `flowforge install`". |
| MCP configurado | Runtime | `DoctorCommand.CheckMcp` ya verifica Cursor/OpenCode. Reutilizable. |
| Server de sync | Opcional | Para HTTP API JSON. Si no corre → fallback a CLI subprocess local (SQLite). |
| User identity | Config | `~/.engram/config.json sync.user` o `ENGRAM_USER`. El spec preliminar usa `--user "victor@team.dev"` — pero el user ya está persistido por `flowforge install`/`sync connect`. **Simplificación**: tomar de config, `--user` como override. |
| Memorias del proyecto | Data | 14 obs en `team/flowforge` (real). Briefing pobre si <10. |
| ENG-480/481 (captura) | Opcional | Mejoran la calidad del briefing, no bloquean. |
| ENG-485 (onboard en engram) | ⚠️ Coordinación | Feature gemela — riesgo de duplicación. |
| ADR-017 / ADR-016 | Proceso | installer-baseline.md + regression tests + composición. |
| AOT | Técnica | source-gen JSON, sin reflection, Process OK. |

---

## 10) Riesgos y mitigaciones

| # | Riesgo | Prob | Impacto | Mitigación |
|---|--------|------|---------|------------|
| R1 | **Duplicación con ENG-485/HU-055** (engram-dotnet ya planea `engram onboard`) | Alta | Alto | Definir frontera en spec: FlowForge = detección de proyecto + CLI UX + ONBOARDING.md; engram = agregación de memorias (vía `mem_context`/`mem_search`/HTTP, o delegar a ENG-485 cuando exista). NO implementar lógica de ranking de memorias en el installer. |
| R2 | `mem_timeline` mal usado en spec preliminar (requiere observation_id) | Alta | Medio | Corregir en spec: actividad reciente = `mem_context`; `mem_timeline` solo para drill-down de una observación elegida. |
| R3 | `type:convention` inexistente → briefing con 0 resultados | Alta | Medio | Buscar `type=pattern` + keywords ("convention", "naming", "style") y `type=decision` para decisiones. |
| R4 | Namespacing `team/{project}` rompe búsquedas | Media | Alto | Resolver `team/` prefix según scope; usar `mem_current_project` para detectar; test con `team/flowforge` real. |
| R5 | CLI engram sin `--json` (search/context/stats) → parseo frágil | Media | Medio | Preferir HTTP API (`/search`, `/context`, `/stats`) si server disponible; CLI como fallback parseando el formato estable `[i] #id (type) — title`. |
| R6 | Sin memorias del proyecto (equipo nuevo) | Media | Medio | Detección temprana: `mem_stats`/`mem_context` vacío → mensaje "No memories found. Start with `flowforge install` + methodology" (spec ya lo prevé). |
| R7 | Server no corriendo en modo sync | Media | Bajo | Fallback a SQLite local vía CLI; avisar "usando memorias locales". |
| R8 | Demasiadas memorias (overwhelming) | Baja | Medio | Límites: top 10 decisiones, top 10 patrones, últimas 5 sesiones; drill-down interactivo (spec ya lo prevé). |
| R9 | AOT: dependencia no AOT-safe | Baja | Alto | Solo BCL + paquetes ya usados (CAF, Spectre, DI). JSON vía source-gen contexts. |
| R10 | Regresión en installer (ADR-017) | Media | Alto | installer-baseline.md + re-run regression tests (install/status/doctor/uninstall) antes y después. |
| R11 | Memoria #112: "4 features bloqueadas por ENG-416 (schema evolution)" | Media | Medio | Verificar si ENG-416 afecta search por type — hoy NO (types son strings libres sin CHECK constraint). Monitorear. |

---

## 11) Casos de uso detallados

### CU-1: Nuevo desarrollador, primer día (objetivo principal)
- **Actor**: nuevo dev en un equipo que ya usa FlowForge + engram (modo sync).
- **Trigger**: `flowforge onboard` (o `/flow-onboard` futuro) después de `flowforge install`.
- **Flujo**: detecta proyecto (`mem_current_project` / `.flowforge.json`/git) → `mem_context(project)`
  (5 sesiones + 20 obs) → `mem_search(type=decision, limit=10)` → `mem_search(type=pattern, limit=10)`
  → briefing CLI interactivo → opcional `--output ONBOARDING.md`.
- **Éxito**: en 5 minutos identifica 3+ decisiones clave y convenciones del proyecto.

### CU-2: Team lead prepara onboarding para un nuevo miembro
- **Actor**: team lead con acceso al server de sync.
- **Trigger**: `flowforge onboard --user nuevo@team.dev --project team/foo --output ONBOARDING.md`.
- **Flujo**: mismo agregado de memorias, output markdown versionable en el repo (docs/).
- **Nota**: `--user` en el spec es el handle del nuevo dev — pero engram aisla por `X-Engram-User`/
  `ENGRAM_USER` (quien corre el comando), no por el destinatario. El `--user` del briefing es
  informativo/display, no filtra memorias por destinatario.

### CU-3: Dev que cambia de proyecto dentro del mismo equipo
- **Actor**: dev con 3 proyectos FlowForge (open question #2 del spec).
- **Trigger**: `flowforge onboard --project otro-proyecto`.
- **Flujo**: `mem_current_project` detecta ambigüedad → listar `available_projects` → elegir.
- **Éxito**: briefing del proyecto correcto sin mezclar memorias.

### CU-4: Equipo sin engram / sin memorias
- **Trigger**: `flowforge onboard` sin engram instalado o sin memorias.
- **Resultado**: diagnóstico claro (patrón DoctorCommand) + guía de primeros pasos
  ("Instalá engram, corré `flowforge install`, usá la metodología para capturar decisiones").

### CU-5: Drill-down de una decisión
- **Actor**: nuevo dev que quiere el detalle completo de la decisión #97.
- **Flujo**: briefing lista `[DECISION] #97` → el dev selecciona → `mem_get_observation(97)` muestra
  contenido completo (+ opcional `mem_timeline(97)` para ver contexto temporal).
- **Nota**: este es el único uso correcto de `mem_timeline`.

---

## 12) Recomendaciones para forge-arch

1. **Definir frontera FF-003 ↔ ENG-485** en el spec (decisión explícita, con justificación).
   Opción recomendada: FF-003 implementa el comando `flowforge onboard` (detección de proyecto,
   orquestación de `mem_context`/`mem_search`/`mem_stats`, briefing CLI + ONBOARDING.md), y DELEGA
   la agregación/ranking a engram vía MCP/HTTP. Si ENG-485 se implementa antes, `flowforge onboard`
   lo envuelve. **No duplicar lógica de ranking de memorias en el installer.**
2. **Corregir el spec preliminar**:
   - `mem_timeline` → solo drill-down por observation_id; actividad reciente = `mem_context`.
   - `type:convention` → `type=pattern` + keywords.
   - Considerar namespacing `team/{project}` en el diseño de `--project`.
3. **Vía de integración preferida**: HTTP API del server de sync (JSON) con fallback a CLI local.
   El server ya está corriendo en el entorno (postgres, v1.1.0). Esto evita parsear texto.
4. **Añadir `mem_current_project` como paso 0** del flujo (detección de proyecto canónica,
   incluye warning de ambigüedad y available_projects).
5. **User**: tomar de `~/.engram/config.json sync.user` (o `ENGRAM_USER`); `--user` = override
   informativo (display en el briefing), no filtro de datos (engram filtra por quien ejecuta).
6. **Interfaz CLI propuesta** (basada en patrones CAF existentes):
   ```
   flowforge onboard [--project <name>] [--user <handle>] [--output <path>] [--limit <n>] [--yes]
   ```
   - `--output` → genera ONBOARDING.md (Phase 4 del spec preliminar).
   - `--limit` → controlar nº de decisiones/patrones (default 10).
   - Sin flag: briefing interactivo (Spectre.Console, drill-down con SelectionPrompt).
7. **Cumplir ADR-017**: producir `installer-baseline.md` (comandos + flags + side effects) y correr
   regression tests de `install --yes`/`status`/`doctor`/`uninstall` antes y después de tocar el installer.
8. **Métricas de éxito del spec** (verificables): briefing < 5s (HTTP) / < 2s (CLI local);
   si `mem_context` devuelve 0 sesiones → mensaje de equipo sin memorias.
9. **AOT**: modelos de respuesta HTTP vía `JsonSerializerContext` (source-gen). Probar `Process`
   con `engram search` en pipeline de test.
10. **Considerar `engram doctor`/`mem_doctor`** en el pre-check del onboarding (patrón DoctorCommand):
    "engram binary OK / MCP configurado / server reachable" antes de buscar memorias.

---

## 13) Constraint check (CKP-0)

- Requisitos del feature: **suficientemente específicos** (spec preliminar detallado en
  docs/backlog/FF-003-onboarding-flow/spec.md con comando, flujo, effort breakdown y riesgos).
- Existencia de duplicación (ENG-485): **es un riesgo de diseño, no una ambigüedad de requisitos** —
  el arquitecto (forge-arch) puede resolverlo con una decisión explícita; no requiere clarificación
  del humano en esta fase.
- **CLEAR** → avanzar a forge-arch (CKP-1).
