# Context Map — open-source-contributor-experience

> **Feature**: Mejorar la experiencia de contribuidores open source en FlowForge  
> **CKP-0**: ✅ Clear — 3 deliverables concretos (issue templates, PR template, CI smoke)  
> **Generated**: 2026-06-04

---

## Goal

Que cualquier persona que llegue al repo de FlowForge en GitHub pueda:
1. Reportar un bug, pedir una feature o hacer una pregunta usando **issue templates** estandarizados
2. Abrir un PR con una **guía y template** que asegure calidad mínima (checklist, contexto, tests)
3. Validar que el smoke de OpenCode funciona automáticamente mediante un **CI workflow** que ejecute el Item 1 del roadmap

---

## In Scope

| # | Deliverable | Description |
|---|-------------|-------------|
| 1 | `.github/ISSUE_TEMPLATE/bug_report.md` | Bug report template con OS, IDE, install mode, logs |
| 2 | `.github/ISSUE_TEMPLATE/feature_request.md` | Feature request template con problema, solución propuesta, alternativas |
| 3 | `.github/ISSUE_TEMPLATE/config.yml` | Config para issues (blank issues enabled/disabled, contact links) |
| 4 | `.github/PULL_REQUEST_TEMPLATE.md` | PR template con checklist (principios, convenciones, tests) |
| 5 | `.github/workflows/opencode-smoke.yml` | CI workflow que ejecuta el smoke test del Item 1 del roadmap |
| 6 | Update `CONTRIBUTING.md` | Referenciar los templates y el workflow CI |
| 7 | Update `docs/17-improvement-plan-specs.md` | Marcar Item 1 como completado (o parcial) |

## Out of Scope

- Publicar el repo (sigue privado hasta release gate)
- Crear el demo repo público `flowforge-demo-task-manager` (opcional del roadmap)
- Item 2 del roadmap (CRUD case) — ya validado en examples/
- Items 5-6 (project template, `.flowforge.json` schema)
- Items 8-14 (resto del improvement plan)
- Traducir CODE_OF_CONDUCT.md a inglés (bug existente, no parte de esta feature)

---

## Dependencies

| Dep | Status | Notes |
|-----|--------|-------|
| OpenCode funcionando en Linux | ✅ Confirmado (sesión activa) | Se necesita para el smoke CI |
| `ide/opencode/opencode.flowforge.json` con paths válidos | 🟡 Usa `__FLOWFORGE_REPO__` placeholder | CI debe resolver el path absoluto |
| `__FLOWFORGE_REPO__` placeholder mechanism | ❓ Desconocido | CI need to resolve or substitute |
| `bash ide/install.sh` funcional | ✅ Confirmado | `install.sh` + `install.ps1` existen |
| GitHub repo público o CI configurado para private | 🟡 Private hoy | GitHub Actions corre en private repos sin issue |

---

## Existing References in Repo

### Files that exist

| File | Relevance |
|------|-----------|
| `CONTRIBUTING.md` (44 lines) | Brief. No templates referenced. Lists OS/IDE/install mode/logs as issue requirements (inline, no template). |
| `SECURITY.md` (27 lines) | Basic. Adequate for now. |
| `CODE_OF_CONDUCT.md` (24 lines) | **In Spanish** — inconsistency with English docs policy (per I18N.md). |
| `LICENSE` | Present (not reviewed — standard). |
| `docs/04-roadmap.md` | Item 1: 📋 OpenCode smoke — "Linux + bundle". Item 8: 📋 All IDE smoke. |
| `docs/17-improvement-plan-specs.md` | Item 1 criteria: 7 subagentes cargan, cada agente carga SKILL.md correcto, orchestrator delega, forge-verify ejecuta tests. |
| `docs/09-open-source-integration.md` | Covers IDE integration model + launch strategy. **No mention of GitHub community templates or CI.** |
| `docs/I18N.md` | Policy: public docs in English; Spanish entry via README.es.md. CODE_OF_CONDUCT.md is an exception. |
| `examples/crud-tareas/CASE-1-VALIDATION.md` | Gap: Item 1 (OpenCode bundle smoke on Linux) marked as P0 pending. |
| `ide/opencode/opencode.flowforge.json` | 7 subagentes con `__FLOWFORGE_REPO__` placeholder. Sin mecanismo de resolución documentado. |
| `ide/opencode/AGENTS.md` | Documenta subagentes, reglas de orquestación. No cambios necesarios. |
| `ide/shared/workflow-orchestrator-parity.md` | Contrato de orquestación compartido. Referencia estable. |

### Files that do NOT exist (the gap)

| Missing | Impact |
|---------|--------|
| `.github/` directory | No GitHub community health files at all |
| `.github/ISSUE_TEMPLATE/*.md` | Contributors must write free-form issues |
| `.github/PULL_REQUEST_TEMPLATE.md` | No PR quality gates |
| `.github/workflows/*.yml` | No CI/CD at all |
| `.github/CODEOWNERS` | Not needed yet (single maintainer) |
| `.github/FUNDING.yml` | Premature |

---

## Risks & Unknowns

| Risk | Severity | Mitigation |
|------|----------|------------|
| **Repo is private**: GitHub Actions corre pero issue templates no son visibles hasta publish | 🟡 Low | CI es funcional hoy; templates se ven al hacer publico. Documentar que es pre-public. |
| **`__FLOWFORGE_REPO__` placeholder no tiene mecanismo de resolución** | 🟡 Medium | CI workflow debe resolver el path absoluto del clone. Alternativa: usar variable de entorno o `GITHUB_WORKSPACE`. |
| **CI smoke test depende de OpenCode instalado en el runner** | 🔴 High | GitHub Actions runners no tienen OpenCode. El smoke debe probar el JSON/config, no ejecutar el IDE. |
| **CODE_OF_CONDUCT.md en español — inconsistencia con política I18N** | 🟡 Low | No es parte de esta feature. Debe corregirse separadamente o incluirse como low-priority. |
| **Item 1 smoke criteria en docs/17 son manuales** (invocar subagentes) | 🔴 High | CI no puede verificar carga de subagentes. El workflow CI debe validar lo automatizable: sintaxis JSON, paths existentes, estructura. |
| **CONTRIBUTING.md no referencia templates** | 🟢 Low | Se actualiza como parte de esta feature. |

---

## Open Questions

1. **CI smoke: ¿qué validamos exactamente?**  
   Los criterios de Item 1 (7 subagentes cargan, SKILL.md existe, orchestrator delega) son **manuales**. El CI puede validar:
   - ✔️ Sintaxis JSON de `opencode.flowforge.json`
   - ✔️ Cada path `__FLOWFORGE_REPO__/skills/forge-*/SKILL.md` existe
   - ✔️ Los archivos referenciados existen en el repo
   - ❌ No puede probar que OpenCode cargue los subagentes
   
   → ¿Documentamos el smoke manual + CI automatizable, o solo CI?

2. **¿Issue templates en inglés o bilingües?**  
   La política I18N dice docs públicos en inglés. CODE_OF_CONDUCT.md está en español (bug). ¿Issue templates siguen la política (inglés) o damos opción español?

3. **¿Blank issues permitidos o solo con template?**  
   `blank_issues_enabled: false` fuerza templates pero puede disuadir contributors con casos complejos. ¿Preferencia?

4. **CODE_OF_CONDUCT.md en español: ¿lo corregimos ahora o lo dejamos para otra issue?**  
   Es una inconsistencia con la política I18N. No es estrictamente parte de esta feature pero sería raro tener templates en inglés y un CoC en español.

5. **¿CI debe correr en cada PR o solo on push a main?**  
   Para un smoke test de configuración, on push a `main` + PRs puede ser suficiente. ¿Trigger preferido?

6. **¿Mantenemos el placeholder `__FLOWFORGE_REPO__` o lo reemplazamos con un install script que parchee?**  
   El roadmap (docs/04) dice "installer should patch a repo-path placeholder". Para CI, necesitamos resolverlo. ¿Variable de entorno? ¿GITHUB_WORKSPACE?

---

## Prior Observations Consulted

- Roadmap Item 1: `docs/04-roadmap.md` § "Item 1 — OpenCode smoke"
- Spec Item 1: `docs/17-improvement-plan-specs.md` § "Item 1: Probar opencode.flowforge.json en OpenCode"
- CRUD validation gap: `examples/crud-tareas/CASE-1-VALIDATION.md` § "Gaps — Item 1 OpenCode bundle smoke on Linux"
- OpenCode config: `ide/opencode/opencode.flowforge.json` — 7 agent definitions
- I18N policy: `docs/I18N.md` — public docs in English
- OSS guide: `docs/09-open-source-integration.md` — no mention of GitHub templates/CI
- CONTRIBUTING.md — current contribution flow
- SECURITY.md, CODE_OF_CONDUCT.md — existing community files
