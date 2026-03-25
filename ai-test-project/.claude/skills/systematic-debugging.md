---
name: systematic-debugging
description: 4-phase root cause analysis. Use when encountering bugs, errors, or unexpected behavior. Evidence → Hypotheses → Test → Fix.
---

# Systematic Debugging (Superpowers)

## THE 4 PHASES

```
1. EVIDENCE    → Gather all facts. No theories yet.
2. HYPOTHESES  → Form ranked root cause candidates.
3. TEST        → Test each in isolation. Document failures.
4. FIX         → Patch root cause. Verify no regressions.
```

---

## PHASE 1 — GATHER EVIDENCE

Do not form theories yet. Only collect facts.

**Collect:**
- Exact error message (copy-paste, not paraphrase)
- Full stack trace
- Exact steps to reproduce
- Last known good state (commit, version, date)
- What changed between good and bad state
- Environment details (OS, Node version, browser, etc.)
- Frequency: always / sometimes / specific conditions

**Tools:**
```bash
# Node.js
node --stack-trace-limit=50 script.js
DEBUG=* node script.js

# Git — find when it broke
git bisect start
git bisect bad HEAD
git bisect good [last-good-commit]
```

**Before hypothesizing:** Can you reproduce it consistently? If not, that's a clue.

---

## PHASE 2 — FORM HYPOTHESES

Now form theories, ranked by probability.

**Format:**
```
H1 (most likely): [hypothesis] — because [evidence that supports it]
H2: [hypothesis] — because [evidence]
H3: [hypothesis] — because [evidence]
```

**Common root cause categories:**
- State mutation (unexpected side effects)
- Async/timing issue (race condition, missing await)
- Type mismatch (null, undefined, string vs number)
- Off-by-one error
- Missing edge case handling
- Environment difference (works locally, fails in CI)
- Dependency version change
- Config/env var missing or wrong

**If you can't form 3 hypotheses:** you need more evidence. Go back to Phase 1.

---

## PHASE 3 — TEST HYPOTHESES

Test each hypothesis **in isolation**. Smallest possible change that proves or disproves.

**Rules:**
- Change one thing at a time
- Add temporary logging/assertions to isolate
- If a fix introduces a new problem: **revert immediately**
- Document every attempt: "Tried X, failed because Y"
- Do not compound fixes — each test must be clean

**Failure log format:**
```
Attempt 1: [what was tried]
Result: [what happened]
Conclusion: [what this tells us]

Attempt 2: ...
```

**Isolation techniques:**
```js
// Binary search — comment out half to find the problem half
// console.log at entry/exit of every function in the path
// Hard-code inputs to eliminate variable data
// Reproduce in isolation (unit test, REPL, minimal script)
```

---

## PHASE 4 — FIX AND VERIFY

**Rules:**
- Fix the **root cause** — never patch symptoms
- Make the smallest change that fixes the root cause
- No "while I'm in here" improvements
- Run the full test suite after fixing
- Verify the original reproduction steps no longer reproduce the bug
- If related issues are found: create tickets, do not fix inline

**Verification checklist:**
```
- [ ] Bug no longer reproducible
- [ ] All existing tests pass
- [ ] New regression test added (proves bug is fixed)
- [ ] Root cause documented (not just the fix)
```

---

## ESCALATION RULE

**After 3 failed attempts:** Stop and surface to user.

Escalation report format:
```
BUG: [description]
EVIDENCE: [collected facts]
ATTEMPTS:
  1. Tried X → failed because Y
  2. Tried A → failed because B
  3. Tried C → failed because D
CURRENT HYPOTHESIS: [best remaining theory]
RECOMMENDED NEXT STEP: [what you think should happen]
NEED: [what you need from the user to proceed]
```

---

## VERIFICATION BEFORE COMPLETION

Before declaring a bug fixed:
1. Run exact reproduction steps — bug must not appear
2. Run full test suite — no new failures
3. Check adjacent code paths — no regressions
4. Add a test that would have caught this bug originally
