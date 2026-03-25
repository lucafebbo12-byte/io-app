---
name: test-driven-development
description: Enforce RED-GREEN-REFACTOR TDD cycle. Use before writing any new implementation code.
---

# Test-Driven Development (Superpowers)

## THE CYCLE

```
RED      → Write a failing test. Run it. Confirm it fails.
GREEN    → Write minimal code to make it pass. Nothing more.
REFACTOR → Clean up code and tests. Both must stay green.
```

**The order is non-negotiable.** Never skip RED.

---

## RED PHASE

1. Write the test before any implementation exists
2. Run the test — it MUST fail
3. If the test passes without implementation: the test is wrong or testing the wrong thing
4. Name the test to describe the expected behavior: `should return 404 when user not found`

```js
// RED — this must fail
test('applyDamage reduces health by damage amount', () => {
  const player = { health: 100 };
  applyDamage(player, 25);
  expect(player.health).toBe(75);
});
// ReferenceError: applyDamage is not defined ✅ (expected failure)
```

---

## GREEN PHASE

1. Write the **minimum** code to make the failing test pass
2. Do not add features, optimizations, or "nice to haves"
3. The only goal is a passing test

```js
// GREEN — minimal implementation
function applyDamage(entity, damage) {
  entity.health -= damage;
}
// Test passes ✅
```

---

## REFACTOR PHASE

1. Improve the implementation without changing behavior
2. Run tests after every change — they must stay green
3. Improve: naming, structure, duplication, readability

```js
// REFACTOR — improved without breaking behavior
function applyDamage(entity, amount) {
  if (amount < 0) throw new Error('Damage must be non-negative');
  entity.health = Math.max(0, entity.health - amount);
}
// Test still passes ✅
```

---

## ANTI-PATTERNS TO REJECT

- ❌ Writing implementation before a test
- ❌ Writing tests after implementation "for coverage"
- ❌ Skipping RED because "it's obvious"
- ❌ Over-mocking to the point the test tests nothing
- ❌ Writing multiple tests before any implementation
- ❌ Giant tests covering multiple behaviors

---

## TEST ORGANIZATION

```
tests/
├── unit/
│   ├── [module].test.js       # pure function tests
│   └── [component].test.js
├── integration/
│   ├── [feature].test.js      # module interaction
│   └── [api-route].test.js
└── e2e/
    └── [flow].test.js         # full user flows
```

---

## WHAT MAKES A GOOD TEST

- Tests **one behavior** per test case
- Has a clear **Arrange → Act → Assert** structure
- Name describes **what** it tests, not how
- Is independent of other tests
- Is fast (unit tests: <10ms, integration: <500ms)
- Fails for the right reason

```js
// Arrange
const player = createPlayer({ health: 50 });
// Act
applyDamage(player, 60);
// Assert
expect(player.health).toBe(0); // clamped to 0, not -10
```

---

## FRAMEWORK SELECTION

| Project | Framework |
|---------|-----------|
| Node.js | Jest / Vitest / Mocha |
| React | Vitest + Testing Library |
| Python | pytest |
| Rust | cargo test (built-in) |
| Go | go test (built-in) |

**Always use the framework already in the project. Never add a new one.**
