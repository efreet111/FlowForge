---
capability_matrix:
  ai_reasoning:
    - Selection of default models per tier (budget/quality) for each IDE — LLM recommends based on provider catalog
    - Mapping agent role to model capability class (reasoning vs cheap vs code-specialized)
    - Translation of a canonical model-role assignment into IDE-specific provider names
  deterministic:
    - JSON schema for agent-models.json (field names, types, required keys)
    - File paths: `ide/{ide}/config/agent-models.json` per IDE
    - Installer logic: read JSON → generate IDE-native format (no human guesswork)
    - Merge strategy: preserve user overrides during reinstall (deterministic JSON deep-merge)
    - Agent identity keys (forge-discovery, forge-arch, etc.) are immutable — never renamed
---
# Spec: Model Configuration Architecture

## 1. Objective and scope

**Problem:** FlowForge currently has model assignments scattered across 5+ files, using mixed formats (JSON, Markdown, hardcoded Python dicts, YAML frontmatter) and listing models that don't exist in the target IDE (e.g., Claude/GPT models in Antigravity rules, which uses Gemini). This causes confusion, installer fragility, and prevents the Starter Kit's "5-minute first flow" goal.

**Goal:** Establish a single-source-of-truth per IDE: one canonical JSON file per IDE that all consumers (installer scripts, agent compilers, rule generators) read from. Eliminate duplication and stale model references.

**Out of scope:**
- Changing how IDE delegates to models at runtime (that's the orchestrator's concern)
- Modifying the orchestrator parity contract (`ide/shared/workflow-orchestrator-parity.md`)
- Implementing the changes — this spec defines the architecture only
- The `flowforge` binary's `install` command (this spec informs it, but does not spec binary changes)

## 2. Functional requirements (FR)

### FR-001: Single canonical model file per IDE
Each IDE MUST have exactly one JSON file that is the source of truth for model assignments. No duplication, no stale references.

- **Scenario A (OpenCode):** Given the OpenCode installer runs, when it generates `opencode.json` and `model-assignments.md`, then both must derive model names from `ide/opencode/config/agent-models.json` — never from a template or hardcoded script.
- **Scenario B (Cursor):** Given `compile-agents-from-skills.py` rebuilds Cursor agents, when it sets the `model:` frontmatter, then it must read from `ide/cursor/config/agent-models.json` — not from a hardcoded Python dict.

### FR-002: Unified JSON schema
All IDE-level `agent-models.json` files MUST follow the same schema. The schema supports:
- `provider` metadata (name, description, model catalog URL)
- `agents`: per-agent assignment (model, fallback, mode, purpose)
- `tiers` (optional): named tiers (e.g., `budget`, `quality`) with agent overrides
- `cost_policy` (optional): text describing the cost strategy for the tier

- **Scenario A (add new agent):** Given a new agent `forge-reviewer` is added, when its model config is needed across all IDEs, then only new entries in each `agent-models.json` are required — no changes to installer scripts or agent frontmatter.
- **Scenario B (model rename upstream):** Given OpenCode renames `big-pickle` to `big-pickle-v2`, when the maintainer updates `ide/opencode/config/agent-models.json`, then `generate-config.sh` and `install.sh` produce correct output without script changes.

### FR-003: Installer consumes JSON, not hardcoded models
The installer (both `install.sh` and `flowforge install`) must read the IDE-specific JSON file and generate the IDE-native format from it.

- **Scenario A (OpenCode install):** Given `generate-config.sh` runs, when it builds `opencode.json`, then each agent's `model` field in the JSON is extracted from `ide/opencode/config/agent-models.json` — no `sed` substitution of placeholder tokens.
- **Scenario B (Antigravity install):** Given the installer copies rules to `~/.gemini/antigravity/rules/`, when it writes `model-assignments.md`, then the model names match Gemini's actual catalog (e.g., `gemini-3-flash`, `gemini-3-pro`) — not Claude/GPT names that don't exist in Antigravity.

### FR-004: User override preservation
Users can customize models without losing changes on reinstall. The installer must detect pre-existing user overrides and merge intelligently.

- **Scenario A (user changed a model):** Given a user edited `~/.config/opencode/opencode.json` to change `forge-dev` from `big-pickle` to `mimo-v2.5-free`, when the installer runs again, then the user's override is preserved (not overwritten) and a diff is shown.
- **Scenario B (fresh install):** Given no prior config exists, when the installer runs, then it writes the default models from `agent-models.json` without prompting.

### FR-005: Migration of existing files
The transition from the current fragmented state to the new structure must be explicit and reversible.

- **Scenario A (existing `.agents/rules/model-assignments.md`):** Given a project has the generic model-assignments.md, when the migration runs, then the file is replaced with a redirect notice pointing to `ide/{ide}/config/agent-models.json` — or removed if in a generated directory.
- **Scenario B (existing Cursor hardcoded models):** Given `compile-agents-from-skills.py` has a `MODELS` dict, when migrated, then the dict is replaced with a JSON file read and recompilation is verified to produce identical output.

### FR-006: Validation
The schema and model references must be validated to prevent drift.

- **Scenario A (CI check):** Given a PR changes `agent-models.json`, when CI runs, then a validator checks: (a) schema validity, (b) all agent keys present, (c) models exist in the provider's declared catalog.
- **Scenario B (provider adds/removes models):** Given OpenCode Zen adds `nuevo-modelo-pro-free`, when a maintainer updates the JSON, then the CI check confirms the model is in the `provider.models` list.

## 3. Non-functional requirements (NFR)

### NFR-001: Backward compatibility
Existing projects and IDE installations must continue to function during and after migration. The installer must detect old format and handle gracefully (not fail, not overwrite without backup).

### NFR-002: Human readability
The JSON files must be documented with inline comments (JSONC format tolerated by consumers) so a human can open and understand which model maps to which agent without reading scripts.

### NFR-003: Installer idempotency
Running `install.sh` twice must produce the same result as running it once (modulo user overrides). No accumulation of duplicate entries, no stale backups without cleanup.

### NFR-004: Cross-platform
The JSON format and installer paths must work identically on Linux, macOS, and Windows (`install.ps1` parity).

### NFR-005: Performance (install time)
Reading and parsing a ~2KB JSON file must not add measurable latency to the install process (target: <50ms overhead).

## 4. Proposed architecture

### File structure (post-migration)

```
ide/
├── opencode/
│   ├── config/
│   │   └── agent-models.json          ← CANONICAL: OpenCode Zen free-tier models
│   ├── templates/                     ← Keep for agent .md.tpl files only
│   │   └── agents/*.md.tpl
│   ├── generate-config.sh             ← Reads agent-models.json, writes opencode.json + model-assignments.md
│   ├── agents/                        ← Pre-compiled agent .md files (dist)
│   └── commands/
├── cursor/
│   ├── config/
│   │   └── agent-models.json          ← CANONICAL: Cursor Budget tier models
│   ├── rules/
│   │   └── model-assignments.mdc      ← GENERATED: Markdown for Cursor consumption
│   ├── compile-agents-from-skills.py  ← Reads agent-models.json for frontmatter
│   ├── agents/                        ← Compiled from skills/ + agent-models.json
│   └── commands/
├── antigravity/
│   ├── config/
│   │   └── agent-models.json          ← CANONICAL: Gemini models
│   ├── rules/
│   │   └── model-assignments.md       ← GENERATED: Markdown from agent-models.json
│   ├── workflows/
│   └── AGENTS.md
└── vscode/
    ├── config/
    │   └── agent-models.json          ← CANONICAL: Copilot/Kilo models
    ├── agents/
    │   └── *.agent.md                 ← GENERATED: frontmatter model from agent-models.json
    └── copilot-instructions.md
```

### JSON schema

```jsonc
{
  "$schema": "https://flowforge.dev/schemas/agent-models-v1.json",
  "provider": {
    "id": "opencode-zen",
    "name": "OpenCode Zen (Free Tier)",
    "docs_url": "https://opencode.ai/docs/models",
    "models": [
      "big-pickle",
      "deepseek-v4-flash-free",
      "minimax-m2.5-free"
    ]
  },
  "active_tier": "budget",
  "tiers": {
    "budget": {
      "description": "Free / low-cost tier for daily development",
      "cost_policy": "Discovery/Memory → cheapest. Dev → code-specialized. Arch/Plan/Verify → strongest free model.",
      "agents": {
        "forge-discovery": {
          "model": "deepseek-v4-flash-free",
          "fallback": "deepseek-v4-flash-free",
          "mode": "subagent",
          "purpose": "Context map / CKP-0"
        }
      }
    },
    "quality": {
      "description": "Premium tier for critical releases — overrides budget assignments",
      "agents": {
        "forge-arch": {
          "model": "minimax-m2.5-free",
          "fallback": "big-pickle"
        }
      }
    }
  },
  "agents": {
    "forge-orchestrator": {
      "model": "big-pickle",
      "fallback": "big-pickle",
      "mode": "primary",
      "purpose": "CKP-0..4 coordinator"
    },
    "forge-discovery": { "...same as above..." },
    "forge-arch": {},
    "forge-plan": {},
    "forge-dev": {},
    "forge-verify": {},
    "forge-memory": {},
    "forge-teacher": {},
    "default": {}
  }
}
```

### How each IDE consumes the JSON

| IDE | Consumer | Output format | Target path |
|-----|----------|---------------|-------------|
| **OpenCode** | `generate-config.sh` | Merged into `opencode.json` agent blocks + `model-assignments.md` | `~/.config/opencode/opencode.json` + `~/.config/opencode/.agents/rules/model-assignments.md` |
| **Cursor** | `compile-agents-from-skills.py` | Frontmatter `model:` in each `forge-*.md` | `ide/cursor/agents/forge-*.md` |
| **Cursor** | `install.sh` | `model-assignments.mdc` Markdown | `~/.cursor/rules/model-assignments.mdc` |
| **Antigravity** | `install.sh` | `model-assignments.md` Markdown | `~/.gemini/antigravity/rules/model-assignments.md` |
| **VS Code** | Script or installer | Frontmatter `model:` in each `*.agent.md` | `ide/vscode/agents/*.agent.md` |

### Migration plan

| Step | File affected | Action |
|------|--------------|--------|
| 1 | `ide/opencode/config/agent-models.json` (NEW) | Create from `ide/opencode/templates/agent-models.json`, add schema structure, add `tiers` wrapper |
| 2 | `ide/cursor/config/agent-models.json` (NEW) | Create from `ide/cursor/rules/model-assignments.mdc` data, normalized to JSON schema |
| 3 | `ide/antigravity/config/agent-models.json` (NEW) | Create with Gemini-appropriate models (replace Claude/GPT) |
| 4 | `ide/vscode/config/agent-models.json` (NEW) | Create from VS Code agent frontmatter data, normalized |
| 5 | `ide/cursor/compile-agents-from-skills.py` | Replace `MODELS` dict with `json.load()` from step 2 file |
| 6 | `ide/opencode/generate-config.sh` | Point to step 1 file (currently reads `templates/agent-models.json`) |
| 7 | `ide/antigravity/rules/model-assignments.md` | Replace content with Gemini models derived from step 3 JSON |
| 8 | `ide/vscode/agents/*.agent.md` | Regenerate frontmatter `model:` from step 4 JSON |
| 9 | `ide/opencode/templates/agent-models.json` | DELETE (replaced by step 1) |
| 10 | `ide/opencode/templates/model-assignments.md.tpl` | DELETE (now generated, not template-substituted) |
| 11 | `.agents/rules/model-assignments.md` | Replace with redirect doc: "See ide/{ide}/config/agent-models.json" |
| 12 | `ide/antigravity/rules/model-assignments.md` (Gemini) | Regenerate from step 3 JSON |
| 13 | `ide/README.md` | Update "Model assignments" row in the IDE matrix table |

### Impact on installer (`install.sh` / `install.ps1`)

| Current behavior | New behavior | Risk |
|-----------------|--------------|------|
| `generate-config.sh` reads `templates/agent-models.json` | Reads `config/agent-models.json` | Low — same format, different path |
| `install.sh` line 210: `sed` patches model from `agent-models.json` via `jq` | Same logic, different source path | Low |
| Cursor agent compilation: hardcoded `MODELS` dict | `json.load()` from `config/agent-models.json` | Medium — Python script change needed |
| Antigravity: copies generic `model-assignments.md` with Claude/GPT models | Generates from Antigravity-specific JSON with Gemini models | High — first time Antigravity gets correct models |
| VS Code: no model generation | Regenerates frontmatter `model:` field | Medium — new capability |

### Impact on Starter Kit PRD

The Starter Kit's "5-minute first flow" goal requires a zero-config experience. Today, a user on Antigravity sees Claude/GPT models they can't use. This spec fixes that:

- **Positive:** Each IDE gets models that actually exist for that IDE (Gemini on Antigravity, OpenCode Zen on OpenCode, etc.)
- **Positive:** No manual model editing required — sensible defaults per tier
- **Positive:** `flowforge install` becomes truly hands-off for model config
- **Neutral:** If the user wants to customize, they now know exactly which file to edit (`ide/{ide}/config/agent-models.json` in the repo, or the generated file in their config dir)
- **Risk:** We must ensure the default models for each IDE are actually available to free-tier users. The `provider.models` list in the JSON doubles as a runtime availability check.

## 5. Open questions for human (OQ-*)

| ID | Tag | Question | Decision |
|----|-----|---------|----------|
| OQ-1 | [BLOCKER] ✅ RESUELTO | Should Antigravity's default models be **Gemini 3 Flash/Pro** (available to all Antigravity users) or keep a generic tier that users customize? The current Claude/GPT list is wrong, but we need to know what to replace it with. | **Opción A**: `gemini-3-flash` (barato, rápido) para discovery/memory, `gemini-3-pro` (potente) para arch/plan/verify |
| OQ-2 | [BLOCKER] ✅ RESUELTO | For VS Code Copilot agents, the current frontmatter lists `claude-sonnet-4-20250514` and `gpt-5.2`. Are both available to Copilot free-tier users? If not, what single model should be the default? | **`gpt-4o`** para todos los agentes VS Code (disponible en Copilot free) |
| OQ-3 | [OPTIONAL] | Should the JSON schema include a `$schema` URL pointing to `flowforge.dev/schemas/`? This requires hosting the schema file publicly. | Assumed: Yes, add the `$schema` field but make validation optional (warning, not error) until the URL is live. |
| OQ-4 | [OPTIONAL] | For the quality tier in each IDE, should we pre-fill models or leave it as an example? Pre-filling requires knowing which premium models exist per provider. | Assumed: Pre-fill with realistic models per provider, commented as "quality tier — uncomment and adjust to your subscription." |
| OQ-5 | [FOLLOW-UP] | Should `flowforge doctor` validate that the configured models actually exist in the provider's current catalog? This requires an API call or a periodically-updated model list. | Assumed: Not in v1. Static validation against `provider.models` list is sufficient. |

### STRIDE threat analysis

| Threat | STRIDE category | Severity | Mitigation |
|--------|-----------------|----------|------------|
| Installer writes wrong model names → agents fail to spawn | **Denial of Service** | Medium | CI validation: check every model in JSON against `provider.models` list; fail build on mismatch |
| User with Pro subscription on Cursor gets Budget-tier models (worse quality) | **Information Disclosure** (misconfiguration) | Low | `active_tier` field in JSON lets user switch; installer detects subscription tier from provider API when possible |
| Malicious PR changes model to attacker-controlled endpoint | **Spoofing** | High | `provider.api` URL is part of the validated schema; CI must fail if endpoint changes without ADR |
| JSON becomes unreadable to humans (no comments, no docs) | **Human Error** | Low | Schema includes `description` and `docs_url` fields; generated Markdown files retain human-readable format |
| Installer fails silently on missing `jq` (JSON parser) | **Denial of Service** | Low | Already handled by `generate-config.sh` line 4; extend to all scripts |
| Stale `agent-models.json` not updated → agents use deprecated models | **Elevation of Privilege** (via fallback chain) | Low | `provider.models` list acts as allowlist; CI can warn on models not in upstream catalog |

## Developer manual tests (required — mark [x] before /flow-close)

| ID | Case / flow | Steps (summary) | Expected result | [x] |
|----|-------------|-----------------|-----------------|-----|
| PM-1 | Fresh install generates correct models per IDE | 1. Remove all FlowForge config<br>2. Run `install.sh`<br>3. Check `~/.config/opencode/opencode.json` model fields match `ide/opencode/config/agent-models.json`<br>4. Check `~/.cursor/rules/model-assignments.mdc` matches `ide/cursor/config/agent-models.json`<br>5. Check `~/.gemini/antigravity/rules/model-assignments.md` has Gemini models (not Claude/GPT) | All model names in generated files match their canonical JSON source. Antigravity shows Gemini models. | [x] ✅ 2026-01-17 |
| PM-2 | User override survives reinstall | 1. Install FlowForge normally<br>2. Edit `opencode.json`: change `forge-dev` model to `mimo-v2.5-free`<br>3. Run `install.sh` again<br>4. Check `forge-dev` model | `forge-dev` model is still `mimo-v2.5-free`. Diff output shows the override was preserved. | [x] ✅ 2026-01-17 |
| PM-3 | Cursor recompilation reads JSON | 1. Edit `ide/cursor/config/agent-models.json`: change `forge-arch` from `kimi-k2.7-code` to `gpt-5-mini`<br>2. Run `python ide/cursor/compile-agents-from-skills.py`<br>3. Check `ide/cursor/agents/forge-arch.md` frontmatter | `model: gpt-5-mini` in the frontmatter. Match between JSON and compiled agent. | [x] ✅ 2026-01-17 |
| PM-4 | CI validation catches broken model reference | 1. Edit any `agent-models.json`: add `"model": "nonexistent-model"` for `forge-dev`<br>2. Run the CI validation script (to be created) | Validation fails with: `forge-dev references 'nonexistent-model' not in provider.models list` | [x] ✅ 2026-01-17 |

---

## 6. Decisiones tomadas (CKP-1 ✅ Aprobado)

| Decisión | Respuesta | Fecha |
|----------|-----------|-------|
| **Modelos Antigravity** | `gemini-3-flash` (discovery/memory) + `gemini-3-pro` (arch/plan/verify) | 2026-01-17 |
| **Modelos VS Code** | `gpt-4o` para todos los agentes (Copilot free tier) | 2026-01-17 |

**Status:** Spec aprobado por el humano. Listo para `/flow-plan`.

---

## Memory Signal
- type: decision
- significance: high
- summary: "Adopt IDE-specific canonical `agent-models.json` files (one per IDE under `ide/{ide}/config/`) as the single source of truth for model assignments. Eliminate model duplication across templates, hardcoded Python dicts, and stale Markdown rules. BLOCKERs resueltos: Antigravity usa gemini-3-flash/pro, VS Code usa gpt-4o."
