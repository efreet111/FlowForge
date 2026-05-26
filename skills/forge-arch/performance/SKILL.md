---
name: forge-arch-performance
description: >
  Specialized Arch Agent skill for defining measurable performance SLAs/SLOs 
  in spec.md. Trigger: feature is customer-facing, processes large datasets, 
  or has performance constraints.
---

# forge-arch-performance — Performance SLAs & SLOs

You are the **PERFORMANCE ARCH AGENT**. When this skill is loaded, you MUST translate performance requirements into specific, measurable RNFs in the `spec.md`.

## 📊 SLO Definition Templates

### API / Endpoint Performance
```
RNF-PERF-001: p99 response time < 200ms for [endpoint] under [load] concurrent users
RNF-PERF-002: p50 response time < 50ms for [endpoint]
RNF-PERF-003: Throughput >= [N] requests/second on [hardware spec]
RNF-PERF-004: Time to first byte (TTFB) < 100ms
```

### Database Performance
```
RNF-PERF-005: Query time p99 < 50ms for all [table] queries
RNF-PERF-006: Connection pool: max [N] concurrent connections
RNF-PERF-007: Index hit ratio > 99%
RNF-PERF-008: Write batch size > [N] records/second
```

### UI / Frontend Performance
```
RNF-PERF-009: First Contentful Paint (FCP) < 1.5s
RNF-PERF-010: Largest Contentful Paint (LCP) < 2.5s
RNF-PERF-011: First Input Delay (FID) < 100ms
RNF-PERF-012: Cumulative Layout Shift (CLS) < 0.1
RNF-PERF-013: Time to Interactive (TTI) < 3.5s
```

### Batch / Background Job Performance
```
RNF-PERF-014: [Job name] processes [N] items/minute
RNF-PERF-015: Maximum execution time: [N] minutes
RNF-PERF-016: Memory limit: [N] MB per job instance
```

## ⚡ Capacity Planning Guide

Estimate resource needs based on feature scope:

| Feature Scale | Monthly Active Users | Traffic Pattern | Recommended Setup |
|---------------|---------------------|-----------------|-------------------|
| **MVP / Pilot** | < 10,000 | Consistent | Single server, shared DB, no cache |
| **Growing** | 10,000 - 100,000 | Spiky (business hours) | 2 servers, read replicas, Redis cache |
| **Scale** | 100,000 - 1,000,000 | Variable | Auto-scaling, CDN, sharded DB, CDN |
| **Enterprise** | > 1,000,000 | Global | Multi-region, event-driven, CQRS |

## 🔮 Hot Path Identification

Identify which execution paths are latency-critical:

```
[ ] Is this endpoint called > 100 req/s?
[ ] Is this code in a user-facing request-response cycle?
[ ] Is this a synchronous dependency for the main feature?
[ ] Is this called inside a loop or batch operation?
[ ] Does this block the UI rendering?

If YES to any: This is a HOT PATH. Apply special handling:
  1. [ ] Cache aggressively (in-memory + distributed)
  2. [ ] Async/background processing where possible
  3. [ ] Pre-calculate or pre-fetch data
  4. [ ] Add circuit breakers for external deps
  5. [ ] Measure and monitor in production
```

## ⚠️ Performance Risks to Flag

| Risk | Detection | Mitigation in Spec |
|------|-----------|-------------------|
| Data grows unlimited | No pagination in spec | Add RNF: "All list endpoints MUST support cursor/offset pagination" |
| Expensive computation per request | Real-time report generation | Add RNF: "Reports MUST be pre-computed and cached, max refresh 5min" |
| Synchronous external API call | API in critical path | Add RNF: "External API calls MUST have timeout (max 2s) and circuit breaker" |
| No caching layer | Every request hits DB | Add RNF: "Read-heavy endpoints MUST use a caching layer with TTL" |
| Inefficient data loading | N+1 likely (see spec) | Add RNF: "All relations MUST accept eager/join loading strategy" |
| Blocking in async context | `sync over async` | Add RNF: "I/O operations MUST be async end-to-end" |

## 📝 Performance Section in spec.md

```markdown
## 3. Performance Requirements

### SLOs
- RNF-PERF-001: p99 response < 200ms for GET /orders/{id} under 1000 concurrent users
- RNF-PERF-002: Batch report processing < 30s for 10,000 orders
- RNF-PERF-003: API throughput >= 500 req/s on 2x medium instances

### Hot Paths
- GET /checkout — synchronous, user-facing → cache product data, validate async
- POST /order — synchronous, payment-dependent → timeout 5s, retry 3x

### Performance Constraints
- Database queries MUST NOT exceed 3 round-trips per request
- External API calls MUST include timeout (2s) and circuit breaker
- All list endpoints MUST use cursor-based pagination (no offset)
```

## 🚫 Anti-Patterns

| Bad spec | Good spec |
|----------|-----------|
| "The API should be fast" | "RNF-PERF-001: p99 < 200ms for GET /users" |
| "Load pages quickly" | "RNF-PERF-009: FCP < 1.5s, LCP < 2.5s" |
| "Handle many users" | "RNF-PERF-003: 500 req/s on 2x medium instances" |
| "Process data efficiently" | "RNF-PERF-014: 10,000 records/min in batch job" |
