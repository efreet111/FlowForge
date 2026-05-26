# AGENTS — FlowForge (Antigravity)

Orquestación del flujo FlowForge de 5 fases con checkpoints CKP-0→CKP-4.

## Reglas activas

- `.agents/rules/workflow.md` — Orquestador + checkpoints
- `.agents/rules/model-assignments.md` — Modelos por agente
- `.agents/rules/git-sin-push.md` — No push sin pedir

## Agentes (7 roles)

| Agente | Fase | Cuándo |
|--------|------|--------|
| forge-discovery | 0 | Nueva feature: buscar memorias, CVEs, compliance, costos |
| forge-arch | 1 | Escribir spec.md con RF/RNF + Capability Matrix |
| forge-plan | 2 | Descomponer spec en tareas atómicas + contratos |
| forge-dev | 3 | Implementar código + tests (Ralph Wiggum Loop) |
| forge-verify | 3 | Auditar código contra spec → PASS o rework |
| forge-memory | 4 | Cerrar: sintetizar, persistir, promover ADRs |

Skills especializadas disponibles en `skills/`. Cada agente carga skills on-demand según contexto (seguridad, performance, a11y, SOLID, patrones, migraciones, etc.).

## Artefactos

```
.ai-work/FLOW-{slug}/
├── spec.md
├── plan.md
├── cert-report.md
└── summary.md
```

## Documentación

Repo de la metodología: [FlowForge](https://github.com/efreet111/FlowForge)
