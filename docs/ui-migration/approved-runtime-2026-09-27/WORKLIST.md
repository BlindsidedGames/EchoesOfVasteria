# Dark UI review worklist

See [evidence and limitations](README.md) before treating a checked item as exhaustive gameplay coverage.

- [x] Apply approved dark HUD/Cauldron styling to main-project menus.
- [x] Preserve ordered navigation and existing gameplay action bindings.
- [x] Broad capture of ten routes at four resolutions.
- [x] Inspect secondary tabs, dialogs, fresh-data gates, inventory and long-content states.
- [x] Iterate on visible defects: slider tracks, row clipping, inventory columns, graph contrast, portrait scale, toast surface, leaderboard formatting and compact layouts.
- [x] Check sprite Images retain ScaleToFit.
- [x] Exercise actual Mix, tasting, crafting, equipment, buff and Prospector callbacks in isolated progression.
- [x] Check forge automation with window closed and 15 repeated window cycles.
- [x] Capture synthetic run details/reaping prompt and local leaderboard fixtures.
- [x] Confirm no enabled legacy screen-space Canvases.
- [x] Final compact-layout recapture and source compilation after restoration.
- [x] Restore all seven temporary source guards and original product identity; record hashes.

## Explicit release QA still required

- [ ] Physical mobile, touch keyboard and controller-only navigation.
- [ ] Every locale / enlarged text combination.
- [ ] Standalone Player visual validation for this styling pass.
- [ ] Actual cloud and save recovery/import/export flows.
- [ ] Natural death/reaping/restart/offline-reward cycles and every incidental gameplay overlay.
- [ ] Long-session retention/allocation profiling; short repeated-window checks do not establish memory stability.

All test progression remains isolated in Library. No real cloud account receives synthetic progress. Harnesses, private saves and temporary build outputs remain outside version control.
