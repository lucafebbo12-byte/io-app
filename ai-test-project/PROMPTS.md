# PROMPTS.md

## 1) Master prompt (recommended)

Paste this into Claude (VS Code Agent mode or Claude Code CLI):

```
Read CLAUDE.md and follow it strictly.

Task: Build an "App Store" MVP (simple marketplace) in this repo.

Requirements:
- Public catalog page listing apps.
- App detail page.
- "Install" button (per-user installed state).
- Admin page for CRUD apps.

Guardrails:
- Start in Plan mode.
- Ask clarifying questions first (max 6).
- Propose a 2-3 milestone plan.
- Implement one milestone at a time.
- After each milestone: run commands to verify, summarize, and propose a git commit message.

Deliverables:
- `npm run dev` starts the app.
- Provide a short README section on how to run it.
```

## 2) "Make it better" prompt (after MVP works)

```
Audit the app for UX, security basics, and maintainability. Propose the top 5 improvements, then implement the top 2.
Constraints: keep changes small; run tests/dev server to verify.
```

## 3) Bug-fix prompt (high signal)

```
Bug report:
- Expected:
- Actual:
- Steps to reproduce:

Fix the bug with the smallest safe change. Add a regression test if practical.
```
