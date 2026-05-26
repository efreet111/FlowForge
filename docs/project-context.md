# Contexto del Proyecto FlowForge

> **Última actualización**: 2026-05-25

---

## Estado Actual

FlowForge está en **v0.3 — Fase de Fortalecimiento de Agentes**. La metodología EngramFlow está completamente definida con 5 checkpoints formalizados y **31 skills** (7 core + 23 especializadas + 1 teacher cross-cutting). El repositorio tiene 16 documentos en `docs/` y archivos de integración IDE en `ide/`.

- **Directorio raíz**: `/media/gantz/300extra/Proyectos/FlowForge/`
- **Repositorio engram-dotnet**: `/media/gantz/300extra/Proyectos/engram-dotnet/`

## Propósito del Proyecto

**FlowForge** es una metodología Agentic SDLC diseñada para equipos SMB (2-20 personas) que integran agentes de IA en su ciclo de desarrollo. Se complementa con **engram-dotnet**, un motor de memoria persistente en .NET 10 con 25 herramientas MCP.

### Pilares del diseño actual (v0.3)

1. **5 fases, 5 checkpoints (CKP-0 → CKP-4), 7 agentes**
2. **31 skills totales**: 7 core + 10 OLA 1+2 (seguridad, SOLID, calidad, patrones) + 8 OLA 3 (infraestructura, dominio) + 5 OLA 4 (métricas, conocimiento) + 1 teacher cross-cutting
3. **Memoria estratificada en 2 niveles**: operativa (DB con TTL) + estructurada (.md versionados)
4. **Model routing por tipo de tarea**: modelos económicos para lectura, fuertes para razonamiento y auditoría
5. **Checkpoints con semántica de colores**: 🔴 binario (CKP-0, CKP-3), 🟡 flexible humano (CKP-1, CKP-2), 🟢 deploy gate (CKP-4)
6. **Integración IDE**: archivos listos para OpenCode, Cursor, Antigravity, y VS Code en `ide/`
7. **Skill de profesor toggleable**: `forge-teacher` se activa/desactiva desde `.flowforge.json`

## Estado de Dependencias

| Dependencia | Versión | Estado |
|-------------|---------|--------|
| engram-dotnet | main (post PR #11) | ✅ 7 features implementadas, 258 tests |
| .NET SDK | 10.0 | ✅ Requerido para compilar engram-dotnet |
| engram (Go original) | upstream | ✅ Referencia de diseño, no dependencia directa |

## Documentación

| Documento | Contenido |
|-----------|-----------|
| `01-engramflow-architecture.md` | Arquitectura completa de EngramFlow v0.3 |
| `02-memory-strategy.md` | Estrategia de 2 niveles de memoria |
| `03-engram-dotnet-gaps.md` | Análisis de gaps (7/7 features implementadas) |
| `04-roadmap.md` | Roadmap v0.3 con 4 OLAS completadas |
| `14-flowforge-complete-reference.md` | Referencia completa con casos de prueba |
| `15-agent-skills-technical-spec.md` | Especificación técnica de 7 agentes y 30 skills |
| `16-ide-integration-plan.md` | Plan de integración con 4 IDEs |
| `ide/` | Archivos listos para OpenCode, Cursor, Antigravity, VS Code |

## Skills por Agente

| Agente | Core | Especializadas | Total |
|--------|------|---------------|-------|
| forge-orchestrator | 1 | — | 1 |
| forge-discovery | 1 | security, compliance, cost | 4 |
| forge-arch | 1 | security, performance, a11y, domain | 5 |
| forge-plan | 1 | security, patterns, migrations, rollback | 5 |
| forge-dev | 1 | security, solid, testing, performance, refactor | 6 |
| forge-verify | 1 | security, complexity, performance, a11y | 5 |
| forge-memory | 1 | metrics, changelog, knowledge | 4 |
| **Total** | **7** | **23** | **30** |
| forge-teacher (cross-cutting) | — | — | **1** |
