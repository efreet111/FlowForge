---
name: forge-arch-domain
description: >
  Specialized Arch Agent skill for Domain-Driven Design — bounded contexts, 
  ubiquitous language, aggregates, domain events. Trigger: feature involves 
  complex business logic, multiple teams, or evolving domain models.
---

# forge-arch-domain — Domain-Driven Design

You are the **DOMAIN ARCH AGENT**. When this skill is loaded, you MUST structure the `spec.md` using DDD principles to ensure the domain model stays consistent and maintainable.

## 🗺️ Bounded Context Identification

For each feature, identify how many bounded contexts it touches:

```
Is this feature limited to ONE domain context?
  [ ] YES → Stay in this context. Don't leak concepts.
  [ ] NO  → Map the interaction between contexts.
```

| Context Interaction | How to handle |
|--------------------|---------------|
| **Cooperation** (context A needs data from context B) | Anti-corruption layer + published language (events/DTOs) |
| **Partnership** (A and B evolve together) | Shared kernel — but keep it small and explicit |
| **Separate Ways** (A and B don't align) | Integration via events, no shared code |
| **Customer-Supplier** (A depends on B) | B provides APIs; A consumes them. B's roadmap affects A. |

### Example: E-commerce
```
┌──────────────────┐     ┌──────────────────┐     ┌──────────────────┐
│   Ordering       │────▶│   Billing        │────▶│   Shipping       │
│ - Orders         │     │ - Invoices       │     │ - Shipments      │
│ - Cart           │     │ - Payments       │     │ - Tracking       │
│ - Discounts      │     │ - Refunds        │     │ - Carriers       │
└──────────────────┘     └──────────────────┘     └──────────────────┘
        │                        │                        │
        │        ┌ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ┐               │
        └────────▶    Inventory Context    ◀───────────────┘
                 │   - Stock              │
                   - Warehouses
                 │   - Suppliers          │
                   └ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ┘
```

## 📖 Ubiquitous Language

Define the domain terms used in the spec:

```
Term: [term name]
Definition: [precise business meaning, not technical]
Same as: [synonyms]
Related to: [other domain terms]
Events produced: [domain events emitted]
```

**In the Capability Matrix, mark ubiquitous language as `deterministic`** — the Dev Agent MUST use these exact terms in code (class names, method names, comments).

## 🧱 Aggregate Design

Each aggregate root MUST follow these rules:

```
Aggregate: [name, e.g., Order]
Root: [OrderId]
  - Invariants: [business rules that must ALWAYS hold]
    1. Order total = sum of line item totals
    2. Order cannot be shipped before payment
    3. Discount cannot exceed 30% of subtotal
  - Entities: [Order, LineItem]
  - Value Objects: [Address, Money, OrderStatus]
  - Domain Events: [OrderPlaced, OrderPaid, OrderShipped]
  - Repository: [IOrderRepository]
```

### Rules for Aggregate Design in spec.md:
1. **Reference by identity**: Aggregates reference each other by ID, not by object reference
2. **One transaction per aggregate**: No cross-aggregate transactions
3. **Eventual consistency**: Between aggregates, use domain events
4. **Keep aggregates small**: If you can't count the entities on one hand, split it

## 📨 Domain Events

For every significant state change, define a domain event:

```markdown
| Event | Producer | Consumers | Payload |
|-------|----------|-----------|---------|
| OrderPlaced | Ordering | Billing (create invoice), Inventory (reserve stock) | OrderId, CustomerId, Total, Items |
| PaymentReceived | Billing | Ordering (mark paid), Shipping (prepare shipment) | OrderId, TransactionId, Amount |
| OrderShipped | Shipping | Ordering (mark shipped), Notification (email customer) | OrderId, TrackingNumber, Carrier |
```

## 🚫 Anti-Patterns to Flag

| Anti-Pattern | Detection | Fix |
|-------------|-----------|-----|
| **Anemic Domain Model** | All entities are data containers (getters/setters only) | Move business logic into domain model methods |
| **Cross-context coupling** | Module A imports Module B's entities directly | Add anti-corruption layer; communicate via events |
| **Technical naming** | Class named `OrderEntityManager` instead of `Order` | Use business language, not technical patterns |
| **God Aggregate** | `User` aggregate has addresses, orders, payments, preferences | Split into bounded contexts |
| **Transaction spanning contexts** | "This must be ACID across the whole feature" | Use eventual consistency + saga pattern |

## 📝 DDD Section in spec.md

```markdown
## 5. Domain Model

### Bounded Contexts
- Ordering Context: Orders, Cart, Discounts
- Billing Context: Invoices, Payments, Refunds

### Ubiquitous Language
| Term | Definition |
|------|------------|
| Order | A request to purchase one or more items. Contains line items, total, and status. |
| Invoice | A bill generated after payment is due. Different from Order — one order can have multiple invoices. |

### Aggregates
- Order (root: OrderId) — invariants: total = sum(items), discount ≤ 30%
- Payment (root: PaymentId) — invariants: amount = invoice total, one attempt per 24h

### Domain Events
- OrderPlaced → [Billing: create invoice, Inventory: reserve stock]
- PaymentSettled → [Ordering: mark paid, Shipping: prepare shipment]
```
