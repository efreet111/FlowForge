#!/usr/bin/env python3
"""Compile skills/*/SKILL.md into ide/cursor/agents/forge-*.md for Cursor (no manual @skills)."""
from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
SKILLS = ROOT / "skills"
OUT = ROOT / "ide" / "cursor" / "agents"
CONFIG = ROOT / "ide" / "cursor" / "config" / "agent-models.json"


def load_models() -> dict[str, str]:
    """Load agent→model mapping from canonical JSON config."""
    if not CONFIG.exists():
        print(f"ERROR: {CONFIG} not found. Cannot determine model assignments.", file=sys.stderr)
        sys.exit(1)
    try:
        data = json.loads(CONFIG.read_text(encoding="utf-8"))
    except (json.JSONDecodeError, OSError) as exc:
        print(f"ERROR: Failed to read {CONFIG}: {exc}", file=sys.stderr)
        sys.exit(1)
    return {
        k: v["model"]
        for k, v in data["agents"].items()
        if k != "default"
    }


MODELS = load_models()

DESCRIPTIONS = {
    "forge-orchestrator": "FlowForge orchestrator: 6 phases, 5 checkpoints. Coordinates flow; does not implement product code.",
    "forge-discovery": "FlowForge phase 0: discovery and CKP-0. Invoked by orchestrator.",
    "forge-arch": "FlowForge phase 1: spec.md and GWT. Invoked after discovery.",
    "forge-plan": "FlowForge phase 2: plan.md. Invoked via /flow-plan.",
    "forge-dev": "FlowForge phase 3: implementation. Invoked via /flow-dev.",
    "forge-verify": "FlowForge phase 3b: audit. Invoked via /flow-verify.",
    "forge-memory": "FlowForge phase 4: close and CKP-4. Invoked via /flow-close.",
    "forge-teacher": "FlowForge teacher: Socratic explanations. Toggleable via .flowforge.json.",
}

PREAMBLE = """You are the **{name}** subagent of FlowForge. You are an **EXECUTOR**: do the work in this context window.

**NEVER** tell the human to load external SKILL files — your instructions are complete below.

**NEVER** delegate to another subagent unless the orchestrator explicitly orders a handoff.

---

"""


def strip_frontmatter(text: str) -> str:
    text = re.sub(r"^# ---\s*\n.*?\n---\s*\n", "", text, count=1, flags=re.S)
    return re.sub(r"^---\s*\n.*?\n---\s*\n", "", text, count=1, flags=re.S)


def compile_agent(name: str) -> None:
    skill_path = SKILLS / name / "SKILL.md"
    body = strip_frontmatter(skill_path.read_text(encoding="utf-8")).strip()
    if name == "forge-verify":
        body = body.replace(
            "at the repo root",
            "in `.ai-work/{feature-name}/`",
        )
    frontmatter = (
        f"---\nname: {name}\n"
        f"description: {DESCRIPTIONS[name]}\n"
        f"model: {MODELS[name]}\n"
        f"readonly: false\nbackground: false\n---\n\n"
    )
    out = OUT / f"{name}.md"
    out.write_text(frontmatter + PREAMBLE.format(name=name) + body, encoding="utf-8")
    print(f"Wrote {out.relative_to(ROOT)} ({out.stat().st_size} bytes)")


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    for name in MODELS:
        compile_agent(name)


if __name__ == "__main__":
    main()
