using FlowForge.Installer.Infrastructure;
using System.Collections.Generic;
using Xunit;

namespace FlowForge.Installer.Tests;

public class PathHelperTests
{
    [Fact]
    public void FR_007_OwnershipTargetsIncludeCriticalPaths()
    {
        var targets = new HashSet<string>(PathHelper.OwnershipTargets);
        Assert.Contains(PathHelper.InstallerBinary, targets);
        Assert.Contains(PathHelper.EngramBinary, targets);
        Assert.Contains(PathHelper.EngramDir, targets);
        Assert.Contains(PathHelper.NativeSqliteLib, targets);
    }
}
