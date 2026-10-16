# ADR-026 — `/fforge-init`: arquitectura híbrida de skill (SKILL.md orquestador + engine bash zero-dependency) con `plan.json` como seam

> **Status**: Accepted
> **Date**: 2026-10-16
> **Feature**: `fforge-init` (HU-047)
> **Deciders**: FlowForge methodology team
> **Links**: [HU-047](../../docs/tasks/HU-001-HU-099/HU-047-fforge-init.md) · [spec](../../.ai-work/fforge-init/spec.md) · [plan](../../.ai-work/fforge-init/plan.md) · [contracts](../../skills/forge-init/contracts.md) · [SKILL.md](../../skills/forge-init/SKILL.md) · [engine](../../skills/forge-init/scripts/fforge-init.sh) · [PM-TEST-RESULTS](../../.ai-work/fforge-init/PM-TEST-RESULTS.md) · [ADR-022](ADR-022-opencode-agents-md-merge.md) · [ADR-016](ADR-016-update-mechanism-by-component.md) · [ADR-025](ADR-025-post-plan-decision-extraction.md)

---

## Contexto

Al heredar un proyecto de otro equipo u otra herramienta, el `AGENTS.md` del proyecto suele traer
configuración que choca con las skills y el flujo globales de FlowForge. Actualizarlo a mano, proyecto
por proyecto, es tedioso y propenso a errores, y el riesgo real no es el trabajo: es **perder
configuración específica del proyecto** que nadie recuerda haber escrito.

HU-047 pidió una skill interactiva `/fforge-init` que inicializara o sincronizara `AGENTS.md` sin
perder esa configuración. La HU se derivó de forma **stateless** (sin pasar por `forge-discovery`),
así que no había constraints de decisiones previas — pero sí había dos anclas duras:

1. **La Capability Matrix del spec parte el trabajo en dos naturalezas distintas.**
   `ai_reasoning`: heurísticas de clasificación de skills, sugerencias ante conflictos de merge
   semántico, recomendación de fragmentos por contexto de proyecto.
   `deterministic`: detección de archivos, backup/restore, copia y concatenación de templates,
   string matching de prefijos, parsing de secciones markdown.
   Un solo medio no sirve bien a las dos.

2. **NFR-006 exige autocontención total**: cero dependencias externas, sin llamadas de red, sin
   package manager, para que la skill funcione en cualquier entorno. La spec además nombra
   explícitamente `.agents.md.backup` (NFR-004/OQ-3) y PM-5, y la HU §7 proponía una estructura
   `src/*.ts`.

El repo ya tenía precedente propio para lo determinista: `install-skills.sh` y `flow-init.sh` son bash
sin dependencias npm; y ninguna skill de FlowForge publica código ejecutable más allá de markdown
(`find skills -type f -not -name SKILL.md` → 2 archivos, ambos markdown).

---

## Decision drivers

- **Autocontención (NFR-006, no negociable)**: la skill debe correr en el proyecto destino sin asumir
  un runtime que ese proyecto no tenga. Nada de Node/npm, nada de red.
- **Determinismo verificable**: el merge (FR-007) y el rollback (NFR-004) no pueden re-derivar lógica
  de strings de forma distinta en cada ejecución. Deben ser auditables y reproducibles desde un
  artefacto, no desde un transcript de conversación.
- **Cero pérdida de datos**: contenido del usuario intacto salvo decisión explícita; backup antes del
  primer byte; `--dry-run` honesto.
- **Seguridad por diseño**: rechazo de path-escape, symlinks, archivos >1 MB y contenido binario;
  escritura atómica; ningún valor de `plan.json` interpolado en shell.
- **Trazabilidad de decisiones (NFR-003)**: el usuario debe ver cada elección antes de que se escriba.
- **Reutilizar el precedente del repo**: no inventar un segundo parser de `AGENTS.md` cuando
  `AgentsMdMerger.cs` ya resuelve secciones, conflictos, escritura atómica y guards de seguridad.
- **Degradación, nunca bloqueo**: si bash no está disponible (Windows sin Git Bash), la skill sigue
  siendo ejecutable de forma degradada.

---

## Options considered

### Option A — Engine en Node/TypeScript (`src/*.ts`), como proponía HU-047 §7

**Pros**: el ecosistema del repo ya tiene tooling JS; tipos compartidos entre parser y merge.
**Cons**: introduce un runtime Node y una superficie de package manager en el proyecto destino.
Contradice NFR-006 de frente, y ninguna otra skill de FlowForge publica código ejecutable.
**Rejected** — el costo de runtime recae en el adoptante, no en FlowForge.

### Option B — Skill puramente prompt: el LLM ejecuta todos los pasos con herramientas nativas

**Pros**: cero archivos nuevos, nada que instalar, funciona en cualquier IDE.
**Cons**: el merge semántico, el union de listas, el cómputo de hash y el orden backup→write→rename
se re-derivarían por LLM en cada ejecución, sin forma de escribir un test determinista. `--dry-run`
dejaría de ser confiable (no ejecutaría el mismo camino que `apply`) y PM-3/PM-5 no serían reproducibles.
**Rejected** — vuelve el rollback y el merge no verificables, que es justo lo que el spec exige probar.

### Option C — Híbrido: SKILL.md orquesta el juicio, engine bash ejecuta lo determinista (ELEGIDA)

`SKILL.md` posee el diálogo de 6 pasos y **todas** las decisiones de criterio (qué secciones
mantener, qué hacer con cada skill externa, qué fragmentos activar, cómo resolver cada conflicto).
Un script POSIX (`scripts/fforge-init.sh`) posee **toda** operación determinista sobre archivos, detrás
de un contrato congelado. El canal entre ambos es un único archivo declarativo: `plan.json`.

**Pros**: cada mitad usa el medio que le corresponde; el juicio se congela en un artefacto antes de
escribir; el determinismo es testeable sin agente en el loop; `--dry-run` reusa el mismo código que
`apply`; NFR-006 se respeta porque el runtime es bash + awk/sed/grep/find/sha256sum, que ya asume el
repo (`install-skills.sh`, `flow-init.sh`); degradación explícita cuando bash no existe.
**Cons**: dos artefactos que pueden divergir → mitigado congelando `contracts.md` como fuente única de
verdad; la lógica del engine es bash (menos ergonómica de mantener que un lenguaje tipado); el seam
`plan.json` añade un paso de serialización que el agente debe construir correctamente.
**Accepted trade-offs**, y el contrato es lo que los hace manejables.

### Option D — Engine en C# reusando `FlowForge.Installer`

**Pros**: reuso máximo de `AgentsMdMerger`/`BackupManager`/`AtomicWriter`; un solo lenguaje.
**Cons**: requeriría el binario del installer disponible en el proyecto destino — exactamente lo que
NFR-006 prohíbe. `FlowForge.Installer` gestiona el `AGENTS.md` **global** (`~/.config/opencode/`),
no el de un proyecto arbitrario.
**Rejected** — dependencia de runtime más pesada aún que Node.

---

## Decision

Se adopta la **Option C**. Decisiones concretas, congeladas en `skills/forge-init/contracts.md` (C-1…C-10):

1. **División de responsabilidades (DEC-1).** `SKILL.md` = razonamiento, diálogo y las cuatro decisiones
   de criterio del flujo (secciones, skills externas, fragmentos, conflictos). `scripts/fforge-init.sh`
   = detección, análisis de secciones, clasificación por prefijo, recomendación, merge, backup,
   restore y escritura. El engine nunca decide; el agente nunca escribe archivos por su cuenta.

2. **`plan.json` como seam único y declarativo (DEC-4).** Toda decisión juzgada por el agente se
   congela en un `plan.json` antes de escribir un solo byte. El engine valida ese plan contra los
   invariantes del contrato (§C-3) y lo aplica. Una violación de invariante sale con **exit 6** y
   mensaje preciso, en vez de ser suavizada. Corolario: `--dry-run` ejecuta el **mismo** camino de
   código con las escrituras deshabilitadas — verificado por el harness (`preview --json` y
   `apply --json` reportan el idéntico `managedBlockHash`), que es lo que hace honesto al dry-run.

3. **Modelo de secciones adoptado de `AgentsMdMerger` (DEC-2).** El análisis produce
   `MarkdownSection(Name, ..., StartLine, EndLine)` con rangos de línea 1-based — exactamente la forma
   que FR-003 exige. Los dos caminos del repo que manipulan un `AGENTS.md` hablan el mismo idioma y se
   comparan como iguales, en lugar de crecer un segundo parser sutilmente distinto al lado del primero.
   Se heredan además sus guards de seguridad (path-escape, symlink, cap de 1 MB, validación UTF-8/NUL).

4. **Marcadores de bloque gestionado para hacer segura la re-ejecución (DEC-3).** El comando se llama
   "init *o sincronizar*", así que correrlo dos veces es un caso de primera clase, no un edge case. La
   región generada se envuelve en `<!-- fforge-init:begin v=1 hash=… --> … <!-- fforge-init:end -->`,
   generalizando el patrón ya establecido por ADR-022 (`<!-- gentle-ai:flowforge -->` + hash embebido,
   `MergeAction.IdempotentNoOp`). Consecuencias: archivo sin cambios + plan idéntico ⇒ **no-op real**
   (exit 0, sin churn de backup); hash distinto ⇒ *drift* reportado y **preguntado**, nunca sobrescrito
   en silencio. A diferencia de ADR-022, **cero bloques es legal** (significa "nunca generado por
   `/fforge-init`", que es el caso primario del FR-002/PM-2); solo **>1 bloque** es exit 5.

5. **Backup project-local `.agents.md.backup`, divergiendo del backup root del installer (DEC-5).**
   La spec (NFR-004, PM-5) y OQ-3 nombran esa ruta explícitamente, así que gana la spec. La divergencia
   respecto de `~/.flowforge-backups/{component}-{timestamp}/` (ADR-016) es deliberada y acotada: el
   target del installer es el `AGENTS.md` **global**, mientras el de esta skill es el `./AGENTS.md`
   **project-local**; un artefacto de rollback local debe viajar con el proyecto y ser descubrible por
   quien corrió el comando. Los dos dueños de un `AGENTS.md` no pueden pisarse entre sí.

6. **Semántica de escritura y rollback (§C-9).** Stage a `.AGENTS.md.tmp` en el directorio del target →
   `mv` atómico sobre el target. El backup se estagea como `.agents.md.backup.tmp` y se renombra sobre
   `.agents.md.backup` **solo tras** una escritura completamente exitosa, de modo que un fallo deja
   intacto el backup *previo* (NFR-004: "overwritten only on successful completion"). Fallo de escritura
   ⇒ auto-restore del target, limpieza de `*.tmp`, **exit 7**. `restore` copia el backup sobre el target
   y luego lo **consume**. Sin backup ⇒ exit 2.

7. **Exit codes congelados 0–7** (`contracts.md` §C-8), y **todo código ≥1 imprime en stderr qué falló,
   por qué y cómo arreglarlo** (NFR-002 como término de contrato, no como cortesía). La regla de bloqueo
   de FR-005 tiene respaldo en el engine: mientras exista una skill externa sin decisión —o una decisión
   `review` sin `--allow-review-todos`— el engine sale con **exit 4 antes de tocar el archivo**. El flag
   nunca relaja una skill *indecisa*.

8. **Convenciones del contrato (CONV-1, CONV-3).** Todo subcomando soporta `--json` o no se publica
   (permite al harness asertar sobre datos, no sobre prosa, y mantiene `--dry-run`/`--json` de acuerdo).
   El directorio de la skill **debe** llamarse `forge-*` para ser instalable — `install-skills.sh`
   descubre con el glob `"$SOURCE_SKILLS_DIR"/forge-*/`, así que un directorio `fforge-init` (como
   escribía HU-047 §3.1) sería **silenciosamente ignorado** por el installer. De ahí `skills/forge-init/`
   con `/fforge-init` como nombre de comando de cara al usuario.

9. **Degradación explícita (fallback de DEC-1).** Sin bash disponible, `SKILL.md` instruye al agente a
   ejecutar los mismos pasos deterministas con herramientas nativas de archivo — degradado, pero nunca
   bloqueado, y anunciado desde el principio en vez de fallar a mitad del flujo.

---

## Consecuencias

**Positivas:**

- NFR-006 cumplido de verdad: el engine corre con bash + awk/sed/grep/find/sha256sum, sin red, sin jq,
  sin Node, y todos los templates viven dentro de `skills/forge-init/`. Verificado además contra una copia
  *instalada* de la skill y a través del symlink relativo `./.agents/skills` (prueba de autocontención).
- El merge y el rollback son **verificables sin un agente en el loop**: 212/212 aserciones del harness
  (`tests/fforge-init/run-tests.sh`, bash-only) + 28 tests de contrato C# (`FforgeInitScriptTests`, 273/273
  en la suite del installer).
- Preservación byte-for-byte de lo que el usuario decide conservar, y **cero pérdida silenciosa**: una
  skill externa sin decisión se *retiene*, no se descarta; `omit` exige selección explícita.
- Idempotencia real: re-ejecutar con plan idéntico sobre un archivo sin cambios es un no-op con exit 0 y
  sin churn de backup; el drift se reporta y se pregunta.
- `--dry-run` es confiable por construcción (mismo camino de código), no por disciplina del autor.
- Los dos caminos que manipulan `AGENTS.md` (installer global, skill project-local) quedan explícitamente
  separados, así que ADR-022 (global) y este ADR (project-local) no se contradicen.

**Negativas / aceptadas:**

- Dos artefactos (SKILL.md, engine) que pueden divergir. Mitigación: `contracts.md` es la fuente única de
  verdad y una divergencia es un bug de cualquiera de los dos lados; el lado que difiere se corrige contra
  el contrato, y si el contrato está mal, se corrige el contrato **primero**.
- El engine es bash: menos ergonómico de mantener que un lenguaje tipado, y la ausencia de `fsync`
  portable está documentada en vez de simulada — la garantía es stage + `mv` atómico en el mismo
  filesystem (el stage vive en el directorio del target). Registrado en `contracts.md` §C-9.5.
- El agente debe construir correctamente `plan.json`; un plan inválido sale con exit 6 y mensaje preciso,
  lo que lo hace ruidoso pero no silencioso.
- Once decisiones que el plan dejó abiertas fueron "fijadas" durante la implementación
  (`plan.md` §11, rulings 1–12) y documentadas como clarificaciones explícitas en `contracts.md` en vez
  de quedar como folklore. Ninguna contradice §3 del plan.
- Los fragmentos de Claude Code y Cursor se entregan *minimal-but-valid* en V1 (§6 Resolución 2) para que
  el menú del paso 5 y FR-009 Scenarios B/C sean honestos en lugar de opciones muertas; la profundidad
  por agente queda para V2, marcada in-place en los propios fragmentos.

**Riesgos:**

- **Riesgo**: un usuario edita a mano la región generada → mitigado por el hash del bloque gestionado:
  se reporta como drift y se pregunta, nunca se sobrescribe.
- **Riesgo**: el archivo cambia entre el `preview` que el humano aprobó y el `apply` → mitigado por
  `originalSha256` (exit 5) con re-check inmediatamente antes de la escritura (§C-10).
- **Riesgo**: un fragmento empieza a referenciar a otro y rompe NFR-005 → mitigado por la convención
  CONV-2 (markdown inerte) y por tests que combinan los 55 pares de fragmentos y rechazan fenced blocks,
  cross-references e ids duplicados.
- **Riesgo**: alguien "arregla" el naming de vuelta a `fforge-init` → mitigado por CONV-3 y la tabla de
  inconsistencias en `plan.md` §6 y `contracts.md`.

## Status History

| Date | Change |
|------|--------|
| 2026-10-16 | Accepted — arquitectura híbrida `SKILL.md` + engine bash zero-dependency, seam `plan.json`, bloques gestionados para re-sync idempotente y backup project-local. Feature `fforge-init` (HU-047) implementada y verificada: 212/212 harness + 273/273 `dotnet test`, verify-report PASS, PM-1…PM-5 ejecutados. |
