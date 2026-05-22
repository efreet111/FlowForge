---
name: forge-dev-testing
description: >
  Specialized Dev Agent skill for advanced testing techniques beyond unit tests.
  Trigger: when business logic is complex, critical, or the spec has edge-case 
  scenarios.
---

# forge-dev-testing — Advanced Testing Patterns

You are the **TESTING DEV AGENT**. When this skill is loaded, you MUST go beyond basic "happy path" unit tests. The core `forge-dev` skill requires one test per Given-When-Then — you add depth. Test the edges, not just the center.

## 🧪 Testing Pyramid (Where Your Time Goes)

```
         ╱ E2E ╲          ← 5%  — Critical user journeys only
        ╱─────────╲
       ╱ Integration ╲    ← 15% — API contracts, DB, external services
      ╱───────────────╲
     ╱    Unit Tests    ╲  ← 80% — Business logic, validation, edge cases
    ╱─────────────────────╲
```

## 🎯 Property-Based Testing (The Most Underused Technique)

Instead of testing specific inputs, test that PROPERTIES always hold true.

### When to use property-based tests:
- Parsing/encoding: `decode(encode(x)) == x`
- Sorting: output is ordered, contains same elements
- Math operations: `add(x, y) == add(y, x)`, `add(x, 0) == x`
- Serialization: `deserialize(serialize(obj))` is equivalent to `obj`
- String operations: `trim(toUpper(s)) == toUpper(trim(s))`

### Examples by language:

```csharp
// .NET (FsCheck + xUnit)
[Property]
public bool RoundTrip_EncodeDecode(string input)
{
    var encoded = Encoder.Encode(input);
    var decoded = Encoder.Decode(encoded);
    return decoded == input;
}

[Property]
public bool Sorting_MaintainsLength(int[] input)
{
    var sorted = input.OrderBy(x => x).ToArray();
    return sorted.Length == input.Length;
}
```

```typescript
// JavaScript/TypeScript (fast-check + Jest)
test.prop([fc.string()])('roundtrip: decode(encode(x)) == x', (input) => {
  const encoded = encode(input);
  const decoded = decode(encoded);
  return decoded === input;
});
```

## 🐛 Fuzzing — Feed It Garbage

For every function that accepts external input, add a fuzz test:

```csharp
// .NET
[Theory]
[InlineData(null)]
[InlineData("")]
[InlineData("   ")]
[InlineData("a")]
[InlineData(new string('a', 10000))]  // Max length
[InlineData("<script>alert('xss')</script>")]
[InlineData("'; DROP TABLE Users; --")]
[InlineData("../../../etc/passwd")]
[InlineData("\0\0\0")]
[InlineData("😀😀😀😀😀")]
[InlineData("ℝℂℕℚ")]
public void ValidateEmail_RejectsInvalid(string input)
{
    Assert.False(EmailValidator.IsValid(input));
}
```

### Fuzzing checklist per function:
```
[ ] null / undefined / empty string
[ ] Whitespace only
[ ] Single character
[ ] Maximum length input
[ ] SQL injection patterns ('; DROP TABLE; --)
[ ] XSS patterns (<script>, javascript:, onerror=)
[ ] Path traversal (../../../etc/passwd)
[ ] Null bytes (\0)
[ ] Unicode edge cases (emojis, RTL markers, homoglyphs)
[ ] Negative numbers (if numeric)
[ ] Zero (if numeric)
[ ] Extremely large numbers (Int64.MaxValue)
[ ] Floats: NaN, Infinity, -Infinity
```

## 🧬 Mutation Testing — Test Your Tests

**Principle**: If you change your production code (mutate it) and NO test fails, your tests are weak.

### Mental mutation testing (no tool needed):
```
For each conditional in your code:
  1. Change > to >= — does a test fail?
  2. Change && to || — does a test fail?
  3. Remove a statement — does a test fail?
  4. Return a constant — does a test fail?
  5. Swap then/else branches — does a test fail?
```

### Example:
```csharp
// Production code
public decimal CalculateDiscount(Order order)
{
    if (order.Total > 100)          // Mutation: change > to >=
        return order.Total * 0.1m;  // Mutation: change 0.1 to 0.05
    return 0;                       // Mutation: return 5
}

// Your tests MUST catch these mutations:
[Fact]
public void CalculateDiscount_Exactly100_ReturnsZero()  // Catches > to >=
{
    var order = new Order { Total = 100 };
    Assert.Equal(0, new DiscountCalculator().CalculateDiscount(order));
}

[Fact]
public void CalculateDiscount_Over100_Returns10Percent() // Catches rate change
{
    var order = new Order { Total = 200 };
    Assert.Equal(20, new DiscountCalculator().CalculateDiscount(order));
}
```

## 🎭 Test Behavior, Not Implementation

### BAD (tests implementation):
```csharp
[Fact]
public void CreateOrder_CallsSaveMethod()
{
    var mockRepo = new Mock<IOrderRepository>();
    mockRepo.Verify(r => r.Save(It.IsAny<Order>()), Times.Once); // Fragile!
}
```

### GOOD (tests behavior):
```csharp
[Fact]
public void CreateOrder_ValidOrder_OrderPersisted()
{
    var repo = new InMemoryOrderRepository();
    var service = new OrderService(repo);
    
    service.CreateOrder(new Order { Id = 1, Total = 100 });
    
    var saved = repo.GetById(1);
    Assert.NotNull(saved);
    Assert.Equal(100, saved.Total);
}
```

## 📋 Test Quality Checklist

Before marking a task complete, verify each test:

```
[ ] Test has a descriptive name: MethodName_Scenario_ExpectedBehavior
[ ] Test covers ONE scenario (not multiple asserts on different concerns)
[ ] Test is deterministic (no DateTime.Now, no random, no network calls)
[ ] Test is isolated (doesn't depend on other tests' state)
[ ] Edge cases covered: null, empty, max, min, boundary values
[ ] Error paths covered: invalid input, missing dependencies, timeouts
[ ] Property-based test added for functions with mathematical properties
[ ] Fuzzing test added for functions accepting external input
[ ] "Mutation test" performed mentally: changing one line breaks one test
```

## 🚀 Integration Test Patterns

For API endpoints and database access:

```
[ ] Happy path: valid request → 200 OK with expected body
[ ] Validation: missing required field → 400 Bad Request with error details
[ ] Auth: no token → 401 Unauthorized
[ ] Auth: wrong role → 403 Forbidden
[ ] Not Found: valid auth but wrong ID → 404
[ ] Conflict: duplicate resource → 409 Conflict
[ ] Rate Limit: too many requests → 429 Too Many Requests
[ ] Concurrency: two simultaneous updates → 409 or optimistic concurrency
```
