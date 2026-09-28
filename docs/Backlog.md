# Development backlog

Updated: 2026-09-26. This is the current planning list. Items below are unimplemented unless explicitly marked complete. Suggested design details are starting points for review, not settled specifications.

## Agreed direction

- Preserve normal endgame movement and task speed: the current feel without Slipstream is acceptable.
- Revisit Slipstream separately rather than applying broad speed nerfs.
- Improve the town scene and build a complete introduction that onboards new players.
- Explore how the new icon collection can support clearer presentation and additional content after inspecting the actual assets.
- Do not pursue starting-distance skips as the Slipstream replacement. Skill levels govern task unlocks, so skipping terrain removes gathering opportunities without inherently unlocking better tasks. Individual task distance restrictions still exist in code.

## UI Toolkit migration follow-ups

- [ ] Fix top navigation button shadows appearing over opened menus. Check shared menu layering and clipping; validate each top menu and supported aspect ratios. Reported 2026-09-27 during the ongoing migration.

## Town scene refresh

- [ ] Review the current town layout, interaction points, navigation and visual hierarchy; identify what feels weak before changing the scene.
- [ ] Establish a coherent visual direction using the existing world and art, with a recognisable arrival point and readable routes to services.
- [ ] Improve building and NPC placement, ground detail, lighting, ambient motion and sound while keeping interactive areas clear.
- [ ] Make available services, quest turn-ins and new unlocks easy to recognise using meaningful icons and consistent interaction feedback.
- [ ] Connect the layout to the first-time player journey so the next useful action is understandable.
- [ ] Check readability and navigation at supported resolutions and input methods, including a developed save with many services unlocked.

Completion target: town feels like a deliberate home base, and players can identify where to start a run, spend rewards and find their next objective without hunting through the scene.

## Full introduction and onboarding

- [ ] Audit existing tutorial, quest and unlock behaviour before designing the replacement flow.
- [ ] Define the opening premise, player motivation and first achievable goal.
- [ ] Guide a new player through starting a run, understanding automatic movement/combat/gathering, collecting rewards, returning to town, buying an upgrade and starting a stronger second run.
- [ ] Explain skill-based task unlocks, quests that can be completed in the field, and the difference between returning, dying and being reaped at the relevant moment.
- [ ] Introduce buffs, echoes, equipment/forging and later town systems as they become relevant rather than presenting every system at the start.
- [ ] Use short contextual prompts, clear highlights and actual player actions; avoid a long sequence of instruction panels.
- [ ] Support skipping, replaying help, and resuming after closing the game. Preserve existing saves and avoid forcing established players through the introduction.
- [ ] Validate localisation, text size and supported controls, and handle unexpected actions without trapping the player.
- [ ] Playtest from a fresh save through the first meaningful upgrade and repeat run, including quitting midway and returning.

Completion target: a new player understands the run/reward/upgrade loop and can choose the next useful action independently.

## Slipstream redesign

- [ ] Decide its role: optional acceleration, catch-up assistance, or a different buff effect. No replacement design is selected yet.
- [ ] Preserve gathering opportunities and the normal endgame pace.
- [ ] Review Cauldron buff-power scaling of game speed: the reviewed maxed setup produced 4x total game speed, with other speed multipliers applied on top.
- [ ] Review the distance cutoff, which currently uses the overall longest run rather than a per-map record, if distance remains part of the design.
- [ ] Compare readability, resource/XP income per real minute and run duration before and after a prototype; avoid turning a presentation improvement into extra grind.
- [ ] Verify attack scheduling at accelerated speed and different frame rates before balancing around displayed attack rates.

## New icons and content opportunities

These are exploration ideas, not approved feature commitments.

- [ ] Inventory the new icons, organise them by purpose and identify gaps, variants and style mismatches.
- [ ] Improve town service recognition, quest rewards, tooltips and run summaries using consistent icon meanings.
- [ ] Explore map-specific discovery collections and milestones that give locations more identity.
- [ ] Explore rare resource variants, unusual chests and elite encounters with recognisable presentation and worthwhile rewards.
- [ ] Explore specialised gathering, treasure, echo and distance-pushing builds without making every option another speed multiplier.

## Scene feel and feedback

- [ ] Explore distinct ambience and restrained environmental detail for Mines and Spooky while preserving readable terrain boundaries.
- [ ] Reduce overlapping reward text and surface valuable drops, discoveries and progress toward the next upgrade more clearly.
- [ ] Improve the return-to-town summary so the result and reason the run ended are clear.

## Historical backlog triage

- [ ] Review [the previous TODO list](archive/LegacyTodo.md) against the current game. Bring forward only still-relevant work, and mark superseded or completed ideas rather than treating the old list as authoritative.

Suggested sequence: review town and existing onboarding together, agree the first-player journey, implement the town and introduction in small playable stages, then prototype Slipstream and evaluate additional content.
