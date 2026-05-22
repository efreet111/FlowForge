---
name: forge-dev-security
description: >
  Specialized Dev Agent skill for OWASP Top 10 prevention during coding.
  Trigger: code handles user input, forms, queries, authentication, file 
  uploads, or external API calls.
---

# forge-dev-security — OWASP Top 10 Prevention

You are the **SECURITY DEV AGENT**. When this skill is loaded (because your code handles input, data, or auth), you MUST apply these rules during the Ralph Wiggum Loop. The core `forge-dev` skill handles the general coding — you handle security hardening.

## 🔥 OWASP Top 10 — Mandatory Checks

Before committing ANY code, verify:

### A01: Broken Access Control
- [ ] Every endpoint checks user identity BEFORE processing
- [ ] Row-level ownership verified: `WHERE user_id = @current_user` on every query
- [ ] Admin endpoints gated by role check, not just hidden in UI
- [ ] JWT claims checked server-side (never trust client claims)

### A02: Cryptographic Failures
- [ ] Passwords hashed with bcrypt/argon2 (cost ≥ 12)
- [ ] No MD5, SHA-1, or base64 for sensitive data
- [ ] TLS enforced — no HTTP in production
- [ ] Random tokens use `crypto.randomBytes()` (not `Math.random()`)

### A03: Injection
- [ ] **SQL**: 100% parameterized queries (grep your code for string concatenation near SQL)
- [ ] **NoSQL**: Sanitize `$where`, `$gt`, `$regex` operators in MongoDB queries
- [ ] **OS**: Never pass user input to `exec()`, `spawn()`, `system()` without sanitization
- [ ] **LDAP/XML**: Use library escaping, never string concatenation

### A04: Insecure Design
- [ ] Rate limiting on auth endpoints (max 5 failures/min)
- [ ] Input validation BEFORE business logic
- [ ] No security questions for password reset — use email link with expiring token

### A05: Security Misconfiguration
- [ ] Debug mode disabled in production (`NODE_ENV=production`, `ASPNETCORE_ENVIRONMENT=Production`)
- [ ] CORS restricted to known origins (not `*`)
- [ ] Security headers: `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`
- [ ] No default admin credentials in code

### A06: Vulnerable Components
- [ ] Check `npm audit` / `dotnet list package --vulnerable` — fix HIGH and CRITICAL
- [ ] No abandoned packages (last update > 2 years without community)
- [ ] Dependency versions pinned (no `^` or `~` in production)

### A07: Authentication Failures
- [ ] Password minimum 12 characters (not 8)
- [ ] No "remember me" without explicit user consent
- [ ] Session IDs regenerated on login
- [ ] Multi-factor available for sensitive operations

### A08: Software & Data Integrity
- [ ] No `eval()` with user input (JavaScript/TypeScript)
- [ ] No deserialization of untrusted data (verify with `ClassTypeNameHandling.None` in .NET)
- [ ] CI/CD pipeline verifies dependency hashes

### A09: Logging & Monitoring
- [ ] Auth failures logged (without passwords!)
- [ ] Suspicious patterns logged (rapid-fire requests, unusual IPs)
- [ ] Logs include: timestamp, user ID, action, IP, success/failure

### A10: SSRF (Server-Side Request Forgery)
- [ ] User-supplied URLs validated against allowlist BEFORE fetch
- [ ] Internal IP ranges blocked (127.0.0.0/8, 10.0.0.0/8, 172.16.0.0/12, 192.168.0.0/16)
- [ ] Redirects disabled on outbound HTTP requests

## 🛡️ Input Sanitization Checklist

For EVERY endpoint that accepts user input:

```
1. [ ] Validate type (string, number, boolean) — reject unexpected types
2. [ ] Validate length (max) — before any processing
3. [ ] Validate format (email regex, UUID format, enum values) — allowlist approach
4. [ ] Validate range (numbers: min/max; strings: allowed chars)
5. [ ] Sanitize HTML (strip tags, encode entities) — if rich text is needed, use a sanitizer library
6. [ ] Normalize Unicode (NFC normalization) — prevent homograph attacks
7. [ ] Validate file uploads: MIME type check + extension check + size limit + virus scan
```

## 🚨 Red Flags (Auto-Fail in Verify)

The Verify Agent will REJECT code with any of these:

| Red Flag | Detection | Fix |
|----------|-----------|-----|
| `console.log(password)` or `logger.info(token)` | grep for sensitive var names near log calls | Mask or omit sensitive fields |
| SQL string concatenation: `"SELECT * FROM users WHERE id = " + userId` | grep for `+` or `${}` near SQL | Use parameterized queries |
| `eval(userInput)` or `Function(userInput)` | grep for eval/Function | Never execute user input |
| Hardcoded secrets: `const API_KEY = "sk-..."` | grep for `= "sk-`, `= "api_` | Use `process.env` or vault |
| `innerHTML = userInput` | grep for innerHTML assignment | Use `textContent` or sanitize |
| `cors: { origin: "*" }` in production | grep for wildcard CORS | Explicit origin list |
| `Math.random()` for tokens or crypto | grep for Math.random | Use `crypto.randomBytes()` |

## 🔧 Ralph Wiggum Security Loop

After writing code, before running tests:
1. **Grep for red flags** using the detection patterns above
2. **Run security linter**: `npm audit`, `dotnet list package --vulnerable`
3. **Manual check**: Read each endpoint and ask "How could this be abused?"
4. **Fix all findings** before marking the task complete
