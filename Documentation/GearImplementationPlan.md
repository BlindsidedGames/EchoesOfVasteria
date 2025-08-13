## Gear System Implementation Plan (v3.2 baseline)

### Decisions (confirmed)
- **Crit chance**: Only from gear. On hit, crit doubles damage (×2) with no extra multiplier. Applied to hero attacks only.
- **Amplifiers**: Omitted for now. Rarity entries will have amplifier count = 0 until later.
- **Rarity weights**: Start with **manual weights defined in ScriptableObjects** per blueprint. We can later switch to/augment with long‑tail tilt and mastery math when we tune anchors.
- **No item levels**. Eight blueprint tiers map 1:1 to ingots (Eznorb → Vastium) and any rarity can roll from any blueprint.
- **Slots (craftable)**: `Weapon, Helm, Chest, Boots`.

### Current codebase fit (what exists vs. missing)
- Exists
  - `ResourceManager` wallet and ingot resources are already present (`Eznorb/Nori/Dlog/Erif/Lirium/Copium/Idle/Vastium` ingots).
  - Stat upgrades (`StatUpgradeController`) and Buffs (`BuffManager`) aggregate into hero stats.
  - Combat pipeline: hero damage assembled in `HeroController.Attack(...)` and applied in `Projectile` → `Enemies.Health.TakeDamage(...)`.
  - Save/load hooks (`OnSaveData`/`OnLoadData`). UI window manager for town panels.
- Missing (to be added)
  - Gear/equipment items, slots, rarity, affixes, and crafting/blueprints.
  - Salvage/Shards currency.
  - Mastery/pity for crafting (data and logic).
  - Crit chance stat layer (from gear only).

### Data model (new ScriptableObjects)
- `StatDefSO` (what it is and why)
  - Defines a single rollable stat/affix: id (string), displayName, icon, isPercent, and how it scales/rolls (minRoll, maxRoll, rollCurve).
  - Tells the system how to apply that stat to the hero via `heroMapping` (Damage, AttackRate, Defense, MaxHealth, HealthRegen, MoveSpeed, CritChance).
  - Also provides an optional `comparisonScale` used by the auto-comparison to convert raw deltas into comparable "points" (see Comparison below). These are content knobs we can tune later.
  - Initial stat set we will author: Damage, Attack Rate, Defense, Max Health, Health Regen, Move Speed, Crit Chance.

- `RaritySO`
  - name, tierIndex (0..7), color.
  - affixCount, floors (percent floor applied to each stat roll), amplifierCount = 0 (for now).
  - optional `globalRarityWeightMod` (for future balancing).

- `CoreSO`
  - tierIndex (0..7), requiredIngot `Resource`, ingotCost.
  - `rarityWeights`: per-rarity base weight and per-level modifier (can be negative), normalized at runtime.
  - optional `slotWeights` for slot distribution (defaults to uniform with smart-protection).

- `CraftingConfigSO`
  - pity thresholds (Rare+ within 10, Epic+ within 40, Legendary+ within 120, Mythic+ within 300).
  - budget caps, auto-crafter defaults.
  - knobs for smart slot protection and reroll behavior.

### Runtime types and services (new)
- `GearItem`
  - slot, rarity (`RaritySO`), list of `{ stat: StatDefSO, value }` affixes, rolled metadata.

- `EquipmentController` (singleton)
  - Holds equipped `GearItem` by slot.
  - Aggregates total gear bonuses per `StatDefSO`, exposes convenience properties (e.g., `CritChanceFromGear`).
  - Event: `OnEquipmentChanged`.

- `CraftingService`
  - Input: selected `CoreSO`.
  - Computes rarity using `CoreSO.rarityWeights` (base) + per-level modifiers (can be negative) with pity clamps.
  - Rolls slot (uniform + smart-protection), then affixes (unique stats) and values honoring rarity floors.
  - Spends ingots via `ResourceManager.Spend` and returns `GearItem`.
  - Tracks pity counters and Ivan level (level is stored in `GameData.CraftingMasteryLevel`).

- `SalvageService`
  - Converts declined gear into tiered crafting resources (chunks/crystals) defined per `CoreSO.salvageDrops` using the same `ResourceDrop` format as enemies/tasks.
  - Yield scaled by a small random range.

- `ComparisonService` (optional for manual crafting)
  - Computes a simple score delta vs. equipped to display in the result prompt.
  - For manual crafting, this is UI-only (no auto decisions).

### Combat integration (Crit, Attack Speed mapping)
- Aggregation: hero effective stats remain `base + upgrades + buffs + gear`.
  - Implement a gear stat layer accessed from `HeroController`.

- Crit application:
  - Source: `EquipmentController.CritChanceFromGear` only.
  - Location: in `HeroController.Attack(...)` before calling `Projectile.Init(...)`.
    - Compute `dmgBase` as today, then `total = dmgBase * killTrackerBonus`.
    - Roll `isCrit = Random.value < critChance`.
    - If crit, set `total *= 2f` and pass `bonusDamage = total - dmgBase` to `Projectile.Init`.
  - No changes to enemy damage calculation; `Health.TakeDamage(amount, bonusDamage)` already applies defense to `amount + bonusDamage`.

### Save data extensions
- Add to `GameData` (non-breaking defaults) — implemented now:
  - `Dictionary<string, GearItemRecord> EquipmentBySlot`.
  - `int CraftingMasteryLevel` (reserved for future; not used yet).
  - `int PityCraftsSinceLast` (simple counter used to clamp minimum rarity per pity thresholds).

### UI
- `ForgeWindowUI`
  - Blueprint picker (8, one per ingot), rarity odds bars (from `BlueprintSO.rarityWeights`), Forge button.
  - Integrate with `TownWindowManager` as a new window.

- `CraftResultPromptUI`
  - Shows before/after stats (and optional score); buttons: Replace | Salvage.

### Economy
- Create a `Resource` asset for `Shards`.
- Blueprint ingot costs: start with a flat cost of **1 ingot per craft** for all tiers (tune later).
- Salvage yields Shards (configurable 10–20 baseline).

### Phased delivery
1) Foundations: SOs, `EquipmentController`, `CraftingService` (manual rarity weights), Shards resource, Forge window, manual craft/replace/salvage, crit from gear, pity.
2) Optional: Comparison scoring UI refinement, telemetry.
3) Later: Auto‑Crafter, mastery integration and long‑tail tilt, smart slot protection tuning.
4) Later: Amplifiers and extended stats.

### Phase 1 deliverables (implemented now)
- ScriptableObjects: `StatDefSO`, `RaritySO`, `BlueprintSO`, `CraftingConfigSO`.
- Runtime systems: `GearItem`, `EquipmentController` (equip, aggregate, save/load), `CraftingService` (manual rarity weights, pity clamp, slot protection), `SalvageService` (Shards yield).
- Combat: Crit integrated in `HeroController.Attack(...)` as 2× chance from gear (via `EquipmentController` + `StatDefSO` mapping to `CritChance`).
- UI: `ForgeWindowUI` for manual crafting, odds display, and Replace | Salvage prompt.
- Save: `EquipmentBySlot`, `CraftingMasteryLevel` placeholder, `PityCraftsSinceLast`.

### Telemetry (optional but recommended)
- Track crafts, rarity outcomes, pity triggers, auto‑stop reasons, mastery level, salvage counts.

### Open choices to confirm
- Exact affix ranges per `StatDefSO` (min/max and curves) — we will start broad and tune.
- Salvage shard yields (baseline 10–20 suggested).

### Code touchpoints (planned edits)
- Add new scripts under `Assets/Scripts/Gear/`:
  - `GearItem.cs`, `EquipmentController.cs`, `CraftingService.cs`, `SalvageService.cs`, optional `ComparisonService.cs`.
- Add new SO types under `Assets/Scripts/Gear/SO/`:
  - `StatDefSO.cs`, `RaritySO.cs`, `BlueprintSO.cs`, `CraftingConfigSO.cs`.
- Integrations:
  - `HeroController.Attack(...)`: read crit chance from `EquipmentController` and apply ×2 crits.
  - `TownWindowManager`: register Forge window.
  - `GameData`: new fields for gear, pity, mastery (can stay unused for now).

### Comparison (manual crafting context)
- Purpose: Provide a clear before/after diff and an optional simple score to help the player decide.
- Mechanics:
  - For each stat, compute `delta = new - current` and convert to points using `StatDefSO.comparisonScale` (UI only).
  - Sum points for an overall score; display as guidance. No automatic decisions.
  - We can later add preferences and thresholds when we implement Auto‑Crafter.

### Health regeneration integration
- Current behavior (implemented): `HeroRegen` updates each frame using `Time.deltaTime`, treating regen as a per‑second value applied continuously.
- If we add a gear `HealthRegen` stat later, we will sum gear‑provided regen with the existing upgrade‑based regen before applying per‑frame healing, so all regen remains consistent and per‑second.

### Your setup checklist (to do in the editor)
- Create 8 `RaritySO` assets (Common → Transcendent) with affix counts and floors per the proposal.
- Create `StatDefSO` assets for: Damage, Attack Rate, Defense, Max Health, Move Speed, Crit Chance. Optionally add Health Regen later.
  - Set roll min/max and curves; set `heroMapping` appropriately (e.g., Crit Chance → `CritChance`).
- Create 8 `BlueprintSO` assets (one per ingot tier), set `requiredIngot` to the matching ingot `Resource`, set `ingotCost = 1`, and fill `rarityWeights` (temporary manual values). Optional: per‑blueprint slot weights.
- Create a `CraftingConfigSO` (defaults are fine) and place it on a `CraftingService` instance.
  
- In your town scene, add: `EquipmentController`, `CraftingService`, `SalvageService`, and `ForgeWindowUI`.
  - Wire `ForgeWindowUI` references (blueprint button parent + prefab, craft/replace/salvage buttons, result panel, odds parent + text prefab).
  - Optionally register the Forge window with `TownWindowManager`.
- Ensure `CraftingService` has access to your `RaritySO` and `StatDefSO` (via inspector or auto‑load from Resources).
- Test: craft with a blueprint, replace/salvage, verify crits by giving a high `Crit Chance` value on an item.

### Verification (current repo scan)
- No existing gear/equipment/crafting/blueprint/rarity/affix systems detected.
- No existing crit system; clean to add crit sourced only from gear.
- Combat damage assembly points identified and compatible with crit hook.
- Ingots exist as `Resource` assets; Shards do not yet exist and will be added.

### Next steps
- Confirm slot list and initial stat definitions (including `CritChance`).
- Create SO types and initial assets (8 `RaritySO`, 8 `BlueprintSO`, `CraftingConfigSO`, `StatDefSO` set for Damage/AttackRate/Defense/MaxHealth/HealthRegen/MoveSpeed/CritChance).
- Implement `EquipmentController` and `CraftingService` with manual rarity weights and pity clamps.
- Add Forge UI and basic craft/replace/salvage flow.
- Hook crit into `HeroController.Attack(...)` reading `EquipmentController.CritChanceFromGear`.


