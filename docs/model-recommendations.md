# FlowForge Model Recommendation Matrix

> **Strategy Definitions:**
> - **Budget (Go)**: Minimum cost with $10/month subscription
> - **Balanced (Go Plus)**: Best value for regular heavy use with $40/month subscription
> - **Premium (Pay-as-you-go)**: Best quality, no rate limits

> **Model Provider Strategy:**
> - **MiniMax M2.7**: Used via user's MiniMax subscription (minimax-coding-plan)
> - **OpenCode Go Models**: Used via OpenCode Go subscription ($10/month)
>   - `deepseek-v4.1-flash` — Best value for medium complexity
>   - `glm-5.3-flash` — Cheapest for simple tasks
>   - `qwen3.7-plus` — Premium reasoning for high complexity

---

## Core Skills (9)

### Skill: forge-orchestrator

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | MiniMax M2.7 | MiniMax | $0.30 | $1.20 | $60 | Routing CKP necesita razonamiento, mínimo disponible capaz |
| Balanced | MiniMax M2.7 | Go Plus | $0.30 | $1.20 | $240 | Alto volumen de decisiones sin límite artificial |
| Premium | MiniMax M2.7 | Pay-as-you-go | $0.30 | $1.20 | Unlimited | CKP-0→4 complejos sin restricciones |

### Skill: forge-discovery

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | DeepSeek V4.1 Flash | Go | $0.15 | $0.60 | $80 | Exploración con patrones, mejor límite/precio |
| Balanced | DeepSeek V4.1 Flash | Go Plus | $0.15 | $0.60 | $160 | Exploración profunda sin límite |
| Premium | MiniMax M2.7 | Pay-as-you-go | $0.30 | $1.20 | Unlimited | Análisis de código grande |

### Skill: forge-arch

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | Qwen3.7 Plus | Go | $0.40 | $1.60 | $60 | Spec writing con razonamiento multi-paso |
| Balanced | Qwen3.7 Plus | Go Plus | $0.40 | $1.60 | $180 | Alta calidad en GWT scenarios |
| Premium | Kimi K2.7 Code | Pay-as-you-go | $0.95 | $4.00 | Unlimited | Capability Matrix compleja |

### Skill: forge-plan

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | DeepSeek V4.1 Flash | Go | $0.15 | $0.60 | $80 | Traducción spec→plan estructurada |
| Balanced | DeepSeek V4.1 Flash | Go Plus | $0.15 | $0.60 | $160 | Tasks detalladas con edge cases |
| Premium | MiniMax M2.7 | Pay-as-you-go | $0.30 | $1.20 | Unlimited | Diseño completo sin límites |

### Skill: forge-dev

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | DeepSeek V4.1 Flash | Go | $0.15 | $0.60 | $80 | Código boilerplate con patrones |
| Balanced | MiniMax M2.7 | Go Plus | $0.30 | $1.20 | $240 | Ralph Wiggum loop complejo |
| Premium | Kimi K2.7 Code | Pay-as-you-go | $0.95 | $4.00 | Unlimited | Code gen especializado sin límites |

### Skill: forge-verify

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | MiniMax M2.7 | MiniMax | $0.30 | $1.20 | $60 | Auditorías con juicio analítico |
| Balanced | MiniMax M2.7 | Go Plus | $0.30 | $1.20 | $240 | LLM-as-Judge con alta precisión |
| Premium | Kimi K2.7 Code | Pay-as-you-go | $0.95 | $4.00 | Unlimited | Rework tickets detallados sin límite |

### Skill: forge-memory

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | DeepSeek V4.1 Flash | Go | $0.15 | $0.60 | $80 | Síntesis de sesión ligera |
| Balanced | DeepSeek V4.1 Flash | Go Plus | $0.15 | $0.60 | $160 | Uploads frecuentes a DB |
| Premium | GLM-5.3-Flash | Pay-as-you-go | $0.15 | $0.50 | Unlimited | ADR promotion y síntesis compleja |

### Skill: forge-index-docs

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | MiMo-V2.5 | Go | $0.14 | $0.28 | $60 | Indexado determinístico sin razonamiento |
| Balanced | MiMo-V2.5 | Go Plus | $0.14 | $0.28 | $120 | PRD, ADR, RFC múltiples |
| Premium | GLM-5.3-Flash | Pay-as-you-go | $0.15 | $0.50 | Unlimited | Indexado con contexto denso |

### Skill: forge-teacher

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | GLM-5.3-Flash | Go | $0.15 | $0.50 | $60 | Explicaciones básicas sin complejidad |
| Balanced | GLM-5.3-Flash | Go Plus | $0.15 | $0.50 | $180 | Teaching mode detallado |
| Premium | DeepSeek V4.1 Flash | Pay-as-you-go | $0.15 | $0.60 | Unlimited | Explicaciones Socráticas sin límite |

---

## OLA 1 Security (5)

### Skill: forge-arch-security

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | DeepSeek V4.1 Flash | Go | $0.15 | $0.60 | $80 | Threat modeling básico con templates |
| Balanced | MiniMax M2.7 | Go Plus | $0.30 | $1.20 | $240 | OWASP ASVS completos |
| Premium | Kimi K2.7 Code | Pay-as-you-go | $0.95 | $4.00 | Unlimited | Análisis STRIDE profundo |

### Skill: forge-plan-security

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | DeepSeek V4.1 Flash | Go | $0.15 | $0.60 | $80 | Patrones secure-by-design |
| Balanced | MiniMax M2.7 | Go Plus | $0.30 | $1.20 | $240 | Planes de seguridad detallados |
| Premium | Kimi K2.7 Code | Pay-as-you-go | $0.95 | $4.00 | Unlimited | Diseño de controles completo |

### Skill: forge-dev-security

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | MiniMax M2.7 | MiniMax | $0.30 | $1.20 | $60 | OWASP Top 10 en código |
| Balanced | MiniMax M2.7 | Go Plus | $0.30 | $1.20 | $240 | Prevención en código real |
| Premium | Kimi K2.7 Code | Pay-as-you-go | $0.95 | $4.00 | Unlimited | SAST con contexto completo |

### Skill: forge-verify-security

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | MiniMax M2.7 | MiniMax | $0.30 | $1.20 | $60 | Auditoría OWASP con juicio |
| Balanced | MiniMax M2.7 | Go Plus | $0.30 | $1.20 | $240 | SAST completo con contexto |
| Premium | Kimi K2.7 Code | Pay-as-you-go | $0.95 | $4.00 | Unlimited | Dependency audit profunda |

### Skill: forge-dev-solid

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | GLM-5.3-Flash | Go | $0.15 | $0.50 | $60 | Validación SOLID checklist |
| Balanced | MiniMax M2.7 | Go Plus | $0.30 | $1.20 | $240 | Post-coding validation completa |
| Premium | Kimi K2.7 Code | Pay-as-you-go | $0.95 | $4.00 | Unlimited | Revisión SOLID detallada |

---

## OLA 2 Code Quality (5)

### Skill: forge-plan-patterns

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | DeepSeek V4.1 Flash | Go | $0.15 | $0.60 | $80 | Selección de patrones con catalog |
| Balanced | Qwen3.7 Plus | Go Plus | $0.40 | $1.60 | $180 | Patrones estructurales avanzados |
| Premium | Kimi K2.7 Code | Pay-as-you-go | $0.95 | $4.00 | Unlimited | Cross-cutting concerns profundo |

### Skill: forge-dev-testing

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | MiniMax M2.7 | MiniMax | $0.30 | $1.20 | $60 | Unit tests con razonamiento |
| Balanced | MiniMax M2.7 | Go Plus | $0.30 | $1.20 | $240 | Business logic compleja |
| Premium | Kimi K2.7 Code | Pay-as-you-go | $0.95 | $4.00 | Unlimited | Edge cases y mutation testing |

### Skill: forge-dev-performance

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | MiniMax M2.7 | MiniMax | $0.30 | $1.20 | $60 | Optimizaciones con análisis |
| Balanced | MiniMax M2.7 | Go Plus | $0.30 | $1.20 | $240 | Queries y APIs optimizadas |
| Premium | Kimi K2.7 Code | Pay-as-you-go | $0.95 | $4.00 | Unlimited | N+1 detection profunda |

### Skill: forge-verify-complexity

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | GLM-5.3-Flash | Go | $0.15 | $0.50 | $60 | Auditoría de complejidad simple |
| Balanced | DeepSeek V4.1 Flash | Go Plus | $0.15 | $0.60 | $160 | Cyclomatic complexity análisis |
| Premium | MiniMax M2.7 | Pay-as-you-go | $0.30 | $1.20 | Unlimited | Cognitive load máxima |

### Skill: forge-verify-performance

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | MiniMax M2.7 | MiniMax | $0.30 | $1.20 | $60 | Benchmarks con análisis |
| Balanced | MiniMax M2.7 | Go Plus | $0.30 | $1.20 | $240 | Validación RNF completa |
| Premium | Kimi K2.7 Code | Pay-as-you-go | $0.95 | $4.00 | Unlimited | Profiling detallado sin límite |

---

## OLA 3 Infrastructure (8)

### Skill: forge-discovery-security

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | DeepSeek V4.1 Flash | Go | $0.15 | $0.60 | $80 | CVE scan con patrones |
| Balanced | DeepSeek V4.1 Flash | Go Plus | $0.15 | $0.60 | $160 | Historial de vulnerabilidades |
| Premium | Kimi K2.7 Code | Pay-as-you-go | $0.95 | $4.00 | Unlimited | Security reconnaissance profunda |

### Skill: forge-discovery-compliance

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | DeepSeek V4.1 Flash | Go | $0.15 | $0.60 | $80 | Check GDPR/SOC2 básico |
| Balanced | MiniMax M2.7 | Go Plus | $0.30 | $1.20 | $240 | Requisitos regulatorios completos |
| Premium | Qwen3.7 Plus | Pay-as-you-go | $0.40 | $1.60 | Unlimited | Análisis de compliance profundo |

### Skill: forge-arch-performance

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | DeepSeek V4.1 Flash | Go | $0.15 | $0.60 | $80 | SLAs/SLOs con templates |
| Balanced | Qwen3.7 Plus | Go Plus | $0.40 | $1.60 | $180 | Performance SLAs completos |
| Premium | Kimi K2.7 Code | Pay-as-you-go | $0.95 | $4.00 | Unlimited | SLOs medibles complejos |

### Skill: forge-arch-a11y

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | GLM-5.3-Flash | Go | $0.15 | $0.50 | $60 | WCAG requirements básicos |
| Balanced | DeepSeek V4.1 Flash | Go Plus | $0.15 | $0.60 | $160 | UI components accesibles |
| Premium | MiniMax M2.7 | Pay-as-you-go | $0.30 | $1.20 | Unlimited | WCAG audit completa |

### Skill: forge-arch-domain

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | Qwen3.7 Plus | Go | $0.40 | $1.60 | $60 | Bounded contexts con razonamiento |
| Balanced | Qwen3.7 Plus | Go Plus | $0.40 | $1.60 | $180 | Ubiquitous language detallado |
| Premium | Kimi K2.6 | Pay-as-you-go | $0.95 | $4.00 | Unlimited | Domain events y aggregates complejos |

### Skill: forge-plan-migrations

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | Qwen3.7 Plus | Go | $0.40 | $1.60 | $60 | Migration strategy con análisis |
| Balanced | Qwen3.7 Plus | Go Plus | $0.40 | $1.60 | $180 | Zero-downtime deployments |
| Premium | Kimi K2.6 | Pay-as-you-go | $0.95 | $4.00 | Unlimited | Schema evolution compleja |

### Skill: forge-plan-rollback

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | DeepSeek V4.1 Flash | Go | $0.15 | $0.60 | $80 | Rollback plan con templates |
| Balanced | MiniMax M2.7 | Go Plus | $0.30 | $1.20 | $240 | Blue-green y canary completos |
| Premium | Kimi K2.7 Code | Pay-as-you-go | $0.95 | $4.00 | Unlimited | Feature flags y contratos complejos |

### Skill: forge-dev-refactor

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | MiniMax M2.7 | MiniMax | $0.30 | $1.20 | $60 | Code smells con catalog |
| Balanced | MiniMax M2.7 | Go Plus | $0.30 | $1.20 | $240 | Martin Fowler catalog |
| Premium | Kimi K2.7 Code | Pay-as-you-go | $0.95 | $4.00 | Unlimited | Test-preserving transformations |

---

## OLA 4 Metrics (5)

### Skill: forge-discovery-cost

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | GLM-5.3-Flash | Go | $0.15 | $0.50 | $60 | Estimación con fórmulas |
| Balanced | GLM-5.3-Flash | Go Plus | $0.15 | $0.50 | $180 | Compute y storage detallado |
| Premium | DeepSeek V4.1 Flash | Pay-as-you-go | $0.15 | $0.60 | Unlimited | Data pipelines complejos |

### Skill: forge-verify-a11y

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | GLM-5.3-Flash | Go | $0.15 | $0.50 | $60 | WCAG checklist rápido |
| Balanced | DeepSeek V4.1 Flash | Go Plus | $0.15 | $0.60 | $160 | Aria y keyboard navigation |
| Premium | MiniMax M2.7 | Pay-as-you-go | $0.30 | $1.20 | Unlimited | Color contrast y focus audit |

### Skill: forge-memory-metrics

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | MiMo-V2.5 | Go | $0.14 | $0.28 | $60 | Métricas formulaicas |
| Balanced | MiMo-V2.5 | Go Plus | $0.14 | $0.28 | $120 | Tech debt y cycle time |
| Premium | GLM-5.3-Flash | Pay-as-you-go | $0.15 | $0.50 | Unlimited | Code quality trends completos |

### Skill: forge-memory-changelog

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | MiMo-V2.5 | Go | $0.14 | $0.28 | $60 | Changelog determinístico |
| Balanced | MiMo-V2.5 | Go Plus | $0.14 | $0.28 | $120 | Formato changelog estándar |
| Premium | GLM-5.3-Flash | Pay-as-you-go | $0.15 | $0.50 | Unlimited | Release notes detalladas |

### Skill: forge-memory-knowledge

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | Qwen3.7 Plus | Go | $0.40 | $1.60 | $60 | Links entre ADRs simple |
| Balanced | Qwen3.7 Plus | Go Plus | $0.40 | $1.60 | $180 | Multi-repo decisions |
| Premium | Kimi K2.7 Code | Pay-as-you-go | $0.95 | $4.00 | Unlimited | Cross-project architecture completa |

---

## FlowDocs (9)

### Skill: flowdoc-hu

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | GLM-5.3-Flash | Go | $0.15 | $0.50 | $60 | HU templating directo |
| Balanced | GLM-5.3-Flash | Go Plus | $0.15 | $0.50 | $180 | HU con escenarios completos |
| Premium | DeepSeek V4.1 Flash | Pay-as-you-go | $0.15 | $0.60 | Unlimited | HU post-dev con decisiones |

### Skill: flowdoc-prd

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | GLM-5.3-Flash | Go | $0.15 | $0.50 | $60 | PRD template directo |
| Balanced | DeepSeek V4.1 Flash | Go Plus | $0.15 | $0.60 | $160 | PRD completo con features |
| Premium | Qwen3.7 Plus | Pay-as-you-go | $0.40 | $1.60 | Unlimited | PRD con edge cases complejos |

### Skill: flowdoc-adr

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | DeepSeek V4.1 Flash | Go | $0.15 | $0.60 | $80 | ADR formato con templates |
| Balanced | DeepSeek V4.1 Flash | Go Plus | $0.15 | $0.60 | $160 | Context → Decision → Consequences |
| Premium | Qwen3.7 Plus | Pay-as-you-go | $0.40 | $1.60 | Unlimited | ADR con implicaciones profundas |

### Skill: flowdoc-rfc

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | GLM-5.3-Flash | Go | $0.15 | $0.50 | $60 | RFC técnico templated |
| Balanced | DeepSeek V4.1 Flash | Go Plus | $0.15 | $0.60 | $160 | RFC con alternativas y tradeoffs |
| Premium | Qwen3.7 Plus | Pay-as-you-go | $0.40 | $1.60 | Unlimited | RFC profundo y completo |

### Skill: flowdoc-api

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | DeepSeek V4.1 Flash | Go | $0.15 | $0.60 | $80 | Scan de endpoints con código |
| Balanced | MiniMax M2.7 | Go Plus | $0.30 | $1.20 | $240 | Endpoints.md template completo |
| Premium | Kimi K2.7 Code | Pay-as-you-go | $0.95 | $4.00 | Unlimited | Documentación API exhaustiva |

### Skill: flowdoc-db

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | DeepSeek V4.1 Flash | Go | $0.15 | $0.60 | $80 | Schema scan con ORM patterns |
| Balanced | MiniMax M2.7 | Go Plus | $0.30 | $1.20 | $240 | Tablas, relaciones, constraints |
| Premium | Kimi K2.7 Code | Pay-as-you-go | $0.95 | $4.00 | Unlimited | schema.md con índices y enums |

### Skill: flowdoc-review

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | GLM-5.3-Flash | Go | $0.15 | $0.50 | $60 | Validación de formato rápida |
| Balanced | DeepSeek V4.1 Flash | Go Plus | $0.15 | $0.60 | $160 | Ortografía y cross-references |
| Premium | MiniMax M2.7 | Pay-as-you-go | $0.30 | $1.20 | Unlimited | ADR index y naming audit |

### Skill: flowdoc-discover

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | MiniMax M2.7 | MiniMax | $0.30 | $1.20 | $60 | Stack analysis con reasoning |
| Balanced | MiniMax M2.7 | Go Plus | $0.30 | $1.20 | $240 | Arquitectura y decisions profundas |
| Premium | Kimi K2.7 Code | Pay-as-you-go | $0.95 | $4.00 | Unlimited | Investigación de proyecto completa |

### Skill: flowdoc-assist

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | Qwen3.7 Plus | Go | $0.40 | $1.60 | $60 | Coordinación FlowDoc con reasoning |
| Balanced | Qwen3.7 Plus | Go Plus | $0.40 | $1.60 | $180 | Specialists múltiples |
| Premium | Kimi K2.7 Code | Pay-as-you-go | $0.95 | $4.00 | Unlimited | Session register completo |

---

## Utilities (13)

### Skill: branch-pr

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | MiMo-V2.5 | Go | $0.14 | $0.28 | $60 | PR checks determinístico |
| Balanced | GLM-5.3-Flash | Go Plus | $0.15 | $0.50 | $180 | Issue-first checks completos |
| Premium | DeepSeek V4.1 Flash | Pay-as-you-go | $0.15 | $0.60 | Unlimited | PR preparación detallada |

### Skill: chained-pr

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | DeepSeek V4.1 Flash | Go | $0.15 | $0.60 | $80 | PR chaining con templates |
| Balanced | MiniMax M2.7 | Go Plus | $0.30 | $1.20 | $240 | Split de PRs sobre 400 líneas |
| Premium | Kimi K2.7 Code | Pay-as-you-go | $0.95 | $4.00 | Unlimited | Stacked PRs complejos |

### Skill: go-testing

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | MiniMax M2.7 | MiniMax | $0.30 | $1.20 | $60 | Go tests con razonamiento |
| Balanced | MiniMax M2.7 | Go Plus | $0.30 | $1.20 | $240 | Bubbletea teatest y golden files |
| Premium | Kimi K2.7 Code | Pay-as-you-go | $0.95 | $4.00 | Unlimited | Go testing patterns completos |

### Skill: work-unit-commits

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | GLM-5.3-Flash | Go | $0.15 | $0.50 | $60 | Commit splitting básico |
| Balanced | DeepSeek V4.1 Flash | Go Plus | $0.15 | $0.60 | $160 | Work units como reviewable chunks |
| Premium | Qwen3.7 Plus | Pay-as-you-go | $0.40 | $1.60 | Unlimited | Chained PRs sin límites |

### Skill: skill-creator

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | DeepSeek V4.1 Flash | Go | $0.15 | $0.60 | $80 | Creación de skills con templates |
| Balanced | Qwen3.7 Plus | Go Plus | $0.40 | $1.60 | $180 | LLM-first skills con frontmatter |
| Premium | Kimi K2.7 Code | Pay-as-you-go | $0.95 | $4.00 | Unlimited | Agent instructions completas |

### Skill: skill-improver

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | DeepSeek V4.1 Flash | Go | $0.15 | $0.60 | $80 | Audit de skills con patrones |
| Balanced | MiniMax M2.7 | Go Plus | $0.30 | $1.20 | $240 | Refactor de skills existente |
| Premium | Kimi K2.7 Code | Pay-as-you-go | $0.95 | $4.00 | Unlimited | Skill quality completa |

### Skill: skill-registry

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | MiMo-V2.5 | Go | $0.14 | $0.28 | $60 | Indexado de skills mecánico |
| Balanced | GLM-5.3-Flash | Go Plus | $0.15 | $0.50 | $180 | Por trigger y path |
| Premium | DeepSeek V4.1 Flash | Pay-as-you-go | $0.15 | $0.60 | Unlimited | Registry completa auto-generada |

### Skill: judgment-day

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | MiniMax M2.7 | MiniMax | $0.30 | $1.20 | $60 | Dual review con juicio |
| Balanced | MiniMax M2.7 | Go Plus | $0.30 | $1.20 | $240 | Blind dual review adversarial |
| Premium | Kimi K2.7 Code | Pay-as-you-go | $0.95 | $4.00 | Unlimited | Re-judge sin límites |

### Skill: comment-writer

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | GLM-5.3-Flash | Go | $0.15 | $0.50 | $60 | Comentarios colaborativos simples |
| Balanced | GLM-5.3-Flash | Go Plus | $0.15 | $0.50 | $180 | Warm y direct feedback |
| Premium | DeepSeek V4.1 Flash | Pay-as-you-go | $0.15 | $0.60 | Unlimited | PR feedback detallado |

### Skill: cognitive-doc-design

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | GLM-5.3-Flash | Go | $0.15 | $0.50 | $60 | Docs de bajo cognitive load |
| Balanced | DeepSeek V4.1 Flash | Go Plus | $0.15 | $0.60 | $160 | READMEs y RFCs estructurados |
| Premium | Qwen3.7 Plus | Pay-as-you-go | $0.40 | $1.60 | Unlimited | Architecture docs completos |

### Skill: issue-creation

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | MiMo-V2.5 | Go | $0.14 | $0.28 | $60 | Issue creation determinístico |
| Balanced | GLM-5.3-Flash | Go Plus | $0.15 | $0.50 | $180 | Issue-first checks completos |
| Premium | DeepSeek V4.1 Flash | Pay-as-you-go | $0.15 | $0.60 | Unlimited | Bug reports detallados |

### Skill: find-skills

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | GLM-5.3-Flash | Go | $0.15 | $0.50 | $60 | Búsqueda de skills simple |
| Balanced | DeepSeek V4.1 Flash | Go Plus | $0.15 | $0.60 | $160 | Descubrimiento de funcionalidades |
| Premium | Qwen3.7 Plus | Pay-as-you-go | $0.40 | $1.60 | Unlimited | Skill installation completa |

### Skill: customize-opencode

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | GLM-5.3-Flash | Go | $0.15 | $0.50 | $60 | Configuración determinística |
| Balanced | DeepSeek V4.1 Flash | Go Plus | $0.15 | $0.60 | $160 | opencode.json y agents |
| Premium | Qwen3.7 Plus | Pay-as-you-go | $0.40 | $1.60 | Unlimited | Plugins y MCP servers completos |

---

## Other (5)

### Skill: forge-onboarding

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | GLM-5.3-Flash | Go | $0.15 | $0.50 | $60 | Briefing inicial con signals |
| Balanced | Qwen3.7 Plus | Go Plus | $0.40 | $1.60 | $180 | Engram memories y decisiones |
| Premium | Kimi K2.7 Code | Pay-as-you-go | $0.95 | $4.00 | Unlimited | Onboarding completo de equipo |

### Skill: forge-memory-contradiction

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | DeepSeek V4.1 Flash | Go | $0.15 | $0.60 | $80 | Detección de contradicciones con patterns |
| Balanced | MiniMax M2.7 | Go Plus | $0.30 | $1.20 | $240 | mem_check_contradictions |
| Premium | Kimi K2.7 Code | Pay-as-you-go | $0.95 | $4.00 | Unlimited | Resolución profunda de conflictos |

### Skill: forge-persona

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | GLM-5.3-Flash | Go | $0.15 | $0.50 | $60 | Personalidad Reginald estilística |
| Balanced | GLM-5.3-Flash | Go Plus | $0.15 | $0.50 | $180 | Mayordomo inglés elegante |
| Premium | DeepSeek V4.1 Flash | Pay-as-you-go | $0.15 | $0.60 | Unlimited | Directo y analítico sin límites |

### Skill: _shared

| Strategy | Modelo | Plan | Input | Output | Límite | Por Qué |
|----------|--------|------|-------|--------|--------|---------|
| Budget | GLM-5.3-Flash | Go | $0.15 | $0.50 | $60 | SDD shared references básico |
| Balanced | DeepSeek V4.1 Flash | Go Plus | $0.15 | $0.60 | $160 | Skills compartidos |
| Premium | GLM-5.3-Flash | Pay-as-you-go | $0.15 | $0.50 | Unlimited | Referencias completas |

---

## Summary Table by Strategy

### Budget Strategy (Go Plan $10/month)

| Modelo | Input | Output | Límite | Skills que cubre |
|--------|-------|--------|--------|-----------------|
| MiMo-V2.5 | $0.14 | $0.28 | $60 | Triviales: index-docs, memory-metrics, changelog, branch-pr, issue-creation |
| GLM-5.3-Flash | $0.15 | $0.50 | $60 | Simples: teacher, solid, compliance-cost, cognitive-doc |
| DeepSeek V4.1 Flash | $0.15 | $0.60 | $80 | Medios: discovery, plan, dev, memory, security, patterns, rollback |
| MiniMax M2.7 | $0.30 | $1.20 | $60 | Alta complejidad: orchestrator, verify, dev-security, testing, performance, refactor |
| Qwen3.7 Plus | $0.40 | $1.60 | $60 | Premium bajo: arch, domain, migrations, knowledge, flowdoc-assist, onboarding |

### Balanced Strategy (Go Plus $40/month)

| Modelo | Input | Output | Límite | Skills que cubre |
|--------|-------|--------|--------|-----------------|
| MiMo-V2.5 | $0.14 | $0.28 | $120 | Triviales (2x límite) |
| GLM-5.3-Flash | $0.15 | $0.50 | $180 | Simples (3x límite) |
| DeepSeek V4.1 Flash | $0.15 | $0.60 | $160 | Medios (2x límite) |
| MiniMax M2.7 | $0.30 | $1.20 | $240 | Alta complejidad (4x límite) |
| Qwen3.7 Plus | $0.40 | $1.60 | $180 | Premium (3x límite) |

### Premium Strategy (Pay-as-you-go)

| Modelo | Input | Output | Mejor Para |
|--------|-------|--------|-----------|
| GLM-5.3-Flash | $0.15 | $0.50 | Tasks triviales sin límite |
| DeepSeek V4.1 Flash | $0.15 | $0.60 | Mejor valor general |
| MiniMax M2.7 | $0.30 | $1.20 | Alta complejidad sin límite |
| Kimi K2.7 Code | $0.95 | $4.00 | Código especializado sin límite |
| Kimi K2.6 | $0.95 | $4.00 | Código premium sin límite |

---

*Generated: 2026-10-14*
*Source: model-guide-3-strategies-20261013.md (Obsidian vault)*
*Document: FlowForge Model Recommendation Matrix v2.0*
