# ADR-026: FlowForge Installer Cache Refresh Strategy

**Date**: 2026-09-18  
**Status**: Accepted  
**Deciders**: FlowForge Team

## Context

The FlowForge installer clones the FlowForge repository to `~/.flowforge/cache/FlowForge` to access IDE configuration files (agents, commands, skills). Initially, the cache was cloned once and never refreshed, causing users to receive stale files even after upstream updates were merged.

**Problem**: When new files were added to the repository (e.g., OpenCode commands in `ide/opencode/commands/`), users with existing caches would not receive them because `FlowForgeRepoLocator.EnsureAvailable()` accepted the cache as-is without checking for updates.

**Impact**: Users running `flowforge init` would get outdated configurations, leading to missing features and confusion.

## Decision

Implement automatic cache refresh in `FlowForgeRepoLocator.EnsureAvailable()` with the following strategy:

1. **Refresh mechanism**: Execute `git pull --ff-only` on the cache directory
2. **Fallback**: If fast-forward fails (diverged branches), execute `git fetch origin main` followed by `git reset --hard origin/main`
3. **Scope**: Only refresh the managed cache at `~/.flowforge/cache/FlowForge`, never touch development repositories
4. **Non-blocking**: If refresh fails (offline, corrupted, no git), log a warning and continue with existing cache
5. **Detection**: Use `IsCachePath()` to distinguish managed cache from development repositories (environment variables, local project walk)

## Implementation Details

### Code Changes

```csharp
public bool EnsureAvailable(out string? repoPath)
{
    repoPath = Locate();
    if (repoPath != null)
    {
        // Only refresh managed cache, not dev repos
        if (IsCachePath(repoPath))
        {
            RefreshCache(repoPath);
        }
        return true;
    }
    
    return TryClone(out repoPath);
}

private void RefreshCache(string cachePath)
{
    try
    {
        var oldSha = GetCurrentSha(cachePath);
        
        // Try fast-forward first
        var exitCode = RunGit("pull --ff-only", cachePath);
        
        if (exitCode != 0)
        {
            // Fallback: fetch + hard reset
            RunGit("fetch origin main", cachePath);
            RunGit("reset --hard origin/main", cachePath);
        }
        
        var newSha = GetCurrentSha(cachePath);
        if (oldSha != newSha)
        {
            _logger.Info($"Cache refreshed: {oldSha[..7]} → {newSha[..7]}");
        }
    }
    catch (Exception ex)
    {
        _logger.Warn($"Cache refresh failed: {ex.Message}. Continuing with existing cache.");
    }
}
```

### Test Coverage

- `EnsureAvailable_RefreshesCache_WhenCacheIsStale`: Verifies pull is executed
- `EnsureAvailable_ContinuesWithExistingCache_WhenOffline`: Verifies non-blocking behavior
- `EnsureAvailable_DoesNotRefresh_WhenPathIsFromEnvironment`: Verifies dev repos are not touched
- `EnsureAvailable_SkipsRefresh_WhenCacheIsNotGitRepo`: Verifies graceful handling of non-git caches

## Consequences

### Positive

1. **Users always get latest files**: Cache is automatically refreshed on every `flowforge init`
2. **Offline resilience**: Installer continues to work without internet (uses existing cache)
3. **Development safety**: Local development repositories are never modified
4. **Transparency**: SHA changes are logged for debugging

### Negative

1. **Network dependency**: Refresh requires internet access (mitigated by non-blocking fallback)
2. **Performance overhead**: Each `flowforge init` now includes a git pull (typically <1s)
3. **Potential for divergence**: Hard reset fallback can discard local changes (acceptable for managed cache)

### Risks

1. **Corrupted cache**: If cache becomes corrupted, refresh may fail. Mitigation: delete cache and re-clone
2. **Breaking changes**: Upstream changes could break existing installations. Mitigation: semantic versioning and release notes

## Alternatives Considered

### Alternative 1: Manual cache refresh command
Add `flowforge cache refresh` command for users to run manually.

**Rejected**: Users shouldn't need to manually refresh; automatic refresh is more user-friendly.

### Alternative 2: Version-stamped cache
Include version number in cache directory name (e.g., `FlowForge-v0.1.0-alpha.14`).

**Rejected**: Would require re-cloning on every release, wasting bandwidth and disk space.

### Alternative 3: Always re-clone
Delete and re-clone cache on every `flowforge init`.

**Rejected**: Too slow and bandwidth-intensive; git pull is much faster.

## Related Decisions

- **ADR-012**: IDE-specific model configuration (files accessed via cache)
- **ADR-017**: Installer protection policy (backup strategy complements cache refresh)

## References

- HU-038: OpenCode slash commands missing
- PR #32: Fix installer cache refresh
- Release: v0.1.0-alpha.14
