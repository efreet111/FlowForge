#!/usr/bin/env bash
# FlowForge Dev Installer — local development setup script
# Installs FlowForge skills to IDEs without requiring .NET or GitHub
#
# Usage:
#   ./install/dev/install-dev.sh              # Interactive TUI
#   ./install/dev/install-dev.sh --ide opencode  # Non-interactive
#
set -euo pipefail

# ── Colors ────────────────────────────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m'

# ── Paths ────────────────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
SKILLS_DIR="$REPO_ROOT/skills"

# ── IDE destinations (ordered list) ──────────────────────────────────────────────────
IDE_KEYS=(opencode cursor antigravity vscode)

declare -A IDE_PATHS
IDE_PATHS[opencode]="$HOME/.config/opencode"
IDE_PATHS[cursor]="$HOME/.cursor"
IDE_PATHS[antigravity]="$HOME/.gemini"
IDE_PATHS[vscode]="$HOME/.copilot"

declare -A IDE_LABELS
IDE_LABELS[opencode]="OpenCode"
IDE_LABELS[cursor]="Cursor"
IDE_LABELS[antigravity]="Antigravity"
IDE_LABELS[vscode]="VS Code (Copilot)"

# ── FlowForge managed block (hardcoded, same as C# implementation) ─────────────
FORGE_BLOCK_OPEN="<!-- gentle-ai:flowforge -->"
FORGE_BLOCK_CLOSE="<!-- /gentle-ai:flowforge -->"
FORGE_BLOCK_HASH="<!-- hash: a1b2c3d4e5f6... -->"  # Placeholder, recalculated on install

FORGE_PREFLIGHT='## Pre-flight: AGENTS.md first (mandatory)

Before reading Engram memory, `.ai-work/`, or `.engram.json`, you MUST read `AGENTS.md` at the repo root.
This is the authoritative skill index, checkpoint contract, and skill path registry.

**Startup order (deterministic — never reorder):**
1. `AGENTS.md` — skill index + checkpoint contract + skill paths
2. Local files — `.ai-work/{feature-slug}/`, `.flowforge.json`
3. Engram memory — secondary reference only; never a substitute for local state

If Engram memory is unavailable or stale, proceed from AGENTS.md + local files alone.
Never block or misroute because memory is absent.'

# ── Helpers ───────────────────────────────────────────────────────────────────
info()    { echo -e "${CYAN}[INFO]${NC} $*"; }
success() { echo -e "${GREEN}[OK]${NC} $*"; }
warn()    { echo -e "${YELLOW}[WARN]${NC} $*"; }
error()   { echo -e "${RED}[ERROR]${NC} $*" >&2; }

# ── Check prerequisites ───────────────────────────────────────────────────────
check_prereqs() {
    if [[ ! -d "$SKILLS_DIR" ]]; then
        error "Skills directory not found: $SKILLS_DIR"
        error "Run this script from the FlowForge repo root."
        exit 1
    fi

    if [[ ! -f "$REPO_ROOT/AGENTS.md" ]]; then
        warn "AGENTS.md not found at repo root. Will install without AGENTS.md merge."
    fi
}

# ── TUI: IDE selection ────────────────────────────────────────────────────────
select_ide() {
    local ide=""
    local selection=""

    local separator
    separator=$(printf '─%.0s' $(seq 1 50))
    echo ""
    echo -e "${BOLD}FlowForge Dev Installer${NC}"
    echo -e "${CYAN}${separator}${NC}"
    echo ""
    echo "Select IDE to install FlowForge skills:"
    echo ""

    local i=1
    for key in "${IDE_KEYS[@]}"; do
        echo "  $i) ${IDE_LABELS[$key]}"
        i=$((i+1))
    done
    echo ""

    while true; do
        echo -n "Selection (1-${#IDE_KEYS[@]}, or IDE name): "
        read -r selection

        # Try numeric selection
        if [[ "$selection" =~ ^[0-9]+$ ]]; then
            local idx=$((selection - 1))
            if [[ "$idx" -ge 0 && "$idx" -lt ${#IDE_KEYS[@]} ]]; then
                ide="${IDE_KEYS[$idx]}"
            fi
        else
            # Try by name
            ide="$(echo "$selection" | tr '[:upper:]' '[:lower:]')"
        fi

        if [[ -n "${IDE_PATHS[$ide:-undefined]:-}" ]]; then
            break
        fi

        error "Invalid selection: $selection"
        echo "Try again or enter an IDE name (opencode, cursor, antigravity, vscode)."
    done

    IDE_SELECTED="$ide"
    IDE_PATH="${IDE_PATHS[$ide]}"
    IDE_LABEL="${IDE_LABELS[$ide]}"
}

# ── TUI: Backup confirmation ──────────────────────────────────────────────────
confirm_backup() {
    echo ""
    echo -e "${BOLD}Backup${NC}"
    echo ""
    echo -e "IDE path: ${CYAN}$IDE_PATH${NC}"

    if [[ -d "$IDE_PATH" ]]; then
        local file_count
        file_count=$(find "$IDE_PATH" -type f 2>/dev/null | wc -l)
        echo -e "Files in directory: ${YELLOW}$file_count${NC}"
    else
        echo -e "Directory does not exist yet."
    fi

    echo ""
    echo -n "Create backup before installing? [Y/n]: "
    read -r response

    case "${response,,}" in
        n|no) BACKUP_ENABLED=0 ;;
        *)    BACKUP_ENABLED=1 ;;
    esac
}

# ── Backup ───────────────────────────────────────────────────────────────────
do_backup() {
    if [[ "$BACKUP_ENABLED" -eq 0 ]]; then
        info "Backup skipped by user."
        return 0
    fi

    if [[ ! -d "$IDE_PATH" ]]; then
        info "Directory does not exist, nothing to backup."
        return 0
    fi

    local backup_root="$HOME/.flowforge-backups"
    local timestamp
    timestamp="$(date +%Y%m%d-%H%M%S)"
    local backup_dir="$backup_root/dev-install-${IDE_SELECTED}-${timestamp}"

    info "Creating backup at: $backup_dir"

    mkdir -p "$backup_root"
    if cp -r "$IDE_PATH" "$backup_dir" 2>/dev/null; then
        success "Backup created: $backup_dir"

        # Prune old backups (keep max 5)
        local backup_count
        backup_count=$(find "$backup_root" -maxdepth 1 -type d -name "dev-install-${IDE_SELECTED}-*" 2>/dev/null | wc -l)
        if [[ "$backup_count" -gt 5 ]]; then
            local to_delete
            to_delete=$(find "$backup_root" -maxdepth 1 -type d -name "dev-install-${IDE_SELECTED}-*" 2>/dev/null | sort | head -n -5)
            echo "$to_delete" | while read -r dir; do
                rm -rf "$dir"
                info "Pruned old backup: $dir"
            done
        fi
    else
        warn "Backup failed. Continuing anyway..."
    fi
}

# ── Copy skills ───────────────────────────────────────────────────────────────
copy_skills() {
    local dest="$IDE_PATH"
    local skill_count=0

    info "Installing FlowForge skills to: $dest"

    # Create destination directories based on IDE
    case "$IDE_SELECTED" in
        opencode)
            mkdir -p "$dest/skills" 2>/dev/null || true
            mkdir -p "$dest/agents" 2>/dev/null || true
            ;;
        cursor)
            mkdir -p "$dest/rules" 2>/dev/null || true
            mkdir -p "$dest/agents" 2>/dev/null || true
            mkdir -p "$dest/commands" 2>/dev/null || true
            ;;
        antigravity)
            mkdir -p "$dest/config/rules" 2>/dev/null || true
            mkdir -p "$dest/config/workflows" 2>/dev/null || true
            mkdir -p "$dest/config/skills" 2>/dev/null || true
            ;;
        vscode)
            mkdir -p "$dest/agents" 2>/dev/null || true
            mkdir -p "$dest/instructions" 2>/dev/null || true
            ;;
    esac

    # Copy skills from repo
    if [[ -d "$SKILLS_DIR" ]]; then
        for skill_dir in "$SKILLS_DIR"/forge-*; do
            if [[ -d "$skill_dir" ]]; then
                local skill_name
                skill_name="$(basename "$skill_dir")"

                case "$IDE_SELECTED" in
                    opencode)
                        cp -r "$skill_dir" "$dest/skills/" 2>/dev/null && skill_count=$((skill_count+1))
                        ;;
                    cursor|vscode|antigravity)
                        if [[ -f "$skill_dir/SKILL.md" ]]; then
                            cp "$skill_dir/SKILL.md" "$dest/agents/" 2>/dev/null && skill_count=$((skill_count+1))
                        fi
                        ;;
                esac
            fi
        done
    fi

    success "Installed $skill_count skills."
}

# ── Merge AGENTS.md ───────────────────────────────────────────────────────────
merge_agents_md() {
    local agents_md="$IDE_PATH/AGENTS.md"

    info "Merging AGENTS.md..."

    # Build the managed block
    local managed_block="${FORGE_BLOCK_OPEN}
${FORGE_BLOCK_HASH}

${FORGE_PREFLIGHT}
${FORGE_BLOCK_CLOSE}
"

    # Check if AGENTS.md exists
    if [[ ! -f "$agents_md" ]]; then
        # Create new with just the FlowForge block
        echo -e "$managed_block" > "$agents_md"
        success "Created new AGENTS.md with FlowForge block."
        return 0
    fi

    # Check if already has the block (idempotency)
    if grep -q "${FORGE_BLOCK_OPEN}" "$agents_md" 2>/dev/null; then
        info "AGENTS.md already has FlowForge block. Skipping."
        return 0
    fi

    # Backup original
    cp "$agents_md" "$agents_md.bak" 2>/dev/null || true

    # Find Engram Protocol marker position
    local engram_line
    engram_line=$(grep -n "<!-- gentle-ai:engram-protocol -->" "$agents_md" 2>/dev/null | head -1 | cut -d: -f1 || echo "0")

    if [[ "$engram_line" -gt 0 ]]; then
        # Insert before Engram Protocol
        local before after
        before=$(head -n $((engram_line - 1)) "$agents_md")
        after=$(tail -n +$engram_line "$agents_md")
        echo -e "${before}\n${managed_block}\n${after}" > "$agents_md"
    else
        # No Engram Protocol found — prepend at start
        local original
        original=$(cat "$agents_md")
        echo -e "${managed_block}\n${original}" > "$agents_md"
    fi

    success "AGENTS.md merged successfully."
}

# ── Non-interactive mode ──────────────────────────────────────────────────────
run_non_interactive() {
    local ide="${1:-}"

    # Validate IDE
    if [[ -z "${IDE_PATHS[$ide]:-}" ]]; then
        error "Unknown IDE: $ide"
        echo "Available: ${!IDE_PATHS[@]}"
        exit 1
    fi

    IDE_SELECTED="$ide"
    IDE_PATH="${IDE_PATHS[$ide]}"
    IDE_LABEL="${IDE_LABELS[$ide]}"
    BACKUP_ENABLED=1  # Default to backup in non-interactive

    echo ""
    info "Installing FlowForge to $IDE_LABEL (non-interactive mode)..."
    echo ""

    check_prereqs
    do_backup
    copy_skills
    merge_agents_md

    echo ""
    success "Installation complete!"
    echo ""
}

# ── Main ───────────────────────────────────────────────────────────────────────
main() {
    # Parse flags
    local non_interactive=""
    local ide_arg=""

    while [[ $# -gt 0 ]]; do
        case "$1" in
            --ide)
                ide_arg="$2"
                shift 2
                ;;
            --help|-h)
                echo "Usage: $0 [--ide <ide>] [--help]"
                echo ""
                echo "Options:"
                echo "  --ide <name>    Install to specific IDE (opencode, cursor, antigravity, vscode)"
                echo "  --help, -h      Show this help"
                echo ""
                echo "IDEs: opencode, cursor, antigravity, vscode"
                exit 0
                ;;
            *)
                shift
                ;;
        esac
    done

    if [[ -n "$ide_arg" ]]; then
        run_non_interactive "$ide_arg"
    else
        check_prereqs
        select_ide
        confirm_backup
        echo ""

        do_backup
        copy_skills
        merge_agents_md

        echo ""
        success "Installation complete!"
        echo ""
        echo -e "${BOLD}Next steps:${NC}"
        echo "  Restart your IDE to load the new skills."
        echo ""
    fi
}

main "$@"
