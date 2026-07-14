{
  "$schema": "https://opencode.ai/config.json",
  "instructions": [
    "./flowforge/AGENTS.md"
  ],
  "agent": {
    "flowforge": {
      "description": "FlowForge Orchestrator — 6 fases, 7 agentes. Coordina el flujo CKP-0 → CKP-4.",
      "mode": "primary",
      "model": "__FLOWFORGE_MODEL__",
      "permission": {
        "edit": "allow",
        "write": "allow",
        "read": "allow",
        "bash": "allow",
        "task": "allow"
      },
      "prompt": "{file:./agents/flowforge.md}",
      "skills": [
        "{file:$FLOWFORGE_REPO/skills/forge-orchestrator/SKILL.md}"
      ],
      "tools": [
        "task"
      ]
    },
    "forge-discovery": {
      "description": "Phase 0 — Context mapping & memory association. Hard-stops on vague requirements.",
      "mode": "subagent",
      "hidden": true,
      "model": "__FLOWFORGE_MODEL__",
      "permission": {
        "bash": "allow",
        "read": "allow",
        "edit": "deny",
        "write": "deny"
      },
      "prompt": "{file:./agents/forge-discovery.md}",
      "skills": [
        "{file:$FLOWFORGE_REPO/skills/forge-discovery/SKILL.md}"
      ],
      "tools": [
        "task"
      ]
    },
    "forge-arch": {
      "description": "Phase 1 — Writes spec.md with capability matrix (FR/NFR) and STRIDE analysis.",
      "mode": "subagent",
      "hidden": true,
      "model": "__FLOWFORGE_MODEL__",
      "permission": {
        "edit": "allow",
        "write": "allow",
        "read": "allow",
        "bash": "allow"
      },
      "prompt": "{file:./agents/forge-arch.md}",
      "skills": [
        "{file:$FLOWFORGE_REPO/skills/forge-arch/SKILL.md}"
      ],
      "tools": [
        "task"
      ]
    },
    "forge-plan": {
      "description": "Phase 2 — Breaks spec into tasks, contracts, effort estimates.",
      "mode": "subagent",
      "hidden": true,
      "model": "__FLOWFORGE_MODEL__",
      "permission": {
        "edit": "allow",
        "write": "allow",
        "read": "allow",
        "bash": "allow"
      },
      "prompt": "{file:./agents/forge-plan.md}",
      "skills": [
        "{file:$FLOWFORGE_REPO/skills/forge-plan/SKILL.md}"
      ],
      "tools": [
        "task"
      ]
    },
    "forge-dev": {
      "description": "Phase 3 — Implements plan.md, writes unit tests, runs the Ralph Wiggum loop.",
      "mode": "subagent",
      "hidden": true,
      "model": "__FLOWFORGE_MODEL__",
      "permission": {
        "edit": "allow",
        "write": "allow",
        "read": "allow",
        "bash": "allow"
      },
      "prompt": "{file:./agents/forge-dev.md}",
      "skills": [
        "{file:$FLOWFORGE_REPO/skills/forge-dev/SKILL.md}"
      ],
      "tools": [
        "task"
      ]
    },
    "forge-verify": {
      "description": "Phase 3 — Audits implementation, generates verify-report.md with PASS / REWORK verdict.",
      "mode": "subagent",
      "hidden": true,
      "model": "__FLOWFORGE_MODEL__",
      "permission": {
        "edit": "allow",
        "write": "allow",
        "read": "allow",
        "bash": "allow"
      },
      "prompt": "{file:./agents/forge-verify.md}",
      "skills": [
        "{file:$FLOWFORGE_REPO/skills/forge-verify/SKILL.md}"
      ],
      "tools": [
        "task"
      ]
    },
    "forge-memory": {
      "description": "Phase 4 — Session closure, session summary, ADR promotion.",
      "mode": "subagent",
      "hidden": true,
      "model": "__FLOWFORGE_MODEL__",
      "permission": {
        "edit": "allow",
        "write": "allow",
        "read": "allow",
        "bash": "allow"
      },
      "prompt": "{file:./agents/forge-memory.md}",
      "skills": [
        "{file:$FLOWFORGE_REPO/skills/forge-memory/SKILL.md}"
      ],
      "tools": [
        "task"
      ]
    },
    "forge-teacher": {
      "description": "Teacher mode — explains FlowForge concepts on demand. Read-only.",
      "mode": "subagent",
      "hidden": true,
      "model": "__FLOWFORGE_MODEL__",
      "permission": {
        "read": "allow",
        "bash": "deny",
        "edit": "deny",
        "write": "deny"
      },
      "prompt": "{file:./agents/forge-teacher.md}",
      "skills": [
        "{file:$FLOWFORGE_REPO/skills/forge-teacher/SKILL.md}"
      ],
      "tools": [
        "task"
      ]
    }
  },
  "provider": {
    "opencode-zen": {
      "api": "https://opencode.ai/zen/v1",
      "npm": "@ai-sdk/openai-compatible",
      "models": [
        "big-pickle",
        "deepseek-v4-flash-free",
        "mimo-v2.5-free",
        "mimo-v2-pro-free",
        "north-mini-code-free",
        "nemotron-3-ultra-free",
        "nemotron-3-super-free",
        "minimax-m2.5-free"
      ]
    },
    "opencode-go": {
      "id": "opencode-go",
      "name": "OpenCode Go - Suscripción",
      "npm": "@ai-sdk/openai-compatible",
      "api": "https://opencode.ai/zen/go/v1",
      "env": [
        "OPENCODIGO_API_KEY"
      ],
      "description": "Conjunto paid de 17+ modelos para suscriptores OpenCode Go",
      "models": {
        "glm-5.1": {
          "name": "GLM 5.1",
          "reasoning": false,
          "structured_output": true,
          "tool_call": true
        },
        "glm-5.2": {
          "name": "GLM 5.2",
          "reasoning": false,
          "structured_output": true,
          "tool_call": true
        },
        "kimi-k2.5": {
          "name": "Kimi K2.5",
          "reasoning": false,
          "structured_output": true,
          "tool_call": true
        },
        "kimi-k2.6": {
          "name": "Kimi K2.6",
          "reasoning": false,
          "structured_output": true,
          "tool_call": true
        },
        "kimi-k2.7-code": {
          "name": "Kimi K2.7 Code",
          "reasoning": false,
          "structured_output": true,
          "tool_call": true
        },
        "mimo-v2.5": {
          "name": "MiMo V2.5",
          "reasoning": false,
          "structured_output": true,
          "tool_call": true
        },
        "mimo-v2.5-pro": {
          "name": "MiMo V2.5 Pro",
          "reasoning": false,
          "structured_output": true,
          "tool_call": true
        },
        "minimax-m2.3": {
          "name": "MiniMax M2.3",
          "reasoning": false,
          "structured_output": true,
          "tool_call": true
        },
        "minimax-m2.5": {
          "name": "MiniMax M2.5",
          "reasoning": false,
          "structured_output": true,
          "tool_call": true
        },
        "minimax-m2.7": {
          "name": "MiniMax M2.7",
          "reasoning": false,
          "structured_output": true,
          "tool_call": true
        },
        "minimax-m3": {
          "name": "MiniMax M3",
          "reasoning": false,
          "structured_output": true,
          "tool_call": true
        },
        "deepseek-v4-flash": {
          "name": "DeepSeek V4 Flash",
          "reasoning": false,
          "structured_output": true,
          "tool_call": true
        },
        "deepseek-v4-pro": {
          "name": "DeepSeek V4 Pro",
          "reasoning": false,
          "structured_output": true,
          "tool_call": true
        },
        "qwen3.5-plus": {
          "name": "Qwen3.5 Plus",
          "reasoning": false,
          "structured_output": true,
          "tool_call": true
        },
        "qwen3.6-plus": {
          "name": "Qwen3.6 Plus",
          "reasoning": false,
          "structured_output": true,
          "tool_call": true
        },
        "qwen3.7-plus": {
          "name": "Qwen3.7 Plus",
          "reasoning": false,
          "structured_output": true,
          "tool_call": true
        },
        "qwen3.7-max": {
          "name": "Qwen3.7 Max",
          "reasoning": false,
          "structured_output": true,
          "tool_call": true
        }
      }
    }
  },
  "permission": {
    "bash": {
      "allow": [
        "*"
      ],
      "deny": []
    },
    "read": {
      "allow": [
        "*"
      ],
      "deny": [
        "**/.env",
        "**/credentials.json",
        "**/secrets/**"
      ]
    }
  },
  "mcp": {
    "engram": {
      "type": "local",
      "enabled": true,
      "command": "$FLOWFORGE_ENGRAM_BIN",
      "environment": {
        "ENGRAM_USER": "$USER"
      },
      "options": {
        "data_dir": "$FLOWFORGE_REPO/.engram"
      }
    }
  }
}
