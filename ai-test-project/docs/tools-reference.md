# Tools Reference — All Integrated Tools

Sources: awesome-claude-code · CC Switch · Superpowers · Ruflo · claude-mem

---

## DESKTOP APP

### CC Switch v3.12.3
**The All-in-One Manager for Claude Code, Codex, Gemini CLI, OpenCode & OpenClaw**
- Visual interface for all 5 CLI tools
- 50+ provider presets, one-click import
- Unified MCP + Skills management with bidirectional sync
- System tray quick switching
- Cloud sync: Dropbox, OneDrive, iCloud, WebDAV
- Built-in proxy with auto-failover + circuit breaker
- Usage tracking + cost analytics
- Session manager

**Install (Windows):** Download MSI or portable ZIP from https://github.com/farion1231/cc-switch/releases
**Install (macOS):** `brew tap farion1231/ccswitch && brew install --cask cc-switch`
**Install (Linux):** .deb / .rpm / .AppImage / .flatpak from releases

**Data location:**
- `~/.cc-switch/cc-switch.db` — SQLite database
- `~/.cc-switch/settings.json` — Settings
- `~/.cc-switch/skills/` — Skills sync

---

## CLAUDE CODE PLUGINS

### claude-mem — Persistent Memory
Preserves context across Claude Code sessions.
```
/plugin marketplace add thedotmack/claude-mem
/plugin install claude-mem
```
- Web UI at localhost:37777
- SQLite + Chroma vector DB
- 3-layer search: search → timeline → get_observations
- Privacy: wrap in `<private>` to exclude from injection
- Docs: https://docs.claude-mem.ai/

### Superpowers v5.0.5
Structured 7-stage software development workflow.
```
/plugin install superpowers@claude-plugins-official
```
Skills: TDD · systematic-debugging · brainstorming · writing-plans · executing-plans · dispatching-parallel-agents · requesting-code-review · using-git-worktrees · finishing-a-development-branch

### Ruflo v3.5
Multi-agent swarm orchestration platform.
```
npx ruflo@latest init --wizard
```
- 60+ specialized agent types
- HNSW vector memory
- WASM Agent Booster (<1ms transforms)
- 12 background auto-workers
- Queen-led Hive Mind
- Docs: https://github.com/ruvnet/ruflo

---

## NPM TOOLS (from awesome-claude-code)

### Session & History
- **cc-sessions** — Session management for Claude Code
- **cchistory** — Browse conversation history
- **ccexp** — Session export tool

### Usage Monitors
- **CC Usage** — Token and cost tracking
- **ccflare** — Real-time usage dashboard
- **Claudex** — Usage analytics

### Orchestrators
- **Claude Squad** — Multi-agent parallel orchestration
- **Auto-Claude** — Autonomous task automation
- **sudocode** — Code with elevated orchestration

### Config Managers
- **ClaudeCTX** — Context and config management
- **claude-rules-doctor** — CLAUDE.md linting and validation

### Security
- **Trail of Bits Security Skills** — Security audit skills
- **parry** — Prompt injection scanner

### IDE Integrations
- VS Code extension
- Emacs integration
- Neovim extension

---

## AGENT FRAMEWORKS

### SuperClaude
- 30 slash commands (`/sc:implement`, `/sc:test`, `/sc:research`, `/sc:brainstorm`, `/sc:pm`, etc.)
- 20 specialized agent personas
- 7 behavioral modes
- 8 MCP servers (Tavily, Context7, Sequential-Thinking, Serena, Playwright, Magic, etc.)
- Install: `superclaude install` (via pipx)

---

## AGENTS IN THIS PROJECT

| Agent | File | Use for |
|-------|------|---------|
| apex | .claude/agents/apex.md | Any non-trivial task |
| coder | .claude/agents/coder.md | Implementation, games |
| ruflo | .claude/agents/ruflo.md | Multi-agent swarm tasks |
| claudette | ~/.claude/agents/claudette.md | Autonomous long tasks |

---

## SKILLS IN THIS PROJECT

| Skill | File |
|-------|------|
| TDD | .claude/skills/test-driven-development.md |
| Debugging | .claude/skills/systematic-debugging.md |
| Brainstorming | .claude/skills/brainstorming.md |
| Plan Writing | .claude/skills/writing-plans.md |

---

## FUTURE PROJECTS — USE THIS FOLDER

When starting a new project, copy `.claude/` from this repo to get:
- All agents (apex, coder, ruflo)
- All skills
- All docs
- Pre-configured team playbook

Command:
```bash
cp -r /path/to/ai-test-project/.claude /path/to/new-project/.claude
```
