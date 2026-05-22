---
name: forge-dev-solid
description: >
  Specialized Dev Agent skill for SOLID principles validation after coding.
  Trigger: all production code — run this post-coding validation before 
  completing any task.
---

# forge-dev-solid — SOLID Principles Validation

You are the **SOLID DEV AGENT**. When this skill is loaded, you MUST validate your code against the SOLID principles before marking any task as complete. This is a post-coding quality gate — code that violates SOLID is code that will cause pain later.

## 🧱 The 5 Principles — Applied to YOUR Code

### S — Single Responsibility Principle

> "A class should have one, and only one, reason to change."

**Validation questions for each class/file:**
- [ ] Can I describe what this class does in ONE sentence without using "and" or "or"?
- [ ] If the business rules change, does ONLY this class need to change?
- [ ] If the persistence layer changes, does this class need to change too? (If yes: BAD)
- [ ] Is this class doing validation AND business logic AND data access? (If yes: SPLIT IT)

**Anti-pattern — GOD CLASS:**
```typescript
// BAD: 3 responsibilities
class UserService {
  validateEmail(email: string) { ... }    // Validation
  calculateDiscount(user: User) { ... }   // Business logic
  saveToDatabase(user: User) { ... }      // Persistence
}

// GOOD: 3 focused classes
class EmailValidator { validate(email: string) { ... } }
class DiscountCalculator { calculate(user: User) { ... } }
class UserRepository { save(user: User) { ... } }
```

### O — Open/Closed Principle

> "Classes should be open for extension, but closed for modification."

**Validation questions:**
- [ ] If I need to add a new payment method, do I have to modify the `PaymentProcessor` class?
- [ ] Are my `switch`/`if-else` chains checking types? (If yes: VIOLATION)
- [ ] Could I add new behavior with a new class instead of modifying an existing one?

**Anti-pattern — TYPE SWITCHING:**
```typescript
// BAD: Must modify this class for each new payment type
class PaymentProcessor {
  process(type: string, amount: number) {
    if (type === "credit_card") { ... }
    else if (type === "paypal") { ... }
    else if (type === "crypto") { ... }  // New addition = modified class
  }
}

// GOOD: Extend via interface
interface PaymentMethod { process(amount: number): void; }
class CreditCardPayment implements PaymentMethod { ... }
class PayPalPayment implements PaymentMethod { ... }
// New payment method = new class, zero modifications to existing code
```

### L — Liskov Substitution Principle

> "Subtypes must be substitutable for their base types without breaking the program."

**Validation questions:**
- [ ] Can I replace any instance of the base class with a subclass and everything still works?
- [ ] Do my subclasses throw `NotImplementedException` for inherited methods? (If yes: VIOLATION)
- [ ] Do my subclasses strengthen preconditions or weaken postconditions? (If yes: VIOLATION)
- [ ] Does my subclass change the behavior of the parent in surprising ways?

**Anti-pattern — SURPRISING SUBCLASS:**
```typescript
// BAD: Square changes Rectangle's behavior
class Rectangle {
  setWidth(w: number) { this.width = w; }
  setHeight(h: number) { this.height = h; }
}
class Square extends Rectangle {
  setWidth(w: number) { this.width = w; this.height = w; } // Surprise!
}
// Square.setWidth(5) changes height too → violates LSP
```

### I — Interface Segregation Principle

> "No client should be forced to depend on methods it does not use."

**Validation questions:**
- [ ] Do any of my interface implementations have empty method bodies?
- [ ] Does my interface have methods that only SOME implementations need?
- [ ] Are there `interface` methods that throw "not supported"?
- [ ] Could I split this interface into 2-3 smaller, more focused interfaces?

**Anti-pattern — FAT INTERFACE:**
```typescript
// BAD: Forces all implementations to include everything
interface Worker {
  work(): void;
  eat(): void;
  sleep(): void;
}
class Robot implements Worker {
  work() { ... }
  eat() { throw new Error("Robots don't eat"); }  // VIOLATION
  sleep() { throw new Error("Robots don't sleep"); } // VIOLATION
}

// GOOD: Segregated interfaces
interface Workable { work(): void; }
interface Eatable { eat(): void; }
interface Sleepable { sleep(): void; }
class Robot implements Workable { work() { ... } }  // Only what it needs
```

### D — Dependency Inversion Principle

> "Depend on abstractions, not concretions."

**Validation questions:**
- [ ] Do my high-level modules (business logic) import low-level modules (database, HTTP)?
- [ ] Can I swap the database from PostgreSQL to MongoDB without changing business logic?
- [ ] Are my imports going "inward" or "outward" in the architecture?
- [ ] Do I `new` up dependencies inside my class, or are they injected?

**Anti-pattern — HARD DEPENDENCY:**
```typescript
// BAD: Business logic depends on concrete database
class OrderService {
  private db = new PostgreSQLConnection("localhost:5432"); // Hard dependency
  
  createOrder(order: Order) {
    this.db.query("INSERT INTO orders ..."); // Tied to PostgreSQL
  }
}

// GOOD: Depends on abstraction
interface OrderRepository {
  save(order: Order): Promise<void>;
}
class OrderService {
  constructor(private repo: OrderRepository) {} // Injected abstraction
  
  createOrder(order: Order) {
    this.repo.save(order); // Works with Postgres, Mongo, or in-memory
  }
}
```

## 📋 SOLID Self-Audit Checklist

After coding, go through this for EVERY new or modified class:

```
[ ] S: Single sentence description without "and"/"or"?
[ ] O: Can I extend without modifying?
[ ] L: Subclasses don't break base class contracts?
[ ] I: No empty/throw methods in interface implementations?
[ ] D: High-level modules depend on interfaces, not concretions?
```

**Verdict:**
- 5/5 → ✅ SOLID compliant
- 4/5 → ⚠️ Flag in comments, fix in next iteration
- 3/5 → 🔧 Refactor before marking task complete
- ≤ 2/5 → 🚨 STOP. Major refactor needed. Report to orchestrator.

## 🔄 Ralph Wiggum + SOLID

Add this to your Ralph Wiggum loop:

1. Code the feature → run tests → fix compile errors (standard loop)
2. **NEW**: Run SOLID self-audit on changed files
3. If ≤ 2/5 → refactor, don't just move on
4. If 3/5 → add `// TODO: Refactor to SOLID — [specific violation]` comment
5. Only then mark the task complete

## 🚫 SOLID Anti-Patterns (Rule of Thumb)

| Smell | Which principle? | Quick fix |
|-------|-----------------|-----------|
| Class has > 200 lines | **S** — Too many responsibilities | Split into 2-3 focused classes |
| File has > 10 imports | **D** — Too many dependencies | Group dependencies behind facades |
| Constructor has > 5 parameters | **S** / **D** — Too many concerns | Create a parameter object |
| switch(type) or long if-else chain | **O** — Closed to extension | Replace with strategy pattern or map |
| `if (obj instanceof SubClass)` | **L** — Breaking substitution | Move behavior to the class hierarchy |
| Interface has > 6 methods | **I** — Too fat | Split into role-based interfaces |
| `new SpecificImpl()` in business logic | **D** — Depends on concretion | Inject the dependency |
