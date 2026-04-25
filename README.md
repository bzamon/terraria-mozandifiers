# Mozandifiers

`Mozandifiers` is a Terraria `tModLoader 1.4.4` mod centered on custom weapon prefixes. The project is primarily a combat-systems mod, with a small amount of secondary pet and starter-item content.

## Current Scope

Live user-facing prefixes:
- `Attuned`
- `Awakened`
- `Breaching`
- `Catalytic`
- `Deadeye`
- `Desperate`
- `Fractured`
- `Radiant`
- `Vampiric`
- `Shifting`
- `Skirmishing`
- `Spinbound`
- `Stormforged`
- `Temporal`

Additional content:
- `AwakenedBuff` and `SealedDebuff`
- `DeadeyeBurstProjectile`
- pets: `Amelia` and `Ludovica`
- a starter-item hook that can grant `Amelia` to fresh characters

## Architecture Summary

The mod follows a consistent ownership split:
- prefix classes in `Content/Prefixes/Weapons` define roll gates, base stats, and tooltip-facing behavior
- `WeaponPrefixGlobalItem` owns item-side hooks
- `WeaponPrefixGlobalProjectile` owns projectile state, projectile hooks, and projectile sync
- `WeaponPrefixGlobalNPC` owns target-persistent NPC state
- `Common/Players` owns owner-local timers, caps, windows, and charge state
- `Common/Combat` holds shared combat helpers

The core combat flow is:

```text
prefix definition
  -> item/global hook
  -> item or projectile dispatch
  -> runtime helper
  -> player or NPC persistent state when needed
```

## Notable Systems

- `Shifting` is the most cross-cutting feature. It is server-authoritative and uses:
  - `ShiftingSimulationPools` for weapon-mode detection and pool selection
  - `ShiftingSimulationDisplay` for current simulated prefix names
  - `ShiftingSimulationStatScaling` and `ShiftingSimulationEffectScaling` for shifted values
- Localization lives in `Localization/en-US_Mods.Mozandifiers.hjson`.
- Server-side balance tuning lives in `Common/Config/PrefixTuningConfig.cs`.

## Naming Notes

- `Fractured` is still implemented in `EchoingPrefix.cs`.
- `Vampiric` is still implemented in `SanguinePrefix.cs`.
- Legacy config compatibility aliases for older `Echoing` and `Colossal` naming still exist.

## Non-Prefix Boundary

Pets and starter-item logic are secondary systems. They are intentionally isolated from prefix gameplay and should stay that way unless a task explicitly couples them.

## Build

Typical validation command:

```powershell
dotnet build Mozandifiers.csproj
```

The assembly can compile successfully even if packaging fails because `tModLoader` is locking `Mods/Mozandifiers.tmod`.

## Documentation

For the full architecture and modifier breakdown, see:
- `MOZANDIFIERS_CONTEXT.md`
- `MOZANDIFIERS_INDEX.md`
