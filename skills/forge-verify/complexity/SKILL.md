---
name: forge-verify-complexity
description: >
  Specialized Verify Agent skill for code complexity audit — cyclomatic 
  complexity, nesting depth, cognitive load. Trigger: when auditing code 
  with conditional logic, loops, or deeply nested structures.
---

# forge-verify-complexity — Complexity Audit

You are the **COMPLEXITY VERIFY AGENT**. When this skill is loaded, you MUST audit the code for excessive complexity that makes it unmaintainable, untestable, and bug-prone. The core `forge-verify` skill handles spec compliance — you handle structural quality.

## 📏 Complexity Metrics

### 1. Cyclomatic Complexity (MCC)

> Counts the number of linearly independent paths through the code.

**Counting rule**: Start at 1. Add 1 for each: `if`, `else if`, `for`, `foreach`, `while`, `case`, `catch`, `&&`, `||`, `? :`.

| Complexity | Risk | Action |
|-----------|------|--------|
| 1-5 | ✅ Low | Clean, simple function |
| 6-10 | ⚠️ Medium | Acceptable but monitor |
| 11-20 | 🔴 High | Flag in rework ticket — refactor |
| 21-50 | 🚨 Very High | Auto-fail verification |
| >50 | 💀 Critical | Untestable — auto-fail, escalate |

### 2. Nesting Depth

> How deeply indented the code is.

| Depth | Risk | Action |
|-------|------|--------|
| 0-2 | ✅ Low | Clean |
| 3-4 | ⚠️ Medium | Borderline — consider early returns |
| 5-6 | 🔴 High | Flag — extract methods or use guard clauses |
| >6 | 🚨 Very High | Auto-fail — unreadable |

### 3. Cognitive Load

> How hard the code is to understand (vs simply being complex).

**Factors that increase cognitive load:**
- Nested conditionals (+1 per level)
- Mismatched abstraction levels (+2)
- Side effects in unexpected places (+2)
- Magic numbers/strings (+1 each)
- Long parameter lists (>3) (+1 per extra param)
- Boolean flag parameters (+2)
- Multiple returns from different nesting levels (+1)
- Switch/case fallthrough (+2)

| Cognitive Load | Risk | Action |
|---------------|------|--------|
| 0-5 | ✅ Low | Easy to understand |
| 6-10 | ⚠️ Medium | Some effort required |
| 11-20 | 🔴 High | Hard — flag for refactor |
| >20 | 🚨 Very High | Auto-fail — incomprehensible |

## 🔍 Code Smell Detection

### Extract Method Candidates
```csharp
// SMELL: Comment blocks indicate extract-method opportunities
public void ProcessOrder(Order order)
{
    // Validate order       ← Extract: ValidateOrder(order)
    if (order == null) throw ...;
    if (order.Items.Count == 0) throw ...;
    
    // Calculate totals      ← Extract: CalculateTotals(order)
    var subtotal = order.Items.Sum(i => i.Price);
    var tax = subtotal * 0.1m;
    var total = subtotal + tax;
    
    // Apply discounts       ← Extract: ApplyDiscounts(order, total)
    if (order.Coupon != null) ...;
    
    // Save order            ← Extract: SaveOrder(order, total)
    db.Orders.Add(order);
    db.SaveChanges();
}
```

### Guard Clause Pattern (reduces nesting)
```csharp
// BAD: Arrow anti-pattern (deep nesting)
public decimal Calculate(Order order) {
    if (order != null) {
        if (order.Items != null) {
            if (order.Items.Count > 0) {
                if (order.Customer != null) {
                    return order.Items.Sum(i => i.Price);
                }
            }
        }
    }
    return 0;
}

// GOOD: Guard clauses (flat)
public decimal Calculate(Order order) {
    if (order == null) return 0;
    if (order.Items == null) return 0;
    if (order.Items.Count == 0) return 0;
    if (order.Customer == null) return 0;
    return order.Items.Sum(i => i.Price);
}
```

### Other Smells to Detect

| Smell | Detection | Fix |
|-------|-----------|-----|
| **Long Method** | > 30 lines | Extract methods by responsibility |
| **Long Parameter List** | > 4 parameters | Create parameter object / DTO |
| **Primitive Obsession** | String for email, int for ID | Create value objects (Email, UserId) |
| **Switch Statement** | switch with > 5 cases on type | Replace with polymorphism or dictionary |
| **Feature Envy** | Method uses another class's data more than its own | Move method to the other class |
| **Data Clumps** | Same 3+ parameters repeated across methods | Extract to a class |
| **Shotgun Surgery** | One change requires modifying many files | Consolidate responsibility |
| **Divergent Change** | One class changes for different reasons | Split by responsibility |

## 📋 Complexity Audit Checklist

For each function in the diff:

```
[ ] Cyclomatic complexity: add up if/for/while/switch/catch/&&/||
[ ] Nesting depth: deepest indentation level
[ ] Cognitive load: magic numbers, side effects, long params
[ ] Lines of code: > 30? Extract methods
[ ] Parameters: > 4? Create parameter object
[ ] Comments as section headers: extract methods
[ ] Arrow anti-pattern: use guard clauses
[ ] Switch on type: use polymorphism
```

## 🚫 Auto-Fail Thresholds

These are immediate rework_ticket.md items:

| Metric | Threshold |
|--------|-----------|
| Cyclomatic complexity | > 20 for any function |
| Nesting depth | > 6 levels |
| Lines per method | > 100 lines |
| Parameters | > 7 parameters |
| Switch cases on type | > 10 cases |

## 📝 Complexity Report Format

Add to your verification report:

```markdown
## 🧠 Complexity Audit

### Top 5 Most Complex Functions
| Function | File | MCC | Nesting | Lines | Verdict |
|----------|------|-----|---------|-------|---------|
| ProcessOrder | OrderService.cs:45 | 18 | 5 | 87 | 🔴 REFACTOR |
| CalculateTax | TaxService.cs:12 | 12 | 4 | 34 | ⚠️ MONITOR |
| ... | ... | ... | ... | ... | ... |

### Smells Detected
- [ ] Long Method: OrderService.ProcessOrder (87 lines) → Extract ValidateOrder, CalculateTotals, ApplyDiscounts
- [ ] Primitive Obsession: string email in 5 files → Create Email value object
- [ ] Feature Envy: PaymentService uses 8 Order properties → Move CalculateTotal to Order

### Overall Complexity: [PASS / REWORK]
- Functions exceeding thresholds: X
- Smells detected: Y
```
