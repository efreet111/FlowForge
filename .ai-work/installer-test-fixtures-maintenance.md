# Installer test fixtures maintenance

## Change

- Added explicit test-output content items with `TargetPath` metadata for
  `README.md`, `README.es.md`, `install/install.sh`,
  `src/FlowForge.Installer/Commands/InstallCommand.cs`,
  `src/FlowForge.Installer/Modules/EngramModule.cs`, and
  `src/FlowForge.Installer/Models/InstallerConfig.cs` in
  `tests/FlowForge.Installer.Tests/FlowForge.Installer.Tests.csproj`.
- Standardized source-fixture tests on `AppContext.BaseDirectory`, so fixtures
  are read from these exact paths under `bin/Release/net10.0/`:
  `README.md`, `README.es.md`, `install/install.sh`,
  `src/FlowForge.Installer/Commands/InstallCommand.cs`,
  `src/FlowForge.Installer/Modules/EngramModule.cs`, and
  `src/FlowForge.Installer/Models/InstallerConfig.cs`.
- Replaced the elapsed-time-based GitHub API timeout test handler with an
  immediate fake `TaskCanceledException` in
  `tests/FlowForge.Installer.Tests/GitHubReleasesClientTests.cs`.

## Rationale

The source-based installer tests read repository fixtures from the test output
tree, so the test project must copy them while preserving their repository
relative paths. `AppContext.BaseDirectory` is the stable test-output anchor;
the previous `..\..` paths depended on the runner's current working directory.
The production GitHub client disables `HttpClient.Timeout` and
uses an internal cancellation source; the old test therefore depended on a
five-second delay and could fail nondeterministically. The fake handler keeps
the test offline and directly verifies cancellation-to-`TimeoutException`
translation.

## Validation

`dotnet` was not available in the maintenance environment. The targeted test
command remains pending in CI:

```text
dotnet test tests/FlowForge.Installer.Tests -c Release --nologo
```
