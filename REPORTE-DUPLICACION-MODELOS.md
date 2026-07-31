# Reporte: Duplicación de Modelos en Agentes

**Fecha:** 2026-07-17
**Problema:** Los modelos de los agentes están definidos en múltiples lugares

---

## 1. El Problema

Actualmente cada agente tiene su modelo definido en **3 lugares**:

| Ubicación | Ejemplo | Prioridad |
|-----------|---------|-----------|
| `opencode.json` → `agent.flowforge.model` | `opencode-go/qwen3.7-plus` | 🔴 Principal |
| `agents/flowforge.md` → frontmatter YAML | `opencode-zen/big-pickle` | ⚠️ Conflicto |
| `.opencode/agents/flowforge.md` → frontmatter | `opencode-go/qwen3.7-plus` | ⚠️ Conflicto |

**Ejemplo real de inconsistencia encontrada:**

```
opencode.json:          forge-arch → opencode-go/mimo-v2.5-pro  ✅
agents/forge-arch.md:   forge-arch → opencode-zen/big-pickle   ❌
.opencode/agents/:      forge-dev   → opencode-go/qwen3.7-plus  ❌ (debería ser MiniMax-M3)
```

## 2. Causa Raíz

El instalador de FlowForge:
1. Define los agentes en `opencode.json` con modelos **FREE** (`opencode-zen`)
2. Copia archivos `.md` a `agents/` y `.opencode/agents/` con modelos FREE
3. El usuario corrige manualmente `opencode.json` con modelos PAID
4. Al reinstalar, `opencode.json` se resetea a FREE
5. Los archivos `.md` pueden o no actualizarse según el momento

**Resultado:** 3 fuentes de verdad que divergen.

## 3. Solución Propuesta

### 3.1 Single Source of Truth

Eliminar el campo `model:` del frontmatter YAML de los archivos `.md`. El modelo **solo** debe estar en `opencode.json`.

Antes:
```yaml
---
description: Phase 1 — Writes spec.md...
mode: subagent
hidden: true
model: opencode-zen/big-pickle    ← ELIMINAR
permission:
  edit: allow
---
```

Después:
```yaml
---
description: Phase 1 — Writes spec.md...
mode: subagent
hidden: true
permission:
  edit: allow
---
```

### 3.2 Backward Compatibility

Si OpenCode requiere el campo `model:` en el `.md`, se debe documentar claramente:
- Que el `.json` tiene prioridad sobre el `.md`
- O viceversa
- Y que NO se edite manualmente en ambos lugares

### 3.3 Instalador

El instalador debe:
1. **NO sobrescribir** `opencode.json` si ya existe una configuración personalizada
2. O preguntar antes de sobrescribir
3. O usar un archivo separado (`opencode.flowforge.json`)

---

## 4. Archivos y Rutas

| Archivo | Propósito | ¿Debe tener modelo? |
|---------|-----------|:---:|
| `~/.config/opencode/opencode.json` | Config principal | ✅ SÍ |
| `~/.config/opencode/agents/*.md` | Prompt del agente | ❌ NO |
| `~/.config/opencode/.opencode/agents/*.md` | Copia del instalador | ❌ NO |
| `~/.config/opencode/.flowforge-managed.json` | Tracking del instalador | ❌ NO |

---

## 5. Estado Actual (2026-07-17)

```
agents/
├── flowforge.md         ⚠️ model: opencode-zen/big-pickle
├── forge-arch.md        ✅ model: opencode-go/mimo-v2.5-pro
├── forge-dev.md         ✅ model: minimax-coding-plan/MiniMax-M3
├── forge-discovery.md   ✅ model: opencode-go/deepseek-v4-flash
├── forge-memory.md      ✅ model: opencode-go/qwen3.7-plus
├── forge-plan.md        ✅ model: opencode-go/qwen3.7-plus
├── forge-teacher.md     ✅ model: opencode-go/qwen3.7-plus
└── forge-verify.md      ✅ model: opencode-go/deepseek-v4-pro

.opencode/agents/
├── flowforge.md         ✅ model: opencode-go/qwen3.7-plus
├── forge-arch.md        ❌ model: opencode-go/deepseek-v4-pro
├── forge-dev.md         ❌ model: opencode-go/qwen3.7-plus
├── forge-discovery.md   ✅ model: opencode-go/deepseek-v4-flash
├── forge-memory.md      ❌ model: opencode-go/deepseek-v4-flash
├── forge-plan.md        ✅ model: opencode-go/qwen3.7-plus
├── forge-teacher.md     ❌ model: opencode-go/deepseek-v4-flash
└── forge-verify.md      ✅ model: opencode-go/deepseek-v4-pro
```
