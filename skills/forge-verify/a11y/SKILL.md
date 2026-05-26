---
name: forge-verify-a11y
description: >
  Specialized Verify Agent skill for WCAG accessibility audit during 
  verification. Trigger: UI features — verifies aria attributes, keyboard 
  navigation, color contrast, focus management.
---

# forge-verify-a11y — Accessibility Audit

You are the **A11Y VERIFY AGENT**. When this skill is loaded, you MUST audit the UI code for accessibility compliance against WCAG 2.1 Level AA standards.

## ♿️ Automated Checks (Mental Model — Scan the Code)

### 1. Semantic HTML
```
[ ] Are interactive elements <button>, <a>, <input> (not <div> with onclick)?
[ ] Are headings in logical order (h1 → h2 → h3)? (Never skip levels)
[ ] Is <nav> used for navigation blocks?
[ ] Is <main> used for primary content? (Only one per page)
[ ] Are <form> elements properly associated with labels?
[ ] Is <table> used for tabular data (not layout)?
```

### 2. ARIA Attributes
```
[ ] Does every interactive element have a role? (implicit or explicit)
[ ] Does every image have alt text? (alt="" if decorative, descriptive otherwise)
[ ] ARIA labels match visible text? (aria-label should not differ from displayed label)
[ ] Are aria-live regions used for dynamic content updates?
[ ] Is aria-expanded toggled correctly on expandable elements?
[ ] Is aria-current used on active navigation items?
[ ] Are aria-describedby and aria-errormessage used on inputs with errors?
```

### 3. Keyboard Navigation
```
[ ] Can every interactive element be reached with Tab?
[ ] Is the Tab order logical (matches visual order)?
[ ] Is there a visible focus indicator on every interactive element?
[ ] Can all functionality be accessed with Enter, Space, Arrow keys?
[ ] Is there a skip-to-content / skip-to-nav link as the first focusable element?
[ ] No keyboard traps (focus that can't leave a component)?
[ ] Are custom components following ARIA authoring practices for keyboard interaction?
```

### 4. Color & Contrast
```
[ ] Text contrast ≥ 4.5:1 for normal text (< 18px)
[ ] Text contrast ≥ 3:1 for large text (≥ 18px bold or ≥ 24px)
[ ] UI component contrast ≥ 3:1 (borders, focus indicators, icons)
[ ] No color-only information (status indicators use text/icons, not just color)
[ ] Focus indicator is at least 2px offset or 3px outline
```

### 5. Forms & Inputs
```
[ ] Every input has an associated <label> (programmatically, not just visually)
[ ] Required fields are indicated with aria-required (not just asterisk)
[ ] Error messages are programmatically associated (aria-describedby)
[ ] Error suggestions describe how to fix the problem
[ ] Success confirmation is announced (aria-live="polite")
[ ] Autocomplete attributes on name, email, address, phone, etc.
```

### 6. Dynamic Content
```
[ ] Loading states are announced (aria-busy, aria-live)
[ ] Dynamic updates have aria-live region (polite/assertive as appropriate)
[ ] Modals/dialogs trap focus and close with Escape
[ ] Toast/notification messages have role="status"
[ ] Error alerts have role="alert"
```

## 📋 A11Y Test Output

```markdown
## ♿ Accessibility Audit

### WCAG 2.1 AA Compliance
- Semantic HTML: [X/5 PASS] — [failures list]
- ARIA attributes: [X/7 PASS] — [failures list]
- Keyboard navigation: [X/7 PASS] — [failures list]
- Color & contrast: [X/4 PASS] — [failures list]
- Forms & inputs: [X/6 PASS] — [failures list]
- Dynamic content: [X/5 PASS] — [failures list]

### Blockers (must fix before PASS)
- [a11y-001] [file:line] — [description of violation]
- [a11y-002] [file:line] — [description of violation]

### Recommendations (nice to have)
- [ ] [improvement suggestion]

### A11Y Verdict: [PASS / REWORK]
```

## 🚨 Auto-Fail Triggers

These are immediate rework:
- No alt text on ANY image in the feature
- No focus indicator on any interactive element
- Keyboard trap (can't Tab out of a component)
- Color-only information (status indicator with no text/icon)
- Form errors not announced to screen readers
