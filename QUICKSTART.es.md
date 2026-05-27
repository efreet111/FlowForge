# FlowForge — Inicio rápido

> **Empezá con FlowForge en unos 5 minutos.**

🇬🇧 Guía en inglés: [`QUICKSTART.md`](QUICKSTART.md)

---

## 1. Instalación

**Linux / macOS** (cuando el repo sea **público**):

```bash
curl -sSL https://raw.githubusercontent.com/efreet111/FlowForge/main/ide/install.sh | bash
```

**Windows (PowerShell)** (repo público):

```powershell
iex ((New-Object System.Net.WebClient).DownloadString('https://raw.githubusercontent.com/efreet111/FlowForge/main/ide/install.ps1'))
```

### Repo privado (recomendado mientras preparás el release)

La instalación remota vía `raw.githubusercontent.com` devuelve **404** si el repo es privado. Usá un clon local:

```bash
git clone https://github.com/efreet111/FlowForge.git
cd FlowForge
```

```bash
# Linux/macOS
bash ide/install.sh

# Opcional: instalar FlowForge en tu repo de aplicación
bash ide/install.sh /ruta/a/tu-app
```

```powershell
# Windows
.\ide\install.ps1
.\ide\install.ps1 -ProjectPath "C:\ruta\a\tu-app"
```

---

## 2. Primer comando

Recargá el IDE, abrí el **modo Agente**, elegí el orquestador **flowforge** (o asegurate de que las reglas FlowForge estén activas) y enviá:

```
/flow-start CRUD de tareas — endpoints REST para crear, listar, actualizar y borrar tareas. Cada tarea tiene título, descripción, estado (pendiente/en-progreso/completada) y fecha de creación.
```

Los artefactos quedan en `.ai-work/{slug-de-la-feature}/` (por ejemplo `crud-de-tareas/`).

---

## 3. Qué pasa después

```
/flow-start
  ├── forge-discovery  → mapa de contexto, riesgos, trabajo previo
  ├── CKP-0 🔴         → requerimiento vago? PARAR y preguntar al humano
  ├── forge-arch       → escribe spec.md (RF/RNF, GWT, pruebas manuales PM-*)
  ├── CKP-1 🟡         → vos aprobás spec.md
/flow-plan
  ├── forge-plan       → escribe plan.md (checklist ordenado)
  ├── CKP-2 🟡         → vos das luz verde a implementar
/flow-dev
  ├── forge-dev        → código + tests (loop Ralph Wiggum hasta verde)
/flow-verify
  ├── forge-verify     → verify-report.md (PASS o rework_ticket.md)
  ├── CKP-3 🔴         → máximo 3 ciclos de rework, luego escalar
/flow-close
  └── forge-memory     → summary.md si los PM-* están hechos (CKP-4 deploy gate)
```

**Reglas que no cambian en ningún IDE:**

- El **orquestador no implementa código de producto** — delega.
- **¿Reporte de bug?** El orquestador crea `rework_ticket.md` → **forge-dev** lo corrige.
- **Dev terminó** ≠ solo tests en verde: checklist del plan con `[x]` + PM-* manuales del spec + verify PASS antes del cierre.

---

## 4. Comandos

| Comando | Fase |
|---------|------|
| `/flow-start <feature>` | Discovery → Spec (CKP-0, CKP-1) |
| `/flow-plan` | Plan (CKP-2) |
| `/flow-dev` | Implementación |
| `/flow-verify` | Auditoría (CKP-3) |
| `/flow-rework` | Bug → ticket → dev (sin parche inline del orquestador) |
| `/flow-close` | Memoria + deploy gate (CKP-4) |
| `/flow-status` | Solo lectura de `.ai-work/` |

También funciona lenguaje natural (por ejemplo: “reportá un bug”, “seguí codificando”, “cerrá la feature”).

---

## 5. Próximos pasos

- [`docs/14-flowforge-complete-reference.md`](docs/14-flowforge-complete-reference.md) — 7 casos de prueba hands-on (inglés)
- [`ide/README.md`](ide/README.md) — instalación por IDE y paridad v0.4
- [`docs/18-replicable-demo-definition.md`](docs/18-replicable-demo-definition.md) — runbook replicable
- [`docs/04-roadmap.md`](docs/04-roadmap.md) — roadmap y checklist de release
- [`README.es.md`](README.es.md) — visión general del proyecto en español

---

## Solución de problemas

| Problema | Solución |
|----------|----------|
| **404 al instalar desde `raw.githubusercontent.com`** | Repo privado o rama incorrecta — usá instalación local arriba |
| **No encuentra `git`** | Instalá Git for Windows o el gestor de paquetes de tu SO |
| **`dubious ownership` en Windows** | `git config --global --add safe.directory E:/Proyectos/FlowForge` |
| **El orquestador codea en lugar de delegar** | Recargá el IDE; decí: “Delegá a forge-dev según workflow — no parchees inline” |
| **Hay que cargar `@skills` a mano** | Usá agentes compilados (Cursor) o packs desde `ide/install` |

> **¿Problemas?** Abrí un issue: https://github.com/efreet111/FlowForge
