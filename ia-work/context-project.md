# Project Context — FlowForge

## Business Goal

FlowForge es una metodología de **Agentic SDLC** (Software Development Lifecycle con Agentes AI) para equipos chicos y medianos (2-20 personas). Resuelve el problema de **integrar agentes AI en el ciclo de desarrollo sin burocracia enterprise** — con 5 checkpoints formales, 7 agentes, y artefactos versionados.

**Para quién**: Equipos que usan AI coding agents (Cursor, OpenCode, Gemini CLI, etc.) y quieren:
- 5 checkpoints humanos (CKP-0 → CKP-4) para controlar ambigüedad
- 7 agentes especializados (no 10+, menos overhead)
- Contexto on-demand (los agentes fetch vía MCP, no todo en el prompt)
- Model routing por tarea (Sonnet para razonar, Haiku para leer/escribir)

---

## Tech Stack

| Capa | Tecnología |
|------|------------|
| **Metodología** | Agentic SDLC con 5 fases + 5 checkpoints (CKP-0 → CKP-4) |
| **Agentes** | 7 roles: Discovery, Architect, Plan, Dev, Verify, Memory, Orchestrator |
| **Skills** | 31 specialized skills (`skills/forge-*/SKILL.md`) |
| **Memoria** | Engram (engram-dotnet v1.1.0+) con sync offline-first |
| **Servidor** | .NET 10 C#, PostgreSQL, Docker en TrueNAS SCALE |
| **IDEs** | OpenCode, Cursor, Antigravity (Gemini CLI), VS Code |
| **Artefactos** | `.ai-work/{feature-slug}/` con `spec.md`, `plan.md`, `verify-report.md`, `rework_ticket.md` |

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                         FLOWFORGE                                    │
│  5 fases | 5 checkpoints (CKP-0→4) | 7 agentes | Model routing      │
└─────────────────────────────────────────────────────────────────────┘

PHASE 0: DISCOVERY ─────────── CKP-0 🔴 HARD STOP ───────────────────
│  Cross-memory association, epic mapping, user-story validation
│  Agente: Discovery Agent
│  Deliverable: Memory association map
│  ⚠️ Binary, no appeal: vague requirements → STOP EVERYTHING

PHASE 1: INTENT ────────────── CKP-1 🟡 YELLOW LIGHT ────────────────
│  Priority review and business-intent validation
│  Agente: Architect Agent → spec.md + Capability Matrix
│  🟡 Human decides: "Approve or adjust?"

PHASE 2: ARCHITECTURE ──────── CKP-2 🟡 YELLOW LIGHT ────────────────
│  Validate and approve the implementation plan
│  Agente: Plan Agent → plan.md + task breakdown
│  🟡 Human decides: "Green light to code?"

PHASE 3: EXECUTION ─────────── Inner loop (autonomous) ──────────────
│  Dev Agent → code + unit tests + Ralph Wiggum loop
│  Verify Agent → traceability vs spec.md + LLM-as-Judge
│  CKP-3 🔴: max 3 rework cycles → escalate to human

PHASE 4: CLOSE ─────────────── CKP-4 🟢 DEPLOY GATE ─────────────────
│  Memory Agent → session summary, ADR promotion
│  🟢 Human decides: "Deploy / merge?"
```

---

## Key Decisions

| Decisión | Fecha | Por qué |
|----------|-------|---------|
| **7 agentes (no 10+)** | 2026-05 | Cada handoff pierde contexto. 10 agentes = 10 prompts, 10 configs, overhead insoportable para SMBs. |
| **5 checkpoints (CKP-0→4)** | 2026-05 | De ~8 interrupciones humanas en SDLC tradicional a 5: CKP-0 (binary), CKP-1 (spec), CKP-2 (plan), CKP-3 (mecánico, 3 ciclos), CKP-4 (deploy). |
| **Contexto on-demand** | 2026-05 | Agentes no reciben todo el contexto en el prompt — fetch vía MCP cuando necesitan. Menos tokens, menos ruido. |
| **Model routing por tarea** | 2026-05 | No usar un modelo para todo. Razonamiento complejo → Sonnet/Opus. Lectura/escritura → Haiku. Persistencia → $0 (SQL directo). |
| **Artefactos versionados como protocolo** | 2026-05 | `spec.md`, `plan.md`, `verify-report.md`, `rework_ticket.md` son el contrato entre agentes. El orchestrator es opcional — los artefactos manejan el flow. |
| **Sync offline-first** | 2026-05 | Equipos necesitan compartir memorias pero sin depender de red constante. SQLite local + PostgreSQL server. |
| **`ia-work/context-project.md`** | 2026-05-29 | Nuevos team members pierden 2-3 días en onboarding sin contexto centralizado. |

---

## Team & Roles

| Rol | Tipo | Responsabilidad |
|-----|------|-----------------|
| **Orchestrator** | AI (`gentle-orchestrator`) | Coordina fases, decide cuándo parar y preguntar al humano. **No implementa código producto**. |
| **Discovery** | AI (`forge-discovery`) | Busca contexto, mapea requisitos, asocia memorias cruzadas. CKP-0. |
| **Architect** | AI (`forge-arch`) | Escribe `spec.md` con Capability Matrix (FR/NFR). CKP-1. |
| **Planner** | AI (`forge-plan`) | Breakdown en tasks, estima esfuerzo, MCP contracts. CKP-2. |
| **Developer** | AI (`forge-dev`) | Implementa código, sigue `plan.md`, unit tests, Ralph Wiggum loop. |
| **Verifier** | AI (`forge-verify`) | LLM-as-Judge, verifica contra `spec.md`, traza FR/NFR, genera `verify-report.md` o `rework_ticket.md`. |
| **Memory Agent** | AI (`forge-memory`) | Sintetiza sesión, guarda en Engram, promueve ADRs, close CKP-4. |
| **Human** | Humano | Aprueba CKP-1 (spec), CKP-2 (plan), CKP-4 (deploy). **No aprueba código línea por línea**. |

---

## Related Projects

| Proyecto | Relación |
|----------|----------|
| **[engram-dotnet](https://github.com/efreet111/engram-dotnet)** | Backend de memoria persistente (.NET 10). FlowForge usa engram para memoria cross-session y sync multi-usuario. |
| **FlowForge** (este repo) | **Proyecto principal** — metodología Agentic SDLC + IDE packs + 31 skills. Lo que estamos construyendo. |
| **Cursor / OpenCode / Antigravity** | IDEs donde se instalan los agents de FlowForge vía `install.sh` / `install.ps1`. |

---

## Getting Started

### 1. Clonar FlowForge
```bash
git clone https://github.com/efreet111/FlowForge.git
cd FlowForge
```

### 2. Instalar engram-dotnet (servidor de memoria)
```bash
git clone https://github.com/efreet111/engram-dotnet.git
cd engram-dotnet
./scripts/setup.sh  # Linux/macOS
# o: .\scripts\setup.ps1  # Windows
```

### 3. Configurar MCP en tu IDE
**OpenCode**: `~/.config/opencode/opencode.json`
**Cursor**: `~/.cursor/mcp.json`

```json
{
  "mcpServers": {
    "engram": {
      "command": "engram",
      "args": ["mcp", "--tools=agent"],
      "env": {
        "ENGRAM_DATA_DIR": "~/.engram",
        "ENGRAM_USER": "tu@email.com",
        "ENGRAM_SYNC_ENABLED": "true",
        "ENGRAM_SERVER_URL": "http://192.168.0.178:7437"
      }
    }
  }
}
```

### 4. Recargar IDE
- OpenCode: cerrar y abrir
- Cursor: `Developer: Reload Window`

### 5. Primer flow
```
/flow-start Task CRUD — create, list, update, delete tasks with title, description, status, timestamps
```

El orchestrator va a:
1. **CKP-0**: Discovery (buscar contexto, validar requisitos) → **HARD STOP si es vago**
2. **CKP-1**: Architect escribe `spec.md` → **Humano aprueba o ajusta**
3. **CKP-2**: Plan escribe `plan.md` → **Humano da green light**
4. **CKP-3**: Dev ↔ Verify inner loop → **Máx 3 rework cycles, luego escala**
5. **CKP-4**: Memory cierra sesión → **Humano decide deploy**
