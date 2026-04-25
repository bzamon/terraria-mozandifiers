# Mozandifiers TODO

This file tracks future implementation work for the current `tModLoader 1.4.4` prefix system. It reflects the repo after the `Shifting` helper split and the Fractured/Vampiric runtime naming pass, with `MOZANDIFIERS_CONTEXT.md` and `MOZANDIFIERS_INDEX.md` treated as the primary project references.

## Critical technical debt

### 1. Remove the unused starter-item state and clean the file
Priority: High
Affected files: `Common/Players/StartingItemsPlayer.cs`
Problem statement: `StartingItemsPlayer.cs` still carries the unused `receivedStarterItem` field and older formatting. It produces the only known standing compile warning and makes the file look less intentional than the rest of the repo.
Proposed implementation plan: Remove the dead field, trim any now-unused imports, and keep the current starter-item behavior unchanged. Add a short guard comment if needed so the slot-0 and max-life checks remain understandable without re-reading the whole file.
Risk level: Low
Validation steps: Build the mod and confirm the CS0169 warning is gone. Verify a fresh character still receives `Amelia` only when slot `0` is empty and max life is `100`.

### 2. Audit projectile sync payload and state ownership after the naming split
Priority: High
Affected files: `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`, `Content/Prefixes/Common/WeaponPrefixProjectileDispatch.cs`, `Content/Prefixes/Common/FracturedRuntime.cs`, `Content/Prefixes/Common/SpinboundRuntime.cs`, `Content/Prefixes/Common/DeadeyeRuntime.cs`, `Content/Prefixes/Common/VampiricRuntime.cs`
Problem statement: `WeaponPrefixGlobalProjectile` now uses clearer Fractured/Vampiric names, but it still owns a large cross-section of gameplay and visual state. The more modifiers use snapshots and one-shot projectile flags, the easier it becomes to over-sync or couple unrelated systems.
Proposed implementation plan: Inventory each synced field and classify it as gameplay-critical, visual-only, or reconstructible. Keep the binary packet order stable until the audit is complete, then remove only fields that can be recomputed safely. Document which projectile states must remain authoritative across clients.
Risk level: High
Validation steps: Build the mod. Verify fracture cascades, Deadeye charged shots, Spinbound bonuses, Vampiric projectile sustain, and shifted projectile behavior in single-player and multiplayer with no host/client divergence.

### 3. Reduce `WeaponPrefixProjectileDispatch` size by splitting responsibility boundaries
Priority: High
Affected files: `Content/Prefixes/Common/WeaponPrefixProjectileDispatch.cs`, `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`, `Content/Prefixes/Common/WeaponPrefixItemDispatch.cs`
Problem statement: The `Shifting` helper split improved one hot spot, but `WeaponPrefixProjectileDispatch.cs` is still a very large mixed-responsibility file covering spawn, hit, visuals, sync, and shifted state snapshots for many modifiers at once.
Proposed implementation plan: Split projectile dispatch into focused files or partial helpers by responsibility, such as spawn initialization, hit resolution, visuals, and sync. Keep `WeaponPrefixGlobalProjectile` as the entry point and avoid moving logic across architecture layers unless the ownership stays obvious.
Risk level: Medium
Validation steps: Build the mod and diff behavior against the current implementation. Verify projectile spawn flags, shifted snapshots, visual updates, and `SendExtraAI`/`ReceiveExtraAI` all still behave identically.

## Modifier naming cleanup

### 4. Finish the remaining Fractured cleanup at the class and compatibility layer
Priority: High
Affected files: `Content/Prefixes/Weapons/EchoingPrefix.cs`, `Content/Prefixes/Common/WeaponPrefix.cs`, `Common/Config/PrefixTuningConfig.cs`, `MOZANDIFIERS_CONTEXT.md`, `MOZANDIFIERS_INDEX.md`
Problem statement: Runtime state now speaks `Fractured`, but the class/file/config compatibility layer still exposes `Echoing`. That is acceptable today, but it still leaks historical naming into maintenance and documentation work.
Proposed implementation plan: Produce an explicit migration map for what would be renamed later and what must stay for compatibility. Keep class/file names and config aliases unchanged until a later dedicated cleanup pass is approved, but document the remaining `Echoing` surfaces clearly so future work does not rediscover them.
Risk level: Medium
Validation steps: Search for `Echoing` after documentation updates and confirm the remaining hits are limited to class/file names, localization keys, config compatibility aliases, and intentionally retained compatibility notes.

### 5. Finish the remaining Vampiric cleanup at the class and compatibility layer
Priority: High
Affected files: `Content/Prefixes/Weapons/SanguinePrefix.cs`, `Common/Config/PrefixTuningConfig.cs`, `MOZANDIFIERS_CONTEXT.md`, `MOZANDIFIERS_INDEX.md`
Problem statement: Runtime state now uses `Vampiric`, but the owning class and compatibility surfaces still say `Sanguine`. This is less confusing than before, but it is still a source of drift when writing docs or planning future refactors.
Proposed implementation plan: Document the exact remaining `Sanguine` surfaces and keep them unchanged for now. When a later rename pass is approved, treat class/file names, config compatibility, and localization keys as separate decisions rather than one all-or-nothing rename.
Risk level: Medium
Validation steps: Search for `Sanguine` after doc updates and confirm the remaining hits are limited to class/file names, localization keys, config compatibility aliases, and intentional transitional notes.

### 6. Document legacy config alias behavior and a future retirement path
Priority: Medium
Affected files: `Common/Config/PrefixTuningConfig.cs`, `MOZANDIFIERS_CONTEXT.md`, `MOZANDIFIERS_INDEX.md`
Problem statement: Old `Echoing` and `Colossal` config aliases still exist for compatibility, but there is no repo-local note explaining what is legacy, what is canonical, and when removal could be considered.
Proposed implementation plan: Add a short maintainer-facing alias note to the docs and keep the aliases unchanged in code. Future removal should only happen behind a versioned deprecation plan with explicit regression checks against older config names.
Risk level: Low
Validation steps: Confirm the docs explain which config names are canonical today and which aliases remain only for compatibility.

## Weapon compatibility cleanup

### 7. Tighten Fractured compatibility taxonomy to the current live support
Priority: High
Affected files: `Content/Prefixes/Common/WeaponPrefix.cs`, `Content/Prefixes/Weapons/EchoingPrefix.cs`, `MOZANDIFIERS_CONTEXT.md`, `MOZANDIFIERS_INDEX.md`
Problem statement: `Fractured` still depends on `Echoing`-era weapon mode helpers that model more melee projectile categories than the project currently wants to support in v1.
Proposed implementation plan: Audit the current `GetEchoingWeaponMode(...)` taxonomy against the intended live support list. Either narrow the helper to the current supported set or make unsupported branches explicitly transitional and documented.
Risk level: Medium
Validation steps: Build the mod and verify currently supported Fractured weapons still roll and behave the same. Confirm unsupported families are either clearly excluded or clearly documented as deferred.

### 8. Re-evaluate `Attuned` roll eligibility for non-projectile magic weapons
Priority: Medium
Affected files: `Content/Prefixes/Weapons/AttunedPrefix.cs`, `Content/Prefixes/Common/WeaponPrefix.cs`, `Common/Players/AttunedPlayer.cs`, `Content/Prefixes/Common/WeaponPrefixGlobalItem.cs`, `Content/Prefixes/Common/WeaponPrefixProjectileDispatch.cs`
Problem statement: `Attuned` still rolls on any mana-using magic weapon, but its strongest payoff and feedback are built around projectile casting. Some non-projectile magic weapons may technically qualify without matching the intended play pattern.
Proposed implementation plan: Build a representative list of vanilla projectile and non-projectile magic weapons, then decide whether the long-term gate should remain broad, become projectile-only, or gain a separate direct-cast path. Keep the final design choice under `Open Questions` until product direction is explicit.
Risk level: Medium
Validation steps: Test the intended sample set and verify resonance gain, empowered casts, and feedback readability on each category before changing eligibility.

### 9. Audit roll gates against child-projectile and delayed-projectile behavior
Priority: Medium
Affected files: `Content/Prefixes/Common/WeaponPrefix.cs`, `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`, `Content/Prefixes/Common/WeaponPrefixProjectileDispatch.cs`, `Common/Combat/StormforgedChainHelper.cs`, `Content/Prefixes/Common/TemporalRuntime.cs`
Problem statement: Several live modifiers rely on item-source snapshots or selective parent-projectile inheritance. Vanilla weapons with delayed spawns, split projectiles, or unusual child projectile chains can still drift from the intended compatibility model.
Proposed implementation plan: Build a compatibility matrix for the current supported melee, ranged projectile, and magic projectile families. For each modifier, document whether it expects item-source tagging, parent inheritance, or direct projectile initialization.
Risk level: Medium
Validation steps: Test representative vanilla weapons with delayed hits, child projectiles, piercing, and local/static immunity. Confirm modifier state applies only where intended.

## Modifier-specific improvements

### 10. Add maintainer notes for the most fragile modifier edge cases
Priority: Medium
Affected files: `MOZANDIFIERS_CONTEXT.md`, `MOZANDIFIERS_INDEX.md`, `Content/Prefixes/Weapons/*`, `Content/Prefixes/Common/*Runtime.cs`
Problem statement: Several modifiers have design-sensitive edge cases that are easy to break during cleanup work, especially `Awakened` boss budgeting, `Temporal` stored-damage collapse, `Spinbound` geometry checks, and `Deadeye` projectile assignment lockout.
Proposed implementation plan: Add short per-modifier "gotchas" notes to the docs that point future work at the fragile runtime assumptions without changing gameplay. Keep the notes close to current implementation reality rather than future wishlist behavior.
Risk level: Low
Validation steps: Cross-check each note against the owning files and verify the docs describe what the code actually does today.

### 11. Review `Temporal` readability for pressure, fracture, and collapse states
Priority: Medium
Affected files: `Content/Prefixes/Common/TemporalRuntime.cs`, `Common/Players/TemporalFeedbackPlayer.cs`, `Content/Prefixes/Common/WeaponPrefixGlobalNPC.cs`, `Localization/en-US_Mods.Mozandifiers.hjson`
Problem statement: `Temporal` is one of the most state-heavy modifiers in the mod, but its pressure, fracture, cooldown, and collapse states are still harder to read than the implementation depth justifies.
Proposed implementation plan: Plan a clarity-only pass that improves state feedback through combat text cadence, visual timing, and possibly tooltip wording. Keep damage storage and collapse behavior unchanged unless a separate gameplay task is approved.
Risk level: Medium
Validation steps: Verify a player can tell when a target is building pressure, when it is fractured, when stored damage is accumulating, and when cooldown is preventing immediate re-fracture.

### 12. Review `Spinbound` onboarding and geometry readability
Priority: Medium
Affected files: `Content/Prefixes/Common/SpinboundRuntime.cs`, `Common/Players/SpinboundPlayer.cs`, `Localization/en-US_Mods.Mozandifiers.hjson`
Problem statement: `Spinbound` has one of the most advanced payoff models in the mod, but its geometry and Fibonacci cadence requirements are still opaque without code-level context.
Proposed implementation plan: Keep the mechanic intact but plan a clarity pass with stronger tooltip wording, more legible release feedback, and a maintainer note explaining how valid geometry is evaluated.
Risk level: Low
Validation steps: Verify a fresh reader can understand the cadence and reward loop from tooltip text, visuals, and documentation without reading the runtime code first.

### 13. Review shifted-value drift for config-backed modifiers
Priority: Medium
Affected files: `Content/Prefixes/Common/ShiftingSimulationStatScaling.cs`, `Content/Prefixes/Common/ShiftingSimulationEffectScaling.cs`, `Content/Prefixes/Weapons/StormforgedPrefix.cs`, `Content/Prefixes/Weapons/BreachingPrefix.cs`, `Content/Prefixes/Weapons/DesperatePrefix.cs`, `Common/Config/PrefixTuningConfig.cs`
Problem statement: The `Shifting` split made the helper boundaries cleaner, but some shifted values still live as helper constants rather than obviously deriving from the same config-backed live values. That can create subtle drift over time.
Proposed implementation plan: Audit each shifted stat/effect helper and decide which ones should remain bespoke and which should derive from the live prefix/config values through shared scaling rules. Do not change balance until each derivation rule is explicitly chosen.
Risk level: Medium
Validation steps: Compare live and shifted behavior per modifier before and after any derivation cleanup and confirm the shifted version still reads as the same modifier at scaled strength.

## Visual/audio feedback improvements

### 14. Add missing sound cues across live modifiers
Priority: High
Affected files: `Content/Prefixes/Common/AwakenedRuntime.cs`, `Content/Prefixes/Common/CatalyticRuntime.cs`, `Content/Prefixes/Common/DeadeyeRuntime.cs`, `Content/Prefixes/Common/FracturedRuntime.cs`, `Content/Prefixes/Common/SpinboundRuntime.cs`, `Content/Prefixes/Common/TemporalRuntime.cs`, `Content/Prefixes/Common/VampiricRuntime.cs`, `Content/Prefixes/Common/WeaponPrefixItemDispatch.cs`, `Content/Prefixes/Common/WeaponPrefixProjectileDispatch.cs`
Problem statement: Several modifiers still have good visuals but weak or missing sound cues, which makes payoff moments harder to read in live combat and reduces distinction between otherwise deep mechanics.
Proposed implementation plan: Inventory current sound usage and add cues for major trigger moments such as `Catalytic` consume, `Temporal` fracture/collapse, `Vampiric` frenzy trigger, `Stormforged` proc, `Fractured` cascade spawn, `Awakened` state change, and `Spinbound` valid release. Prefer vanilla sounds first.
Risk level: Low
Validation steps: Verify each modifier has an audible cue for its major payoff event without becoming noisy in rapid-hit or projectile-heavy scenarios.

### 15. Standardize visual strength scaling between live and shifted variants
Priority: Medium
Affected files: `Content/Prefixes/Common/WeaponPrefixVisuals.cs`, `Content/Prefixes/Common/WeaponPrefixItemDispatch.cs`, `Content/Prefixes/Common/WeaponPrefixProjectileDispatch.cs`, `Content/Prefixes/Common/AwakenedRuntime.cs`, `Content/Prefixes/Common/TemporalRuntime.cs`, `Content/Prefixes/Common/VampiricRuntime.cs`, `Content/Prefixes/Common/SpinboundRuntime.cs`
Problem statement: Some shifted effects scale their visuals directly by `ShiftingStrengthMultiplier`, while others effectively use bespoke visual rules. The result is readable but inconsistent across the mod.
Proposed implementation plan: Define a small set of visual-scaling rules for light intensity, dust density, afterimage opacity, and burst size, then apply them gradually without changing gameplay numbers.
Risk level: Low
Validation steps: Compare live and shifted variants for readability, consistency, and performance during projectile-heavy encounters.

## Multiplayer/networking review

### 16. Review `ShiftingPlayer` sync and held-item authority end-to-end
Priority: High
Affected files: `Mozandifiers.cs`, `Common/Players/ShiftingPlayer.cs`, `Content/Prefixes/Common/ShiftingSimulationPools.cs`, `Content/Prefixes/Common/ShiftingSimulationDisplay.cs`, `Content/Prefixes/Common/WeaponPrefixIdentity.cs`
Problem statement: `Shifting` remains server-authoritative and cross-cutting even after the helper split. Any drift between held item context, reroll timing, and client display still creates confusing multiplayer bugs.
Proposed implementation plan: Document the exact `ShiftingPlayer` sync contract, including what the server decides, when packets are sent, what clients may display locally, and how held-item identity is validated. Then audit weapon swap, reconnect, and late-join edge cases.
Risk level: High
Validation steps: Test weapon swap during combat, rerolls while projectiles are in flight, reconnect or late join, and tooltip/combat-text updates on clients.

### 17. Audit per-player NPC state reset and array lifetime safety
Priority: Medium
Affected files: `Content/Prefixes/Common/WeaponPrefixGlobalNPC.cs`, `Content/Prefixes/Common/CatalyticRuntime.cs`, `Content/Prefixes/Common/TemporalRuntime.cs`, `Content/Prefixes/Common/VampiricRuntime.cs`, `Content/Prefixes/Common/AwakenedRuntime.cs`
Problem statement: `WeaponPrefixGlobalNPC` owns multiple per-player arrays and timers for different modifiers. The model is appropriate, but it deserves a lifetime audit to reduce stale state or cross-player leakage risk.
Proposed implementation plan: Review initialization, reset points, inactive-player handling, and NPC deactivation behavior for each array-backed system. Add internal comments describing which arrays are authoritative, temporary, or feedback-only.
Risk level: Medium
Validation steps: Test multiple players hitting the same NPC with different modifiers and confirm marks, temporal pressure, frenzy triggers, and awakened boss budgeting remain isolated per player.

### 18. Re-check parent/child projectile inheritance rules per modifier
Priority: Medium
Affected files: `Content/Prefixes/Common/WeaponPrefixProjectileDispatch.cs`, `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`, `Content/Prefixes/Common/FracturedRuntime.cs`, `Common/Combat/StormforgedChainHelper.cs`
Problem statement: Some modifiers intentionally inherit to child projectiles and others must stay item-sourced only. That distinction is one of the easiest places to introduce multiplayer or compatibility regressions.
Proposed implementation plan: Document which modifiers may inherit from parent projectiles, which must be initialized only from the source item, and which must never propagate. Convert that document into targeted validation cases before changing any propagation rule.
Risk level: Medium
Validation steps: Verify representative parent/child projectile chains in both single-player and multiplayer and confirm there are no unintended duplicate sustain, proc loops, or fracture cascades.

## Documentation tasks

### 19. Keep `MOZANDIFIERS_CONTEXT.md` and `MOZANDIFIERS_INDEX.md` aligned with current helper/file names
Priority: High
Affected files: `MOZANDIFIERS_CONTEXT.md`, `MOZANDIFIERS_INDEX.md`
Problem statement: The docs still mention `ShiftingSimulation.cs` and `ShiftingSimulationCatalog`, even though the repo now uses split helper files like `ShiftingSimulationTypes`, `ShiftingSimulationPools`, and `ShiftingSimulationDisplay`.
Proposed implementation plan: Update both docs so they reference the current file layout, current naming state, and current responsibility split. Add a small maintainer note that any architecture change should update both docs in the same task.
Risk level: Low
Validation steps: Cross-check every file or helper name mentioned in the docs against the current repo tree and confirm no stale references remain.

### 20. Add a repo-level maintenance note about non-prefix content boundaries
Priority: Low
Affected files: `MOZANDIFIERS_CONTEXT.md`, `README.md` if added later
Problem statement: The project is prefix-focused, but it also includes pets and starter-item content. Without a clear note, future contributors may over-couple those systems to prefix gameplay or ignore them entirely.
Proposed implementation plan: Add a short maintenance note clarifying that pets and starter items are secondary systems and should remain isolated unless a task explicitly ties them into prefix behavior.
Risk level: Low
Validation steps: Verify the docs communicate project scope clearly without implying new gameplay coupling.

## Testing checklist

### 21. Build a manual validation matrix for all live modifiers
Priority: High
Affected files: `TODO.md`, `MOZANDIFIERS_INDEX.md`
Problem statement: The project has many weapon-family- and runtime-specific branches, but no durable validation checklist tying each live modifier to representative weapons and edge cases.
Proposed implementation plan: Create a future test matrix covering each live modifier, at least one representative vanilla weapon per intended family, and one or two edge-case weapons where relevant. Include direct-hit, projectile, shifted, multiplayer, and boss/target-state checks where applicable.
Risk level: Low
Validation steps: Use the matrix before and after any modifier change and confirm every touched modifier has at least one successful regression pass.

### 22. Add a compatibility regression checklist for unusual vanilla weapons
Priority: Medium
Affected files: `TODO.md`, future docs or test notes
Problem statement: Several prefix systems are sensitive to vanilla quirks such as delayed projectiles, boomerang-style returns, channeling, no-use-graphic items, and mixed item/projectile behaviors.
Proposed implementation plan: Maintain a focused list of "weird but relevant" vanilla weapons to test before expanding support or changing roll gates. Keep the list scoped to current modifier support rather than all possible weapon families.
Risk level: Low
Validation steps: Confirm each future compatibility change is tested against the relevant edge-case weapon set before it is considered complete.

### 23. Add a release-time packaging and lock checklist
Priority: Low
Affected files: `TODO.md`
Problem statement: Local validation can compile successfully but still fail during packaging when `tModLoader` locks the `.tmod` output. This is already a recurring workflow issue.
Proposed implementation plan: Keep a short release checklist for future maintainers: build once with `tModLoader` closed, separate compile failures from packaging-lock failures, and record any intentionally deferred warnings.
Risk level: Low
Validation steps: Confirm future verification tasks report compile status and packaging-lock status separately.

## Future modifier ideas or expansion hooks

### 24. Prepare extension seams for future weapon-family expansion
Priority: Medium
Affected files: `Content/Prefixes/Common/WeaponPrefix.cs`, `Content/Prefixes/Common/WeaponPrefixIdentity.cs`, `Content/Prefixes/Common/WeaponPrefixGlobalItem.cs`, `Content/Prefixes/Common/WeaponPrefixGlobalProjectile.cs`
Problem statement: The current architecture is intentionally scoped to standard melee, ranged projectile, and magic paths, but future work may eventually target spears, flails, yoyos, or held/channelled projectile families.
Proposed implementation plan: Do not expand support yet. Instead, document the extension seams now: weapon-family predicates, item vs projectile ownership, child-projectile inheritance rules, and which modifiers are most likely to support a new family cleanly.
Risk level: Low
Validation steps: Confirm a future expansion task can point to concrete entry points without first untangling core architecture.

### 25. Prepare a reusable pattern guide for future stateful modifiers
Priority: Medium
Affected files: `Content/Prefixes/Common/*Runtime.cs`, `Common/Players/*`, `Content/Prefixes/Common/WeaponPrefixGlobalNPC.cs`, `MOZANDIFIERS_CONTEXT.md`
Problem statement: The mod already has repeatable patterns for owner-local state, target-persistent state, projectile snapshots, and dispatch/runtime separation, but those patterns are still implicit rather than documented as extension hooks.
Proposed implementation plan: Add a maintainer-facing pattern note describing when to use `ModPlayer`, `GlobalNPC`, item hooks, projectile hooks, or a runtime helper. Use existing modifiers like `Temporal`, `Awakened`, and `Catalytic` as examples instead of introducing a new framework.
Risk level: Low
Validation steps: Verify a future modifier task can cite a known ownership pattern before any new implementation starts.

## Open Questions

### A. Should `Attuned` stay eligible for all mana-using magic weapons?
Priority: Medium
Affected files: `Content/Prefixes/Weapons/AttunedPrefix.cs`, `Content/Prefixes/Common/WeaponPrefix.cs`
Problem statement: The current roll gate is broad, but the implementation fantasy is strongest on projectile magic weapons.
Proposed implementation plan: Decide whether the design goal is "all magic weapons," "projectile magic only," or "all magic with separate direct-cast handling." Defer gameplay changes until that direction is chosen.
Risk level: Medium
Validation steps: Review representative vanilla magic weapons and confirm the intended fantasy and payoff fit for each category.

### B. When should legacy config aliases be removed?
Priority: Low
Affected files: `Common/Config/PrefixTuningConfig.cs`
Problem statement: `Echoing` and `Colossal` aliases still exist, but there is no versioned deprecation target.
Proposed implementation plan: Choose a future release boundary for alias removal, then add a migration note and regression tests before touching the compatibility shim.
Risk level: Low
Validation steps: Confirm old config names continue to load until the chosen deprecation point.

### C. Which modifiers should receive the next polish budget first?
Priority: Medium
Affected files: `Content/Prefixes/Common/*Runtime.cs`, `Localization/en-US_Mods.Mozandifiers.hjson`
Problem statement: Several modifiers could benefit from stronger sound, visual, and tooltip feedback, but not all need the same level of polish immediately.
Proposed implementation plan: Choose a small next-wave polish set, likely from `Temporal`, `Vampiric`, `Fractured`, `Catalytic`, and `Spinbound`, based on player-facing clarity rather than code novelty.
Risk level: Low
Validation steps: After direction is chosen, verify the selected modifiers are more readable in live combat without changing balance.
