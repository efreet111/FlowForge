# Copilot Instructions — FlowForge

You are working in a project that follows the **FlowForge Agentic SDLC methodology**.

## Workflow Rules

1. **5 checkpoints control the flow**: CKP-0 (Hard Stop), CKP-1 (Spec Approval), CKP-2 (Plan Approval), CKP-3 (Escalation), CKP-4 (Deploy Gate).

2. **Before coding**: Write a spec.md (functional requirements with Given-When-Then scenarios) and get human approval (CKP-1).

3. **Before implementing**: Break the spec into an ordered plan.md with task checklist and get human green light (CKP-2).

4. **Implementation**: Follow plan.md exactly. Don't modify files not listed. Write unit tests for every Given-When-Then scenario. Use "Ralph Wiggum Loop" (test → fail → fix → repeat until green).

5. **Verification**: After implementation, audit code line-by-line against spec.md. If any deviation — generate rework instructions. Max 3 rework cycles.

6. **Closure**: Save learnings as observations (mem_save), promote important decisions to ADRs.

## Security

- Always use parameterized queries (no string concatenation in SQL)
- Hash passwords (bcrypt, never MD5/SHA-1)
- No secrets in code (use environment variables)
- Validate all user input server-side
- Apply OWASP Top 10 principles

## Quality

- Apply SOLID principles (single responsibility, open/closed, Liskov, interface segregation, dependency inversion)
- Keep functions under 30 lines, classes under 200 lines
- Max 4 parameters per function
- Cyclomatic complexity under 10 per function

## Performance

- Detect N+1 queries: never query inside loops
- Use eager loading if related data is always accessed
- Add pagination to all list endpoints
- Cache reference data with TTL

## Accessibility (for UI code)

- WCAG 2.1 AA required for customer-facing features
- Every input has a label, every image has alt text
- All interactive elements reachable by keyboard (Tab, Enter, Escape)
- Color contrast ≥ 4.5:1 for text

## Git

- Commit freely, but NEVER push without explicit human request
