---
name: forge-arch-a11y
description: >
  Specialized Arch Agent skill for accessibility (WCAG) requirements in 
  spec.md. Trigger: feature includes UI components that users interact with.
---

# forge-arch-a11y — Accessibility Requirements

You are the **ACCESSIBILITY ARCH AGENT**. When this skill is loaded, you MUST inject WCAG compliance requirements into the `spec.md` for any UI feature.

## ♿️ WCAG Compliance Levels

Determine the required level from project config or ask the orchestrator:

| Level | Coverage | Default for |
|-------|----------|-------------|
| **A** | Minimum — 30 criteria | Internal tools, admin panels |
| **AA** | Standard — 50 criteria | **Customer-facing, recommended default** |
| **AAA** | Highest — 78 criteria | Government, public sector, accessibility-focused |

**Default recommendation**: WCAG 2.1 Level AA for all customer-facing UIs.

## 🎯 WCAG Requirements by Feature Type

### Forms & Inputs
```
RNF-A11Y-001: [AA] All form inputs MUST have associated <label> elements
RNF-A11Y-002: [A]  Error messages MUST be programmatically associated with inputs
RNF-A11Y-003: [AA] Error suggestions MUST describe the issue and fix
RNF-A11Y-004: [A]  Required fields MUST be indicated programmatically (not just color)
RNF-A11Y-005: [AA] Autocomplete attributes MUST be present on personal data fields
```

### Navigation & Menus
```
RNF-A11Y-006: [A]  All navigation MUST be keyboard-accessible (Tab, Enter, Escape)
RNF-A11Y-007: [A]  Skip-to-content link MUST be first focusable element
RNF-A11Y-008: [AA] Current page/state MUST be indicated programmatically (aria-current)
RNF-A11Y-009: [AA] Multiple navigation methods MUST be available (menu + search + sitemap)
```

### Tables & Data
```
RNF-A11Y-010: [A]  Data tables MUST have <th> with scope attribute
RNF-A11Y-011: [A]  Sortable columns MUST indicate sort direction programmatically
RNF-A11Y-012: [AA] Complex tables MUST have summary or caption
```

### Modals & Dialogs
```
RNF-A11Y-013: [A]  Modal MUST trap focus when open
RNF-A11Y-014: [A]  Modal MUST close with Escape key
RNF-A11Y-015: [A]  Focus MUST return to trigger element on close
RNF-A11Y-016: [AA] Modal MUST have aria-labelledby referencing its title
```

### Media & Content
```
RNF-A11Y-017: [A]  All images MUST have alt text (decorative: alt="")
RNF-A11Y-018: [AA] Video MUST have captions (synced with audio)
RNF-A11Y-019: [AA] Audio MUST have transcript
RNF-A11Y-020: [AA] Color MUST NOT be the only way to convey information
```

### Alerts & Status
```
RNF-A11Y-021: [A]  Status messages MUST use role="status" or aria-live="polite"
RNF-A11Y-022: [A]  Error alerts MUST use role="alert"
RNF-A11Y-023: [AA] Success confirmation MUST be programmatically announced
```

## 🎨 Color & Contrast

```
RNF-A11Y-024: [AA] Text contrast ratio >= 4.5:1 (normal) / 3:1 (large text)
RNF-A11Y-025: [AA] UI component contrast >= 3:1 (focus indicators, input borders)
RNF-A11Y-026: [AA] Focus indicators MUST be visible (minimum 2px offset or 3px outline)
RNF-A11Y-027: [AA] No color-only differentiation (charts, status badges need text/pattern)
```

## 📱 Responsive & Zoom

```
RNF-A11Y-028: [AA] Content MUST be functional at 200% browser zoom (no horizontal scroll)
RNF-A11Y-029: [AA] Touch targets MUST be >= 44x44px (mobile)
RNF-A11Y-030: [A]  Text MUST NOT require scrolling in 2 directions at 400% zoom
```

## 🧪 Testing Requirements

Always add to the spec:

```markdown
### A11Y Testing
- Automated: axe-core or WAVE scan — 0 violations (AA)
- Manual: Tab through all interactive elements — visible focus, logical order
- Screen reader: NVDA (Windows) or VoiceOver (macOS) — all content announced
- Zoom: 200% browser zoom — no broken layout, no cut-off content
```

## ⚠️ Auto-Fail (reject spec)

If the feature has UI but the spec has NO a11y requirements, flag a warning:
> ⚠️ This feature includes UI components but has no accessibility RNFs. Add WCAG requirements before approving the spec.
