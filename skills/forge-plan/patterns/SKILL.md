---
name: forge-plan-patterns
description: >
  Specialized Plan Agent skill for design pattern selection and architecture 
  decisions. Trigger: when the spec requires structural decisions, reusable 
  components, or cross-cutting concerns.
---

# forge-plan-patterns — Design Pattern Catalog

You are the **PATTERN PLAN AGENT**. When this skill is loaded, you MUST inject appropriate design patterns into the `plan.md` instead of reinventing solutions. The core `forge-plan` skill handles task ordering — you handle pattern selection.

## 🏗️ Pattern Selection Decision Tree

```
Is this a creational problem? (object creation complexity)
  ├── Single instance needed globally?              → Singleton
  ├── Family of related objects?                     → Abstract Factory
  ├── Complex object with many optional params?      → Builder
  └── Clone existing objects?                        → Prototype

Is this a structural problem? (how objects relate)
  ├── Incompatible interfaces need to work together? → Adapter
  ├── Need to add behavior without modifying class?  → Decorator
  ├── Complex subsystem needs simple interface?      → Facade
  ├── Many objects, need shared state?               → Flyweight
  └── One-to-many dependency, notifications?         → Observer

Is this a behavioral problem? (how objects communicate)
  ├── Algorithm varies, select at runtime?           → Strategy
  ├── Request handled by chain of handlers?          → Chain of Responsibility
  ├── Undo/redo or state machine needed?             → Command / State
  ├── Traverse collection without exposing internals?→ Iterator
  └── Decouple sender from receiver?                 → Mediator
```

## 📐 Patterns by Layer (Where to Apply in plan.md)

### Controller / API Layer
| Pattern | When to use | Plan task example |
|---------|------------|-------------------|
| **Command** | Every action is a command object (CQRS) | `[ ] Create CreateOrderCommand with handler` |
| **Mediator** | Decouple controllers from services | `[ ] Inject IMediator in OrderController` |
| **DTO** | Separate API contract from domain model | `[ ] Create OrderRequest and OrderResponse DTOs` |
| **Factory Method** | Different response types per client | `[ ] ResponseFactory: builds JSON, XML, or CSV response` |

### Service / Business Logic Layer
| Pattern | When to use | Plan task example |
|---------|------------|-------------------|
| **Strategy** | Multiple algorithms for same task (payment, shipping, tax) | `[ ] Define ITaxCalculator + implementations per region` |
| **Chain of Responsibility** | Sequential validation/processing pipeline | `[ ] Build OrderValidationPipeline: Stock → Credit → Fraud` |
| **Specification** | Complex business rules combinable | `[ ] Create ISpecification<Order> for composable rules` |
| **Template Method** | Fixed algorithm steps, variable details | `[ ] BaseReportGenerator with abstract FormatData()` |

### Data / Repository Layer
| Pattern | When to use | Plan task example |
|---------|------------|-------------------|
| **Repository** | Abstract data access behind interface | `[ ] Create IOrderRepository with Save/Get/Delete` |
| **Unit of Work** | Coordinate multiple repositories in transaction | `[ ] Implement UnitOfWork for atomic operations` |
| **Lazy Loading** | Defer expensive data loading | `[ ] Configure EF Core lazy loading for Order.Items` |
| **Identity Map** | Cache loaded entities within request | `[ ] Use DbContext as identity map per request scope` |

### Infrastructure / Cross-Cutting Layer
| Pattern | When to use | Plan task example |
|---------|------------|-------------------|
| **Dependency Injection** | Always — wire dependencies | `[ ] Register all services in DI container` |
| **Decorator** | Add logging, caching, retry without modifying original | `[ ] Create CachingOrderRepository decorator` |
| **Adapter** | Integrate third-party API with different interface | `[ ] Create PaymentGatewayAdapter for Stripe` |
| **Facade** | Simplify complex subsystem (multiple services) | `[ ] Create OrderFacade orchestrating inventory + payment + shipping` |

## 🏛️ Enterprise Patterns (For complex systems)

| Pattern | Use case | Example |
|---------|----------|---------|
| **CQRS** | Separate read and write models | `[ ] Create OrderQueryService (read) and OrderCommandHandler (write)` |
| **Event Sourcing** | Track every state change as event | `[ ] Publish OrderPlaced event; rebuild state from event stream` |
| **Saga** | Distributed transaction across services | `[ ] Implement OrderSaga: create → reserve stock → charge payment` |
| **Outbox Pattern** | Reliable event publishing | `[ ] Store events in outbox table; process with background worker` |
| **Circuit Breaker** | Prevent cascading failures | `[ ] Add Polly circuit breaker on PaymentService calls` |
| **Bulkhead** | Isolate failures in thread pools | `[ ] Separate thread pool for payment processing` |

## 🚫 Anti-Patterns to Flag in Plans

| Anti-Pattern | Detection | Fix |
|-------------|-----------|-----|
| **God Object** | One class listed for business logic + validation + data + notifications | Split into Strategy + Validator + Repository |
| **Spaghetti Architecture** | No layers defined, everything in one directory | Apply layered architecture (Controller → Service → Repository) |
| **Reinventing the Wheel** | Custom solution for solved problems (auth, logging, caching) | Use established library (JWT, Serilog, Redis) |
| **Premature Abstraction** | Interface with ONE implementation "just in case" | Remove interface; add only when second implementation exists |
| **Magic Dependency** | `new ConcreteService()` in business logic | Inject via constructor; register in DI |
| **Anemic Domain** | Models with only getters/setters, logic elsewhere | Move business rules into domain model methods |

## 🚀 Cloud-Native Patterns (For distributed systems)

| Pattern | Problem it solves |
|---------|------------------|
| **12-Factor App** | Configuration via environment, stateless processes |
| **Sidecar** | Deploy helper (proxy, logger) alongside main service |
| **Ambassador** | Offload cross-cutting concerns (retry, auth) to proxy |
| **Strangler Fig** | Gradually replace legacy system with new one |
| **Backend for Frontend (BFF)** | Separate API gateway per client type (web, mobile) |

## 📋 Pattern Annotation in plan.md

When you select a pattern, annotate the task:
```
[ ] [PATTERN: Repository] Create IOrderRepository interface at src/Data/IOrderRepository.cs
[ ] [PATTERN: Strategy] Implement IPaymentProcessor with CreditCard and PayPal strategies
```

This makes the Verify Agent's traceability audit more effective — patterns are explicit design decisions.
