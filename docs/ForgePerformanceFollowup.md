# Forge performance follow-up — 26 September 2026

**The proposed 41 ms forge saving is not supported by the follow-up measurements.** The original 66.8 ms open / 25.6 ms closed Editor p95 was a real recorded difference, but it was neither a direct UI-method timing nor reproducible as a stable Player cost. It should not be used to justify a large forge rewrite or promise 41 ms of reclaimed frame time.

In the clean rendered Player confirmation, skipping *all* measured forge display refreshes reduced main-thread p95 by **0.23 and 0.37 ms**, while leaving stale UI. Both the unchanged and modified controls still had about **8.34 ms frame p95** at the 120 FPS cap. This is a diagnostic estimate of removable display work in this workload, not a shippable optimisation or a universal upper bound. A correct partial optimisation will recover only some of it. The initial exploratory pass showed reductions of roughly 0.23–0.57 ms; it is secondary evidence because the user reported an OS-style popup during that pass.

## Clean Player comparisons

Same high-end machine as the main audit: Ryzen 9950X / RTX4080SUPER, Windows x64 Mono Development, 3840x2160, VSync0, cap120. Two rounds in reverse order; each captures 18 seconds after 6 seconds total settling/warm-up. Real craft/equip/salvage services execute 181 operations per window, rotating all four slots, Vastium, Ivan480 and synthetic fuel. Tasting/autocrafting/buffs stopped. The original Editor was running at launch and ordinary PC use was allowed; it was found out of Play mode at the final check, with the exact transition time unrecorded; submillisecond timing differences have background-work uncertainty.

| Control | Frame median / p95 / p99, ms (range across two runs) | Main CPU p95, ms | GPU p95, ms | GC KB/frame |
|---|---|---:|---:|---:|
| Unchanged forge | 8.333–8.333 / 8.337–8.338 / 8.407–8.600 | 4.344–4.393 | 2.151–2.182 | 11.763–11.861 |
| Suppress all visual refresh — stale UI | 8.333–8.333 / 8.337–8.338 / 8.574–8.616 | 3.971–4.164 | 2.137–2.185 | 7.912–7.922 |
| Freeze layout groups/fitters after opening | 8.333–8.333 / 8.337–8.337 / 8.484–8.579 | 4.052–4.557 | 2.065–2.154 | 11.763–11.793 |
| Forge window closed | 8.333–8.333 / 8.337–8.338 / 8.550–8.566 | 3.824–3.951 | 2.023–2.165 | 11.036–11.044 |

Freezing all forge layout groups did **not** produce a consistent benefit: one repetition was about 0.16 ms slower, the other 0.29 ms faster in main-thread p95. Merely disabling ContentSizeFitters also failed to improve the exploratory pass; the exported hierarchy shows a fitter on the odds tooltip, not a demonstrated giant fitter chain across the entire forge. Those hypotheses are not supported strongly enough to prescribe a layout rewrite.

Hiding the entire window lowered main-thread p95 by approximately 0.44–0.52 ms in the confirmation. That removes visible rendering as well as layout and is not a valid optimisation of an open forge. It gives context for the small scale of the observed Player opportunity. No measurable p95 frame-time/FPS improvement was demonstrated at the 120 cap; inspect CPU work and allocations separately from capped frame intervals.

## Direct code costs and the best smaller target

Allocation-free stopwatch probes wrapped the existing methods in the disposable copy. Their totals include the probe overhead. LateUpdate includes its refresh children, so inclusive totals must not be added twice.

| Method | Direct mean ms per call, unchanged clean Player |
|---|---:|
| GearSlots | 0.0185–0.0185 |
| SelectedStats | 0.0348–0.0357 |
| AggregateStats | 0.1241–0.1275 |
| Resources | 0.0614–0.0626 |
| IvanXP | 0.0231–0.0234 |

Total deferred forge refresh cost was about **0.242–0.248 ms per equipment-change cycle**, averaged over the windows. This is not a per-frame p95. Craft/equip/salvage itself remained separate from these deferred UI calls.

The largest directly timed refresh was [UpdateAggregateStatsText](../Assets/Scripts/Gear/UI/ForgeWindowUI/ForgeWindowUI.cs#L1797), roughly half the direct refresh work. Its builder recreates equipment sections, lists and formatted strings. Suppressing gear/stat refreshes in the exploratory pass reduced mean allocations from about 11.8 to 8.2 KB/frame, approximately **0.42 MB/s** at 120 frames/s. Suppressing all visual refreshes reduced them to about 7.9 KB/frame, roughly one third less than baseline. These are deletion-control measurements, not gains from a correct implementation.

If improving the forge now, start with cached aggregate/per-slot text and reusable formatting buffers. Invalidate on equipment changes, load, and relevant quality/rate/level changes; refresh only the affected section, keep correct values on reopen, and preserve background simulation. Aim to recover part of the measured allocation stream and submillisecond work. Verify fresh equipped stats, quality percentages after Ivan level changes, all slots/rarities, resource changes and panel reopen. Do not expect tens of milliseconds.

Simply adding `if (text != newText)` before every assignment is insufficient: this project's TMP_Text setter already compares equal strings before marking layout/vertices dirty (`Library/PackageCache/com.unity.ugui@23caec89ae27/Runtime/TMP/TMP_Text.cs`, lines 125–143). The remaining opportunity includes avoiding construction of identical strings and unnecessary stat calculations. Likewise, broad event coalescing already exists in ForgeWindowUI.LateUpdate; another generic throttle is not a root-cause fix.

## What happened to the 67 ms Editor result?

It did not reproduce. The new original-style Editor windows were **27.63 and 26.16 ms frame p95**; Player-matched open windows were 30.59 and 26.42 ms; layout-frozen windows 24.26 and 24.63 ms; closed windows 21.23 and 22.40 ms. The first two windows used the current live-state copy; subsequent resets used the original private audit baseline. Use the second round for comparisons at the old progression state. Results support a smaller Editor UI penalty, not a reliable 41 ms saving.

The targeted Editor timeline contained 219 frames. ForgeWindowUI.LateUpdate totalled 19.75 ms inclusive, worst call 1.743 ms. CanvasUpdate.Layout totalled 94.31 ms inclusive, worst sample 5.332 ms. GC.Collect had two samples totalling 64.62 ms, worst 39.66 ms, and the trace includes substantial Editor work and profiler overhead. Its own frame p95 worsened to 115 ms when the Profiler was opened. These samples **do not explain the old 67 ms capture conclusively**; they show why an isolated Editor frame gap must not be treated as an equivalent Player saving. The earlier slow frames coincided one-for-one with 97 equipment operations, so the original association was real, but exact attribution remains unknown.

The clean Player had no built-in layout/TMP recorder available; requested absent counters were omitted. Method stopwatches plus controlled branch/layout suppression provide the evidence above. No full Deep Profile or shipping IL2CPP run was performed. Random background systems share Unity's RNG, and final gear was not identical in every window despite equal seeds and operation counts, so these are repeated matched workloads, not deterministic replay.

## Recommendation

For performance, **prioritise the cauldron next**, where the earlier rendered Player audit showed roughly 9.5 ms higher main-thread p95 with its window open. That is a larger validated target, although its whole penalty is not yet a promised fix gain. The forge's frame-rate-dependent crafting scheduler is still a separate correctness issue worth fixing. A large forge-layout redesign is not justified by this investigation; a focused allocation/caching improvement is plausible but comparatively small.

No production optimisation or gameplay rebalance was implemented. All new disposable source probes were restored, Editor callbacks/layout toggles removed, and the current live progression restored. The 59 real save hashes were unchanged throughout the guarded Editor experiment. The confirmation Player used private storage and disabled Steam/UGS paths, then exited. At the final read-only check the original Editor was out of Play mode, with audit keys cleared, save-root override null and Profiler off; it was left in that state. Restoration had completed before this later observation. Dormant Pipeline assembly metadata persists until normal reload; no active audit callbacks remain. Inactive source records, raw captures and limitations are in [the evidence folder](profiling/2026-09-26/forge-investigation/README.md), with [clean confirmation results](profiling/2026-09-26/forge-investigation/confirmation/ScenarioResults.md) and [source preservation](profiling/2026-09-26/forge-investigation/source-preservation.json).
