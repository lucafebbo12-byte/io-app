---
name: writing-plans
description: Create detailed implementation plans with 2-5 minute tasks, exact file paths, and verification steps. Use after brainstorming is complete and design is approved.
---

# Writing Plans (Superpowers)

## PURPOSE

Transform an approved design into a precise, executable implementation plan.
Tasks should be small enough that each is fully achievable in 2-5 minutes.

---

## PLAN FORMAT

```markdown
# Implementation Plan: [Feature Name]

## Context
- What we're building (1-2 sentences)
- Key constraints
- Assumptions made

## Milestone 1: [Name]
- [ ] 1.1 — [exact action] in `path/to/file.js`
  - Verification: [how to confirm this works]
- [ ] 1.2 — [exact action] in `path/to/other.js`
  - Verification: [test or check]
- [ ] 1.3 — Write failing test for [behavior] in `tests/unit/feature.test.js`
  - Verification: test runs and fails with expected error

## Milestone 2: [Name]
- [ ] 2.1 — [exact action]
  - Verification: ...
...

## Milestone N: QA + Cleanup
- [ ] N.1 — Run full test suite: `npm test`
  - Verification: all tests green
- [ ] N.2 — Check for regressions in [related feature]
- [ ] N.3 — Propose commit message
```

---

## TASK SIZING RULES

**Each task must:**
- Take 2-5 minutes for a professional developer
- Touch at most 1-2 files
- Have a single clear output
- Be independently verifiable

**If a task takes more than 5 minutes:** break it into sub-tasks.

**If a task has no verifiable output:** rewrite it more specifically.

---

## REQUIRED FIELDS PER TASK

1. **Action verb** — what to do (create, add, modify, delete, run)
2. **Exact file path** — `src/services/auth.js`, not "auth service"
3. **What changes** — be specific about the function/class/variable
4. **Verification** — test to run, assertion to check, or output to confirm

---

## ORDERING RULES

1. Tests before implementation (TDD)
2. Data layer before business logic
3. Business logic before routes/UI
4. Core path before edge cases
5. Happy path before error handling

---

## WHAT NOT TO INCLUDE

- ❌ Vague tasks: "update the auth system"
- ❌ Giant tasks: "implement the entire user flow"
- ❌ No verification: tasks with no way to confirm completion
- ❌ Implementation details in planning: focus on what, not how
- ❌ Tasks that depend on unknown decisions: resolve those first

---

## BEFORE EXECUTING THE PLAN

Confirm with the user:
- Does the plan match the approved design?
- Are there missing edge cases?
- Is the order correct?
- Any concerns before we start?

Only start execution after the plan is approved.
