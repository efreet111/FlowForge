# /flow-close — Close the feature and persist memory

When invoked, run the Memory phase (CKP-4):

1. Delegate to forge-memory to:
   - Write session summary (Goal, Discoveries, Accomplished, Next Steps)
   - Persist learnings to engram-dotnet (mem_save)
   - Promote architecture decisions to ADRs (mem_promote_to_md)
   - Capture metrics: test coverage, cycle time, tech debt
   - Update CHANGELOG.md
2. Present: "Feature complete. Deploy?" (CKP-4)
