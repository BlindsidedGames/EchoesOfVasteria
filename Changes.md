# Changes

- Removed Claude Code configuration and AI council artifacts, blocked their return, and aligned repository documentation with Unity 6000.5.1f1.
- Updated prefab pool identity tracking to use Unity 6.5 `EntityId` values instead of obsolete instance IDs.
- Removed stale extracted Odin Addressables and Localization modules so Odin 4.0.2.1 can reinstall their Unity 6.5-compatible sources.
- Fixed the Better Rule Tiles 1.5.0 upgrade by correcting the editor assembly placement of `GUIBuilder` and `GUIGrid` and removing superseded 1.4.6 scripts.
- Removed the leftover Unity MCP `PythonTools` configuration asset after uninstalling the package.
- Added `Todo/CodeQualityReview-2026-05-23.md` with prioritized maintainability and performance cleanup tasks from a Unity project code review.
