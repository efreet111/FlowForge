# FlowForge para IDEs: Integración Cursor, Antigravity y VS Code

> **Versión**: 1.0 — Análisis de Cursor Enterprise Agents + plan de integración
> **Fecha**: 2026-05-25
> **Estado**: IDE files creados en `ide/`

---

## PARTE 1: ¿Qué aprendimos de Cursor Enterprise Agents?

Revisamos el proyecto `cursor-enterprise-agents_ver 0.0.5` y encontramos patrones directamente aplicables a FlowForge:

### Lo que ya tenemos (coincidencias)

| FlowForge | Cursor EA | Estado |
|-----------|-----------|--------|
| `forge-orchestrator` (tabla CKP, delegación) | `workflow.mdc` (orquestador + comandos) | ✅ Paralelo directo |
| `forge-discovery` + `forge-arch` | `spec-writer` + `project-explorer` | ✅ Conceptualmente igual |
| `forge-plan` (patterns, security, rollback) | `solution-architect` (propuestas) | ✅ FlowForge es más completo |
| `forge-dev` (6 skills) | `dev-agent` (1 skill monolítica) | ✅ FlowForge es más granular |
| `forge-verify` (5 skills) | `verifier-agent` (1 skill) | ✅ FlowForge audita más dimensiones |
| `forge-memory` (4 skills) | `hu-closer` (documental) | ✅ FlowForge persiste + métricas |
| `forge-teacher` | `user-liaison` (`/explain-more`) | ✅ Ambos son toggleable |
| `.flowforge.json` (planeado) | `model-assignments.mdc` | 📝 Por implementar |
| `docs/14` (casos de prueba) | `GUIA-USO.md` + `BEST-PRACTICES.md` | ✅ Ambos documentados |

### Lo que NO tenemos (gaps detectados en Cursor EA)

| Gap | Cursor EA lo tiene como | Prioridad |
|-----|------------------------|-----------|
| Git safety rule (no push sin pedir) | `git-sin-push.mdc` | 🔴 Crítico |
| Skills con referencias por stack | `verifier-security-gaps/references/stack-*.md` | 🟡 Importante |
| Auto-bootstrap de reglas | Sección en `workflow.mdc` | 🟡 Importante |
| Instalación IDE (copiar archivos) | `INSTALACION.md` + `install_workspace.ps1` | 🟡 Importante |
| Checkpoint de "danger zones" | `human-in-the-loop-checkpoints.md` §2 | 🟢 Mejora |
| Intenciones en lenguaje natural | Tabla de "señales" en workflow | 🟢 Mejora |

---

## PARTE 2: Modelo de Integración con IDEs

### Principio: FlowForge es IDE-agnóstico

Las skills son archivos Markdown. Cada IDE tiene su propio formato de reglas y agentes. Nuestra tarea es **traducir** las skills al formato de cada IDE, no reescribirlas.

```
skills/ (fuente canónica — 31 archivos SKILL.md)
  │
  ├──→ ide/cursor/     ← Traducción a formato Cursor (.mdc + agents/*.md)
  ├──→ ide/antigravity/← Traducción a formato Antigravity (.agents/rules/ + workflows/)
  └──→ ide/vscode/     ← Traducción a formato VS Code/Copilot (.vscode/)
```

### Formato por IDE

| IDE | Rules | Agents | Workflows | Instalación |
|-----|-------|--------|-----------|-------------|
| **Cursor** | `.cursor/rules/*.mdc` (frontmatter `alwaysApply: true`) | `.cursor/agents/*.md` | Slash commands via rules | Copiar a `%USERPROFILE%\.cursor\` |
| **Antigravity** | `.agents/rules/*.md` | `.agents/skills/*/SKILL.md` | `.agents/workflows/*.md` | Copiar al workspace del repo |
| **VS Code / Copilot** | `.vscode/copilot-instructions.md` | N/A (instrucciones inline) | N/A (prompts en chat) | Copiar al repo o config |

### Qué archivos creamos para cada IDE

**Cursor** (`ide/cursor/`):
```
ide/cursor/
├── rules/
│   ├── workflow.mdc           ← forge-orchestrator compilado (checkpoints + delegación)
│   ├── model-assignments.mdc  ← Tabla agente→modelo (adaptable por proyecto)
│   └── git-sin-push.mdc       ← "Never push without explicit request"
└── agents/
    ├── forge-discovery.md     ← Instrucciones del discovery agent
    ├── forge-arch.md          ← Instrucciones del arch agent
    ├── forge-plan.md          ← Instrucciones del plan agent
    ├── forge-dev.md           ← Instrucciones del dev agent
    ├── forge-verify.md        ← Instrucciones del verify agent
    └── forge-memory.md        ← Instrucciones del memory agent
```

**Antigravity** (`ide/antigravity/`):
```
ide/antigravity/
├── rules/
│   ├── workflow.md            ← forge-orchestrator para Antigravity
│   ├── model-assignments.md   ← Tabla de modelos
│   ├── project-context.md     ← Plantilla de contexto del proyecto
│   └── git-sin-push.md        ← No push sin pedir
├── workflows/
│   ├── flow-start.md          ← Gatillo /flow-start (≡ /hu-start)
│   ├── flow-dev.md            ← Gatillo /flow-dev
│   ├── flow-verify.md         ← Gatillo /flow-verify
│   └── flow-close.md          ← Gatillo /flow-close
└── AGENTS.md                  ← Índice de agentes (raíz del repo)
```

**VS Code** (`ide/vscode/`):
```
ide/vscode/
└── copilot-instructions.md    ← Instrucciones compiladas para GitHub Copilot
```

---

## PARTE 3: Arquitectura de los Archivos IDE

### 3.1 Cursor: `workflow.mdc`

El archivo `workflow.mdc` es el más importante. Compila la esencia de `forge-orchestrator` en formato Cursor:

```markdown
---
alwaysApply: true
description: FlowForge Workflow Orchestrator (CKP-0 → CKP-4)
---

# FlowForge — Workflow Orchestrator

You are the FlowForge orchestrator. Your job is to COORDINATE, not to implement.

## Checkpoints (the traffic light)

| ID | Color | Meaning |
|----|-------|---------|
| CKP-0 | 🔴 HARD STOP | Vague requirement → STOP. Ask what they really need. |
| CKP-1 | 🟡 YELLOW | spec.md ready → "Approve or adjust?" |
| CKP-2 | 🟡 YELLOW | plan.md ready → "Green light to code?" |
| CKP-3 | 🔴 EMERGENCY | 3 rework cycles → ESCALATE to human |
| CKP-4 | 🟢 DEPLOY GATE | Feature done → "Deploy?" |

## Delegation Rules

Always delegate complex work to sub-agents:

| Phase | Sub-agent | When |
|-------|-----------|------|
| Discovery | `@forge-discovery` | New feature request |
| Spec | `@forge-arch` | Write spec.md |
| Plan | `@forge-plan` | Write plan.md |
| Code | `@forge-dev` | Implement plan |
| Verify | `@forge-verify` | Audit code |
| Memory | `@forge-memory` | Close session |

## Commands

| Command | What triggers it |
|---------|-----------------|
| `/flow-start <name>` | Start new feature → discovery + spec |
| `/flow-dev` | Implement active feature |
| `/flow-verify` | Verify implementation |
| `/flow-close` | Close feature + persist memory |
```

### 3.2 Mapeo de Skills a Archivos IDE

No copiamos las 31 skills completas a cada IDE — sería demasiado contexto. En su lugar:

1. **workflow.mdc** tiene las reglas de orquestación + delegación
2. **agents/*.md** tienen las instrucciones esenciales de cada rol
3. **skills/*.md** se cargan on-demand cuando el contexto lo requiere

### 3.3 Skill de Referencias por Stack (NUEVO)

El Cursor EA tiene `skills/verifier-security-gaps/references/stack-*.md` — referencias específicas por lenguaje. FlowForge debería adoptar este patrón. En lugar de tener una skill monolítica que intenta cubrir todos los lenguajes, cada skill debería tener:

```
skills/forge-dev/security/
├── SKILL.md                  ← Instrucciones generales (lenguaje-agnóstico)
└── references/
    ├── stack-dotnet.md       ← Patrones OWASP específicos para .NET
    ├── stack-python.md       ← Patrones OWASP específicos para Python
    ├── stack-javascript.md   ← Patrones OWASP específicos para JS/TS
    └── stack-sql.md          ← Patrones de sanitización SQL
```

Esto se documenta en [referencias-stack.md](referencias-stack.md) pero la implementación queda para una próxima iteración.

---

## PARTE 4: Instalación y Uso

### Cursor

```bash
# Desde el repo FlowForge, copiar a perfil Cursor:
cp ide/cursor/rules/*.mdc ~/.cursor/rules/
cp ide/cursor/agents/*.md  ~/.cursor/agents/
```

### Antigravity

```bash
# En el repo del proyecto:
mkdir -p .agents/rules .agents/workflows .agents/skills
cp ide/antigravity/rules/*.md .agents/rules/
cp ide/antigravity/workflows/*.md .agents/workflows/
cp ide/antigravity/AGENTS.md .
```

### VS Code

```bash
mkdir -p .vscode
cp ide/vscode/copilot-instructions.md .vscode/
```

---

## PARTE 5: Próximos Pasos

### Inmediato (esta sesión)
- [ ] Crear archivos IDE en `ide/cursor/`, `ide/antigravity/`, `ide/vscode/`
- [ ] Agregar `git-sin-push` rule a skills/forge-orchestrator

### Corto plazo (próxima sesión)
- [ ] Probar flujo con proyecto real (Task Manager API)
- [ ] Implementar Generador de Reglas para compilar skills automáticamente

### Mediano plazo
- [ ] Agregar `references/` stack-specific a skills de seguridad
- [ ] Crear script `install.sh` / `install.ps1` para automatizar instalación IDE

---

## Referencias

- [Cursor Enterprise Agents v0.0.5](file:///home/gantz/Documentos/proyectos/cursor-enterprise-agents_ver%200.0.5/)
- [FlowForge Complete Reference](14-flowforge-complete-reference.md)
- [FlowForge Technical Spec](15-agent-skills-technical-spec.md)
