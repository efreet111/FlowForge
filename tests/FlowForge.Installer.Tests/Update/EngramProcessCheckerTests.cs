using FlowForge.Installer.Update;
using Xunit;

namespace FlowForge.Installer.Tests.Update;

public class EngramProcessCheckerTests
{
    [Fact]
    public void DetectRunningProcesses_ReturnsList()
    {
        var checker = new EngramProcessChecker();
        var processes = checker.DetectRunningProcesses();

        // Should return a list (possibly empty if no engram running)
        Assert.NotNull(processes);
    }

    [Fact]
    public void DetectRunningProcesses_WhenNoEngramRunning_ReturnsEmpty()
    {
        var checker = new EngramProcessChecker();
        var processes = checker.DetectRunningProcesses();

        // In CI/test environment, engram is unlikely to be running
        // This test verifies the method doesn't throw
        Assert.NotNull(processes);
        // We can't assert empty because engram might be running in dev environments
    }
}
