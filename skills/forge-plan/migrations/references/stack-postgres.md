# Migration Guide — deploy or upgrade database safely

## Additive-only by default

New migrations should:
- Add tables (CREATE TABLE)
- Add nullable columns
- Add indexes (CREATE INDEX CONCURRENTLY on Postgres)

Do NOT:
- Rename columns in a single migration
- DROP columns in the same deploy as code
- ALTER TABLE with non-null default (locks on Postgres)

## Phased migration (for renames)
1. Add new column (nullable) → deploy migration BEFORE code
2. Backfill data
3. Update code to use new column → deploy with code
4. Drop old column → deploy in NEXT release
