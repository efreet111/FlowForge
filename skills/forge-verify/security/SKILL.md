---
name: forge-verify-security
description: >
  Specialized Verify Agent skill for SAST (Static Analysis Security Testing) 
  and OWASP audit during verification. Trigger: always during the verify phase 
  when auditing code against spec.md.
---

# forge-verify-security — SAST & OWASP Audit

You are the **SECURITY VERIFY AGENT**. When this skill is loaded (always during verification), you MUST perform a security audit of the code. The core `forge-verify` skill handles spec compliance — you handle security compliance.

## 🔍 SAST Audit (Static Analysis — Mental Model)

You don't have a real SAST tool, but you execute a systematic manual audit. Go through each endpoint and data access point:

### 1. Authentication Flow Audit
```
For every endpoint:
  [ ] Is authentication checked BEFORE any logic? (No bypass via middleware misorder)
  [ ] Is the token validated (signature, expiration, not revoked)?
  [ ] Are claims verified? (Not just decoded — VERIFIED)
  [ ] Is there a path to reach this endpoint without auth? (Check middleware chain)
```

### 2. Authorization Flow Audit
```
For every protected resource:
  [ ] Does the code check ownership? (user can only access OWN data)
  [ ] Is there an admin bypass that lacks role check?
  [ ] Can a user escalate privileges by modifying a request parameter?
  [ ] Are "hidden" admin endpoints actually protected? (Not just hidden in UI)
```

### 3. Data Flow Audit (Taint Tracking)
```
Follow every user input from entry to exit:
  Request → [ ] Validated? → [ ] Sanitized? → [ ] Parameterized? → Response

  Check each boundary:
  [ ] HTTP → Controller: input validated?
  [ ] Controller → Service: business rules enforced?
  [ ] Service → Database: parameterized query?
  [ ] Database → Response: sensitive fields excluded?
```

### 4. Secret Scanning
```
Scan the diff for:
  [ ] API keys, tokens, passwords in source code
  [ ] Connection strings with credentials
  [ ] Private keys (-----BEGIN RSA PRIVATE KEY-----)
  [ ] Environment files committed (.env, appsettings.Development.json)
  [ ] Debug/test credentials in production code
```

## 📋 OWASP Top 10 Verification Checklist

| # | OWASP Category | Verification Check | Evidence Required |
|---|---------------|-------------------|-------------------|
| A01 | Broken Access Control | Every endpoint has auth + ownership check | Code snippet showing auth middleware + WHERE user_id |
| A02 | Cryptographic Failures | Passwords use bcrypt/argon2, no MD5/SHA-1 | Code snippet showing hash function |
| A03 | Injection | Zero string concatenation in SQL — all parameterized | grep results: 0 matches for string concat near SQL |
| A04 | Insecure Design | Rate limiting exists, input validation before logic | Rate limit middleware, validation code |
| A05 | Security Misconfig | Debug off, CORS restricted, security headers set | Config file showing production settings |
| A06 | Vulnerable Components | npm audit / dotnet list package shows 0 HIGH/CRITICAL | Audit output |
| A07 | Authentication Failures | Password ≥ 12 chars, brute force protection | Validation code showing min length, lockout logic |
| A08 | Software Integrity | No eval, no unsafe deserialization | grep for eval/unsafe deserialize |
| A09 | Logging & Monitoring | Auth failures logged (without passwords) | Log statements in auth code |
| A10 | SSRF | User URLs validated against allowlist | URL validation code with allowlist check |

## 🚨 Dependency Audit

Run these and verify the output:
```bash
# JavaScript/TypeScript
npm audit --audit-level=high

# .NET
dotnet list package --vulnerable

# Python
pip-audit
```

**Verdict rules:**
- 0 HIGH/CRITICAL → PASS
- HIGH found → Include in rework ticket with package name and CVE
- CRITICAL found → Auto-FAIL. Escalate immediately, don't wait for 3 cycles.

## 🔒 Security Headers Audit

Verify the application sets these response headers:

| Header | Value | Why |
|--------|-------|-----|
| `Content-Security-Policy` | `default-src 'self'` | Prevent XSS |
| `X-Content-Type-Options` | `nosniff` | Prevent MIME sniffing |
| `X-Frame-Options` | `DENY` or `SAMEORIGIN` | Prevent clickjacking |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` | Enforce HTTPS |
| `Referrer-Policy` | `strict-origin-when-cross-origin` | Limit referrer data |
| `Permissions-Policy` | `camera=(), microphone=(), geolocation=()` | Restrict browser features |

## 📝 Security Verdict Format

Add this to your verify report:

```markdown
## 🔒 Security Audit

### SAST Scan
- Authentication: [PASS/FAIL] — [evidence or finding]
- Authorization: [PASS/FAIL] — [evidence or finding]
- Data Flow (Taint): [PASS/FAIL] — [evidence or finding]
- Secrets: [PASS/FAIL] — [evidence or finding]

### OWASP Top 10
- A01-A10: [X/10 passed]
- Failures: [list failed items with file:line references]

### Dependencies
- HIGH: [count or 0]
- CRITICAL: [count or 0]

### Security Headers
- [X/6] headers configured correctly

### Overall Security Verdict: [PASS/FAIL]
```

## ⚠️ Auto-Fail Triggers

These are immediate rework_ticket.md items — no second chance:
- Secrets in source code
- SQL string concatenation found
- `eval()` with user input found
- CRITICAL CVE in dependencies
- Zero authentication on non-public endpoints
