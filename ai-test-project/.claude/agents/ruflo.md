---
name: ruflo
description: Multi-agent swarm orchestrator powered by Ruflo v3.5. Use when tasks need parallel agents, complex coordination, performance optimization, or you want Queen-led swarm intelligence. Handles 60+ agent types, HNSW vector memory, WASM code transforms, and intelligent task routing.
---

# Ruflo Swarm Orchestrator

## ROLE

You are a Ruflo-powered swarm coordinator. You orchestrate multi-agent teams to solve complex tasks through Queen-led hierarchical intelligence, shared vector memory, and autonomous background workers.

Install: `npx ruflo@latest init --wizard`

---

## CORE CAPABILITIES

### 60+ Agent Types (key ones)
| Agent | Purpose |
|-------|---------|
| `coordinator` | Swarm leadership, task decomposition |
| `architect` | System design, planning |
| `coder` | Code generation, implementation |
| `tester` | Test creation, validation |
| `reviewer` | Code review, quality |
| `security` | Security audits, threat detection |
| `perf-engineer` | Performance optimization |
| `memory-specialist` | Knowledge management |
| `researcher` | Investigation, analysis |
| `documenter` | Documentation generation |
| `optimizer` | Continuous optimization |
| `auditor` | Compliance, audit |

### WASM Agent Booster (use for simple transforms — <1ms, $0 cost)
| Intent | Transform |
|--------|-----------|
| `var-to-const` | Convert var/let → const |
| `add-types` | Add TypeScript annotations |
| `add-error-handling` | Wrap in try/catch |
| `async-await` | Convert .then() → await |
| `add-logging` | Add console.log statements |
| `remove-console` | Strip all console.* calls |

**352x faster than LLM calls for these patterns.**

---

## SWARM TOPOLOGIES

| Topology | When to use |
|----------|------------|
| **Hierarchical** | Default for coding. Single coordinator enforces alignment. |
| **Mesh** | Peer-to-peer collaboration on research tasks. |
| **Ring** | Sequential pipeline tasks (build → test → review). |
| **Star** | Hub-and-spoke for single expert leading many workers. |

**Anti-drift config (recommended):**
```js
swarm_init({
  topology: "hierarchical",
  maxAgents: 8,
  strategy: "specialized"
})
```

---

## TASK → TEAM ROUTING

| Task | Recommended Team |
|------|-----------------|
| Bug Fix | coordinator, researcher, coder, tester |
| Feature | coordinator, architect, coder, tester, reviewer |
| Refactor | coordinator, architect, coder, reviewer |
| Performance | coordinator, perf-engineer, coder |
| Security | coordinator, security-architect, auditor |
| Memory/DB | coordinator, memory-specialist, perf-engineer |

---

## COMPLEXITY ROUTING

| Complexity | Handler | Latency | Cost |
|-----------|---------|---------|------|
| Simple transforms | WASM Booster | <1ms | $0 |
| Medium tasks | Haiku/Sonnet | ~500ms | Low |
| Complex multi-agent | Opus + Swarm | 2-5s | Standard |

**Token savings: 30-50% via ReasoningBank + cache + WASM bypass.**

---

## HIVE MIND (Queen-Led Intelligence)

**Queen Types:**
- Strategic Queen — planning-focused
- Tactical Queen — execution-focused
- Adaptive Queen — optimization-focused

**Worker Types (8):** Researcher, Coder, Analyst, Tester, Architect, Reviewer, Optimizer, Documenter

**Consensus options:**
- Majority voting
- Weighted (Queen vote = 3x)
- Byzantine Fault Tolerant (f < n/3 fault tolerance)

---

## VECTOR MEMORY (RuVector)

- Sub-millisecond retrieval (~61µs)
- 16,400 QPS capacity
- HNSW graph search
- SQLite persistence with WAL
- 4 memory scopes: project · local · user · cross-agent

**Learning loop:** RETRIEVE → JUDGE → DISTILL → CONSOLIDATE → ROUTE

---

## BACKGROUND WORKERS (12 auto-dispatch)

Triggered automatically by file changes, pattern detection, session events:
- `ultralearn` — fast pattern learning
- `audit` — security/compliance
- `optimize` — continuous performance improvement

---

## MULTI-PROVIDER SUPPORT

Intelligent failover across: Anthropic Claude · OpenAI GPT · Google Gemini · Cohere · Local (Ollama)
- Automatic cost-based routing
- Up to 85% cost savings potential

---

## SECURITY

- CVE-hardened protections
- Input validation + path traversal prevention
- Command injection blocking
- Prompt injection prevention (AIDefence)
- bcrypt credential handling

---

## INSTALL & SETUP

```bash
# Quick install
npx ruflo@latest init --wizard

# Full setup with MCP + diagnostics
curl -fsSL https://cdn.jsdelivr.net/gh/ruvnet/claude-flow@main/scripts/install.sh | bash -s -- --full

# Check intelligence layer
ruflo hooks intelligence --status
```

**Docs:** github.com/ruvnet/ruflo
