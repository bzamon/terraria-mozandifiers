# MOZANDIFIERS CONTEXT

## 1. Project Overview
`Mozandifiers` is a Terraria mod for `tModLoader 1.4.4` written in C#. The project is focused on custom weapon prefixes/modifiers rather than a broad content pack. The player-facing concept, based on `description.txt`, `description_workshop.txt`, localization, and the prefix class names, is a roster of custom weapon modifiers with distinct tradeoffs, plus two bonus vanity dog pets.

The live modifier roster currently contains 14 weapon prefixes:
- `Echoing`
- `Attuned`
- `Breaching`
- `Skirmishing`
- `Sanguine`
- `Radiant`
- `Stormforged`
- `Temporal`
- `Desperate`
- `Siphoning`
- `Catalytic`
- `Headshot`
- `Spinbound`
- `Shifting`

The system is built around explicit per-prefix classes plus shared runtime hooks for item-side, projectile-side, NPC-side, and player-side behavior.

## 2. Technical Stack and Runtime Context
- Language: `C#`
- Project type: `tModLoader` mod project via `Mozandifiers.csproj`
- Framework/runtime evidence:
  - `Mozandifiers.csproj` imports `..\tModLoader.targets`
  - `AGENTS.md` explicitly identifies Terraria `tModLoader 1.4.4`
- Solution: single-project solution in `Mozandifiers.sln`
- Dependency model:
  - no NuGet package declarations in `Mozandifiers.csproj`
  - relies on Terraria/tModLoader APIs
- Config:
  - server-side mod config via `Common/Config/PrefixTuningConfig.cs`
- Content organization:
  - `Content/Prefixes/Weapons`: per-prefix definitions
  - `Content/Prefixes/Common`: shared prefix architecture and runtime logic
  - `Common/Players`: player-local timers/state/caps
  - `Common/Combat`: reusable combat helpers
  - `Localization/en-US_Mods.Mozandifiers.hjson`: localization and tooltip text
  - `Content/Pets`: Amelia/Ludovica vanity pets
  - `icon.png` / `icon_small.png`: mod icons
  - Aseprite source files exist under pet content folders
- Asset model:
  - visuals are implemented with Terraria-native `Dust`, `Lighting`, `CombatText`, and `PreDraw`
  - no custom sound assets were found; sound uses `SoundID.*`
  - no shader system or custom render pipeline was found

## 3. Repository Map
Focused tree of the files relevant to Mozandifiers:

```text
AGENTS.md                                      Project-specific coding and architecture rules
Mozandifiers.sln                               Single-project Visual Studio solution
Mozandifiers.csproj                            tModLoader mod project file
Mozandifiers.cs                                Mod entry point and Shifting sync packet handling
build.txt                                      Mod metadata (display name, author, version)
description.txt                                Short local description text
description_workshop.txt                       Workshop-facing description text
Localization/
  en-US_Mods.Mozandifiers.hjson                Prefix, config, item, buff, and pet localization
Common/
  Config/
    PrefixTuningConfig.cs                      Server-side tuning config for Echo chance and Breaching size source
  Combat/
    StormforgedChainHelper.cs                  Stormforged chain targeting and chain VFX
  Players/
    BreachingFeedbackPlayer.cs                 Breaching visual cooldown gate
    HeadshotFeedbackPlayer.cs                  Headshot visual/sound cooldown gate
    SanguineHealPlayer.cs                      Sanguine heal-per-second cap
    ShiftingPlayer.cs                          Shifting simulation state machine and sync logic
    SiphoningPlayer.cs                         Siphoning mana-per-second cap
    SpinboundPlayer.cs                         Spinbound cadence and queued shot context tracking
    StormforgedPlayer.cs                       Chain recursion suppression flag
    TemporalFeedbackPlayer.cs                  Temporal use-pulse cooldown gate
Content/
  Prefixes/
    Accessories/.gitkeep                       Placeholder folder; no accessory prefixes implemented
    Armors/.gitkeep                            Placeholder folder; no armor prefixes implemented
    Common/
      BasePrefix.cs                            Minimal base ModPrefix behavior
      WeaponPrefix.cs                          Shared weapon-prefix helpers and compatibility tests
      WeaponPrefixGlobalItem.cs                Item-side modifier behavior and impact helpers
      WeaponPrefixGlobalProjectile.cs          Projectile-side modifier logic, VFX/SFX, sync, and hit handling
      WeaponPrefixGlobalNPC.cs                 NPC-persistent Catalytic mark state and VFX
      WeaponPrefixVisuals.cs                   Shared colors, dust ids, and pulse helpers
      ShiftingSimulation.cs                    Shifting simulation pools, display names, and shifted stat math
    Weapons/
      AttunedPrefix.cs                         Magic stat package
      BreachingPrefix.cs                       Siege / bunker-buster heavy anti-armor modifier
      CatalyticPrefix.cs                       Magic mark-and-consume setup/payoff modifier
      DesperatePrefix.cs                       Low-life damage scaler
      EchoingPrefix.cs                         Echo-shot modifier
      HeadshotPrefix.cs                        Long-range ranged precision modifier
      RadiantPrefix.cs                         Fire/light modifier
      SanguinePrefix.cs                        Life-steal modifier
      ShiftingPrefix.cs                        Rotating simulated-prefix modifier
      SiphoningPrefix.cs                       Mana-return modifier
      SkirmishingPrefix.cs                     Fast ranged skirmish stat package
      SpinboundPrefix.cs                       Fibonacci cadence / Golden Spin ranged modifier
      StormforgedPrefix.cs                     Chain-lightning modifier
      TemporalPrefix.cs                        Time-distortion speed modifier
  Pets/
    Amelia/*                                   Puppy-clone vanity pet content
    Ludovica/*                                 Puppy-clone vanity pet content
```

Notable absences:
- No `README.md`, `DESIGN.md`, or `TODO.md` files were found.
- `Content/Projectiles/Weapons` is currently empty.

## 4. Core Architecture
### Prefix definition model
All weapon modifiers derive from:
- `BasePrefix` -> common roll chance and value multiplier behavior
- `WeaponPrefix` -> weapon-specific helpers, armor-penetration tooltip support, and compatibility helpers

Each prefix class in `Content/Prefixes/Weapons` typically defines:
- `PrefixCategory`
- `CanRoll(Item item)`
- `SetStats(...)`
- optional extra tooltip lines via `GetExtraTooltipLines(Item item)`
- optional constants for gameplay tuning

There is no explicit registration file; prefixes are discovered by tModLoader through the `ModPrefix` classes.

### Shared runtime flow
High-level data flow:

```text
Item with prefix
  -> WeaponPrefix.Apply / SetStats affects base item stats
  -> WeaponPrefixGlobalItem handles item-use and direct-hit behavior
  -> if projectile is spawned:
       WeaponPrefixGlobalProjectile tags projectile on OnSpawn
       projectile AI / PreDraw / OnHitNPC / ModifyHitNPC apply runtime behavior
  -> if target-persistent state is needed:
       WeaponPrefixGlobalNPC stores per-player NPC state
  -> if owner-local caps/timers/state are needed:
       ModPlayer classes in Common/Players store them
```

### Key architectural roles
- `BasePrefix.cs`
  - minimal common behavior for roll chance and value modification
- `WeaponPrefix.cs`
  - shared weapon helpers:
    - `IsStandardWeapon`
    - `IsMagicWeapon`
    - `IsRangedWeapon`
    - `IsProjectileWeapon`
    - melee/ranged/magic compatibility helpers
    - Echoing compatibility taxonomy
- `WeaponPrefixGlobalItem.cs`
  - direct-hit effects
  - held-item light
  - use-animation feedback
  - shifted stat projections
  - Breaching impact effect
  - sustain helpers for Sanguine and Siphoning
- `WeaponPrefixGlobalProjectile.cs`
  - projectile tagging from item source
  - parent-projectile inheritance for selected behaviors
  - per-projectile state flags
  - projectile visuals in `AI`
  - custom `PreDraw` for Echoing and Temporal
  - hit modification and on-hit feedback
  - `SendExtraAI` / `ReceiveExtraAI` sync for many modifier states
- `WeaponPrefixGlobalNPC.cs`
  - Catalytic mark lifetime and arm delay per player per NPC
- `ShiftingSimulation.cs`
  - determines which simulated modifier Shifting can become for each weapon mode
  - contains shifted-stat scaling methods
- `ShiftingPlayer.cs`
  - manages active simulation, rotation timing, combat window, sync, aura, and combat text

### Lifecycle / update patterns
- `UseAnimation` in `WeaponPrefixGlobalItem`:
  - Shifting weapon-use registration
  - Temporal use pulse
- `HoldItem` in `WeaponPrefixGlobalItem`:
  - Radiant and Temporal held-light pulses
- `ModifyShootStats` in `WeaponPrefixGlobalItem`:
  - Spinbound shot-context capture and shifted shoot-speed adjustments
- `OnSpawn` in `WeaponPrefixGlobalProjectile`:
  - direct item-source projectile tagging
  - headshot spawn position snapshot
  - Breaching impact scaling snapshot
  - Spinbound cadence consumption and Golden Spin evaluation
  - shifted simulation snapshot initialization
- `AI` in `WeaponPrefixGlobalProjectile`:
  - Radiant, Temporal, Echoing, and Spinbound visuals
- `ModifyHitNPC` in `WeaponPrefixGlobalProjectile`:
  - pre-hit damage/crit/armor-penetration math for Headshot, Spinbound, Catalytic, and shifted variants
- `OnHitNPC` in item/projectile globals:
  - sustain effects
  - status effects
  - breach/heavy hit feedback
  - Headshot feedback
  - Stormforged chain proc
  - Catalytic apply/consume

## 5. Modifier Catalogue
### Attuned
- Status: implemented
- Gameplay fantasy: magic efficiency / tuned spellcasting
- Affected weapon categories: magic weapons via `IsMagicWeapon`
- Main mechanics:
  - lower damage
  - lower knockback
  - faster use speed
  - higher projectile speed
  - lower mana cost
- Visual/audio behavior: none found
- Key files:
  - `Content/Prefixes/Weapons/AttunedPrefix.cs`
  - shifted support in `Content/Prefixes/Common/ShiftingSimulation.cs`
- Dependencies or related systems:
  - Shifting can simulate Attuned for magic
- Known issues, quirks, or limitations:
  - no tooltip beyond display name
  - no distinct feedback
- Design observations:
  - Reads as a pure stat package rather than a finished modifier identity.

### Breaching
- Status: implemented
- Gameplay fantasy: siege / bunker-buster / heavy anti-armor impact
- Affected weapon categories:
  - heavy swing melee
  - heavy projectile melee
  - slower heavy ranged projectile weapons
  - launchers
- Main mechanics:
  - `+12` armor penetration
  - `+20%` damage
  - `+18%` knockback
  - slower use speed
  - reduced crit
  - qualifying heavy/resistant hits can trigger breach feedback
  - heavy swing melee inherits a merged size bonus using legacy Colossal tuning config
- Visual/audio behavior:
  - debris/stone burst
  - smoke burst
  - streak effect
  - light flash
  - no sound currently; sound cue was intentionally removed
- Key files:
  - `Content/Prefixes/Weapons/BreachingPrefix.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalItem.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`
  - `Common/Players/BreachingFeedbackPlayer.cs`
  - `Localization/en-US_Mods.Mozandifiers.hjson`
- Dependencies or related systems:
  - `PrefixTuningConfig.ColossalScaleMultiplier` is reused as the size source
  - Shifting can simulate Breaching
- Known issues, quirks, or limitations:
  - config keys still use legacy Colossal naming
  - feedback is intentionally reduced and soundless at present
- Design observations:
  - The Colossal fantasy was partially absorbed here; heavy-size support is melee-only.

### Catalytic
- Status: implemented
- Gameplay fantasy: magic mark-and-consume combo setup
- Affected weapon categories: standard magic projectile weapons
- Main mechanics:
  - marks targets
  - mark arms after a short delay
  - next eligible hit consumes the mark for bonus damage
  - shifted variant extends duration and bonus
- Visual/audio behavior:
  - apply pulse
  - persistent pulsing mark aura around NPC
  - consume burst
  - no sound found
- Key files:
  - `Content/Prefixes/Weapons/CatalyticPrefix.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalNPC.cs`
  - `Content/Prefixes/Common/WeaponPrefixVisuals.cs`
- Dependencies or related systems:
  - NPC-persistent state via `WeaponPrefixGlobalNPC`
  - Shifting can simulate Catalytic
- Known issues, quirks, or limitations:
  - `IsCatalyticEligibleProjectile` currently only checks `projectile.active`
  - `pendingCatalyticConsume` state in `WeaponPrefixGlobalProjectile` is fragile by design because consume sequencing depends on projectile hit flow
- Design observations:
  - The code clearly favors consistency across magic projectile families over restrictive eligibility.

### Desperate
- Status: implemented
- Gameplay fantasy: stronger while close to death
- Affected weapon categories: standard melee/ranged/magic combat weapons
- Main mechanics:
  - static damage penalty
  - static crit bonus
  - additional dynamic damage multiplier based on missing life
- Visual/audio behavior: none found
- Key files:
  - `Content/Prefixes/Weapons/DesperatePrefix.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalItem.cs`
  - shifted support in `Content/Prefixes/Common/ShiftingSimulation.cs`
- Dependencies or related systems:
  - Shifting can simulate Desperate
- Known issues, quirks, or limitations:
  - low-life power state has no feedback
- Design observations:
  - Functional, but invisible in play.

### Echoing
- Status: implemented, with partial melee taxonomy
- Gameplay fantasy: duplicate / ghost echo follow-up attack
- Affected weapon categories:
  - ranged projectile
  - magic projectile
  - melee thrust
  - melee boomerang
- Main mechanics:
  - chance to spawn an echoed copy of an attack
  - echoed shot deals reduced damage
  - echo projectile preserves source immunity settings by normalization
- Visual/audio behavior:
  - blue ghost tint
  - blue torch dust
  - echo spawn burst
  - custom `PreDraw` underlay/afterimage
  - no sound found
- Key files:
  - `Content/Prefixes/Weapons/EchoingPrefix.cs`
  - `Content/Prefixes/Common/WeaponPrefix.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`
  - `Content/Prefixes/Common/WeaponPrefixVisuals.cs`
  - `Common/Config/PrefixTuningConfig.cs`
- Dependencies or related systems:
  - Echo chance is server-configurable
  - Shifting can simulate Echoing
- Known issues, quirks, or limitations:
  - weapon-mode taxonomy includes several unsupported melee special cases
  - no sound feedback
- Design observations:
  - The implementation is strongest on projectile paths. The code suggests broader melee ambitions than what is live.

### Headshot
- Status: implemented
- Gameplay fantasy: long-range precision ranged hits
- Affected weapon categories: ranged projectile weapons
- Main mechanics:
  - slower fire rate
  - more crit
  - damage scales with travel distance up to a cap
  - crit damage improves, with a stronger bonus after a long-range threshold
- Visual/audio behavior:
  - normal hit: restrained blue precision burst
  - long-range hit: stronger impact burst and a sound cue
  - cooldown-gated to control spam
- Key files:
  - `Content/Prefixes/Weapons/HeadshotPrefix.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`
  - `Common/Players/HeadshotFeedbackPlayer.cs`
  - `Content/Prefixes/Common/WeaponPrefixVisuals.cs`
- Dependencies or related systems:
  - Shifting can simulate Headshot
- Known issues, quirks, or limitations:
  - relies on projectile travel distance from spawn position, so unusual spawn logic could affect feel
- Design observations:
  - This is now a clear precision modifier with differentiated long-range confirmation.

### Radiant
- Status: implemented
- Gameplay fantasy: burning, luminous impact
- Affected weapon categories: standard direct combat weapons and shifted equivalents
- Main mechanics:
  - applies `On Fire!`
  - slight stat tradeoffs
- Visual/audio behavior:
  - held light pulse
  - projectile light pulse and ember dust
  - hit burst
  - no sound found
- Key files:
  - `Content/Prefixes/Weapons/RadiantPrefix.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalItem.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`
  - `Content/Prefixes/Common/WeaponPrefixVisuals.cs`
- Dependencies or related systems:
  - Shifting can simulate Radiant
- Known issues, quirks, or limitations:
  - no audio cue
- Design observations:
  - Readable and fairly complete for a lightweight modifier.

### Sanguine
- Status: implemented
- Gameplay fantasy: life steal / blood sustain
- Affected weapon categories: standard melee, ranged, and magic weapons
- Main mechanics:
  - heal for a portion of damage dealt
  - heal-per-second cap enforced in `SanguineHealPlayer`
- Visual/audio behavior:
  - uses vanilla heal feedback only
  - no custom Sanguine visual or sound found
- Key files:
  - `Content/Prefixes/Weapons/SanguinePrefix.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalItem.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`
  - `Common/Players/SanguineHealPlayer.cs`
- Dependencies or related systems:
  - Shifting can simulate Sanguine
- Known issues, quirks, or limitations:
  - weak player-facing identity compared with higher-polish modifiers
- Design observations:
  - Mechanically clear, aesthetically thin.

### Shifting
- Status: implemented
- Gameplay fantasy: unstable weapon that rewrites itself into temporary stronger simulated modifiers
- Affected weapon categories:
  - melee swing
  - ranged projectile
  - magic projectile
- Main mechanics:
  - while in combat, periodically selects a simulated modifier from a mode-specific pool
  - simulated modifiers apply at `1.5x` strength
  - state expires out of combat
  - synced from server to owner via mod packet
- Visual/audio behavior:
  - combat text announcing current simulation
  - aura around the player/weapon hand
  - reroll burst
  - no sound found
- Key files:
  - `Content/Prefixes/Weapons/ShiftingPrefix.cs`
  - `Content/Prefixes/Common/ShiftingSimulation.cs`
  - `Common/Players/ShiftingPlayer.cs`
  - `Mozandifiers.cs`
- Dependencies or related systems:
  - almost every other modifier via simulation catalog
- Known issues, quirks, or limitations:
  - simulation ids and stats are hardcoded in multiple places
  - retains a legacy reserved `Colossal` enum slot
- Design observations:
  - This is a central systems modifier and one of the most architecturally significant pieces in the repo.

### Siphoning
- Status: implemented
- Gameplay fantasy: mana return on hit for magic projectile weapons
- Affected weapon categories: standard magic projectile weapons
- Main mechanics:
  - mana restore derived from item mana cost
  - per-hit cap
  - per-second cap
  - item-side and projectile-side restore helpers
- Visual/audio behavior:
  - relies on vanilla `ManaEffect`
  - no custom SFX or VFX found
- Key files:
  - `Content/Prefixes/Weapons/SiphoningPrefix.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalItem.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`
  - `Common/Players/SiphoningPlayer.cs`
- Dependencies or related systems:
  - Shifting can simulate Siphoning
- Known issues, quirks, or limitations:
  - no bespoke feedback
- Design observations:
  - The sustain math is explicit and capped, but the modifier has little identity beyond utility.

### Skirmishing
- Status: implemented
- Gameplay fantasy: faster ranged pressure / mobile fighting
- Affected weapon categories: ranged projectile weapons
- Main mechanics:
  - faster use speed
  - faster shoot speed
  - damage / knockback tradeoff
  - crit bonus
- Visual/audio behavior: none found
- Key files:
  - `Content/Prefixes/Weapons/SkirmishingPrefix.cs`
  - shifted support in `Content/Prefixes/Common/ShiftingSimulation.cs`
- Dependencies or related systems:
  - Shifting can simulate Skirmishing
- Known issues, quirks, or limitations:
  - no tooltip-specific lines beyond display name
  - no feedback
- Design observations:
  - Currently a pure ranged stat package.

### Spinbound
- Status: implemented, with active debug output
- Gameplay fantasy: Fibonacci cadence and geometric “golden” release precision
- Affected weapon categories: ranged projectile weapons
- Main mechanics:
  - Fibonacci shot-gap cadence controls empowered releases
  - empowered shot gets damage/crit bonuses
  - Golden Spin tier is derived from shot velocity ratio, angle, player motion ratio, and precision-release conditions
  - static armor penetration bonus
  - stabilization nudges projectile speed
- Visual/audio behavior:
  - tiered spawn sounds
  - tiered spawn flash
  - tiered gold trail, spiral, light, and release burst
  - compact `CombatText`
  - temporary `Main.NewText` debug dump
- Key files:
  - `Content/Prefixes/Weapons/SpinboundPrefix.cs`
  - `Common/Players/SpinboundPlayer.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalItem.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`
- Dependencies or related systems:
  - Shifting can simulate Spinbound for ranged weapons
- Known issues, quirks, or limitations:
  - debug chat text is still active
  - cadence and shot-context pairing are sensitive to spawn order by design
- Design observations:
  - This is currently the strongest polish benchmark in the repo, but it still contains temporary tuning instrumentation.

### Stormforged
- Status: implemented
- Gameplay fantasy: lightning chain proc
- Affected weapon categories:
  - swinging melee
  - standard projectile combat weapons
- Main mechanics:
  - chance to chain damage to nearby enemies
  - limited jumps
  - damage decays per jump
  - recursion suppression via `StormforgedPlayer.SuppressStormforgedChain`
- Visual/audio behavior:
  - lightning zig-zag dust path
  - burst at source and chained targets
  - no sound found
- Key files:
  - `Content/Prefixes/Weapons/StormforgedPrefix.cs`
  - `Common/Combat/StormforgedChainHelper.cs`
  - `Common/Players/StormforgedPlayer.cs`
  - item/projectile hook integration in shared globals
- Dependencies or related systems:
  - Shifting can simulate Stormforged
- Known issues, quirks, or limitations:
  - no sound cue
  - code still has minor comment/format residue in `StormforgedChainHelper.cs`
- Design observations:
  - Mechanically and visually readable, though still lighter than Spinbound/Headshot in polish.

### Temporal
- Status: implemented
- Gameplay fantasy: time distortion affecting attack cadence and projectile travel
- Affected weapon categories: all standard weapons via `IsStandardWeapon`
- Main mechanics:
  - doubled attack speed
  - projectile weapons also get doubled projectile speed
  - damage penalty to offset speed
- Visual/audio behavior:
  - held light pulse
  - use pulse
  - projectile light pulse and sapphire dust
  - subtle afterimage in `PreDraw`
  - no sound found
- Key files:
  - `Content/Prefixes/Weapons/TemporalPrefix.cs`
  - `Common/Players/TemporalFeedbackPlayer.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalItem.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`
  - `Content/Prefixes/Common/WeaponPrefixVisuals.cs`
- Dependencies or related systems:
  - Shifting can simulate Temporal
- Known issues, quirks, or limitations:
  - projectile-speed component matters only for projectile weapons; tooltip now states that explicitly
- Design observations:
  - Now coherent as a time-distortion modifier rather than a vague speed bundle.

### Colossal
- Status: referenced / legacy-reserved
- Gameplay fantasy: legacy oversized heavy-weapon identity
- Affected weapon categories:
  - no live rollable prefix remains
  - remnants only persist as compatibility/config residue
- Main mechanics:
  - merged into Breaching for heavy swing weapon size scaling
- Visual/audio behavior: none live
- Key files:
  - `Content/Prefixes/Common/ShiftingSimulation.cs` (reserved enum slot)
  - `Common/Config/PrefixTuningConfig.cs` (`ColossalSizePercent`, `ColossalScaleMultiplier`)
  - `Localization/en-US_Mods.Mozandifiers.hjson` (config relabeling)
- Dependencies or related systems:
  - Breaching uses its former scale config source
- Known issues, quirks, or limitations:
  - legacy naming remains
  - not live as a separate modifier
- Design observations:
  - Inference: retained mainly to avoid breaking compatibility or config expectations.

## 6. Weapon-Type Behaviour Matrix
### By weapon family

| Weapon family | System behavior | Compatibility notes | Known fragility |
|---|---|---|---|
| Melee swing | Primarily item-side hit handling in `WeaponPrefixGlobalItem`; can also use Shifting melee pool | Supports Breaching, Stormforged, Radiant, Sanguine, Desperate, Temporal, Shifting; Breaching merged size applies here | Large effect differences vs projectile weapons because no projectile-side visuals unless the modifier also has item-side feedback |
| Ranged projectile | Strongest support path; projectile tagging in `OnSpawn`, `ModifyHitNPC`, `OnHitNPC`, `AI`, and `PreDraw` | Supports Headshot, Spinbound, Echoing, Skirmishing, Breaching, Stormforged, Temporal, Radiant, Sanguine, Desperate, Shifting | Rapid-fire spam risk must be manually controlled per modifier |
| Magic projectile | Strong projectile path; especially important for Attuned, Siphoning, Catalytic, Echoing, Temporal | Standard magic projectile weapons are the main supported magic path | Channelled magic is often excluded by `!item.channel` gates |
| Summon | Mostly excluded | `WeaponPrefix.IsStandardCombatWeapon` and individual prefix roll gates avoid summon and summon melee speed in many places | No summon-specific modifier support found |
| Boomerangs | Treated as `EchoingWeaponMode.MeleeBoomerang` for Echoing | Echoing explicitly supports boomerang melee projectile echo mode | Other modifiers treat them according to general projectile/melee gates; behavior may vary by item stats |
| Spears / thrust melee | Treated as `EchoingWeaponMode.MeleeThrust` for Echoing | Echoing explicitly supports melee thrust | Headshot/Spinbound/etc. do not target this path |
| Slash/projectile melee | Projectile melee is recognized by `IsProjectileMeleeWeapon` | Breaching can support heavy projectile melee if knockback is high enough | Echoing taxonomy names more melee special cases than are implemented |
| Yoyos / flails / held projectiles | Classified in `GetProjectileMeleeEchoMode` but not implemented for Echoing | No dedicated live modifier systems target these as a first-class path | Clear area of incomplete or intentionally deferred support |
| Channelled / held projectiles | Often excluded via `!item.channel` in compatibility helpers | Temporal can roll broadly; many projectile-special modifiers exclude channelled weapons | Behavior is intentionally constrained rather than fully supported |

### Spawn / damage / compatibility differences
- Direct-hit melee weapons rely on `WeaponPrefixGlobalItem.OnHitNPC`.
- Projectile weapons rely on `WeaponPrefixGlobalProjectile.OnSpawn`, `ModifyHitNPC`, `OnHitNPC`, `AI`, and `PreDraw`.
- Some actual prefix behaviors inherit from a parent projectile:
  - `Sanguine`
  - `Radiant`
  - `Stormforged`
  - `Headshot`
- Shifted inheritance is even narrower via `CanInheritShiftedSimulation(...)`:
  - `Sanguine`
  - `Radiant`
  - `Stormforged`
  - `Siphoning`
  - `Headshot`

## 7. Projectile and Combat Interaction Patterns
### Spawned projectiles
- Direct item-source spawn tagging happens in `WeaponPrefixGlobalProjectile.OnSpawn`.
- The file checks `IEntitySource_WithStatsFromItem` to detect item-origin projectiles.
- Several modifier flags are stamped onto the projectile at spawn:
  - `IsSanguineProjectile`
  - `IsRadiantProjectile`
  - `IsBreachingProjectile`
  - `IsTemporalProjectile`
  - `IsStormforgedProjectile`
  - `IsSiphoningProjectile`
  - `IsCatalyticProjectile`
  - `IsHeadshotProjectile`
  - `IsSpinboundProjectile`
- Some projectile families inherit flags from parent projectiles through `EntitySource_Parent`.

### Duplicate / echo projectiles
- Echoing duplicates are created in `TrySpawnEchoProjectile(...)`.
- The echoed projectile:
  - uses the same projectile type
  - gets a small random velocity rotation
  - is flagged as `IsSpawnedEchoProjectile`, `IsEchoDamageProjectile`, and `IsEchoVisualProjectile`
  - uses normalized local NPC immunity
- Echoes are damage-reduced via `EchoDamageMultiplier`.

### Hit immunity handling
- Echoing explicitly normalizes source and echo hit immunity in `NormalizeEchoHitTracking(...)`:
  - disables ID static NPC immunity
  - enables local NPC immunity
  - reuses source cooldown when available
- This is one of the clearest deliberate projectile-fragility mitigations in the repo.

### NPC hit tracking / target-persistent state
- Catalytic is the only modifier using `GlobalNPC` persistent state.
- `WeaponPrefixGlobalNPC` stores:
  - expire tick per player
  - arm-after tick per player
  - separate actual and shifted arrays

### Projectile ownership
- Many feedback and effect paths are owner-local:
  - `player.whoAmI == Main.myPlayer`
  - `projectile.owner == Main.myPlayer`
- This is used to prevent duplicate VFX/SFX spam in multiplayer.
- `Shifting` state sync is handled server -> owner through `Mozandifiers.SendShiftingState(...)`.

### Delayed spawns / child projectiles
- Parent inheritance exists, but only for a narrow set of supported modifiers.
- `CanInheritShiftedSimulation(...)` prevents broad inheritance of all shifted simulations.
- Inference: the repo intentionally avoids blanket child-projectile propagation because projectile families vary widely.

### Scaling / orientation / rotation
- Headshot scales damage by projectile travel distance from stored spawn position.
- Spinbound stores reference speed and computes Golden Spin tier from velocity geometry and player movement.
- Temporal and Echoing use `PreDraw` overlays/afterimages based on current projectile velocity.
- Breaching impact size is scaled by `GetBreachImpactMultiplier(item)`.

### Trails, glow, draw layers
- No custom shader stack was found.
- Common visual tools are:
  - `Lighting.AddLight(...)`
  - `Dust.NewDustPerfect(...)`
  - `CombatText.NewText(...)`
  - custom `PreDraw(...)` underlays for Echoing and Temporal
- `PreDraw` is used selectively, not as a general system.

### Collision / hitbox caveats
- Headshot feedback depends on where the projectile was spawned vs where it hit.
- Spinbound cadence depends on shot context queuing in `SpinboundPlayer`.
- Catalytic consume sequencing depends on `pendingCatalyticConsume` and hit order in `ModifyHitNPC` + `OnHitNPC`.

## 8. Visual, Audio, and Feedback Patterns
### Readability patterns
- The repo favors restrained, localized feedback rather than persistent full-screen effects.
- Most modifiers use:
  - small dust bursts
  - localized light pulses
  - occasional combat text
- Heavier feedback is cooldown-gated through `ModPlayer` helper classes.

### Impact fantasy handling
- Armor break / heavy-hit fantasy:
  - handled by Breaching via stone/smoke/streak burst in `WeaponPrefixGlobalItem.SpawnBreachingImpactEffect(...)`
- Ghost/echo fantasy:
  - handled by Echoing via tint, afterimage, orbit dust, and echo spawn burst
- Time distortion:
  - handled by Temporal via pulse lighting, sapphire dust, and subtle projectile afterimage
- Precision:
  - handled by Headshot via small precision impact burst, stronger long-range burst, and one sound cue
- Rotational release mastery:
  - handled by Spinbound via tiered gold VFX/SFX and current debug text

### Dust / lighting / draw patterns
- Shared color/dust constants live in `WeaponPrefixVisuals.cs`.
- Lighting is used as the main low-cost visual anchor for:
  - Radiant
  - Breaching
  - Temporal
  - Catalytic
  - Stormforged
  - Spinbound
- `PreDraw` is used only for:
  - Echoing ghost underlays
  - Temporal afterimage underlay

### Sound patterns
- Sound usage is sparse.
- Active sound cues found in the repo:
  - Headshot long-range hit: `SoundID.Item153`
  - Spinbound true shot: `SoundID.Item42`
  - higher Spinbound tiers: `SoundID.Item20`, `SoundID.Item113`
- Breaching sound was intentionally removed.
- No sound was found for:
  - Echoing
  - Radiant
  - Stormforged
  - Catalytic
  - Sanguine
  - Siphoning
  - Temporal
  - Attuned
  - Skirmishing
  - Desperate

### Target reaction
- Radiant directly applies `BuffID.OnFire`.
- Catalytic stores target state and shows persistent target aura.
- Most other modifiers are hit-feedback-only rather than target-state-heavy.

## 9. Design Intent Extracted from the Repo
### Explicit intent
- `AGENTS.md` explicitly defines the architecture:
  - prefix stats in prefix classes
  - item-side behavior in `WeaponPrefixGlobalItem`
  - projectile-side behavior in `WeaponPrefixGlobalProjectile`
  - NPC state in `WeaponPrefixGlobalNPC`
  - player-local caps/timers in `Common/Players`
- `description_workshop.txt` explicitly says the project is focused on weapon prefixes and bonus pets, not accessory or armor prefixes.
- Prefix and tooltip names clearly express intended fantasies:
  - `Headshot`
  - `Stormforged`
  - `Spinbound`
  - `Catalytic`
  - `Breaching`
  - `Temporal`

### Inferred intent
- Inference: the project is deliberately moving modifiers away from hidden stat bundles and toward readable gameplay identities. Evidence:
  - recent readable VFX/SFX work for Spinbound, Headshot, Temporal, and Breaching
  - more explicit tooltips in localization
- Inference: support is intentionally conservative for unusual weapon families. Evidence:
  - `Ask First` rules in `AGENTS.md`
  - many `CanRoll` gates excluding channelled or unsupported weapon types
- Inference: multiplayer duplication avoidance is a design priority. Evidence:
  - repeated owner-local gates before VFX/SFX and sustain application
- Inference: `Shifting` is intended as a higher-complexity showcase modifier. Evidence:
  - its dedicated packet sync, aura, combat text, and simulation catalog

## 10. Current Problems and Fragile Areas
- **Spinbound still ships with temporary debug chat output**
  - What is happening: `Main.NewText(BuildGoldenSpinDebugText(goldenSpin), Color.Gold);` is still live.
  - Likely cause: tuning instrumentation was left enabled.
  - Impacted files: `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`
  - Confidence level: High

- **Workshop version text is stale**
  - What is happening: `description_workshop.txt` still says version `0.2`, while `build.txt` says `0.3`.
  - Likely cause: metadata update was partial.
  - Impacted files: `description_workshop.txt`, `build.txt`
  - Confidence level: High

- **Echoing taxonomy is broader than live support**
  - What is happening: `WeaponPrefix.cs` defines many `EchoingWeaponMode` values, but only a subset is treated as implemented.
  - Likely cause: planned or formerly explored support was narrowed without collapsing the taxonomy.
  - Impacted files: `Content/Prefixes/Common/WeaponPrefix.cs`
  - Confidence level: High

- **Legacy Colossal residue remains**
  - What is happening: `ShiftingSimulationId.Colossal` and config names `ColossalSizePercent` / `ColossalScaleMultiplier` still exist.
  - Likely cause: backward compatibility / migration avoidance after Breaching absorbed Colossal’s size identity.
  - Impacted files: `Content/Prefixes/Common/ShiftingSimulation.cs`, `Common/Config/PrefixTuningConfig.cs`, `Localization/en-US_Mods.Mozandifiers.hjson`
  - Confidence level: High

- **`WeaponPrefixGlobalProjectile.cs` is a maintainability hotspot**
  - What is happening: one file owns a very large share of projectile flags, sync, visuals, on-hit logic, and temporary debug behavior.
  - Likely cause: centralized shared-projectile architecture plus iterative feature growth.
  - Impacted files: `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`
  - Confidence level: High

- **Projectile-family compatibility remains fragile by design**
  - What is happening: many behaviors depend on direct item-source spawn tagging, parent inheritance rules, and explicit compatibility gates.
  - Likely cause: Terraria projectile families differ widely in spawn/hit behavior.
  - Impacted files: `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`, `Content/Prefixes/Common/WeaponPrefix.cs`, individual prefix `CanRoll(...)` methods
  - Confidence level: High

- **Breaching still carries legacy config semantics**
  - What is happening: player-facing labels were updated, but the config fields remain Colossal-named.
  - Likely cause: config compatibility preservation.
  - Impacted files: `Common/Config/PrefixTuningConfig.cs`, `Localization/en-US_Mods.Mozandifiers.hjson`
  - Confidence level: High

- **Modifier polish is uneven**
  - What is happening: some modifiers have full feedback loops (`Spinbound`, `Headshot`, `Temporal`, `Breaching`, `Catalytic`), while others are nearly stat-only (`Attuned`, `Skirmishing`, `Desperate`).
  - Likely cause: development focus favored a subset of modifiers first.
  - Impacted files: many, especially `Content/Prefixes/Weapons/*.cs`
  - Confidence level: High

- **Pet code still contains template residue**
  - What is happening: Amelia/Ludovica files retain ExampleMod-style comments and extra imports.
  - Likely cause: pet content was cloned from example content and lightly adapted.
  - Impacted files: `Content/Pets/Amelia/*`, `Content/Pets/Ludovica/*`
  - Confidence level: High

## 11. Extension Points for New Modifiers
Practical implementation path for adding a new modifier:

1. **Create the prefix class**
   - Location: `Content/Prefixes/Weapons/NewPrefix.cs`
   - Base type: `WeaponPrefix`
   - Implement:
     - `PrefixCategory`
     - `CanRoll(Item item)`
     - `SetStats(...)`
     - optional constants for tuning
     - optional tooltip lines via `GetExtraTooltipLines(Item item)`

2. **Add localization**
   - File: `Localization/en-US_Mods.Mozandifiers.hjson`
   - Add:
     - `DisplayName`
     - any tooltip keys referenced by the new prefix class

3. **Add runtime behavior in the correct layer**
   - Item-side direct-hit / held / use behavior:
     - `Content/Prefixes/Common/WeaponPrefixGlobalItem.cs`
   - Projectile tagging / projectile visuals / projectile hit logic:
     - `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`
   - NPC-persistent state:
     - `Content/Prefixes/Common/WeaponPrefixGlobalNPC.cs`
   - Player-local cap/timer/state:
     - new `Common/Players/*Player.cs`
   - Shared combat helper:
     - `Common/Combat/*.cs` if logic is reusable and self-contained

4. **If Shifting should simulate it**
   - Add it to `ShiftingSimulationId`
   - add display-name mapping in `ShiftingSimulationCatalog.GetDisplayName(...)`
   - add shifted stat behavior methods
   - add it to the relevant simulation pool(s)
   - update any shifted projectile/item behavior if needed

5. **If visuals are needed**
   - Add shared colors/dust ids/pulse helpers to `WeaponPrefixVisuals.cs` only if reuse is real
   - use small, readable, owner-local effects
   - prefer `Lighting`, `Dust`, and optional restrained `PreDraw`

6. **If audio is needed**
   - Use existing `SoundID.*`
   - gate spam with a `ModPlayer` cooldown helper if the effect can trigger rapidly

7. **Testing implications**
   - Check both item-side and projectile-side paths
   - check multiplayer ownership assumptions
   - check local/static immunity behavior for projectile duplication or multi-hit logic
   - check whether child projectiles should inherit the modifier or not
   - update tooltips/description text if the behavior is visible to players

Minimal pseudocode shape:

```text
NewPrefix : WeaponPrefix
  CanRoll(item) -> define supported weapon families
  SetStats(...) -> define base stat tradeoffs
  GetExtraTooltipLines(...) -> explain visible behavior

WeaponPrefixGlobalItem / Projectile / NPC / Player
  if item.prefix == NewPrefix or shifted simulation == NewPrefix:
      apply runtime behavior
      gate feedback as needed
      sync only what must cross multiplayer boundaries
```

## 12. Glossary of Project-Specific Terms
- **Prefix**: a Terraria weapon modifier implemented as a `ModPrefix`.
- **WeaponPrefix**: the project’s shared base class for weapon modifiers.
- **Shifting simulation**: the temporary simulated modifier that a `Shifting` weapon currently mimics.
- **Golden Spin**: Spinbound’s tiered precision-evaluation system for empowered shots.
- **Breach**: a qualifying Breaching impact against heavy/resistant targets that triggers siege-style feedback.
- **Echo**: a duplicated attack produced by Echoing.
- **Catalytic mark**: a target state applied by Catalytic that later arms and can be consumed.
- **Owner-local**: logic intentionally restricted to `Main.myPlayer` to avoid duplicated effects or state application.
- **Merged scale**: the heavy swing weapon size bonus now attached to Breaching using the former Colossal config source.

## 13. Open Questions / Missing Context
- Why was `ShiftingSimulationId.Colossal` intentionally retained instead of removed? The repo shows the residue, but not the explicit migration rationale.
- Should the Colossal-named config fields eventually be renamed, or are they intentionally frozen for compatibility?
- Which unsupported Echoing melee modes are intentionally deferred vs abandoned?
- Is the live `Main.NewText` Spinbound debug output meant to remain temporarily, or was it simply not cleaned up yet?
- Should Breaching eventually regain a sound cue, or is silent heavy impact the intended direction?
- There is no README or design note explaining the long-term modifier quality target beyond what can be inferred from recent changes.

## 14. Recommended Next Documentation Files
- `MODIFIER_GUIDE.md`
  - Why: a per-modifier implementation and balance guide would reduce repeated repository scanning for each new session.
- `COMBAT_PIPELINE.md`
  - Why: item -> projectile -> NPC -> player data flow is central to the project and currently spread across several files.
- `WEAPON_COMPATIBILITY.md`
  - Why: compatibility gates are one of the biggest sources of fragility; a maintained matrix would clarify what is intentionally supported.
- `SHIFTING_SIMULATIONS.md`
  - Why: `Shifting` is a cross-cutting system and deserves a dedicated document for pools, scaling, sync, and inheritance rules.
- `VFX_RULES.md`
  - Why: recent polish passes show a growing quality bar, but there is no repo-level guide for dust/light/PreDraw/sound restraint.
