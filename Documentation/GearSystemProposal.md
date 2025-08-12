# EoV Gear System v3.2 — 8 Blueprint Tiers, Ingot-Linked Crafting with Full Rarity Access, Auto-Upgrade

## 0) Design Commitments

- **No item levels** on blueprints or gear.
- **8 blueprint tiers** match 8 ingot types 1:1:
  1. Common → Eznorb ingots
  2. Uncommon → Nori ingots
  3. Rare → Dlog ingots
  4. Epic → Erif ingots
  5. Legendary → Lirium ingots
  6. Mythic → Copium ingots
  7. Ancient → Idle ingots
  8. Transcendent → Vastium ingots
- **Rarities use standard naming** as above.
- **Single craft target:** crafting rolls a **random gear slot**; blueprints have **no stat affinities**.
- **Mastery (long‑tail):** any blueprint can roll **any rarity**; Ivan’s mastery scales the **upper‑tail** odds. Lower‑tier blueprints retain *extremely* small chances for top tiers.
- **No gear hoarding:** after each craft choose **Replace** or **Salvage**; Auto-Upgrade mode mass-crafts and interrupts only for likely upgrades.
- **Rollable stats:** Attack, Attack Speed, Crit Chance, Health, Health Regen, Defense, Move Speed.

---

## 1) Core Loop

1. Acquire a **Blueprint** of a given rarity (linked to an ingot type).
2. Spend the required ingot type to craft.
3. System rolls **Rarity → Slot → Affixes** using a **long‑tail weighting** (any tier eligible) shaped by the blueprint’s base tilt, Ivan’s mastery, and pity. Lower-tier blueprints keep vanishingly small odds for top tiers.
4. Present a **Replace vs Salvage** choice. Auto mode stops only on upgrades.

---

## 2) Content Objects

### 2.1 Blueprint

- **Identity:** one per rarity tier, fixed to a specific ingot type.
- **Function:** sets the **ingot cost** and a **base tilt** of rarity weights centered near the blueprint’s tier, **without capping** higher tiers (long-tail remains > 0).
- **Cost:** consumes the matching ingot per craft (amount tuned per tier).

### 2.2 Gear

- **Slot:** chosen at craft (random with smart-protection).
- **Rarity:** rolled from the full table with blueprint-tier-biased weights, mastery scaling, and pity.
- **Affixes:** from the global stat list; count and floors by rarity.

### 2.3 Salvage

- Declined crafts convert to **Shards** for rerolls or random blueprint purchases.

---

## 3) Rarities

1. Common — 1 affix, low floors
2. Uncommon — 2 affixes
3. Rare — 3 affixes
4. Epic — 4 affixes
5. Legendary — 4 affixes, +higher floors
6. Mythic — 4 affixes, +higher floors, +1 Amplifier
7. Ancient — 5 affixes, high floors, +1 Amplifier
8. Transcendent — 5 affixes, very high floors, +2 Amplifiers

**Floor defaults:** Common 0%, Uncommon 10%, Rare 25%, Epic 40%, Legendary 55%, Mythic 65%, Ancient 75%, Transcendent 85%.

---

## 4) Weighting & Mastery — Long‑Tail Model

### 4.1 Base rarity weights (global)

```
Base = { Common:1000, Uncommon:500, Rare:240, Epic:120, Legendary:60, Mythic:30, Ancient:12, Transcendent:3 }
```

These establish broad rarity scarcity independent of blueprint.

### 4.2 Blueprint tilt (no hard cap)

Let blueprint tier index be `b ∈ {0..7}` and target rarity tier be `t ∈ {0..7}`.

- Define distance up/down: `du = max(0, t-b)`, `dd = max(0, b-t)`.
- Apply a symmetric decay around `b`:

```
Tilt_b(t) = A_b * U_b^du * D_b^dd
```

Where `U_b ∈ (0,1)` controls *upward* tail steepness (smaller → rarer high tiers), `D_b ∈ (0,1)` controls *downward* tilt, and `A_b` is an apex normalizer. Lower‑tier blueprints use smaller `U_b` (steeper decay) than higher tiers.

### 4.3 Mastery amplification (upper-tail only)

Ivan’s Mastery level `M ∈ [0..20]` multiplies the **upward tail**:

```
UpperAmp(M) = 3^(M/20)   // ×1.0 → ×3.0 from L0→L20
Tail_b,M(t) = Tilt_b(t) * (UpperAmp(M))^du
```

This preserves the long‑tail shape while rewarding progression.

### 4.4 Final weights (per craft)

```
W[r] = Base[r] * Tail_b,M(t) * GlobalRarityMods[r]
Normalize W → probabilities
```

### 4.5 Anchor constraints (design targets)

We calibrate `U_b` and `A_b` so specific **anchor odds** hold at Mastery L0 (after normalization):

- **P(Transcendent | Common blueprint) ≈ 1e−6** (≈ 1 in 1,000,000)
- **P(Transcendent | Transcendent blueprint) ≈ 0.05** (≈ 1 in 20) Suggested baseline anchors at **M=0** for top-tier odds:

```
Blueprint → P(Transcendent)
Common ............ 0.000001
Uncommon .......... 0.000005
Rare .............. 0.000050
Epic .............. 0.000200
Legendary ......... 0.000800
Mythic ............ 0.003000
Ancient ........... 0.010000
Transcendent ...... 0.050000
```

At **M=20**, multiply these by `UpperAmp(20) = 3.0` (clamped so Transcendent blueprint tops at \~12–15% after full normalization). All tiers remain eligible at all times.

### 4.6 Pity (bounded, non‑top forcing)

- Guarantee **Rare+** within 10 crafts, **Epic+** within 40, **Legendary+** within 120, **Mythic+** within 300.
- Pity **never** guarantees Transcendent; it only raises the minimum tier up to Mythic, preserving the long‑tail jackpot feel.

## 5) Slot & Affix Rolling

- **Slot:** uniform random with smart protection.
- **Stats:** Attack, Attack Speed, Crit Chance, Health, Health Regen, Defense, Move Speed.
- **Affix count & floors:** driven by rarity table.

---

## 6) Auto-Upgrade System

- **Options:** Auto Craft toggle, Budget, Stop on Upgrade Candidate.
- **Preferences:** Balanced / Offense / Defense / Speed.
- **Candidate detection:**
  1. More affixes than current.
  2. Multi-stat improvement score exceeds threshold.
  3. Amplifier bonus counts toward score.
- **Flow:** Non-candidates auto-salvage, candidates prompt Replace vs Salvage.

---

## 7) Economy

- **Craft cost:** fixed ingot requirement per blueprint tier.
- **Salvage yield:** 10–20 Shards.
- **Optional sink:** extra ingots for increased higher-rarity odds.

---

## 8) Minimal UI

- Craft Panel: blueprint picker, rarity odds bars, Auto toggle, Budget, Preferences, Forge.
- Result Prompt: before/after diff, Replace | Salvage.
- History Drawer: last 10 results.

---

## 9) Implementation Notes

- SOs: `BlueprintSO`, `RaritySO`, `StatDefSO`, `CraftingConfigSO`.
- Services: `CraftingService`, `AutoCrafter`, `ComparisonService`, `SalvageService`.
- Telemetry: craft counts, rarity outcomes, pity fires, auto-stop reasons, mastery progression.

---

## 10) Next Steps

- Finalize per-blueprint base rarity tables and mastery multipliers.
- Implement `CraftingService` logic in Unity 6.

