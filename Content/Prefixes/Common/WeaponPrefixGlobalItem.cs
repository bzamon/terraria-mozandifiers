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

		bool hasTemporalPrefix = HasTemporalPrefix(item);
		bool hasShiftedTemporal = TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)
			&& simulationId == ShiftingSimulationId.Temporal;
		if (hasTemporalPrefix || hasShiftedTemporal) {
			EmitTemporalUseFeedback(
				player,
				hasShiftedTemporal ? ShiftingPrefix.ShiftingStrengthMultiplier : 1f);
		}
	}

	public override void HoldItem(Item item, Player player)
	{
		if (HasRadiantPrefix(item)) {
			AddRadiantLight(player.MountedCenter, WeaponPrefixVisuals.GetRadiantPulse(player.whoAmI * 0.35f));
			return;
		}

		if (HasTemporalPrefix(item)) {
			AddTemporalLight(player.MountedCenter, WeaponPrefixVisuals.GetTemporalPulse(player.whoAmI * 0.27f));
			return;
		}

		if (TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)
			&& simulationId == ShiftingSimulationId.Radiant) {
			AddRadiantLight(
				player.MountedCenter,
				WeaponPrefixVisuals.GetRadiantPulse(player.whoAmI * 0.35f) * ShiftingSimulationCatalog.GetShiftedRadiantLightMultiplier());
			return;
		}

		if (simulationId == ShiftingSimulationId.Temporal) {
			AddTemporalLight(
				player.MountedCenter,
				WeaponPrefixVisuals.GetTemporalPulse(player.whoAmI * 0.27f) * ShiftingPrefix.ShiftingStrengthMultiplier);
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
		SpinboundPlayer spinboundPlayer = player.GetModPlayer<SpinboundPlayer>();
		bool hasSpinboundPrefix = HasSpinboundPrefix(item);
		bool hasShiftedSpinbound = TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)
			&& simulationId == ShiftingSimulationId.Spinbound;

		if (hasShiftedSpinbound) {
			velocity *= ShiftingSimulationCatalog.GetShiftedShootSpeedMultiplier(simulationId);
		}

		if ((hasSpinboundPrefix || hasShiftedSpinbound) && player.active && !player.dead) {
			if (hasSpinboundPrefix) {
				spinboundPlayer.ResolveSpinboundShot(item, velocity, false);
			}

			if (hasShiftedSpinbound) {
				spinboundPlayer.ResolveSpinboundShot(item, velocity, true);
			}
		}

		if (!hasShiftedSpinbound) {
			return;
		}
	}

	public override void ModifyItemScale(Item item, Player player, ref float scale)
	{
		if (HasBreachingPrefix(item) && BreachingPrefix.SupportsMergedScale(item)) {
			scale *= BreachingPrefix.GetMergedScaleMultiplier(PrefixTuningConfig.Instance.ColossalScaleMultiplier);
		}

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

		if (HasBreachingPrefix(item)) {
			TryTriggerBreachingImpact(
				player,
				target,
				new Vector2(hit.HitDirection, 0f),
				damageDone,
				BreachingPrefix.GetBreachImpactMultiplier(item));
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
			return;
		}

		if (simulationId == ShiftingSimulationId.Breaching) {
			TryTriggerBreachingImpact(
				player,
				target,
				new Vector2(hit.HitDirection, 0f),
				damageDone,
				BreachingPrefix.GetBreachImpactMultiplier(item) * ShiftingPrefix.ShiftingStrengthMultiplier);
		}
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

	private static bool HasSpinboundPrefix(Item item)
	{
		return item.prefix == ModContent.PrefixType<SpinboundPrefix>();
	}

	private static bool HasBreachingPrefix(Item item)
	{
		return item.prefix == ModContent.PrefixType<BreachingPrefix>();
	}

	private static bool HasTemporalPrefix(Item item)
	{
		return item.prefix == ModContent.PrefixType<TemporalPrefix>();
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
		ApplyRadiantHitEffects(target, RadiantPrefix.OnFireDurationTicks, 1f);
	}

	internal static void ApplyRadiantHitEffects(NPC target, int durationTicks)
	{
		ApplyRadiantHitEffects(target, durationTicks, 1f);
	}

	internal static void ApplyRadiantHitEffects(NPC target, int durationTicks, float visualMultiplier)
	{
		target.AddBuff(BuffID.OnFire, durationTicks);
		SpawnRadiantHitEffect(target, visualMultiplier);
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
		Lighting.AddLight(position, 1f * multiplier, 0.74f * multiplier, 0.24f * multiplier);
	}

	private static void AddTemporalLight(Vector2 position, float multiplier = 1f)
	{
		Lighting.AddLight(position, 0.18f * multiplier, 0.26f * multiplier, 0.38f * multiplier);
	}

	private static void EmitTemporalUseFeedback(Player player, float visualMultiplier)
	{
		if (player == null
			|| !player.active
			|| player.dead
			|| player.whoAmI != Main.myPlayer
			|| !player.GetModPlayer<TemporalFeedbackPlayer>().CanEmitUsePulse()) {
			return;
		}

		Vector2 center = player.MountedCenter + new Vector2(player.direction * 12f, -6f);
		float pulse = WeaponPrefixVisuals.GetTemporalPulse(player.whoAmI * 0.41f) * visualMultiplier;
		Lighting.AddLight(center, 0.16f * pulse, 0.24f * pulse, 0.35f * pulse);

		int dustCount = System.Math.Max(3, (int)System.MathF.Round(4f * visualMultiplier));
		for (int i = 0; i < dustCount; i++) {
			Vector2 offset = Main.rand.NextVector2Circular(10f, 12f);
			Vector2 velocity = new Vector2(player.direction * Main.rand.NextFloat(0.6f, 1.2f), 0f) - offset * 0.035f;
			Dust dust = Dust.NewDustPerfect(center + offset, WeaponPrefixVisuals.TemporalDustType, velocity);
			dust.noGravity = true;
			dust.scale = Main.rand.NextFloat(0.82f, 1.05f) * visualMultiplier;
			dust.fadeIn = 0.95f;
		}
	}

	internal static void TryTriggerBreachingImpact(Player player, NPC target, Vector2 impactDirection, int damageDone, float impactScale)
	{
		if (player == null
			|| !player.active
			|| player.dead
			|| player.whoAmI != Main.myPlayer
			|| target == null
			|| !target.active
			|| damageDone <= 0) {
			return;
		}

		bool resistantTarget = target.defense >= BreachingPrefix.BreachDefenseThreshold
			|| target.boss
			|| target.knockBackResist <= 0.4f;
		bool heavyDamage = damageDone >= BreachingPrefix.BreachDamageThreshold;
		if (!resistantTarget && !heavyDamage) {
			return;
		}

		BreachingFeedbackPlayer feedbackPlayer = player.GetModPlayer<BreachingFeedbackPlayer>();
		float visualMultiplier = impactScale * (resistantTarget ? 1.08f : 1f);
		if (feedbackPlayer.CanEmitBreachVisual()) {
			SpawnBreachingImpactEffect(target.Center, impactDirection, visualMultiplier);
		}
	}

	private static void SpawnBreachingImpactEffect(Vector2 position, Vector2 impactDirection, float visualMultiplier)
	{
		Vector2 direction = impactDirection.LengthSquared() > 0.0001f
			? impactDirection.SafeNormalize(Vector2.UnitX)
			: Vector2.UnitX;
		Vector2 tangent = direction.RotatedBy(MathHelper.PiOver2);
		Lighting.AddLight(position, 0.34f * visualMultiplier, 0.24f * visualMultiplier, 0.11f * visualMultiplier);

		int debrisCount = System.Math.Max(3, (int)System.MathF.Round(4f * visualMultiplier));
		for (int i = 0; i < debrisCount; i++) {
			float side = i % 2 == 0 ? -1f : 1f;
			float spread = 1f + i / 2f;
			Dust debrisDust = Dust.NewDustPerfect(
				position + tangent * (3f * spread * side),
				WeaponPrefixVisuals.BreachingDustType,
				direction * Main.rand.NextFloat(0.8f, 1.8f) + tangent * (0.25f * spread * side),
				0,
				WeaponPrefixVisuals.BreachingDustColor,
				0.9f * visualMultiplier);
			debrisDust.noGravity = true;
			debrisDust.fadeIn = 0.95f;
		}

		int smokeCount = System.Math.Max(2, (int)System.MathF.Round(2.5f * visualMultiplier));
		for (int i = 0; i < smokeCount; i++) {
			Vector2 offset = Main.rand.NextVector2Circular(6f, 6f);
			Dust smokeDust = Dust.NewDustPerfect(
				position + offset,
				WeaponPrefixVisuals.BreachingSmokeDustType,
				direction * Main.rand.NextFloat(0.35f, 0.7f) + Main.rand.NextVector2Circular(0.4f, 0.4f),
				0,
				WeaponPrefixVisuals.BreachingSmokeColor,
				0.85f * visualMultiplier);
			smokeDust.fadeIn = 1.05f;
		}

		int streakCount = System.Math.Max(1, (int)System.MathF.Round(1.25f * visualMultiplier));
		for (int i = 0; i < streakCount; i++) {
			Vector2 streakOffset = direction * (4f + i * 4f);
			Dust streakDust = Dust.NewDustPerfect(
				position + streakOffset,
				WeaponPrefixVisuals.BreachingDustType,
				direction * Main.rand.NextFloat(1.1f, 1.8f),
				0,
				WeaponPrefixVisuals.BreachingDustColor,
				0.8f * visualMultiplier);
			streakDust.noGravity = true;
			streakDust.fadeIn = 0.9f;
		}
	}

	private static void SpawnRadiantHitEffect(NPC target, float visualMultiplier)
	{
		if (target == null || !target.active) {
			return;
		}

		Lighting.AddLight(target.Center, 0.68f * visualMultiplier, 0.46f * visualMultiplier, 0.12f * visualMultiplier);

		int dustCount = System.Math.Max(4, (int)System.MathF.Round(5f * visualMultiplier));
		for (int i = 0; i < dustCount; i++) {
			Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1f, 2.3f) * visualMultiplier;
			Dust dust = Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Circular(10f, 12f), WeaponPrefixVisuals.RadiantDustType, velocity);
			dust.noGravity = true;
			dust.scale = Main.rand.NextFloat(0.95f, 1.2f) * visualMultiplier;
			dust.fadeIn = 1.05f;
		}
	}
}
