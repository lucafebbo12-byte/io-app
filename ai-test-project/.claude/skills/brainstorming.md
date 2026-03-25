---
name: brainstorming
description: Structured design refinement before coding. Use at the start of any non-trivial feature. Ask Socratic questions, surface assumptions, get design approved before implementation.
---

# Brainstorming (Superpowers)

## PURPOSE

Do this BEFORE writing any code.
Goal: understand the real requirements, not the stated ones.

---

## THE SOCRATIC APPROACH

Ask questions that expose hidden assumptions, edge cases, and scope boundaries.

**Question categories:**

**1. Users & Goals**
- Who actually uses this feature?
- What are they trying to accomplish?
- What does success look like for them?

**2. Edge Cases**
- What happens when [X is empty / null / maximum]?
- What if the user does this twice?
- What if this runs concurrently?

**3. Constraints**
- What are the performance requirements?
- What's the acceptable error rate?
- What devices/browsers/environments must it support?

**4. Scope**
- What is explicitly out of scope?
- What should we defer to v2?
- What integrations are required now vs later?

**5. Existing System**
- How does this interact with [existing feature]?
- What breaks if we change [current behavior]?
- Is there existing code we should reuse?

---

## PRESENTING THE DESIGN

After questions are answered, present the design in digestible sections:

**Section 1: Problem Summary** (1-2 sentences)
**Section 2: Proposed Solution** (high-level, no code)
**Section 3: Data Model** (what changes)
**Section 4: User Flow** (step by step)
**Section 5: Edge Cases Handled** (list)
**Section 6: Out of Scope** (explicit list)
**Section 7: Open Questions** (anything still unclear)

Get approval on each section before moving to planning.

---

## WHEN TO STOP BRAINSTORMING

Move to planning when:
- User has approved the design
- All blockers are resolved
- Scope is clearly defined
- Edge cases are explicitly handled or deferred

Do NOT move to planning if:
- Requirements are still changing
- A critical constraint is unknown
- User hasn't approved the approach

---

## ANTI-PATTERNS

- ❌ Jumping to implementation details during brainstorm
- ❌ Asking too many questions at once (max 3 at a time)
- ❌ Presenting solutions before understanding the problem
- ❌ Ignoring non-functional requirements
- ❌ Assuming the first stated requirement is the real requirement
