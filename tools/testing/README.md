# Windows editor tests

Close this Unity project after preserving any unsaved edits, then run from the repository:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/testing/run_windows.ps1 `
  -Filter Tests.PlayMode.FieldsProductionPlayModeTests.Guidance `
  -Output Artifacts/GuidanceTests
```

This runs Unity's batch **editor** test runner, not a game build. The supported
fixture owns a temporary save directory and an empty test scene. The script
uses full domain reloads during the run and restores the original editor settings
in `finally`. It refuses to compete with an open editor for this project.

Results are `results.xml`, `editor.log`, and `summary.json` under the output
folder. A missing report, zero tests, no passed tests, any test/setup failure,
nonzero Unity exit code, or timeout fails the command. A stale CLI `running`
status is not accepted as evidence of success.

The existing `run.py` provides the separate sandboxed macOS workflow.
