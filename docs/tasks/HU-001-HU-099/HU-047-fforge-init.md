---
flowforge_slug: fforge-init
status: done
---

# HU-047: `/fforge-init` — Skill de Inicialización de AGENTS.md

## Metadata

| Campo | Valor |
|-------|-------|
| **HU** | HU-047 |
| **Titulo** | Skill `/fforge-init` para configurar AGENTS.md en proyectos heredados |
| **Estado** | done |
| **flowforge_slug** | fforge-init |
| **Fecha de creacion** | 2026-07-30 |
| **Prioridad** | Alta |
| **Skills relacionadas** | `forge-orchestrator`, `flowdoc-assist`, `reginald-persona` |
| **Tags** | `tooling`, `onboarding`, `skill` |

---

## 1. Resumen

Esta HU describe la skill `/fforge-init`, una herramienta de inicializacion de configuracion de agentes (AGENTS.md) disenada para proyectos heredados o nuevos. La skill permite aplicar plantillas base, seleccionar fragmentos, y hacer merge inteligente de configuraciones existentes con intervencion del usuario en cada decision.

---

## 2. Motivacion

Al heredar proyectos de otras herramientas o equipos, los archivos `AGENTS.md` suelen contener configuraciones heredadas que chocan con las skills y flujos de trabajo globales del agente. Actualmente, actualizar `AGENTS.md` manualmente en cada proyecto es tedioso y propenso a errores.

Esta skill automatiza el proceso de configuracion, manteniendo la flexibilidad necesaria para respetar documentacion especifica del proyecto.

---

## 3. Arquitectura de Plantillas

### 3.1 Estructura de archivos

```
skills/
└── fforge-init/
    └── SKILL.md
    └── templates/
        ├── base/
        │   └── AGENTS.md.base          # Plantilla base comun
        └── fragments/
            ├── stack/
            │   ├── android.md.fragment
            │   ├── web.md.fragment
            │   ├── python.md.fragment
            │   └── ...
            ├── functionality/
            │   ├── testing.md.fragment
            │   ├── security.md.fragment
            │   ├── database.md.fragment
            │   └── ...
            └── agent/
                ├── opencode.md.fragment
                ├── claude-code.md.fragment
                └── cursor.md.fragment
```

### 3.2 Plantilla Base

La plantilla base incluye:
- Header estandar con project metadata
- Secciones vacias para: Overview, Tech Stack, AI Agent Rules, Build & Test Commands
- Referencias a templates de FlowDoc
- Flags de configuracion basica (debug mode, logging, etc.)

### 3.3 Fragmentos

**Por stack:**
- `android.md.fragment` — Android/Kotlin
- `web.md.fragment` — JavaScript/TypeScript
- `python.md.fragment` — Python
- (extensible)

**Por funcionalidad:**
- `testing.md.fragment` — Testing commands y skills
- `security.md.fragment` — Security guidelines
- `database.md.fragment` — Database schema y migrations
- (extensible)

**Por agente:**
- `opencode.md.fragment` — Optimizado para OpenCode
- `claude-code.md.fragment` — Optimizado para Claude Code
- `cursor.md.fragment` — Optimizado para Cursor
- (extensible)

---

## 4. Flujo de Ejecucion — Paso a Paso

El comando `/fforge-init` ejecuta los siguientes pasos de forma secuencial:

### Paso 1: Deteccion

```
[1/6] DETECCION
────────────────
Buscando AGENTS.md en el proyecto...
✓ Encontrado: ./AGENTS.md
⚠ Archivo existente detectado
```

Si NO existe `AGENTS.md`:
```
[1/6] DETECCION
────────────────
Buscando AGENTS.md en el proyecto...
✗ No encontrado
→ Ir a Paso 2a (Plantilla base)
```

### Paso 2a: Sin AGENTS.md existente — Seleccion de Plantilla

```
[2/6] PLANTILLA BASE
────────────────────
No se detecto AGENTS.md existente.

Selecciona una plantilla base:
  [1] Blank (solo estructura minima)
  [2] Kotlin/Android (base + android fragment)
  [3] JavaScript/TypeScript (base + web fragment)
  [4] Python (base + python fragment)

> _
```

**Seleccion:** El usuario elige una opcion.

### Paso 2b: Con AGENTS.md existente — Deteccion de Secciones

```
[2/6] ANALISIS DE AGENTS.md EXISTENTE
──────────────────────────────────────
Se detectaron las siguientes secciones:

  [✓] Overview                  (lineas 1-15)
  [✓] Tech Stack                (lineas 17-25)
  [✓] AI Agent Rules           (lineas 27-40)
  [✓] Skills                    ← ⚠ EXTERNA (no FlowForge)
  [ ] Commands                  (vacio)
  [✓] Build & Test Commands     (lineas 45-60)
  [✓] Common Errors            (lineas 62-75)
  [ ] Documentation             (vacio)
  [✓] Skills                    ← ⚠ EXTERNA (no FlowForge)

⚠ 2 secciones contain skills no-FlowForge:
  1. Skills (linea 28-35) — skill heredada: 'legacy-tooling'
  2. Skills (linea 70-80) — skill heredada: 'old-debug'

→ SELECCION REQUERIDA antes de continuar
```

### Paso 3: Seleccion de Secciones a Mantener

```
[3/6] SELECCION DE SECCIONES
──────────────────────────────
Marca las secciones a mantener del AGENTS.md original.

  [x] Overview                  → Copiar
  [ ] Tech Stack                → Descartar
  [x] AI Agent Rules            → Copiar
  [ ] Skills (legacy)           → Descartar
  [x] Build & Test Commands     → Copiar
  [ ] Common Errors             → Descartar
  [ ] Documentation             → Vacio, omitir

  [A] Seleccionar todo
  [N] Deseleccionar todo
  [?] Help

> _
```

### Paso 4: Clasificacion de Skills

Para cada skill detectada en el AGENTS.md existente:

```
[4/6] SKILLS EXTERNAS DETECTADAS
───────────────────────────────────
Skill: 'legacy-tooling' (linea 30-35)

  Tipo detectado: tooling
  Origen: proyecto heredado
  ¿Pertenece a FlowForge? NO

  Acciones disponibles:
    [1] Descartar completamente
    [2] Mantener sin modificar
    [3] Migrar a skill FlowForge equivalente (si existe)
    [4] Revisar contenido manualmente

> _
```

Si el usuario selecciona [3] y existe migracion:

```
  Migracion sugerida: 'legacy-tooling' → 'forge-dev-tooling'
  ¿Aplicar migracion?
    [Y] Si, migrar automaticamente
    [N] No, mantener como esta
    [R] Revisar despues

> _
```

**Regla:** Si hay skills externas sin decision, el proceso **se bloquea** hasta que el usuario responda cada una.

### Paso 5: Seleccion de Fragmentos (Hibrido)

```
[5/6] SELECCION DE FRAGMENTOS
───────────────────────────────
Elige fragmentos a agregar segun el contexto del proyecto.

PLANTILLA BASE: kotlin-android-base

STACK DETECTADO: Android/Kotlin
  [✓] android.md.fragment (recomendado)
  [ ] kotlin.md.fragment

FUNCIONALIDAD:
  [ ] testing.md.fragment
  [ ] security.md.fragment
  [ ] database.md.fragment
  [✓] compose.md.fragment (detectado: Jetpack Compose)

AGENTE DESTINO:
  [✓] opencode.md.fragment (agente actual)
  [ ] claude-code.md.fragment
  [ ] cursor.md.fragment

  [A] Seleccionar recomendados
  [N] Ninguno
  [?] Help

> _
```

### Paso 6: Merge y Aplicacion

```
[6/6] APLICACION
────────────────
Generando AGENTS.md...

  ✓ Copiando Overview del original
  ✓ Aplicando Tech Stack: kotlin-android-base
  ✓ Aplicando Skills: forge-orchestrator, forge-memory-*, reginald-persona
  ✓ Agregando Build Commands
  ✓ Agregando Testing fragment
  ✓ Agregando Android fragment
  ✓ Agregando OpenCode fragment

  ⚠ 2 skills externas descartadas: legacy-tooling, old-debug

AGENTS.md generado exitosamente.
Guardado en: ./AGENTS.md

¿Ver el resultado?
  [V] Ver ahora
  [G] Guardar y salir

> _
```

---

## 5. Merge Inteligente — Detalle

### 5.1 Algoritmo de Merge Semantico

El merge inteligente no simplemente reemplaza secciones; intenta preservar informacion util.

| Escenario | Comportamiento |
|-----------|----------------|
| **Listas de skills** | Une ambas listas, marca duplicados |
| **Comandos de build** | Combina ambos, detecta conflicts si hay mismo comando con distinto valor |
| **Descripciones de proyecto** | Se ofrece al usuario para decidir |
| **Secciones vacias** | Rellena con template base |
| **Conflicts irresolubles** | Pausa y pregunta |

### 5.2 Clasificacion de Skills

La clasificacion se realiza mediante **string match** contra una lista de prefijos conocidos:

```
┌─────────────────────────────────────────────────────────┐
│ FLOWFORGE (prefijo forge-, flowdoc-, reginald-)        │
├─────────────────────────────────────────────────────────┤
│ • forge-*           • flowdoc-*                       │
│ • reginald-persona                                     │
│                                                         │
│ → Se incluyen automaticamente en AGENTS.md generado      │
└─────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────┐
│ EXTERNAS / HEREDADAS (todo lo demas)                   │
├─────────────────────────────────────────────────────────┤
│ • sdd-*              • custom-*                       │
│ • Cualquier otra skill instalada por el usuario         │
│ → Se presentan al usuario para decision                │
└─────────────────────────────────────────────────────────┘
```

### 5.3 Descripcion del Proyecto

La "descripcion del proyecto" en el merge semantico se resuelve asi:

| Escenario | Comportamiento |
|-----------|----------------|
| Existe en AGENTS.md original | Se ofrece al usuario para decidir |
| Es un link al README | Se preserva como referencia |
| Es texto libre | Se usa directamente |

### 5.4 Opciones para Skills Externas

| Opcion | Descripcion | Bloquea proceso |
|--------|-------------|------------------|
| **Descartar** | Elimina la skill del AGENTS.md | No |
| **Mantener** | Copia la skill sin cambios | No |
| **Migrar** | Si existe en mapping estatico, propone migracion | No |
| **Revisar** | Agrega comentario `<!-- TODO: revisar -->` | Si, hasta resolver |

### 5.5 Mapping Estatico de Migracion

El skill incluye un mapping interno para migraciones comunes. Este mapping es estatico y se extiende manualmente:

| Skill Externa | Equivalente FlowForge |
|---------------|----------------------|
| `sdd-explore` | `forge-discovery` |
| `sdd-init` | `forge-init` |
| `sdd-new` | `forge-arch` |
| `sdd-continue` | `forge-dev` |
| `sdd-verify` | `forge-verify` |
| `sdd-archive` | `forge-memory` |
| `sdd-ff` | `forge-dev` |
| `sdd-apply` | `forge-dev` |

> **Nota**: Skills personalizadas del usuario se tratan como externas y no tienen mapping. El usuario decide caso por caso.

---

## 6. Comandos y Sub-comandos

### 6.1 Comando Principal

```bash
/fforge-init
```

Inicia el flujo completo paso a paso.

### 6.2 Flags

| Flag | Descripcion |
|------|-------------|
| `--template <name>` | Salta la seleccion y usa una plantilla especifica |
| `--no-interactive` | Usa valores por defecto (no recomendado) |
| `--dry-run` | Muestra que haria sin aplicar cambios |
| `--verbose` | Muestra mas detalles durante la ejecucion |

### 6.3 Ejemplos

```bash
# Flujo completo interactivo
/fforge-init

# Aplicar plantilla Android directamente
/fforge-init --template android

# Ver que cambiaria sin aplicar
/fforge-init --dry-run

# Verbose para debug
/fforge-init --verbose
```

---

## 7. Estructura de la Skill

```
skills/
└── fforge-init/
    ├── SKILL.md                    # Este archivo
    ├── src/
    │   ├── main.ts                # Entry point
    │   ├── detector.ts            # Detecta AGENTS.md existente
    │   ├── analyzer.ts            # Analiza contenido
    │   ├── classifier.ts          # Clasifica skills
    │   ├── merger.ts              # Merge inteligente
    │   ├── applicator.ts          # Aplica cambios
    │   └── templates/              # Plantillas
    │       ├── base/
    │       └── fragments/
    └── README.md                   # Documentacion de uso
```

---

## 8. Criterios de Aceptacion

### 8.1 Funcionales

- [x] El comando `/fforge-init` se ejecuta sin errores
- [x] Detecta correctamente si existe o no un `AGENTS.md`
- [x] Muestra todas las secciones del `AGENTS.md` existente
- [x] Clasifica correctamente las skills en FlowForge vs Externas
- [x] Bloquea el proceso si hay skills externas sin decision
- [x] Permite seleccionar fragmentos de forma hibrida (stack + funcionalidad)
- [x] Aplica el merge semantico correctamente
- [x] Genera un `AGENTS.md` valido
- [x] Soporta multiples agentes (OpenCode, Claude Code, Cursor)

### 8.2 No Funcionales

- [x] Tiempo de ejecucion < 30 segundos para proyectos tipicos
- [x] Mensajes de error claros y accionables
- [x] Historial de decisiones visible durante el proceso
- [x] Rollback disponible si algo falla

### 8.3 Rollback

El rollback funciona de la siguiente manera:

| Situacion | Comportamiento |
|-----------|----------------|
| Antes de aplicar cambios | Se guarda backup en `.agents.md.backup` |
| Si falla la aplicacion | Se restaura automaticamente desde backup |
| Si el usuario decide deshacer | Ejecuta `/fforge-init --restore` |

```bash
# Restaurar desde backup
/fforge-init --restore

# Verificar si existe backup
ls -la .agents.md.backup
```

### 8.4 Testeables

- [x] `dry-run` no modifica archivos
- [x] Plantillas base son validas (parsean correctamente)
- [x] Cada fragmento es independiente y combinable
- [x] El merge no pierde informacion de secciones seleccionadas
- [x] El rollback restaura correctamente el archivo original

---

## 9. Dependencias

| Dependencia | Tipo | Notas |
|-------------|------|-------|
| Ninguna | — | Skill autocontenida, sin dependencias externas |

### 9.1 Justificacion

- **Templates integrados**: Los templates base y fragmentos viven en `skills/forge-init/templates/`. No se requiere invocar `flowdoc-assist` para generarlos.

- **Clasificacion sin `find-skills`**: La clasificacion FlowForge vs Externa es un simple string match contra el prefijo `forge-`. No se necesita un registry de skills externo.

- **Migracion de skills**: La migracion a equivalentes FlowForge se realiza mediante un mapping estatico interno (ver seccion 5.5). No requiere invocacion a otro skill.

### 9.2 Engram (opcional, no bloqueante)

Si Engram esta disponible, se loguean las decisiones de sync para historial. Si no, el skill funciona sin ello.

---

## 10. Roadmap

### Fase 1 — MVP
- [x] Comando `/fforge-init` funcional
- [x] Plantilla base + fragmentos basicos
- [x] Solo OpenCode

### Fase 2 — Multi-agente
- [ ] Soporte para Claude Code
- [ ] Soporte para Cursor
- [ ] Fragmentos especificos por agente

### Fase 3 — Extensiones
- [ ] Migracion automatica de skills conocidas
- [ ] Template registry centralizado
- [ ] Integracion con `skills.sh`

---

## 11. Preguntas Abiertas

Ninguna por ahora. Todas las decisiones han sido tomadas.

---

## 12. Memoria de Decisiones

| Decision | Justificacion |
|----------|---------------|
| Plantillas dentro de la skill | Instalacion autocontenida, no depende de archivos externos |
| Flujo paso a paso | Maxima transparencia y control del usuario |
| Merge semantico avanzado | Preserva informacion util en lugar de simplemente reemplazar |
| Clasificacion FlowForge vs Externa | Evita contaminar la configuracion con skills heredadas |
| No persistir preferencias | Cada proyecto es independiente; no assumptions |
| Fragmentos hibridos (stack + funcionalidad) | Flexible segun contexto del proyecto |
| Skill autocontenida (sin dependencias) | Evita falhas en cadena si falla una dependencia |
| String match para clasificacion | Simple, rapido, sin registry externo |
| Mapping estatico para migraciones | Mantenible, predecible, sin invocaciones externas |
| Rollback automatico con backup | Protege al usuario de perdidas de informacion |

---

**Notas del autor**: Esta HU fue disenada para maximizar la transparencia del usuario sobre lo que se esta haciendo con su proyecto, sin asumir que una configuracion "ideal" aplica universalmente.
