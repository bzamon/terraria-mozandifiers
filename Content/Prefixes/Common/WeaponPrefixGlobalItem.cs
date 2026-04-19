using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Mozandifiers.Common.Combat;
using Mozandifiers.Common.Config;
using Mozandifiers.Common.Players;
using Mozandifiers.Content.Prefixes.Weapons;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Common;

public sealed class WeaponPrefixGlobalItem : GlobalItem
{
	public override void UseAnimation(Item item, Player player)
	{
		if (HasShiftingPrefix(item)) {
			player.GetModPlayer<ShiftingPlayer>().RegisterWeaponUse(item);
		}
	}

	public override void HoldItem(Item item, Player player)
	{
		if (HasRadiantPrefix(item)) {
			AddRadiantLight(player.MountedCenter);
			return;
		}

		if (TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)
			&& simulationId == ShiftingSimulationId.Radiant) {
			AddRadiantLight(player.MountedCenter, ShiftingSimulationCatalog.GetShiftedRadiantLightMultiplier());
		}
	}

	public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
	{
		if (!HasDesperatePrefix(item)) {
			if (!TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)) {
				return;
			}

			damage *= ShiftingSimulationCatalog.GetShiftedDamageMultiplier(simulationId);
			if (simulationId == ShiftingSimulationId.Desperate) {
				damage *= GetDesperateDamageMultiplier(player, ShiftingSimulationCatalog.GetShiftedDesperateMaxDamageBonus());
			}

			return;
		}

		damage *= GetDesperateDamageMultiplier(player, DesperatePrefix.MaxDamageBonus);
	}

	public override void ModifyWeaponCrit(Item item, Player player, ref float crit)
	{
		if (!TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)) {
			return;
		}

		crit += ShiftingSimulationCatalog.GetShiftedCritBonus(simulationId);
	}

	public override void ModifyWeaponKnockback(Item item, Player player, ref StatModifier knockback)
	{
		if (!TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)) {
			return;
		}

		knockback *= ShiftingSimulationCatalog.GetShiftedKnockbackMultiplier(simulationId);
	}

	public override void ModifyManaCost(Item item, Player player, ref float reduce, ref float mult)
	{
		if (!TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)) {
			return;
		}

		mult *= ShiftingSimulationCatalog.GetShiftedManaCostMultiplier(simulationId);
	}

	public override float UseTimeMultiplier(Item item, Player player)
	{
		if (!TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)) {
			return 1f;
		}

		return ShiftingSimulationCatalog.GetShiftedUseSpeedMultiplier(simulationId);
	}

	public override float UseAnimationMultiplier(Item item, Player player)
	{
		if (!TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)) {
			return 1f;
		}

		return ShiftingSimulationCatalog.GetShiftedUseSpeedMultiplier(simulationId);
	}

	public override void ModifyShootStats(Item item, Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
	{
		if (!TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)) {
			return;
		}

		velocity *= ShiftingSimulationCatalog.GetShiftedShootSpeedMultiplier(simulationId);
	}

	public override void ModifyItemScale(Item item, Player player, ref float scale)
	{
		if (!TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)) {
			return;
		}

		scale *= ShiftingSimulationCatalog.GetShiftedScaleMultiplier(
			simulationId,
			PrefixTuningConfig.Instance.ColossalScaleMultiplier);
	}

	public override void ModifyHitNPC(Item item, Player player, NPC target, ref NPC.HitModifiers modifiers)
	{
		if (!TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)) {
			return;
		}

		if (simulationId == ShiftingSimulationId.Breaching || simulationId == ShiftingSimulationId.Spinbound) {
			modifiers.ArmorPenetration += simulationId == ShiftingSimulationId.Breaching
				? ShiftingSimulationCatalog.GetShiftedArmorPenetrationBonus(simulationId)
				: ShiftingSimulationCatalog.GetShiftedSpinboundArmorPenetrationBonus();
		}
	}

	public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
	{
		Player localPlayer = Main.LocalPlayer;
		if (localPlayer == null
			|| !localPlayer.active
			|| !TryGetShiftingSimulation(item, localPlayer, out ShiftingSimulationId simulationId)) {
			return;
		}

		string simulationName = ShiftingSimulationCatalog.GetDisplayName(Mod.Name, simulationId);
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
		if (HasShiftingPrefix(item) && damageDone > 0) {
			player.GetModPlayer<ShiftingPlayer>().RegisterSuccessfulHit(item);
		}

		if (HasSanguinePrefix(item)) {
			TryApplySanguineHeal(player, damageDone);
		}

		if (HasRadiantPrefix(item) && target.active) {
			ApplyRadiantHitEffects(target);
		}

		if (HasStormforgedPrefix(item)) {
			StormforgedChainHelper.TryTriggerChain(player, target, damageDone);
		}

		if (!TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)) {
			return;
		}

		if (simulationId == ShiftingSimulationId.Sanguine) {
			TryApplySanguineHeal(
				player,
				damageDone,
				ShiftingSimulationCatalog.GetShiftedSanguineLifeStealMultiplier(),
				ShiftingSimulationCatalog.GetShiftedSanguineHealCapPerSecond());
		}

		if (simulationId == ShiftingSimulationId.Radiant && target.active) {
			ApplyRadiantHitEffects(target, ShiftingSimulationCatalog.GetShiftedRadiantDurationTicks());
			return;
		}

		if (simulationId == ShiftingSimulationId.Stormforged) {
			StormforgedChainHelper.TryTriggerChain(
				player,
				target,
				damageDone,
				ShiftingSimulationCatalog.GetShiftedStormforgedProcChance(),
				ShiftingSimulationCatalog.GetShiftedStormforgedMaxChainJumps(),
				ShiftingSimulationCatalog.GetShiftedStormforgedChainRangePixels(),
				ShiftingSimulationCatalog.GetShiftedStormforgedChainDamageDecay());
		}
	}

	private static bool HasEchoingPrefix(Item item)
	{
		return item.prefix == ModContent.PrefixType<EchoingPrefix>();
	}

	private static bool HasSanguinePrefix(Item item)
	{
		return item.prefix == ModContent.PrefixType<SanguinePrefix>();
	}

	private static bool HasRadiantPrefix(Item item)
	{
		return item.prefix == ModContent.PrefixType<RadiantPrefix>();
	}

	private static bool HasStormforgedPrefix(Item item)
	{
		return item.prefix == ModContent.PrefixType<StormforgedPrefix>();
	}

	private static bool HasDesperatePrefix(Item item)
	{
		return item.prefix == ModContent.PrefixType<DesperatePrefix>();
	}

	private static bool HasShiftingPrefix(Item item)
	{
		return item.prefix == ModContent.PrefixType<ShiftingPrefix>();
	}

	private static bool TryGetShiftingSimulation(Item item, Player player, out ShiftingSimulationId simulationId)
	{
		simulationId = ShiftingSimulationId.None;
		return player != null
			&& player.active
			&& player.GetModPlayer<ShiftingPlayer>().TryGetActiveSimulation(item, out simulationId);
	}

	internal static void TryApplySanguineHeal(Player player, int damageDone)
	{
		TryApplySanguineHeal(player, damageDone, SanguinePrefix.LifeStealMultiplier, 5);
	}

	internal static void TryApplySanguineHeal(Player player, int damageDone, float lifeStealMultiplier, int healCapPerSecond)
	{
		if (player.whoAmI != Main.myPlayer || damageDone <= 0) {
			return;
		}

		int requestedHeal = System.Math.Max(
			1,
			(int)System.MathF.Round(damageDone * lifeStealMultiplier));
		int healAmount = player.GetModPlayer<SanguineHealPlayer>().ConsumeAvailableHeal(requestedHeal, healCapPerSecond);
		if (healAmount <= 0) {
			return;
		}

		player.Heal(healAmount);
	}

	internal static void ApplyRadiantHitEffects(NPC target)
	{
		ApplyRadiantHitEffects(target, RadiantPrefix.OnFireDurationTicks);
	}

	internal static void ApplyRadiantHitEffects(NPC target, int durationTicks)
	{
		target.AddBuff(BuffID.OnFire, durationTicks);
	}

	internal static int GetSiphoningManaRestoreAmount(Item item)
	{
		return GetSiphoningManaRestoreAmount(item, SiphoningPrefix.ManaRestoreFromManaCost, SiphoningPrefix.MaxManaRestorePerHit);
	}

	internal static int GetSiphoningManaRestoreAmount(Item item, float manaRestoreRatio, int maxManaRestorePerHit)
	{
		if (item == null || item.IsAir || item.mana <= 0) {
			return 0;
		}

		return System.Math.Min(
			maxManaRestorePerHit,
			System.Math.Max(
				1,
				(int)System.MathF.Round(item.mana * manaRestoreRatio)));
	}

	internal static int TryApplySiphoningManaRestore(Player player, int requestedManaRestore)
	{
		return TryApplySiphoningManaRestore(player, requestedManaRestore, SiphoningPrefix.MaxManaRestorePerSecond);
	}

	internal static int TryApplySiphoningManaRestore(Player player, int requestedManaRestore, int maxManaRestorePerSecond)
	{
		if (player.whoAmI != Main.myPlayer || requestedManaRestore <= 0) {
			return 0;
		}

		int missingMana = player.statManaMax2 - player.statMana;
		if (missingMana <= 0) {
			return 0;
		}

		int effectiveRequestedRestore = System.Math.Min(requestedManaRestore, missingMana);
		
		int manaRestoreAmount = player.GetModPlayer<SiphoningPlayer>().ConsumeAvailableManaRestore(
			effectiveRequestedRestore,
			maxManaRestorePerSecond);

		if (manaRestoreAmount <= 0) {
			return 0;
		}

		player.statMana += manaRestoreAmount;
		player.ManaEffect(manaRestoreAmount);
		return manaRestoreAmount;
	}

	private static float GetDesperateDamageMultiplier(Player player, float maxDamageBonus)
	{
		if (player.statLifeMax2 <= 0) {
			return 1f;
		}

		float currentLifeRatio = player.statLife / (float)player.statLifeMax2;
		float missingLifeRatio = 1f - currentLifeRatio;
		float bonusMultiplier = System.MathF.Min(maxDamageBonus, missingLifeRatio / 0.9f);
		return 1f + bonusMultiplier;
	}

	private static void AddRadiantLight(Vector2 position, float multiplier = 1f)
	{
		Lighting.AddLight(position, 0.75f * multiplier, 0.55f * multiplier, 0.18f * multiplier);
	}
}
