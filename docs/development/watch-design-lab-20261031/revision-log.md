# Native capture revisions

## Initial batch — cd95f36b9

All ten executable SwiftUI layouts were captured on the owned Series 11 42mm and 46mm simulators. Original screenshots are 374×446 and 416×496 respectively. Contact sheets are labeled derivatives; the individual PNGs retain simulator pixels. These observations were made before the next code revision. This batch is intentionally retained as evidence of the initial defects.

| Design | Observed in native initial screenshots | Next revision |
|---|---|---|
| 01 Expedition dial | Saved portrait appears very small inside the arc; footer is close to lower edge. Health shows 89% from float flooring of 0.899999976. | Increase authored portrait footprint, round the health percent, bound the shared container and permit scrolling. |
| 02 Living trail | Route is readable; header/footer spacing stretches on the larger face, and very thin water strip appears as a soil seam. | Use stable top anchoring, give the authored running character more visual space, retain the real pixel bank as the main trail. |
| 03 Combat watch | Health and hero are clear, but screen is vertically centered below excess blank space. Health provenance lacks an explicit age next to the number. | Anchor content to the top and timestamp last-phone health locally. |
| 04 Harvest counter | Chest and resource total work well. Run averages button sits near lower edge; background variants could imply forecast totals even though this panel uses phone totals. | Stable bounded scrolling, preserve total/run-average toggle and label snapshot provenance. |
| 05 Since you left | Tall 2×2 tiles push title/refresh into system clock; current-distance row is entirely clipped on 42mm. | Compact tiles and use a bounded scroll viewport below a fixed header. Keep gains and distance semantically separate. |
| 06 Run goal | Goal arc is clear; session-only qualifier is below the visible edge. Goal metric uses old generic hero rather than the selected portrait. | Bound/scroll the content, show the selected character, retain explicit session semantics. |
| 07 Skillbook | Only the first row is visible and header has excess top space. Skill art/levels are crisp. | Remove the nested scrolling container, anchor header and reduce card height for a useful initial viewport. |
| 08 Run ribbon | Bars have genuine zero baseline and visible estimate marks. Selected distance lacks a visible unit; footer is clipped on 42mm. | Add metres/resources context, bounded scrolling and readable history qualification. |
| 09 Run comparison | All requested row fields exist, but rows force header under clock and comparison qualification is clipped. Estimated row mark does not qualify calculated delta strongly enough. | Bound scroll layout and explicitly qualify mixed estimated comparison. |
| 10 Map dossier | Rate/receipt are near lower clip; refresh collides with clock. Recent rate includes estimates without visible qualification. No-history maps cannot be selected. | Fixed header, compact content, explicit includes-estimates label, all five known maps selectable with honest no-records message. |

The revision also makes fixed custom type scale with the Watch dynamic type environment. Large type uses a taller header and scrolling rather than changing only an environment value with no visible scaling.

## Second and third native capture batches

`revision2` preserves an unsuccessful layout fix on both sizes: bounded scrolling stopped the system-clock collision, but watchOS navigation safe-area reservation pushed headers too low and left too little initial content visible. `revision3` uses an explicit 32pt top region under the system clock, checked in the actual Simulator bezel as well as raw screenshots. This corrected header position and improved the saved portrait, health timestamp, plain names, source labels and map control.

The third 42mm sheet still showed lower-content clipping in the trail, resource toggle, away distance, goal qualifier, chart toggle and comparison footer. Those defects were not accepted. The next revision reduces each content group to the available first viewport rather than merely allowing a hidden scroll area: shorter trail, compact paired counters/gains, segmented local resource goal, six 44pt skill cells, shorter chart, comparison delta above compact receipt cards and latest map distance beside its title. Accessibility text is allowed to scroll and actually scales through `ScaledMetric`.

Root independently inspected both initial sheets and called out the same fit defects, misleading 89% flooring, duplicate elapsed time, vague internal names, generic goal art, rate qualification and inconsistent numeric presentation. These findings informed the revisions; they were not treated as native acceptance. The comparison rate delta now derives from the same rounded rate values displayed in the two selected rows. Map latest and recorded best use the same formatting. No health forecasts, fake XP rings or lifetime map claims were added.

## Fourth native batch and final refinements

The normal fourth batch fits the primary information at both sizes. All six skill cards are visible on42mm, comparison arithmetic reads145−138=7, away gains and current distance fit together, and the map summary separates latest retained distance from recorded best. The independent review accepted this as a major improvement but still requested a more distinct goal and correct actor scaling/water visibility.

The linear goal alternative was therefore implemented for direct comparison with the retained fourth-batch segmented arc. It gives remaining quantity the largest type, then an exact stepped progress track, current/target together and one44pt Edit goal action. The native editor supports metric changes, Crown input and44pt plus/minus controls. Its values always use the last phone snapshot, including while away; trajectory gains never replace phone totals as the goal basis. The target belongs only to this lab process. A phone run reset would change the latest totals used for remaining quantity; there is no generatedAt-based run-identity inference or accumulation across runs.

Both scene actors now render their full64px authored canvases at80pt (1.25pt per pixel), with resting source feet at y41 aligned to a38pt scene baseline. The54pt scene also accommodates FlyIn frame0's y53 foot extent without cropping the authored arrival. Hero running and Carl arrival retain their actual independent source frame sequences. The bank is decorative route scenery, not a promise of terrain or future encounters. The water crop includes enough blue below the real grass/stone edge to remain visible.

Large-type captures showed real increases to title, value and label glyphs. They also exposed the map's latest distance wrapping212 as21/2. The accessibility layout now moves latest distance to a dedicated row; dense screens use native scrolling instead of shrinking away the requested text size. Compact values use uppercase K/M/B so a million suffix cannot be confused with metres. Large four-counter rows stack icons above numbers rather than forcing very small horizontal text.

Controlled dead and reaped snapshots display an explicit ending status and static actor without inventing a cumulative foreground reap count. Health uses only the phone snapshot for every value. Restart, terminal and expired gains remain frozen under the unchanged production decoder.

## Final correctness and native input pass

Final scope corrections supersede the earlier five-map selector: only the current map plus IDs present in retained history may be selected, because discovery is absent from the contract. Unknown future map art gets a neutral map symbol. Reaped fixtures now end at their effective cap with zero health and both ending flags; Reaped takes status precedence and the terminal ring is muted. Expired/phase2 away reports no longer say New run. Estimated dial time focus says away time. A pinned skill really moves first, and unpin restores source ordering. SF heart/map symbols resize to their declared bounds, fixing the crowded health percentage.

The linear goal's exact Canvas fill leaves a partial final segment at988M/1B and a partial second segment at16/100. The resource comparison unit moved below the numeric delta to keep large values on one readable line. Source clocks and data scopes remain explicit. Carl's arrival uses a finite four-frame task rather than an explicit timeline that did not reliably reach its last frame after cold mounting. Both actor feet are optically aligned to the grass lip while keeping their common source-pixel pitch.

Native standard DoubleTap actions successfully opened the goal editor, advanced its actual preset and scrolled large-text screens to reveal lower controls. Ordinary point/Crown input remained blocked by the desktop tool and was not reported as passed. Native recordings demonstrate foreground authored motion, the held arrival pose, background static health and actual Always On dimming. `PROOF.md` and `video-provenance.json` distinguish unmodified recordings from static playback holds. No more layout redesign was introduced after the coordinator accepted the final normal matrix.
