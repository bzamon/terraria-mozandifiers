# AGENTS.md

## Project Overview
- `Mozandifiers` is a Terraria `tModLoader 1.4.4` mod written in C#.
- The project is lightweight: `Mozandifiers.csproj` imports `tModLoader.targets`; there are no extra package dependencies.
- Main feature area today is weapon prefixes/modifiers.
- Prefix definitions live in `Content/Prefixes/Weapons`.
- Shared prefix architecture lives in `Content/Prefixes/Common`:
  - `BasePrefix.cs`
  - `WeaponPrefix.cs`
  - `WeaponPrefixGlobalItem.cs`
  - `WeaponPrefixGlobalProjectile.cs`
  - `WeaponPrefixGlobalNPC.cs`
- Shared gameplay helpers live in `Common/Combat`, `Common/Players`, and `Common/Config`.
- Localization lives in `Localization/en-US_Mods.Mozandifiers.hjson`.
- Custom modifier projectiles live in `Content/Projectiles/Weapons`.

## Architecture Notes
- Prefer adding new modifier stats in the prefix class first.
- Put item-side behavior in `WeaponPrefixGlobalItem`.
- Put projectile-side behavior and projectile flag propagation in `WeaponPrefixGlobalProjectile`.
- Put target-persistent NPC state in `WeaponPrefixGlobalNPC`.
- Put owner-local timers, caps, or per-player state in `Common/Players`.
- Put reusable combat logic in `Common/Combat`.
- Reuse existing patterns before adding new systems.

## Coding Standards
- Follow existing C# style in the repo:
  - file-scoped namespaces are acceptable where already used
  - PascalCase for types/methods/properties
  - camelCase for locals/fields unless constants
  - `const` for balance constants when practical
  - early returns over deep nesting
- Keep changes small and reversible.
- Prefer stat hooks and existing tModLoader hooks over custom frameworks.
- Keep modifier logic explicit and readable; do not hide behavior in generic abstractions unless repetition is real.
- Update localization whenever tooltip-visible behavior changes.
- Keep user-facing balance constants close to the relevant prefix class.
- Avoid adding debug chat text (`Main.NewText`) unless explicitly requested for temporary debugging.

## Always
- Read this file before changing code.
- Inspect the current implementation before editing behavior.
- Preserve the current modifier architecture unless a change is clearly justified.
- Check both item-side and projectile-side paths when modifying combat behavior.
- Consider multiplayer ownership and synchronization for projectile/NPC/player state.
- Keep behavior-level changes aligned with tooltip/localization text.
- Run a build check after code changes when feasible.
- Mention if packaging fails because `tModLoader` is locking `.tmod` output.

## Ask First
- Any change that alters the intended gameplay identity of an existing modifier.
- Any balance change to existing modifiers that is not a direct bug fix.
- Any expansion of support to new weapon families:
  - spears
  - flails
  - yoyos
  - channelled/held projectiles
  - summon/sentry weapons
- Any change to current exclusion rules for child projectiles, dummies, bosses, or multiplayer ownership.
- Any refactor that moves logic across `GlobalItem`, `GlobalProjectile`, `GlobalNPC`, or `ModPlayer`.
- Any change to config shape, localization key structure, or folder structure.
- Any destructive git action, branch management change, or release/publish step.

## Never
- Never push directly to `main`.
- Never rewrite unrelated files while fixing a single modifier bug.
- Never remove existing safeguards without confirming the gameplay intent.
- Never assume projectile behavior is the same across melee, ranged, and magic.
- Never add new dependencies unless explicitly justified.
- Never leave stale tooltips or descriptions after changing visible behavior.
- Never use `git reset --hard`, `git checkout --`, or similar destructive commands unless explicitly requested.

## Terraria / tModLoader Guidance
- Prefer direct item-source projectile tagging for modifier propagation unless inheritance is explicitly intended.
- Be careful with `OnSpawn`, `OnHitNPC`, `ModifyHitNPC`, immunity settings, and `SendExtraAI/ReceiveExtraAI`.
- For projectile effects, think through:
  - piercing
  - local/static NPC immunity
  - delayed hits
  - child projectiles
  - unusual vanilla spawn logic
  - dummy behavior
  - multiplayer ownership
- For sustain effects, use owner-local caps/timers in `ModPlayer`.
- For target-specific combo/setup effects, use `GlobalNPC`.
- Avoid over-broad support in v1; standard melee/ranged/magic paths are preferred first.

## Build / Validation
- Typical validation command: `dotnet build Mozandifiers.csproj`
- A successful assembly compile may still fail packaging if `tModLoader` is open and locking `Mods/Mozandifiers.tmod`.
- If a build fails, separate compile errors from packaging-lock errors in the report.

## Reference Material
- tModLoader docs: https://docs.tmodloader.net/docs/stable/annotated.html
- ExampleMod (1.4.4): https://github.com/tModLoader/tModLoader/tree/1.4.4/ExampleMod
- Use official docs and ExampleMod patterns when unsure about hooks, sync, or content loading.
