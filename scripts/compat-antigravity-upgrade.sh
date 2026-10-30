#!/usr/bin/env bash
# Regression: upgrading an EXISTING FlowForge Antigravity install must not lose
# anything the previous installer wrote, and must be idempotent.
#
# Covers two real "previously installed" shapes:
#   A. Old FlowForge 0.5.x state: config/rules with the inert Cursor key,
#      global_workflows/flow-*.md, config/skills/forge-* as copies.
#   B. Older generation: ~/.gemini/antigravity/{AGENTS.md,rules,workflows},
#      config/workflows/, config/skills.json and dangling skill symlinks
#      (clone moved) — the case that aborted the installer on 2026-10-29.
#
# ADR-017 (installer protection policy) + ADR-019 Amendment.
# Usage: bash scripts/compat-antigravity-upgrade.sh
set -euo pipefail

REPO_ROOT="${1:-$(cd "$(dirname "$0")/.." && pwd)}"
TMP="$(mktemp -d /tmp/ff-compat.XXXXXX)"
WORK="$TMP/repo"
HOME_A="$TMP/home-a"
HOME_B="$TMP/home-b"
fails=0

cleanup() { rm -rf "$TMP"; }
trap cleanup EXIT

ok()   { echo "  ok   $1"; }
fail() { echo "  FAIL $1"; fails=$((fails + 1)); }
check(){ if eval "$2"; then ok "$1"; else fail "$1"; fi; }

# Work on a copy: the installer recompiles repo files (Cursor agents) and we
# must never touch the developer's working tree.
mkdir -p "$WORK"
if command -v rsync >/dev/null 2>&1; then
  rsync -a --exclude '.git' --exclude '*.bundle' --exclude 'bin/' --exclude 'obj/' \
        "$REPO_ROOT"/ "$WORK"/ 2>/dev/null
else
  cp -a "$REPO_ROOT"/. "$WORK"/ 2>/dev/null || true
  rm -rf "$WORK/.git"
fi

run_install() {
  local home="$1"
  HOME="$home" PATH="/usr/bin:/bin:/usr/sbin:/sbin" \
    bash "$WORK/ide/install.sh" >"$TMP/install.log" 2>&1
}

fingerprint() {
  local root="$1"
  {
    find "$root" -path "$root/.flowforge-backups" -prune -o -type d -print0 |
      while IFS= read -r -d '' d; do echo "DIR  ${d#"$root"}"; done
    find "$root" -path "$root/.flowforge-backups" -prune -o -type l -print0 |
      while IFS= read -r -d '' f; do echo "LINK ${f#"$root"} -> $(readlink "$f")"; done
    find "$root" -path "$root/.flowforge-backups" -prune -o -type f -print0 |
      while IFS= read -r -d '' f; do
        echo "FILE ${f#"$root"} $(sha256sum "$f" | cut -c1-16)"
      done
  } | sort
}

# ── Shape A: FlowForge 0.5.x state ──────────────────────────────────────────
echo "A. estado FlowForge 0.5.x (rules alwaysApply + global_workflows)"
mkdir -p "$HOME_A/.gemini/config"/{rules,global_workflows,skills}
printf -- '---\nalwaysApply: true\ndescription: old\n---\n# old rule\n' \
  > "$HOME_A/.gemini/config/rules/workflow.md"
for wf in flow-start flow-plan flow-dev; do
  printf -- '---\ndescription: %s old\n---\n\n# /%s body\n' "$wf" "$wf" \
    > "$HOME_A/.gemini/config/global_workflows/$wf.md"
done
# Custom user workflow: the pack does not define it, so migration must keep it.
printf -- '---\ndescription: deploy custom\n---\n\n# /flow-deploy-custom body\n' \
  > "$HOME_A/.gemini/config/global_workflows/flow-deploy-custom.md"
mkdir -p "$HOME_A/.gemini/config/skills/forge-dev"
printf -- '---\nname: forge-dev\ndescription: old skill\n---\n\n# body\n' \
  > "$HOME_A/.gemini/config/skills/forge-dev/SKILL.md"

run_install "$HOME_A"
CFG_A="$HOME_A/.gemini/config"

check "no quedan global_workflows/"          "[ ! -d '$CFG_A/global_workflows' ]"
check "rules migradas a trigger:"            "grep -q '^trigger: always_on' '$CFG_A/rules/workflow.md'"
check "sin alwaysApply en rules"             "! grep -q 'alwaysApply' '$CFG_A/rules/workflow.md'"
check "/flow-* disponibles como skills"      "[ -f '$CFG_A/skills/flow-start/SKILL.md' ] && [ -f '$CFG_A/skills/flow-plan/SKILL.md' ]"
check "cuerpo del pack (comando gestionado)"   "grep -q '# /flow-start' '$CFG_A/skills/flow-start/SKILL.md'"
check "workflow custom preservado como skill"  "grep -q '# /flow-deploy-custom body' '$CFG_A/skills/flow-deploy-custom/SKILL.md'"
check "skill preexistente conservada"        "[ -f '$CFG_A/skills/forge-dev/SKILL.md' ]"
check "skill flowforge (orquestador)"        "[ -f '$CFG_A/skills/flowforge/SKILL.md' ]"
check "7 subagentes instalados"              "[ \$(ls '$CFG_A/agents'/forge-*.md '$CFG_A/agents/flowforge.md' 2>/dev/null | wc -l) -eq 7 ]"
check "espejo workspace sincronizado"        "[ -f '$CFG_A/.agents/skills/flow-start/SKILL.md' ] && [ -f '$CFG_A/.agents/agents/forge-dev.md' ]"
check "GEMINI.md sin frontmatter"            "! head -1 '$HOME_A/.gemini/GEMINI.md' | grep -q '^---$'"

fp1="$(fingerprint "$HOME_A")"
run_install "$HOME_A"
fp2="$(fingerprint "$HOME_A")"
check "idempotente (2da corrida sin cambios)" "[ \"\$fp1\" = \"\$fp2\" ]"

# ── Shape B: generación legacy + symlinks rotos ─────────────────────────────
echo "B. generación legacy (pack antigravity/, skills.json, symlinks rotos)"
mkdir -p "$HOME_B/.gemini"/antigravity/{rules,workflows}
mkdir -p "$HOME_B/.gemini/config"/{rules,workflows,skills}
echo "# legacy AGENTS" > "$HOME_B/.gemini/antigravity/AGENTS.md"
printf -- '---\nalwaysApply: true\ndescription: old\n---\n# old\n' \
  > "$HOME_B/.gemini/antigravity/rules/workflow.md"
printf -- '---\ndescription: x\n---\n# /flow-start\n' \
  > "$HOME_B/.gemini/antigravity/workflows/flow-start.md"
cp "$HOME_B/.gemini/antigravity/workflows/flow-start.md" "$HOME_B/.gemini/config/workflows/"
echo '{"entries":[{"path":"/old"}]}' > "$HOME_B/.gemini/config/skills.json"
ln -s /nonexistent/moved-clone/skills/flow-start "$HOME_B/.gemini/config/skills/flow-start"
ln -s /nonexistent/moved-clone/skills/forge-discovery "$HOME_B/.gemini/config/skills/forge-discovery"

if run_install "$HOME_B"; then
  CFG_B="$HOME_B/.gemini/config"
  ok "el instalador no aborta con symlinks rotos"
  check "pack legacy eliminado"             "[ ! -f '$HOME_B/.gemini/antigravity/AGENTS.md' ]"
  check "rules/workflows legacy eliminados" "[ ! -d '$HOME_B/.gemini/antigravity/rules' ] && [ ! -d '$HOME_B/.gemini/antigravity/workflows' ]"
  check "skills.json eliminado"             "[ ! -f '$CFG_B/skills.json' ]"
  check "config/workflows migrado"          "[ ! -d '$CFG_B/workflows' ] && [ -f '$CFG_B/skills/flow-start/SKILL.md' ]"
  check "symlink roto reemplazado"          "[ -f '$CFG_B/skills/flow-start/SKILL.md' ] && [ ! -L '$CFG_B/skills/flow-start' -o -f '$CFG_B/skills/flow-start/SKILL.md' ]"
  check "sin symlinks rotos"                "[ \$(find '$HOME_B/.gemini' -xtype l | wc -l) -eq 0 ]"
else
  fail "el instalador abortó con symlinks rotos (ver estado legacy)"
  tail -5 "$TMP/install.log" | sed 's/^/       /'
fi

# ── No regresión en otros IDEs ──────────────────────────────────────────────
echo "C. otros IDEs intactos"
mkdir -p "$HOME_A/.cursor/rules"
echo "user-owned" > "$HOME_A/.cursor/rules/user.mdc"
run_install "$HOME_A"
check "archivos de usuario preservados"     "grep -qx 'user-owned' '$HOME_A/.cursor/rules/user.mdc'"

echo
if [ "$fails" -gt 0 ]; then
  echo "compat-antigravity-upgrade: $fails check(s) FAILED"
  exit 1
fi
echo "compat-antigravity-upgrade: all checks passed"
