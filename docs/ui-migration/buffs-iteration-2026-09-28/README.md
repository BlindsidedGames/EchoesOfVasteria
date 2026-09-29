# Buffs window and Prospector removal

The Buffs screen now has a compact five-slot loadout and a two-column list that falls back to one column below 560 reference units. Buff icons use the shared native PPU and centered masks. Equipped entries use purple borders; effects and timing stay concise. The list scrolls within the window, and assignments remain locked during runs. Autocast uses the existing echo marker.

Prospector's recipe, icons, both picker implementations and picker prefabs are removed. Its three casting quests are removed, and Windfall, Collector's Instinct and Echo Resonance use the existing preceding milestone (stable quest ID `Slipstream20`, 5,000 resource requirement). No substitute casting chain or save-schema change was introduced. Stable serialized effect IDs and historical migration data remain readable. Loading a former Prospector/Slipstream assignment clears that slot and its autocast flag. Dormant card and quest data are not erased from saves.

## Validation

- 51 assertions passed in `GatheringBuffValidation.Main()`, exercising isolated in-memory saves and objects: full historical migration, cleared retired slots, surviving unlock references, no retired pool entries, Collector weighting, Windfall rewards and Echo Resonance behavior.
- Runtime UI checks use a disposable profile in `Library/ApprovedUIDisposableSaves`, synthetic completed quests and five unlocked slots. They do not represent naturally reached progression.
- Steam, UGS initialization, leaderboards, unique names and feedback uploads were temporarily guarded; the application identity was isolated. Those temporary edits are restored byte-for-byte after testing.
- Screenshots cover desktop and 844×390 landscape; `checks-ui.txt` records assignment, cancellation, autocast, run-lock and scrolling checks. This is an iteration of the Buffs window, not a whole-game visual approval.

See [desktop](buffs-final.png), [landscape](buffs-landscape.png) and [remaining buffs](buffs-scroll-bottom.png).

Final cleanup confirmed normal application identity, restored guard files, clean compilation, Play mode stopped and the Game view set to 3840×2160.

## Follow-up: restore previous equipped boxes

At user request, the equipped strip now reuses the previous 29×30 ability boxes, 21-unit proportional icons and original autocast marker positioning. The added heading, slot numbers and larger box layout are removed. The new two-column ability list remains. The screenshots above precede this small follow-up.
