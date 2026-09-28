# UI Toolkit migration

Current visual direction and reviewed runtime evidence: [approved dark UI integration](ui-migration/approved-runtime-2026-09-27/README.md). This supersedes the earlier pixel-parity styling; consult its coverage limits before interpreting older completion statements.

Status: the native cutover is installed in the main project. All reachable town windows, navigation, run HUD/buffs, summaries/death controls, world indicators, notifications and loading/recovery presentation now have UI Toolkit implementations. The secondary Editor has been closed. Main-project integration checks pass; Development Player validation is in progress. The historical sections below record the staged migration and should not be read as the current installation status.

## Scope

Replace the game-facing uGUI interfaces with native UI Toolkit views, preserving appearance, navigation, input, progression gates, data updates, and desktop/mobile behaviour. Use shared UXML/USS and a consistent localization layer. Current direction: prioritize a reusable design system and simpler hierarchies over exact pixel matching. Keep sprite proportions intact; authored nine-sliced panels may resize normally. Targeted performance improvements are authorized during migration, with measurements separated from estimates and gameplay balance preserved.

The recent Prospector picker is excluded from the visual reference baseline. Its targeting functionality is implemented by the native picker, using the shared theme and proportional icons.

## Preservation and capture isolation

- The existing dirty workspace is the source; do not reset it to HEAD. The starting status is retained privately in `Library/UITK-workspace-before.txt`.

- Early baseline captures used a separate copy at `%LOCALAPPDATA%/Temp/EOV-UITK-20260927` with a distinct company/product identity and disposable save root.

- That copy disabled Steam initialization, UGS initialization/submission, profile cloud reads/writes, and feedback submission. Verified at runtime: Steam false, UGS Uninitialized, save root inside the disposable copy.

- Fresh state, copied developed progression, and synthetic all-quest-unlocked presentation fixtures are labelled separately in the capture manifest. The developed source is the private binary snapshot from the earlier profiling session; it is not committed.

- Main-project cutover validation subsequently uses a temporary distinct product identity, Library-only save root and the same offline guards. Original guarded source bytes and identity are restored after testing.
- Online service results cannot be validated using these offline fixtures.

## Baseline evidence

See [baseline folder](ui-migration/baseline-2026-09-27/). `scene-ui-inventory.json` records 3,292 UI transforms from the initial scene; `buttons.json` records 363 buttons and persistent actions. `prefabs.json` inventories existing reusable UI assets. Runtime-generated controls are additional to these Edit-mode counts.

`capture-manifest.json` records the setup, native resolution, time scale, and outcome for each runtime screenshot. Adjacent `-text.json` files record visible text and hierarchy paths. Screenshots are native runtime captures, not mockups.

Open the [capture gallery](ui-migration/baseline-2026-09-27/index.html) and [coverage review](ui-migration/baseline-2026-09-27/coverage.csv). Capture helpers and reproduction/isolation notes are in [tools](ui-migration/tools/README.md). The first standalone inventory attempts are invalid references; corrected captures open it through Forge. `90-run-hud` is a loading transition; use `91-run-hud-settled` for the HUD. The extra startup image is a 1920x1080 downsample, whereas the main desktop batch is native 3840x2160.

The [supplement gallery](ui-migration/baseline-supplement-2026-09-27/index.html) covers six skill tabs, equipment slots, crafting result, intro and run states. Its graph-hover attempt failed and its three recovery screenshots did not show the recovery panel; those are explicitly invalid in its coverage CSV. The [correction gallery](ui-migration/baseline-final-2026-09-27/index.html) captures the populated graph and actual hover details. The first corrected recovery captures forced the dialog over Main and remained limited. The additional `05-recovery-loading-boundary` capture invokes the actual Loading transition and confirms that the original recovery panel runs off-screen there too. This is an existing layout defect, not a baseline setup artifact. No recovery action was clicked.

## Historical implementation stages

- `Assets/UI/Toolkit/Book.uxml` and `Theme.uss` provide the native scroll/foldout layout.

- `ToolkitTheme` holds references to existing sprites and font; it does not duplicate/repaint the art.

- `ToolkitPanel` uses the original 16 reference sprite pixels per unit and 432-unit vertical reference. The default Toolkit value of 100 produced oversized borders in the first prototype and was rejected.

- `ToolkitTextBinding` owns and disposes localization subscriptions. Imported Library body text keeps existing table/key references, and text style preserves small caps/bold.

- `ToolkitBookMigration` is an Editor-only authoring import from the existing Library. The resulting view has no runtime dependency on TMP, Canvas or the old hierarchy. Inline sprite indices are preserved when importing the text sprite atlas.

- Shared styling, inline sprites, font effects, scrolling, locale fallback and keyboard/pointer behaviour have initial checks; exhaustive typography/device parity remains open.

- The shared `ToolkitBookScreen` owns the native document, safe-area sizing and view lifetime. Main now assigns the native Library/Credits hosts to `TownWindowManager`. It preserves expanded chapters and scroll position across close/reopen, matching the original behaviour.

## Historical staged validation

[Runtime checks](ui-migration/validation-2026-09-27/library-checks.txt) cover all 13 native Library chapter buttons, exact inline sprite index/name mapping, native navigation-submit expansion, asynchronous localized content, scroll bounds and 25 view construct/dispose cycles. [Navigation checks](ui-migration/validation-2026-09-27/navigation-checks.txt) cover town button routing, toggle-close, switching to another window, 25 open/close cycles, detached content on close and desktop/wide safe-area calculations. These checks do not establish touch/controller device coverage or absence of every subscription leak.

The latest [collapsed Library capture](ui-migration/validation-2026-09-27/library-collapsed.png) was compared against the original at 3840x2160. The [pixel comparison](ui-migration/validation-2026-09-27/library-image-comparison.json) is diagnostic, not a parity score; typography rasterization, subpixel rounding and some geometry still differ. Existing TMP underlay styling has been translated to Toolkit text shadow/outline styling. The original content inset and foldout icon anchor were also carried over after comparison.

[Credits checks](ui-migration/validation-2026-09-27/credits-checks.txt) pass for nine entries, all title/body labels, non-interactive rows, the original 743-unit viewport width, scrolling and repeated disposal. Credits uses its original eight-unit scrollbar and one-unit separation; Library uses its own six-unit scrollbar. The shared theme preserves this existing difference.

The native meeting prefab and view pass [12 view/lifecycle checks](ui-migration/validation-2026-09-27/meeting-checks.txt) and [task integration checks for all five NPC assets](ui-migration/validation-2026-09-27/meeting-task-checks.txt). `TalkToNpcTask` initializes the native presenter; its existing completion callback still owns quest updates and XP. All five task assets now reference the native prefab. Shared Meet/Next/Close strings are editable localized references in `Meeting.asset`; existing dialogue prose still comes from NPC task assets and needs a localization authoring pass. Tests call task arrival directly in an isolated fixture, not natural hero navigation.

[Installed-scene checks](ui-migration/validation-2026-09-27/installed-books-checks.txt) pass for actual serialized Library/Credits routes, expansion and scroll restoration, window switching and 25 repeated cycles. Main installation adds two native hosts and their references. Unity also regenerated unrelated animated tile speeds while saving; those changes were removed using the immediately preceding scene snapshot. NPC prefab serialization was similarly narrowed to the single `meetingPrefab` reference per asset, preserving pre-existing contents.

The opening notice is connected in Main and passes [six runtime checks](ui-migration/validation-2026-09-27/intro-checks.txt): initial visibility, rejecting early close, countdown with game time paused, persisting the existing completion key, disposal and remaining hidden for returning players. Its original controller was replaced and the old Welcome hierarchy remains inactive. Native text keeps the original two-percent character spacing, converted using the TMP implementation's `fontSize * 0.01` em scale. These checks ran only against disposable preferences.

The native recovery dialog passes [13 isolated checks](ui-migration/validation-2026-09-27/recovery-checks.txt), including the actual Loading transition, status-specific labels, visible actions, busy-state input rejection, literal diagnostic text and callback/disposal behaviour. Both Loading and the fallback Oracle in Main reference its native prefab. Save/retry/fresh-recovery algorithms are unchanged; the tests invoke fake callbacks on a separate dialog and never execute actual recovery actions. Unlike the original off-screen panel, the native version fits inside the safe area. Loading serialization was narrowed to the new prefab reference using an immediate pre-edit backup.

A progression review found three Library chapter gates missing from the first native implementation. The authoring importer now copies the original quest rules into data-driven native visibility rules. [Eight checks](ui-migration/validation-2026-09-27/library-gates-checks.txt) compare fresh and staged unlocks against the original controllers, preserve expansion state and verify unlocking while the native Library remains open.

Native navigation passes [45 runtime checks](ui-migration/validation-2026-09-27/native-navigation-checks.txt): fresh/staged quest gates, all window routes, dropdown exclusivity, contextual Auto-Pin, automation attention indicators, repeated enable cycles, and actual starts/returns for all five authored maps. Map selection calls a shared GameManager route with the original config/music/scaling metadata. The native prefab has no Canvas graphics; it routes to remaining legacy window bodies until those are ported. The run-progress presentation was separated from rendering and [360 states match the exact previous MapUI implementation](ui-migration/validation-2026-09-27/run-progress-checks.txt). These cases cover distance and kill progression, thresholds, mode transitions, zero maximum and clamping; they do not cover every buff/demo combination.

[Ten geometry checks](ui-migration/validation-2026-09-27/navigation-layout-checks.txt) pass for original window positioning, the eight toolbar button bounds and the echo-total frame. The first geometry comparison used world coordinates incorrectly; the corrected test projects through the Canvas camera before comparing Toolkit coordinates. Visual corrections preserve the gear icon size, sliced close icon, transparent close hit area, right-aligned Discord control and nested echo-total frame. A temporary 22-unit Canvas layout spacer preserves existing window offsets until the surrounding window container is migrated. [16 installed-scene checks](ui-migration/validation-2026-09-27/installed-navigation-checks.txt) pass at 1280x720, 2340x1080 and 3840x2160, including Options/Library/Hub routing and safe-area bounds. These remain desktop viewport checks, not mobile touch or a Player build.

## Migration acceptance

For each screen, compare against its recorded baseline, then verify navigation, disabled/locked states, tooltips, scrolling, data updates, and localization. Validate long text, font fallback, safe areas and mobile input. Do not call a static screenshot export or a hidden-uGUI rendering bridge a completed UI Toolkit migration.

The staged checks below were performed before cutover. The saved main scenes and gameplay prefabs now disable their legacy presentation components. Retained authoring hierarchies are references, not a runtime fallback.

## Screen tracking

| Interface | Existing reference | Native implementation | Remaining validation/work |

| --- | --- | --- | --- |

| Town navigation, map/Hub/Townsfolk menus | Fresh/developed town and three dropdown captures | Native toolbar and menus connected in Main | Dad-o-cado native; physical device and translated-text checks remain |

| Library | Collapsed plus 13 chapter captures | Native book view connected in Main | Per-chapter wrapping, locale variants and device input |

| Credits | Developed credits capture | Native credits rows connected in Main | Remaining typography differences and device input |

| Options/audio/display/save slots | Fresh/developed and viewport captures | Native screen connected in Main | Slot load/delete actions, actual window-mode changes and platform differences |

| Import/export/language | Presentation captures | Native dialogs connected through Options | Valid import/load outcomes, long input/device keyboard, remaining error states and locale layout |

| Statistics | General, rank, graphs, enemies/tasks/items | Native tabs, graphs, filters and rows connected in Main | Live online responses, rare service errors and physical device input |

| Quests and pinned goals | Completed history plus fresh entries | Native quest board and pinned HUD connected in Main | Additional requirement/reward types, locale/device input and installed pinned-HUD viewport checks |

| Buffs and slots | Fresh/developed/unlocked list and scrolling | Native assignment/picker and five run slots connected in Main | Run-slot duration/hover/input, translated text fitting and device input |

| Skills/milestones | Main view; supplement adds each skill | Native screen connected in Main | Set-bonus fixtures, typography and device input |

| Forge and equipment | Main, inventory, odds, slot/result supplement | Native screen connected; ForgeSession owns background crafting independently | Physical touch, broader combinations of gear/progression and service-driven load failures |

| Cauldron/collections | Main, scroll, weights and viewports | Native screen connected in Main | Physical touch/controller, localization, font rasterization and broader automation overlap |

| Alter Echoes/resources | Main and scroll captures | Native Alter Echoes and shared inventory connected, with native companion bounds | Collection, filters, pending resources, upgrades and icon cropping |

| Run HUD/breakdown | Settled HUD, skills, death, return and supplement | Native HUD, five buff slots, death/retreat/restart, breakdown and summary connected | Physical hold/keyboard/controller combinations and rare run-end overlap |

| NPC meetings | Five real prefab portraits/dialogue presentations | Native prefab connected to five task assets | Remaining portrait/long-text visual checks, natural encounters, device input and localized dialogue |

| Intro/notifications/quit | Startup, synthetic dialogs and supplement | Intro, notifications and desktop quit connected in Main | Remaining visual/device checks, notification queue and platform controls |

| Developer console | Existing backquote/mobile gesture and command API | Native log/input/history/completion; existing command processor and authentication | Physical mobile keyboard and third-party interactive command extensions |
| Loading/save recovery | Actual Loading recovery boundary plus limited earlier variants | Native loading and recovery connected in Loading/Main | Actual retry/fresh/folder operations intentionally unexecuted; device layout and loading presentation |

Legacy-looking roots, including Carving and obsolete inventory/stat tabs, are not exposed by the current navigation catalog. Their renderers and input components are disabled along with the retired UI; they are not silently reintroduced as new routes.

## Coverage still to resolve

Contact sheets have been reviewed for the captured cases; pixel-level review is performed during each screen's migration. Fresh quests, run HUD, forced death, return summary and NPC presentation are now captured. Remaining gaps include additional rare error states, natural NPC encounters, additional gear-result/equipment states, actual mobile platform controls/touch/safe areas and localization variants. The 1280x720 and 2340x1080 batches are desktop viewport checks. The completed capture count does not establish complete coverage by itself.

### Desktop quit controls

The native controls preserve the original 16-unit icon and two 47-unit confirmation buttons with a one-unit gap. The original surrounding Image components are disabled, so no extra frame is drawn. Main uses the native host and retains the old controls inactive. Both presentations share GameQuitRequest and the unchanged Oracle.PrepareForQuit checkpoint entry point. Mobile hides the quit control as before.

Validation: validation-2026-09-27/quit-checks.txt records 12 passing checks, including failed-checkpoint retry, duplicate-exit suppression, cancel, panel visibility and 25 enable cycles. Checkpoint and exit behavior used fake callbacks; no actual save or application exit was invoked. Screenshots cover closed and confirmation states at 3840x2160. Actual mobile visibility and platform exit remain untested.

### Options and input dialogs

Native Options now uses the original sprites and measured 508-unit content width, slider ranges, save-slot safety controls and footer routes. SaveSlotActions extracts the existing save/delete/checkpoint calls from SettingsPanelUI; SaveSlotPresentation supplies view-independent metadata. The existing uGUI screen remains inactive as a reference. Import/export dialogs use native multiline TextFields, and the locale chooser retains English/Russian and the original Russian warning.

Evidence: options-checks.txt has 33 passing runtime checks; options-extras-checks.txt adds seven width-preview/locale checks. Slot titles, playtime and version/date labels matched the existing renderer for all three fixture slots. The tested controls in options-geometry.txt differ by less than 0.1 reference units after fixing button margins. Export wrote only the isolated profile and confirmed a durable checkpoint; invalid import was rejected. Clipboard contents and test preferences were restored. Valid import, slot switching/deletion, actual desktop window-mode changes and device keyboard/touch remain untested.

Existing behavior recorded separately: toggling VSync on sets the unlimited frame cap but does not immediately set QualitySettings.vSyncCount; that quirk is preserved during the UI migration. The player/enemy floating-text field names and visible labels also map counterintuitively; the existing mappings and 10/2/2 duration multipliers are preserved.

Installed-scene validation adds 15 passing checks in installed-options-checks.txt: serialized routing, safe-area placement, native input dialogs and global close at 1280x720, 2340x1080 and 3840x2160. These remain desktop Editor viewport checks.

### Buff assignment and Prospector

Native Buffs and its independent picker are connected in Main. Existing BuffManager assignment, autocast and target APIs remain authoritative. Recipe order, quest gates and concise description strings are unchanged. The picker retains task unlocking, alphabetical skill filters, cancel/confirm semantics, native-proportion cropped icons and the previous device-density sizing formula. The original run-slot HUD/keyboard handling remains active during the staged migration; keyboard buff casting is suppressed while either picker is open.

Evidence: buffs-checks.txt records 38 passing runtime checks using developed and synthetic fresh quest/slot states, actual assignment/autocast/target changes, simulated run-state callbacks, and 25 repeated opens. buffs-geometry.txt records 32 matching panel/slot/row/Assign bounds (maximum error below 0.001 reference units). The first comparison accidentally compared a stale fresh legacy list with the restored developed native list; rebuilding the legacy reference corrected that fixture mismatch. installed-buffs-checks.txt adds 18 passing routing, safe-area, picker-scale and close checks at three desktop sizes. Prospector is excluded from the baseline capture inventory, as requested; its native validation images are migration checks.

Remaining: actual touch/controller interactions, translated/dynamic text fitting and run HUD integration. Text rasterization is similar but is not pixel-identical to TMP; the native instruction line break still differs. No device or Player parity claim is made.

### Skills and milestones

Native Skills is connected in Main, with the original six selectors, XP display, totals, active-slot list, milestone toggles and resource unlock entries. SkillTotalsPresentation extracts the existing formatting/calculation order; the legacy renderer delegates to it as well. Native controls call SkillController and retain its capacity checks. The XP sprite is clipped at full width to match SlicedFilledImage instead of shrinking its sliced end cap.

skills-checks.txt records 54 passing checks across six developed skill views and a synthetic level-one fixture, including exact displayed values/descriptions, row counts, toggle visibility, activation/deactivation and 25 opens. skills-events-checks.txt records 12 passing checks for controlled XP/level-up events, highlight clearing, full-slot rejection and exactly one subscription per native event handler after 25 cycles. Temporary progression changes were restored inside the offline disposable profile. Current developed milestone assets expose no active sets; populated primary/secondary set panels remain untested. Typography and full long-text/device coverage remain in progress.

Installed Skills validation: installed-skills-checks.txt adds 18 passing checks for saved routing, safe bounds, Mining selection and global close at 1280x720, 2340x1080 and 3840x2160. These are Editor viewports; mobile/platform input remains untested.

### Quest board

The native board is connected in Main. QuestNoticeboardEntry exposes the existing Ready/Pinned/Active/Completed ordering, and QuestRequirementPresentation shares the old requirement formatting. Native pin and turn-in controls call QuestManager. Tutorial routing and the resource-inventory companion are preserved. A temporary invisible Canvas layout spacer reserves the quest area while that companion and other windows are migrated; it draws no graphics. This spacer is transitional, not the final all-Toolkit layout architecture.

quests-checks.txt records 33 passing checks, including displayed strings, readiness, pin/unpin, actual opening-quest completion, duplicate rejection, history expansion and 25 subscription/open cycles. Initial comparison failures came from counting the inactive scene template and comparing collapsed legacy history rows before their localization components had enabled; the corrected comparison excludes the template and opens history on both sides. quests-resource-checks.txt adds seven passing checks using synthetic completed history with the real “A Sturdier Frame” quest reopened. Its Log cost was deducted exactly. No matching resource-cost quest with a resource reward was found by the fixture selector, so resource rewards are not separately claimed as tested. Fixture progression was restored inside the offline disposable copy.

Remaining: independent checks for every requirement type, XP/resource reward combinations, physical touch/controller input, translated layout, pinned run HUD, and final removal of the legacy layout dependencies. Native font rendering remains similar rather than pixel-identical.

Installed quest validation adds 18 passing checks at three Editor viewport sizes. The 178-unit inventory reservation differs by at most 0.267 reference units (less than one physical pixel) due to Toolkit pixel-grid rounding; the results record actual and expected widths.

### Shared resource inventory

Native ResourceInventory is connected in Main for Quests, standalone inventory, Forge Info/Inventory switching and the existing Alter Echoes highlight route. All 69 resources keep their existing order, six-column 26-by-32 layout, tier-border/background rules and formatted amounts. The original inventory icons use fixed 18-by-17 rectangles with preserveAspect disabled; that existing behavior is retained. Selection uses the original three seconds of scaled time and leaves the selected title after the highlight expires.

resources-checks.txt records 25 passing checks; installed-resources-checks.txt adds 21 checks across three Editor viewports, including opening a closed panel through an external highlight request and scrolling after its first layout. The first fixture incorrectly assumed ResourceManager.Add was one-for-one despite tier bonuses; the corrected fixture spends the actual delta, disables tier rolls and restores resource accounting totals. The native view uses ResourceManager events and removes its subscription while closed.

The shared inventory still uses an invisible Canvas layout spacer while Forge/Alter Echoes and the town layout are being replaced. It renders entirely through UI Toolkit, but removal of this transitional positioning dependency remains part of the full migration. Fresh locked-resource and long localized-name variants, touch/controller input and Player builds remain unverified.

### Pinned run goals

Pinned goals are connected in Main through a native HUD document. PinnedQuestPresentation mechanically extracts the original readiness/text calculations, including its existing per-requirement rules; both renderers use it. The HUD follows GameManager map-UI visibility, uses the saved collapse preference and refreshes from quest/progression/localization events. The old pinned renderer is suppressed when its native reference is configured.

pinned-checks.txt and pinned-five-checks.txt each record nine passing checks during actual isolated Farmlands runs and returns. The five-goal case pins authored quests synthetically and is not a naturally reached quest state. Display text, readiness indicators, collapse/expand preference and town hiding matched. The first reference capture caught loading; the replacement waits for the run loading overlay to close. CRLF/final-newline normalization and imported font metrics corrected the extra native blank line. Geometry reports show width differences under 0.4 reference units and height differences under 0.1 for these two fixtures. Toolkit preferred-size measurement omits letter spacing, so the HUD adds that width explicitly.

Remaining: installed three-viewport checks, physical touch/controller, translated and resource-icon-rich goal variants, subscription-cycle checks, and the original soft MPImage backdrop edge (the native version currently uses a rounded flat fill). The map-UI visibility property still follows the legacy map root during staged migration.

Installed pinned HUD validation adds 16 passing checks at the three Editor viewport sizes, including one saved native host, legacy suppression, safe-area anchoring, 25 enable cycles with one subscription per quest event, and hiding after an actual return to town. Physical device input and the remaining visual variants are still untested.

### Cauldron and collections

The native Cauldron is connected in Main. It uses the imported original panel/tier/bar sprites and Eva/cauldron animation frames. Mixing calls the existing manager, including full-stack consumption, ordered Mix All pairing and automatic tasting. The currently authored hidden Taste/Stop controls remain hidden. Town attention still uses the existing global manager callback. The collection bonus retains the old load/tasting-stop triggers; this migration does not rebalance it.

[38 runtime checks](ui-migration/validation-2026-09-27/cauldron-checks.txt) compare all 30 food slots and 81 developed collection entries, exact totals and tooltip text, selection replacement, actual mixing, actual tasting with the window open/closed, attention, and 25 open/close subscription cycles. [35 tier checks](ui-migration/validation-2026-09-27/cauldron-tier-checks.txt) compare counts, sprites and fill immediately below/at every resource and buff threshold, and an Eva-level-one/no-Infinity fixture. That early fixture retains developed unlocks and inventories: it is a synthetic presentation case, not naturally reached early progression. Temporary state is restored inside the offline disposable project.

The initial visual comparison found and corrected the pot's aspect ratio, collection bar sprite/color, count text color, disabled Mix tint, pie outline/hover indicator and scrollbar width. The odds text comparison found a bullet/asterisk difference between the live scene's fallback and its unused optional presenter; the native view now matches the live fallback. Exact text comparisons that use shared extracted helpers do not independently verify their algorithms. Font rasterization remains visibly different from TMP; complete localization/device parity is not claimed.

Installed Cauldron validation adds [24 passing checks](ui-migration/validation-2026-09-27/installed-cauldron-checks.txt) at 1280x720, 2340x1080 and 3840x2160. These exercise saved routing, bounds, collection scrolling/clipping and global close in the isolated Editor, not a mobile device or Development Player.

### Alter Echoes

The native panel is connected in Main with its original sprites, 61 developed generator rows and shared resource inventory. Individual collection calls the generator; Collect All retains the existing resource batch and save notification. Generation remains manager-owned while the panel is closed. The temporary Canvas layout spacer remains until the surrounding layout migration is complete.

[25 runtime checks](ui-migration/validation-2026-09-27/alterecho-checks.txt) compare row ordering and values, individual/bulk collection including resource tier bonuses, inventory highlighting, open/closed generation, generator replacement after loading, and subscriptions after 25 open/close cycles. [24 installed checks](ui-migration/validation-2026-09-27/installed-alterecho-checks.txt) cover the three desktop viewports, saved routing, inventory reservation, scrolling/clipping and global close. Their inherited “Collections” check labels refer to the generator list. Tests used the offline disposable project. Initial text comparison failures were not reproduced in the subsequent complete comparison; no cause has been established. Physical input, translated layouts, Player behavior and exact font rasterization remain unverified.

The original Editor stopped servicing main-thread CLI requests during this stage. Authoring and runtime checks continued in the disposable Editor. Only the verified Alter Echoes scene blocks and newly generated native assets were transferred back, with the original scene snapshot checked before applying them. Unrelated tile/animation serialization changes were excluded. The original Editor's reload/compile of that transfer remains pending; the disposable project compiled it successfully.

### Shared design-system consolidation

Following the authoring review, [UIDesignSystem.md](UIDesignSystem.md) defines the separation between theme tokens, reusable components, screen composition and gameplay bindings. Tokens.uss centralizes common USS typography and interaction colors. Components.uss and ToolkitControls supply shared button/text/icon/recessed-scroll primitives; the former Buffs button factory delegates to the shared control. This is partial adoption: inline typography, repeated imported sprite references and screen-specific dimensions still require consolidation. Neither pixel-perfect parity nor complete global restyling is claimed.

### Statistics (connected in Main)

All six legacy tabs were recaptured through their actual button callbacks in `stats-tab-layouts.txt` and the tab captures. Native General/Graphs passed 53 data/bar checks; Items passed 36 checks across all five sorts and 69 resources. Tasks and Enemies now share `ToolkitStatRow`, with aspect-preserving icons and content-driven heights. They passed 128 checks covering all task sorts, all seven enemy sorts at zero/half/maximum preview distance, every displayed field, reveal progress and task toggling. The first two toggle-test failures were caused by a synthetic submit event without its target; the corrected event exercises the button callback and passes. The first results remain alongside the successful run.

Rank uses a read-only adapter to the existing leaderboard client. Its 34 offline fixture checks cover Top/around-player ordering, missing scores, boundary ranks, both score formats, metadata, failure/retry, stale requests and disposal. Native names render as literal text with a separately styled discriminator, because visual review showed that copying TMP's HTML-style escaping displayed entities literally in TextCore. No synthetic leaderboard scores were submitted, and no live leaderboard/network parity is claimed.

Statistics is connected in Main. Its installed route passed 101 checks across 1280x720, 2340x1080 and 3840x2160: all six tabs, safe-area bounds, legacy visibility, aspect-fitting art, offline failure presentation and 25 open/close cycles with subscription removal. These are desktop Editor viewport checks, not physical mobile validation. As with Alter Echoes, authoring used the disposable Editor and transferred only two existing route/parent blocks and three new prefab blocks; unrelated tile animation and layout serialization was excluded. Original Editor reload/compile is still unverified.

The shared slider now serves Options and enemy preview distance. Twelve isolated checks cover volume callbacks, fill fractions, nonzero minimums, silent binding and Options component adoption. Its first broad query incorrectly included ScrollView's internal scrollbar sliders; the final check targets the seven authored Options controls.

### Updated visual and performance direction

The user approved standardizing close-enough layouts and simplifying hierarchy rather than reproducing every legacy pixel. Sprite Image controls now use aspect-preserving fitting, including formerly stretched inventory and Cauldron art. Sliced backgrounds retain their authored borders. ToolkitWindowLayout centralizes common centered/full-width geometry and caches applied bounds; identical bounds do not rewrite styles. These changes supersede earlier notes that intentionally retained stretched legacy icon wells.

Alter Echoes' unchanged-value refresh is being benchmarked before/after display caching. The synchronous Editor benchmark measures the presentation method, not overall frame time, layout, GPU, Player performance or gameplay throughput. The Mono per-thread allocation counter returned zero even for a calibration allocation and is treated as unavailable.

Shared-style validation now passes 29 checks in `shared-style-checks.txt`: a single heading token changes real navigation, quit and Library labels and restores them; nine native routes retain aspect-fitting sprite Images; tall woodcutting sprites are present in the picker; unchanged bounds avoid style rewrites. The initial token failure exposed inline font overrides in navigation/quit, which were removed for their standard heading size. The temporary override stylesheet existed only in the disposable copy and was deleted after testing.

The Alter Echoes unchanged-value microbenchmark used 61 developed generators at 3840x2160, time scale 1, cap 60, VSync off, 30 warm-up calls and three repetitions of 1,000 synchronous calls. Median method time was 0.6708-0.6736 ms before and 0.0430-0.0445 ms after caching. This measures unchanged-value presentation work; no frames advance inside a repetition, and it excludes deferred layout/rendering. It is not evidence for an equivalent whole-frame saving. `alterecho-refresh-before.txt` and `alterecho-refresh-after.txt` retain p95/p99/worst values. The per-thread allocation counter failed its calibration; allocation results are unavailable. Rate/zero-interval/restoration/power-change checks passed separately in `alterecho-cache-checks.txt`.

The revised Alter Echoes comparison waits for three game frames before taking the old renderer's reference; its first capture had sampled rows before their Update populated the labels. After this correction, all 25 collection/load/open-cycle checks pass with caching enabled. The failed first capture is retained as `alterecho-cache-first-checks.txt`. Items adds 36 passing checks for all five sort modes, all 69 row values, icons and tier frames in `statistics-items-checks.txt`.

### Follow-up visual defect reported during migration

- [ ] Top navigation button shadows draw over opened menus. Inspect the native navigation document's sorting order, button shadow bounds and dropdown clipping/stacking. Reproduce with each top menu, fix the shared layer/component, and verify desktop and mobile safe-area layouts. Added from the user's report while migration continues; not yet investigated or fixed.

### Forge (staged; not connected to Main yet)

The native Forge prefab and catalog now exist, using imported scene artwork, shared controls, disclosures, numeric inputs and square gear wells. Sprite Images use aspect-preserving fitting. The inventory companion uses native bounds rather than a Canvas spacer. The first render exposed oversized default numeric-input fonts and non-square gear frames; both were corrected before the reviewed `forge-native-standardized.png` capture. Ivan's portrait is currently static; the legacy decorative animation remains an open parity item.

`ForgeSession` separates selection, pending gear and autocrafting from the visible tree. It retains the old batching, salvage and upgrade-stop rules. The old Forge route is still active while integration validation continues. The conversion pipeline no longer stores an unused uGUI section reference; the statistics text builder is shared by both renderers. No gameplay rate or reward balance changes are intended.

Runtime validation in the disposable, offline Editor:

- `forge-foundation-checks.txt`: 69 passes covering pre-extraction statistics strings, all eight core mappings, and 32 seeded manual crafts across eight cores and four slots. The synchronous CLI request timed out, but the callback completed and wrote all results. Later tests use staged Editor callbacks. The developed test profile was altered by those earlier runtime crafts; it must not be described as an unchanged realistic save.

- `forge-view-checks.txt`: 116 passes comparing equipped text, craft availability, maximum crafts and icons against the legacy Forge; square gear frames, shared input font, aspect-fitting art and native inventory bounds/closure.

- `forge-conversion-checks.txt`: 289 passes across 96 synthetic cases: eight cores, four conversion types and desired amounts 1/500/50,000 against a controlled stock of 1,200 per resource. Old/new results match for affordability, calculated maximum, resource costs/yields, tier changes, cumulative resource totals and conversion telemetry. The final core's unavailable upward conversion is included. This is correctness coverage, not performance measurement.

- `shared-repeat-checks.txt`: nine timed pointer/keyboard checks pass. The initial run (`shared-repeat-before-fix.txt`) exposed Button's Clickable consuming presses before the repeat handler. The shared handler now observes pointer events in the trickle-down phase. It preserves the old 500 ms hold delay and 100 ms repeat interval, with a normal release click; release, leave, cancellation, disabled state and detachment stop repeats. These are synthetic native UI events, not physical touchscreen tests.

- `forge-automation-checks.txt`: 33 passes. Legacy/native fixtures both consumed three crafts at a configured cap of 2/s and 17 crafts at 50/s before stopping out of resources, and stopped after one upgrade with an empty slot. Synthetic high-stat equipped gear prevented upgrades in the exhaustion cases. Native crafting continued with its window closed, reattached on reopening, and stopped on core/slot changes. Twenty-five open/close cycles removed the rendered tree and session presentation subscription. No claim of zero retained managed UI objects or measured frame-time improvement follows from those checks.

The new session's load handler also clears pending automation without recording a cancellation against the newly loaded profile. The direct handler and delayed-batch cancellation checks pass; full end-to-end save-slot loading with automation remains outstanding. Automation fixture assets/settings were changed only in the disposable runtime and restored before leaving Play mode. Private/synthetic progression was never sent to Steam or UGS. Tests temporarily change the disposable progression; original save files are outside their target paths.

Remaining Forge work before activation: stat-lock/Vastium stop cases, pending replacement and salvage edge cases, full load lifecycle, live speed changes, mobile/safe-area layouts, decorative animation, installation and final subscription/retention verification. Development Player/device validation is still outstanding. All these results are Editor correctness checks, not whole-frame performance results. The original Editor has not confirmed compilation of the transferred staging files.

## Main-project cutover — 27 September 2026

The temporary UITK Editor is closed. The implementation is in the original `EchoesOfVasteria` checkout and its Main/Loading scenes; no second working project is required to play it.

- Repaired the missing Alter Echoes and Statistics native scene references, installed Forge, and removed menu fallbacks to old window bodies.

- Installed the native run HUD, five buff slots (activation, autocast, hold/right-click, duration/cooldown), retreat/death/restart actions, run breakdown, resource summary, Dad-o-cado, notifications and loading overlay.

- Replaced spawned world-space canvases with one native world-indicator document. Authored bindings contain sprite/text/layout data and read health, task and echo state directly. They do not render a hidden Canvas to a texture or read a TMP renderer's text each frame. Off-screen instances do not allocate visual nodes, and pooled/deactivated anchors release their nodes.

- Enemy health and task progress use the existing custom sliced-bar artwork; echo lifetime retains its green/yellow/red thresholds. Echo skill indicators compact into a native row. Ordinary world sprites and MeshRenderer-based floating damage/resource text remain world graphics, not uGUI canvases.

- Disabled retired canvases, graphics, GraphicRaycasters, controls, layouts, UI animators and legacy presenters in the saved scenes and prefabs. Disabled presenters no longer subscribe/build hidden content from `Awake`. The original authoring objects/code remain for traceability; they do not provide the live UI.

- Resource companion bounds no longer depend on hidden Canvas layout spacers. GameManager exposes death/town/loading presentation state directly. The old periodic run-button text coroutine is not started.

- Forge keeps an independent session while its visible tree is closed. Ivan's native portrait uses the original six-frame animation. Existing Cauldron portrait/pot animation is retained.

- Top-navigation shadows are clipped while a menu/window is open. This is shared behavior rather than per-window overrides.

### Main-project evidence

See [main-project captures and checks](ui-migration/main-validation-2026-09-27/README.md). Editor runs use a private copy of the developed save, an isolated product identity/save root, disabled Steam/UGS/network entry points, a 30 FPS cap and repeatable scripted actions. Synthetic cases are explicitly distinguished from natural progression.

- `installed-routes.txt`: 12 passes for installed native routes and isolation.

- `native-lifecycle.txt`: 106 passes, including all five authored maps, native return/breakdown/summary, forced death/restart/queued return, buff activation/autocast and 45 window-open cycles. No enabled Canvas, GraphicRaycaster or uGUI Graphic remains during the checked run/town states. Three-dimensional TextMeshPro floating effects are deliberately excluded from the uGUI count.

- `native-finishing.txt`: 11 passes for echo lifetime/skill layout, pool cleanup and notification event routing/expiry with time paused.

- `forge-edge-cases.txt`: 15 passes for pending replacement, single salvage on slot changes, closed/reopened automation, core change cancellation, forced Vastium stop, stat-set matching, full `LoadData` event cancellation and 25 reopen cycles.

- Repeated window cycles retained 19 PanelSettings before and after. This is a bounded-lifetime check, not a memory/performance benchmark or proof of zero leaks in an unlimited session.

An initial lifecycle audit incorrectly counted MeshRenderer-based floating TextMeshPro as uGUI because both inherit `Graphic`; the audit was corrected to distinguish the actual rendering paths. The world import also required a separate branch for the project's `SlicedFilledImage`, which does not inherit `Image`. Both issues were investigated rather than hidden by disabling spawned objects inside the tests.

### Verification limits

Desktop Editor viewport checks and synthetic pointer events do not establish physical Android/iOS touch, keyboard, safe-area, GPU or font-fallback parity. No real cloud writes, production recovery actions or real save-slot switches were performed. Full translations and rare error/dialog combinations still need platform QA. No whole-frame speedup is claimed by this migration's correctness checks.


### Player integration corrections

Player validation found that runtime-only PanelSettings omitted Unity ICU text data. `Assets/Resources/UI/ToolkitPanel.asset` now supplies the serialized UI shader and ICU dependencies; every runtime view clones it and keeps its own sorting/lifetime. Subsequent player logs no longer contain the missing-ICU warnings. Number-row and numpad buff activation were moved from the retired presenter to the native HUD and exercised with a temporary virtual keyboard.

The developer console now uses native controls, log/history, command completion and asynchronous result display. Existing project commands still use the same parser and authentication gate; their output routes to the native view. Backquote and the existing three-finger mobile activation route target it. Third-party Quantum Console interactive action extensions are outside the game command coverage and require separate compatibility checks.

## Visual quality correction status

The initial native cutover was functionally tested but visually rejected. The [visual parity correction pass](ui-migration/visual-parity-2026-09-27/README.md) records concrete layout, slicing and typography corrections and remaining acceptance work. Component standardization must preserve the original rendered design.
