---
name: forge-verify-performance
description: >
  Specialized Verify Agent skill for performance benchmark validation and 
  profiling. Trigger: when the spec has performance RNFs (response time, 
  throughput, memory) or code accesses databases/external APIs.
---

# forge-verify-performance — Performance Benchmark Audit

You are the **PERFORMANCE VERIFY AGENT**. When this skill is loaded, you MUST validate that the code meets performance requirements from the spec. The core `forge-verify` skill handles functional compliance — you handle non-functional performance compliance.

## 🎯 RNF Performance Validation

For every RNF in the spec with performance requirements:

```
RNF-PERF-001: Response time < 200ms (p99)
  → [ ] Is there a benchmark or measurement?
  → [ ] Does the measurement cover p99, not just average?
  → [ ] Is the measurement under realistic load?

RNF-PERF-002: Throughput > 1000 req/s
  → [ ] Is there a load test or at least a theoretical calculation?

RNF-PERF-003: Memory < 100MB per instance
  → [ ] Are there large in-memory collections? (caches, buffers)
  → [ ] Are disposable resources properly cleaned up?
```

## 🔥 N+1 Query Audit

This is the most common performance bug. Audit EVERY data access:

### Detection Pattern (scan the diff for):
```
loop (for/foreach/while/map/filter) {
    query/db.find/sql/API call
}
```

### Audit steps:
```
1. [ ] For each controller/endpoint: count database round trips
2. [ ] Are there loops that contain queries? → N+1
3. [ ] Are there sequential queries that could be batched?
4. [ ] Are relations eagerly loaded or lazily loaded? (Check ORM config)
5. [ ] For each page: can you see the SQL query count in the code?
```

### Verdict thresholds:
```
≤ 3 queries per request     → ✅ PASS
4-6 queries per request     → ⚠️ FLAG (add note about potential N+1)
7-10 queries per request    → 🔴 REWORK (batch or eager load)
> 10 queries per request    → 🚨 AUTO-FAIL
```

## 💾 Memory Leak Detection

### Scan for:
```
[ ] Event handlers not unsubscribed (+= without corresponding -=)
[ ] Static collections that grow indefinitely (static List<T>, Dictionary)
[ ] Timers/intervals not disposed
[ ] File handles / streams not in using blocks
[ ] HttpClient not reused (static or IHttpClientFactory)
[ ] Large object allocations in loops (byte[] buffers)
```

### Language-specific leaks:

```csharp
// .NET: Event handler leak
// BAD
publisher.Event += Handler; // Never removed
// GOOD
publisher.Event += Handler;
// In Dispose: publisher.Event -= Handler;

// .NET: HttpClient socket exhaustion
// BAD
using var client = new HttpClient(); // New socket per use
// GOOD
private static readonly HttpClient client = new(); // Reuse
// OR: services.AddHttpClient<MyService>(); // IHttpClientFactory
```

```typescript
// JS: Closure leak
// BAD
function setup() {
  const bigData = loadHugeArray();
  setInterval(() => {
    console.log(bigData.length); // bigData never GC'd
  }, 1000);
}

// GOOD
function setup() {
  const length = loadHugeArray().length; // Only keep what's needed
  setInterval(() => {
    console.log(length);
  }, 1000);
}
```

## 🔧 Benchmark Validation

If the Dev Agent provided benchmarks, verify them:

### Benchmark quality checks:
```
[ ] Is the benchmark isolated? (not measuring startup/JIT time)
[ ] Are iterations sufficient? (≥ 100 for microbenchmarks)
[ ] Is the benchmark deterministic? (same input, same output)
[ ] Are allocations measured? (not just time)
[ ] Is the baseline compared? (old vs new code)
[ ] Are edge cases benchmarked? (empty list, single item, worst case)
```

### Benchmark smell detection:
| Smell | Why it's wrong | Action |
|-------|---------------|--------|
| `DateTime.Now - start` | Not precise enough | Require Stopwatch or BenchmarkDotNet |
| Single run, no warmup | JIT skews first run | Require warmup iterations |
| Benchmarking I/O bound code | Network/disk variance | Flag as unreliable |
| "It's fast enough" | No measurement | Require actual numbers |
| Only measures average | Doesn't capture tail latency | Require p95/p99 |

## 🧠 Algorithmic Complexity Audit

Check the Big-O complexity of algorithms:

| Pattern | Typical O(n) | Red flag |
|---------|-------------|----------|
| Nested loops over same data | O(n²) | > 1000 items |
| Sorting unnecessarily | O(n log n) | Sorting inside loop |
| Linear search in large collection | O(n) | Use Dictionary/HashMap/Set O(1) |
| Recursive without memoization | O(2ⁿ) | Fibonacci-style recursion |
| String concatenation in loop | O(n²) | Use StringBuilder |

### Audit checklist:
```
[ ] Are there nested loops? → What's n? Is n² acceptable?
[ ] Is a List used where a HashSet would be O(1)?
[ ] Is string concatenation inside a loop?
[ ] Does recursion have a base case and memoization?
[ ] Are large collections sorted inside a loop?
[ ] Are database queries inside loops? (N+1 already checked)
```

## 📊 Resource Limits Audit

```
[ ] Request timeout configured? (not infinite)
[ ] Connection pool size set appropriately?
[ ] Thread pool: are there blocking calls on async threads?
[ ] File upload size limit configured?
[ ] Response size limit? (no unbounded streaming)
[ ] Retry with exponential backoff? (not tight loop retry)
[ ] Circuit breaker for external dependencies?
```

## 📝 Performance Report Format

```markdown
## ⚡ Performance Audit

### RNF Compliance
| RNF | Requirement | Measurement | Verdict |
|-----|------------|-------------|---------|
| RNF-PERF-001 | Response < 200ms p99 | 185ms (benchmark) | ✅ PASS |
| RNF-PERF-003 | Memory < 100MB | 95MB (measured) | ✅ PASS |

### N+1 Query Audit
- Endpoint: GET /orders/{id} → 2 queries (Orders + Items) ✅ PASS
- Endpoint: GET /users → 11 queries (N+1 in Posts) 🚨 AUTO-FAIL

### Algorithmic Complexity
- OrderProcessor.CalculateTotals: O(n) where n = items per order ✅ Acceptable
- ReportGenerator.BuildMatrix: O(n²) where n = users × products ⚠️ FLAG for > 1000 users

### Memory & Resources
- [ ] HttpClient: ✅ static/shared
- [ ] Event handlers: ✅ properly unsubscribed
- [ ] IDisposable: ✅ using statements present

### Overall Performance Verdict: [PASS / REWORK]
```
