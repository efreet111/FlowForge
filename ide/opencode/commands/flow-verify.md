---
description: Verify implementation against spec and plan
agent: flowforge
---
Verify implementation against spec.md and plan.md.

Run LLM-as-Judge audit:
- Check all functional requirements implemented
- Validate acceptance criteria met
- Review code quality and test coverage

Generate verify-report.md with PASS/REWORK verdict.
If REWORK: create rework tickets and return to flow-dev.
If PASS: proceed to flow-close.
