# FlowForge — Quickstart

> **Empezá a usar FlowForge en 5 minutos.**

---

## 1. Instalación

**Linux / macOS:**
```bash
curl -sSL https://raw.githubusercontent.com/efreet111/FlowForge/main/ide/install.sh | bash
```

**Windows (PowerShell):**
```powershell
iex ((New-Object System.Net.WebClient).DownloadString('https://raw.githubusercontent.com/efreet111/FlowForge/main/ide/install.ps1'))
```

## 2. Primer comando

Reiniciá tu IDE, seleccioná el agente **`flowforge`** y escribí:

```
/flow-start CRUD de tareas — endpoints para crear, listar, actualizar y eliminar tareas. Cada tarea tiene título, descripción, estado (pendiente/en-progreso/completada) y fecha de creación.
```

## 3. ¿Qué va a pasar?

El agente va a seguir este flujo automático:

```
/flow-start
  ├── forge-discovery  → investiga contexto, CVEs, compliance
  ├── CKP-0 🟢         → requerimiento claro, continuá
  ├── forge-arch       → escribe spec.md con RF/RNF + escenarios
  ├── CKP-1 🟡         → humano aprueba spec
  ├── forge-plan       → descompone en tareas con contratos
  ├── CKP-2 🟡         → humano da luz verde
  ├── forge-dev        → implementa código + tests
  ├── CKP-3 🔴         → 3 reworks máx, luego escala a humano
  └── humano decide ✅ → forge-memory cierra y persiste
```

## 4. Comandos disponibles

| Comando | Qué hace |
|---------|----------|
| `/flow-start <feature>` | Discovery + Spec (CKP-0, CKP-1) |
| `/flow-dev` | Plan + Dev + Verify (CKP-2, CKP-3) |
| `/flow-verify` | Solo auditoría (CKP-3) |
| `/flow-close` | Memoria + deploy (CKP-4) |

## 5. Siguientes pasos

- 📖 [`docs/14-flowforge-complete-reference.md`](docs/14-flowforge-complete-reference.md) — 7 casos de prueba prácticos
- 🔧 [`docs/17-improvement-plan-specs.md`](docs/17-improvement-plan-specs.md) — backlog de mejora
- 🧠 [`docs/15-agent-skills-technical-spec.md`](docs/15-agent-skills-technical-spec.md) — especificación técnica de los 7 agentes

---

> **¿Problemas?** Abrí un issue en https://github.com/efreet111/FlowForge
