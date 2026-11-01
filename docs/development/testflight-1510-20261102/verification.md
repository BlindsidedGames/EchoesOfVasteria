# TestFlight 1.5.10 (194), 2 November 2026

Destination: the existing Internal group. Normal game content, LZ4HC, normal
App Store Connect export (`internalOnly: false`). Android remains 187; no other
platform or production release is requested. App Store Connect's current builds
and pending uploads were checked again: 193 is the highest uploaded build.
The earlier prepared 194 package was never uploaded and is superseded.

The user approved the dialogue/Mines proposal, shorter notes and TestFlight upload
in the current instruction. All eight Library translations match the six short
notes in `localized-notes.json`. English notes SHA-256:
`9cd22857344073e48c2fd73d5dfd47c987533bee489e1fd0237e63364051f8f8`.

## Dialogue and Mines checks

- 36 localized strings checked against all eight native language tables.
- Sixteen opening/completion conversations have their approved text, speakers,
  anchors and keys. Apple and Oak use the correct orchard-state variants.
- The existing quest acceptance record persists openings; no save field or
  migration was added. Completion conversations do not award quest rewards.
- Two real QuestManager/disk persistence cases passed: new saves accept without
  spending, pay exactly 100 Logs at completion, and cannot pay again; previously
  completed Mines unlocks remain available and uncharged.
- The automatic Mines opening was observed in the private Main scene, accepted
  without completing the quest, and showed the meeting.
- Actual dialogue text and pointer actions passed 46 cases / 76 lines across
  eight locales and both orientations. A final 12-case / 18-line check in English,
  German and Japanese verified speaker names, both orchard states and the new
  Old Timer camera anchor. Screenshots were visually inspected in both layouts.
- Some CLI test calls returned zero discovered tests; those are not passes.

## Earlier seed/drop validation retained

ResourceDropTests 7/7; Farm EditMode 72/72; QuestResourceAccessTests 5/5;
TownPathProgressionTests 12/12; BackgroundRunEstimatorTests 37/37;
Hero/Echo crop completion 1/1; Fields pointer selection/plant/reload 1/1.
Actual foreground and background task awards, Turnip hand-in and locked-pack UI
were also checked using private saves. Ordinary looting behaviour was unchanged.

The broader Fields PlayMode fixture previously returned 74 passed and 22 failed;
its remaining failures have not all been established as baseline failures. It is
not represented as a passing full suite.

Local evidence: `Artifacts/QuestGatedSeeds20261102`,
`Artifacts/TestFlight1510Seeds20261102`, and
`Artifacts/TestFlight1510Dialogue20261102`. Player saves were preserved and private
capture was ended. The final build receipt records the exact source SHA, package
hashes, signing checks, upload receipt and store availability separately.
