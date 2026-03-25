# CLAUDE.md

## Coding style
- Use clear, small functions and meaningful names.
- Prefer simple, readable code over clever code.
- Keep route handlers thin and move logic into modules as the app grows.

## Architecture rules
- Keep entrypoint in `index.js` and split features into folders (`routes`, `services`, `utils`) when needed.
- Validate request inputs before processing.
- Return consistent JSON for API routes.
- Add tests for new endpoints before refactors.

## How to work (important)
- Start in Plan mode for any non-trivial task.
- Ask clarifying questions when requirements are ambiguous.
- Propose a small milestone plan (1-3 milestones) before large edits.
- Prefer incremental changes with frequent `git` commits at stable points.
- When running commands, explain what you will run and why.

## Agent playbook (use these roles)
When building a real app, act like a small team. Use these roles explicitly in your responses:
- Product Manager (PM): clarifies goals, user flows, acceptance criteria.
- Architect: proposes high-level design, data model, and boundaries.
- Backend Engineer: implements API, data layer, auth, validation.
- Frontend Engineer: implements UI, state, API integration, UX polish.
- QA Engineer: writes test plan, adds basic automated tests, reproduces bugs.
- DevOps/Release: sets up scripts, env vars, deploy notes, GitHub workflow.

Default behavior:
- PM and Architect first, then Backend/Frontend, then QA, then DevOps.
- If a step is risky or ambiguous, stop and ask a question.

## App template: "App Store" MVP (prompt you can paste)
Use this prompt to create an app-store-like MVP (not Apple App Store, just a simple marketplace):

```
You are my software team. Build an "App Store" web app MVP in this repo.

Goals:
- A public catalog page that lists apps (name, description, price/free tag).
- App detail page.
- "Install" button that simulates install (toggle installed state per user).
- Admin page to add/edit/remove apps.

Constraints:
- Keep it simple and fast to ship. Prefer TypeScript if you introduce a frontend.
- For storage, start with a local JSON file or SQLite (pick one and explain why).
- Authentication can be minimal (single admin password env var) unless I request full auth.
- Provide scripts: `npm run dev` and `npm test` (if tests exist).

Workflow:
1) Ask me up to 6 clarifying questions (PM).
2) Propose an implementation plan with 2-3 milestones (Architect).
3) Implement milestone-by-milestone. After each milestone:
   - run the app/tests
   - summarize what changed
   - propose a commit message
```

## Good default tech stack (if not specified)
- Backend: Node.js + Express
- Frontend: Vite + React (only if UI is requested)
- DB: SQLite (or JSON for quickest MVP)
