# Timber resource setup — technical evidence

The [readable report and actual visuals](../../planning/premium-wood-icons-2026-10-01/Echoes-premium-wood-icon-proposal.md) describe the approved change. This directory contains focused verification for review, without replacing the user's existing cleanup, migration or farming work.

- [Manifest](timber-manifest.json): approved source sprites, stable new resource GUIDs/IDs and glyph references. Log57 and Stick2 retain their old identities.
- [Existing-file diff](focused-existing.diff): comparison with the immediately preceding local bytes, rather than HEAD's older seed atlas. New resources are under `Assets/Resources/Resource Items`; the new test is `Assets/Tests/EditMode/TimberResourceTests.cs`.
- [Pixel/data verification](pixel-data-verification.json): all 236 prior glyphs unchanged; 16 appended; 756 original interior pixels and 316 exterior outline pixels verified; tree configuration and original resource fields retained.
- [Tests](test-report.json): 78 passed, zero failed/skipped. The original Unity XML is retained in the evidence directory.
- [Build](build-report.json): arm64/Mono Mac Player, zero errors/two existing warnings.
- [Render](player-render-report.json): Metal, all eight inventory/TMP sprites verified, inactive Oracle fixture, no render errors.
- [Restored compilation](restored-compilation.json): zero errors and exact existing compiler warnings after helper removal.
- [Preservation](source-preservation.json): 16,405-file baseline, intended changes only, Main and staging preserved, save/cloud bytes intact, 93 backup hashes intact. Two performance-test preference keys added; no other preference changes.
- [Guard restoration](guard-restoration.json): both temporary UGS changes restored byte-for-byte. ProjectSettings is also restored.

Original reports, test XML, raw Player logs, preserved first render failure, temporary helper/scene source and the disposable validation app remain at `/Users/matthewrushworth/Projects/Echoes Farming Implementation Evidence/2026-10-01/timber-resources`. The disposable app contains validation fixtures and is not a production build. Main was not included in it. The Editor is available again in clean Main/Edit mode.

No new save migration, holdings conversion, balance redesign, farming mechanics, commit, push or release was performed. Mobile/AOT, hero navigation and a full adventure run remain outside this narrow validation. Actual pooled resource animation is verified by the follow-up below. The known pre-existing shutdown warnings are retained, not suppressed.

## Adventure feedback follow-up

[Independent reference review](independent-reference-review.md) found no broken references and describes the base-only quest costs, absent species recipe demand, independent tiers and expanded completion cohort. [Actual completion/animation report](adventure-adventure-report.json) records 16 passing cases across both sizes of all four families, expected payouts/glyphs, one completion event, rise/fade and pool return. The [second build](adventure-build-report.json) has zero errors and the same two warnings.

The isolated fixture drives the actual public tree Tick/completion path after placing the timer at completed work. It preserves authored drop data and does not instantiate an active hero, Oracle save lifecycle, SaveManager or FarmService. Both earlier renderer-inspection fixture failures are retained with the original evidence; the final harness reads the task's exact serialized renderer. No gameplay fix or balance change was made.
