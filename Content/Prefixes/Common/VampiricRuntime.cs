using Microsoft.Xna.Framework;
using Mozandifiers.Common.Players;
using Mozandifiers.Content.Prefixes.Weapons;
using Terraria;
using Terraria.ID;

namespace Mozandifiers.Content.Prefixes.Common;

internal static class VampiricRuntime
{
	internal static void ApplyItemFeed(Player player, Item item, NPC target, int damageDone, bool crit, float strengthMultiplier = 1f, bool shifted = false)
	{
		if (player == null
			|| !player.active
			|| player.dead
			|| item == null
			|| item.IsAir
			|| damageDone <= 0) {
			return;
		}

		VampiricPlayer vampiricPlayer = player.GetModPlayer<VampiricPlayer>();
		VampiricPreyState preyState = vampiricPlayer.ConsumePendingDirectPreyState();
		if (preyState == VampiricPreyState.None && target != null && target.lifeMax > 0) {
			preyState = SanguinePrefix.GetPreyState((target.life + damageDone) / (float)target.lifeMax);
		}

		float feedMultiplier = SanguinePrefix.GetFeedMultiplier(preyState);
		if (crit) {
			feedMultiplier *= SanguinePrefix.CriticalFeedMultiplier;
		}

		float lifeStealMultiplier = shifted
			? ShiftingSimulationEffectScaling.GetShiftedVampiricLifeStealMultiplier()
			: SanguinePrefix.LifeStealMultiplier;
		int healCapPerSecond = shifted
			? ShiftingSimulationEffectScaling.GetShiftedVampiricHealCapPerSecond()
			: SanguinePrefix.BaseHealCapPerSecond;
		int healAmount = TryApplyHeal(player, damageDone, lifeStealMultiplier * feedMultiplier, healCapPerSecond);

		int manaRestoreAmount = 0;
		bool magicWeapon = WeaponPrefix.IsMagicWeapon(item);
		if (magicWeapon) {
			float manaRestoreRatio = shifted
			? ShiftingSimulationEffectScaling.GetShiftedVampiricManaRestoreRatio()
				: SanguinePrefix.ManaRestoreFromManaCost;
			int maxManaRestorePerHit = shifted
			? ShiftingSimulationEffectScaling.GetShiftedVampiricMaxRestorePerHit()
				: SanguinePrefix.MaxManaRestorePerHit;
			int maxManaRestorePerSecond = shifted
			? ShiftingSimulationEffectScaling.GetShiftedVampiricMaxRestorePerSecond()
				: SanguinePrefix.BaseManaRestorePerSecond;
			int requestedManaRestore = System.Math.Max(
				1,
				(int)System.MathF.Round(GetManaRestoreAmount(item, manaRestoreRatio, maxManaRestorePerHit) * feedMultiplier));
			manaRestoreAmount = TryApplyManaRestore(player, requestedManaRestore, maxManaRestorePerSecond);
		}

		bool triggeredFrenzy = TryTriggerFrenzy(player, target, preyState);
		if (triggeredFrenzy) {
			vampiricPlayer.TriggerFrenzy(preyState, shifted ? strengthMultiplier : 1f);
		}

		if ((healAmount > 0 || manaRestoreAmount > 0 || triggeredFrenzy)
			&& player.whoAmI == Main.myPlayer) {
			SpawnFeedEffect(
				player,
				target,
				preyState,
				healAmount,
				manaRestoreAmount,
				crit,
				triggeredFrenzy,
				shifted ? strengthMultiplier : 1f);
		}
	}

	internal static void ApplyProjectileFeed(
		Player player,
		NPC target,
		int damageDone,
		bool crit,
		VampiricPreyState preyState,
		int baseManaRestoreAmount,
		float strengthMultiplier = 1f,
		bool shifted = false)
	{
		if (player == null || !player.active || player.dead || damageDone <= 0) {
			return;
		}

		float feedMultiplier = SanguinePrefix.GetFeedMultiplier(preyState);
		if (crit) {
			feedMultiplier *= SanguinePrefix.CriticalFeedMultiplier;
		}

		float lifeStealMultiplier = shifted
			? ShiftingSimulationEffectScaling.GetShiftedVampiricLifeStealMultiplier()
			: SanguinePrefix.LifeStealMultiplier;
		int healCapPerSecond = shifted
			? ShiftingSimulationEffectScaling.GetShiftedVampiricHealCapPerSecond()
			: SanguinePrefix.BaseHealCapPerSecond;
		int healAmount = TryApplyHeal(player, damageDone, lifeStealMultiplier * feedMultiplier, healCapPerSecond);

		int manaRestoreAmount = 0;
		if (baseManaRestoreAmount > 0) {
			int maxManaRestorePerSecond = shifted
			? ShiftingSimulationEffectScaling.GetShiftedVampiricMaxRestorePerSecond()
				: SanguinePrefix.BaseManaRestorePerSecond;
			int requestedManaRestore = System.Math.Max(1, (int)System.MathF.Round(baseManaRestoreAmount * feedMultiplier));
			manaRestoreAmount = TryApplyManaRestore(player, requestedManaRestore, maxManaRestorePerSecond);
		}

		VampiricPlayer vampiricPlayer = player.GetModPlayer<VampiricPlayer>();
		bool triggeredFrenzy = TryTriggerFrenzy(player, target, preyState);
		if (triggeredFrenzy) {
			vampiricPlayer.TriggerFrenzy(preyState, shifted ? strengthMultiplier : 1f);
		}

		if ((healAmount > 0 || manaRestoreAmount > 0 || triggeredFrenzy)
			&& player.whoAmI == Main.myPlayer
			&& target != null
			&& target.active) {
			SpawnFeedEffect(
				player,
				target,
				preyState,
				healAmount,
				manaRestoreAmount,
				crit,
				triggeredFrenzy,
				shifted ? strengthMultiplier : 1f);
		}
	}

	internal static void TryApplyFrenzyCrit(Player player, VampiricPreyState preyState, ref NPC.HitModifiers modifiers, float strengthMultiplier)
	{
		if (player == null
			|| !player.active
			|| player.dead
			|| preyState == VampiricPreyState.None
			|| !player.GetModPlayer<VampiricPlayer>().IsFrenzied) {
			return;
		}

		VampiricPlayer vampiricPlayer = player.GetModPlayer<VampiricPlayer>();
		int critBonus = (int)System.MathF.Round(SanguinePrefix.GetFrenzyCritBonus(preyState) * vampiricPlayer.GetFrenzyTierScale());
		if (critBonus <= 0) {
			return;
		}

		float critChance = System.MathF.Min(0.95f, (critBonus * System.MathF.Max(1f, strengthMultiplier)) / 100f);
		if (Main.rand.NextFloat() < critChance) {
			modifiers.SetCrit();
		}
	}

	internal static void TryApplyProjectileFrenzyCrit(Projectile projectile, VampiricPreyState preyState, ref NPC.HitModifiers modifiers, float strengthMultiplier)
	{
		if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers || preyState == VampiricPreyState.None) {
			return;
		}

		Player owner = Main.player[projectile.owner];
		if (!owner.active || owner.dead) {
			return;
		}

		TryApplyFrenzyCrit(owner, preyState, ref modifiers, strengthMultiplier);
	}

	internal static int TryApplyHeal(Player player, int damageDone, float lifeStealMultiplier, int healCapPerSecond)
	{
		if (player.whoAmI != Main.myPlayer || damageDone <= 0 || lifeStealMultiplier <= 0f) {
			return 0;
		}

		int requestedHeal = System.Math.Max(1, (int)System.MathF.Round(damageDone * lifeStealMultiplier));
		int healAmount = player.GetModPlayer<VampiricPlayer>().ConsumeAvailableHeal(requestedHeal, healCapPerSecond);
		if (healAmount <= 0) {
			return 0;
		}

		player.Heal(healAmount);
		return healAmount;
	}

	internal static int GetManaRestoreAmount(Item item)
	{
		return GetManaRestoreAmount(item, SanguinePrefix.ManaRestoreFromManaCost, SanguinePrefix.MaxManaRestorePerHit);
	}

	internal static int GetManaRestoreAmount(Item item, float manaRestoreRatio, int maxManaRestorePerHit)
	{
		if (item == null || item.IsAir || item.mana <= 0 || manaRestoreRatio <= 0f || maxManaRestorePerHit <= 0) {
			return 0;
		}

		return System.Math.Min(
			maxManaRestorePerHit,
			System.Math.Max(1, (int)System.MathF.Round(item.mana * manaRestoreRatio)));
	}

	internal static int TryApplyManaRestore(Player player, int requestedManaRestore, int maxManaRestorePerSecond)
	{
		if (player.whoAmI != Main.myPlayer || requestedManaRestore <= 0 || maxManaRestorePerSecond <= 0) {
			return 0;
		}

		int missingMana = player.statManaMax2 - player.statMana;
		if (missingMana <= 0) {
			return 0;
		}

		int effectiveRequestedRestore = System.Math.Min(requestedManaRestore, missingMana);
		int manaRestoreAmount = player.GetModPlayer<VampiricPlayer>().ConsumeAvailableManaRestore(
			effectiveRequestedRestore,
			maxManaRestorePerSecond);
		if (manaRestoreAmount <= 0) {
			return 0;
		}

		player.statMana += manaRestoreAmount;
		player.ManaEffect(manaRestoreAmount);
		return manaRestoreAmount;
	}

	internal static void EmitStateFeedback(Player player, float visualMultiplier)
	{
		if (player == null || !player.active || player.dead) {
			return;
		}

		VampiricPlayer vampiricPlayer = player.GetModPlayer<VampiricPlayer>();
		if (!vampiricPlayer.IsFrenzied) {
			return;
		}

		float pulse = WeaponPrefixVisuals.GetVampiricPulse(player.whoAmI * 0.29f);
		float frenzyProgress = vampiricPlayer.GetFrenzyProgress();
		float frenzyTierScale = 0.65f + 0.35f * vampiricPlayer.GetFrenzyTierScale();
		Vector2 position = player.MountedCenter + new Vector2(player.direction * 7f, -6f);
		AddLight(position, WeaponPrefixVisuals.VampiricFrenzyColor, (0.1f + frenzyProgress * 0.08f) * pulse * visualMultiplier * frenzyTierScale);

		if (player.whoAmI != Main.myPlayer || !vampiricPlayer.CanEmitFrenzyAuraVisual()) {
			return;
		}

		for (int i = 0; i < 2; i++) {
			Vector2 offset = Main.rand.NextVector2Circular(10f, 12f);
			Dust dust = Dust.NewDustPerfect(position + offset, WeaponPrefixVisuals.VampiricDustType);
			dust.noGravity = true;
			dust.velocity = new Vector2(0f, -0.1f) + offset.SafeNormalize(Vector2.UnitY) * 0.08f;
			dust.scale = (0.72f + frenzyProgress * 0.18f) * visualMultiplier * frenzyTierScale;
			dust.fadeIn = 0.96f;
		}
	}

	internal static void UpdateProjectileVisual(Projectile projectile, bool magicFeed, float intensityMultiplier)
	{
		float pulse = WeaponPrefixVisuals.GetVampiricPulse(projectile.identity * 0.14f) * intensityMultiplier;
		Color feedColor = WeaponPrefixVisuals.GetVampiricFeedColor(VampiricPreyState.None, magicFeed);
		Lighting.AddLight(projectile.Center, feedColor.ToVector3() * (0.1f * pulse));

		if (projectile.owner != Main.myPlayer
			|| projectile.numUpdates != 0
			|| projectile.timeLeft % (magicFeed ? 6 : 8) != 0) {
			return;
		}

		Vector2 lateralOffset = projectile.velocity.SafeNormalize(Vector2.UnitX * projectile.direction)
			.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-4f, 4f);
		Dust bloodDust = Dust.NewDustPerfect(projectile.Center + lateralOffset, WeaponPrefixVisuals.VampiricDustType);
		bloodDust.noGravity = true;
		bloodDust.velocity = projectile.velocity * -0.03f + Main.rand.NextVector2Circular(0.08f, 0.08f);
		bloodDust.scale = 0.72f * intensityMultiplier;
		bloodDust.fadeIn = 0.94f;

		if (!magicFeed || projectile.timeLeft % 12 != 0) {
			return;
		}

		Dust manaDust = Dust.NewDustPerfect(projectile.Center + Main.rand.NextVector2Circular(3f, 3f), WeaponPrefixVisuals.VampiricMagicDustType);
		manaDust.noGravity = true;
		manaDust.velocity = projectile.velocity * -0.025f + Main.rand.NextVector2Circular(0.06f, 0.06f);
		manaDust.scale = 0.7f * intensityMultiplier;
		manaDust.fadeIn = 0.9f;
	}

	private static bool TryTriggerFrenzy(Player player, NPC target, VampiricPreyState preyState)
	{
		if (player == null
			|| !player.active
			|| player.dead
			|| target == null
			|| !target.active
			|| preyState == VampiricPreyState.None) {
			return false;
		}

		return target.GetGlobalNPC<WeaponPrefixGlobalNPC>().TryTriggerVampiricFrenzy(player.whoAmI, preyState);
	}

	private static void SpawnFeedEffect(
		Player player,
		NPC target,
		VampiricPreyState preyState,
		int healAmount,
		int manaRestoreAmount,
		bool crit,
		bool triggeredFrenzy,
		float visualMultiplier)
	{
		if (player == null || !player.active || player.dead || target == null || !target.active) {
			return;
		}

		VampiricPlayer vampiricPlayer = player.GetModPlayer<VampiricPlayer>();
		bool strongerFeed = crit || preyState == VampiricPreyState.Critical || manaRestoreAmount > 0;
		if (!triggeredFrenzy && !vampiricPlayer.CanEmitFeedVisual(strongerFeed)) {
			return;
		}

		Vector2 directionToPlayer = (player.MountedCenter - target.Center).SafeNormalize(Vector2.UnitY);
		Vector2 tangent = directionToPlayer.RotatedBy(MathHelper.PiOver2);
		Color feedColor = WeaponPrefixVisuals.GetVampiricFeedColor(preyState, manaRestoreAmount > 0);
		AddLight(target.Center, feedColor, (0.12f + (crit ? 0.06f : 0f) + (manaRestoreAmount > 0 ? 0.05f : 0f)) * visualMultiplier);

		int feedDustCount = crit ? 4 : 3;
		if (preyState == VampiricPreyState.Critical) {
			feedDustCount++;
		}

		for (int i = 0; i < feedDustCount; i++) {
			float side = i % 2 == 0 ? -1f : 1f;
			Vector2 velocity = directionToPlayer * Main.rand.NextFloat(0.7f, 1.6f) + tangent * (0.22f * side);
			Dust dust = Dust.NewDustPerfect(
				target.Center + Main.rand.NextVector2Circular(8f, 10f),
				WeaponPrefixVisuals.VampiricDustType,
				velocity);
			dust.noGravity = true;
			dust.scale = (0.74f + (crit ? 0.1f : 0f) + (preyState == VampiricPreyState.Critical ? 0.08f : 0f)) * visualMultiplier;
			dust.fadeIn = 0.96f;
		}

		if (manaRestoreAmount > 0) {
			for (int i = 0; i < 2; i++) {
				Vector2 offset = Main.rand.NextVector2Circular(10f, 10f);
				Dust dust = Dust.NewDustPerfect(
					target.Center + offset,
					WeaponPrefixVisuals.VampiricMagicDustType,
					directionToPlayer * Main.rand.NextFloat(0.55f, 1.1f));
				dust.noGravity = true;
				dust.scale = (0.72f + 0.06f * i) * visualMultiplier;
				dust.fadeIn = 0.9f;
			}
		}

		if (triggeredFrenzy) {
			SpawnFrenzyTriggerEffect(player, target.Center, manaRestoreAmount > 0, visualMultiplier);
		}
	}

	private static void SpawnFrenzyTriggerEffect(Player player, Vector2 targetCenter, bool magicFeed, float visualMultiplier)
	{
		Vector2 origin = player.MountedCenter;
		Color frenzyColor = magicFeed ? WeaponPrefixVisuals.VampiricArcaneColor : WeaponPrefixVisuals.VampiricFrenzyColor;
		AddLight(origin, frenzyColor, 0.24f * visualMultiplier);

		for (int i = 0; i < 6; i++) {
			Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(0.9f, 1.8f);
			Dust dust = Dust.NewDustPerfect(origin + Main.rand.NextVector2Circular(12f, 12f), WeaponPrefixVisuals.VampiricDustType, velocity);
			dust.noGravity = true;
			dust.scale = 0.9f * visualMultiplier;
			dust.fadeIn = 1f;
		}

		if (!magicFeed) {
			return;
		}

		for (int i = 0; i < 2; i++) {
			Vector2 velocity = (targetCenter - origin).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.35f) * Main.rand.NextFloat(0.7f, 1.3f);
			Dust dust = Dust.NewDustPerfect(origin, WeaponPrefixVisuals.VampiricMagicDustType, velocity);
			dust.noGravity = true;
			dust.scale = 0.86f * visualMultiplier;
			dust.fadeIn = 0.94f;
		}
	}

	private static void AddLight(Vector2 position, Color color, float multiplier = 1f)
	{
		Lighting.AddLight(position, color.ToVector3() * multiplier);
	}
}
