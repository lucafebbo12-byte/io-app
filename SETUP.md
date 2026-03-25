# Setup (Windows, based on the tutorial video)

## 1) Prerequisites verified
- Node.js installed
- npm installed
- Git installed
- VS Code installed

## 2) Install Claude Code (npm method from tutorial)
```powershell
npm.cmd install -g @anthropic-ai/claude-code
```

## 3) Open this project in VS Code
```powershell
code .
```

## 4) In VS Code terminal
- Open terminal: `Ctrl+Shift+``
- Start Claude: `claude`
- Trust directory when prompted

## 5) Use Plan Mode first (recommended in tutorial)
- In Claude, switch to Plan Mode (`Shift+Tab` until Plan)
- Ask Claude to ask clarifying questions before coding
- Review plan before allowing edits

## 6) Safe workflow
- Commit early and often before bug-fix prompts
- Use branches for each feature/fix

## 7) Suggested first prompt
```text
Build a small app from scratch. Ask me clarifying questions about product requirements, hard constraints, engineering principles, and architecture before implementation. Then show me a step-by-step implementation plan for approval.
```
