#!/usr/bin/env bash
# =============================================================================
# fforge-init.sh — deterministic engine for the /fforge-init skill.
#
# CONTRACT (single source of truth): ../contracts.md
#   C-1 skill frontmatter   C-2 section model      C-3 plan.json invariants
#   C-4 classification      C-5 managed block      C-6 fragment manifest
#   C-7 base template       C-7a skills union      C-7b title rule
#   C-8 CLI + exit codes    C-9 backup/rollback    C-10 source-changed guard
#
# USAGE
#   fforge-init.sh detect    [--root <dir>] [--json]
#   fforge-init.sh analyze   [--file <path>] [--root <dir>] [--json]
#   fforge-init.sh classify  [--from-analysis <path.json>] [--names <csv>] [--json]
#   fforge-init.sh recommend [--root <dir>] [--agent <name>] [--json]
#   fforge-init.sh preview   --config <plan.json> [--json] [--allow-review-todos]
#   fforge-init.sh apply     --config <plan.json> [--dry-run] [--allow-review-todos] [--verbose] [--json]
#   fforge-init.sh restore   [--root <dir>] [--dry-run] [--json]
#   fforge-init.sh version
#   fforge-init.sh help
#
# EXIT CODES (frozen — contracts.md C-8)
#   0  success (also a dry-run or an idempotent no-op)   4  unresolved external skills (flow blocked)
#   1  usage error / unknown flag / missing argument     5  unresolved conflict / ambiguous block / source changed
#   2  target not found                                  6  invalid plan.json (schema or invariant)
#   3  permission denied / read-only FS / path escape    7  write failed mid-apply (backup preserved, target restored)
#
# Every exit code >= 1 prints an actionable message on stderr naming
# what failed, why, and how to fix it (NFR-002).
#
# RUNTIME: bash 4+, awk, sed, grep, find, sha256sum (or shasum -a 256).
# No network, no jq, no node, no npm, no git — NFR-006 self-containment.
# =============================================================================
set -u

PROG="fforge-init.sh"
FALLBACK_VERSION="1.0.0"
MAX_FILE_SIZE=1048576
BACKUP_NAME=".agents.md.backup"
STAGE_WRITE="AGENTS.md.tmp"
ANCHOR='<!-- fforge-init:fragments -->'

SCRIPT_DIR=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
SKILL_DIR=$(cd -- "$SCRIPT_DIR/.." && pwd)
TEMPLATES_DIR="$SKILL_DIR/templates"
BASE_TEMPLATE="$TEMPLATES_DIR/base/AGENTS.md.base"
BASE_META="$TEMPLATES_DIR/base/base.meta.json"
FRAGMENTS_DIR="$TEMPLATES_DIR/fragments"

# ---------------------------------------------------------------------------
# Output helpers
# ---------------------------------------------------------------------------

err() { printf '%s\n' "$*" >&2; }

# fail <code> <what> <why> <fix>
fail() {
  local code="$1" what="$2" why="$3" fix="$4"
  printf '%s: ERROR (exit %s) — %s\n' "$PROG" "$code" "$what" >&2
  printf '  Why: %s\n' "$why" >&2
  printf '  Fix: %s\n' "$fix" >&2
  exit "$code"
}

usage_err() {
  fail 1 "$1" "${2:-the arguments passed to $PROG are not valid}" \
       "run \"$PROG help\" to see the usage and the exit-code table"
}

warn() { printf '⚠ %s\n' "$*" >&2; }

# ---------------------------------------------------------------------------
# Small utilities
# ---------------------------------------------------------------------------

trim() {
  local s="$1"
  s="${s#"${s%%[![:space:]]*}"}"
  s="${s%"${s##*[![:space:]]}"}"
  printf '%s' "$s"
}

lower() { printf '%s' "${1,,}"; }

abs_path() { # resolve to an absolute, normalised path (no symlink resolution)
  local p="$1"
  case "$p" in
    /*) printf '%s' "$p" ;;
    *) printf '%s/%s' "$(pwd)" "${p#./}" ;;
  esac
}

path_is_under() { # $1 path, $2 root  -> 0 when $1 is $2 or under it
  case "$1" in
    "$2") return 0 ;;
    "$2"/*) return 0 ;;
    *) return 1 ;;
  esac
}

has_glob() { case "$1" in *'*'*|*'?'*|*'['*) return 0 ;; *) return 1 ;; esac; }

SHA_TOOL=""
sha_tool() {
  [ -n "$SHA_TOOL" ] && return 0
  if command -v sha256sum >/dev/null 2>&1; then
    SHA_TOOL="sha256sum"
  elif command -v shasum >/dev/null 2>&1; then
    SHA_TOOL="shasum -a 256"
  fi
  return 0
}

sha256_of_file() {
  sha_tool
  if [ -z "$SHA_TOOL" ]; then
    fail 3 "cannot hash '$1'" "neither sha256sum nor shasum is available" \
         "install coreutils (sha256sum) or perl (shasum) and re-run"
  fi
  # shellcheck disable=SC2086
  $SHA_TOOL < "$1" | awk '{print $1}'
}

sha256_of_string() {
  sha_tool
  if [ -z "$SHA_TOOL" ]; then
    fail 3 "cannot hash content" "neither sha256sum nor shasum is available" \
         "install coreutils (sha256sum) or perl (shasum) and re-run"
  fi
  # shellcheck disable=SC2086
  printf '%s' "$1" | $SHA_TOOL | awk '{print $1}'
}

json_escape_str() {
  local s="$1"
  s=${s//\\/\\\\}
  s=${s//\"/\\\"}
  s=${s//$'\t'/\\t}
  s=${s//$'\r'/\\r}
  s=${s//$'\n'/\\n}
  printf '%s' "$s"
}

# split_to_array <string> <array-name>
split_to_array() {
  local -n __out="$2"
  __out=()
  [ -n "$1" ] || return 0
  local __line
  while IFS= read -r __line || [ -n "$__line" ]; do
    __out+=("$__line")
  done <<< "$1"
}

# join_from <array-name> <from-index> <to-index>  (prints lines joined by \n)
join_from() {
  local -n __arr="$1"
  local i from="$2" to="$3" out=""
  [ "$from" -lt 0 ] && from=0
  for ((i = from; i <= to && i < ${#__arr[@]}; i++)); do
    out+="${__arr[$i]}"$'\n'
  done
  printf '%s' "$out"
}

# ---------------------------------------------------------------------------
# Minimal JSON reader (no external JSON tooling — NFR-006)
#
# json_flatten prints one "path<TAB>TYPE:value" line per scalar.
#   TYPE: S string | N number | B bool | Z null
# ---------------------------------------------------------------------------

JSON_AWK='
function ws(   c) {
  while (I <= L) {
    c = substr(P, I, 1)
    if (c == " " || c == "\t" || c == "\n" || c == "\r") I++
    else break
  }
}
function bad(m) {
  printf("fforge-init.sh: invalid JSON: %s (near byte offset %d)\n", m, I) > "/dev/stderr"
  exit 2
}
function str(   c, out) {
  I++
  out = ""
  while (I <= L) {
    c = substr(P, I, 1)
    if (c == "\\") {
      I++
      c = substr(P, I, 1)
      if (c == "n") out = out "\\n"
      else if (c == "t") out = out "\\t"
      else if (c == "r") out = out "\\r"
      else if (c == "b" || c == "f") out = out " "
      else if (c == "u") { I = I + 4; out = out "?" }
      else out = out c
      I++
    } else if (c == "\"") { I++; return out }
    else { out = out c; I++ }
  }
  bad("unterminated string")
  return ""
}
function value(path,   c, tok, key) {
  ws()
  c = substr(P, I, 1)
  if (c == "{") { object(path); return }
  if (c == "[") { array(path); return }
  if (c == "\"") { print path "\tS:" str(); return }
  tok = ""
  while (I <= L) {
    c = substr(P, I, 1)
    if (c ~ /[A-Za-z0-9.+-]/) { tok = tok c; I++ } else break
  }
  if (tok == "") bad("unexpected character")
  if (tok == "true") print path "\tB:true"
  else if (tok == "false") print path "\tB:false"
  else if (tok == "null") print path "\tZ:"
  else if (tok ~ /^-?[0-9]+(\.[0-9]+)?([eE][-+]?[0-9]+)?$/) print path "\tN:" tok
  else bad("invalid literal " tok)
}
function object(path,   key, c) {
  I++
  ws()
  if (substr(P, I, 1) == "}") { I++; return }
  while (1) {
    ws()
    if (substr(P, I, 1) != "\"") bad("expected an object key")
    key = str()
    ws()
    if (substr(P, I, 1) != ":") bad("expected a colon after an object key")
    I++
    if (path == "") value(key)
    else value(path "." key)
    ws()
    c = substr(P, I, 1)
    if (c == ",") { I++; continue }
    if (c == "}") { I++; return }
    bad("expected , or }")
  }
}
function array(path,   idx, c) {
  I++
  ws()
  if (substr(P, I, 1) == "]") { I++; return }
  idx = 0
  while (1) {
    value(path "[" idx "]")
    idx++
    ws()
    c = substr(P, I, 1)
    if (c == ",") { I++; continue }
    if (c == "]") { I++; return }
    bad("expected , or ]")
  }
}
{ P = P $0 "\n" }
END {
  L = length(P)
  I = 1
  value("")
  ws()
  if (I <= L) bad("trailing content after the top-level value")
}
'

JSON_FLAT=""
JSON_SRC=""

# flat_load <file> — reads a JSON file into JSON_FLAT; exit 6 on a parse error.
flat_load() {
  local file="$1" out
  if [ ! -f "$file" ]; then
    fail 6 "config '$file' does not exist" "the engine was asked to read a JSON file that is not there" \
         "check the path (the skill builds this file in step 6) and re-run"
  fi
  if ! out=$(awk "$JSON_AWK" "$file" 2>&1); then
    err "$out"
    fail 6 "'$file' is not valid JSON" "the built-in JSON parser rejected the file at the byte offset above" \
         "re-generate plan.json from 'analyze --json', or validate the syntax with a JSON formatter"
  fi
  JSON_FLAT="$out"
  JSON_SRC="$file"
}

json_get_raw() { # $1 = path -> "TYPE:value"; rc 1 when the path is absent
  local p="$1"
  printf '%s\n' "$JSON_FLAT" | awk -v p="$p" '
    index($0, p "\t") == 1 && !found { v = substr($0, length(p) + 2); found = 1 }
    END { if (found) { print v; exit 0 } exit 1 }
  ' 2>/dev/null
}

json_get() { # $1 = path -> scalar value ("" for null); rc 1 when absent
  local raw
  raw=$(json_get_raw "$1") || return 1
  case "$raw" in
    Z:) printf '' ;;
    ?:*) printf '%s' "${raw#??}" ;;
    *) printf '%s' "$raw" ;;
  esac
}

json_has() { json_get_raw "$1" >/dev/null 2>&1; }

json_req() { # $1 = path, $2 = label for the error message
  local v
  if ! v=$(json_get "$1"); then
    fail 6 "plan.json is missing '$1'" "${2:-every plan must declare this field}" \
         "add \"$1\" to the plan (see contracts.md C-3 for the required shape)"
  fi
  printf '%s' "$v"
}

json_arr() { # $1 = array path -> one value per line, in array order (scalars only)
  local base="$1"
  printf '%s\n' "$JSON_FLAT" | awk -v p="$base[" '
    index($0, p) == 1 {
      rest = substr($0, length(p) + 1)
      i = index(rest, "]")
      if (i <= 1) next
      idx = substr(rest, 1, i - 1)
      if (idx !~ /^[0-9]+$/) next
      after = substr(rest, i + 1, 1)
      if (after != "" && after != "\t") next
      raw = substr(rest, i + 2)
      if (substr(raw, 1, 2) == "Z:") print ""
      else print substr(raw, 3)
    }
  '
}

json_arr_count() {
  local base="$1"
  printf '%s\n' "$JSON_FLAT" | awk -v p="$base[" '
    index($0, p) == 1 {
      rest = substr($0, length(p) + 1)
      i = index(rest, "]")
      if (i <= 1) next
      idx = substr(rest, 1, i - 1)
      if (idx !~ /^[0-9]+$/) next
      after = substr(rest, i + 1, 1)
      if (after != "" && after != "\t" && after != ".") next
      seen[idx] = 1
    }
    END { print length(seen) }
  '
}

# ---------------------------------------------------------------------------
# YAML frontmatter reader for base.meta.json companions and *.md.fragment
# ---------------------------------------------------------------------------

FM_AWK_SCALAR='
NR == 1 && $0 ~ /^---[ \t]*$/ { fm = 1; next }
fm && $0 ~ /^---[ \t]*$/ { exit }
!fm { next }
{
  line = $0
  sub(/[ \t\r]+$/, "", line)
  if (index(line, key ":") == 1) {
    v = substr(line, length(key) + 2)
    sub(/^[ \t]+/, "", v)
    if (v ~ /^".*"$/) v = substr(v, 2, length(v) - 2)
    print v
    exit
  }
}
'

FM_AWK_LIST='
function unq(s) { gsub(/^"/, "", s); gsub(/"$/, "", s); return s }
NR == 1 && $0 ~ /^---[ \t]*$/ { fm = 1; next }
fm && $0 ~ /^---[ \t]*$/ { exit }
!fm { next }
{
  line = $0
  sub(/[ \t\r]+$/, "", line)
  if (index(line, key ":") == 1) {
    rest = substr(line, length(key) + 2)
    sub(/^[ \t]+/, "", rest)
    if (rest ~ /^\[/) {
      sub(/^\[/, "", rest)
      sub(/\][ \t]*$/, "", rest)
      gsub(/[ \t]/, "", rest)
      n = split(rest, parts, ",")
      for (i = 1; i <= n; i++) if (parts[i] != "") print unq(parts[i])
      exit
    }
    inlist = 1
    next
  }
  if (inlist) {
    if (line ~ /^[ \t]+-[ \t]*/) {
      v = line
      sub(/^[ \t]+-[ \t]*/, "", v)
      print unq(v)
      next
    }
    if (line ~ /^[ \t]+/) next
    inlist = 0
  }
}
'

fm_scalar() { awk -v key="$2" "$FM_AWK_SCALAR" "$1"; }
fm_list() { awk -v key="$2" "$FM_AWK_LIST" "$1"; }

# strip_fm <file> — prints the body of a Markdown file with YAML frontmatter removed
strip_fm() {
  awk '
    NR == 1 && $0 ~ /^---[ \t]*$/ { fm = 1; next }
    fm { if ($0 ~ /^---[ \t]*$/) { fm = 0 } ; next }
    { print }
  ' "$1"
}

# ---------------------------------------------------------------------------
# Markdown section analysis (contracts.md C-2)
# ---------------------------------------------------------------------------

ANALYZE_AWK='
function trim(s) { gsub(/^[ \t\r]+/, "", s); gsub(/[ \t\r]+$/, "", s); return s }
function nlead(s,   n) { n = 0; while (substr(s, n + 1, 1) == "#") n++; return n }
function stripcomments(s,   out, p, q) {
  out = s
  while (1) {
    p = index(out, "<!--")
    if (p == 0) break
    q = index(substr(out, p), "-->")
    if (q == 0) { out = substr(out, 1, p - 1); break }
    out = substr(out, 1, p - 1) substr(out, p + q + 2)
  }
  return trim(out)
}
function isfence(s) { return (s ~ /^(```|~~~)[A-Za-z0-9+#._-]*$/) }
function lastcontent(a, b,   i) {
  for (i = b; i >= a; i--) if (trim(line[i]) != "") return i
  return a - 1
}
function hascontent(a, b,   i, t) {
  for (i = a; i <= b; i++) {
    t = trim(line[i])
    if (t == "") continue
    if (isfence(t)) continue
    if (stripcomments(t) != "") return 1
  }
  return 0
}
function emit_section(lvl, nm, s, e, ce,   i, t, isrow, isitem, rest, tok) {
  si++
  cnt[nm]++
  disp = nm
  if (cnt[nm] > 1) disp = nm " (" cnt[nm] - 1 ")"
  if (hascontent(s + 1, e)) empty = "false"
  else empty = "true"
  print "S\t" si "\t" lvl "\t" disp "\t" s "\t" e "\t" managed[s] "\t" empty
  split("", seen)
  for (i = s; i <= ce; i++) {
    if (fence[i]) continue
    t = trim(line[i])
    if (t == "") continue
    if (isfence(t)) continue
    if (substr(t, 1, 3) == "```" || substr(t, 1, 3) == "~~~") continue
    isrow = (index(t, "|") > 0)
    isitem = (t ~ /^([-*+]|[0-9]+\.)[ \t]/)
    if (!isrow && !isitem) continue
    rest = t
    while (match(rest, /`[^`]+`/)) {
      tok = substr(rest, RSTART + 1, RLENGTH - 2)
      rest = substr(rest, RSTART + RLENGTH)
      if (length(tok) <= 40 && tok ~ /^[a-z][a-z0-9]*(-[a-z0-9]+)+$/) {
        if (!(tok in seen)) { seen[tok] = 1; print "K\t" si "\t" tok "\t" i }
      }
    }
  }
}
{ line[NR] = $0 }
END {
  n = NR
  infence = 0; fencechar = ""; inhtml = 0; inmanaged = 0; mcount = 0
  for (i = 1; i <= n; i++) {
    t = trim(line[i])
    managed[i] = inmanaged
    if (index(t, "<!--") == 1) inhtml = 1
    if (inhtml && index(t, "-->") > 0) inhtml = 0
    if (infence) {
      if (substr(t, 1, 3) == fencechar fencechar fencechar) infence = 0
    } else if (substr(t, 1, 3) == "```" || substr(t, 1, 3) == "~~~") {
      infence = 1
      fencechar = substr(t, 1, 1)
    }
    fence[i] = infence
    if (t ~ /^<!--[ \t]*fforge-init:begin/) { mcount++; mb[mcount] = i; inmanaged = 1 }
    else if (t ~ /^<!--[ \t]*fforge-init:end/ && mcount > 0 && !(mb[mcount] in mend)) {
      mend[mb[mcount]] = i
      inmanaged = 0
    }
  }
  hk = 0
  for (i = 1; i <= n; i++) {
    if (fence[i]) continue
    t = trim(line[i])
    if (t !~ /^#+[ \t]/) continue
    lvl = nlead(t)
    nm = trim(substr(t, lvl + 1))
    sub(/[ \t]+#+[ \t]*$/, "", nm)
    nm = trim(nm)
    if (nm == "") continue
    hk++; hlevel[hk] = lvl; hname[hk] = nm; hline[hk] = i
  }
  si = 0
  if (hk == 0) {
    if (n > 0) {
      e = lastcontent(1, n)
      if (e >= 1 && hascontent(1, e)) emit_section(0, "Preamble", 1, e, e)
    }
  } else {
    if (hline[1] > 1) {
      e = lastcontent(1, hline[1] - 1)
      if (e >= 1 && hascontent(1, e)) emit_section(0, "Preamble", 1, e, e)
    }
    for (k = 1; k <= hk; k++) {
      s = hline[k]
      e = n
      ce = n
      for (j = k + 1; j <= hk; j++) {
        if (hlevel[j] <= hlevel[k]) { e = hline[j] - 1; break }
      }
      for (j = k + 1; j <= hk; j++) {
        if (hlevel[j] > hlevel[k]) { ce = hline[j] - 1; break }
      }
      if (ce > e) ce = e
      le = lastcontent(s, e)
      if (le < s) le = s
      emit_section(hlevel[k], hname[k], s, le, ce)
    }
  }
  for (m = 1; m <= mcount; m++) {
    b = mb[m]
    e = (b in mend) ? mend[b] : 0
    h = ""
    if (match(line[b], /hash=[0-9a-fA-F]+/)) h = substr(line[b], RSTART + 5, RLENGTH - 5)
    print "M\t" b "\t" e "\t" h
  }
  print "X\t" n
}
'

analyze_tsv() { awk "$ANALYZE_AWK" "$1"; }

# --- in-memory analysis state ---------------------------------------------

A_TSV=""
A_LINECOUNT=0
SEC_IDX=(); SEC_LEVEL=(); SEC_NAME=(); SEC_START=(); SEC_END=(); SEC_MANAGED=(); SEC_EMPTY=()
SK_SIDX=(); SK_NAME=(); SK_LINE=()
MB_COUNT=0; MB_BEGIN=(); MB_END=(); MB_STORED=()
FILE_LINES=()

analysis_reset() {
  A_TSV=""; A_LINECOUNT=0
  SEC_IDX=(); SEC_LEVEL=(); SEC_NAME=(); SEC_START=(); SEC_END=(); SEC_MANAGED=(); SEC_EMPTY=()
  SK_SIDX=(); SK_NAME=(); SK_LINE=()
  MB_COUNT=0; MB_BEGIN=(); MB_END=(); MB_STORED=()
  FILE_LINES=()
}

read_file_lines() { # $1 = file
  FILE_LINES=()
  local line
  while IFS= read -r line || [ -n "$line" ]; do
    FILE_LINES+=("$line")
  done < "$1"
}

# analyze_load <file> — fills the arrays above from the file at $1
analyze_load() {
  local f="$1" rec kind
  analysis_reset
  A_TSV=$(analyze_tsv "$f")
  read_file_lines "$f"
  while IFS= read -r rec; do
    [ -n "$rec" ] || continue
    kind="${rec%%$'\t'*}"
    case "$kind" in
      S)
        IFS=$'\t' read -r idx lvl nm st en mg em <<< "${rec#S$'\t'}"
        SEC_IDX+=("$idx"); SEC_LEVEL+=("$lvl"); SEC_NAME+=("$nm")
        SEC_START+=("$st"); SEC_END+=("$en"); SEC_MANAGED+=("$mg"); SEC_EMPTY+=("$em")
        ;;
      K)
        IFS=$'\t' read -r sidx nm ln <<< "${rec#K$'\t'}"
        SK_SIDX+=("$sidx"); SK_NAME+=("$nm"); SK_LINE+=("$ln")
        ;;
      M)
        IFS=$'\t' read -r b e h <<< "${rec#M$'\t'}"
        MB_COUNT=$((MB_COUNT + 1)); MB_BEGIN+=("$b"); MB_END+=("$e"); MB_STORED+=("$h")
        ;;
      X)
        A_LINECOUNT="${rec#X$'\t'}"
        ;;
    esac
  done <<< "$A_TSV"
  [ "${#SEC_IDX[@]}" -gt 0 ] || A_LINECOUNT="${#FILE_LINES[@]}"
  return 0
}

sec_count() { printf '%s' "${#SEC_IDX[@]}"; }

# sec_lookup <name> -> index (1-based) or empty
sec_lookup() {
  local want i
  want=$(lower "$(trim "$1")")
  for ((i = 0; i < ${#SEC_NAME[@]}; i++)); do
    if [ "$(lower "$(trim "${SEC_NAME[$i]}")")" = "$want" ]; then
      printf '%s' "${SEC_IDX[$i]}"
      return 0
    fi
  done
  return 1
}

sec_index_of_array() { # $1 = 1-based section id -> 0-based array index
  local want="$1" i
  for ((i = 0; i < ${#SEC_IDX[@]}; i++)); do
    if [ "${SEC_IDX[$i]}" = "$want" ]; then printf '%s' "$i"; return 0; fi
  done
  return 1
}

# skills_in_section <sid> -> one skill name per line
skills_in_section() {
  local sid="$1" i
  for ((i = 0; i < ${#SK_NAME[@]}; i++)); do
    if [ "${SK_SIDX[$i]}" = "$sid" ]; then printf '%s\n' "${SK_NAME[$i]}"; fi
  done
}

# ---------------------------------------------------------------------------
# Classification (contracts.md C-4)
# ---------------------------------------------------------------------------

MIGRATION_MAP='sdd-explore:forge-discovery
sdd-init:forge-init
sdd-new:forge-arch
sdd-continue:forge-dev
sdd-ff:forge-dev
sdd-apply:forge-dev
sdd-verify:forge-verify
sdd-archive:forge-memory'

skill_origin() { # $1 name -> flowforge|external  (contracts.md C-4)
  case "$1" in
    flowforge | forge-* | flowdoc-* | reginald-*) printf 'flowforge' ;;
    *) printf 'external' ;;
  esac
}

skill_migration() { # $1 name -> suggested equivalent or empty
  local line
  while IFS= read -r line; do
    if [ "${line%%:*}" = "$1" ]; then printf '%s' "${line#*:}"; return 0; fi
  done <<< "$MIGRATION_MAP"
  return 0
}

SKILL_VERSION=""
skill_version() {
  [ -n "$SKILL_VERSION" ] && { printf '%s' "$SKILL_VERSION"; return 0; }
  local v=""
  if [ -f "$SKILL_DIR/SKILL.md" ]; then
    v=$(fm_scalar "$SKILL_DIR/SKILL.md" version)
  fi
  [ -n "$v" ] || v="$FALLBACK_VERSION"
  SKILL_VERSION="$v"
  printf '%s' "$v"
}

# ---------------------------------------------------------------------------
# Security guards (contracts.md C-8 clarification 5)
# ---------------------------------------------------------------------------

guard_path_under_root() { # $1 absolute path, $2 absolute root
  path_is_under "$1" "$2" && return 0
  fail 3 "path '$1' escapes the project root '$2'" \
       "the engine only ever touches AGENTS.md inside the project root (path-escape rejection)" \
       "pass a --root that contains the file, or move the file back inside the project"
}

guard_target_file() { # $1 = existing file
  local f="$1" size nul
  if [ -L "$f" ]; then
    fail 3 "target '$f' is a symbolic link" \
         "following a symlink could write outside the project (and 'restore' could overwrite the link target)" \
         "replace the symlink with a regular file, or point at the real file inside the root"
  fi
  if [ ! -f "$f" ]; then
    fail 3 "target '$f' is not a regular file" "only regular files can be analysed or replaced" \
         "remove the directory/device at that path or pick another --file"
  fi
  size=$(wc -c < "$f" | tr -d ' ')
  if [ "$size" -gt "$MAX_FILE_SIZE" ]; then
    fail 3 "target '$f' is $size bytes (limit $MAX_FILE_SIZE)" \
         "an AGENTS.md larger than 1 MB is treated as suspicious (AgentsMdMerger RNF-SEC-005)" \
         "split the file, or archive the extra material and re-run"
  fi
  nul=$(tr -dc '\000' < "$f" | wc -c | tr -d ' ')
  if [ "$nul" -gt 0 ]; then
    fail 3 "target '$f' contains $nul NUL byte(s)" "the file is binary or corrupt, not UTF-8 markdown" \
         "restore it from git or rewrite it as plain UTF-8 text"
  fi
  if LC_ALL=C grep -q -F $'\xef\xbf\xbd' "$f" 2>/dev/null; then
    fail 3 "target '$f' contains U+FFFD replacement characters" \
         "the file is not valid UTF-8 (bytes were already lost at some point)" \
         "restore it from git or re-encode it as UTF-8"
  fi
  return 0
}

# ---------------------------------------------------------------------------
# Fragment helpers (contracts.md C-6)
# ---------------------------------------------------------------------------

fragment_files() { find "$FRAGMENTS_DIR" -name '*.md.fragment' -type f | LC_ALL=C sort; }

# fragment_files_ordered — stack, functionality, agent (HU-047 step 5 menu order)
fragment_files_ordered() {
  local kind d
  for kind in stack functionality agent; do
    for d in "$FRAGMENTS_DIR/$kind"/*.md.fragment; do
      [ -f "$d" ] && printf '%s\n' "$d"
    done
  done
}

fragment_body() { # $1 = fragment path -> body with trailing blank lines removed
  strip_fm "$1" | awk '{ n++; l[n] = $0 } END { while (n > 0 && l[n] ~ /^[ \t\r]*$/) n--; for (i = 1; i <= n; i++) print l[i] }'
}

pattern_matches() { # $1 = root, $2 = detect pattern
  local root="$1" pat="$2" base found
  [ -n "$pat" ] || return 1
  if ! has_glob "$pat"; then
    [ -e "$root/$pat" ] && return 0
    return 1
  fi
  base="${pat##*/}"
  [ -n "$base" ] || return 1
  found=$(find "$root" \
      \( -name .git -o -name node_modules -o -name .agents -o -name .ai-work \
         -o -name .venv -o -name build -o -name dist -o -name .gradle \) -prune -o \
      -name "$base" -print 2>/dev/null | head -n 1)
  [ -n "$found" ]
}

# ---------------------------------------------------------------------------
# detect (contracts.md C-8)
# ---------------------------------------------------------------------------

cmd_detect() {
  local root="$PWD" json=0
  while [ $# -gt 0 ]; do
    case "$1" in
      --root) [ $# -ge 2 ] || usage_err "--root needs a directory"; root="$2"; shift 2 ;;
      --json) json=1; shift ;;
      -h | --help) cmd_help; return 0 ;;
      *) usage_err "unknown argument '$1' for 'detect'" ;;
    esac
  done
  root=$(abs_path "$root")
  local target="$root/AGENTS.md"
  guard_path_under_root "$target" "$root"
  local exists=false size=0
  if [ -L "$target" ]; then
    fail 3 "target '$target' is a symbolic link" \
         "a symlinked AGENTS.md could be rewritten outside the project" \
         "replace the symlink with a regular file and re-run"
  fi
  if [ -f "$target" ]; then
    exists=true
    size=$(wc -c < "$target" | tr -d ' ')
  fi
  if [ "$json" = 1 ]; then
    printf '{\n'
    printf '  "root": "%s",\n' "$(json_escape_str "$root")"
    printf '  "exists": %s,\n' "$exists"
    printf '  "path": "./AGENTS.md",\n'
    printf '  "sizeBytes": %s\n' "$size"
    printf '}\n'
    return 0
  fi
  printf '[1/6] DETECTION\n────────────────\n'
  printf 'Searching AGENTS.md in the project...\n'
  if [ "$exists" = "true" ]; then
    printf '✓ Found: ./AGENTS.md (%s bytes)\n' "$size"
    printf '⚠ Existing file detected → step 2b (analysis)\n'
  else
    printf '✗ Not found\n'
    printf '→ Go to step 2a (base template selection)\n'
  fi
}

# ---------------------------------------------------------------------------
# analyze (contracts.md C-2)
# ---------------------------------------------------------------------------

target_rel() { printf './%s' "$(basename "$1")"; }

emit_analysis_json() { # $1 = file, $2 = rel path, $3 = size
  local f="$1" rel="$2" size="$3" i j first sha
  sha=$(sha256_of_file "$f")
  printf '{\n'
  printf '  "path": "%s",\n' "$(json_escape_str "$rel")"
  printf '  "exists": true,\n'
  printf '  "sizeBytes": %s,\n' "$size"
  printf '  "sha256": "%s",\n' "$sha"
  printf '  "lineCount": %s,\n' "$A_LINECOUNT"
  printf '  "sections": ['
  for ((i = 0; i < ${#SEC_IDX[@]}; i++)); do
    [ "$i" -gt 0 ] && printf ','
    printf '\n    {\n'
    printf '      "id": "s%02d",\n' "${SEC_IDX[$i]}"
    printf '      "name": "%s",\n' "$(json_escape_str "${SEC_NAME[$i]}")"
    printf '      "level": %s,\n' "${SEC_LEVEL[$i]}"
    printf '      "startLine": %s,\n' "${SEC_START[$i]}"
    printf '      "endLine": %s,\n' "${SEC_END[$i]}"
    printf '      "empty": %s,\n' "${SEC_EMPTY[$i]}"
    if [ "${SEC_MANAGED[$i]}" = "1" ]; then printf '      "managed": true,\n'
    else printf '      "managed": false,\n'; fi
    printf '      "skills": ['
    first=1
    for ((j = 0; j < ${#SK_NAME[@]}; j++)); do
      [ "${SK_SIDX[$j]}" = "${SEC_IDX[$i]}" ] || continue
      [ "$first" -eq 1 ] && first=0 || printf ','
      printf ' { "name": "%s", "line": %s, "origin": "%s", "migration": %s }' \
        "$(json_escape_str "${SK_NAME[$j]}")" "${SK_LINE[$j]}" \
        "$(skill_origin "${SK_NAME[$j]}")" \
        "$(local m; m=$(skill_migration "${SK_NAME[$j]}"); if [ -n "$m" ]; then printf '"%s"' "$m"; else printf 'null'; fi)"
    done
    if [ "$first" -eq 1 ]; then printf ']\n    }'; else printf ' ]\n    }'; fi
  done
  [ "${#SEC_IDX[@]}" -gt 0 ] && printf '\n  '
  printf '],\n'
  printf '  "externalSkills": ['
  first=1
  while IFS= read -r i; do
    [ -n "$i" ] || continue
    [ "$first" -eq 1 ] && first=0 || printf ', '
    printf '"%s"' "$(json_escape_str "$i")"
  done < <(all_skill_names | while IFS= read -r n; do
             [ "$(skill_origin "$n")" = "external" ] && printf '%s\n' "$n"
           done)
  printf ']\n'
  printf '}\n'
}

# all_skill_names — unique skill names found anywhere, in document order
all_skill_names() {
  local i seen=" " n
  for ((i = 0; i < ${#SK_NAME[@]}; i++)); do
    n="${SK_NAME[$i]}"
    case "$seen" in *" $n "*) continue ;; esac
    seen+="$n "
    printf '%s\n' "$n"
  done
}

cmd_analyze() {
  local root="$PWD" file="" json=0
  while [ $# -gt 0 ]; do
    case "$1" in
      --root) [ $# -ge 2 ] || usage_err "--root needs a directory"; root="$2"; shift 2 ;;
      --file) [ $# -ge 2 ] || usage_err "--file needs a path"; file="$2"; shift 2 ;;
      --json) json=1; shift ;;
      -h | --help) cmd_help; return 0 ;;
      *) usage_err "unknown argument '$1' for 'analyze'" ;;
    esac
  done
  root=$(abs_path "$root")
  local target
  if [ -n "$file" ]; then target=$(abs_path "$file"); else target="$root/AGENTS.md"; fi
  guard_path_under_root "$target" "$root"
  if [ ! -e "$target" ]; then
    fail 2 "no AGENTS.md found at '$target'" \
         "there is nothing to analyse in this project (step 2b needs an existing file)" \
         "run 'detect' first: if the file is absent, use step 2a (template selection) with mode=init"
  fi
  guard_target_file "$target"
  analyze_load "$target"
  if [ "$json" = 1 ]; then
    emit_analysis_json "$target" "$(target_rel "$target")" "$(wc -c < "$target" | tr -d ' ')"
    return 0
  fi
  local i j
  printf '[2/6] ANALYSIS OF EXISTING AGENTS.md\n────────────────────────────────────\n'
  for ((i = 0; i < ${#SEC_IDX[@]}; i++)); do
    local mark=" " ext=""
    [ "${SEC_EMPTY[$i]}" = "true" ] && mark=" " || mark="✓"
    for ((j = 0; j < ${#SK_NAME[@]}; j++)); do
      if [ "${SK_SIDX[$j]}" = "${SEC_IDX[$i]}" ] && [ "$(skill_origin "${SK_NAME[$j]}")" = "external" ]; then
        ext=" ← ⚠ EXTERNAL (non-FlowForge)"
      fi
    done
    if [ "${SEC_EMPTY[$i]}" = "true" ]; then
      printf '  [%s] %-28s (empty)%s\n' "$mark" "${SEC_NAME[$i]}" "$ext"
    else
      printf '  [%s] %-28s (lines %s-%s)%s\n' "$mark" "${SEC_NAME[$i]}" "${SEC_START[$i]}" "${SEC_END[$i]}" "$ext"
    fi
  done
  local n_ext=0 n_sec=0
  while IFS= read -r _; do n_ext=$((n_ext + 1)); done < <(
    all_skill_names | while IFS= read -r n; do [ "$(skill_origin "$n")" = "external" ] && printf '%s\n' "$n"; done
  )
  for ((i = 0; i < ${#SEC_IDX[@]}; i++)); do
    for ((j = 0; j < ${#SK_NAME[@]}; j++)); do
      if [ "${SK_SIDX[$j]}" = "${SEC_IDX[$i]}" ] && [ "$(skill_origin "${SK_NAME[$j]}")" = "external" ]; then
        n_sec=$((n_sec + 1))
        break
      fi
    done
  done
  if [ "$n_ext" -gt 0 ]; then
    printf '\n⚠ %s external skill(s) found in %s section(s) — step 4 must decide each one:\n' "$n_ext" "$n_sec"
    for ((i = 0; i < ${#SEC_IDX[@]}; i++)); do
      for ((j = 0; j < ${#SK_NAME[@]}; j++)); do
        if [ "${SK_SIDX[$j]}" = "${SEC_IDX[$i]}" ] && [ "$(skill_origin "${SK_NAME[$j]}")" = "external" ]; then
          printf "  - %s (lines %s-%s) — inherited skill: '%s'\n" \
            "${SEC_NAME[$i]}" "${SEC_START[$i]}" "${SEC_END[$i]}" "${SK_NAME[$j]}"
        fi
      done
    done
  fi
  printf '\n→ DECISION REQUIRED before continuing (step 4)\n'
}

# ---------------------------------------------------------------------------
# classify (contracts.md C-4)
# ---------------------------------------------------------------------------

cmd_classify() {
  local from="" names="" json=0
  while [ $# -gt 0 ]; do
    case "$1" in
      --from-analysis) [ $# -ge 2 ] || usage_err "--from-analysis needs a path"; from="$2"; shift 2 ;;
      --names) [ $# -ge 2 ] || usage_err "--names needs a comma-separated list"; names="$2"; shift 2 ;;
      --json) json=1; shift ;;
      -h | --help) cmd_help; return 0 ;;
      *) usage_err "unknown argument '$1' for 'classify'" ;;
    esac
  done
  local -a list=()
  if [ -n "$names" ]; then
    local IFS=','
    read -r -a list <<< "$names"
  elif [ -n "$from" ]; then
    [ -f "$from" ] || fail 2 "analysis '$from' not found" "classify was given a file that does not exist" \
                              "run 'analyze --json > analysis.json' first"
    flat_load "$from"
    local rec
    while IFS= read -r rec; do
      [ -n "$rec" ] || continue
      list+=("${rec#*$'\t'}")
    done < <(printf '%s\n' "$JSON_FLAT" | awk '
      /^sections\[[0-9]+\]\.skills\[[0-9]+\]\.name\t/ {
        v = substr($0, index($0, "\t") + 1)
        if (substr(v, 1, 2) == "S:") v = substr(v, 3)
        print v
      }
    ')
  else
    usage_err "'classify' needs --from-analysis <path> or --names <csv>" \
              "the engine classifies a list of skill names that it is given"
  fi
  [ "${#list[@]}" -gt 0 ] || fail 6 "no skill names to classify" "the input produced an empty list" \
                                       "pass --names forge-dev,my-linter or an analysis file with skills"
  if [ "$json" = 1 ]; then
    printf '{\n  "skills": ['
    local i first=1
    for ((i = 0; i < ${#list[@]}; i++)); do
      local nm="${list[$i]}" mig ori
      nm=$(trim "$nm")
      [ -n "$nm" ] || continue
      ori=$(skill_origin "$nm"); mig=$(skill_migration "$nm")
      [ "$first" -eq 1 ] && first=0 || printf ','
      printf '\n    { "name": "%s", "origin": "%s", "migration": %s }' \
        "$(json_escape_str "$nm")" "$ori" \
        "$([ -n "$mig" ] && printf '"%s"' "$mig" || printf 'null')"
    done
    printf '\n  ]\n}\n'
    return 0
  fi
  printf '[4/6] SKILL CLASSIFICATION\n───────────────────────────\n'
  local i
  for ((i = 0; i < ${#list[@]}; i++)); do
    local nm mig ori
    nm=$(trim "${list[$i]}")
    [ -n "$nm" ] || continue
    ori=$(skill_origin "$nm"); mig=$(skill_migration "$nm")
    if [ "$ori" = "flowforge" ]; then
      printf '  FLOWFORGE  `%s`\n' "$nm"
    else
      if [ -n "$mig" ]; then
        printf '  EXTERNAL   `%s`  → suggested migration: %s\n' "$nm" "$mig"
      else
        printf '  EXTERNAL   `%s`  → no FlowForge equivalent (decide: discard / keep / review)\n' "$nm"
      fi
    fi
  done
}

# ---------------------------------------------------------------------------
# recommend (contracts.md C-6, C-8)
# ---------------------------------------------------------------------------

cmd_recommend() {
  local root="$PWD" agent="opencode" json=0
  while [ $# -gt 0 ]; do
    case "$1" in
      --root) [ $# -ge 2 ] || usage_err "--root needs a directory"; root="$2"; shift 2 ;;
      --agent) [ $# -ge 2 ] || usage_err "--agent needs a name"; agent="$2"; shift 2 ;;
      --json) json=1; shift ;;
      -h | --help) cmd_help; return 0 ;;
      *) usage_err "unknown argument '$1' for 'recommend'" ;;
    esac
  done
  root=$(abs_path "$root")
  [ -d "$root" ] || fail 2 "project root '$root' does not exist" "cannot scan a directory that is not there" \
                          "pass --root <dir> pointing at an existing project"

  FR_ID=(); FR_KIND=(); FR_PATH=(); FR_LABEL=(); FR_REC=(); FR_REASON=()
  local f id kind label pat reason rec
  while IFS= read -r f; do
    [ -n "$f" ] || continue
    id=$(fm_scalar "$f" id); kind=$(fm_scalar "$f" kind); label=$(fm_scalar "$f" label)
    [ -n "$id" ] || continue
    [ -n "$kind" ] || kind="functionality"
    rec=0; reason=""
    while IFS= read -r pat; do
      [ -n "$pat" ] || continue
      if pattern_matches "$root" "$pat"; then rec=1; reason="detected: $pat"; break; fi
    done < <(fm_list "$f" detect)
    FR_ID+=("$id"); FR_KIND+=("$kind"); FR_PATH+=("${f#"$FRAGMENTS_DIR"/}"); FR_LABEL+=("$label")
    FR_REC+=("$rec"); FR_REASON+=("$reason")
  done < <(fragment_files_ordered)

  # stack priority resolves mutually exclusive matches (android > kotlin > web > python)
  local -a stack_prio=(android kotlin web python)
  local winner="" i j
  for ((j = 0; j < ${#stack_prio[@]}; j++)); do
    for ((i = 0; i < ${#FR_ID[@]}; i++)); do
      if [ "${FR_ID[$i]}" = "${stack_prio[$j]}" ] && [ "${FR_REC[$i]}" = "1" ]; then winner="${FR_ID[$i]}"; break; fi
    done
    [ -n "$winner" ] && break
  done
  # mutually exclusive pass: only the winning stack fragment stays recommended
  for ((i = 0; i < ${#FR_ID[@]}; i++)); do
    [ "${FR_KIND[$i]}" = "stack" ] || continue
    [ "${FR_REC[$i]}" = "1" ] || continue
    [ "${FR_ID[$i]}" = "$winner" ] && continue
    FR_REC[$i]=0
    FR_REASON[$i]="conflicts with ${winner} (mutually exclusive stack fragments)"
  done

  # recommendedWith companions
  for ((i = 0; i < ${#FR_ID[@]}; i++)); do
    [ "${FR_REC[$i]}" = "1" ] || continue
    while IFS= read -r comp; do
      [ -n "$comp" ] || continue
      for ((j = 0; j < ${#FR_ID[@]}; j++)); do
        if [ "${FR_ID[$j]}" = "$comp" ] && [ "${FR_REC[$j]}" = "0" ]; then
          FR_REC[$j]=1; FR_REASON[$j]="companion of ${FR_ID[$i]}"
        fi
      done
    done < <(fm_list "$FRAGMENTS_DIR/${FR_PATH[$i]}" recommendedWith)
  done

  # agent fragment
  if [ -n "$agent" ] && [ "$agent" != "none" ]; then
    for ((i = 0; i < ${#FR_ID[@]}; i++)); do
      if [ "${FR_KIND[$i]}" = "agent" ] && [ "${FR_ID[$i]}" = "$agent" ]; then
        FR_REC[$i]=1; FR_REASON[$i]="current agent: $agent"
      fi
    done
  fi

  local base="blank"
  case "$winner" in
    android | kotlin) base="kotlin-android" ;;
    web) base="web" ;;
    python) base="python" ;;
    *) base="blank" ;;
  esac

  if [ "$json" = 1 ]; then
    printf '{\n'
    printf '  "root": "%s",\n' "$(json_escape_str "$root")"
    printf '  "base": "%s",\n' "$base"
    printf '  "agent": "%s",\n' "$(json_escape_str "$agent")"
    printf '  "fragments": ['
    local first=1
    for ((i = 0; i < ${#FR_ID[@]}; i++)); do
      [ "$first" -eq 1 ] && first=0 || printf ','
      printf '\n    { "id": "%s", "kind": "%s", "path": "%s", "label": "%s", "recommended": %s, "reason": "%s" }' \
        "$(json_escape_str "${FR_ID[$i]}")" "${FR_KIND[$i]}" "$(json_escape_str "${FR_PATH[$i]}")" \
        "$(json_escape_str "${FR_LABEL[$i]}")" \
        "$([ "${FR_REC[$i]}" = "1" ] && printf 'true' || printf 'false')" \
        "$(json_escape_str "${FR_REASON[$i]}")"
    done
    printf '\n  ]\n}\n'
    return 0
  fi
  printf '[5/6] FRAGMENT RECOMMENDATION\n──────────────────────────────\n'
  printf 'BASE TEMPLATE: %s\n\n' "$base"
  local kind cur=""
  for ((i = 0; i < ${#FR_ID[@]}; i++)); do
    if [ "${FR_KIND[$i]}" != "$cur" ]; then
      cur="${FR_KIND[$i]}"
      printf '%s:\n' "$(lower "$cur")"
    fi
    if [ "${FR_REC[$i]}" = "1" ]; then
      printf '  [✓] %-32s (recommended — %s)\n' "${FR_PATH[$i]}" "${FR_REASON[$i]}"
    else
      printf '  [ ] %-32s\n' "${FR_PATH[$i]}"
    fi
  done
  printf '\nRecommendations are advisory: the user may deselect any of them (FR-006 Scenario D).\n'
}

# ---------------------------------------------------------------------------
# plan.json loading and validation (contracts.md C-3)
# ---------------------------------------------------------------------------

P_MODE=""; P_ROOT=""; P_TARGET_ABS=""; P_TARGET_REL=""; P_BASE=""; P_ORIG_SHA=""
P_KEEP=(); P_REPLACE=(); P_OMIT=()
P_EXT_NAME=(); P_EXT_DECISION=(); P_EXT_MIGRATE=()
P_FRAG=()
P_CF_KEY=(); P_CF_EXISTING=(); P_CF_TEMPLATE=(); P_CF_RES=()
BASE_META_FLAT=""

base_meta_get() {
  local saved="$JSON_FLAT" out rc
  JSON_FLAT="$BASE_META_FLAT"
  out=$(json_get "$1"); rc=$?
  JSON_FLAT="$saved"
  printf '%s' "$out"
  return "$rc"
}

# base_required_section <name> — listed in base.meta.json requiredSections?
base_required_section() {
  local want nm
  want=$(lower "$(trim "$1")")
  while IFS= read -r nm; do
    [ -n "$nm" ] || continue
    [ "$(lower "$(trim "$nm")")" = "$want" ] && return 0
  done < <(printf '%s\n' "$BASE_META_FLAT" | awk '
    /^requiredSections\[[0-9]+\]\t/ { v = $0; sub(/^[^\t]*\t/, "", v); if (substr(v, 1, 2) == "S:") v = substr(v, 3); print v }
  ')
  return 1
}

profile_names() {
  printf '%s\n' "$BASE_META_FLAT" | awk '
    /^profiles\./ && index($0, "\t") > 0 {
      p = substr($0, 10)
      i = index(p, ".")
      if (i > 0) p = substr(p, 1, i - 1)
      if (p != "" && !(p in seen)) { seen[p] = 1; print p }
    }
  '
}

load_base_meta() {
  [ -f "$BASE_META" ] || fail 6 "base metadata '$BASE_META' is missing" \
                                "the skill installation is incomplete (templates/base/base.meta.json)" \
                                "re-install the skill (./install-skills.sh) and re-run"
  BASE_META_FLAT=$(awk "$JSON_AWK" "$BASE_META" 2>&1) || fail 6 "base metadata is not valid JSON" "$BASE_META_FLAT" \
                                                                    "re-install the skill and re-run"
}

profile_has() {
  local p
  while IFS= read -r p; do
    [ "$p" = "$1" ] && return 0
  done < <(profile_names)
  return 1
}

# in_list <needle> <array...>
in_list() {
  local needle="$1"; shift
  local e
  for e in "$@"; do
    [ "$(lower "$(trim "$e")")" = "$needle" ] && return 0
  done
  return 1
}

plan_load() {
  local cfg="$1" v
  [ -f "$cfg" ] || fail 6 "plan '$cfg' does not exist" \
                          "apply/preview need the frozen plan built in step 6" \
                          "write plan.json from 'analyze --json' + 'recommend --json' and re-run"
  flat_load "$cfg"

  v=$(json_req schemaVersion "every plan must declare the contract version it was built against")
  [ "$v" = "1" ] || fail 6 "plan declares schemaVersion='$v'" \
                           "this engine implements schemaVersion 1 (contracts.md C-3)" \
                           "regenerate the plan with the current /fforge-init skill"

  v=$(json_get mode) || v=""
  case "$v" in
    init | sync) P_MODE="$v" ;;
    *) fail 6 "invalid mode '${v}'" "mode must be 'init' (target absent) or 'sync' (target present)" \
            "set \"mode\" in plan.json to init or sync" ;;
  esac

  local root
  root=$(json_get root) || root=""
  [ -n "$root" ] || root="$PWD"
  P_ROOT=$(abs_path "$root")
  [ -d "$P_ROOT" ] || fail 6 "plan root '$P_ROOT' is not a directory" \
                             "the plan must point at an existing project" \
                             "fix \"root\" in plan.json"

  local target
  target=$(json_get target) || target=""
  [ -n "$target" ] || target="./AGENTS.md"
  case "$target" in
    /*) P_TARGET_ABS="$target" ;;
    *) P_TARGET_ABS=$(abs_path "$P_ROOT/${target#./}") ;;
  esac
  P_TARGET_REL=$(target_rel "$P_TARGET_ABS")
  guard_path_under_root "$P_TARGET_ABS" "$P_ROOT"

  load_base_meta
  P_BASE=$(json_get base) || P_BASE=""
  [ -n "$P_BASE" ] || P_BASE="blank"
  profile_has "$P_BASE" || fail 6 "unknown base profile '$P_BASE'" \
      "the plan must pick one of: $(profile_names | tr '\n' ' ')" \
      "set \"base\" to one of those profile names (contracts.md C-7)"

  local exists=0
  [ -e "$P_TARGET_ABS" ] && exists=1
  if [ "$P_MODE" = "init" ] && [ "$exists" = 1 ]; then
    fail 6 "mode 'init' but '$P_TARGET_ABS' already exists" \
         "init only creates a new file; refusing to overwrite an existing one silently (contracts.md C-3)" \
         "re-run with mode 'sync' after analysing the existing file"
  fi
  if [ "$P_MODE" = "sync" ] && [ "$exists" = 0 ]; then
    fail 2 "mode 'sync' but there is no file at '$P_TARGET_ABS'" \
         "sync merges into an existing AGENTS.md" \
         "run 'detect' and use mode 'init' when the file does not exist"
  fi
  if [ "$exists" = 1 ]; then
    guard_target_file "$P_TARGET_ABS"
    analyze_load "$P_TARGET_ABS"
    local actual
    actual=$(sha256_of_file "$P_TARGET_ABS")
    P_ORIG_SHA=$(json_get originalSha256) || P_ORIG_SHA=""
    if [ -z "$P_ORIG_SHA" ]; then
      fail 6 "plan.json is missing originalSha256" \
           "a sync plan must pin the exact file state it was derived from (contracts.md C-10)" \
           "copy the sha256 from 'analyze --json' output into the plan"
    fi
    if [ "$P_ORIG_SHA" != "$actual" ]; then
      fail 5 "the target changed since the plan was created" \
           "plan expects sha256=$P_ORIG_SHA but the file on disk is $actual" \
           "re-run analyze + preview to rebuild the plan from the current file"
    fi
  fi

  # sections
  P_KEEP=(); P_REPLACE=(); P_OMIT=()
  local name lc
  while IFS= read -r name; do
    [ -n "$name" ] || continue
    lc=$(lower "$(trim "$name")")
    if [ -z "$(sec_lookup "$name")" ]; then
      fail 6 "sections.keep names '$name', which the analysis did not find" \
           "typo or stale plan: silently dropping a section selection is never allowed (contracts.md C-3)" \
           "use one of: $(known_section_names)"
    fi
    in_list "$lc" "${P_REPLACE[@]}" "${P_OMIT[@]}" && fail 6 "'$name' appears in more than one of keep/replace/omit" \
        "a section must have exactly one decision" "remove the duplicate entry from plan.json"
    P_KEEP+=("$name")
  done < <(json_arr sections.keep)
  while IFS= read -r name; do
    [ -n "$name" ] || continue
    lc=$(lower "$(trim "$name")")
    if [ -z "$(sec_lookup "$name")" ]; then
      fail 6 "sections.replace names '$name', which the analysis did not find" \
           "typo or stale plan: silently dropping a section selection is never allowed (contracts.md C-3)" \
           "use one of: $(known_section_names)"
    fi
    in_list "$lc" "${P_KEEP[@]}" "${P_OMIT[@]}" && fail 6 "'$name' appears in more than one of keep/replace/omit" \
        "a section must have exactly one decision" "remove the duplicate entry from plan.json"
    P_REPLACE+=("$name")
  done < <(json_arr sections.replace)
  while IFS= read -r name; do
    [ -n "$name" ] || continue
    lc=$(lower "$(trim "$name")")
    if [ -z "$(sec_lookup "$name")" ]; then
      fail 6 "sections.omit names '$name', which the analysis did not find" \
           "typo or stale plan: silently dropping a section selection is never allowed (contracts.md C-3)" \
           "use one of: $(known_section_names)"
    fi
    in_list "$lc" "${P_KEEP[@]}" "${P_REPLACE[@]}" && fail 6 "'$name' appears in more than one of keep/replace/omit" \
        "a section must have exactly one decision" "remove the duplicate entry from plan.json"
    P_OMIT+=("$name")
  done < <(json_arr sections.omit)

  # external skills
  P_EXT_NAME=(); P_EXT_DECISION=(); P_EXT_MIGRATE=()
  local n i nm dec mig
  n=$(json_arr_count externalSkills)
  for ((i = 0; i < n; i++)); do
    nm=$(json_get "externalSkills[$i].name") || nm=""
    dec=$(json_get "externalSkills[$i].decision") || dec=""
    mig=$(json_get "externalSkills[$i].migrateTo") || mig=""
    [ -n "$nm" ] || fail 6 "externalSkills[$i] has no name" "every entry needs the skill it decides about" \
                         "add \"name\" to that entry in plan.json"
    case "$dec" in
      discard | keep | review) ;;
      migrate)
        [ -n "$mig" ] || fail 6 "externalSkills[$i] ('$nm') is 'migrate' without migrateTo" \
                               "a migration needs a destination (contracts.md C-3)" \
                               "set \"migrateTo\" to the FlowForge equivalent" ;;
      *)
        fail 6 "externalSkills[$i] ('$nm') has decision '${dec}'" \
             "decision must be one of discard, keep, migrate, review" \
             "fix the decision in plan.json" ;;
    esac
    if ! all_skill_names | grep -qx -- "$nm"; then
      warn "unknown external skill '$nm' (not found in the analysis) — it will not create content"
    fi
    P_EXT_NAME+=("$nm"); P_EXT_DECISION+=("$dec"); P_EXT_MIGRATE+=("$mig")
  done

  # fragments
  P_FRAG=()
  local p
  while IFS= read -r p; do
    [ -n "$p" ] || continue
    case "$p" in
      /*) fail 3 "fragment path '$p' is absolute" "fragments must be relative to templates/fragments/ (contracts.md C-3)" \
              "use a path such as stack/android.md.fragment" ;;
      *..*) fail 3 "fragment path '$p' contains '..'" "path-escape rejection: fragments stay inside the skill (contracts.md C-3)" \
                "use a path such as functionality/testing.md.fragment" ;;
    esac
    if [ ! -f "$FRAGMENTS_DIR/$p" ]; then
      fail 6 "fragment '$p' does not exist under templates/fragments/" \
           "the plan references a fragment the skill does not ship" \
           "run 'recommend --json' and use one of the paths it lists"
    fi
    P_FRAG+=("$p")
  done < <(json_arr fragments)

  # conflicts
  P_CF_KEY=(); P_CF_EXISTING=(); P_CF_TEMPLATE=(); P_CF_RES=()
  n=$(json_arr_count conflicts)
  local key res
  for ((i = 0; i < n; i++)); do
    key=$(json_get "conflicts[$i].key") || key=""
    res=$(json_get "conflicts[$i].resolution") || res=""
    [ -n "$(trim "$key")" ] || fail 6 "conflicts[$i] has an empty key" "every conflict entry needs the key it resolves" \
                                                                        "set \"key\" in plan.json"
    [ -n "$(trim "$res")" ] || fail 6 "conflicts[$i] ('$key') has an empty resolution" \
                                     "resolution must be existing, template or a literal value (contracts.md C-3)" \
                                     "set \"resolution\" in plan.json"
    P_CF_KEY+=("$(lower "$(trim "$key")")")
    P_CF_EXISTING+=("$(json_get "conflicts[$i].existing")")
    P_CF_TEMPLATE+=("$(json_get "conflicts[$i].template")")
    P_CF_RES+=("$res")
  done
}

known_section_names() {
  local i out=""
  for ((i = 0; i < ${#SEC_NAME[@]}; i++)); do
    [ -n "$out" ] && out+=", "
    out+="${SEC_NAME[$i]}"
  done
  [ -n "$out" ] || out="(the analysis found no sections)"
  printf '%s' "$out"
}

# review_undecided_count — external skills present in the file with no decision at all.
# These always block (FR-005 Scenario B: 2 externals, 1 decided ⇒ 1 unresolved).
review_undecided_count() {
  local i n=0 decided nm
  while IFS= read -r nm; do
    [ -n "$nm" ] || continue
    decided=0
    for ((i = 0; i < ${#P_EXT_NAME[@]}; i++)); do
      [ "${P_EXT_NAME[$i]}" = "$nm" ] && decided=1
    done
    [ "$decided" = "0" ] && n=$((n + 1))
  done < <(external_skill_names)
  printf '%s' "$n"
}

# review_explicit_count — decisions recorded as 'review' (relaxed by --allow-review-todos)
review_explicit_count() {
  local i n=0
  for ((i = 0; i < ${#P_EXT_DECISION[@]}; i++)); do
    [ "${P_EXT_DECISION[$i]}" = "review" ] && n=$((n + 1))
  done
  printf '%s' "$n"
}

# review_unresolved_total — everything the user still has to decide
review_unresolved_total() {
  printf '%s' "$(( $(review_undecided_count) + $(review_explicit_count) ))"
}

# review_blocking_count — what blocks *right now*: undecided externals always,
# an explicit 'review' unless --allow-review-todos was passed.
review_blocking_count() {
  local n
  n=$(review_undecided_count)
  if [ "${ALLOW_REVIEW:-0}" != "1" ]; then n=$((n + $(review_explicit_count))); fi
  printf '%s' "$n"
}

# external_skill_names — unique external skills found in the analysis, in document order
external_skill_names() {
  local nm
  while IFS= read -r nm; do
    [ -n "$nm" ] || continue
    [ "$(skill_origin "$nm")" = "external" ] && printf '%s\n' "$nm"
  done < <(all_skill_names)
}

# ---------------------------------------------------------------------------
# Merge composition
# ---------------------------------------------------------------------------

BASE_LINES=()
BSEC_IDX=(); BSEC_LEVEL=(); BSEC_NAME=(); BSEC_START=(); BSEC_END=()
OUT_CONTENT=""; OUT_ACTION=""; OUT_BLOCK_HASH=""; OUT_DRIFT=0; OUT_EXISTING_BLOCK_HASH=""
OUT_SECTION_NAMES=(); OUT_SECTION_OUTCOMES=()
CF_KEY=(); CF_EXISTING=(); CF_TEMPLATE=(); CF_BASEINDEX=()
OUT_SKILLS=()
DIFF_TEXT=""; DIFF_ADDED=0; DIFF_REMOVED=0; DIFF_CHANGED=0; DIFF_IDENTICAL=0

base_analyze() { # $1 = rendered base text
  local text="$1" rec kind tsv
  split_to_array "$text" BASE_LINES
  BSEC_IDX=(); BSEC_LEVEL=(); BSEC_NAME=(); BSEC_START=(); BSEC_END=()
  tsv=$(printf '%s' "$text" | awk "$ANALYZE_AWK")
  while IFS= read -r rec; do
    [ -n "$rec" ] || continue
    kind="${rec%%$'\t'*}"
    if [ "$kind" = "S" ]; then
      IFS=$'\t' read -r idx lvl nm st en _mg _em <<< "${rec#S$'\t'}"
      BSEC_IDX+=("$idx"); BSEC_LEVEL+=("$lvl"); BSEC_NAME+=("$nm")
      BSEC_START+=("$st"); BSEC_END+=("$en")
    fi
  done <<< "$tsv"
}

base_sec_lookup() {
  local want i
  want=$(lower "$(trim "$1")")
  for ((i = 0; i < ${#BSEC_NAME[@]}; i++)); do
    if [ "$(lower "$(trim "${BSEC_NAME[$i]}")")" = "$want" ]; then printf '%s' "${BSEC_IDX[$i]}"; return 0; fi
  done
  return 1
}

base_sec_array_index() { # $1 base section id -> 0-based
  local want="$1" i
  for ((i = 0; i < ${#BSEC_IDX[@]}; i++)); do
    [ "${BSEC_IDX[$i]}" = "$want" ] && { printf '%s' "$i"; return 0; }
  done
  return 1
}

# base_body_end <start-1based> <end-1based> — never swallow the injection anchor,
# the generated-by footer or the closing managed marker (contracts.md C-5/C-7).
base_body_end() {
  local from="$1" to="$2" k tt
  for ((k = from + 1; k <= to; k++)); do
    tt=$(trim "${BASE_LINES[$((k - 1))]}")
    case "$tt" in
      "$ANCHOR" | "<!-- Generated by"* | "<!-- fforge-init:end"*) printf '%s' "$((k - 1))"; return 0 ;;
    esac
  done
  printf '%s' "$to"
}

render_base() {
  local text date project_name ver lang fw tests ver_min
  [ -f "$BASE_TEMPLATE" ] || fail 6 "base template '$BASE_TEMPLATE' is missing" \
      "the skill installation is incomplete" "re-install the skill (./install-skills.sh) and re-run"
  text=$(cat "$BASE_TEMPLATE")

  project_name=$(basename "$P_ROOT")
  [ -n "$project_name" ] || project_name="project"
  if [ -n "${SOURCE_DATE_EPOCH:-}" ]; then
    date=$(date -u -d "@$SOURCE_DATE_EPOCH" +%F 2>/dev/null || date -u +%F)
  else
    date=$(date -u +%F)
  fi
  ver=$(skill_version)
  lang=$(base_meta_get "profiles.$P_BASE.placeholders.__STACK_LANG__") || lang="(fill in)"
  fw=$(base_meta_get "profiles.$P_BASE.placeholders.__STACK_FRAMEWORK__") || fw="(fill in)"
  tests=$(base_meta_get "profiles.$P_BASE.placeholders.__STACK_TESTS__") || tests="(fill in)"
  ver_min=$(base_meta_get "profiles.$P_BASE.placeholders.__STACK_VERSION__") || ver_min="(fill in)"

  text="${text//__PROJECT_NAME__/$project_name}"
  text="${text//__DATE__/$date}"
  text="${text//__FLOWFORGE_VERSION__/$ver}"
  text="${text//__STACK_LANG__/$lang}"
  text="${text//__STACK_FRAMEWORK__/$fw}"
  text="${text//__STACK_TESTS__/$tests}"
  text="${text//__STACK_VERSION__/$ver_min}"
  printf '%s' "$text"
}

# value_is_concrete <value>
value_is_concrete() {
  local v
  v=$(trim "$1")
  v="${v%%<!--*}"
  v=$(trim "$v")
  [ -n "$v" ] || return 1
  case "$v" in
    *__*) return 1 ;;
    "(fill in)" | TODO | TBD | N/A | "-") return 1 ;;
  esac
  return 0
}

# kv_of_section <array-name> <start-line-1based> <end-line>  -> "key|lineno|value"
kv_of_section() {
  local -n __arr="$1"
  local from="$2" to="$3" i line t rest key val
  for ((i = from - 1; i < to && i < ${#__arr[@]}; i++)); do
    line="${__arr[$i]}"
    t=$(trim "$line")
    case "$t" in
      [-*+]*)
        rest=$(trim "${t#?}")
        ;;
      *) continue ;;
    esac
    rest="${rest//\*\*/}"
    case "$rest" in
      *:*) ;;
      *) continue ;;
    esac
    key=$(trim "${rest%%:*}")
    val=$(trim "${rest#*:}")
    [ -n "$key" ] || continue
    printf '%s|%s|%s\n' "$(lower "$key")" "$((i + 1))" "$val"
  done
}

# detect_conflicts — fills CF_* with the conflicts between the target and the base
detect_conflicts() {
  CF_KEY=(); CF_EXISTING=(); CF_TEMPLATE=(); CF_BASEINDEX=()
  local i bi sidx b0 k kv tkey tval bkey bval bline
  for ((i = 0; i < ${#SEC_IDX[@]}; i++)); do
    sidx="${SEC_IDX[$i]}"
    [ "${PRESERVE[$i]:-0}" = "1" ] && continue
    [ "${SEC_EMPTY[$i]}" = "true" ] && continue
    bi=$(base_sec_lookup "${SEC_NAME[$i]}") || continue
    b0=$(base_sec_array_index "$bi") || continue
    local bstart="${BSEC_START[$b0]}" bend="${BSEC_END[$b0]}"
    while IFS='|' read -r tkey _ tval; do
      [ -n "$tkey" ] || continue
      value_is_concrete "$tval" || continue
      while IFS='|' read -r bkey bline bval; do
        [ "$bkey" = "$tkey" ] || continue
        value_is_concrete "$bval" || continue
        [ "$(trim "$tval")" = "$(trim "$bval")" ] && continue
        CF_KEY+=("$tkey"); CF_EXISTING+=("$(trim "$tval")"); CF_TEMPLATE+=("$(trim "$bval")")
        CF_BASEINDEX+=("$((bline - 1))")
      done < <(kv_of_section BASE_LINES "$bstart" "$bend")
    done < <(kv_of_section FILE_LINES "${SEC_START[$i]}" "${SEC_END[$i]}")
  done
}

# build_skills — the C-7a union, as bullet lines
build_skills() {
  OUT_SKILLS=()
  local -a base_skills=() add=()
  local i n seen=" " nm mig dec sidx keep_section
  while IFS= read -r n; do
    [ -n "$n" ] || continue
    base_skills+=("$n")
  done < <(printf '%s\n' "$BASE_META_FLAT" | awk '
    /^skills\[[0-9]+\]\t/ { v = $0; sub(/^[^\t]*\t/, "", v); if (substr(v, 1, 2) == "S:") v = substr(v, 3); print v }
  ')

  # retained skills from sections that are NOT preserved outside the block
  for ((i = 0; i < ${#SK_NAME[@]}; i++)); do
    nm="${SK_NAME[$i]}"
    sidx="${SK_SIDX[$i]}"
    keep_section=0
    for ((n = 0; n < ${#SEC_IDX[@]}; n++)); do
      if [ "${SEC_IDX[$n]}" = "$sidx" ]; then
        [ "${PRESERVE[$n]:-0}" = "1" ] && keep_section=1
        break
      fi
    done
    [ "$keep_section" = "1" ] && continue
    if [ "$(skill_origin "$nm")" = "flowforge" ]; then
      add+=("$nm|")
      continue
    fi
    dec=""; mig=""
    for ((n = 0; n < ${#P_EXT_NAME[@]}; n++)); do
      if [ "${P_EXT_NAME[$n]}" = "$nm" ]; then dec="${P_EXT_DECISION[$n]}"; mig="${P_EXT_MIGRATE[$n]}"; fi
    done
    case "$dec" in
      migrate) add+=("${mig}|") ;;
      keep) add+=("${nm}|") ;;
      review)
        if [ "${ALLOW_REVIEW:-0}" = "1" ]; then add+=("${nm}|<!-- TODO: revisar -->"); fi
        ;;
      discard) ;; # explicitly dropped by the user
      *) add+=("${nm}|") ;; # no decision ⇒ retain (nothing is dropped without an explicit decision)
    esac
  done

  local -a final=()
  for nm in "${base_skills[@]}" "${add[@]}"; do
    [ -n "$nm" ] || continue
    local name_only="${nm%%|*}" flag="${nm#*|}"
    local lc; lc=$(lower "$name_only")
    case "$seen" in
      *" $lc "*) continue ;;
    esac
    seen+="$lc "
    if [ "$(skill_origin "$name_only")" = "external" ]; then
      if [ -n "$flag" ]; then final+=("- \`$name_only\` — external skill retained as-is $flag")
      else final+=("- \`$name_only\` — external skill retained as-is"); fi
    else
      final+=("- \`$name_only\`")
    fi
  done
  OUT_SKILLS=("${final[@]}")
}

# ---------------------------------------------------------------------------
# compose — the shared code path of preview and apply (DEC-4)
# ---------------------------------------------------------------------------

# compose state (declared here so that `set -u` never fires on an unused slot)
PRESERVE=()
OUTER_LINES=()
BASE_OUT=()
KEPT_SKILLS_OUTER=""
KEPT_SKILLS_END=0
ALLOW_REVIEW=0
CONFLICT_REPORT=""
FR_ID=(); FR_KIND=(); FR_PATH=(); FR_LABEL=(); FR_REC=(); FR_REASON=()

compose() {
  local i k
  declare -A TDROP=() BDROP=() CREWRITE=()
  declare -A SUPPRESS=() T2O=() SCAFFOLD=() SCAFFOLD_LINE=()
  PRESERVE=()
  OUT_SECTION_NAMES=(); OUT_SECTION_OUTCOMES=()
  OUTER_LINES=()
  KEPT_SKILLS_OUTER=""; KEPT_SKILLS_END=0

  # --- base render + analysis (must precede the decision model: names are
  #     matched against the base's own sections) ----------------------------
  local base_text; base_text=$(render_base)
  base_analyze "$base_text"

  # --- managed-block state of the target (contracts.md C-5) ---------------
  OUT_DRIFT=0
  OUT_EXISTING_BLOCK_HASH=""
  if [ -f "$P_TARGET_ABS" ]; then
    if [ "$MB_COUNT" -gt 1 ]; then
      fail 5 "the target contains $MB_COUNT managed blocks" \
           "more than one 'fforge-init' block makes ownership ambiguous (contracts.md C-5)" \
           "keep exactly one block, or delete both to re-adopt the file as hand-written"
    fi
    if [ "$MB_COUNT" = 1 ]; then
      local mb="${MB_BEGIN[0]}" me="${MB_END[0]}" stored="${MB_STORED[0]}" mk mbody=""
      if [ "$me" = "0" ]; then
        fail 5 "the managed block that starts at line $mb is never closed" \
             "an unterminated block cannot be regenerated safely (contracts.md C-5)" \
             "restore the '<!-- fforge-init:end -->' marker, or delete the block and re-run from step 2b"
      fi
      for ((mk = mb + 1; mk < me; mk++)); do mbody+="${FILE_LINES[$((mk - 1))]}"$'\n'; done
      OUT_EXISTING_BLOCK_HASH=$(sha256_of_string "$mbody")
      if [ -z "$stored" ] || [ "$stored" != "$OUT_EXISTING_BLOCK_HASH" ]; then OUT_DRIFT=1; fi
    fi
  fi

  # --- target-side decision model -----------------------------------------
  for ((i = 0; i < ${#SEC_IDX[@]}; i++)); do
    local name lc outcome preserve=0
    name="${SEC_NAME[$i]}"
    lc=$(lower "$(trim "$name")")
    if [ "${SEC_MANAGED[$i]}" = "1" ]; then
      outcome="managed"
    elif in_list "$lc" "${P_OMIT[@]}"; then
      if [ -n "$(base_sec_lookup "$name")" ] && base_required_section "$name"; then
        outcome="scaffold"; SCAFFOLD["$lc"]=1
      else
        outcome="omitted"; SUPPRESS["$lc"]=1
      fi
    elif in_list "$lc" "${P_KEEP[@]}"; then
      if [ "${SEC_EMPTY[$i]}" = "true" ]; then
        if [ -n "$(base_sec_lookup "$name")" ]; then
          outcome="filled"
          [ "$lc" = "skills" ] || SUPPRESS["$lc"]=1
        else outcome="omitted"; fi
      else
        outcome="copied"; preserve=1; SUPPRESS["$lc"]=1
        if [ "$lc" = "skills" ]; then KEPT_SKILLS_OUTER="$name"; KEPT_SKILLS_END="${SEC_END[$i]}"; fi
      fi
    elif in_list "$lc" "${P_REPLACE[@]}"; then
      if [ -n "$(base_sec_lookup "$name")" ]; then outcome="replaced"; else outcome="omitted"; fi
    else
      if [ -n "$(base_sec_lookup "$name")" ]; then
        outcome="replaced"
      elif [ "${SEC_EMPTY[$i]}" = "true" ]; then
        outcome="omitted"
      else
        outcome="copied"; preserve=1; SUPPRESS["$lc"]=1
        if [ "$lc" = "skills" ]; then KEPT_SKILLS_OUTER="$name"; KEPT_SKILLS_END="${SEC_END[$i]}"; fi
      fi
    fi
    PRESERVE[$i]="$preserve"
    OUT_SECTION_NAMES+=("$name")
    OUT_SECTION_OUTCOMES+=("$outcome")
  done

  # --- outside region ------------------------------------------------------
  for ((i = 0; i < ${#SEC_IDX[@]}; i++)); do
    [ "${PRESERVE[$i]:-0}" = "1" ] && continue
    for ((k = ${SEC_START[$i]}; k <= ${SEC_END[$i]}; k++)); do TDROP["$k"]=1; done
  done
  for ((k = 0; k < MB_COUNT; k++)); do
    local b="${MB_BEGIN[$k]}" e="${MB_END[$k]}"
    [ "$e" = "0" ] && e="$b"
    for ((i = b; i <= e; i++)); do TDROP["$i"]=1; done
  done
  local -a outer_raw=()
  for ((i = 0; i < ${#FILE_LINES[@]}; i++)); do
    [ "${TDROP[$((i + 1))]:-0}" = "1" ] && continue
    T2O["$((i + 1))"]="${#outer_raw[@]}"
    outer_raw+=("${FILE_LINES[$i]}")
  done

  # --- skills union (needs BASE_META + decisions) --------------------------
  build_skills

  # --- base section suppression / Skills body replacement -----------------
  local sk_head_line0=""
  for ((i = 0; i < ${#BSEC_NAME[@]}; i++)); do
    local blc; blc=$(lower "$(trim "${BSEC_NAME[$i]}")")
    local bstart="${BSEC_START[$i]}" bend="${BSEC_END[$i]}" skip=0
    bend=$(base_body_end "$bstart" "$bend")
    if [ "${SUPPRESS[$blc]:-0}" = "1" ]; then
      if [ "${BSEC_LEVEL[$i]}" = "1" ]; then
        BDROP["$((bstart - 1))"]=1
      else
        for ((k = bstart; k <= bend; k++)); do BDROP["$((k - 1))"]=1; done
      fi
      skip=1
    fi
    if [ "$blc" = "skills" ] && [ "$skip" = "0" ]; then
      sk_head_line0=$((bstart - 1))
      for ((k = bstart + 1; k <= bend; k++)); do BDROP["$((k - 1))"]=1; done
    fi
    if [ "${SCAFFOLD[$blc]:-0}" = "1" ]; then
      for ((k = bstart + 1; k <= bend; k++)); do BDROP["$((k - 1))"]=1; done
      SCAFFOLD_LINE["$((bstart - 1))"]=1
    fi
  done

  # --- title rule (C-7b) ---------------------------------------------------
  local has_h1_kept=0
  for ((i = 0; i < ${#SEC_LEVEL[@]}; i++)); do
    if [ "${SEC_LEVEL[$i]}" = "1" ] && [ "${PRESERVE[$i]:-0}" = "1" ]; then has_h1_kept=1; fi
  done
  if [ "$has_h1_kept" = "1" ]; then
    for ((i = 0; i < ${#BSEC_LEVEL[@]}; i++)); do
      if [ "${BSEC_LEVEL[$i]}" = "1" ]; then BDROP["$((BSEC_START[$i] - 1))"]=1; fi
    done
  fi

  # --- conflicts -----------------------------------------------------------
  detect_conflicts
  local ci pi found
  for ((ci = 0; ci < ${#CF_KEY[@]}; ci++)); do
    found=0
    for ((pi = 0; pi < ${#P_CF_KEY[@]}; pi++)); do
      if [ "${P_CF_KEY[$pi]}" = "${CF_KEY[$ci]}" ]; then
        found=1
        case "${P_CF_RES[$pi]}" in
          template) ;;
          existing) rewrite_kv_line "${CF_BASEINDEX[$ci]}" "${CF_EXISTING[$ci]}" ;;
          *) rewrite_kv_line "${CF_BASEINDEX[$ci]}" "${P_CF_RES[$pi]}" ;;
        esac
      fi
    done
    if [ "$found" = "0" ]; then
      printf '%s\n' "${CF_KEY[$ci]}|${CF_EXISTING[$ci]}|${CF_TEMPLATE[$ci]}" >> "${CONFLICT_REPORT:-/dev/null}"
    fi
  done

  # --- fragment text -------------------------------------------------------
  local -a frag_lines=()
  for ((i = 0; i < ${#P_FRAG[@]}; i++)); do
    if [ "$i" -gt 0 ]; then frag_lines+=(""); fi
    while IFS= read -r k; do frag_lines+=("$k"); done < <(fragment_body "$FRAGMENTS_DIR/${P_FRAG[$i]}")
  done

  # --- assemble the managed block -----------------------------------------
  local -a new_base=()
  for ((i = 0; i < ${#BASE_LINES[@]}; i++)); do
    if [ "${BDROP[$i]:-0}" = "1" ]; then continue; fi
    local line="${BASE_LINES[$i]}"
    if [ -n "${CREWRITE[$i]:-}" ]; then line="${CREWRITE[$i]}"; fi
    new_base+=("$line")
    if [ "$i" = "${sk_head_line0:-}" ] && [ "${#OUT_SKILLS[@]}" -gt 0 ]; then
      new_base+=("")
      new_base+=("FlowForge skills used by this project (load them by path; this file is the index):")
      new_base+=("")
      for k in "${OUT_SKILLS[@]}"; do new_base+=("$k"); done
      new_base+=("")
    fi
    if [ "$(trim "$line")" = "$ANCHOR" ] && [ "${#frag_lines[@]}" -gt 0 ]; then
      new_base+=("")
      for k in "${frag_lines[@]}"; do new_base+=("$k"); done
    fi
    if [ "${SCAFFOLD_LINE[$i]:-0}" = "1" ]; then
      new_base+=("")
      new_base+=("<!-- omitted by /fforge-init: this section is required by the base template -->")
    fi
  done
  [ "$sk_head_line0" = "" ] && new_base+=("")

  normalize_blank_lines new_base BASE_OUT

  # --- kept Skills section: append the union entries that are missing -----
  if [ -n "$KEPT_SKILLS_OUTER" ] && [ "${#OUT_SKILLS[@]}" -gt 0 ]; then
    local -a extra=() entry entry_name
    for entry in "${OUT_SKILLS[@]}"; do
      entry_name="${entry#- \`}"
      entry_name="${entry_name%%\`*}"
      if ! printf '%s\n' "${outer_raw[@]}" | grep -qF "\`$entry_name\`"; then extra+=("$entry"); fi
    done
    if [ "${#extra[@]}" -gt 0 ]; then
      local pos="${T2O[$KEPT_SKILLS_END]:-}"
      if [ -z "$pos" ]; then pos=$((${#outer_raw[@]} - 1)); fi
      local -a with_extra=()
      for ((i = 0; i <= pos && i < ${#outer_raw[@]}; i++)); do with_extra+=("${outer_raw[$i]}"); done
      with_extra+=("")
      for entry in "${extra[@]}"; do with_extra+=("$entry"); done
      for ((i = pos + 1; i < ${#outer_raw[@]}; i++)); do with_extra+=("${outer_raw[$i]}"); done
      outer_raw=("${with_extra[@]}")
    fi
  fi

  # --- outer region normalisation -----------------------------------------
  normalize_blank_lines outer_raw OUTER_LINES

  # --- block hash ----------------------------------------------------------
  local blen=${#BASE_OUT[@]} begin endline body hash
  if [ "$blen" -lt 2 ]; then
    fail 6 "the base template produced an empty document" \
         "templates/base/AGENTS.md.base is missing or was emptied" \
         "re-install the skill and re-run"
  fi
  begin="${BASE_OUT[0]}"
  endline="${BASE_OUT[$((blen - 1))]}"
  case "$(trim "$begin")" in
    "<!-- fforge-init:begin"*) ;;
    *) fail 6 "the base template does not start with the managed-block marker" \
            "the engine cannot own a region it cannot delimit (contracts.md C-5)" \
            "restore templates/base/AGENTS.md.base from the skill repository" ;;
  esac
  case "$(trim "$endline")" in
    "<!-- fforge-init:end"*) ;;
    *) fail 6 "the base template does not end with the managed-block marker" \
            "the engine cannot own a region it cannot delimit (contracts.md C-5)" \
            "restore templates/base/AGENTS.md.base from the skill repository" ;;
  esac
  body=""
  for ((i = 1; i < blen - 1; i++)); do body+="${BASE_OUT[$i]}"$'\n'; done
  hash=$(sha256_of_string "$body")
  BASE_OUT[0]="<!-- fforge-init:begin v=1 hash=$hash -->"
  OUT_BLOCK_HASH="$hash"

  local block=""
  block+="${BASE_OUT[0]}"$'\n'
  block+="$body"
  block+="${BASE_OUT[$((blen - 1))]}"$'\n'

  if [ "${#OUTER_LINES[@]}" -gt 0 ]; then
    OUT_CONTENT=""
    for ((i = 0; i < ${#OUTER_LINES[@]}; i++)); do OUT_CONTENT+="${OUTER_LINES[$i]}"$'\n'; done
    OUT_CONTENT+=$'\n'"$block"
  else
    OUT_CONTENT="$block"
  fi

  # --- drift resolution ----------------------------------------------------
  if [ "$OUT_DRIFT" = "1" ]; then
    local res="" drifound=0
    for ((pi = 0; pi < ${#P_CF_KEY[@]}; pi++)); do
      if [ "${P_CF_KEY[$pi]}" = "managed-block" ]; then drifound=1; res="${P_CF_RES[$pi]}"; fi
    done
    if [ "$drifound" = "1" ] && [ "$res" = "existing" ]; then
      local orig=""
      for ((i = 0; i < ${#FILE_LINES[@]}; i++)); do orig+="${FILE_LINES[$i]}"$'\n'; done
      OUT_CONTENT="$orig"
    fi
  fi

  # --- action --------------------------------------------------------------
  OUT_ACTION="update"
  if [ -f "$P_TARGET_ABS" ]; then
    local newhash oldhash
    newhash=$(sha256_of_string "$OUT_CONTENT")
    oldhash=$(sha256_of_file "$P_TARGET_ABS")
    if [ "$newhash" = "$oldhash" ]; then OUT_ACTION="no-change"; fi
  fi
  build_diff "$(if [ -f "$P_TARGET_ABS" ]; then cat "$P_TARGET_ABS"; fi)" "${OUT_CONTENT%$'\n'}"
}

rewrite_kv_line() { # $1 = 0-based BASE_LINES index, $2 = new value ; fills CREWRITE
  local idx="$1" val="$2" line prefix
  [ "$idx" -ge 0 ] || return 0
  line="${BASE_LINES[$idx]}"
  prefix="${line%%:*}"
  CREWRITE["$idx"]="${prefix}: ${val}"
}

normalize_blank_lines() { # $1 = input array name, $2 = output array name
  local -n __nin="$1"
  local -n __nout="$2"
  local i line prev_blank=1 n
  local -a tmp=()
  for ((i = 0; i < ${#__nin[@]}; i++)); do
    line="${__nin[$i]}"
    if [ "$(trim "$line")" = "" ]; then
      [ "$prev_blank" = "1" ] && continue
      tmp+=("")
      prev_blank=1
    else
      prev_blank=0
      tmp+=("$line")
    fi
  done
  n=${#tmp[@]}
  while [ "$n" -gt 0 ] && [ "$(trim "${tmp[$((n - 1))]}")" = "" ]; do n=$((n - 1)); done
  __nout=()
  for ((i = 0; i < n; i++)); do __nout+=("${tmp[$i]}"); done
}

build_diff() { # $1 old text, $2 new text
  local old="$1" new="$2"
  local -a OLD=() NEW=()
  split_to_array "$old" OLD
  split_to_array "$new" NEW
  local no=${#OLD[@]} nn=${#NEW[@]} p=0 s=0
  while [ "$p" -lt "$no" ] && [ "$p" -lt "$nn" ] && [ "${OLD[$p]}" = "${NEW[$p]}" ]; do p=$((p + 1)); done
  while [ "$s" -lt $((no - p)) ] && [ "$s" -lt $((nn - p)) ] && \
        [ "${OLD[$((no - 1 - s))]}" = "${NEW[$((nn - 1 - s))]}" ]; do s=$((s + 1)); done
  local omid=$((no - p - s)) nmid=$((nn - p - s)) changed
  if [ "$omid" -lt "$nmid" ]; then changed=$omid; else changed=$nmid; fi
  DIFF_CHANGED=$changed
  DIFF_ADDED=$((nmid - changed))
  DIFF_REMOVED=$((omid - changed))
  DIFF_IDENTICAL=0
  [ "$omid" -eq 0 ] && [ "$nmid" -eq 0 ] && DIFF_IDENTICAL=1
  DIFF_TEXT="--- a/$P_TARGET_REL"$'\n'"+++ b/$P_TARGET_REL"$'\n'"@@ -$((p + 1)),$omid +$((p + 1)),$nmid @@"
  local i
  for ((i = p; i < p + omid; i++)); do DIFF_TEXT+=$'\n'"-${OLD[$i]}"; done
  for ((i = p; i < p + nmid; i++)); do DIFF_TEXT+=$'\n'"+${NEW[$i]}"; done
}

# ---------------------------------------------------------------------------
# preview / apply
# ---------------------------------------------------------------------------

emit_diff() {
  if [ "$DIFF_IDENTICAL" = "1" ]; then
    printf '  (no changes)\n'
  else
    printf '%s\n' "$DIFF_TEXT"
  fi
}

print_summary() {
  local mode="$1" i cf="" fr="" sk=""
  for ((i = 0; i < ${#CF_KEY[@]}; i++)); do
    cf+=" ${CF_KEY[$i]}(existing='${CF_EXISTING[$i]}', template='${CF_TEMPLATE[$i]}')"
  done
  [ -n "$cf" ] || cf=" none"
  if [ "${#P_FRAG[@]}" -gt 0 ]; then fr="${P_FRAG[*]}"; else fr="none"; fi
  if [ "${#OUT_SKILLS[@]}" -gt 0 ]; then
    sk=$(printf '%s\n' "${OUT_SKILLS[@]}" | sed 's/^- //; s/ —.*$//' | tr '\n' ' ')
  else
    sk="none"
  fi
  printf '────────────────────────────────────────────\n'
  printf 'Mode:               %s\n' "$P_MODE"
  printf 'Target:             %s\n' "$P_TARGET_REL"
  printf 'Base template:      %s\n' "$P_BASE"
  printf 'Sections:           %s\n' "$(outcomes_summary)"
  printf 'Detected conflicts:%s\n' "$cf"
  printf 'Fragments:          %s\n' "$fr"
  printf 'Skills kept:        %s\n' "$sk"
  printf 'Managed-block hash: %s\n' "$OUT_BLOCK_HASH"
  if [ "$OUT_DRIFT" = "1" ]; then
    printf 'Drift:              ⚠ the managed block was edited by hand (stored hash mismatch)\n'
  fi
  if [ "$mode" = "update" ]; then
    if [ "$P_MODE" = "sync" ]; then
      printf 'Backup:             %s (created before the first byte changes)\n' "$BACKUP_NAME"
    else
      printf 'Backup:             none (init mode — nothing to lose)\n'
    fi
  elif [ "$mode" = "dry-run" ]; then
    if [ "$P_MODE" = "sync" ]; then
      printf 'Backup:             %s (would be created before the first byte changes)\n' "$BACKUP_NAME"
    else
      printf 'Backup:             none (init mode — nothing to lose)\n'
    fi
  else
    printf 'Backup:             none (no change — nothing will be written)\n'
  fi
  printf '────────────────────────────────────────────\n'
}

outcomes_summary() {
  local i out="" seen=" "
  for ((i = 0; i < ${#OUT_SECTION_OUTCOMES[@]}; i++)); do
    local o="${OUT_SECTION_OUTCOMES[$i]}"
    case "$seen" in *" $o "*) ;; *) seen+="$o "; out+="$o "; esac
  done
  [ -n "$out" ] || out="none"
  printf '%s' "${out% }"
}

emit_result_json() { # $1 = action
  local i first
  printf '{\n'
  printf '  "mode": "%s",\n' "$P_MODE"
  printf '  "target": "%s",\n' "$(json_escape_str "$P_TARGET_REL")"
  printf '  "absoluteTarget": "%s",\n' "$(json_escape_str "$P_TARGET_ABS")"
  printf '  "action": "%s",\n' "$1"
  printf '  "managedBlockHash": "%s",\n' "$OUT_BLOCK_HASH"
  printf '  "drift": %s,\n' "$([ "$OUT_DRIFT" = "1" ] && printf true || printf false)"
  printf '  "backupPath": "%s",\n' "$(json_escape_str "$P_ROOT/$BACKUP_NAME")"
  printf '  "diff": { "added": %s, "removed": %s, "changed": %s, "identical": %s },\n' \
    "$DIFF_ADDED" "$DIFF_REMOVED" "$DIFF_CHANGED" "$([ "$DIFF_IDENTICAL" = "1" ] && printf true || printf false)"
  printf '  "sections": ['
  first=1
  for ((i = 0; i < ${#OUT_SECTION_NAMES[@]}; i++)); do
    [ "$first" -eq 1 ] && first=0 || printf ','
    printf '\n    { "name": "%s", "outcome": "%s" }' \
      "$(json_escape_str "${OUT_SECTION_NAMES[$i]}")" "${OUT_SECTION_OUTCOMES[$i]}"
  done
  [ "${#OUT_SECTION_NAMES[@]}" -gt 0 ] && printf '\n  '
  printf '],\n'
  printf '  "conflicts": ['
  first=1
  for ((i = 0; i < ${#CF_KEY[@]}; i++)); do
    [ "$first" -eq 1 ] && first=0 || printf ','
    printf '\n    { "key": "%s", "existing": "%s", "template": "%s" }' \
      "$(json_escape_str "${CF_KEY[$i]}")" "$(json_escape_str "${CF_EXISTING[$i]}")" \
      "$(json_escape_str "${CF_TEMPLATE[$i]}")"
  done
  [ "${#CF_KEY[@]}" -gt 0 ] && printf '\n  '
  printf '],\n'
  printf '  "unresolvedExternals": %s\n' "$(review_unresolved_total)"
  printf '}\n'
}

check_review_blocking() { # $1 = 1 when the caller asked for --json
  local total blocking
  total=$(review_unresolved_total)
  blocking=$(review_blocking_count)
  if [ "$blocking" -gt 0 ]; then
    if [ "${1:-0}" = "1" ]; then
      printf '{\n  "action": "blocked",\n  "reason": "unresolved-external-skills",\n  "unresolvedExternals": %s,\n  "undecided": %s,\n  "review": %s\n}\n' \
        "$total" "$(review_undecided_count)" "$(review_explicit_count)"
    fi
    printf '⚠ %s unresolved external skill(s)\n' "$total" >&2
    fail 4 "$total external skill(s) are still unresolved" \
         "FR-005: the flow is blocked until every external skill has a decision — undecided ones always block, an explicit 'review' blocks unless --allow-review-todos is given (contracts.md C-8)" \
         "decide each one in plan.json (discard/keep/migrate/review); 'review' additionally needs --allow-review-todos"
  fi
}

# emit_conflicts_json — machine-readable payload for the exit-5 conflict path (T-010:
# "preview --json lists every unresolved conflict and exits 5")
emit_conflicts_json() {
  local i first=1
  printf '{\n  "action": "blocked",\n  "reason": "unresolved-conflicts",\n  "conflicts": ['
  for ((i = 0; i < ${#CF_KEY[@]}; i++)); do
    [ "$first" -eq 1 ] && first=0 || printf ','
    printf '\n    { "key": "%s", "existing": "%s", "template": "%s", "resolution": null }' \
      "$(json_escape_str "${CF_KEY[$i]}")" "$(json_escape_str "${CF_EXISTING[$i]}")" \
      "$(json_escape_str "${CF_TEMPLATE[$i]}")"
  done
  [ "${#CF_KEY[@]}" -gt 0 ] && printf '\n  '
  printf '],\n  "unresolvedExternals": %s\n}\n' "$(review_unresolved_total)"
}

cmd_preview() {
  local cfg="" json=0
  ALLOW_REVIEW=0
  while [ $# -gt 0 ]; do
    case "$1" in
      --config) [ $# -ge 2 ] || usage_err "--config needs a path"; cfg="$2"; shift 2 ;;
      --json) json=1; shift ;;
      --allow-review-todos) ALLOW_REVIEW=1; shift ;;
      -h | --help) cmd_help; return 0 ;;
      *) usage_err "unknown argument '$1' for 'preview'" ;;
    esac
  done
  [ -n "$cfg" ] || usage_err "'preview' needs --config <plan.json>" "preview cannot guess the merge decisions"
  plan_load "$cfg"
  check_review_blocking "$json"
  CONFLICT_REPORT=$(mktemp)
  compose
  local missing; missing=$(cat "$CONFLICT_REPORT" 2>/dev/null)
  rm -f "$CONFLICT_REPORT"
  if [ -n "$missing" ]; then
    local key
    [ "$json" = "1" ] && emit_conflicts_json
    printf '⚠ %s unresolved conflict(s)\n' "$(printf '%s\n' "$missing" | wc -l | tr -d ' ')" >&2
    while IFS='|' read -r key ex tp; do
      printf '  - key "%s": existing="%s" template="%s" → add it to plan.json.conflicts with a resolution\n' \
        "$key" "$ex" "$tp" >&2
    done <<< "$missing"
    fail 5 "plan.json does not resolve every detected conflict" \
         "an unresolved conflict would silently pick a winner (FR-007 Scenario D)" \
         "add each key above to plan.json.conflicts with resolution existing|template|<literal>"
  fi
  if [ "$OUT_DRIFT" = "1" ]; then
    local drifound=0 pi
    for ((pi = 0; pi < ${#P_CF_KEY[@]}; pi++)); do
      [ "${P_CF_KEY[$pi]}" = "managed-block" ] && drifound=1
    done
    if [ "$drifound" = "0" ]; then
      if [ "$json" = "1" ]; then
        printf '{\n  "action": "blocked",\n  "reason": "managed-block-drift",\n  "existingHash": "%s",\n  "recordedHash": "%s"\n}\n' \
          "$OUT_EXISTING_BLOCK_HASH" "${MB_STORED[0]:-}"
      fi
      printf '⚠ the managed block was edited by hand (hash drift)\n' >&2
      fail 5 "the managed block has drifted from its recorded hash" \
           "the user hand-edited the generated region; overwriting silently is forbidden (contracts.md C-5)" \
           "ask the user, then add { \"key\": \"managed-block\", \"resolution\": \"template\" } (regenerate) or \"existing\" (keep their version) to plan.json.conflicts"
    fi
  fi
  if [ "$json" = 1 ]; then
    emit_result_json "$OUT_ACTION"
    return 0
  fi
  printf '[6/6] MERGE PREVIEW\n────────────────────\n'
  if [ "$OUT_ACTION" = "no-change" ]; then
    printf '  ✓ no change — the target already matches this plan (idempotent no-op)\n'
  else
    emit_diff
  fi
  printf '\n'
  print_summary "$OUT_ACTION"
}

cmd_apply() {
  local cfg="" dry=0 json=0 verbose=0
  ALLOW_REVIEW=0
  while [ $# -gt 0 ]; do
    case "$1" in
      --config) [ $# -ge 2 ] || usage_err "--config needs a path"; cfg="$2"; shift 2 ;;
      --dry-run) dry=1; shift ;;
      --allow-review-todos) ALLOW_REVIEW=1; shift ;;
      --verbose) verbose=1; shift ;;
      --json) json=1; shift ;;
      -h | --help) cmd_help; return 0 ;;
      *) usage_err "unknown argument '$1' for 'apply'" ;;
    esac
  done
  [ -n "$cfg" ] || usage_err "'apply' needs --config <plan.json>" "apply never invents merge decisions"
  plan_load "$cfg"
  check_review_blocking "$json"
  CONFLICT_REPORT=$(mktemp)
  compose
  local missing; missing=$(cat "$CONFLICT_REPORT" 2>/dev/null)
  rm -f "$CONFLICT_REPORT"
  if [ -n "$missing" ]; then
    local key
    [ "$json" = "1" ] && emit_conflicts_json
    while IFS='|' read -r key ex tp; do
      printf '  - key "%s": existing="%s" template="%s"\n' "$key" "$ex" "$tp" >&2
    done <<< "$missing"
    fail 5 "plan.json does not resolve every detected conflict" \
         "an unresolved conflict would silently pick a winner (FR-007 Scenario D)" \
         "add each key above to plan.json.conflicts with resolution existing|template|<literal>"
  fi
  if [ "$OUT_DRIFT" = "1" ]; then
    local drifound=0 pi
    for ((pi = 0; pi < ${#P_CF_KEY[@]}; pi++)); do
      [ "${P_CF_KEY[$pi]}" = "managed-block" ] && drifound=1
    done
    if [ "$drifound" = "0" ]; then
      if [ "$json" = "1" ]; then
        printf '{\n  "action": "blocked",\n  "reason": "managed-block-drift",\n  "existingHash": "%s",\n  "recordedHash": "%s"\n}\n' \
          "$OUT_EXISTING_BLOCK_HASH" "${MB_STORED[0]:-}"
      fi
      fail 5 "the managed block has drifted from its recorded hash" \
           "the user hand-edited the generated region; overwriting silently is forbidden (contracts.md C-5)" \
           "ask the user, then add { \"key\": \"managed-block\", \"resolution\": \"existing\" } to keep their version or \"template\" to regenerate"
    fi
  fi

  if [ "$OUT_ACTION" = "no-change" ]; then
    if [ "$json" = 1 ]; then emit_result_json "no-change"; return 0; fi
    printf '✓ no change — nothing written (idempotent no-op, no backup churn)\n'
    return 0
  fi

  if [ "$dry" = 1 ]; then
    if [ "$json" = 1 ]; then emit_result_json "dry-run"; return 0; fi
    emit_diff
    printf '\n'
    print_summary "dry-run"
    printf 'DRY-RUN: no backup created, nothing written\n'
    return 0
  fi

  # source-changed guard re-checked immediately before the write (C-10)
  if [ "$P_MODE" = "sync" ]; then
    local now; now=$(sha256_of_file "$P_TARGET_ABS")
    if [ "$now" != "$P_ORIG_SHA" ]; then
      fail 5 "the target changed between preview and the write" \
           "plan expects $P_ORIG_SHA but the file is now $now" \
           "re-run analyze + preview and apply the fresh plan"
    fi
  fi

  apply_write
  if [ "$verbose" = 1 ]; then
    local i
    for ((i = 0; i < ${#OUT_SECTION_NAMES[@]}; i++)); do
      printf '  ~ section "%s" → %s\n' "${OUT_SECTION_NAMES[$i]}" "${OUT_SECTION_OUTCOMES[$i]}"
    done
    for ((i = 0; i < ${#P_FRAG[@]}; i++)); do printf '  ~ fragment %s\n' "${P_FRAG[$i]}"; done
  fi
  if [ "$json" = 1 ]; then emit_result_json "applied"; return 0; fi
  printf '[6/6] APPLICATION\n──────────────────\n'
  printf '  ✓ Copied %s section(s) from the original\n' "$(count_outcome copied)"
  printf '  ✓ Applied base template: %s\n' "$P_BASE"
  printf '  ✓ Applied %s fragment(s)\n' "${#P_FRAG[@]}"
  if [ "${#P_FRAG[@]}" -gt 0 ]; then
    local i
    for ((i = 0; i < ${#P_FRAG[@]}; i++)); do printf '    - %s\n' "${P_FRAG[$i]}"; done
  fi
  printf '\nAGENTS.md generated successfully.\n'
  printf 'Saved to: %s\n' "$P_TARGET_REL"
  if [ "$P_MODE" = "sync" ]; then
    printf 'Backup:   %s (restore with: /fforge-init --restore)\n' "$BACKUP_NAME"
  fi
}

count_outcome() {
  local want="$1" i n=0
  for ((i = 0; i < ${#OUT_SECTION_OUTCOMES[@]}; i++)); do
    [ "${OUT_SECTION_OUTCOMES[$i]}" = "$want" ] && n=$((n + 1))
  done
  printf '%s' "$n"
}

apply_write() {
  local dir; dir=$(dirname "$P_TARGET_ABS")
  [ -d "$dir" ] || fail 3 "directory '$dir' does not exist" "the target cannot be written" \
                          "create the project directory and re-run"
  [ -w "$dir" ] || fail 3 "cannot write to '$dir'" \
                        "the directory is not writable by $(id -un) (permission denied / read-only FS)" \
                        "check the directory permissions or run the command in a project you own"
  if [ -e "$P_TARGET_ABS" ] && [ ! -w "$P_TARGET_ABS" ]; then
    fail 3 "target '$P_TARGET_ABS' is not writable" "the file permissions deny the write" \
         "run 'chmod u+w $P_TARGET_ABS' (or restore from git) and re-run"
  fi

  local staged_backup="$dir/$BACKUP_NAME.tmp"
  local backup="$dir/$BACKUP_NAME"
  local staged_write="$dir/$STAGE_WRITE"
  local had_target=0
  [ -f "$P_TARGET_ABS" ] && had_target=1

  if [ "$had_target" = "1" ]; then
    cp -- "$P_TARGET_ABS" "$staged_backup" 2>/dev/null || fail 7 "could not stage the backup '$staged_backup'" \
        "copying the current target failed" "check free disk space and permissions, then re-run"
  fi
  if ! printf '%s' "$OUT_CONTENT" > "$staged_write"; then
    rm -f "$staged_backup" "$staged_write"
    fail 7 "could not write '$staged_write'" "the staged write failed (disk full, quota, or I/O error)" \
         "free disk space and re-run; the target was not modified"
  fi
  if ! mv -f -- "$staged_write" "$P_TARGET_ABS"; then
    if [ "$had_target" = "1" ] && [ -f "$staged_backup" ]; then
      cp -- "$staged_backup" "$P_TARGET_ABS" 2>/dev/null
    fi
    rm -f "$staged_backup" "$staged_write"
    fail 7 "replacing '$P_TARGET_ABS' failed" "the atomic rename failed after the staged copy was written" \
         "the target was restored from the staged backup; check disk health and re-run"
  fi
  if [ "$had_target" = "1" ]; then
    if ! mv -f -- "$staged_backup" "$backup"; then
      rm -f "$staged_backup"
      fail 7 "could not publish the backup '$backup'" \
           "the target was written but the backup rename failed; the previous backup is intact" \
           "check the directory permissions and re-run to create a fresh backup"
    fi
  fi
  rm -f "$staged_write"
}

# ---------------------------------------------------------------------------
# restore (contracts.md C-9)
# ---------------------------------------------------------------------------

cmd_restore() {
  local root="$PWD" dry=0 json=0
  while [ $# -gt 0 ]; do
    case "$1" in
      --root) [ $# -ge 2 ] || usage_err "--root needs a directory"; root="$2"; shift 2 ;;
      --dry-run) dry=1; shift ;;
      --json) json=1; shift ;;
      -h | --help) cmd_help; return 0 ;;
      *) usage_err "unknown argument '$1' for 'restore'" ;;
    esac
  done
  root=$(abs_path "$root")
  local target="$root/AGENTS.md" backup="$root/$BACKUP_NAME"
  guard_path_under_root "$target" "$root"
  if [ ! -f "$backup" ]; then
    fail 2 "no backup found at '$backup'" \
         "the backup is removed after a successful restore, and never created in init mode" \
         "run /fforge-init on a project that has an AGENTS.md to create one"
  fi
  if [ "$dry" = 1 ]; then
    if [ "$json" = 1 ]; then
      printf '{\n  "action": "restore-dry-run",\n  "backup": "%s",\n  "target": "%s",\n  "wouldRemoveBackup": true\n}\n' \
        "$(json_escape_str "$backup")" "$(json_escape_str "$target")"
      return 0
    fi
    printf 'DRY-RUN: would restore %s → %s and then remove the backup\n' "$BACKUP_NAME" "$target"
    return 0
  fi
  local dir; dir=$(dirname "$target")
  [ -w "$dir" ] || fail 3 "cannot write to '$dir'" "the directory is not writable by $(id -un)" \
                        "check the directory permissions and re-run"
  cp -- "$backup" "$target" 2>/dev/null || fail 7 "restore failed" "copying '$backup' over '$target' failed" \
      "check free disk space and permissions; the backup is still intact"
  rm -f "$backup" || fail 7 "restore copied the file but the backup could not be removed" \
      "the restore itself succeeded" "delete '$backup' manually"
  if [ "$json" = 1 ]; then
    printf '{\n  "action": "restored",\n  "target": "%s",\n  "backupRemoved": true\n}\n' "$(json_escape_str "$target")"
    return 0
  fi
  printf '✓ restored %s from %s\n' "$target" "$BACKUP_NAME"
  printf '  the backup has been removed (a successful restore consumes it)\n'
}

# ---------------------------------------------------------------------------
# version / help / dispatch
# ---------------------------------------------------------------------------

cmd_version() {
  printf '%s %s\n' "$PROG" "$(skill_version)"
  printf 'skill dir: %s\n' "$SKILL_DIR"
}

cmd_help() {
  cat <<EOF
$PROG — deterministic engine for the /fforge-init skill.

Contract: $(basename "$SKILL_DIR")/contracts.md (single source of truth)

USAGE
  $PROG detect    [--root <dir>] [--json]
  $PROG analyze   [--file <path>] [--root <dir>] [--json]
  $PROG classify  [--from-analysis <path.json>] [--names <csv>] [--json]
  $PROG recommend [--root <dir>] [--agent <name>] [--json]
  $PROG preview   --config <plan.json> [--json] [--allow-review-todos]
  $PROG apply     --config <plan.json> [--dry-run] [--allow-review-todos] [--verbose] [--json]
  $PROG restore   [--root <dir>] [--dry-run] [--json]
  $PROG version
  $PROG help

EXIT CODES (frozen — contracts.md C-8)
  0  success (also a dry-run or an idempotent no-op)
  1  usage error / unknown flag / missing argument
  2  target not found (analyze without AGENTS.md, restore without a backup)
  3  permission denied / read-only FS / path escape / symlink / > 1 MB / binary
  4  unresolved external skills — the flow is blocked (FR-005)
  5  unresolved merge conflict / ambiguous managed block / source changed under the plan
  6  invalid plan.json (schema or invariant violation)
  7  write failed mid-apply (backup preserved, target restored)

Every code >= 1 explains on stderr what failed, why, and how to fix it.

The engine never calls the network and needs nothing beyond the POSIX utilities
listed at the top of this file (NFR-006: self-containment).
EOF
}

main() {
  [ $# -ge 1 ] || usage_err "no subcommand given" "the engine needs one of detect/analyze/classify/recommend/preview/apply/restore/version/help"
  local cmd="$1"; shift
  case "$cmd" in
    detect) cmd_detect "$@" ;;
    analyze) cmd_analyze "$@" ;;
    classify) cmd_classify "$@" ;;
    recommend) cmd_recommend "$@" ;;
    preview) cmd_preview "$@" ;;
    apply) cmd_apply "$@" ;;
    restore) cmd_restore "$@" ;;
    version | --version | -V) cmd_version ;;
    help | --help | -h) cmd_help ;;
    *) usage_err "unknown subcommand '$cmd'" "the engine has no such command" ;;
  esac
}

# Dispatch only when executed, never when sourced (tests source this file).
if [ "${BASH_SOURCE[0]}" = "$0" ]; then
  main "$@"
fi
