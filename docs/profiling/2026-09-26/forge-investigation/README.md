# Forge follow-up capture notes

These are temporary ablation experiments, not implemented optimisations. Original game source is unchanged. The report will distinguish direct method costs, whole-frame distributions and diagnostic controls that intentionally leave stale UI.

- `FirstPass.md`, `summary.*`, `ScenarioResults.md`: 21 windows, seven controls x three varied-order rounds. The user reported approving an OS-style allow-changes/network popup at an uncertain time. Treat this pass as exploratory; the final `confirmation/` pass occurs afterward.
- `confirmation/`: eight windows, unchanged / frozen layout / suppressed visual refresh / closed window in forward and reverse order. Visibly rendered at launch, no popup covering the observed game. All 18-second captures follow three seconds of panel settling and another three seconds of warmup. Native 3840x2160, VSync0, cap120, Mono Development, no attached Profiler/Deep Profile. Original Editor was running at launch but found out of Play mode at the final check (transition time unrecorded); ordinary PC use was allowed, so submillisecond differences have background-work uncertainty.
- `editor/`: original setup (native tasting active, saved Helmet selection, VSync1/uncapped request), Player-matched controls (tasting stopped, selected Weapon, VSync0/cap120), layout-frozen and closed controls, two rounds. First two windows used the newly captured current state; `fixture-switch.txt` marks subsequent resets to the original audit baseline, while retaining the current state separately for restoration. Round 2 is the valid comparison with the old baseline. Last timeline has explicit Profiler overhead.
- Per-scenario `-methods.json`: aggregate stopwatch timings and calls for GearSlots, SelectedStats, AggregateStats, Resources, IvanXP and LateUpdate. LateUpdate includes the first four, so do not sum those inclusive totals again. `operation_ms` covers real Craft/Equip/Salvage. FrameTiming CPU p95 is distinct from these direct method timings.
- `forge-layout.txt`: runtime paths and layout/text components. Mode4 disables LayoutGroups and ContentSizeFitters after layout settles; mode5 disables only fitters. These are diagnostic upper-opportunity controls and are not safe responsive-layout implementations.
- `instrumentation/`: inactive sources and exact disposable-only method patch. Modes1/2/3 early-return selected UI refreshes; they leave stale values and are not shippable fixes.

All Player windows use the private baseline at ignored `Library/PerformanceAudit/original-live.bin`, Ivan480/Eva732 before load progression, synthetic 100M resource fuel, Vastium core, four rotating equipment slots and existing craft/equip/salvage services. Automation and buffs are stopped except explicitly labelled original Editor setup tasting. Each Player window performs 181 operations in approximately 18 seconds (first request at window start); every operation consumes resources, equips its item and salvages the previous item. This is ten-per-second stress, not a claim that normal users replace gear that often. The confirmation records final equipped outcomes; Unity's global RNG is also used by background systems, so an initial seed alone does not guarantee identical rolls.

The tests use lightweight counters and stopwatch probes. Detailed built-in layout/TMP marker recorders were requested in Player but were not available there; no marker values are invented. The Editor raw timeline supplies layout attribution with profiler overhead. Draw/batch all-zero limitations from the main audit still apply.

The disposable build retained the original audit's unconditional Steam/UGS guards and a private save root. It was rebuilt into ForgeConfirmation then copied to the same ForgeBuild launch path to reuse the already-approved executable location. Build success logs are retained locally outside version control. The new disposable source edits have been restored; running binaries remain only as isolated, non-shipping test artifacts.

The Editor trial captured the user's current live state separately, redirected save storage, disabled cloud reporting, and suppressed autosave before loading fixtures. `editor/cleanup-success.txt` verifies 59 real save hashes stayed unchanged throughout that guarded experiment and current progression was restored. Normal real play/autosaving can resume after restoration. The Player never uses the real save root. No live cloud callback experiment was performed.

Recalculate each folder using the parent audit analyser:

```powershell
python docs/profiling/2026-09-26/analyse.py docs/profiling/2026-09-26/forge-investigation
python docs/profiling/2026-09-26/analyse.py docs/profiling/2026-09-26/forge-investigation/confirmation
python docs/profiling/2026-09-26/analyse.py docs/profiling/2026-09-26/forge-investigation/editor
```

Per-frame CSVs are stored losslessly in frame-captures.zip in each results directory. Every archived entry was SHA-256 checked against the raw file before removing the loose copy. The analyser reads archives directly.
