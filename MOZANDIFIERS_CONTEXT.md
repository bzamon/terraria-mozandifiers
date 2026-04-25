# MOZANDIFIERS CONTEXT

Maintainer note: architecture or naming changes should update `MOZANDIFIERS_CONTEXT.md` and `MOZANDIFIERS_INDEX.md` in the same task.

## 1. Project Overview
`Mozandifiers` is a Terraria `tModLoader 1.4.4` mod centered on custom weapon prefixes, with a small amount of pet and starter-item content around that core. Current authored scope in the repo:
- 14 custom weapon prefixes in `Content/Prefixes/Weapons`
- 2 pet items and pet projectiles: `Amelia` and `Ludovica`
- 2 buffs/debuffs tied to `Awakened`: `AwakenedBuff` and `SealedDebuff`
- 1 hidden gameplay projectile: `DeadeyeBurstProjectile`
- 1 server-side tuning config covering every live prefix: `Common/Config/PrefixTuningConfig.cs`
- 1 starter-item hook: new characters receive `Amelia` if slot 0 is empty on world entry

Live user-facing prefix names:
- `Attuned`
- `Awakened`
- `Breaching`
- `Catalytic`
- `Deadeye`
- `Desperate`
- `Fractured` (`EchoingPrefix.cs`)
- `Radiant`
- `Vampiric` (`SanguinePrefix.cs`)
- `Shifting`
- `Skirmishing`
- `Spinbound`
- `Stormforged`
- `Temporal`

## 2. Technical Stack
- Language: `C#`
- Runtime: `tModLoader 1.4.4`
- Project shape: one mod project, `Mozandifiers.csproj`, importing `..\tModLoader.targets`
- Extra NuGet or third-party runtime dependencies: none found
- Localization: `Localization/en-US_Mods.Mozandifiers.hjson`
- Network sync:
  - explicit mod packet in `Mozandifiers.cs` for `ShiftingPlayer`
  - projectile state sync through `SendExtraAI` / `ReceiveExtraAI`

## 3. Repository Map
High-signal authored files and folders:

```text
Mozandifiers.cs
  Mod entry point and packet handler for Shifting sync.

Common/Config/PrefixTuningConfig.cs
  Server-side config surface for all 14 live prefixes.

Common/Combat/
  AwakenedCombatHelper.cs
    Sweet-spot evaluation against the current melee hitbox.
  StormforgedChainHelper.cs
    Chain-target search, damage routing, and lightning VFX.

Common/Players/
  AttunedPlayer.cs
  AwakenedPlayer.cs
  BreachingFeedbackPlayer.cs
  DeadeyePlayer.cs
  DesperatePlayer.cs
  ShiftingPlayer.cs
  SkirmishingPlayer.cs
  SpinboundPlayer.cs
  StartingItemsPlayer.cs
  StormforgedPlayer.cs
  TemporalFeedbackPlayer.cs
  VampiricPlayer.cs

Content/Prefixes/Common/
  BasePrefix.cs
  WeaponPrefix.cs
  WeaponPrefixIdentity.cs
  WeaponPrefixGlobalItem.cs
  WeaponPrefixGlobalProjectile.cs
  WeaponPrefixGlobalNPC.cs
  WeaponPrefixItemDispatch.cs
  WeaponPrefixProjectileDispatch.cs
  WeaponPrefixVisuals.cs
  AwakenedRuntime.cs
  CatalyticRuntime.cs
  DeadeyeRuntime.cs
  FracturedRuntime.cs
  ShiftingSimulationTypes.cs
  ShiftingSimulationPools.cs
  ShiftingSimulationDisplay.cs
  ShiftingSimulationStatScaling.cs
  ShiftingSimulationEffectScaling.cs
  SpinboundRuntime.cs
  TemporalRuntime.cs
  VampiricRuntime.cs

Content/Prefixes/Weapons/
  AttunedPrefix.cs
  AwakenedPrefix.cs
  BreachingPrefix.cs
  CatalyticPrefix.cs
  DeadeyePrefix.cs
  DesperatePrefix.cs
  EchoingPrefix.cs
  RadiantPrefix.cs
  SanguinePrefix.cs
  ShiftingPrefix.cs
  SkirmishingPrefix.cs
  SpinboundPrefix.cs
  StormforgedPrefix.cs
  TemporalPrefix.cs

Content/Projectiles/Weapons/
  DeadeyeBurstProjectile.cs

Content/Buffs/
  AwakenedBuff.cs
  SealedDebuff.cs

Content/Pets/
  Amelia/
  Ludovica/
```

## 4. Core Architecture
The mod is organized around a clear split:
- Prefix classes own roll eligibility, base stat package, tooltip text, and balance constants.
- `WeaponPrefixGlobalItem` owns item hooks and dispatches item-side behavior.
- `WeaponPrefixGlobalProjectile` owns per-projectile state, projectile hooks, and projectile sync.
- `WeaponPrefixGlobalNPC` owns target-persistent per-player NPC state.
- `Common/Players/*` owns owner-local timers, windows, caps, charge state, and feedback throttles.
- `Common/Combat/*` owns reusable combat math.

Current combat flow is:

```text
Prefix definition
  -> item/global hook
  -> item or projectile dispatch layer
  -> specialized runtime helper
  -> player or NPC persistent state when needed
```

Important shared layers:
- `WeaponPrefixIdentity.cs`
  - centralized prefix id lookup and `HasXPrefix(...)` helpers
  - also bridges active `Shifting` simulation lookup
- `WeaponPrefix.cs`
  - weapon-family predicates such as `IsMagicWeapon`, `IsStandardSwordWeapon`, `IsStandardProjectileCombatWeapon`
  - still carries `Echoing` mode taxonomy for `Fractured`
- `WeaponPrefixVisuals.cs`
  - all shared dust ids, colors, pulses, and tint helpers

## 5. Prefix Runtime Breakdown
### Attuned
- Roll gate: any mana-using magic weapon
- Base stats: lower damage/knockback/use speed, lower mana cost, higher shoot speed
- Runtime:
  - `AttunedPlayer` tracks resonance stacks, decay, hit gain cooldown, and empowered-cast window
  - item mana hook consumes full resonance for a free cast
  - projectile spawn/hit path tags empowered shots and grants the projectile-speed release bonus

### Awakened
- Roll gate: standard swinging swords only
- Base stats: dormant use-speed bonus and crit bonus
- Runtime:
  - `AwakenedPlayer` tracks dormant, awakened, and recovery states
  - `AwakenedCombatHelper` evaluates sweet-spot contact against the live melee hitbox
  - `AwakenedRuntime` handles meter gain, awakened strike bonus damage, VFX, and boss-budgeted bonus damage
  - `WeaponPrefixGlobalNPC` stores the per-player rolling boss bonus budget window

### Breaching
- Roll gate:
  - heavy swing melee
  - heavy projectile melee with enough knockback
  - ranged projectile weapons with high knockback or rockets
- Base stats: damage, knockback, faster use, crit penalty, armor penetration
- Runtime:
  - optional melee scale bonus for heavy swing weapons
  - impact feedback against resistant or heavy-hit targets
  - projectile tagging preserves breach visuals and impact scale

### Catalytic
- Roll gate: standard magic projectile weapons
- Base stats: damage penalty, crit penalty
- Runtime:
  - target mark stored in `WeaponPrefixGlobalNPC`
  - first qualifying hit applies mark, later armed hit consumes it for bonus damage
  - shifted catalytic uses the same infrastructure with separate per-player arrays

### Deadeye
- Roll gate: standard ranged projectile weapons
- Base stats: faster use, faster projectile speed, lower knockback, crit bonus
- Runtime:
  - `DeadeyePlayer` tracks held-charge progress, stationary charge shortcut, ready state, and projectile assignment lockout
  - charged projectile gains bonus crit chance and crit damage
  - crits create hidden `DeadeyeBurstProjectile` hits on nearby enemies

### Desperate
- Roll gate: standard melee, ranged, and magic combat weapons
- Base stats: damage penalty plus crit bonus
- Runtime:
  - `DesperatePlayer` computes threshold state from current life
  - critical-life state arms periodic surge hits for extra source damage
  - both direct and projectile hits can consume surge and emit impact feedback

### Fractured
- Roll gate: only `Echoing` weapon modes that `WeaponPrefix.IsImplementedEchoingMode(...)` allows
- Base stats: damage penalty, faster use, crit penalty
- Runtime:
  - `FracturedRuntime` spawns fracture copies in up to three tiers
  - source projectile snapshots hit-immunity behavior so fracture copies behave consistently
  - delayed tier II and III spawns are scheduled per projectile
  - visual tinting and afterimages are handled in projectile draw hooks

### Radiant
- Roll gate: standard direct combat weapons
- Base stats: slight damage/knockback penalty, small crit bonus
- Runtime:
  - item or projectile hit applies `On Fire!`
  - held/projectile visuals are light-weight and purely feedback-oriented

### Vampiric
- Roll gate: standard melee, ranged, and magic weapons
- Runtime file naming still uses `Sanguine`
- Runtime:
  - life steal and optional mana siphon
  - prey-state thresholds: weakened, bloodied, critical
  - per-second sustain caps live in `VampiricPlayer`
  - frenzy state grants attack speed, bigger caps, and conditional crit forcing
  - NPC-side per-player threshold trigger tracking prevents repeated frenzy spam

### Shifting
- Roll gate: only weapon families supported by `ShiftingSimulationPools`
  - swing melee
  - ranged projectile
  - magic projectile
- Runtime:
  - `ShiftingPlayer` owns active simulation state, authority, reroll timing, and packet sync for the currently held item
  - `ShiftingSimulationPools` owns weapon-mode detection and simulation-pool selection
  - `ShiftingSimulationDisplay` owns simulation display-name lookup
  - `ShiftingSimulationStatScaling` and `ShiftingSimulationEffectScaling` own shifted stat and effect values
  - server is authoritative; client state is synced by mod packet
  - projectile spawn snapshots shifted state when necessary
  - the simulation pools differ by weapon mode

### Skirmishing
- Roll gate: ranged projectile weapons
- Base stats: damage/knockback penalty, much faster use, faster projectiles, crit bonus
- Runtime:
  - hits open a timed mobility window in `SkirmishingPlayer`
  - firing during the window consumes it and empowers the next projectile
  - projectile follow-up state grants bonus crit damage and distinct VFX

### Spinbound
- Roll gate: ranged projectile weapons
- Base stats: damage/use/shoot-speed bonuses, knockback and crit penalty, armor penetration
- Runtime:
  - `SpinboundPlayer` tracks Fibonacci cadence per item type and per shifted/non-shifted context
  - `SpinboundRuntime` evaluates shot geometry against golden-ratio and golden-angle checks
  - successful releases assign a `GoldenSpinTier`
  - hit bonus is consumed once per projectile

### Stormforged
- Roll gate: swing melee and standard projectile combat weapons
- Base stats: small damage penalty, faster use, crit bonus
- Runtime:
  - `StormforgedChainHelper` applies chance-based chain lightning to nearby targets
  - local owner gating prevents duplicate feedback and damage loops
  - `StormforgedPlayer.SuppressStormforgedChain` blocks recursive chaining

### Temporal
- Roll gate: any standard weapon
- Base stats:
  - `TemporalPrefix.SetStats(...)` assigns strong attack-speed and projectile-speed behavior directly
  - tooltip and runtime frame it as a tempo modifier with delayed collapse payoff
- Runtime:
  - hits build target-local per-player pressure in `WeaponPrefixGlobalNPC`
  - reaching pressure max starts a temporary fracture state
  - fractured hits store resolved damage instead of dealing it immediately
  - fracture expiry or lethal stored damage triggers collapse for multiplied release damage

## 6. Shifting Simulation Pools
`Shifting` does not simulate every prefix for every weapon type.

Pool by weapon mode:
- Melee swing:
  - `Breaching`
  - `Desperate`
  - `Radiant`
  - `Vampiric`
  - `Temporal`
  - `Stormforged`
- Ranged projectile:
  - `Breaching`
  - `Skirmishing`
  - `Deadeye`
  - `Desperate`
  - `Radiant`
  - `Vampiric`
  - `Temporal`
  - `Fractured`
  - `Stormforged`
  - `Spinbound`
- Magic projectile:
  - `Attuned`
  - `Desperate`
  - `Radiant`
  - `Vampiric`
  - `Temporal`
  - `Fractured`
  - `Stormforged`
  - `Catalytic`

## 7. Persistent State Ownership
- `AttunedPlayer`
  - resonance and empowered-cast state
- `AwakenedPlayer`
  - awakened meter, awakened/recovery timers, current swing hitbox
- `DeadeyePlayer`
  - ready charge, stationary tracking, projectile assignment lockout
- `DesperatePlayer`
  - low-life threshold state and surge cooldown
- `ShiftingPlayer`
  - active simulation, item context, combat timeout, sync payload
- `SkirmishingPlayer`
  - skirmish window and follow-up projectile window
- `SpinboundPlayer`
  - cadence state and queued shot contexts
- `TemporalFeedbackPlayer`
  - local-only feedback throttles
- `VampiricPlayer`
  - sustain caps, frenzy timer/state, pending direct prey state
- `WeaponPrefixGlobalNPC`
  - catalytic marks
  - vampiric frenzy trigger state per player
  - awakened boss bonus-damage budgeting
  - temporal pressure, fracture, cooldown, and stored damage per player

## 8. Non-Prefix Content
- `StartingItemsPlayer`
  - gives `AmeliaItem` to a fresh character if max life is `100` and inventory slot `0` is empty
- Pets:
  - `AmeliaItem` / `AmeliaBuff` / `AmeliaProjectile`
- `LudovicaItem` / `LudovicaBuff` / `LudovicaProjectile`
  - both pet projectiles clone vanilla `Puppy` behavior
- Maintainer boundary:
  - pets and starter-item logic are secondary systems
  - they are intentionally isolated from prefix gameplay
  - future tasks should not couple them to prefix behavior unless that coupling is explicit

## 9. Current Naming and Compatibility Quirks
- User-facing `Fractured` is still implemented in `EchoingPrefix.cs` and related `Echo*` terminology.
- User-facing `Vampiric` is still implemented in `SanguinePrefix.cs`.
- `PrefixTuningConfig` keeps compatibility properties for older `Echoing` and `Colossal` names.
- `WeaponPrefix.GetEchoingWeaponMode(...)` still models more melee-projectile categories than the project currently wants to expand intentionally.

## 10. Maintenance Notes
- The repo is architecture-driven now: dispatch and runtime helpers are a first-class part of the design, not incidental.
- `Shifting` remains the most cross-cutting feature and the main documentation risk if it drifts.
- `StartingItemsPlayer` still contains an unused `receivedStarterItem` field and old-style `using` clutter.
- Pet files still use older formatting and extra unused imports, but they are isolated from the prefix systems.
