# FlowForge — Especificación Técnica de Agentes, Skills y Flujos

> **Propósito**: Entender qué hace cada agente con cada skill, detectar faltantes
> **Versión**: 1.0 (post-OLA 1-4)
> **Skills**: 30 | **Agentes**: 7 | **Checkpoints**: 5

---

## PARTE 1: ESPECIFICACIÓN POR AGENTE

Cada agente tiene:
- **Core Skill**: funciones base que ejecuta SIEMPRE
- **Specialized Skills**: funciones adicionales que se cargan on-demand según el contexto
- **Checkpoint**: qué punto de control le corresponde

---

### 1. forge-orchestrator — El Semáforo

**Rol**: Director de estado. No escribe código ni specs. Solo delega y aplica checkpoints.
**Checkpoint**: CKP-0 → CKP-4 (transversal)

---

#### Core Skill `forge-orchestrator/SKILL.md`

| Función | Input | Output | Proceso | Trigger |
|---------|-------|--------|---------|---------|
| `evaluar_fase()` | Prompt del usuario + `.flowforge.json` | Fase actual (0-4) o "nueva solicitud" | Lee archivos existentes (spec.md, plan.md) y el estado del proyecto | Inicio de cualquier interacción |
| `delegar_discovery()` | Prompt del usuario | Context Map + veredicto CKP-0 | Invoca a forge-discovery con el prompt original | Fase = 0 |
| `aplicar_ckp0()` | Context Map | 🔴 STOP o 🟢 continuar | Si descubrimiento vago o sin contexto → PARA. Si claro → avanza a Fase 1 | Después de discovery |
| `delegar_arch()` | Context Map + prompt original | spec.md + Capability Matrix | Invoca forge-arch con el Context Map | CKP-0 superado |
| `aplicar_ckp1()` | spec.md | 🟡 "¿Aprobás?" o continuar | Presenta spec.md al humano y espera confirmación explícita | spec.md generado |
| `delegar_plan()` | spec.md aprobado | plan.md | Invoca forge-plan con el spec.md | CKP-1 aprobado |
| `aplicar_ckp2()` | plan.md | 🟡 "¿Luz verde?" o continuar | Presenta plan.md al humano y espera confirmación | plan.md generado |
| `delegar_dev()` | plan.md | Código + tests | Invoca forge-dev con el plan.md | CKP-2 aprobado |
| `delegar_verify()` | Código generado | PASS o rework_ticket.md | Invoca forge-verify sobre el diff | Dev completa su ciclo |
| `aplicar_ckp3()` | rework_ticket.md | 🔴 ESCALAR o 🔄 reintentar | Si cycle_count = 3 → para y escala. Si < 3 → devuelve a dev | Verify emite FAIL |
| `delegar_memory()` | Resultado del ciclo | Session summary + ADRs | Invoca forge-memory para cerrar la sesión | Verify emite PASS |
| `aplicar_ckp4()` | Session summary | 🟢 "¿Deployeamos?" o continuar | Pregunta al humano si deployea | Memory completó |

**Gap detectado**: No hay función de `rollback_de_fase()` — si el humano rechaza el spec/plan, no hay un protocolo formal para reintentar. Hoy es "el humano pide ajustes" pero no hay un ciclo controlado como en CKP-3.

---

### 2. forge-discovery — Fase 0

**Rol**: Mapear contexto previo antes de planificar.
**Checkpoint**: CKP-0 🔴

---

#### Core Skill `forge-discovery/SKILL.md`

| Función | Input | Output | Proceso | Trigger |
|---------|-------|--------|---------|---------|
| `extraer_keywords()` | Prompt del usuario | 3-5 términos técnicos/de negocio | Parsea el prompt, extrae términos como "auth", "login", "jwt" | Inicio de discovery |
| `mem_search_engram()` | Keywords + proyecto | IDs de observaciones candidatas | `mem_search()` contra engram-dotnet con filtro de proyecto | Keywords extraídas |
| `mem_get_observations()` | IDs de observaciones | Observaciones completas | Para cada resultado truncado, llama a `mem_get_observation(id)` | Resultados de búsqueda |
| `grep_fallback()` | Keywords | Archivos .md locales | Si engram-dotnet no responde, busca en `.engram/local_memory/` | Engram no disponible |
| `mapear_epicas()` | Observaciones + prompt | Asociación épica actual vs pasadas | Compara topic_keys, detecta si pertenece a épica existente | Datos de memoria obtenidos |
| `generar_context_map()` | Asociaciones | Context Map (narrativa) | Documenta hallazgos como prefacio para Fase 1 | Mapeo completado |
| `evaluar_ambigüedad()` | Context Map + prompt | 🔴 "VAGO" o 🟢 "CLARO" | Si no hay contexto suficiente O el prompt es genérico → VAGO. Si no → CLARO | Disparador de CKP-0 |

#### Specialized: Security `forge-discovery/security/SKILL.md`

| Función | Input | Output | Proceso | Trigger |
|---------|-------|--------|---------|---------|
| `escanear_cves_stack()` | Stack tecnológico | Lista de CVEs conocidos | Busca `mem_search(keywords: ["CVE", stack_component])` en engram + conocimiento externo | Feature toca auth/datos/APIs |
| `evaluar_riesgo_dependencia()` | Dependencia propuesta | 🔴 Crítico / 🟡 Alto / 🟢 Bajo | Evalúa CVSS, mantenimiento del repo, historial de seguridad | Feature introduce nueva dependencia |
| `alertar_hard_stop_security()` | Evaluación de riesgo | 🔴 STOP | Si CVE crítico, incidente pasado no resuelto, o PII sin infraestructura adecuada | Riesgo = Crítico |

#### Specialized: Compliance `forge-discovery/compliance/SKILL.md`

| Función | Input | Output | Proceso | Trigger |
|---------|-------|--------|---------|---------|
| `identificar_regulaciones()` | Tipo de datos que procesa la feature | GDPR, SOC2, HIPAA, PCI-DSS, o Ninguna | Clasifica el tipo de dato (personal/salud/pago/biométrico) y cruza con regulación aplicable | Feature toca datos de usuarios |
| `generar_requisitos_compliance()` | Regulación identificada | Checklist de requisitos (consentimiento, retención, cifrado, etc.) | Según la regulación, genera los RNF de compliance que deben aparecer en el spec | Regulación identificada |
| `alertar_hard_stop_compliance()` | Requisitos vs. infraestructura actual | 🔴 STOP | Si la infra no está certificada para el nivel requerido (ej: PHI sin BAA) | Brecha de compliance detectada |

#### Specialized: Cost `forge-discovery/cost/SKILL.md`

| Función | Input | Output | Proceso | Trigger |
|---------|-------|--------|---------|---------|
| `estimar_compute()` | Perfil de CPU de la feature | Costo mensual estimado en USD | Evalúa si es CRUD (< 5% CPU), batch (burst), real-time, ML, o file processing | Feature nueva |
| `estimar_storage()` | Proyección de datos por mes | Costo de storage + backup por mes | Calcula registros × tamaño × retención + backups | Feature almacena datos |
| `estimar_bandwidth()` | Tráfico estimado | Costo de transferencia por mes | Calcula egress (API externa, descargas, streaming) + CDN si aplica | Feature expone datos |
| `estimar_api_externa()` | Servicio externo + volumen de llamadas | Costo por mes | Costo por llamada o % de transacción × volumen | Feature usa API de terceros |
| `generar_cost_profile()` | Todas las estimaciones | Perfil de costo: LOW/MEDIUM/HIGH + proyecciones a escala | Suma todos los componentes con proyecciones a 10K/100K/1M usuarios | Todas las estimaciones listas |

**Gaps detectados**:
1. No hay skill para `analisis_competencia()` — ¿existen alternativas open source que resuelvan esto?
2. No hay `factibilidad_tecnica()` — ¿la API externa requerida realmente existe y tiene la documentación adecuada?
3. `compliance/identificar_regulaciones()` depende de que el usuario especifique el tipo de dato — no hay detección automática desde el código existente.

---

### 3. forge-arch — Fase 1

**Rol**: Traducir intención humana en spec.md ejecutable.
**Checkpoint**: CKP-1 🟡

---

#### Core Skill `forge-arch/SKILL.md`

| Función | Input | Output | Proceso | Trigger |
|---------|-------|--------|---------|---------|
| `escribir_objetivo()` | Context Map + prompt | Sección "Objetivo y Alcance" del spec | Define qué resuelve y qué queda fuera del alcance | Inicio de spec |
| `definir_rf()` | Requerimientos funcionales del prompt | RF-001 a RF-N con escenarios GWT | Cada RF tiene descripción + 2 escenarios Given-When-Then | Objetivo definido |
| `definir_rnf()` | Requerimientos no funcionales | RNF-001 a RNF-N | Performance, seguridad, disponibilidad, etc. | RF definidos |
| `definir_capability_matrix()` | RFs + RNFs | Tabla ai_reasoning vs deterministic | Qué decisiones delega al LLM y qué reglas son inmutables | Spec completo |
| `definir_validacion_manual()` | Escenarios no capturados por tests | Escenarios de prueba manual para el humano | Casos como interacción visual, flujo de consola, error de red en caliente | Spec casi completo |
| `detectar_conflictos_memoria()` | RFs propuestos | ⚠️ Alerta de conflicto o ✅ Sin conflicto | Busca en memoria decisiones previas que contradigan lo propuesto | Antes de finalizar spec |

#### Specialized: Security `forge-arch/security/SKILL.md`

| Función | Input | Output | Proceso | Trigger |
|---------|-------|--------|---------|---------|
| `aplicar_stride()` | Arquitectura propuesta | 6 RNF-SEC obligatorios (Spoofing, Tampering, Repudiation, Disclosure, DoS, Elevation) | Por cada threat del modelo STRIDE, genera al menos 1 RNF-SEC | Feature toca auth/datos/APIs |
| `definir_rnf_auth()` | Feature con auth | RNF-SEC-AUTH-001-003 | Endpoints protegidos, JWT con expiración, lockout por intentos fallidos | Feature requiere login |
| `definir_rnf_input()` | Feature con input de usuario | RNF-SEC-INPUT-001-003 | Validación server-side, whitelist, max length | Feature acepta input |
| `definir_rnf_data()` | Feature con datos sensibles | RNF-SEC-DATA-001-003 | Bcrypt/argon2, AES-256, parameterized queries | Feature persiste datos |
| `definir_rnf_config()` | Feature con secrets | RNF-SEC-CONFIG-001-002 | No secrets en código, environment variables | Feature usa API keys |
| `declarar_sin_riesgo()` | Feature sin superficie de ataque | "## Security Assessment: No aplica" | Documenta explícitamente que la feature no requiere seguridad | Evaluación completa y no hay riesgos |

#### Specialized: Performance `forge-arch/performance/SKILL.md`

| Función | Input | Output | Proceso | Trigger |
|---------|-------|--------|---------|---------|
| `definir_slo_api()` | Endpoints expuestos | RNF-PERF-001 a 004 (p99, p50, throughput, TTFB) | Define latencias máximas por endpoint según tipo | Feature expone API |
| `definir_slo_db()` | Consultas a DB | RNF-PERF-005 a 008 (query time, pool, index hit, write batch) | Limita tiempos de consulta y conexiones concurrentes | Feature accede a DB |
| `definir_slo_ui()` | Componentes frontend | RNF-PERF-009 a 013 (FCP, LCP, FID, CLS, TTI) | Define métricas Core Web Vitals | Feature tiene UI |
| `definir_slo_batch()` | Jobs background | RNF-PERF-014 a 016 (items/min, max execution, memory limit) | Define rendimiento de procesamiento por lotes | Feature procesa batches |
| `identificar_hot_paths()` | Todos los paths de ejecución | Lista de hot paths con recomendaciones | Si un path está en el ciclo request-response y es llamado > 100 req/s → hot path | Análisis de paths |
| `alertar_riesgo_performance()` | Detección de riesgos | RNFs mitigando cada riesgo | Datos sin paginación → RNF de paginación. Reportes en tiempo real → RNF de pre-cómputo | Riesgo detectado |

#### Specialized: A11y `forge-arch/a11y/SKILL.md`

| Función | Input | Output | Proceso | Trigger |
|---------|-------|--------|---------|---------|
| `determinar_nivel_wcag()` | Proyecto + tipo de feature | Nivel A / AA / AAA requerido | Customer-facing → AA. Gobierno → AAA. Internal tool → A. Default: AA | Feature con UI |
| `definir_rnf_formularios()` | Componentes de formulario | RNF-A11Y-001 a 005 | Labels, errores asociados, autocomplete, required fields | Feature tiene formularios |
| `definir_rnf_navegacion()` | Menús y navegación | RNF-A11Y-006 a 009 | Keyboard accessible, skip-to-content, aria-current, múltiples métodos de navegación | Feature tiene navegación |
| `definir_rnf_tablas()` | Datos tabulares | RNF-A11Y-010 a 012 | <th> con scope, sort direction, summary/caption | Feature muestra datos en tabla |
| `definir_rnf_modal()` | Modales y diálogos | RNF-A11Y-013 a 016 | Focus trap, Escape key, focus return, aria-labelledby | Feature usa modales |
| `definir_rnf_media()` | Imágenes/video/audio | RNF-A11Y-017 a 020 | Alt text, captions, transcripts, color-not-only | Feature incluye media |
| `definir_rnf_color()` | Paleta de colores | RNF-A11Y-024 a 027 | Contraste 4.5:1, 3:1 para componentes, focus indicator, no color-only | Feature tiene UI visual |
| `definir_rnf_responsive()` | Layout | RNF-A11Y-028 a 030 | 200% zoom funcional, touch targets 44x44px, sin scroll horizontal | Feature tiene layout |
| `declarar_sin_riesgo_a11y()` | Feature sin UI | "## Accessibility Assessment: No UI — no WCAG requirements apply" | Feature es backend-only | Evaluación completa, no hay UI |

#### Specialized: Domain `forge-arch/domain/SKILL.md`

| Función | Input | Output | Proceso | Trigger |
|---------|-------|--------|---------|---------|
| `identificar_contextos()` | Descripción de la feature | Lista de bounded contexts involucrados | Analiza qué áreas del dominio toca la feature (Ordering, Billing, Shipping, etc.) | Feature con lógica de negocio compleja |
| `definir_lenguaje_ubicuo()` | Términos de negocio extraídos del prompt | Glosario de términos con definiciones precisas | Cada término tiene: nombre, definición de negocio (no técnica), sinónimos, eventos que produce | Contextos identificados |
| `diseniar_agregados()` | Entidades identificadas | Agregados con invariantes, entidades, VOs, eventos | Cada aggregate tiene root ID, invariantes (reglas SIEMPRE verdaderas), entidades internas, value objects, eventos de dominio | Lenguaje ubicuo definido |
| `definir_eventos_dominio()` | Cambios de estado significativos | Tabla de eventos con producer/consumers/payload | Por cada estado -> evento: qué lo produce, quién lo consume, qué datos lleva | Agregados diseñados |
| `detectar_anti_patrones_domain()` | Diseño propuesto | Flag si hay anti-patrones (anemia, acoplamiento cross-context, god aggregate) | Revisa que no haya: getters-only sin lógica, imports entre contextos, aggregates con > 5 entidades | Diseño completo |

**Gaps detectados**:
1. No hay función `definir_stack_tecnologico()` — el arch asume el stack existente sin evaluar si es el adecuado para la feature
2. `a11y/determinar_nivel_wcag()` necesita consultar configuración del proyecto — no hay un mecanismo formal de configuración todavía (depende del CLI Wizard)
3. No hay `definir_contratos_api()` — los contratos entre servicios (OpenAPI, gRPC) no se especifican en el spec, quedan para el plan
4. `domain/identificar_contextos()` no tiene acceso a un mapa de contextos del proyecto completo — solo analiza la feature actual

---

### 4. forge-plan — Fase 2

**Rol**: Descomponer spec.md en tareas atómicas.
**Checkpoint**: CKP-2 🟡

---

#### Core Skill `forge-plan/SKILL.md`

| Función | Input | Output | Proceso | Trigger |
|---------|-------|--------|---------|---------|
| `analizar_impacto()` | spec.md | Análisis de impacto: componentes existentes tocados + nuevas dependencias | Identifica qué archivos existen y se modifican, qué librerías nuevas se necesitan | Inicio de plan |
| `ordenar_tareas_topologicas()` | Lista de cambios necesarios | Checklist ordenado: DB/DTOs → lógica → controllers → tests | Primero dependencias (DB, modelos), luego infraestructura (endpoints), al final tests | Análisis de impacto listo |
| `definir_contratos()` | RFs del spec | Estructuras de datos exactas: DTOs, esquemas DB, firmas de métodos | Cada contrato tiene propiedades, tipos, constraints. Nada queda a interpretación del dev | Antes de escribir tareas |
| `proponer_cambios_archivos()` | Contratos + checklist | Lista de [NEW] / [MODIFY] / [DELETE] por archivo | Cada archivo tiene: ruta, responsabilidad, cambios exactos | Contratos definidos |
| `buscar_patrones_existentes()` | Código existente | Patrones previos a respetar | `mem_search(topic_key: pattern)` para encontrar convenciones del proyecto | Antes de definir estructura de archivos |

#### Specialized: Security `forge-plan/security/SKILL.md`

| Función | Input | Output | Proceso | Trigger |
|---------|-------|--------|---------|---------|
| `aplicar_principios_secure_design()` | Plan propuesto | Tasks con anotaciones [SEC] aplicando Least Privilege, Defense in Depth, Fail Securely, Never Trust Input, Secure by Default | Cada tarea hereda principios de seguridad | Cualquier plan |
| `verificar_asvs_authentication()` | Plan que toca auth | Checklist V2: password policy, brute force, credential recovery | Marca si falta alguna tarea de autenticación | Plan tiene auth |
| `verificar_asvs_session()` | Plan que toca sesiones | Checklist V3: session tokens, logout, timeout | Marca si falta gestión de sesiones | Plan tiene sesiones |
| `verificar_asvs_access()` | Plan que toca permisos | Checklist V4: RBAC/ABAC, row ownership, admin segregation | Marca si falta control de acceso | Plan tiene roles/permisos |
| `verificar_asvs_input()` | Plan que toca input de usuario | Checklist V5: input validation, parameterized queries, file upload | Marca si falta validación de entrada | Plan acepta input |
| `verificar_asvs_output()` | Plan que renderiza datos | Checklist V6: context-aware encoding, JSON escaping, no raw HTML | Marca si falta encoding de salida | Plan tiene output al usuario |
| `anotar_tareas_sec()` | Checklist de plan | Tasks con prefijo [SEC] y anti-patrones marcados | Modifica las tareas existentes agregando anotaciones de seguridad | Verificaciones completadas |

#### Specialized: Patterns `forge-plan/patterns/SKILL.md`

| Función | Input | Output | Proceso | Trigger |
|---------|-------|--------|---------|---------|
| `seleccionar_patron_creacional()` | Problema de creación de objetos | Singleton / Factory / Builder / Prototype + justificación | Árbol de decisión: ¿instancia única? → Singleton. ¿Familia de objetos? → Abstract Factory | Plan tiene creación compleja de objetos |
| `seleccionar_patron_estructural()` | Problema de relación entre objetos | Adapter / Decorator / Facade / Flyweight + justificación | Árbol de decisión: ¿interfaces incompatibles? → Adapter. ¿Agregar comportamiento sin modificar? → Decorator | Plan tiene objetos con interfaces incompatibles |
| `seleccionar_patron_behavioral()` | Problema de comunicación entre objetos | Strategy / Chain / Command / Mediator / Observer + justificación | Árbol de decisión: ¿algoritmo variable? → Strategy. ¿Pipeline de validación? → Chain of Responsibility | Plan tiene lógica de negocio compleja |
| `seleccionar_patron_enterprise()` | Problema de sistema distribuido | CQRS / Event Sourcing / Saga / Outbox / Circuit Breaker / Bulkhead | Según el problema: ¿separar lecturas/escrituras? → CQRS. ¿Transacción distribuida? → Saga | Plan tiene múltiples servicios o alta concurrencia |
| `anotar_patrones_en_tasks()` | Selección de patrones | Tasks con prefijo [PATTERN: X] | Cada tarea documenta el patrón aplicado: `[ ] [PATTERN: Strategy] IPaymentProcessor` | Patrón seleccionado |
| `detectar_anti_patrones_plan()` | Plan propuesto | Alerta si hay anti-patrones (God Object, Spaghetti, Premature Abstraction, Magic Dependency, Anemic Domain) | Revisa que no haya: una clase que hace todo, sin capas, interfaz con una impl, new en lógica de negocio | Plan completado |

#### Specialized: Migrations `forge-plan/migrations/SKILL.md`

| Función | Input | Output | Proceso | Trigger |
|---------|-------|--------|---------|---------|
| `clasificar_tipo_migracion()` | Cambios en DB del plan | 🟢 Additive / 🟡 Modifying / 🔴 Destructive | ¿Agrega columnas? → Additive. ¿Renombra/cambia tipo? → Modifying. ¿Elimina? → Destructive | Plan toca DB |
| `generar_fases_migracion()` | Cambio de DB | Plan en 1-4 fases (pre-deploy, code deploy, post-deploy, future release) | Cada fase es una tarea separada del checklist con su propio SQL y rollback | Tipo de migración identificado |
| `verificar_zero_downtime()` | Plan de migración | Checklist de zero-downtime: backward compatible, rollback script, lock time estimado, data backfill, read replicas | Marca si la migración es segura para deploy sin downtime | Migración planificada |
| `generar_rollback_migration()` | Migración SQL propuesta | Script de rollback correspondiente | Para ADD TABLE → DROP TABLE. Para ADD COLUMN → DROP COLUMN. Para RENAME → reverse rename | Migración definida |
| `alertar_lock_time()` | ALTER TABLE statement | ⚠️ Si lock time > 5s, requiere revisión manual | ALTER TABLE ADD DEFAULT value → lock en Postgres (escribe en toda la tabla) | Migración con ALTER TABLE pesado |

#### Specialized: Rollback `forge-plan/rollback/SKILL.md`

| Función | Input | Output | Proceso | Trigger |
|---------|-------|--------|---------|---------|
| `seleccionar_estrategia_deploy()` | Cambios del plan | Rolling / Blue-Green / Canary / Feature Flag | Árbol de decisión: ¿breaking change? → Blue-Green. ¿Riesgo alto? → Canary + Feature Flag. ¿Stateless? → Rolling | Plan listo |
| `definir_plan_rollback()` | Estrategia seleccionada + cambios | Plan de rollback: pasos, criterios, tiempo estimado de recuperación | Documenta paso a paso cómo revertir, cuándo decidir hacerlo, y cuánto tiempo lleva | Estrategia seleccionada |
| `diseniar_feature_flag()` | Feature de alto riesgo | Task [FEATURE_FLAG: NAME] + rollout plan (10% → 50% → 100%) + removal timeline | Define nombre del flag, rollout gradual, y cuándo remover el código del flag | Feature es HIGH o CRITICAL risk |
| `calcular_nivel_riesgo()` | Todos los cambios del plan | LOW / MEDIUM / HIGH / CRITICAL | Según: ¿breaking change? ¿schema migration? ¿multi-service? ¿payment/auth? | Plan completo |
| `verificar_anti_patrones_deploy()` | Plan de rollback | Flag: "We'll fix forward", sin rollback probado, sin smoke test | Detector de malas prácticas de deploy | Plan de rollback definido |

**Gaps detectados**:
1. `patterns/seleccionar_patron_*()` no tiene acceso a un catálogo formal — depende del conocimiento del modelo. Si el modelo no conoce un patrón, no lo va a sugerir.
2. No hay `definir_orden_deploy()` — tareas paralelizables vs secuenciales. Qué se deploya primero si son múltiples servicios.
3. `rollback/calcular_nivel_riesgo()` es subjetivo — no hay pesos objetivos para cada factor de riesgo.
4. No hay función de `estimar_esfuerzo()` — cuánto tiempo/horas estimadas por tarea. Sin esto, no hay forma de medir cycle time real vs estimado.

---

### 5. forge-dev — Fase 3a

**Rol**: Codificar siguiendo el plan.md al pie de la letra.
**Checkpoint**: Inner Loop (sin checkpoint humano directo)

---

#### Core Skill `forge-dev/SKILL.md`

| Función | Input | Output | Proceso | Trigger |
|---------|-------|--------|---------|---------|
| `implementar_plan()` | plan.md (checklist) | Código implementado | Para cada tarea del checklist en orden, escribe el código | CKP-2 aprobado |
| `reportar_defecto_plan()` | Firma inviable detectada | ⚠️ STOP + reporte de defecto estructural | Si el plan pide una firma imposible en el lenguaje, NO inventa una solución — reporta | Durante implementación, firma inviable |
| `escribir_tests_unitarios()` | Escenarios GWT del spec.md | Tests con nombre [RF-XXX] en descripción | 1 test por escenario Given-When-Then, con trazabilidad directa al RF | Por cada función implementada |
| `ralph_wiggum_loop()` | Código escrito | Código compilado + tests verdes | Compilar → testear → fallar → corregir. Loop autónomo hasta verde. | Después de escribir código |
| `reportar_limite_loop()` | 3 iteraciones en el mismo error | 🚨 Solicitar ayuda + reporte | Si después de 3 intentos el mismo error persiste, se detiene y pide ayuda | Mismo error 3 veces |
| `mem_save_gotcha()` | Bug difícil resuelto o comportamiento oscuro | Observación tipo `bugfix` o `discovery` en engram | Guarda el gotcha antes de entregar resultado para que futuras sesiones lo tengan | Durante debug, encuentra algo no obvio |

#### Specialized: Security `forge-dev/security/SKILL.md`

| Función | Input | Output | Proceso | Trigger |
|---------|-------|--------|---------|---------|
| `verificar_owasp_a01()` | Código de endpoints | ✅ / 🚨 Broken Access Control | Cada endpoint: ¿checkea identidad? ¿row ownership? ¿admin endpoint gated? | Código con endpoints |
| `verificar_owasp_a02()` | Código con crypto | ✅ / 🚨 Cryptographic Failures | Passwords: ¿bcrypt? ¿cost ≥ 12? ¿no MD5/SHA-1? Tokens: ¿crypto.randomBytes? | Código con hashing/encryption |
| `verificar_owasp_a03()` | Código con queries | ✅ / 🚨 Injection | SQL: ¿100% parameterized? NoSQL: ¿$where sanitized? OS: ¿no exec con input? | Código con acceso a datos |
| `verificar_owasp_a04_a05()` | Configuración | ✅ / 🚨 Insecure Design / Misconfiguration | ¿Rate limiting? ¿Debug mode off? ¿CORS restricted? ¿Headers de seguridad? | Código de configuración |
| `verificar_owasp_a06()` | package.json, .csproj, requirements.txt | ✅ / 🚨 Vulnerable Components | `npm audit --audit-level=high` / `dotnet list package --vulnerable` | Dependencias agregadas/modificadas |
| `verificar_owasp_a07_a08()` | Código de auth + deserialización | ✅ / 🚨 Auth Failures / Integrity | Passwords ≥ 12 chars, no eval, no deserialización no segura | Código con auth o parsing |
| `verificar_owasp_a09_a10()` | Logs + HTTP calls | ✅ / 🚨 Logging / SSRF | Auth failures logged (sin passwords), URLs validadas contra allowlist | Código con logging o fetch |
| `escanear_red_flags()` | Todo el diff | Lista de red flags detectados (0 = PASS) | grep mental: `console.log(password)`, `+` en SQL, `eval(input)`, secrets hardcodeados, `innerHTML`, `Math.random()` para crypto | Antes de Ralph Wiggum |

#### Specialized: SOLID `forge-dev/solid/SKILL.md`

| Función | Input | Output | Proceso | Trigger |
|---------|-------|--------|---------|---------|
| `auditar_srp()` | Clase modificada | ✅/🚩 Single Responsibility | ¿Describible sin "and"/"or"? ¿Cambia por una sola razón? ¿< 200 líneas? | Cada clase nueva o modificada |
| `auditar_ocp()` | Clase con condicionales de tipo | ✅/🚩 Open/Closed | ¿switch/if-else checkeando tipo? ¿Podría agregar comportamiento sin modificar? | Cada clase con lógica condicional |
| `auditar_lsp()` | Jerarquía de clases | ✅/🚩 Liskov Substitution | ¿Subclase lanza NotImplementedException? ¿Cambia comportamiento del padre? | Cada subclase o implementación de interfaz |
| `auditar_isp()` | Interfaz implementada | ✅/🚩 Interface Segregation | ¿Implementación tiene métodos vacíos o que tiran error? ¿Interfaz es > 6 métodos? | Cada implementación de interfaz |
| `auditar_dip()` | Constructor/dependencias | ✅/🚩 Dependency Inversion | ¿Importa capas de infraestructura? ¿Usa `new` concreto en lógica de negocio? ¿Constructor > 5 parámetros? | Cada clase con dependencias |
| `calcular_puntaje_solid()` | Resultados de 5 auditorías | Puntaje 0-5 + acción (✅ ≥ 4, ⚠️ = 3, 🔧 = 2, 🚨 ≤ 1) | Promedio simple. Si ≤ 2 → refactor antes de continuar | Auditorías completadas |

#### Specialized: Testing `forge-dev/testing/SKILL.md`

| Función | Input | Output | Proceso | Trigger |
|---------|-------|--------|---------|---------|
| `escribir_tests_propiedad()` | Función con propiedades matemáticas | Property-based test | `decode(encode(x)) == x`, sort mantiene elementos, `add(x,0)== x` | Función tiene propiedades predecibles |
| `escribir_fuzzing()` | Función que acepta input externo | Test con 14 inputs maliciosos | null, whitespace, SQL injection, XSS, path traversal, Unicode, emoji, NaN, Infinity, etc. | Función expuesta a input externo |
| `mutacion_mental()` | Cada condicional del código | Confirmación: mutar un condicional rompe un test | Para cada if/&&/||: cambiar > por >=, && por ||, eliminar statement. ¿Test falla? | Tests unitarios escritos |
| `verificar_calidad_test()` | Test individual | Checklist de calidad: nombre descriptivo, un escenario, determinístico, aislado, edge cases | Cada test debe pasar la checklist | Test escrito |
| `escribir_tests_integracion_api()` | Endpoints del spec | 8 tests de integración (happy path, validation, auth, not found, conflict, rate limit, concurrency) | Cada verbo HTTP + escenario de error | API endpoints implementados |

#### Specialized: Performance `forge-dev/performance/SKILL.md`

| Función | Input | Output | Proceso | Trigger |
|---------|-------|--------|---------|---------|
| `detectar_n_plus_1()` | Loop con query dentro | 🚨 Alerta N+1 + solución (eager loading / batch query) | Escanea: ¿for/foreach/map con db.query/Items.Where adentro? | Código con loops y acceso a datos |
| `aplicar_cache_aside()` | Datos consultados frecuentemente | Implementación de caché con TTL + invalidation | Cache → miss → fetch from source → store with TTL → return | Datos de referencia o consulta frecuente |
| `batch_operations()` | Operaciones individuales en loop | Implementación batch (AddRange, insertMany, UPDATE WHERE IN) | Reemplaza N inserts en loop por 1 batch | Operaciones bulk detectadas |
| `analizar_hot_path()` | Código en request-response | Checklist de hot path: sin allocaciones, sin excepciones, sin LINQ, sin async, sin reflection | Revisa que el hot path esté optimizado | Código en hot path identificado |
| `verificar_explain()` | Query SQL | Output de EXPLAIN ANALYZE | Corre EXPLAIN en cada query nueva y verifica que use índices | Código con queries nuevas |

#### Specialized: Refactor `forge-dev/refactor/SKILL.md`

| Función | Input | Output | Proceso | Trigger |
|---------|-------|--------|---------|---------|
| `extract_method()` | Función > 30 líneas o con bloques comentados | N funciones más pequeñas con nombres de intención | Identifica bloques lógicos por comentarios, los extrae en métodos separados | Función larga detectada |
| `rename_variable()` | Nombre poco claro (t, o, data) | Nombre con intención clara (total, order, orderData) | Busca variables de 1 letra, abreviaturas, nombres ambiguos | Variable con nombre pobre |
| `introduce_parameter_object()` | Función con > 4 parámetros | Objeto DTO agrupando parámetros relacionados | Agrupa parámetros que siempre viajan juntos en una clase/interface | Función con muchos parámetros |
| `replace_conditional_polymorphism()` | switch/if-else con 5+ casos por tipo | Jerarquía de clases con interfaz compartida | Cada tipo se convierte en una clase que implementa la misma interfaz | switch/if-else por tipo |
| `decompose_conditional()` | Condicionales anidados (> 3 niveles) | Guard clauses + condiciones extraídas a métodos con nombre | Invierte ifs: "if not valid → return" en lugar de "if valid then if other then..." | Nesting depth > 3 |
| `replace_magic_number()` | Números literales sin nombre | Constantes con nombre (const FREE_SHIPPING_THRESHOLD = 100) | Busca números, strings, booleanos literales que no sean obvios | Código con literales no obvios |
| `separate_query_modifier()` | Método que consulta Y modifica | Método command (void) + método query (return) | Separa side effects de lecturas | Método híbrido detectado |
| `preservar_tests_durante_refactor()` | Tests existentes | Tests VERDES después de cada transformación | 1 transformación → run tests → VERDES → siguiente transformación. Si falla → git checkout | Antes de empezar refactor |

**Gaps detectados**:
1. `testing/escribir_fuzzing()` no es específico por lenguaje — los 14 inputs son genéricos. Faltan: fuzzing de JSON malformed, XML bombs, buffer overflows específicos del lenguaje.
2. No hay `testing/cobertura_mutation_tool()` — la mutación mental es débil comparada con herramientas reales (Stryker, PIT).
3. `performance/detectar_n_plus_1()` no tiene acceso a un profiler real — solo hace escaneo estático del código.
4. No hay `performance/profiling()` — el dev no puede medir realmente el rendimiento, solo estimarlo.
5. No hay función de `refactor/split_large_class()` — clases > 200 líneas solo se detectan pero no hay algoritmo de extracción.

---

### 6. forge-verify — Fase 3b

**Rol**: Auditar el código contra el spec.md y emitir PASS o Rework Ticket.
**Checkpoint**: CKP-3 🔴

---

#### Core Skill `forge-verify/SKILL.md`

| Función | Input | Output | Proceso | Trigger |
|---------|-------|--------|---------|---------|
| `inspeccionar_linea_por_linea()` | Diff del código modificado | Lista de errores obvios (missing returns, empty blocks, debug prints) | Lee cada línea del diff buscando errores sintácticos/lógicos | Verify inicia |
| `verificar_constantes_spec()` | Código vs spec.md | ✅ Coinciden o 🚨 Desviación detectada | Si spec dice "Default: MEDIUM", el código debe tener exactamente "MEDIUM" | Constantes en spec |
| `verificar_cobertura_gwt()` | Tests vs escenarios del spec | Cantidad de escenarios cubiertos vs totales | Cada Given-When-Then debe tener 1 test. Si falta → FAIL | Tests implementados |
| `ejecutar_tests()` | Test suite | Output verde o rojo | Corre `npm test`, `dotnet test`, `pytest` (según stack) y parsea el resultado | Código listo |
| `emitir_pass()` | Todo verde + spec compliance | PASS + manual verification steps block | Solo si tests 100% verdes y spec 100% compliance | Todas las verificaciones pasan |
| `emitir_rework_ticket()` | Fallas detectadas | rework_ticket.md con cycle_count + failure reason + affected files + correction instruction | Crea el ticket con frontmatter YAML: cycle_count, max_cycles:3, status:rejected | Alguna verificación falla |
| `incrementar_cycle_count()` | rework_ticket.md existente | cycle_count + 1 | Si ya hay un ticket, incrementa el contador. Si llega a 3, no crea nuevo — escala | Rework anterior |

#### Specialized: Security `forge-verify/security/SKILL.md`

| Función | Input | Output | Proceso | Trigger |
|---------|-------|--------|---------|---------|
| `auditar_flujo_auth()` | Endpoints del diff | ✅/🚨 Auth flow correcto | Por cada endpoint: ¿auth checkeado ANTES de lógica? ¿token VALIDADO (no solo decodificado)? ¿claims verificados? ¿middleware chain correcto? | Código con endpoints |
| `auditar_autorizacion()` | Recursos protegidos | ✅/🚨 Authorization correcto | ¿Row ownership check? ¿Admin bypass sin role check? ¿Privilege escalation por param? | Código con recursos por usuario |
| `auditar_data_flow()` | Input → proceso → output | ✅/🚨 Tainting correcto | ¿Input validado? ¿Sanitizado? ¿Parameterized? ¿Sensitive fields excluidos de response? | Código con entrada de usuario |
| `escanear_secretos()` | Todo el diff | 🚨 Secreto detectado o ✅ Limpio | API keys, tokens, passwords, connection strings, private keys en el diff | Siempre |
| `verificar_dependencias()` | Manifiesto de paquetes | ✅ 0 HIGH/CRITICAL o 🚨 CVE detectado | npm audit, dotnet list package --vulnerable, pip-audit | Cada verify |
| `auditar_owasp_top10()` | Código completo | X/10 OWASP checks passed | Aplica checklist de A01 a A10 | Siempre |
| `auditar_security_headers()` | Configuración de respuesta HTTP | X/6 headers configurados correctamente | CSP, X-Content-Type-Options, X-Frame-Options, HSTS, Referrer-Policy, Permissions-Policy | Feature HTTP |

#### Specialized: Complexity `forge-verify/complexity/SKILL.md`

| Función | Input | Output | Proceso | Trigger |
|---------|-------|--------|---------|---------|
| `calcular_mcc()` | Función individual | MCC score (1 + if/for/while/case/catch/&&/||) | Cuenta puntos de decisión en la función | Cada función en el diff |
| `calcular_nesting_depth()` | Función individual | Máxima profundidad de indentación | Encuentra el nivel de anidamiento más profundo | MCC calculado |
| `calcular_cognitive_load()` | Función individual | Puntaje de carga cognitiva | +1 por nesting, +2 por side effects, +1 por magic numbers, +2 por boolean params, +2 por mismatch de abstracción | Nesting calculado |
| `detectar_code_smells()` | Función individual | Lista de smells (long method, long param list, primitive obsession, feature envy, data clumps, switch statement) | Detecta: > 30 líneas, > 4 params, string para email, usa getters de otra clase, mismos params repetidos, switch por tipo | Función analizada |
| `recomendar_extract_method()` | Función con bloques comentados o > 30 líneas | "Extraer método: validateOrder(), calculateTotals(), persistOrder()" | Identifica bloques por comentarios o por responsabilidad | Long method detectado |
| `recomendar_guard_clauses()` | Función con nesting > 3 | Versión con guard clauses (if not → return) | Invierte condicionales para aplanar la función | Arrow anti-pattern detectado |

#### Specialized: Performance `forge-verify/performance/SKILL.md`

| Función | Input | Output | Proceso | Trigger |
|---------|-------|--------|---------|---------|
| `auditar_n_plus_1()` | Endpoints del diff | 🚨 N+1 detectado → rework | Por cada endpoint: contar DB round trips. Si loop + query → N+1 | Endpoint con acceso a datos |
| `detectar_memory_leaks()` | Código con recursos | 🚨 Leak detectado (event handler sin unsubscribe, static collection, timer sin dispose, HttpClient sin reuse) | Escanea: ¿+= event sin -=? ¿static List/Dictionary? ¿new Timer()? ¿new HttpClient()? | Código con recursos |
| `verificar_benchmark()` | Benchmarks provistos por dev | ✅ Benchmark válido o 🚨 No válido | ¿Aislamiento? ¿Iteraciones ≥ 100? ¿Determinístico? ¿Allocations medidas? ¿Baseline comparada? | RNF-PERF en spec |
| `analizar_big_o()` | Algoritmos del código | Notación Big-O de cada algoritmo + alerta si O(n²) o superior | ¿Nested loops? ¿List vs HashSet? ¿String concat en loop? ¿Recursión sin memoization? | Código con loops o recursión |
| `auditar_resource_limits()` | Configuración de la app | Checklist: timeout configurado, connection pool, thread pool, file upload limit, response size limit, retry con backoff, circuit breaker | Verifica configuraciones de producción | Siempre |

#### Specialized: A11y `forge-verify/a11y/SKILL.md`

| Función | Input | Output | Proceso | Trigger |
|---------|-------|--------|---------|---------|
| `auditar_html_semantico()` | Template/JSX/HTML del diff | X/5 checks semánticos pasados | ¿<button> no <div>? ¿headings orden lógico? ¿<nav>? ¿<main> único? ¿<label> asociado? | Feature con UI |
| `auditar_aria()` | Atributos ARIA en el diff | X/7 checks ARIA pasados | ¿role? ¿alt text? ¿aria-label vs visible? ¿aria-live? ¿aria-expanded? ¿aria-current? ¿aria-describedby? | UI existente |
| `auditar_keyboard()` | Elementos interactivos | X/7 checks teclado pasados | ¿Tab reachable? ¿orden lógico? ¿focus visible? ¿Enter/Space? ¿skip-to-content? ¿sin traps? ¿ARIA practices? | UI interactiva |
| `auditar_contraste()` | Colores en CSS/styles | X/4 checks contraste pasados | 4.5:1 texto, 3:1 large text, 3:1 UI components, no color-only info | UI con colores |
| `emitir_veredicto_a11y()` | Resultados de auditoría | PASS / REWORK | Si bloqueos → rework. Si solo recomendaciones → PASS condicional | Auditorías completadas |

**Gaps detectados**:
1. `verify/core/ejecutar_tests()` depende de poder correr el test suite — en entornos donde no hay runtime (ej: solo chat), esto no es posible. No hay fallback claro.
2. `verify/security/auditar_owasp_top10()` no tiene acceso a un SAST real (SonarQube, Semgrep) — solo hace escaneo mental. Faltan herramientas reales.
3. `verify/performance/verificar_benchmark()` requiere que el dev haya escrito benchmarks — si no los escribió, no hay nada que verificar.
4. No hay `verify/core/verificar_trazabilidad_rf()` — el core skill menciona `mem_traceability` pero no especifica cómo verificar que cada test tiene el prefijo [RF-XXX].

---

### 7. forge-memory — Fase 4

**Rol**: Cerrar el ciclo: sintetizar, persistir, promover.
**Checkpoint**: CKP-4 🟢

---

#### Core Skill `forge-memory/SKILL.md`

| Función | Input | Output | Proceso | Trigger |
|---------|-------|--------|---------|---------|
| `detectar_topicos_evolutivos()` | Título de observación a guardar | topic_key normalizado | `mem_suggest_topic_key(title)` → devuelve key estable como `architecture/auth-model` | Antes de mem_save |
| `sintetizar_offline()` | Archivos en `.engram/local_memory/` | Observaciones consolidadas deduplicadas | Lee archivos locales, filtra noise (debug prints), mergea bugs similares, comprime en 1 observación canónica | Modo offline detectado |
| `promover_a_adr()` | Decisión de arquitectura importante | ADR.md en `docs/decisions/` + índice actualizado | `mem_promote_to_md(obs_id)` + `mem_sync_md_to_repo()` | Decisión permanente detectada |
| `aplicar_retention_prune()` | Ninguno (automático) | DB limpiada de observaciones expiradas | `mem_doctor()` (healthcheck) + `mem_retention_prune()` (TTL cleanup) | Al inicio y cierre de sesión |
| `escribir_session_summary()` | Todo el ciclo de la feature | Session summary con: Goal, Discoveries, Accomplished, Next Steps, Relevant Files | Sintetiza aprendizajes del ciclo en formato estructurado | Cierre de sesión |

#### Specialized: Metrics `forge-memory/metrics/SKILL.md`

| Función | Input | Output | Proceso | Trigger |
|---------|-------|--------|---------|---------|
| `calcular_coverage_delta()` | Coverage antes/después + tests agregados | Observación `metrics/test-coverage` | Busca cobertura previa en mem_search, calcula delta, guarda tendencia | Feature completa |
| `medir_cycle_time()` | Tiempo por fase (horas) | Observación `metrics/cycle-time` | Suma horas de discovery + spec + plan + implementación + verify | Feature completa |
| `evaluar_tech_debt()` | Reportes de complexity + SOLID + TODOs | Observación `metrics/tech-debt` | Cuenta code smells, SOLID violations, TODO/FIXME agregados | Feature completa |
| `analizar_tendencias()` | Últimas 5 observaciones de métricas | 🟢 Improving / 🟡 Stable / 🔴 Worsening por métrica | Compara valores actuales vs tendencia de las últimas 5 features | Feature completa |
| `emitir_health_verdict()` | Todas las métricas + tendencias | Health snapshot: coverage, cycle time, complexity, TODOs, reworks | Verdict: 🟢 HEALTHY / 🟡 STABLE / 🔴 AT RISK + recomendaciones | Análisis de tendencias listo |

#### Specialized: Changelog `forge-memory/changelog/SKILL.md`

| Función | Input | Output | Proceso | Trigger |
|---------|-------|--------|---------|---------|
| `parsear_git_log()` | Commits desde último tag | Lista de commits clasificados por tipo | `git log oneline --no-decorate` desde último tag. Clasifica: feat → Added, fix → Fixed, perf → Performance, sec → Security | Pre-release |
| `compilar_changelog()` | Commits clasificados + features del release | CHANGELOG.md en formato Keep a Changelog | Agrupa commits por sección (Added/Changed/Fixed/Security/Performance/Deprecated) con enlaces a commits | Commits clasificados |
| `verificar_release_readiness()` | Estado del release | Checklist: verify PASS, 0 reworks, tests verdes, changelog generado, tag creado | Verifica condiciones para release | Changelog compilado |
| `persistir_release_metadata()` | Versión + descripción del release | Observación `release/vX.Y.Z` en engram | Guarda el release como observación para trazabilidad futura | Release listo |

#### Specialized: Knowledge `forge-memory/knowledge/SKILL.md`

| Función | Input | Output | Proceso | Trigger |
|---------|-------|--------|---------|---------|
| `detectar_afectacion_cross_project()` | Observación a guardar | Lista de proyectos afectados + tipo de relación | Analiza si la decisión afecta a otros proyectos (API change, schema change, deprecation) | Observación con impacto potencial cross-project |
| `crear_cross_reference()` | Observación + proyecto afectado | Observación cross-ref en el proyecto destino | Guarda en engram: "CROSS-REF: Auth migration affects service-web" con type=discovery y scope=team | Afectación detectada |
| `buscar_contexto_multi_repo()` | Keywords de la feature | Observaciones relevantes de otros proyectos | `mem_search(keyword, project=related_project)` en cada proyecto conocido | Feature relacionada a otro proyecto |
| `linkear_adrs_cross_project()` | ADR promovido | ADR.md con sección Cross-References + enlaces a ADRs de otros proyectos | Agrega en el frontmatter del ADR las referencias a proyectos afectados | ADR promovido |
| `alertar_breaking_change()` | Breaking change detectado | Alerta al orquestador + lista de equipos afectados | Si el cambio afecta API, schema o dependencia de otro proyecto → alerta | Cambio con impacto cross-project detectado |

**Gaps detectados**:
1. `metrics/medir_cycle_time()` no tiene un mecanismo para medir el tiempo REAL — depende de que el humano reporte horas. No hay tracking automático.
2. `metrics/evaluar_tech_debt()` depende del reporte de verify/complexity — si ese skill no se cargó, no hay datos de code smells.
3. `changelog/parsear_git_log()` requiere que los commits sigan conventional commits — si no, la clasificación falla.
4. `knowledge/buscar_contexto_multi_repo()` requiere acceso a múltiples proyectos de engram-dotnet — si no están registrados, no hay cross-project visibility.
5. No hay `memory/core/promover_a_wiki()` — solo promueve a ADR.md. No hay exportación a wiki o documentación viva.

---

## PARTE 2: FLUJOS DE TRABAJO COMPLETOS

### Flujo 1: Happy Path (Feature Simple)

```
Usuario: "Agregar endpoint GET /users/{id} que devuelva datos del usuario"
   │
   ├── forge-orchestrator.evaluar_fase() → new
   │
   ├── forge-discovery
   │   ├── extraer_keywords("GET /users/{id}", "user data")
   │   ├── mem_search_engram() → sin contexto previo de users
   │   └── evaluar_ambigüedad() → CLARO ✅
   │
   ├── CKP-0: superado ✅
   │
   ├── forge-arch
   │   ├── escribir_objetivo() → "Endpoint GET /users/{id}"
   │   ├── definir_rf() → RF-001: GET /users/{id} retorna 200 + UserDto
   │   ├── definir_rnf() → RNF-001: Response time < 200ms
   │   └── definir_capability_matrix() → ai_reasoning [formato error] deterministic [auth check, DB query]
   │
   ├── CKP-1: humano aprueba spec ✅
   │
   ├── forge-plan
   │   ├── analizar_impacto() → 3 archivos: controller, service, repository
   │   ├── ordenar_tareas_topologicas() → 1. UserRepository 2. UserService 3. UsersController 4. Tests
   │   └── definir_contratos() → UserDto { id, name, email }
   │
   ├── CKP-2: humano da luz verde ✅
   │
   ├── forge-dev.ralph_wiggum_loop()
   │   ├── implementar_plan() → código de repository, service, controller
   │   ├── escribir_tests_unitarios() → RF-001 scenarios
   │   └── compila + tests verdes después de 1 ciclo ✅
   │
   ├── forge-verify
   │   ├── inspeccionar_linea_por_linea() → sin errores
   │   ├── verificar_constantes_spec() → OK
   │   └── ejecutar_tests() → 100% verde
   │
   ├── CKP-3: PASS ✅ (cycle_count = 0)
   │
   ├── forge-memory
   │   ├── escribir_session_summary()
   │   └── mem_save(id=..., title="GET /users/{id} endpoint", type=feature)
   │
   └── CKP-4: humano decide deploy ✅
```

**Duración estimada**: 10-15 min con un modelo rápido.

---

### Flujo 2: Feature con Seguridad + Refactor

```
Usuario: "Migrar auth de JWT a OAuth 2.0"
   │
   ├── forge-discovery
   │   ├── extraer_keywords("OAuth 2.0", "JWT", "auth migration")
   │   ├── mem_search_engram("auth", "JWT", "token") → encuentra ADR de auth actual
   │   └── FORGE-DISCOVERY-SECURITY
   │       ├── escanear_cves_stack("passport", "oauth2") → 0 CVEs críticos
   │       └── evaluar_riesgo_dependencia("oauth2-library") → LOW
   │
   ├── CKP-0: superado ✅ (contexto encontrado + seguridad evaluada)
   │
   ├── forge-arch
   │   ├── FORGE-ARCH-SECURITY
   │   │   └── aplicar_stride() → Spoofing (check redirect URIs), DoS (rate limit on /token)
   │   ├── FORGE-ARCH-DOMAIN
   │   │   ├── identificar_contextos() → Auth + User Profile
   │   │   └── definir_lenguaje_ubicuo() → Token, Grant Type, Scope, Client ID, Redirect URI
   │   └── sí: CAPABILITY MATRIX: deterministic: [validate redirect_uri, enforce pkce]
   │
   ├── CKP-1: humano aprueba spec ✅
   │
   ├── forge-plan
   │   ├── FORGE-PLAN-SECURITY
   │   │   ├── anotar_tareas_sec() → login flow, token refresh, logout marked [SEC]
   │   │   └── verificar_asvs_authentication() → pkce check, redirect validation
   │   ├── FORGE-PLAN-PATTERNS
   │   │   └── seleccionar_patron_enterprise() → Strategy: OAuth providers (Google, GitHub, Custom)
   │   ├── FORGE-PLAN-ROLLBACK
   │   │   ├── seleccionar_estrategia_deploy() → Feature Flag (NEW_AUTH)
   │   │   └── diseniar_feature_flag() → [FEATURE_FLAG: NEW_AUTH] 10% → 50% → 100%
   │   └── checklist: 12 tareas con 5 [SEC] + 1 [PATTERN: Strategy] + 1 [FEATURE_FLAG]
   │
   ├── CKP-2: humano da luz verde ✅
   │
   ├── forge-dev
   │   ├── implementar_plan() → código OAuth flow
   │   ├── FORGE-DEV-SECURITY
   │   │   ├── verificar_owasp_a01() → redirect_uri validated ✅
   │   │   ├── verificar_owasp_a07() → pkce enforced, state parameter ✅
   │   │   └── escanear_red_flags() → 0 red flags ✅
   │   ├── FORGE-DEV-SOLID
   │   │   ├── auditar_ocp() → OAuthProvider interface ✅ EXTENSIBLE
   │   │   ├── auditar_dip() → Provider injected through constructor ✅
   │   │   └── calcular_puntaje_solid() → 5/5 ✅
   │   └── Ralph Wiggum: 2 ciclos (1 error en PKCE validation) → verde ✅
   │
   ├── forge-verify
   │   ├── FORGE-VERIFY-SECURITY
   │   │   ├── auditar_flujo_auth() → auth antes de business logic ✅
   │   │   ├── auditar_data_flow() → code → token → validated claims ✅
   │   │   └── escanear_secretos() → 0 secrets ✅
   │   ├── FORGE-VERIFY-COMPLEXITY
   │   │   ├── calcular_mcc() → AuthService: 12 (MEDIUM) ⚠️
   │   │   └── recomendar_extract_method() → split validateGrant + exchangeCode + buildToken
   │   └── REWORK cycle 1: refactor AuthService → extract methods
   │
   ├── forge-dev (cycle 2)
   │   └── FORGE-DEV-REFACTOR
   │       ├── extract_method() → 3 funciones extraídas
   │       └── preservar_tests_durante_refactor() → tests VERDES ✅
   │
   ├── forge-verify (cycle 2)
   │   ├── FORGE-VERIFY-COMPLEXITY → MCC: 8 (LOW) ✅
   │   └── FORGE-VERIFY-SECURITY → PASS ✅
   │
   ├── CKP-3: PASS (cycle_count = 2) ✅
   │
   ├── forge-memory
   │   ├── FORGE-MEMORY-KNOWLEDGE
   │   │   └── detectar_afectacion_cross_project() → service-api, service-mobile affected
   │   ├── FORGE-MEMORY-METRICS
   │   │   ├── medir_cycle_time() → 45 min total
   │   │   └── emitir_health_verdict() → 🟢 HEALTHY
   │   └── Session summary + ADR-013: "Migrated auth to OAuth 2.0"
   │
   └── CKP-4: humano decide deploy ✅
```

**Duración estimada**: 45-60 min (incluye 1 ronda de rework por complejidad)

---

### Flujo 3: Feature Bloqueada en CKP-0

```
Usuario: "Mejorar el login"
   │
   ├── forge-discovery
   │   ├── extraer_keywords("mejorar", "login") → muy genérico
   │   └── evaluar_ambigüedad() → VAGO 🔴
   │
   └── CKP-0: HARD STOP 🔴
       forge-orchestrator: "Tu pedido es muy vago. 'Mejorar el login' puede significar:
       a) Agregar OAuth (Google, GitHub)
       b) Cambiar UI del formulario de login
       c) Agregar 2FA
       d) Optimizar velocidad de autenticación
       
       Por favor, especificá cuál de estas opciones (o describí qué mejora necesitás)."
```

**Resultado**: El flujo no avanza hasta que el humano clarifica. Sin excepción.

---

### Flujo 4: Feature con Escalación en CKP-3

```
Usuario: "Agregar reportes con agregaciones complejas en SQL"
...
   ├── forge-dev (cycle 1)
   │   └── código con N+1 queries en las agregaciones
   │
   ├── forge-verify (cycle 1)
   │   └── FORGE-VERIFY-PERFORMANCE
   │       └── auditar_n_plus_1() → 15 queries por request 🚨
   │       → REWORK ticket cycle 1/3
   │
   ├── forge-dev (cycle 2)
   │   └── corrige N+1... pero introduce memory leak
   │
   ├── forge-verify (cycle 2)
   │   └── FORGE-VERIFY-PERFORMANCE
   │       └── detectar_memory_leaks() → static list grows unbounded 🚨
   │       → REWORK ticket cycle 2/3
   │
   ├── forge-dev (cycle 3)
   │   └── corrige leak... pero rompe test existente
   │
   ├── forge-verify (cycle 3)
   │   └── ejecutar_tests() → 1 test falla 🚨
   │       → cycle_count = 3 → 🔴 CKP-3 ENGAGE
   │
   └── CKP-3: FRENO DE EMERGENCIA 🔴
       forge-orchestrator: "El agente Dev falló 3 veces:
       Cycle 1: N+1 queries (15 por request)
       Cycle 2: Memory leak (static list sin límite)
       Cycle 3: Test existente roto
       
       Revisión manual requerida. ¿Querés:
       a) Revisar el código vos mismo
       b) Modificar el plan.md y resetear el ciclo
       c) Descartar la feature?"
```

**Resultado**: El sistema no permite un 4to intento automático. El humano debe intervenir.

---

## PARTE 3: GAPS DETECTADOS (30 en total)

### 🚨 Gaps Críticos (bloquean el flujo)

| # | Gap | Skills afectadas | Solución propuesta |
|---|-----|-----------------|-------------------|
| 1 | No hay `rollback_de_fase()` — si humano rechaza spec/plan, no hay ciclo formal | orchestrator/core | Agregar función que permita re-apertura controlada de fase anterior con contexto |
| 2 | `verify/core/ejecutar_tests()` no funciona sin runtime (chat-only) | verify/core | Fallback: pedir al humano output de tests, o usar análisis estático de cobertura |
| 3 | No hay SAST real — solo escaneo mental del LLM | verify/security | Integrar con Semgrep/SonarQube vía MCP cuando esté disponible |
| 4 | `metrics/medir_cycle_time()` no tiene tracking automático de tiempo | memory/metrics | Agregar timestamps automáticos al inicio/fin de cada fase |

### ⚠️ Gaps Importantes (degradan la calidad)

| # | Gap | Skills afectadas | Solución propuesta |
|---|-----|-----------------|-------------------|
| 5 | No hay `definir_stack_tecnologico()` en arch | arch/core | Arch debería verificar que el stack es adecuado antes de escribir spec |
| 6 | No hay `definir_contratos_api()` formal (OpenAPI) | arch/core | spec.md debería incluir contracts OpenAPI/gRPC |
| 7 | `patterns/` depende del conocimiento del modelo, no de un catálogo formal | plan/patterns | Crear catálogo en skills/forge-plan/patterns/references/ |
| 8 | No hay `estimar_esfuerzo()` por tarea | plan/core | Sin estimación vs real, las métricas de cycle time son débiles |
| 9 | `testing/escribir_fuzzing()` no es específico por lenguaje | dev/testing | Agregar secciones por lenguaje (JS, .NET, Python, Java, Go) |
| 10 | No hay `testing/cobertura_mutation_tool()` real | dev/testing | Recomendar Stryker (JS), PIT (.NET), MutPy (Python) |
| 11 | `performance/detectar_n_plus_1()` es escaneo estático solamente | dev/performance | Necesita profiler real o al menos ORM logging configurado |
| 12 | No hay `refactor/split_large_class()` | dev/refactor | Agregar extract class pattern con algoritmo de identificación de responsabilidades |
| 13 | `changelog/parsear_git_log()` requiere conventional commits | memory/changelog | Si no hay conventional commits, parsear mensajes completos con LLM |
| 14 | `knowledge/buscar_contexto_multi_repo()` requiere múltiples proyectos registrados | memory/knowledge | Documentar requisito de setup multi-repo en CLI Wizard |

### 🔵 Gaps Menores (mejoras futuras)

| # | Gap | Skills afectadas | Solución propuesta |
|---|-----|-----------------|-------------------|
| 15 | No hay `analisis_competencia()` en discovery | discovery | ¿Hay alternativas OSS que resuelvan esto? |
| 16 | No hay `factibilidad_tecnica()` — ¿la API externa existe? | discovery | Verificar documentación disponible antes de planificar |
| 17 | `compliance/identificar_regulaciones()` no detecta tipos de datos automáticamente | discovery/compliance | Analizar el código existente para detectar campos PII |
| 18 | `a11y/determinar_nivel_wcag()` necesita configuración del proyecto | arch/a11y | Agregar campo wcag_level en .flowforge.json |
| 19 | No hay `definir_orden_deploy()` (tareas paralelizables vs secuenciales) | plan/patterns | Agregar análisis de dependencias entre tareas para deploy |
| 20 | `rollback/calcular_nivel_riesgo()` es subjetivo | plan/rollback | Agregar matriz de riesgo con pesos objetivos (schema=3, payment=5, auth=4, etc.) |
| 21 | No hay `memory/core/promover_a_wiki()` | memory/core | Exportación a wiki viva (Confluence, Notion, Obsidian) |
| 22 | `metrics/evaluar_tech_debt()` depende de verify/complexity report | memory/metrics | Si no hay reporte, calcular estimación básica desde simple grep de TODOs y FIXMEs |
| 23 | No hay tracking automático de tiempo por fase | memory/metrics | Timestamps automáticos al pasar cada CKP |
| 24 | No hay verificación de trazabilidad [RF-XXX] en tests | verify/core | Agregar grep obligatorio de prefijos en nombres de tests |

### 🔮 Gaps de Incubadora (de docs/13-edge-cases-and-risks.md)

| # | Gap | Fuente |
|---|------|--------|
| 25 | Context Poisoning Guardrail — validar engramas viejos antes de usarlos | Edge Cases §2 |
| 26 | Conflict Resolution Agent — detectar colisiones entre agentes en el mismo namespace | Edge Cases §2 |
| 27 | Cost Observability Dashboard — costo en USD por fase/épica | Edge Cases §2 |
| 28 | Drift Health Check — comparar código actual vs plan.md cada N commits | Edge Cases §3 |
| 29 | Message Queue para escrituras .md — evitar colisiones en archivos de respaldo | Edge Cases §1 |
| 30 | Lineage Enforcement en CKP-3 — bloquear si el linaje de datos no es válido | Edge Cases §5 |

---

## CONCLUSIÓN

FlowForge tiene **30 skills funcionales** que cubren **7 roles de agente** en **5 fases** con **5 checkpoints**.

**Lo que funciona bien:**
- El flujo base (happy path) está completo y documentado
- Las skills de seguridad cubren OWASP Top 10 + STRIDE + ASVS en todas las fases
- Las skills de performance cubren N+1, caching, memory leaks, Big-O
- Los checkpoints tienen semántica clara (🔴 binario, 🟡 flexible, 🟢 consulta)
- AGENTS.md tiene el índice completo para que los agentes sepan qué cargar

**Los 4 gaps críticos a resolver:**
1. `rollback_de_fase()` — reintentar spec/plan rechazado
2. Fallback para `ejecutar_tests()` sin runtime
3. SAST real vs escaneo mental
4. Tracking automático de tiempo para métricas

**Los gaps de incubadora (25-30)** son features post-MVP que harían de FlowForge un sistema mucho más robusto, especialmente el Context Poisoning Guardrail y el Drift Health Check.

---

> **Última actualización**: 2026-05-25
> **Commits**: todos en main (01e4b5b, e7fcab6, 3798f22, c3ea8ae, 1962d1c, 1e1f371, 36b45ec)
> **30 gaps detectados**: 4 críticos, 10 importantes, 10 menores, 6 de incubadora
