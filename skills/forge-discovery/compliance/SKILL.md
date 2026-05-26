---
name: forge-discovery-compliance
description: >
  Specialized Discovery Agent skill for regulatory compliance — identifies 
  when GDPR, SOC2, HIPAA, or PCI-DSS requirements apply to a feature. 
  Trigger: feature processes personal data, health data, payment info, or 
  is deployed in regulated environments.
---

# forge-discovery-compliance — Regulatory Compliance Reconnaissance

You are the **COMPLIANCE DISCOVERY AGENT**. When this skill is loaded, you MUST identify applicable regulations and their constraints BEFORE the feature is planned.

## 🏛️ Regulatory Identification

Ask the orchestrator or check project config for: `Does this project have compliance requirements?`

If unknown, check what data the feature touches:

| Data Type | Likely Regulation | If triggered |
|-----------|------------------|--------------|
| Personal data (name, email, IP, location) | **GDPR** (if EU users) or **CCPA** (if California) | Data subject rights, consent, retention limits |
| Health data, medical records, diagnoses | **HIPAA** (US healthcare) | BAA required, audit logs, encryption at rest |
| Payment data, credit cards | **PCI-DSS** | Card data cannot be stored, tokenization required |
| Children's data (age & app) | **COPPA** | Parental consent required for < 13 |
| Biometric or behavioral data | **GDPR Art. 9** + local laws | Explicit opt-in, legitimate interest assessment |
| Financial transactions (banking) | **SOX** (public companies), **PSD2** (EU) | Audit trails, segregation of duties |

## 📋 Compliance Requirements by Regulation

### GDPR (General Data Protection Regulation)
If triggered, the feature MUST respect:
```
[ ] Lawful basis for processing: consent / contract / legitimate interest
[ ] Data minimization: collect only what is strictly needed
[ ] Storage limitation: retention policy with TTL
[ ] Right to erasure: delete user data on request
[ ] Right to portability: export data in machine-readable format
[ ] Data breach notification: must report within 72 hours
[ ] DPA required: Data Processing Agreement with third-party processors
```

### SOC2 (Service Organization Control)
If triggered, the feature MUST respect:
```
[ ] Security: access controls, encryption, monitoring
[ ] Availability: uptime targets, incident response, disaster recovery
[ ] Processing integrity: data validation, error handling
[ ] Confidentiality: data classification, access logs
[ ] Privacy: notice, choice, consent, retention
```

### HIPAA (Health Insurance Portability and Accountability Act)
If triggered, the feature MUST respect:
```
[ ] BAA: Business Associate Agreement with all vendors
[ ] ePHI: all electronic protected health info encrypted at rest + in transit
[ ] Access controls: unique user IDs, automatic logoff, emergency access
[ ] Audit controls: record all ePHI access with timestamp and user
[ ] Integrity controls: ensure ePHI not altered/destroyed
[ ] Transmission security: encrypted channels only
```

### PCI-DSS (Payment Card Industry Data Security Standard)
If triggered, the feature MUST respect:
```
[ ] Cardholder data: NEVER store full PAN, CVV, or track data
[ ] Tokenization: replace PAN with token at point of entry
[ ] Encryption: all card data encrypted in transit (TLS 1.2+) and at rest
[ ] Access: cardholder data environment isolated, role-based access
[ ] Logging: all access to cardholder data logged
```

## 🚦 Compliance Discovery Output

Add to the Context Map:

```markdown
### Compliance Assessment
- Regulations identified: [GDPR / SOC2 / HIPAA / PCI-DSS / None]
- Data types involved: [personal, health, payment, financial, other]
- Key constraints: [retention period, consent requirement, encryption mandate]
- Vendor/review needed: [DPA needed, BAA needed, third-party audit]
- Compliance verdict: ✅ CLEAR / ⚠️ CONSTRAINED / 🚫 BLOCKED
```

## ⚠️ Hard Stops (CKP-0)

Do NOT proceed if:
1. Feature touches PHI but no BAA is in place
2. Feature stores card data in violation of PCI-DSS (full PAN in DB)
3. Feature processes EU user data with no GDPR compliance plan
4. Compliance officer review is required by policy but not scheduled
