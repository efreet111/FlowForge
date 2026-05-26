---
name: forge-dev-refactor
description: >
  Specialized Dev Agent skill for safe refactoring — Martin Fowler's catalog, 
  code smell detection, and test-preserving transformations. Trigger: during 
  the Ralph Wiggum loop when the code has structural issues or code smells.
---

# forge-dev-refactor — Safe Refactoring Catalog

You are the **REFACTOR DEV AGENT**. When this skill is loaded, you MUST apply safe refactoring patterns BEFORE moving on. **Every refactor must preserve behavior** — tests must still pass after each step.

## ⚠️ First Rule of Refactoring

> **"Refactoring is a change to the internal structure of software to make it easier to understand and cheaper to modify without changing its observable behavior."** — Martin Fowler

**Before you start refactoring:**
1. Ensure tests exist for the code you're changing
2. Run the tests — they MUST pass before you start
3. Refactor in SMALL steps — one transformation at a time
4. Run tests after EACH step — if they fail, you went too far

---

## 🔧 Refactoring Catalog

### 1. Extract Method (Most Common)

| Before | After |
|--------|-------|
| `processOrder()` has 80 lines with 3 distinct blocks | `validateOrder()` + `calculateTotals()` + `saveOrder()` |

```typescript
// BEFORE
function processOrder(order: Order) {
  if (!order.items.length) throw Error("Empty order");
  if (order.total < 0) throw Error("Negative total");
  let subtotal = order.items.reduce((s, i) => s + i.price, 0);
  let tax = subtotal * 0.1;
  order.total = subtotal + tax;
  db.orders.save(order);
}

// AFTER
function processOrder(order: Order) {
  validateOrder(order);
  calculateOrderTotals(order);
  persistOrder(order);
}

function validateOrder(order: Order) {
  if (!order.items.length) throw Error("Empty order");
  if (order.total < 0) throw Error("Negative total");
}

function calculateOrderTotals(order: Order) {
  let subtotal = order.items.reduce((s, i) => s + i.price, 0);
  order.total = subtotal + subtotal * 0.1;
}

function persistOrder(order: Order) {
  db.orders.save(order);
}
```

### 2. Rename Variable/Method

| Smell | Rule | Example |
|-------|------|---------|
| Single-letter name | `t` → `total` | `let t = calc();` |
| Abbreviation | `calc` → `calculateTotal` | `calc()` → `calculateTotal()` |
| Misleading name | `data` → `orderData` | `process(data)` → `process(orderData)` |

```typescript
// BEFORE
let t = calc(o);     // What is t? What is o?

// AFTER
let total = calculateTotal(order);  // Clear intent
```

### 3. Introduce Parameter Object

| Before | After |
|--------|-------|
| `createUser(name, email, phone, address, city, zip)` | `createUser(user: UserDto)` |

```typescript
// BEFORE — 6 parameters, easy to mix up
function createUser(name: string, email: string, phone: string,
                    address: string, city: string, zip: string)

// AFTER — single parameter object
interface CreateUserRequest {
  name: string;
  email: string;
  phone: string;
  address: string;
  city: string;
  zip: string;
}
function createUser(request: CreateUserRequest)
```

### 4. Replace Conditional with Polymorphism

| Before | After |
|--------|-------|
| `switch(type)` with 5+ cases | Each type is its own class with a shared interface |

```typescript
// BEFORE
function calculateShipping(order: Order, carrier: string) {
  switch (carrier) {
    case "fedex": return order.weight * 1.5;
    case "ups": return order.weight * 1.3 + 5;
    case "dhl": return order.weight * 1.8 + 3;
    default: throw new Error("Unknown carrier");
  }
}

// AFTER
interface ShippingCalculator {
  calculate(order: Order): number;
}
class FedExShipping implements ShippingCalculator {
  calculate(order: Order) { return order.weight * 1.5; }
}
class UPSShipping implements ShippingCalculator {
  calculate(order: Order) { return order.weight * 1.3 + 5; }
}
```

### 5. Decompose Conditional

| Before | After |
|--------|-------|
| Nested if/else with complex conditions | Guard clauses + extracted condition methods |

```typescript
// BEFORE — nested, hard to read
function applyDiscount(order: Order) {
  if (order.customer) {
    if (order.customer.isPremium) {
      if (order.total > 100) {
        order.discount = order.total * 0.2;
      }
    }
  }
}

// AFTER — flat, intent-revealing
function applyDiscount(order: Order) {
  if (!order.customer) return;
  if (!order.customer.isPremium) return;
  if (order.total <= 100) return;
  order.discount = order.total * 0.2;
}
```

### 6. Replace Magic Number with Constant

```typescript
// BEFORE
if (order.total > 100) { ... }    // What is 100? Why 100?

// AFTER
const FREE_SHIPPING_THRESHOLD = 100;
if (order.total > FREE_SHIPPING_THRESHOLD) { ... }
```

### 7. Separate Query from Modifier

```typescript
// BEFORE — command and query mixed
function saveAndGetTotal(order: Order): number {
  db.orders.save(order);          // Command (side effect)
  return calculateTotal(order);   // Query
}

// AFTER — separated
function saveOrder(order: Order): void {   // Command only
  db.orders.save(order);
}
function getOrderTotal(order: Order): number {  // Query only
  return calculateTotal(order);
}
```

## 🧪 Refactoring Workflow (Preserving Tests)

```
1. [ ] Write tests for the code (if none exist)
2. [ ] Run tests → ALL GREEN
3. [ ] Apply ONE refactoring transformation
4. [ ] Run tests → should still ALL GREEN
5. [ ] Repeat steps 3-4 for each transformation
6. [ ] If tests fail → undo last change (git checkout)
7. [ ] All done? Commit refactoring separately from feature code
```

**Never combine refactoring and feature changes in the same commit.**

## 🚫 Code Smells to Flag During Ralph Wiggum

| Smell | Detection | Refactoring |
|-------|-----------|-------------|
| Long Method (> 30 lines) | Count lines | Extract Method |
| Large Class (> 200 lines) | Count lines | Extract Class |
| Long Parameter List (> 4) | Count params | Introduce Parameter Object |
| Duplicated Code | Copy-paste blocks (≥ 3 lines repeated 3+ times) | Extract Method + Template Method |
| Switch Statement | switch/if-else-chain on type | Replace Conditional with Polymorphism |
| Feature Envy | Method uses another class' getters > its own | Move Method |
| Message Chain | `a.b().c().d()` | Hide Delegate |
| Primitive Obsession | `string email`, `int id` | Replace with Value Object |
| Shotgun Surgery | One change requires 5+ files | Move Method + Inline Class |
| Speculative Generality | Interface with 1 implementation | Collapse Hierarchy |

## 📝 Ralph Wiggum + Refactoring

During the Ralph Wiggum loop, AFTER the feature compiles and tests pass:

```
1. ✅ Feature compiles
2. ✅ Tests pass (unit + integration)
3. 🔧 Run code smell scan (mental: scan for the smells above)
4. 🔧 If smells found: apply safe refactoring ONE STEP AT A TIME
5. ✅ After each step: run tests again
6. ✅ If tests still pass → next refactor or done
7. 🚨 If tests fail → git checkout and try smaller step
8. 📝 Commit message: "refactor: [smell resolved] in [class/method]"
```
