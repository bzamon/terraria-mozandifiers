# MOZANDIFIERS CONTEXT

## 1. Project Overview
`Mozandifiers` is a Terraria `tModLoader 1.4.4` mod written in C#. The repository is centered on custom weapon prefixes and currently ships:
- 14 live rollable weapon prefixes
- 2 bonus pet items/projectiles (`Amelia`, `Ludovica`)
- 1 server-side tuning config for Fracture I/II/III chance and Breaching heavy-size scaling

Live rollable prefixes found in `Content/Prefixes/Weapons`:
- `Attuned`
- `Awakened`
- `Breaching`
- `Catalytic`
- `Desperate`
- `Fractured` (implemented by `EchoingPrefix`)
- `Deadeye`
- `Radiant`
- `Vampiric` (implemented by `SanguinePrefix`)
- `Shifting`
- `Skirmishing`
- `Spinbound`
- `Stormforged`
- `Temporal`

## 2. Technical Stack and Runtime Context
- Language: `C#`
- Project type: single-project `tModLoader` mod
- Runtime evidence:
  - `Mozandifiers.csproj` imports `..\tModLoader.targets`
  - `AGENTS.md` identifies Terraria `tModLoader 1.4.4`
- Solution file: `Mozandifiers.sln`
- Dependencies: no extra NuGet or third-party runtime dependencies are declared
- Networking:
  - explicit mod packets are handled in `Mozandifiers.cs`
  - current packet usage is for `ShiftingPlayer` sync
  - projectile-side state sync uses `SendExtraAI` / `ReceiveExtraAI`
- Config:
  - `Common/Config/PrefixTuningConfig.cs`
  - `ConfigScope.ServerSide`
- Asset/content structure:
  - gameplay code under `Common/*` and `Content/Prefixes/*`
  - localization under `Localization/en-US_Mods.Mozandifiers.hjson`
  - pet content under `Content/Pets/*`
- Feedback stack:
  - `Dust`
  - `Lighting`
  - selective `CombatText`
  - selective `PreDraw`
  - built-in `SoundID`
- No custom shaders or custom sound assets were found.

## 3. Repository Map
Focused map of the files that materially affect current modifier behavior.

```text
AGENTS.md
  Project-specific architecture, compatibility, and safety rules.

Mozandifiers.cs
  Mod entry point and packet handling for Shifting sync.

Mozandifiers.csproj
  Minimal tModLoader project file.

build.txt
  Build metadata; current version is 0.3.

description.txt
  Local/player-facing feature summary for the live roster.

description_workshop.txt
  Workshop-facing feature summary for the live roster.

Localization/en-US_Mods.Mozandifiers.hjson
  Prefix display names, tooltip text, config labels, pet text.

Common/Config/PrefixTuningConfig.cs
  Fracture cascade chance and Breaching size tuning, plus Echo compatibility aliases.

Common/Combat/AwakenedCombatHelper.cs
  Sword sweet-spot evaluation helper for Awakened.

Common/Combat/StormforgedChainHelper.cs
  Stormforged chain search, damage application, and recursion suppression.

Common/Players/
  AttunedPlayer.cs
    Resonance stacks, decay, empowered-cast window.
  AwakenedPlayer.cs
    Awakened state machine, meter, timers, visual gates.
  BreachingFeedbackPlayer.cs
    Breaching visual cooldown gate.
  DesperatePlayer.cs
    Threshold state, surge cooldown, direct-hit feedback queue.
  DeadeyePlayer.cs
    Ready-shot charge state, stationary early-arm tracking, and local audio/visual cooldowns.
  ShiftingPlayer.cs
    Active simulation state, combat timing, packet serialization.
  SkirmishingPlayer.cs
    Skirmish window and follow-up shot window.
  SpinboundPlayer.cs
    Fibonacci cadence state and queued shot contexts.
  StartingItemsPlayer.cs
    Starting item grant logic, not part of the prefix runtime.
  StormforgedPlayer.cs
    Recursive chain suppression flag.
  TemporalFeedbackPlayer.cs
    Temporal use-pulse cooldown.
  VampiricPlayer.cs
    Sustain caps, Frenzy timer/tier, local visual gates.

Content/Prefixes/Common/
  BasePrefix.cs
    Minimal shared prefix behavior.
  WeaponPrefix.cs
    Shared weapon classification helpers and Fractured compatibility rules.
  WeaponPrefixGlobalItem.cs
    Item-side runtime, direct-hit logic, held/use feedback, sustain helpers.
  WeaponPrefixGlobalProjectile.cs
    Projectile tagging, projectile-side hit logic, visuals, sync, debug residue.
  WeaponPrefixGlobalNPC.cs
    Target-persistent state for Catalytic, Vampiric, and Awakened boss cap tracking.
  WeaponPrefixVisuals.cs
    Shared colors, dust types, and pulse helpers.
  ShiftingSimulation.cs
    Shifting pools and shifted stat/effect helpers.

Content/Prefixes/Weapons/
  AttunedPrefix.cs
  AwakenedPrefix.cs
  BreachingPrefix.cs
  CatalyticPrefix.cs
  DesperatePrefix.cs
  EchoingPrefix.cs
    Player-facing Fractured identity.
  DeadeyePrefix.cs
  RadiantPrefix.cs
  SanguinePrefix.cs
    Player-facing Vampiric identity.
  ShiftingPrefix.cs
  SkirmishingPrefix.cs
  SpinboundPrefix.cs
  StormforgedPrefix.cs
  TemporalPrefix.cs

Content/Prefixes/Accessories/
  .gitkeep only.

Content/Prefixes/Armors/
  .gitkeep only.

Content/Projectiles/Weapons/
  Empty at current HEAD.
```

Notable absences:
- no `README.md`
- no `DESIGN.md`
- no `TODO.md`
- no live custom modifier projectile classes in `Content/Projectiles/Weapons`

## 4. Core Architecture
### How modifiers are defined
Every modifier is a `ModPrefix` through:
- `Content/Prefixes/Common/BasePrefix.cs`
- `Content/Prefixes/Common/WeaponPrefix.cs`

Per-prefix classes typically define:
- `PrefixCategory`
- `CanRoll(Item item)`
- `SetStats(...)`
- behavior constants
- optional extra tooltip lines through `GetExtraTooltipLines(...)`

There is no manual registry. Prefix discovery is via tModLoader content loading.

### Ownership model
- Prefix class:
  - baseline stats
  - tooltip-visible constants
  - roll compatibility
- `WeaponPrefixGlobalItem`:
  - item-side behavior
  - direct-hit behavior
  - held/use feedback
  - state machine application for item-only paths
- `WeaponPrefixGlobalProjectile`:
  - projectile spawn tagging
  - projectile-side hit math
  - projectile-side effects and consume/payoff logic
  - projectile visuals in `AI`
  - `PreDraw`
  - `SendExtraAI` / `ReceiveExtraAI`
- `WeaponPrefixGlobalNPC`:
  - target-persistent modifier state
  - currently used by `Catalytic`, `Vampiric`, and `Awakened`
- `Common/Players/*Player.cs`:
  - owner-local timers, caps, windows, and cooldown gates
- `Common/Combat/*`:
  - reusable combat helpers where a modifier needs standalone combat math

### Shared data flow

```text
Item with prefix
  -> prefix stats applied through SetStats / Apply
  -> item-side runtime in WeaponPrefixGlobalItem
  -> if projectile weapon:
       -> WeaponPrefixGlobalProjectile.OnSpawn tags state
       -> projectile visuals in AI / PreDraw
       -> projectile-side math in ModifyHitNPC
       -> projectile-side payoff in OnHitNPC
  -> target-persistent state, if needed, lives in WeaponPrefixGlobalNPC
  -> owner-local state, if needed, lives in a ModPlayer
```

### Important shared helpers in `WeaponPrefix.cs`
`WeaponPrefix.cs` is the main compatibility gate file. It provides:
- family checks such as `IsMagicWeapon(...)`, `IsRangedWeapon(...)`, `IsProjectileWeapon(...)`
- melee-path checks such as:
  - `IsProjectileMeleeWeapon(...)`
  - `IsSwingingMeleeWeapon(...)`
  - `IsStandardSwordWeapon(...)`
- standard support gates such as:
  - `IsStandardProjectileCombatWeapon(...)`
  - `IsStandardMagicProjectileWeapon(...)`
  - `IsStandardDirectCombatWeapon(...)`
  - `IsStandardCombatWeapon(...)`
- Fractured mode classification and implemented-mode filtering

### Important live hooks
- `UseAnimation`
  - `Shifting` combat registration
  - `Temporal` use pulse
- `HoldItem`
  - held-state feedback for `Awakened`, `Attuned`, `Desperate`, `Radiant`, `Skirmishing`, `Temporal`, `Vampiric`, `Shifting`
- `ModifyManaCost`
  - `Attuned` empowered-cast consumption
- `ModifyShootStats`
  - `Spinbound` cadence capture
  - `Attuned` empowered projectile speed
  - `Skirmishing` follow-up consumption
- `ModifyWeaponDamage`, `ModifyWeaponCrit`, `ModifyWeaponKnockback`, `ModifyItemScale`, `UseSpeedMultiplier`
  - baseline and dynamic item-side stats
- `ModifyHitNPC` in `WeaponPrefixGlobalItem`
  - `Awakened` sweet-spot bonus damage
  - direct-hit Frenzy crit for `Vampiric`
- `OnHitNPC` in `WeaponPrefixGlobalItem`
  - item-side sustain, setup/payoff triggers, and Awakened sweet-spot meter gain
- `OnSpawn` in `WeaponPrefixGlobalProjectile`
  - projectile flag stamping and shifted snapshot setup
- `ModifyHitNPC` in `WeaponPrefixGlobalProjectile`
  - projectile-side crit/damage/armor-penetration math
- `OnHitNPC` in `WeaponPrefixGlobalProjectile`
  - projectile sustain and projectile-side payoff
- `AI`
  - projectile-local VFX
- `PreDraw`
  - selected underlays / tracers / afterimages

### Main maintainability hotspot
`Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs` is still the largest coupling point. It owns:
- most projectile flags
- most projectile-side modifier math
- most projectile VFX paths
- projectile sync fields
- live Spinbound debug residue

## 5. Modifier Catalogue
### Attuned
- Status: implemented
- Gameplay fantasy: resonance casting that builds to a free empowered cast
- Affected weapon categories: mana-using magic weapons
- Main mechanics:
  - use speed / mana efficiency support
  - resonance from repeated casts or hits
  - full resonance empowers the next cast
  - empowered cast is free and improves projectile release quality
- Visual/audio behavior:
  - held resonance aura
  - empowered cast release flash
  - empowered projectile visuals
  - small empowered impact confirmation
  - empowered cast sound cue
- Key files:
  - `Content/Prefixes/Weapons/AttunedPrefix.cs`
  - `Common/Players/AttunedPlayer.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalItem.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`
- Dependencies or related systems:
  - shifted simulation support in `Content/Prefixes/Common/ShiftingSimulation.cs`
- Known issues / limitations:
  - non-projectile magic weapons can roll it, but the strongest runtime path is projectile-based
- Design observations:
  - one of the clearest cast-loop modifiers in the repo

### Awakened
- Status: implemented
- Gameplay fantasy: sword-only sweet-spot mastery that awakens into a stronger state, then locks out
- Affected weapon categories: standard swinging sword-like melee weapons
- Main mechanics:
  - three-state loop: dormant, awakened, recovery lockout
  - dormant sweet-spot hits build a meter
  - awakened lasts for a timed window
  - awakened sweet-spot hits add target-max-life-based bonus damage
  - bosses use a per-target rolling bonus-damage budget
- Visual/audio behavior:
  - held dormant buildup feedback
  - stronger awakened aura
  - awaken start burst
  - awakened sweet-spot strike feedback
- Key files:
  - `Content/Prefixes/Weapons/AwakenedPrefix.cs`
  - `Common/Players/AwakenedPlayer.cs`
  - `Common/Combat/AwakenedCombatHelper.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalItem.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalNPC.cs`
- Dependencies or related systems:
  - `AwakenedCombatHelper` line-based sweet-spot approximation
  - `WeaponPrefixGlobalNPC` rolling boss budget queue
- Known issues / limitations:
  - intentionally restricted to sword-like swing melee
  - no projectile path by design
- Design observations:
  - the cleanest item-only state machine currently in the project

### Breaching
- Status: implemented
- Gameplay fantasy: siege / bunker-buster force against armor and resistant targets
- Affected weapon categories:
  - heavy swing melee
  - heavy projectile melee
  - slow forceful ranged projectile weapons
  - launchers
- Main mechanics:
  - armor penetration
  - heavier damage and knockback
  - slower use speed and reduced crit
  - qualifying impacts trigger breach feedback
  - heavy swing melee receives the merged heavy-size bonus
- Visual/audio behavior:
  - restrained debris, smoke, and streak burst
  - no current breach sound
- Key files:
  - `Content/Prefixes/Weapons/BreachingPrefix.cs`
  - `Common/Players/BreachingFeedbackPlayer.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalItem.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`
- Dependencies or related systems:
  - `Common/Config/PrefixTuningConfig.cs`
  - shifted simulation support
- Known issues / limitations:
  - no current dedicated sound cue
- Design observations:
  - this is the live heavy-impact identity that absorbed old Colossal behavior

### Catalytic
- Status: implemented
- Gameplay fantasy: mark a target, wait for arm delay, then consume the mark for a follow-up hit
- Affected weapon categories: standard magic projectile weapons
- Main mechanics:
  - target mark
  - arm delay
  - consume for bonus damage
- Visual/audio behavior:
  - apply burst
  - persistent marked-target aura
  - consume burst
  - no sound cue
- Key files:
  - `Content/Prefixes/Weapons/CatalyticPrefix.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalNPC.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`
- Dependencies or related systems:
  - shifted simulation support
- Known issues / limitations:
  - projectile gating uses the standard magic projectile support path, but projectile-side helper checks remain intentionally broad
- Design observations:
  - the clearest setup/consume modifier in the repo

### Desperate
- Status: implemented
- Gameplay fantasy: low-life last stand with escalating thresholds and surge hits
- Affected weapon categories: standard melee, ranged, and magic combat weapons
- Main mechanics:
  - damage scales with missing life
  - threshold states at `50%`, `25%`, and `12.5%`
  - critical state unlocks a cooldown-gated surge hit
- Visual/audio behavior:
  - held threshold aura
  - stronger critical readiness pulse
  - surge impact feedback
  - no dedicated sound cue
- Key files:
  - `Content/Prefixes/Weapons/DesperatePrefix.cs`
  - `Common/Players/DesperatePlayer.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalItem.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`
- Dependencies or related systems:
  - shifted simulation support
- Known issues / limitations:
  - no explicit UI or meter
- Design observations:
  - one of the clearer recent "visible state" reworks

### Fractured
- Status: implemented
- Gameplay fantasy: unstable dimensional overlap that splits shots into staggered fractured copies
- Affected weapon categories:
  - ranged projectile
  - magic projectile
  - melee thrust
  - melee boomerang
  - other non-channelled melee projectile shots
- Main mechanics:
  - sequential 3-tier fracture cascade evaluated on original projectile spawn
  - Fracture I can spawn immediately; Fracture II and III can appear a few frames later if the cascade continues
  - fractured hits deal reduced damage
  - fractured hit tracking is normalized to avoid bad immunity interactions
- Visual/audio behavior:
  - tiered dimensional tinting and instability
  - underlay/afterimage draw path
  - fracture spawn dust
  - restrained built-in fracture sound on cascade start
- Key files:
  - `Content/Prefixes/Weapons/EchoingPrefix.cs`
  - `Content/Prefixes/Common/WeaponPrefix.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`
  - `Common/Config/PrefixTuningConfig.cs`
- Dependencies or related systems:
  - shifted simulation support
- Known issues / limitations:
  - internal naming is still Echoing-based for compatibility and lower churn
  - `EchoingWeaponMode` still lists unsupported flail/channelled melee projectile categories
- Design observations:
  - solid projectile-path identity with a clearer fracture fantasy than the old single-copy echo

### Deadeye
- Status: implemented
- Gameplay fantasy: a periodic ghostly precision round for ranged projectile weapons
- Affected weapon categories: ranged projectile weapons
- Main mechanics:
  - Deadeye Ready arms after 5 seconds or 2 seconds of holding still
  - the next valid projectile consumes the charge and becomes a Deadeye Shot
  - Deadeye Shots gain projectile speed, bonus crit chance, and bonus crit damage
  - critical Deadeye hits trigger a leafy AoE burst
- Visual/audio behavior:
  - purple ready pulse near the held weapon
  - purple projectile trail / afterimage on charged shots
  - green leafy burst on critical hits
  - quiet ready cue and restrained crit-burst cue
- Key files:
  - `Content/Prefixes/Weapons/DeadeyePrefix.cs`
  - `Common/Players/DeadeyePlayer.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalItem.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`
- Dependencies or related systems:
  - shifted simulation support
- Known issues / limitations:
  - multi-projectile control is enforced through a short spawn lockout rather than explicit shot grouping
- Design observations:
  - more specific and more legible than the old Headshot distance-scaling design

### Radiant
- Status: implemented
- Gameplay fantasy: luminous burning strikes
- Affected weapon categories: standard direct combat weapons
- Main mechanics:
  - inflicts `On Fire!`
  - modest stat tradeoff with crit support
- Visual/audio behavior:
  - held glow
  - projectile glow and ember dust
  - impact burst
  - no sound cue
- Key files:
  - `Content/Prefixes/Weapons/RadiantPrefix.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalItem.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`
- Dependencies or related systems:
  - shifted simulation support
- Known issues / limitations:
  - no audio feedback
- Design observations:
  - lightweight but readable

### Vampiric
- Status: implemented
- Gameplay fantasy: predator/extraction sustain that feeds harder on weakened prey
- Affected weapon categories: standard melee, ranged, and magic combat weapons
- Main mechanics:
  - steals life on hit
  - magic weapons also restore mana on hit
  - crits increase sustain payoff
  - prey thresholds at `<50%`, `<25%`, and `<12.5%` increase feed
  - Frenzy triggers from threshold progression and same-threshold retrigger timing
  - Frenzy scales by trigger tier and boosts attack speed, sustain caps, and weakened-prey crit pressure
- Visual/audio behavior:
  - crimson feed effects
  - arcane accent for magic siphon
  - stronger Frenzy trigger burst
  - held Frenzy aura
  - no dedicated sound cue
- Key files:
  - `Content/Prefixes/Weapons/SanguinePrefix.cs`
  - `Common/Players/VampiricPlayer.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalItem.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalNPC.cs`
- Dependencies or related systems:
  - shifted simulation support
- Known issues / limitations:
  - internal naming still uses `Sanguine`
- Design observations:
  - this is now the live sustain identity

### Shifting
- Status: implemented
- Gameplay fantasy: weapon temporarily rewrites itself into stronger simulated modifier states
- Affected weapon categories:
  - melee swing
  - ranged projectile
  - magic projectile
- Main mechanics:
  - active only in combat
  - rotates through mode-specific simulation pools
  - applies simulated behavior at `1.5x` strength
  - explicit packet sync to the owning player
- Visual/audio behavior:
  - reroll combat text
  - held aura
  - reroll burst
  - no sound cue
- Key files:
  - `Content/Prefixes/Weapons/ShiftingPrefix.cs`
  - `Common/Players/ShiftingPlayer.cs`
  - `Content/Prefixes/Common/ShiftingSimulation.cs`
  - `Mozandifiers.cs`
- Dependencies or related systems:
  - every simulated modifier branch
- Known issues / limitations:
  - hardcoded pools, ids, and shifted helper catalog
- Design observations:
  - the heaviest cross-cutting system in the project

### Skirmishing
- Status: implemented
- Gameplay fantasy: mobile ranged pressure and repositioning
- Affected weapon categories: ranged projectile weapons
- Main mechanics:
  - hits open a skirmish window
  - active window grants movement benefits
  - next shot during the window becomes a follow-up shot
  - follow-up adds projectile speed and crit-damage payoff
- Visual/audio behavior:
  - owner-local movement dust/light during the window
  - follow-up release/tracer visuals
  - no sound cue
- Key files:
  - `Content/Prefixes/Weapons/SkirmishingPrefix.cs`
  - `Common/Players/SkirmishingPlayer.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalItem.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`
- Dependencies or related systems:
  - shifted simulation support
- Known issues / limitations:
  - follow-up is shot-level, so multi-projectile shots all inherit it
- Design observations:
  - clearly distinct from Deadeye, Spinbound, and Breaching

### Spinbound
- Status: implemented
- Gameplay fantasy: Fibonacci cadence mastery with Golden Spin geometry
- Affected weapon categories: ranged projectile weapons
- Main mechanics:
  - Fibonacci cadence empowers select releases
  - empowered release adds damage and crit-damage bonus
  - Golden Spin tiering amplifies the payoff
  - stabilization pulls projectile speed toward a reference speed
- Visual/audio behavior:
  - tiered release burst, light, trail, and sound stack
  - combat text and live debug chat output
- Key files:
  - `Content/Prefixes/Weapons/SpinboundPrefix.cs`
  - `Common/Players/SpinboundPlayer.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`
- Dependencies or related systems:
  - shifted simulation support
- Known issues / limitations:
  - `Main.NewText` debug output is still active
- Design observations:
  - one of the highest-polish modifiers, but still carries tuning residue

### Stormforged
- Status: implemented
- Gameplay fantasy: lightning proc that chains into nearby enemies
- Affected weapon categories:
  - swing melee
  - standard projectile combat weapons
- Main mechanics:
  - proc chance
  - maximum chain jumps
  - chain range and damage decay
  - recursion suppression through `StormforgedPlayer`
- Visual/audio behavior:
  - electric chain dust and lighting
  - no sound cue
- Key files:
  - `Content/Prefixes/Weapons/StormforgedPrefix.cs`
  - `Common/Combat/StormforgedChainHelper.cs`
  - `Common/Players/StormforgedPlayer.cs`
- Dependencies or related systems:
  - shifted simulation support
- Known issues / limitations:
  - helper still carries minor cleanup residue
- Design observations:
  - functionally solid and lighter on polish than `Spinbound` or `Deadeye`

### Temporal
- Status: implemented
- Gameplay fantasy: time distortion that accelerates attacks and projectile travel
- Affected weapon categories: all standard weapons
- Main mechanics:
  - strong attack-speed increase
  - projectile weapons also gain projectile-speed increase
  - damage is reduced to pay for the tempo
- Visual/audio behavior:
  - held/use pulse
  - projectile pulse and light
  - restrained `PreDraw` afterimage
  - no sound cue
- Key files:
  - `Content/Prefixes/Weapons/TemporalPrefix.cs`
  - `Common/Players/TemporalFeedbackPlayer.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalItem.cs`
  - `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`
- Dependencies or related systems:
  - shifted simulation support
- Known issues / limitations:
  - projectile-speed portion only matters on projectile weapons
- Design observations:
  - coherent and lightweight after the identity pass

## 6. Weapon-Type Behaviour Matrix
| Weapon family | Live support pattern | Modifier notes | Known fragility |
|---|---|---|---|
| Sword-like swing melee | Strong item-side path | Supports `Awakened`, `Breaching`, `Stormforged`, `Radiant`, `Vampiric`, `Desperate`, `Temporal`, `Shifting` | `Awakened` uses a stricter sword-only gate than general swing-melee support |
| Other swing melee | Strong item-side path | Supports `Breaching`, `Stormforged`, `Radiant`, `Vampiric`, `Desperate`, `Temporal`, `Shifting` | Support is broader than the sword-only Awakened path |
| Ranged projectile | Strongest overall path | Supports `Deadeye`, `Spinbound`, `Skirmishing`, `Breaching`, `Fractured`, `Stormforged`, `Radiant`, `Vampiric`, `Desperate`, `Temporal`, `Shifting` | Rapid-fire spam must be gated per modifier |
| Magic projectile | Strong path | Supports `Attuned`, `Catalytic`, `Fractured`, `Temporal`, `Stormforged`, `Radiant`, `Vampiric`, `Desperate`, `Shifting` | Channelled magic is commonly excluded |
| Magic non-projectile | Partial support | `Attuned`, `Vampiric`, `Desperate`, `Temporal`, and some broad standard-combat modifiers can still roll | Inference: not the primary intended path for the newer modifier loops |
| Projectile melee | Limited but intentional | `Breaching` supports heavy projectile melee; `Fractured` supports thrust/boomerang and other non-channelled melee projectile shots; some broad combat modifiers can still apply | Behavior varies by projectile family and source tagging |
| Boomerangs | Narrow support | Explicit `Fractured` path | Most other specialty modifiers do not target boomerangs directly |
| Spears / thrust melee | Narrow support | Explicit `Fractured` path | Other projectile-special modifiers do not target this family directly |
| Flails / yoyos / held melee projectiles | Mostly excluded or deferred | `EchoingWeaponMode` names some of them, but implemented support excludes them | Clear taxonomy/runtime mismatch |
| Channelled / held projectiles | Mostly excluded | Many helpers and `CanRoll(...)` paths use `!item.channel` | Intentional conservative support |
| Summon / sentry | Not supported as a live family | Weapon helpers avoid summon classes | No summon-side runtime exists |

## 7. Projectile and Combat Interaction Patterns
### Projectile spawn tagging
- Main propagation happens in `WeaponPrefixGlobalProjectile.OnSpawn`.
- Modifier flags are stamped there and synced with `SendExtraAI` / `ReceiveExtraAI` only when remote state is needed.

### Item-side only paths
- `Awakened` is intentionally item-side only.
- Its sweet-spot, meter gain, awakened bonus damage, and boss budget logic do not use projectile paths.

### Parent-projectile inheritance
Live inheritance from parent projectiles is narrow and explicit:
- `Vampiric`
- `Radiant`
- `Stormforged`

Shifted inheritance is also narrow:
- `Vampiric`
- `Radiant`
- `Stormforged`

Inference: the repo intentionally avoids broad child-projectile inheritance because vanilla projectile families are inconsistent.

### Duplicate / fractured projectiles
- `Fractured` uses a tiered cascade scheduler on the original projectile.
- Fractured copies:
  - reuse the projectile type
  - rotate velocity slightly with tier-based variance
  - reduce damage through `EchoDamageMultiplier`
  - normalize NPC immunity behavior

### Hit immunity handling
- `Fractured` explicitly rewrites hit tracking through `NormalizeEchoHitTracking(...)`.
- This is one of the clearest projectile-family safety layers in the repo.

### NPC hit tracking and target-persistent state
- `Catalytic` stores per-player mark and arm timing in `WeaponPrefixGlobalNPC`.
- `Vampiric` stores per-player prey-threshold trigger memory and retrigger timing there.
- `Awakened` stores per-player rolling boss bonus-damage samples there.

### Ownership and multiplayer safety
- Owner-local gates are standard:
  - `player.whoAmI == Main.myPlayer`
  - `projectile.owner == Main.myPlayer`
- These gates are used for:
  - sustain application
  - local-only visuals and sounds
  - combat text

### Delayed windows and timed states
Current player-side windows/state machines include:
- `Attuned`
- `Awakened`
- `Desperate`
- `Skirmishing`
- `Spinbound`
- `Vampiric`

### Scaling / orientation / rotation patterns
- `Awakened`: sweet-spot line segment and scale-aware blade length
- `Deadeye`: spawn-consumed projectile tagging plus player-side ready state
- `Spinbound`: velocity geometry plus player movement
- `Breaching`: impact scale from weapon weight/tempo profile
- `Temporal`, `Fractured`, `Skirmishing`: afterimage/tracer underlays
- `Vampiric`: prey-state scaling and Frenzy tier scaling

### Collision / hitbox caveats
- `Awakened` depends on a line-based approximation of the outer blade section, not the full melee hitbox
- `Deadeye` depends on correct player-side charge consumption before multi-projectile spawn bursts
- `Spinbound` depends on queued shot contexts in `SpinboundPlayer`
- `Catalytic` depends on correct ordering between `ModifyHitNPC` and `OnHitNPC`
- `Attuned` and `Skirmishing` are shot-level, so multi-projectile casts share the same cast/shot decision
- `Vampiric` threshold triggers are deterministic because tracking is target-local in `WeaponPrefixGlobalNPC`

## 8. Visual, Audio, and Feedback Patterns
### Current feedback style
The repo consistently prefers restrained Terraria-native feedback:
- `Dust.NewDustPerfect(...)`
- `Lighting.AddLight(...)`
- occasional `CombatText.NewText(...)`
- selective `PreDraw` underlays, afterimages, and tracers

There are no UI bars, custom shaders, or custom sound assets.

### Held-state feedback
Current modifiers with explicit held/use-state feedback:
- `Attuned`
- `Awakened`
- `Desperate`
- `Radiant`
- `Shifting`
- `Skirmishing`
- `Temporal`
- `Vampiric`

### Impact feedback
Current modifiers with explicit hit/payoff confirmation:
- `Awakened`
- `Breaching`
- `Catalytic`
- `Desperate`
- `Deadeye`
- `Radiant`
- `Vampiric`

### Projectile-local VFX
Projectile-local visual updates are active for:
- `Attuned`
- `Desperate`
- `Fractured`
- `Skirmishing`
- `Spinbound`
- `Temporal`
- `Vampiric`

### `PreDraw` usage
`PreDraw` is used selectively for:
- `Fractured` parallel-path distortion underlays
- `Skirmishing` follow-up tracer
- `Temporal` afterimage

### Sound usage
Sound is still sparse and reserved for higher-signal events.
Built-in sound usage is present for:
- `Attuned` empowered cast
- `Fractured` fracture spawn cue
- `Deadeye` ready / crit-burst cues
- `Spinbound` tiered release stack

No dedicated sound cue is present for:
- `Awakened`
- `Breaching`
- `Catalytic`
- `Desperate`
- `Radiant`
- `Shifting`
- `Skirmishing`
- `Stormforged`
- `Temporal`
- `Vampiric`

### Feedback identity notes
- `Awakened`: electric awakening aura and sword-tip sweet-spot confirmation
- `Breaching`: heavy impact / debris / armor-break read
- `Deadeye`: ghostly ranged precision with green leaf-burst payoff
- `Fractured`: unstable spatial duplication
- `Spinbound`: tiered release spectacle
- `Vampiric`: crimson extraction with arcane accent on magic feeds

## 9. Design Intent Extracted from the Repo
### Explicit intent
- `AGENTS.md` explicitly defines the ownership model:
  - prefix stats in prefix classes
  - item-side behavior in `WeaponPrefixGlobalItem`
  - projectile-side behavior in `WeaponPrefixGlobalProjectile`
  - target-persistent state in `WeaponPrefixGlobalNPC`
  - owner-local timers/caps/windows in `Common/Players`
- `description.txt` and `description_workshop.txt` explicitly position the mod as weapon-prefix-focused with bonus pet content.
- Prefix names, constants, and tooltip text define distinct fantasies such as:
  - resonance casting
  - awakened sword state
  - siege breach
  - mobility pressure
  - cadence mastery
  - predator sustain

### Inferred intent
- Inference: the project is moving away from invisible stat bundles toward visible gameplay loops.
  - Evidence: recent stateful or feedback-heavy implementations for `Attuned`, `Awakened`, `Desperate`, `Skirmishing`, `Temporal`, `Vampiric`, and `Breaching`.
- Inference: support for unusual weapon families is intentionally conservative.
  - Evidence: `AGENTS.md` ask-first rules and many `!item.channel` / standard-weapon helper checks.
- Inference: owner-local feedback safety is a deliberate house style.
  - Evidence: repeated `Main.myPlayer` and owner-gating around VFX, SFX, sustain, and combat text.
- Inference: `Shifting` is the main cross-cutting showcase system.
  - Evidence: explicit packets, pool routing, shifted display mapping, and broad branch integration.

## 10. Current Problems and Fragile Areas
- **Spinbound still has live debug chat output**
  - What is happening: `Main.NewText(BuildGoldenSpinDebugText(goldenSpin), Color.Gold);` is still active.
  - Likely cause: Golden Spin tuning residue.
  - Impacted files: `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`
  - Confidence: High

- **`WeaponPrefixGlobalProjectile.cs` remains the main maintainability hotspot**
  - What is happening: one file owns most projectile flags, sync fields, projectile-side modifier math, and projectile VFX.
  - Likely cause: iterative growth around a central projectile runtime model.
  - Impacted files: `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`
  - Confidence: High

- **Fractured taxonomy is broader than the live implementation**
  - What is happening: `EchoingWeaponMode` still lists unsupported flail/channelled melee projectile modes, while `IsImplementedEchoingMode(...)` excludes them.
  - Likely cause: planned or abandoned support was not fully collapsed.
  - Impacted files: `Content/Prefixes/Common/WeaponPrefix.cs`
  - Confidence: High

- **Attuned roll support is broader than its most complete runtime path**
  - What is happening: `AttunedPrefix.CanRoll(...)` accepts mana-using magic weapons generally, but the strongest implementation path is projectile-based.
  - Likely cause: the projectile magic path was prioritized first.
  - Impacted files: `Content/Prefixes/Weapons/AttunedPrefix.cs`, `Content/Prefixes/Common/WeaponPrefixGlobalItem.cs`, `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`
  - Confidence: Medium

- **Awakened sword compatibility is intentionally narrow**
  - What is happening: `Awakened` uses `IsStandardSwordWeapon(...)` and a separate sweet-spot helper instead of the broader swing-melee path.
  - Likely cause: the modifier is intentionally scoped to sword-like swings.
  - Impacted files: `Content/Prefixes/Common/WeaponPrefix.cs`, `Content/Prefixes/Weapons/AwakenedPrefix.cs`, `Common/Combat/AwakenedCombatHelper.cs`
  - Confidence: High

- **`Content/Projectiles/Weapons/` still exists but is empty**
  - What is happening: the folder remains in the repo even though live modifier runtime no longer uses custom modifier projectile classes.
  - Likely cause: old structure was left in place after cleanup.
  - Impacted files: `Content/Projectiles/Weapons/`
  - Confidence: High

- **Pet files still contain sample/template residue**
  - What is happening: pet item files still include ExampleMod-style comments/import residue.
  - Likely cause: they were adapted from sample content with minimal cleanup.
  - Impacted files: `Content/Pets/Amelia/AmeliaItem.cs`, `Content/Pets/Ludovica/LudovicaItem.cs`
  - Confidence: High

## 11. Extension Points for New Modifiers
Safest implementation path:

1. **Create the prefix class**
   - Path: `Content/Prefixes/Weapons/NewPrefix.cs`
   - Base: `WeaponPrefix`
   - Implement:
     - `PrefixCategory`
     - `CanRoll(Item item)`
     - `SetStats(...)`
     - behavior constants
     - optional `GetExtraTooltipLines(...)`

2. **Add localization**
   - File: `Localization/en-US_Mods.Mozandifiers.hjson`
   - Add `DisplayName` and any user-visible tooltip keys.

3. **Choose the smallest correct state owner**
   - item-side direct-hit or held/use behavior:
     - `Content/Prefixes/Common/WeaponPrefixGlobalItem.cs`
   - projectile tagging / visuals / projectile-side hit logic:
     - `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`
   - target-persistent setup/threshold/mark/budget state:
     - `Content/Prefixes/Common/WeaponPrefixGlobalNPC.cs`
   - owner-local timers/windows/caps:
     - `Common/Players/NewModifierPlayer.cs`
   - shared combat helper:
     - `Common/Combat/*.cs`

4. **Reuse live patterns before adding a new subsystem**
   - cast/shot windows:
     - `AttunedPlayer`
     - `SkirmishingPlayer`
     - `SpinboundPlayer`
   - direct-hit state machine:
     - `AwakenedPlayer`
   - target-persistent state:
     - `WeaponPrefixGlobalNPC`
   - owner-local sustain/tier state:
     - `DesperatePlayer`
     - `VampiricPlayer`

5. **If Shifting support is desired**
   - add or map a `ShiftingSimulationId`
   - update display-name mapping in `ShiftingSimulationCatalog.GetDisplayName(...)`
   - add shifted stat/effect helpers
   - place it in one or more pools
   - wire shifted runtime behavior in item/projectile globals

6. **Keep feedback restrained**
   - Prefer:
     - `Lighting.AddLight`
     - `Dust.NewDustPerfect`
     - selective `PreDraw`
     - built-in `SoundID`
   - Add owner-local cooldown gates in `Common/Players` when rapid-fire spam is possible.

7. **Testing implications**
   - test both item-side and projectile-side combat routes when applicable
   - test multiplayer ownership assumptions
   - test child-projectile inheritance explicitly instead of assuming it
   - test unusual vanilla spawn logic when spawn position, cadence, projectile speed, or target state matters
   - update localization whenever visible behavior changes

## 12. Glossary of Project-Specific Terms
- **Prefix**: a custom Terraria weapon modifier implemented as a `ModPrefix`.
- **WeaponPrefix**: the project's shared base class for weapon prefixes.
- **Shifting simulation**: the temporary modifier state a `Shifting` weapon currently mimics.
- **Resonance**: Attuned's cast/hit-built state that arms a free empowered cast.
- **Sweet spot**: the outer portion of a sword swing used by `Awakened` for valid hits.
- **Awakened state**: the temporary powered phase of the `Awakened` modifier.
- **Skirmish Window**: the short post-hit window that grants movement bonuses and empowers the next Skirmishing shot.
- **Golden Spin**: Spinbound's geometry/tier evaluation system for empowered shots.
- **Breach**: a qualifying Breaching impact against resistant or heavy targets.
- **Fracture copy**: a duplicated projectile spawned by Fractured's cascade.
- **Catalytic mark**: a target state that arms and is later consumed for damage.
- **Frenzy**: Vampiric's short-lived predator state triggered by weakened-prey thresholds.
- **Owner-local**: logic intentionally restricted to the local owner (`Main.myPlayer`) to avoid duplicate feedback or state.

## 13. Open Questions / Missing Context
- Is `AttunedPrefix.CanRoll(...)` intentionally meant to include non-projectile magic weapons long-term?
- Are the unsupported Fractured melee modes planned, or are they simply leftover taxonomy?
- Is the live Spinbound debug chat output still intentionally used for tuning?
- Does `Content/Projectiles/Weapons/` remain intentionally reserved, or is it now dead structure?

## 14. Recommended Next Documentation Files
- `MODIFIER_GUIDE.md`
  - Why: one compact place to describe each live modifier's loop, compatibility, and main tuning knobs.
- `COMBAT_PIPELINE.md`
  - Why: item -> projectile -> NPC -> player flow is central and currently spread across several files.
- `WEAPON_COMPATIBILITY.md`
  - Why: compatibility and exclusion rules are a recurring fragility source.
- `SHIFTING_SIMULATIONS.md`
  - Why: `Shifting` is a cross-cutting system with pools, scaling, and sync behavior that currently spans several files.
- `VFX_RULES.md`
  - Why: the repo now has a clear restrained, readable, owner-local feedback style, but it is not documented separately.
