# Base Context Mapping — FlowForge ↔ FlowDoc Skills

> **Status**: Active
> **Feature**: `adopt-flowdocs-skills` (HU-021)
> **Related**: [FR-007](spec.md) · [FR-008](spec.md) · [ADR-004](../../docs/decisions/ADR-004-flowdoc-integration.md) · [ADR-005](../../docs/decisions/ADR-005-installer-headless-native-libs.md)

---

## Purpose

This document defines the **runtime contract** between forge-orchestrator and the FlowDoc skills layer. When forge-orchestrator invokes any flowdoc skill, it injects these mappings into the base context so that the skill receives FlowForge-canonical paths instead of its upstream defaults.

**Key principle**: No flowdoc skill files are modified. All path resolution happens at invocation time via base context injection.

---

## Template Reference Mapping

Each flowdoc skill expects templates at upstream paths that differ from FlowForge's actual template locations. forge-orchestrator maps these at runtime:

| Skill | Skill expects (upstream default) | FlowForge provides (base context injection) |
|-------|----------------------------------|---------------------------------------------|
| `flowdoc-hu` | `docs/templates/user-stories/template-user-story.md` | `docs/templates/HU-template.md` |
| `flowdoc-adr` | `docs/templates/architecture/ADR_template.md` | `docs/templates/adr-template.md` |
| `flowdoc-rfc` | `docs/templates/architecture/RFC_template.md` | `docs/templates/rfc-template.md` |
| `flowdoc-prd` | `docs/templates/PRD/PRD_template.md` | `docs/templates/PRD.md` |

> **Source of mismatch (F-3)**: FlowDoc upstream uses nested directory structures (`user-stories/`, `architecture/`, `PRD/`) while FlowForge flattens templates under `docs/templates/`. This mapping resolves the mismatch without modifying either side.

---

## HU Path Mapping (ADR-005)

flowdoc-hu's default output path is flat (`docs/tasks/HU-NNN-name.md`). FlowForge uses range-bins per ADR-005:

| Skill default output | FlowForge override (base context) |
|-----------------------|-----------------------------------|
| `docs/tasks/HU-NNN-name.md` (flat) | `docs/tasks/HU-001-HU-099/HU-NNN.md` (range-bin) |

> **Source of mismatch (F-4)**: flowdoc-hu writes flat by default. The base context overrides the output path to comply with ADR-005 range-bins.

---

## Output Path Mapping (all skills)

| Skill | Output path (FlowForge canonical) |
|-------|-----------------------------------|
| `flowdoc-hu` | `docs/tasks/HU-001-HU-099/HU-{NNN}.md` |
| `flowdoc-prd` | `docs/PRD.md` |
| `flowdoc-adr` | `docs/architecture/adr/` |
| `flowdoc-rfc` | `docs/architecture/rfc/` |
| `flowdoc-api` | `docs/endpoints.md` |
| `flowdoc-db` | `docs/schema.md` |
| `flowdoc-review` | Read-only report (no file output) |
| `flowdoc-discover` | Base context (no file output) |

---

## Invocation Mode

| Cycle | Skills | Mode | Hub |
|-------|--------|------|-----|
| **Cycle A: User Stories** | `flowdoc-hu` | Direct (forge-orchestrator → flowdoc-hu) | forge-orchestrator |
| **Cycle B: Base Documentation** | `flowdoc-assist` → {discover, prd, adr, rfc, api, db, review} | Delegated (forge-orchestrator → flowdoc-assist → specialist) | forge-orchestrator → flowdoc-assist |

> **Rule**: `flowdoc-hu` is NEVER invoked via `flowdoc-assist`. The two cycles are independent; forge-orchestrator is the single hub connecting both.

---

## Contract (JSON representation)

```json
{
  "flowforge_slug": "<current-feature-slug>",
  "template_refs": {
    "hu": "docs/templates/HU-template.md",
    "adr": "docs/templates/adr-template.md",
    "rfc": "docs/templates/rfc-template.md",
    "prd": "docs/templates/PRD.md"
  },
  "output_paths": {
    "hu": "docs/tasks/HU-001-HU-099/HU-{NNN}.md",
    "adr": "docs/architecture/adr/",
    "rfc": "docs/architecture/rfc/",
    "prd": "docs/PRD.md"
  },
  "invocation_mode": {
    "hu": "direct",
    "base_docs": "delegated_via_flowdoc_assist"
  }
}
```

---

## How to use

1. forge-orchestrator reads this mapping before invoking any flowdoc skill.
2. The mapping is injected into the base context passed to the skill.
3. The skill receives FlowForge paths, not upstream defaults.
4. No skill files are modified — this is a **runtime mapping only**.

---

## Drift detection

If a FlowDoc upstream update changes template paths or output conventions:
1. Compare the new upstream paths against this mapping.
2. Update this document if paths changed.
3. Re-run `flowdoc-review` to validate template compatibility.
4. Update version pins in [`docs/20-flowdoc-ecosystem.md`](../../docs/20-flowdoc-ecosystem.md).
