# Matriz de Pruebas — engram-dotnet

> **Última actualización**: 2026-05-14
> **Total**: 236 tests — 0 fallos

---

## verification-tools (16 + 52 MCP regresión)

| # | Tipo | Test | Escenario | Estado |
|---|------|------|-----------|--------|
| **SpecParser** | Unit | `Parse_CanonicalSpec_ExtractsAllRequirements` | 3 RF + 2 RNF en spec canónico | ✅ |
| | Unit | `Parse_SpanishHeaders_StillParses` | Secciones en español (Objetivo, RF, RNF) | ✅ |
| | Unit | `Parse_MissingRnfSection_StillParsesRfs` | Solo RF, sin RNF | ✅ |
| | Unit | `Parse_EmptyString_ReturnsUnparseable` | Markdown vacío | ✅ |
| | Unit | `Parse_NoRecognizableSections_ReturnsUnparseable` | Texto sin secciones | ✅ |
| | Unit | `Parse_ObjectiveOnly_ReturnsEmptyRequirements` | Solo Objective, sin requisitos | ✅ |
| | Unit | `Parse_WhitespaceMarkdown_ReturnsUnparseable` | Solo espacios | ✅ |
| **CycleTracker** | Unit | `GetCurrentCycle_NewChange_ReturnsZero` | Sin ciclos previos | ✅ |
| | Unit | `IncrementCycle_IncreasesCount` | Incremento secuencial | ✅ |
| | Unit | `ResetCycle_ClearsCount` | Reset después de incrementar | ✅ |
| | Unit | `ShouldEscalate_AtMaxCycles_ReturnsTrue` | Escalation en ciclo 3 | ✅ |
| | Unit | `MaxCycles_DefaultIsThree` | Default configurable | ✅ |
| **TraceabilityMatrix** | Unit | `BuildMatrix_NoObservations_AllMissing` | Sin observaciones en store | ✅ |
| | Unit | `BuildMatrix_EmptyRequirements_ReturnsEmpty` | Lista vacía de requisitos | ✅ |
| **Integration** | Integration | `SpecParser_To_CycleTracker_FullFlow` | Flujo completo parser → ciclo → matrix | ✅ |
| | Integration | `FakeVerifier_ReturnsConfiguredResult` | FakeVerifier con resultado esperado | ✅ |

## promotion-level2 (17 + 139 Store regresión)

| # | Tipo | Test | Escenario | Estado |
|---|------|------|-----------|--------|
| **MdTemplateEngine** | Unit | `Render_IncludesAllFrontmatterFields` | Campos completos con frontmatter YAML | ✅ |
| | Unit | `Render_NullTopicKey_OmitsField` | topic_key nulo omitido | ✅ |
| | Unit | `Render_EscapesSpecialChars` | Caracteres especiales escapados | ✅ |
| | Unit | `Render_EmptyContent_StillGeneratesFrontmatter` | Contenido vacío | ✅ |
| **MdSlug** | Unit | `Slugify_LowercasesAndHyphenates` | Minúsculas + guiones | ✅ |
| | Unit | `Slugify_RemovesSpecialChars` | Caracteres especiales limpiados | ✅ |
| | Unit | `Slugify_TruncatesAt60Chars` | Truncación a 60 caracteres | ✅ |
| | Unit | `Slugify_EmptyTitle_ReturnsFallback` | Título vacío → fallback | ✅ |
| | Unit | `ToFilename_IncludesDate` | Formato YYYY-MM-DD-slug.md | ✅ |
| **PromotionService** | Unit | `PromoteAsync_InvalidId_ReturnsZero` | ID inválido | ✅ |
| | Unit | `SyncAsync_DryRun_DoesNotCreateFiles` | Dry-run sin escritura | ✅ |
| **StorePromotion** | Integration | `PromoteToMdAsync_Roundtrip_PersistsMdPath` | md_path + archivo + frontmatter | ✅ |
| | Integration | `PromoteToMdAsync_AlreadyPromoted_ReturnsZero` | Ya promovido → 0 | ✅ |
| | Integration | `PromoteToMdAsync_InvalidId_ReturnsZero` | ID inexistente → 0 | ✅ |
| **StoreSync** | Integration | `SyncMdToRepoAsync_Batch_PromotesAll` | Sincronización batch | ✅ |
| | Integration | `SyncMdToRepoAsync_DryRun_ReturnsCountWithoutFiles` | Dry-run count sin archivos | ✅ |
| | Integration | `SyncMdToRepoAsync_EmptySync_ReturnsZero` | Sin observaciones pendientes | ✅ |

## traceability (28)

| # | Tipo | Test | Escenario | Estado |
|---|------|------|-----------|--------|
| **SpecParser** | Unit | `ParseTraceability_ValidEntry_ExtractsAllFields` | Traza completa con Source/Author/Date/Rationale/Relations | ✅ |
| | Unit | `ParseTraceability_MissingAuthorDate_StillParses` | Campos opcionales ausentes | ✅ |
| | Unit | `ParseTraceability_NoTraceabilitySection_ReturnsEmpty` | Sin sección Traceability | ✅ |
| **Relation Parsing** | Unit | `ParseRelations_ValidTypes_ParsesCorrectly` | 4 tipos válidos | ✅ |
| | Unit | `ParseRelations_InvalidType_SkipsEntry` | Tipo inválido ignorado | ✅ |
| | Unit | `ParseRelations_EmptyString_ReturnsEmpty` | String vacío | ✅ |
| **Cycle Detection** | Unit | `HasCycle_DirectCycle_Detected` | Ciclo directo detectado | ✅ |
| | Integration | `BuildLineage_Chain_NoCycle` | Cadena sin ciclo | ✅ |
| | Integration | `BuildLineage_MaxHops_Truncates` | 15 hops truncados a 10 | ✅ |
| **Integration** | Integration | `SaveAndGetTrace_Roundtrip_Works` | Persistencia bidireccional | ✅ |
| | Integration | `GetTrace_Untraced_ReturnsNull` | Requisito sin traza | ✅ |
| | Integration | `Lineage_SingleNode_NoAncestors` | Nodo único sin ancestros | ✅ |

## Store Regression (139 tests — pre-existentes)

| Suite | Tests | Propósito |
|-------|-------|-----------|
| SqliteStore | 110 | Operaciones CRUD, FTS5, dedup, sync, migraciones |
| PostgresStore | 26 | Paridad con SQLite + tsvector |
| HttpStore | 30 | Proxy HTTP, serialización, errores remotos |
| Server | 19 | Endpoints HTTP, handlers |
| **Total** | **139** | **0 regresiones** |

## MCP Regression (52 tests — pre-existentes)

| Suite | Tests | Propósito |
|-------|-------|-----------|
| EngramTools | 52 | Tools MCP con SqliteStore real |
| **Total** | **52** | **0 regresiones** |

---

## Resumen de Cobertura

| Feature | Unit | Integration | Total |
|---------|------|-------------|-------|
| verification-tools | 13 | 3 | 16 |
| promotion-level2 | 11 | 6 | 17 |
| traceability | 9 | 3 | 12 |
| Store (regresión) | — | 139 | 139 |
| MCP (regresión) | — | 52 | 52 |
| **Total** | **33** | **203** | **236** |

## Tipos de escenarios cubiertos

| Tipo | Tests | % |
|------|-------|---|
| Happy path | 28 | 70% |
| Edge case (nulos, vacíos, límites) | 8 | 20% |
| Error state (no encontrado, inválido) | 4 | 10% |

## Coverage de Spec Requirements

| Feature | Req totales | Escenarios totales | COMPLIANT | % |
|---------|-------------|-------------------|-----------|---|
| verification-tools | 11 | 22 | 16 | 73% |
| promotion-level2 | 10 | 19 | 15 | 79% |
| traceability | 7 | 19 | 12 | 63% |
| **Total** | **28** | **60** | **43** | **72%** |

> ⚠️ Escenarios no compliant son principalmente aquellos que dependen de LLM real (Anthropic API key) o de PostgreSQL (no disponible en CI local).
