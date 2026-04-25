using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Mozandifiers.Common.Config;
using Mozandifiers.Common.Players;
using Mozandifiers.Content.Buffs;
using Mozandifiers.Content.Prefixes.Weapons;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Common;

public sealed class WeaponPrefixGlobalItem : GlobalItem
{
	public override void UseAnimation(Item item, Player player)
	{
		if (WeaponPrefixIdentity.HasShiftingPrefix(item)) {
			player.GetModPlayer<ShiftingPlayer>().RegisterWeaponUse(item);
		}

		bool hasTemporalPrefix = WeaponPrefixIdentity.HasTemporalPrefix(item);
		bool hasShiftedTemporal = WeaponPrefixIdentity.TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)
			&& simulationId == ShiftingSimulationId.Temporal;
		if (hasTemporalPrefix || hasShiftedTemporal) {
			TemporalRuntime.EmitUseFeedback(
				player,
				hasShiftedTemporal ? ShiftingPrefix.ShiftingStrengthMultiplier : 1f);
		}
	}

	public override void HoldItem(Item item, Player player)
	{
		WeaponPrefixItemHoldDispatch.Dispatch(item, player);
	}

	public override void UseItemHitbox(Item item, Player player, ref Rectangle hitbox, ref bool noHitbox)
	{
		WeaponPrefixItemSwingDispatch.UseItemHitbox(item, player, hitbox, noHitbox);
	}

	public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
	{
		if (WeaponPrefixIdentity.HasAwakenedPrefix(item)) {
			damage *= player.GetModPlayer<AwakenedPlayer>().GetCurrentDamageMultiplier();
		}

		if (!WeaponPrefixIdentity.HasDesperatePrefix(item)) {
			if (!WeaponPrefixIdentity.TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)) {
				return;
			}

			damage *= ShiftingSimulationStatScaling.GetShiftedDamageMultiplier(simulationId);
			if (simulationId == ShiftingSimulationId.Desperate) {
				damage *= GetDesperateDamageMultiplier(player, ShiftingSimulationEffectScaling.GetShiftedDesperateMaxDamageBonus());
			}

			return;
		}

		damage *= GetDesperateDamageMultiplier(player, DesperatePrefix.MaxDamageBonus);
	}

	public override void ModifyWeaponCrit(Item item, Player player, ref float crit)
	{
		if (!WeaponPrefixIdentity.TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)) {
			return;
		}

		crit += ShiftingSimulationStatScaling.GetShiftedCritBonus(simulationId);
	}

	public override void ModifyWeaponKnockback(Item item, Player player, ref StatModifier knockback)
	{
		if (!WeaponPrefixIdentity.TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)) {
			return;
		}

		knockback *= ShiftingSimulationStatScaling.GetShiftedKnockbackMultiplier(simulationId);
	}

	public override float UseSpeedMultiplier(Item item, Player player)
	{
		float useSpeedMultiplier = 1f;
		if (WeaponPrefixIdentity.HasAwakenedPrefix(item)) {
			useSpeedMultiplier *= player.GetModPlayer<AwakenedPlayer>().GetCurrentUseSpeedMultiplier();
		}

		VampiricPlayer vampiricPlayer = player.GetModPlayer<VampiricPlayer>();
		if (!vampiricPlayer.IsFrenzied) {
			return useSpeedMultiplier;
		}

		if (WeaponPrefixIdentity.HasVampiricPrefix(item)) {
			useSpeedMultiplier *= 1f + vampiricPlayer.GetFrenzyAttackSpeedBonus();
			return useSpeedMultiplier;
		}

		if (WeaponPrefixIdentity.TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)
			&& simulationId == ShiftingSimulationId.Vampiric) {
			useSpeedMultiplier *= 1f + (vampiricPlayer.GetFrenzyAttackSpeedBonus() * ShiftingPrefix.ShiftingStrengthMultiplier);
		}

		return useSpeedMultiplier;
	}

	public override void ModifyManaCost(Item item, Player player, ref float reduce, ref float mult)
	{
		bool hasAttunedPrefix = WeaponPrefixIdentity.HasAttunedPrefix(item);
		bool hasShiftedAttuned = WeaponPrefixIdentity.TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)
			&& simulationId == ShiftingSimulationId.Attuned;
		if (hasAttunedPrefix || hasShiftedAttuned) {
			AttunedPlayer attunedPlayer = player.GetModPlayer<AttunedPlayer>();
			bool empoweredCast = attunedPlayer.RegisterQualifyingCast();
			if (hasShiftedAttuned) {
				mult *= ShiftingSimulationStatScaling.GetShiftedManaCostMultiplier(simulationId);
			}

			if (empoweredCast) {
				reduce = 1f;
				mult = 0f;
				WeaponPrefixItemHoldDispatch.EmitAttunedCastReleaseFeedback(
					player,
					hasShiftedAttuned ? ShiftingPrefix.ShiftingStrengthMultiplier : 1f);
			}
			return;
		}

		if (!WeaponPrefixIdentity.TryGetShiftingSimulation(item, player, out simulationId)) {
			return;
		}

		mult *= ShiftingSimulationStatScaling.GetShiftedManaCostMultiplier(simulationId);
	}

	public override float UseTimeMultiplier(Item item, Player player)
	{
		if (!WeaponPrefixIdentity.TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)) {
			return 1f;
		}

		return ShiftingSimulationStatScaling.GetShiftedUseSpeedMultiplier(simulationId);
	}

	public override float UseAnimationMultiplier(Item item, Player player)
	{
		if (!WeaponPrefixIdentity.TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)) {
			return 1f;
		}

		return ShiftingSimulationStatScaling.GetShiftedUseSpeedMultiplier(simulationId);
	}

	public override void ModifyShootStats(Item item, Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
	{
		WeaponPrefixItemShootDispatch.Dispatch(item, player, ref position, ref velocity, ref type, ref damage, ref knockback);
	}

	public override void ModifyItemScale(Item item, Player player, ref float scale)
	{
		if (WeaponPrefixIdentity.HasAwakenedPrefix(item)) {
			scale *= player.GetModPlayer<AwakenedPlayer>().GetCurrentScaleMultiplier();
		}

		if (WeaponPrefixIdentity.HasBreachingPrefix(item) && BreachingPrefix.SupportsMergedScale(item)) {
			scale *= BreachingPrefix.GetMergedScaleMultiplier(PrefixTuningConfig.Instance.BreachingScaleMultiplier);
		}

		if (!WeaponPrefixIdentity.TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)) {
			return;
		}

		scale *= ShiftingSimulationStatScaling.GetShiftedScaleMultiplier(
			simulationId,
			PrefixTuningConfig.Instance.BreachingScaleMultiplier);
	}

	public override void ModifyHitNPC(Item item, Player player, NPC target, ref NPC.HitModifiers modifiers)
	{
		WeaponPrefixItemHitDispatch.ModifyHitNPC(item, player, target, ref modifiers);
	}

	public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
	{
		Player localPlayer = Main.LocalPlayer;
		if (localPlayer == null || !localPlayer.active) {
			return;
		}

		if (WeaponPrefixIdentity.HasAwakenedPrefix(item) && !localPlayer.HasBuff(ModContent.BuffType<AwakenedBuff>())) {
			ReplaceAwakenedNameWithDormant(tooltips);
		}

		if (!WeaponPrefixIdentity.TryGetShiftingSimulation(item, localPlayer, out ShiftingSimulationId simulationId)) {
			return;
		}

		string simulationName = ShiftingSimulationDisplay.GetDisplayName(Mod.Name, simulationId);
		if (string.IsNullOrEmpty(simulationName)) {
			return;
		}

		tooltips.Add(new TooltipLine(
			Mod,
			"ShiftingCurrentSimulation",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.ShiftingPrefix.CurrentTooltip",
				simulationName,
				(int)(ShiftingPrefix.ShiftingStrengthMultiplier * 100f))));
	}

	public override void OnHitNPC(Item item, Player player, NPC target, NPC.HitInfo hit, int damageDone)
	{
		WeaponPrefixItemHitDispatch.OnHitNPC(item, player, target, hit, damageDone);
	}

	private static float GetDesperateDamageMultiplier(Player player, float maxDamageBonus)
	{
		if (player == null || !player.active || player.statLifeMax2 <= 0) {
			return 1f;
		}

		float missingLifeRatio = 1f - player.statLife / (float)player.statLifeMax2;
		return 1f + maxDamageBonus * MathHelper.Clamp(missingLifeRatio, 0f, 1f);
	}

	private void ReplaceAwakenedNameWithDormant(List<TooltipLine> tooltips)
	{
		string awakenedName = Language.GetTextValue($"Mods.{Mod.Name}.Prefixes.AwakenedPrefix.DisplayName");
		string dormantName = Language.GetTextValue($"Mods.{Mod.Name}.Prefixes.AwakenedPrefix.DormantDisplayName");
		if (string.IsNullOrWhiteSpace(awakenedName) || string.IsNullOrWhiteSpace(dormantName)) {
			return;
		}

		for (int i = 0; i < tooltips.Count; i++) {
			TooltipLine line = tooltips[i];
			if (line.Mod != "Terraria" || line.Name != "ItemName" || string.IsNullOrEmpty(line.Text)) {
				continue;
			}

			string awakenedPrefix = awakenedName + " ";
			if (!line.Text.StartsWith(awakenedPrefix, System.StringComparison.Ordinal)) {
				continue;
			}

			line.Text = dormantName + line.Text[awakenedName.Length..];
			tooltips[i] = line;
			return;
		}
	}
}
