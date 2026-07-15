---
capability_matrix:
  ai_reasoning:
    - Redacción de `description:` en frontmatter de workflows (texto corto para picker `/`, coherente con propósito de cada comando)
    - Mensajes de error/aviso del doctor cuando faltan skills o frontmatter (tono accionable para el desarrollador)
    - Decisión de si un workflow con frontmatter malformado debe bloquear install o solo advertir en modo doctor interactivo
  deterministic:
    - Todo `ide/antigravity/workflows/flow-*.md` debe tener frontmatter YAML con clave `description:` en una sola línea
    - `ide/antigravity/rules/workflow.md` debe incluir `alwaysApply: true` en frontmatter (paridad con `.agents/rules/workflow.md`)
    - Destino global Antigravity en todos los canales de instalación = `~/.gemini/config/` (Linux) / `%USERPROFILE%\.gemini\config\` (Windows); nunca `%LOCALAPPDATA%\Google\Gemini\antigravity\`
    - Skills metodología = symlinks (o copia recursiva en fallback) desde `skills/forge-*/` hacia `config/skills/` (global) y `.agents/skills/` (proyecto)
    - El instalador no debe escribir ni depender de `skills.json` en proyecto ni global (ADR-009)
    - CI debe fallar si algún `flow-*.md` del pack fuente carece de `description:` o si `config/skills/forge-discovery/SKILL.md` no existe tras install
    - Reinstalar no debe eliminar frontmatter válido ya presente en destino (origen fuente corregido antes de copia)
---

# Spec: Paridad instalador Antigravity — workflows `/flow-*` y skill `forge-discovery`

## 1. Objective and scope

### Objetivo

Corregir la instalación de customizations FlowForge en **Antigravity** (Google Gemini IDE) para que:

1. Los siete comandos `/flow-*` aparezcan en el picker `/` y en Customizations → Workflows.
2. El skill **`forge-discovery`** (y el resto de skills `forge-*`) esté disponible en instalación **global** y **por proyecto**, en **Linux y Windows**, con paridad entre `flowforge install` (C#), `ide/install.sh` y `ide/install.ps1`.

### Problema (síntomas confirmados)

| Síntoma | Causa raíz |
|---------|------------|
| `/flow-start`, `/flow-plan`, etc. ausentes (0 o solo `/flow-status`) | Pack fuente `ide/antigravity/workflows/` tiene 6/7 archivos **sin** frontmatter YAML `description:`; Antigravity parser devuelve 0 resultados |
| Windows: 0 workflows tras `ide/install.ps1` | Script escribe en ruta legacy `%LOCALAPPDATA%\Google\Gemini\antigravity\` que Antigravity 2.0 **no escanea** |
| `forge-discovery` no invocable tras `/flow-start` | `.agents/skills/` vacío en proyecto; `install.ps1` no instala skills; delegación requiere skill visible en workspace |
| Reinstalar destruye frontmatter del proyecto | Instalador copia desde `ide/antigravity/` (defectuoso) sobre `.agents/workflows/` (corregido manualmente) |

### Alcance (in-scope)

| ID | Ámbito |
|----|--------|
| **A** | Sincronizar/corregir pack fuente `ide/antigravity/workflows/` (frontmatter en los 7 `flow-*.md`) y `ide/antigravity/rules/workflow.md` (`alwaysApply: true`) |
| **B** | Instalación de skills Antigravity (global + proyecto), incluyendo `forge-discovery`; paridad `ide/install.ps1` con C# / `ide/install.sh` |
| **C** | Migrar `ide/install.ps1` de ruta legacy a `%USERPROFILE%\.gemini\config\` |
| **D** | Validación en instalador y/o `flowforge doctor` + checks CI para frontmatter de workflows y presencia de skills |
| **E** | Limpieza documental donde el pack aún referencia `~/.gemini/antigravity/` (`ide/antigravity/AGENTS.md`, referencias cruzadas ADR si aplica) |

### Fuera de alcance (v1)

- Compilar skills en agents Antigravity (patrón Cursor); Antigravity usa symlinks a `SKILL.md`.
- Cambios en pack OpenCode o Cursor (solo Antigravity).
- Automatizar reload de Antigravity post-install (sigue siendo paso manual del usuario).
- Resolver workspace mixto cuando el usuario abre `~/.gemini/config/` vs repo — se documenta comportamiento esperado, no se cambia Antigravity IDE.
- Implementar generador de frontmatter en build (v1 sincroniza archivos estáticos; generación es follow-up opcional).

### Referencias

- ADR-009: rutas canónicas `~/.gemini/config/`, formato workflows, rechazo de `skills.json`.
- Context map: `.ai-work/fix-antigravity-forge-discovery/context-map.md`
- Patrón canónico skills: `FlowForgeModule.InstallAntigravitySkills()` + `ide/install.sh` `install_antigravity_skills()`.

---

## 2. Functional requirements (FR)

### Pack fuente Antigravity (A)

- **FR-001: Frontmatter en workflows fuente** — Los siete archivos `ide/antigravity/workflows/flow-*.md` deben incluir bloque YAML de apertura con `description:` en **una sola línea**, alineado semánticamente con las descripciones ya presentes en `.agents/workflows/` equivalentes.
  * **Scenario A:** Given el repo FlowForge con el fix aplicado, When se inspecciona `ide/antigravity/workflows/flow-start.md`, Then el archivo comienza con `---`, contiene `description:` en una línea, y cierra con `---` antes del cuerpo markdown.
  * **Scenario B:** Given los siete `flow-*.md` en `ide/antigravity/workflows/`, When un script de validación (doctor o CI) escanea el directorio, Then los siete archivos pasan la regla de frontmatter obligatorio y ninguno empieza directamente con `# /flow-`.

- **FR-002: Paridad contenido workflows** — Tras añadir frontmatter, el cuerpo de cada workflow en `ide/antigravity/workflows/` debe ser funcionalmente equivalente al de `.agents/workflows/` (mismos pasos de orquestación, mismos nombres de agente delegado). La fuente canónica del instalador sigue siendo `ide/antigravity/`; `.agents/workflows/` del repo FlowForge no debe divergir en contenido operativo tras el fix.
  * **Scenario A:** Given `flow-dev.md` en ambos directorios tras el fix, When se comparan las instrucciones de delegación (sin contar frontmatter), Then ambos archivos delegan a `forge-dev` con los mismos checkpoints.
  * **Scenario B:** Given un desarrollador ejecuta `flowforge init` en un repo nuevo, When se listan `{repo}/.agents/workflows/`, Then los siete archivos tienen frontmatter válido idéntico en estructura al pack fuente corregido.

- **FR-003: Regla orquestador always-on** — `ide/antigravity/rules/workflow.md` debe incluir frontmatter con `alwaysApply: true` y `description:` (paridad con `.agents/rules/workflow.md`), de modo que la regla del orquestador se aplique sin depender solo de selección manual.
  * **Scenario A:** Given el pack fuente corregido, When Antigravity carga reglas desde `config/rules/` tras install global, Then `workflow.md` está marcada como always-applied según convención Antigravity.
  * **Scenario B:** Given `flowforge init` en un proyecto, When se inspecciona `{repo}/.agents/rules/workflow.md`, Then contiene `alwaysApply: true` en frontmatter.

### Instalación skills (B)

- **FR-004: Skills globales Linux** — `ide/install.sh` y `flowforge install` (C#) deben continuar instalando **todos** los directorios `skills/forge-*` como symlinks (o copia recursiva si symlink falla) en `~/.gemini/config/skills/` y espejo `~/.gemini/config/.agents/skills/`, incluyendo `forge-discovery`.
  * **Scenario A:** Given Linux con clone FlowForge, When se ejecuta `bash ide/install.sh` sin argumentos de proyecto, Then existe `~/.gemini/config/skills/forge-discovery/SKILL.md` apuntando al repo o cache.
  * **Scenario B:** Given install remoto (`curl | bash`) con repo en `/tmp`, When symlink falla, Then el instalador copia recursivamente el contenido de `skills/forge-discovery/` y el doctor/CI detecta `SKILL.md` presente.

- **FR-005: Skills globales Windows (install.ps1)** — `ide/install.ps1` debe instalar skills con la misma semántica que C#: iterar `skills/forge-*`, crear symlink en `%USERPROFILE%\.gemini\config\skills\` (y espejo `config\.agents\skills\`), con fallback a copia recursiva si `CreateSymbolicLink` falla.
  * **Scenario A:** Given Windows con Developer Mode o privilegio de symlink, When se ejecuta `ide/install.ps1`, Then `%USERPROFILE%\.gemini\config\skills\forge-discovery\SKILL.md` existe y referencia el clone FlowForge.
  * **Scenario B:** Given Windows sin privilegio de symlink, When se ejecuta `ide/install.ps1`, Then los skills se instalan por copia recursiva y `forge-discovery` sigue siendo invocable desde Antigravity.

- **FR-006: Skills por proyecto** — Tanto `flowforge init` (C#) como `ide/install.sh <ruta>` como `ide/install.ps1 -ProjectPath <ruta>` deben poblar `{repo}/.agents/skills/forge-*/` con symlinks (o copia) hacia `skills/forge-*` del repo FlowForge clonado o cache `~/.flowforge/cache/FlowForge`.
  * **Scenario A:** Given el repo FlowForge tras `flowforge init .` (o re-init), When se lista `.agents/skills/`, Then contiene al menos `forge-discovery/` con `SKILL.md` accesible.
  * **Scenario B:** Given un proyecto externo inicializado con FlowForge desde Windows vía `install.ps1 -ProjectPath`, When el usuario abre ese repo en Antigravity y ejecuta `/flow-start`, Then el orquestador puede delegar a forge-discovery porque el skill está en `.agents/skills/forge-discovery/`.

- **FR-007: Política skills.json** — Ningún canal de instalación Antigravity debe crear ni depender de `.agents/skills.json` o `config/skills.json`. Si existe por instalación manual previa, la documentación debe indicar que no es mecanismo soportado (ADR-009).
  * **Scenario A:** Given instalación limpia vía `flowforge install`, When se inspecciona `~/.gemini/config/`, Then no existe `skills.json` escrito por FlowForge.
  * **Scenario B:** Given `flowforge init` en proyecto, When se lista `.agents/`, Then FlowForge no añade `skills.json`; skills viven solo en `.agents/skills/forge-*/`.

### Migración install.ps1 (C)

- **FR-008: Destino config en Windows** — `ide/install.ps1` debe escribir AGENTS, rules, workflows, skills y mcp en `%USERPROFILE%\.gemini\config\` (y espejo `config\.agents\` donde aplique), alineado con `PathHelper.AntigravityConfigDir` del instalador C#.
  * **Scenario A:** Given Windows, When se ejecuta `ide/install.ps1`, Then `%USERPROFILE%\.gemini\config\workflows\flow-start.md` existe y **no** se escribe en `%LOCALAPPDATA%\Google\Gemini\antigravity\workflows\`.
  * **Scenario B:** Given Windows con restos de install.ps1 legacy, When se ejecuta el script corregido, Then elimina o deja de usar el pack FlowForge bajo `antigravity/` y el usuario ve workflows en la ruta escaneada por Antigravity 2.0.

- **FR-009: Paridad artefactos globales Windows** — Tras migración, `install.ps1` debe instalar el mismo conjunto que C#/bash: `AGENTS.md`, `rules/*`, `workflows/*`, `skills/forge-*`, `mcp_config.json` (Engram), y copiar `rules/workflow.md` → `%USERPROFILE%\.gemini\GEMINI.md` cuando corresponda.
  * **Scenario A:** Given Windows post-`install.ps1`, When se compara inventario con post-`flowforge install`, Then ambos tienen los mismos tipos de artefacto bajo `config/` (salvo diferencias de symlink vs copy).
  * **Scenario B:** Given usuario que solo usa PowerShell (sin binario C#), When ejecuta `install.ps1`, Then obtiene paridad funcional equivalente a `ide/install.sh` en Linux.

### Validación doctor / CI (D)

- **FR-010: Validación frontmatter en fuente** — Debe existir verificación automatizada (script en CI y/o método doctor) que falle si cualquier `ide/antigravity/workflows/flow-*.md` no tiene frontmatter con `description:` en línea única.
  * **Scenario A:** Given un PR que elimina frontmatter de `flow-plan.md`, When corre CI `test-installer`, Then el job falla con mensaje que identifica el archivo y la regla incumplida.
  * **Scenario B:** Given repo local correcto, When se ejecuta `flowforge doctor` (o script equivalente), Then reporta OK para workflows Antigravity fuente.

- **FR-011: Validación skills post-install** — CI Linux (existente) debe extenderse para assert `~/.gemini/config/skills/forge-discovery/SKILL.md` tras install. CI Windows debe añadir job o steps Antigravity equivalentes: destino `config\`, conteo workflows, frontmatter, y skill discovery.
  * **Scenario A:** Given CI Linux tras `flowforge install`, When corre el step Antigravity, Then verifica 7 workflows con frontmatter y skill discovery presente.
  * **Scenario B:** Given CI Windows tras `ide/install.ps1` o `flowforge install`, When corre el step Antigravity, Then verifica ruta `%USERPROFILE%\.gemini\config\workflows\` (no legacy) y `forge-discovery` skill.

- **FR-012: Doctor ampliado** — `flowforge doctor` debe advertir (mínimo) si: (a) global `config/workflows/` tiene archivos sin frontmatter, (b) falta `config/skills/forge-discovery`, (c) proyecto actual carece de `.agents/skills/forge-discovery` cuando `.flowforge.json` está presente.
  * **Scenario A:** Given máquina con install roto (6/7 sin FM), When se ejecuta `flowforge doctor`, Then lista incumplimientos Antigravity con remedio sugerido (`flowforge install` / actualizar FlowForge).
  * **Scenario B:** Given máquina correcta post-fix, When se ejecuta `flowforge doctor`, Then no reporta errores Antigravity en workflows ni skills.

### Documentación (E)

- **FR-013: AGENTS.md Antigravity actualizado** — `ide/antigravity/AGENTS.md` (y copia instalada) debe documentar rutas `~/.gemini/config/` y `%USERPROFILE%\.gemini\config\`, no `~/.gemini/antigravity/`. Debe mencionar requisito de frontmatter en workflows y ubicación de skills.
  * **Scenario A:** Given un desarrollador lee `ide/antigravity/AGENTS.md`, When busca rutas de instalación global, Then encuentra `config/workflows/` y `config/skills/` como destinos canónicos.
  * **Scenario B:** Given README o ADR referenciados desde AGENTS, When se valida coherencia, Then no hay contradicción con ADR-009 sobre rutas legacy.

- **FR-014: Nota post-install reload** — Documentación de instalación Antigravity debe recordar reiniciar Antigravity tras install/reinstall para que `/flow-*` aparezcan (comportamiento IDE, no automatizable en v1).
  * **Scenario A:** Given usuario sigue guía post-`flowforge install`, When lee sección Antigravity, Then encuentra paso explícito de reload/restart IDE.
  * **Scenario B:** Given usuario reporta workflows ausentes sin reload, When consulta troubleshooting en docs, Then encuentra reload como primera acción antes de reinstalar.

### Paridad multi-canal

- **FR-015: Coherencia C# / bash / PowerShell** — Los tres canales de instalación Antigravity deben producir resultado funcional equivalente en sus respectivas plataformas (mismos archivos lógicos, mismas reglas de frontmatter, mismos skills).
  * **Scenario A:** Given Linux, When se instala con C# y con `install.sh` en máquinas limpias, Then ambos dejan `config/workflows/` con 7 archivos frontmatter-validos y skills completos.
  * **Scenario B:** Given Windows, When se instala con C# y con `install.ps1`, Then ambos usan `config\` y exponen los siete `/flow-*` tras reload de Antigravity.

---

## 3. Non-functional requirements (NFR)

- **NFR-001: Paridad Win/Linux** — Ningún requisito funcional se marca cumplido si solo aplica a un SO; excepciones deben documentarse en plan con justificación técnica.
- **NFR-002: Compatibilidad ADR-009** — El fix refuerza ADR-009; no introduce rutas alternativas ni `skills.json` como registry.
- **NFR-003: Idempotencia** — Ejecutar install dos veces no debe degradar frontmatter ni duplicar skills de forma conflictiva; reinstalar sobrescribe con pack fuente corregido.
- **NFR-004: Install remoto** — Paridad con patrón existente: symlinks desde cache `~/.flowforge/cache/FlowForge` cuando el repo no es persistente (`/tmp`, CI).
- **NFR-005: No regresión OpenCode/Cursor** — Cambios limitados a paths Antigravity, pack `ide/antigravity/`, doctor/CI Antigravity y `install.ps1` sección Antigravity.
- **NFR-006: Mensajes de fallo accionables** — Validaciones CI/doctor deben citar archivo, regla (`description:` faltante, ruta legacy, skill ausente) y comando de remediación.

---

## 4. Developer manual tests (PM-*) — required for CKP-4

## Developer manual tests (required — mark [x] before /flow-close)

| ID | Case / flow | Steps (summary) | Expected result | [x] |
|----|-------------|-----------------|-----------------|-----|
| PM-1 | Linux — `/flow-*` tras install global | 1. En Linux limpio o VM: `flowforge install` (o `bash ide/install.sh`)<br>2. Reiniciar Antigravity<br>3. Abrir chat, escribir `/` y buscar `flow` | Aparecen los 7 comandos (`flow-start`, `flow-plan`, `flow-dev`, `flow-verify`, `flow-close`, `flow-status`, `flow-rework`); al elegir `/flow-start` el orquestador menciona delegar a forge-discovery | [ ] |
| PM-2 | Windows — install.ps1 paridad config | 1. En Windows: ejecutar `ide/install.ps1`<br>2. Verificar `%USERPROFILE%\.gemini\config\workflows\` (no `%LOCALAPPDATA%\...\antigravity\`)<br>3. Reiniciar Antigravity; probar `/flow-start` | Workflows en ruta `config\`; picker muestra `/flow-*`; skill discovery invocable | [ ] |
| PM-3 | Proyecto — skills en `.agents/skills/` | 1. En clone FlowForge: `flowforge init .` (o `install.sh .` / `install.ps1 -ProjectPath .`)<br>2. Listar `.agents/skills/`<br>3. Abrir repo en Antigravity; `/flow-start` con feature de prueba | `.agents/skills/forge-discovery/SKILL.md` presente; delegación a discovery no falla por skill ausente | [ ] |
| PM-4 | Doctor / diagnóstico | 1. Con install correcto: `flowforge doctor`<br>2. (Opcional) Simular un workflow sin FM en copia local y re-ejecutar doctor | Caso OK: sin alertas Antigravity workflows/skills; caso roto: doctor lista incumplimiento con remedio | [ ] |
| PM-5 | Reinstall no degrada frontmatter | 1. Tras PM-1, ejecutar install otra vez<br>2. `head -5 ~/.gemini/config/workflows/flow-start.md` | Sigue teniendo bloque `---` y `description:`; contenido no revertido a markdown sin YAML | [ ] |

---

## 5. Open questions for human (OQ-*)

| ID | Tag | Question | Default / assumption |
|----|-----|----------|---------------------|
| OQ-1 | [OPTIONAL] | ¿Fuente de verdad para sincronizar workflows: copiar frontmatter+contenido desde `.agents/workflows/` hacia `ide/antigravity/workflows/`, o mantener edición solo en `ide/antigravity/` y alinear `.agents/` en el repo meta? | **Assumed:** `ide/antigravity/` es pack instalador canónico; el fix copia frontmatter y alinea contenido **desde** `.agents/workflows/` (que ya está correcto) **hacia** `ide/antigravity/workflows/`, luego ambos se mantienen en sync en el mismo PR. |
| OQ-2 | [OPTIONAL] | ¿Qué hacer con `.agents/skills.json` existente en repos que lo tengan por experimentos previos? | **Assumed:** no tocar en install v1; documentar en AGENTS.md/ADR-009 que no es soportado; el usuario puede borrarlo manualmente. No escribir ni migrar. |
| OQ-3 | [OPTIONAL] | ¿Doctor debe **fallar** (exit code ≠ 0) o solo **advertir** cuando detecta workflows sin frontmatter en destino global? | **Assumed:** CI **falla**; `flowforge doctor` **advierte** con severidad error visible pero exit code distinto solo si se usa flag `--strict` (si no existe flag, warn en stderr y exit 0 — implementación en plan). |
| OQ-4 | [OPTIONAL] | ¿Instalar todos los `skills/forge-*` (8 incl. forge-teacher) o solo los 7 roles del flujo principal? | **Assumed:** mismo set que C# `InstallAntigravitySkills()` — todos los directorios `skills/forge-*` presentes en repo, incluido `forge-teacher` si existe en árbol fuente. |
| OQ-5 | [FOLLOW-UP] | ¿Generar frontmatter en build/CI desde plantilla para evitar divergencia futura entre `.agents/` e `ide/antigravity/`? | — (v2; v1 usa sync estático en PR) |

---

## Memory Signal

- type: decision
- significance: high
- summary: "Antigravity exige frontmatter description: en workflows y symlinks skills/forge-* en config/ y .agents/; ide/antigravity/ es pack canónico del instalador y debe alinearse con .agents/; install.ps1 migra a %USERPROFILE%\.gemini\config\ con paridad C#/bash; doctor+CI validan frontmatter y forge-discovery."
