# Quests, Settings, Library and Credits review

28 September 2026. Alter Echoes was deliberately excluded. This pass uses the approved dark gameplay styling, sharp corners, restrained dividers, shared checkmarks and consistent spacing.

## Changes

- **Quests:** clearer title/body/reward hierarchy, explicit Pin/Pinned actions, lighter progress bars, category counts and retained category expansion. Existing noticeboard categories, requirements, turn-in rules, auto-pin and companion resources remain.
- **Settings:** Preferences and Save files tabs; grouped audio, display and floating-text controls; fixed footer; labelled overwrite/delete locks. Closing Settings disarms these locks and dismisses its dialogs. Import/export and language dialogs use the shared buttons and backdrop.
- **Library:** consistent expandable chapter headers and readable body layout. Existing localized prose, inline icons and progression gates remain. Expansion and scrolling are retained across reopening.
- **Credits:** aligned names/roles with restrained separators. Removed BorgGrown and Invariel from the Toolkit definition and retained legacy scene rows; removed Invariel from the shared supporter text. Other names remain.
- Fixed the Settings VSync callback so it applies `QualitySettings.vSyncCount` as well as the frame cap. Corrected autosave copy to the existing one-minute recurring interval (the first scheduled save occurs after 30 seconds).

## Final screenshots

| Surface | Capture |
|---|---|
| Settings: Preferences | [1920 × 1080](menus-settings-final.png) |
| Settings: Save files | [1920 × 1080](menus-saves-final.png) |
| Library: Forge expanded | [1920 × 1080](menus-library-final.png) |
| Credits: first entries | [1920 × 1080](menus-credits-final.png) |
| Credits: remaining supporters | [Lower entries](menus-credits-bottom.png) |
| Quests: active entries | [1920 × 1080](menus-quests-final.png) |
| Quests at 4K | [3840 × 2160](menus-quests-4k.png) |
| Compact landscape | [Settings](menus-settings-landscape.png), [Quests](menus-quests-landscape.png), [Library](menus-library-landscape.png), [Credits](menus-credits-landscape.png) |

## Runtime QA

Ran two main capture/interaction passes and a targeted follow-up in the connected Unity 6000.6 Editor. Captured at 1280 × 720, 844 × 390, 1920 × 1080 and 3840 × 2160. Inspected final screenshots and compact landscape views, then corrected selected-tab contrast and vertical spacing. Slider visuals were reduced without reducing their hit regions.

Evidence: [main checks](checks-ui.txt), [follow-up checks](checks-followup.txt).

Verified actual callbacks for music, focus mute, VSync, floating-text settings, quest pin/unpin and quest turn-in. Verified save locks, relocking, invalid import rejection, generated read-only export content, Russian/English selection, chapter gates under fresh quest state, expansion persistence, full-content scrolling, image aspect preservation and five repeated window-switch cycles. Checked that all Preferences controls and the save note fit the desktop viewport.

The main log retains one failed Library-scroll assertion: the harness requested a bottom offset before the expanded content completed layout. The follow-up separates expansion, scrolling and verification across frames and passes. No production scrolling change was necessary. Credits were likewise captured at the top after the existing restored scroll position had settled.

### Isolation and cleanup

Used synthetic progression under `Library/ApprovedUIDisposableSaves`, with temporary application identity `EOV UI Validation / EOVApprovedUIValidation`. Network initialization, Steam and feedback submission were blocked for these captures. Resource quantities and quest states were synthetic fixtures, not a claim about reachable progression. Export clipboard contents were restored afterward.

Temporary guards were backed up and restored byte-for-byte, with SHA-256 verification of all seven source files. Font atlas and QualitySettings changes from testing were also restored. Restored normal application identity, left Play mode, and restored the Game view to 3840 × 2160. Unrelated workspace changes were preserved.

The final post-cleanup live `eval`/`run_script` checks timed out in the Editor bridge. The runtime QA above completed before cleanup; source restoration and the normal application identity were separately verified on disk. `recompile_status` reported completed with no errors, but a fresh live post-cleanup state query could not be confirmed. The last successful live check was out of Play mode, and the follow-up capture pass had already set the Game view to 4K. The Editor was not forcibly restarted.

### Limits

These are Editor interaction and visual checks, not a Development Player performance audit or physical mobile-device validation. Compact landscape captures check fit, not touch ergonomics on hardware. Actual file deletion, successful import and save-slot switching were not exercised; existing save operations remain wired through the original services. Language switching was tested, but this pass does not provide new translations for the newly authored English labels. Library prose was preserved rather than rewritten as an exhaustive mechanics audit.
