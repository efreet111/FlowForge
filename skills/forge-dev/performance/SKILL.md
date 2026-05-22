---
name: forge-dev-performance
description: >
  Specialized Dev Agent skill for performance optimization during coding.
  Trigger: code accesses databases, external APIs, processes large datasets,
  or is in a hot path.
---

# forge-dev-performance — Performance Optimization

You are the **PERFORMANCE DEV AGENT**. When this skill is loaded, you MUST prevent performance anti-patterns BEFORE they reach the Verify phase. The core `forge-dev` skill handles correctness — you handle efficiency.

## 🔥 N+1 Query Detection (The #1 Performance Killer)

### Detection: Every time you see this pattern, ALARM BELLS:
```
for each (item in collection) {
    db.query("SELECT * FROM related WHERE parent_id = ?", item.id)
}
```

### The fix is ALWAYS:

```csharp
// BAD: N+1 — 1 query for orders + N queries for items
var orders = db.Orders.ToList();
foreach (var order in orders) {
    var items = db.Items.Where(i => i.OrderId == order.Id).ToList(); // QUERY PER ORDER
}

// GOOD: Eager loading — 1 query total
var orders = db.Orders.Include(o => o.Items).ToList();

// ALSO GOOD: Batch query — 2 queries total
var orderIds = orders.Select(o => o.Id).ToList();
var allItems = db.Items.Where(i => orderIds.Contains(i.OrderId)).ToList();
```

### Language-specific patterns:

```typescript
// TypeScript/Prisma: N+1
const users = await prisma.user.findMany();
for (const user of users) {
  const posts = await prisma.post.findMany({ where: { authorId: user.id } }); // BAD
}

// Fix: include relation
const users = await prisma.user.findMany({ include: { posts: true } }); // GOOD
```

```python
# Python/SQLAlchemy: N+1
users = session.query(User).all()
for user in users:
    posts = user.posts  # BAD: lazy load per user

# Fix: joinedload
users = session.query(User).options(joinedload(User.posts)).all()  # GOOD
```

## ⚡ Caching Patterns

### When to cache:
| Data | TTL | Strategy |
|------|-----|----------|
| Reference data (countries, categories) | 24h | Cache-Aside |
| User session | 15 min | Write-Through |
| Computed results (reports) | 1h | Cache-Aside |
| API responses (third-party) | 5-30 min | Cache-Aside with stale-while-revalidate |
| Configuration | On change | Write-Through |

### Cache-Aside (most common):
```
1. Check cache → if hit, return
2. If miss, fetch from source
3. Store in cache with TTL
4. Return result
```

### Implementation checklist:
```
[ ] Cache key strategy defined (user:{id}, product:{slug})
[ ] TTL appropriate for data freshness requirements
[ ] Cache invalidation on data mutation
[ ] Stale-while-revalidate for hot data
[ ] Cache stampede protection (lock on miss)
[ ] Memory limit configured (LRU eviction)
[ ] No sensitive data in cache keys or values
```

## 📦 Batching & Bulk Operations

### Anti-pattern: Individual inserts
```csharp
// BAD: N round trips
foreach (var item in items) {
    db.Items.Add(item);
    db.SaveChanges(); // DB call per item
}

// GOOD: Single batch
db.Items.AddRange(items);
db.SaveChanges(); // One DB call
```

### Batching rules:
```
[ ] Bulk insert: AddRange() / insertMany() instead of loop
[ ] Bulk update: UPDATE ... WHERE id IN (...) instead of loop
[ ] Bulk delete: DELETE ... WHERE id IN (...) instead of loop
[ ] API calls: batch endpoints or use Promise.all() / Task.WhenAll()
```

## 🏃 Lazy vs Eager Loading Decision

| Factor | Lazy Loading | Eager Loading |
|--------|-------------|---------------|
| Access pattern | Rarely access related data | Always access related data |
| Data size | Large related collections | Small related collections |
| Network | Fine for local DB | Prefer for remote APIs |
| Memory | Lower memory footprint | Higher memory footprint |

### Rule of thumb:
```
IF related data is accessed > 50% of the time → EAGER
IF related data is accessed < 10% of the time → LAZY
IF 10-50% → Measure both; choose the faster one
```

## 🚫 Performance Anti-Patterns

| Anti-Pattern | Detection | Fix |
|-------------|-----------|-----|
| **SELECT *** | `SELECT * FROM` in query | Select only needed columns |
| **Missing index** | Query scans without WHERE on indexed column | Add index; verify with EXPLAIN |
| **Cartesian product** | JOIN without ON clause | Every JOIN must have ON |
| **RBAR** (Row By Agonizing Row) | Loop with individual DB calls | Use set-based operations |
| **Chatty I/O** | Multiple small reads/writes | Buffer and batch |
| **Unbounded queries** | No LIMIT/TOP on SELECT | Always limit result sets |
| **Synchronous blocking** | `.Result` or `.Wait()` on async | Use `await` throughout |
| **String concatenation in loop** | `+=` in a loop building large strings | Use StringBuilder / StringBuffer |
| **Reflection in hot path** | `typeof()`, `GetType()` in loops | Cache reflection results |
| **Logging in hot loop** | `logger.Info()` inside tight loop | Use sampling or structured logging levels |

## 🎯 Hot Path Optimization Checklist

For code in request handlers, event processors, or tight loops:

```
[ ] No allocations in the hot path (reuse buffers, use ArrayPool)
[ ] No exceptions for control flow (try-catch is expensive)
[ ] No LINQ in tight loops (use for/foreach)
[ ] No async in tight loops (async has overhead; use sync if fast)
[ ] No reflection (cache Type/MethodInfo at startup)
[ ] No boxing/unboxing (use generics, not object)
[ ] String comparisons: Ordinal, not Culture-sensitive
[ ] Collections: pre-size if known (new List<T>(expectedCount))
[ ] Database: use compiled queries or stored procedures
[ ] JSON: use source generators (System.Text.Json) not reflection-based serializers
```

## 🔧 Performance Tooling

Always include these in your Ralph Wiggum loop for performance-sensitive code:

```bash
# .NET
dotnet-counters monitor  # Live performance counters
dotnet-trace collect     # Detailed trace for analysis

# Node.js
node --inspect --prof app.js  # CPU profiling
clinic doctor -- node app.js  # Auto-diagnose performance issues

# Database
EXPLAIN ANALYZE SELECT ...    # Check query plan
```

**Before marking task complete for DB-heavy code**: Run EXPLAIN on every new query.
