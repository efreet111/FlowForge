# Backlog — PostgreSQL idx_obs_dedupe Index Overflow (Sync Push Blocked)

> **Status:** Proposed (fix not implemented)  
> **Priority:** P0 — Critical (sync push fails silently, data loss)  
> **Created:** 2026-07-24  
> **IDs:** NS-10 · engram-dotnet ENG-475  
> **Related:** [engram-dotnet ticket](../../../engram-dotnet/.ai-work/eng-475-postgres-dedupe-index-overflow/ticket.md) ·
> [ENG-474 Obsidian Memory Graph](../../../engram-dotnet/docs/BACKLOG.md) ·
> [06-engram-sync-convention](../06-engram-sync-convention.md)

---

## User story

**As a** developer using engram-dotnet with sync enabled (multi-device or team setup),  
**I want** my memories to sync to the PostgreSQL server without failing on long observations,  
**so that** my knowledge base is consistent across devices and I don't lose data silently.

---

## Problem (evidence 2026-07-24)

### Symptoms

1. **Sync push fails with HTTP 500** when observations have content >2000 bytes.
2. **Error message:** `PostgresException: 54000: index row size 2800 exceeds btree version 4 maximum 2704 for index "idx_obs_dedupe"`
3. **Impact:** 48 mutations blocked from syncing (13 observations with long content).
4. **Silent data loss:** User believes sync is working, but memories never reach the server.

### Root cause

The `idx_obs_dedupe` index in `PostgresStore.cs:99` is a composite B-tree index:

```sql
CREATE INDEX IF NOT EXISTS idx_obs_dedupe 
ON observations(normalized_hash, project, scope, type, title, created_at DESC) 
WHERE normalized_hash IS NOT NULL;
```

**PostgreSQL B-tree limitation:** Version 4 cannot index rows larger than 2704 bytes (1/3 of 8KB buffer page).

When `title` + `project` + `type` + other fields exceed ~2600 bytes, the index insertion fails.

### Stack trace (from server logs)

```
PostgresException: 54000: index row size 2800 exceeds btree version 4 maximum 2704 for index "idx_obs_dedupe"
   at Engram.Store.PostgresStore.ApplyObservationUpsertAsync(...) in PostgresStore.cs:line 2525
   at Engram.Store.PostgresStore.InsertMutationBatchAsync(...) in PostgresStore.cs:line 1899
   at Engram.Server.CloudSyncEndpoints.HandleMutationPushAsync(...) in CloudSyncEndpoints.cs:line 191
```

### Affected observations (sample)

| seq | entity_key | project | title_len | content_len |
|-----|------------|---------|-----------|-------------|
| 227086 | obs-75cfd055e9fcf4db | team/engram-dotnet | 62 | 8287 |
| 227082 | obs-17d0490c2f3bdee9 | team/flowforge | 31 | 6349 |
| 227078 | obs-f3b5a51de37f1c6e | team/flowforge | 67 | 4861 |
| 227072 | obs-120b1c11167d2ef4 | team/engram-dotnet | 35 | 4325 |
| 227092 | obs-ecf62c9f9cb648b9 | team/flowforge | 79 | 3527 |

**Pattern:** Observations with detailed content (architecture decisions, bug analyses, session summaries) exceed the index limit.

---

## Why this matters for memory curation

When agents save memories via `mem_save` or `mem_capture_passive`, they often include:
- **Detailed context** (file paths, code snippets, error messages)
- **Long titles** (descriptive summaries of decisions or bugs)
- **Structured content** (markdown with tables, lists, code blocks)

This is **good behavior** — rich memories are more useful. But the current index design penalizes detailed memories.

**Paradox:** The more valuable the memory, the more likely it is to fail sync.

---

## Proposed fix (Option A — recommended)

Change the index to use `md5(normalized_hash)` instead of the raw hash:

```sql
-- Migration SQL (run on PostgreSQL server)
DROP INDEX IF EXISTS idx_obs_dedupe;
CREATE INDEX idx_obs_dedupe 
ON observations(md5(normalized_hash), project, scope, type, created_at DESC) 
WHERE normalized_hash IS NOT NULL;
```

**Why this works:**
- `md5()` produces a fixed 32-character string, regardless of input size
- Deduplication still works (same hash → same md5)
- Index row size stays well under 2704 bytes

**Trade-offs:**
- Requires migration on existing PostgreSQL databases
- Slight performance overhead for md5() computation (negligible)

---

## Alternative fixes considered

### Option B: Remove `title` from index

```sql
DROP INDEX IF EXISTS idx_obs_dedupe;
CREATE INDEX idx_obs_dedupe 
ON observations(normalized_hash, project, scope, type, created_at DESC) 
WHERE normalized_hash IS NOT NULL;
```

**Pros:** Simple, removes the variable-length field.  
**Cons:** If dedup logic searches by `title`, it may break. Need to verify.

### Option C: Truncate `title` before insert

Modify `ApplyObservationUpsertAsync` to truncate `title` to 200 chars.

**Pros:** No DB migration needed.  
**Cons:** Loses information, doesn't fix existing data.

### Option D: Minimal index (hash only)

```sql
DROP INDEX IF EXISTS idx_obs_dedupe;
CREATE INDEX idx_obs_dedupe 
ON observations(normalized_hash) 
WHERE normalized_hash IS NOT NULL;
```

**Pros:** Smallest possible index, never exceeds limit.  
**Cons:** Loses ability to filter by project/scope/type in dedup queries. May cause false positives.

---

## Acceptance criteria

- [ ] AC-1: Observations with content >5000 bytes sync successfully to PostgreSQL.
- [ ] AC-2: Deduplication still works (same content → same hash → no duplicates).
- [ ] AC-3: Existing databases can migrate without data loss.
- [ ] AC-4: No performance regression in sync push/pull operations.
- [ ] AC-5: `idx_obs_dedupe` definition in `PostgresStore.cs` uses a strategy that cannot exceed 2704 bytes.
- [ ] AC-6: Integration test verifies sync of observations with 10KB+ content.

---

## Work breakdown (for implementing agent)

### A — engram-dotnet (code fix)

1. **Update `PostgresStore.cs:99`** — change index definition to use `md5(normalized_hash)` or remove `title`.
2. **Verify dedup logic** — ensure `ApplyObservationUpsertAsync` still works with the new index.
3. **Add integration test** — `SyncPush_LargeObservation_Succeeds` with 10KB content.
4. **Update migration** — add idempotent migration to drop/recreate index.

### B — PostgreSQL server (migration)

1. **Backup database** before migration.
2. **Execute migration SQL:**
   ```sql
   DROP INDEX IF EXISTS idx_obs_dedupe;
   CREATE INDEX idx_obs_dedupe 
   ON observations(md5(normalized_hash), project, scope, type, created_at DESC) 
   WHERE normalized_hash IS NOT NULL;
   ```
3. **Verify index size** — `pg_indexes` should show new definition.
4. **Test sync** — push a large observation manually.

### C — Documentation

1. **Update ADR** — document the decision to use md5() for dedup index.
2. **Update CHANGELOG** — note the fix for ENG-475.
3. **Update troubleshooting guide** — add section on "sync fails with index overflow".

---

## Workaround (applied 2026-07-24)

Mark affected observations as `acked_at` in the local SQLite DB to unblock sync:

```bash
sqlite3 ~/.engram/engram.db "UPDATE sync_mutations SET acked_at = datetime('now') WHERE seq IN (227086, 227082, 227078, 227072, 227092, 227096, 227048, 227090, 227094, 227074, 227076, 227088, 227080);"
```

**Note:** This loses sync of those 13 observations with the server. They can be re-synced after the fix is applied.

---

## Related issues

- **ENG-474** (Obsidian Memory Graph) — discovered this bug during sync verification.
- **ENG-459** (Sync failure feedback) — this bug highlights the need for better error visibility.
- **NS-09** (Engram MCP Anthropic dependency) — another sync/install issue discovered in the field.

---

## Memory signal (for agents loading this ticket)

**Key insight:** PostgreSQL B-tree indexes have a 2704-byte limit. Composite indexes with variable-length fields (like `title`) can exceed this limit when content is large.

**Lesson learned:** When designing indexes for deduplication, use fixed-size fields (hashes, IDs) rather than variable-length content. If you must include variable fields, use a hash function (md5, sha256) to normalize size.

**Pattern to avoid:** `CREATE INDEX ON (hash, project, scope, type, title, timestamp)` — `title` can be arbitrarily long.

**Pattern to use:** `CREATE INDEX ON (md5(hash), project, scope, type, timestamp)` — all fields are bounded.

---

## References

- [PostgreSQL B-tree limitations](https://www.postgresql.org/docs/current/btree.html)
- [engram-dotnet ENG-475 ticket](../../../engram-dotnet/.ai-work/eng-475-postgres-dedupe-index-overflow/ticket.md)
- [PostgresStore.cs:99](../../../engram-dotnet/src/Engram.Store/PostgresStore.cs) — index definition
- [PostgresStore.cs:2525](../../../engram-dotnet/src/Engram.Store/PostgresStore.cs) — ApplyObservationUpsertAsync (failure point)
