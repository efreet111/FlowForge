# Security Policy

## Supported versions

FlowForge is evolving. Use the latest version on `main` when possible.

## Reporting a vulnerability

Do **not** open a public issue with exploitable details.

Include:

- Description and impact.
- Steps to reproduce.
- Affected files (e.g. `ide/install.ps1`, `ide/install.sh`, `skills/**/SKILL.md`).
- Environment (OS, IDE).

## In scope

- Installers that **overwrite files outside expected directories** without warning.
- Accidental secret exposure in docs or scripts.
- Instructions that encourage unsafe practices without explanation.

## Out of scope

- General methodology disagreements (use regular issues).
- Third-party IDE or model provider vulnerabilities (report to the vendor).
