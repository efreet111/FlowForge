# Contexto del Proyecto FlowForge

> **Última actualización**: 2026-05-13

---

## Estado Actual

FlowForge está en **fase de diseño conceptual** — se ha definido la metodología EngramFlow v0.2 y la estrategia de memoria. El repositorio es principalmente documental, con 10 documentos en `docs/`.

- **Directorio raíz**: `/media/gantz/300extra/Proyectos/FlowForge/`
- **Repositorio engram-dotnet**: `/media/gantz/300extra/Proyectos/engram-dotnet/`

## Propósito del Proyecto

**EngramFlow** es una metodología Agentic SDLC diseñada para equipos SMB (2-20 personas) que integran agentes de IA en su ciclo de desarrollo. Se complementa con **engram-dotnet**, un motor de memoria persistente en .NET 10.

### Pilares del diseño actual (v0.2)

1. **4 fases, 3 checkpoints humanos, 5-6 agentes**
2. **Memoria estratificada en 2 niveles**: operativa (DB con TTL) + estructurada (.md versionados)
3. **Model routing por tipo de tarea**: Sonnet para razonar, Haiku para tareas baratas, SQL para persistencia
4. **Orquestador AI opcional**: el flujo funciona con artefactos versionados como protocolo
5. **Feedback loop autónomo**: Verify Agent escribe rework_ticket.md → Dev Agent retoma

## Documentación Generada

| Documento | Contenido |
|-----------|-----------|
| `01-engramflow-architecture.md` | Arquitectura completa de EngramFlow v0.2 |
| `02-memory-strategy.md` | Estrategia de 2 niveles de memoria |
| `03-engram-dotnet-gaps.md` | Análisis de gaps entre engram-dotnet y lo que EngramFlow necesita |
| `04-roadmap.md` | Roadmap conjunto en 3 fases + timeline |
| `05-comparison-methodologies.md` | Investigación comparativa de metodologías Agentic SDLC |
| `new-workflow.md` | Workflow previo (6 etapas) — desactualizado vs EngramFlow |
| `PRD.md` | Product Requirements Document — visión general del ecosistema |

## Estado de Dependencias

| Dependencia | Versión | Estado |
|-------------|---------|--------|
| engram-dotnet | main (v1.2.0) | ⚠️ Necesita cambios (TTL, promoción .md, verification tools) |
| .NET SDK | 10.0 | ✅ Requerido para compilar engram-dotnet |
| engram (Go original) | upstream | ✅ Referencia de diseño, no dependencia directa |

## Próximos Pasos

1. **Fase 1**: Implementar TTL + Pruning en engram-dotnet (7-10h)
2. **Fase 2**: Implementar Promoción a Nivel 2 — .md (7-9h)
3. **Fase 3**: Implementar Verification Tools (8-10h)
4. Refinar EngramFlow v0.2 según feedback de implementación

Ver [04-roadmap.md](04-roadmap.md) para el detalle completo.
