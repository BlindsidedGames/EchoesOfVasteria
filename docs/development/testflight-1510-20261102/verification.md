# Internal candidate, 2 November 2026

Gameplay candidate: `c57eac7d406ef4aa023c42c998911bf75dd6e26f` on `Matt/watch-design-lab`.
Marketing version: 1.5.10. iOS build number awaits a fresh App Store Connect inventory;
193 must not be reused. Android remains 187 and is not being deployed.

Windows Unity 6000.6.0f1 checks passed:

- ResourceDropTests: 7/7.
- Farm EditMode tests: 72/72.
- QuestResourceAccessTests: 5/5.
- TownPathProgressionTests: 12/12.
- BackgroundRunEstimatorTests: 37/37.
- HeroAndEchoCropCompletionsShareTheSameSeedRollAndSingleCredit: 1/1.
- FieldsPointerSelectionPlantAndReloadStayCoherent: 1/1. Its existing late-game
  setup now includes completed mill/well projects instead of assuming level alone
  unlocks automation.

The broader Fields PlayMode fixture previously returned 74 passed and 22 failed.
It has not been rerun in full, and its remaining failures have not all been
established as baseline failures. The focused Hero/Echo and pointer tests above
were rerun successfully after their setup corrections. Some subsequent CLI test
filters returned zero tests, which is not counted as a pass. Clean Mac continuation
and lifecycle checks remain pending.

Private native runtime verification exercised the actual production owners:

- 1,000 background crop completions with the introduction locked paid no packs;
  after unlocking, 1,000 completions paid 65. Forecast pack yield was approximately
  0.07 per task.
- Hero and Echo each completed 128 real crop tasks, awarding 11 and 12 packs.
  Ticking completed tasks again did not award another pack.
- QuestManager spent 6,000 Turnips, awarded one pack and approximately 20,000
  Farming XP, enabled future Turnip pack drops, and rejected repeat hand-in.
- All 24 crop/sapling mappings, eight new quests and eight localizations were
  checked. Existing crop hand-in costs/XP, ordinary task drops and sapling chance
  curves were preserved.
- Actual Fields and Library UI were captured and visually inspected at
  1920×1080 and 1080×1920. The locked-pack quest hint, selection and disabled
  planting action worked. New Library notes match the six-bullet draft and retain
  the previous release history.

Evidence is retained locally in ignored `Artifacts/QuestGatedSeeds20261102` and
`Artifacts/TestFlight1510Seeds20261102`. All runtime checks used disposable save
state; private capture was ended afterwards. Native Mac export, signing,
App Store Connect validation, upload and Internal tester availability are pending.
Player note approval is also pending under AGENTS.md.
