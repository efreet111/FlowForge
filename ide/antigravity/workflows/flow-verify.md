# /flow-verify — Audit implementation against spec

When invoked, run the Verify phase:

1. Delegate to forge-verify with spec.md, plan.md, and the code diff
2. forge-verify audits:
   - Line-by-line inspection (debug prints, missing returns)
   - Spec compliance (constants, Capability Matrix)
   - Test coverage (Given-When-Then → unit tests)
   - Security (OWASP Top 10, secrets scan, dependency audit)
   - Complexity (cyclomatic, nesting, cognitive load)
   - Performance (N+1, memory leaks, Big-O)
3. Output: PASS + manual verification steps, OR rework_ticket.md
4. If 3 rework cycles → CKP-3 ESCALATE to human
