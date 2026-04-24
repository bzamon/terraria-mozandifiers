using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Mozandifiers.Common.Combat;
using Mozandifiers.Common.Config;
using Mozandifiers.Common.Players;
using Mozandifiers.Content.Prefixes.Weapons;
using Terraria;
using Terraria.Audio;
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
		if (HasAwakenedPrefix(item)) {
			EmitAwakenedStateFeedback(player);
			return;
		}

		bool hasDeadeyePrefix = HasDeadeyePrefix(item);
		bool hasShiftedDeadeye = TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)
			&& simulationId == ShiftingSimulationId.Deadeye;
		if (hasDeadeyePrefix || hasShiftedDeadeye) {
			EmitDeadeyeStateFeedback(
				player,
				item,
				hasShiftedDeadeye ? ShiftingPrefix.ShiftingStrengthMultiplier : 1f);
			return;
		}

		bool hasSanguinePrefix = HasSanguinePrefix(item);
		bool hasShiftedSanguine = simulationId == ShiftingSimulationId.Sanguine;
		if (hasSanguinePrefix || hasShiftedSanguine) {
			EmitVampiricStateFeedback(
				player,
				hasShiftedSanguine ? ShiftingPrefix.ShiftingStrengthMultiplier : 1f);
			return;
		}

		if (HasRadiantPrefix(item)) {
			AddRadiantLight(player.MountedCenter, WeaponPrefixVisuals.GetRadiantPulse(player.whoAmI * 0.35f));
			return;
		}

		if (HasTemporalPrefix(item)) {
			AddTemporalLight(player.MountedCenter, WeaponPrefixVisuals.GetTemporalPulse(player.whoAmI * 0.27f));
			return;
		}

		if (simulationId == ShiftingSimulationId.Radiant) {
			AddRadiantLight(
				player.MountedCenter,
				WeaponPrefixVisuals.GetRadiantPulse(player.whoAmI * 0.35f) * ShiftingSimulationCatalog.GetShiftedRadiantLightMultiplier());
			return;
		}

		if (simulationId == ShiftingSimulationId.Temporal) {
			AddTemporalLight(
				player.MountedCenter,
				WeaponPrefixVisuals.GetTemporalPulse(player.whoAmI * 0.27f) * ShiftingPrefix.ShiftingStrengthMultiplier);
			return;
		}

		bool hasAttunedPrefix = HasAttunedPrefix(item);
		bool hasShiftedAttuned = simulationId == ShiftingSimulationId.Attuned;
		if (hasAttunedPrefix || hasShiftedAttuned) {
			EmitAttunedStateFeedback(
				player,
				hasShiftedAttuned ? ShiftingPrefix.ShiftingStrengthMultiplier : 1f);
			return;
		}

		bool hasSkirmishingPrefix = HasSkirmishingPrefix(item);
		bool hasShiftedSkirmishing = simulationId == ShiftingSimulationId.Skirmishing;
		if (hasSkirmishingPrefix || hasShiftedSkirmishing) {
			ApplySkirmishingMobilityAndFeedback(
				player,
				hasShiftedSkirmishing ? ShiftingPrefix.ShiftingStrengthMultiplier : 1f,
				hasShiftedSkirmishing);
			return;
		}

		bool hasDesperatePrefix = HasDesperatePrefix(item);
		bool hasShiftedDesperate = simulationId == ShiftingSimulationId.Desperate;
		if (hasDesperatePrefix || hasShiftedDesperate) {
			EmitDesperateStateFeedback(
				player,
				hasShiftedDesperate ? ShiftingPrefix.ShiftingStrengthMultiplier : 1f);
		}
	}

	public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
	{
		if (HasAwakenedPrefix(item)) {
			damage *= player.GetModPlayer<AwakenedPlayer>().GetCurrentDamageMultiplier();
		}

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

	public override float UseSpeedMultiplier(Item item, Player player)
	{
		float useSpeedMultiplier = 1f;
		if (HasAwakenedPrefix(item)) {
			useSpeedMultiplier *= player.GetModPlayer<AwakenedPlayer>().GetCurrentUseSpeedMultiplier();
		}

		VampiricPlayer vampiricPlayer = player.GetModPlayer<VampiricPlayer>();
		if (!vampiricPlayer.IsFrenzied) {
			return useSpeedMultiplier;
		}

		if (HasSanguinePrefix(item)) {
			useSpeedMultiplier *= 1f + vampiricPlayer.GetFrenzyAttackSpeedBonus();
			return useSpeedMultiplier;
		}

		if (TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)
			&& simulationId == ShiftingSimulationId.Sanguine) {
			useSpeedMultiplier *= 1f + (vampiricPlayer.GetFrenzyAttackSpeedBonus() * ShiftingPrefix.ShiftingStrengthMultiplier);
		}

		return useSpeedMultiplier;
	}

	public override void ModifyManaCost(Item item, Player player, ref float reduce, ref float mult)
	{
		bool hasAttunedPrefix = HasAttunedPrefix(item);
		bool hasShiftedAttuned = TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)
			&& simulationId == ShiftingSimulationId.Attuned;
		if (hasAttunedPrefix || hasShiftedAttuned) {
			AttunedPlayer attunedPlayer = player.GetModPlayer<AttunedPlayer>();
			bool empoweredCast = attunedPlayer.RegisterQualifyingCast();
			if (hasShiftedAttuned) {
				mult *= ShiftingSimulationCatalog.GetShiftedManaCostMultiplier(simulationId);
			}

			if (empoweredCast) {
				reduce = 1f;
				mult = 0f;
				EmitAttunedCastReleaseFeedback(
					player,
					hasShiftedAttuned ? ShiftingPrefix.ShiftingStrengthMultiplier : 1f);
			}
			return;
		}

		if (!TryGetShiftingSimulation(item, player, out simulationId)) {
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
		AttunedPlayer attunedPlayer = player.GetModPlayer<AttunedPlayer>();
		SkirmishingPlayer skirmishingPlayer = player.GetModPlayer<SkirmishingPlayer>();
		bool hasSpinboundPrefix = HasSpinboundPrefix(item);
		bool hasShiftedSpinbound = TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)
			&& simulationId == ShiftingSimulationId.Spinbound;
		bool hasAttunedPrefix = HasAttunedPrefix(item);
		bool hasShiftedAttuned = simulationId == ShiftingSimulationId.Attuned;
		bool hasSkirmishingPrefix = HasSkirmishingPrefix(item);
		bool hasShiftedSkirmishing = simulationId == ShiftingSimulationId.Skirmishing;

		if ((hasAttunedPrefix || hasShiftedAttuned)
			&& attunedPlayer.IsEmpoweredProjectileWindowActive()) {
			float attunedShootSpeedMultiplier = hasShiftedAttuned
				? ShiftingSimulationCatalog.GetShiftedAttunedEmpoweredShootSpeedMultiplier()
				: AttunedPrefix.EmpoweredCastShootSpeedMultiplier;
			velocity *= attunedShootSpeedMultiplier;
		}

		if ((hasSkirmishingPrefix || hasShiftedSkirmishing)
			&& skirmishingPlayer.TryConsumeFollowUpShot()) {
			float followUpShootSpeedMultiplier = hasShiftedSkirmishing
				? ShiftingSimulationCatalog.GetShiftedSkirmishingFollowUpShootSpeedMultiplier()
				: SkirmishingPrefix.FollowUpShootSpeedMultiplier;
			velocity *= followUpShootSpeedMultiplier;
			EmitSkirmishingFollowUpReleaseFeedback(
				player,
				hasShiftedSkirmishing ? ShiftingPrefix.ShiftingStrengthMultiplier : 1f);
		}

		if (hasShiftedSpinbound || hasShiftedAttuned || hasShiftedSkirmishing) {
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
		if (HasAwakenedPrefix(item)) {
			scale *= player.GetModPlayer<AwakenedPlayer>().GetCurrentScaleMultiplier();
		}

		if (HasBreachingPrefix(item) && BreachingPrefix.SupportsMergedScale(item)) {
			scale *= BreachingPrefix.GetMergedScaleMultiplier(PrefixTuningConfig.Instance.BreachingScaleMultiplier);
		}

		if (!TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)) {
			return;
		}

		scale *= ShiftingSimulationCatalog.GetShiftedScaleMultiplier(
			simulationId,
			PrefixTuningConfig.Instance.BreachingScaleMultiplier);
	}

	public override void ModifyHitNPC(Item item, Player player, NPC target, ref NPC.HitModifiers modifiers)
	{
		if (HasAwakenedPrefix(item)) {
			ApplyAwakenedSweetSpotBonus(item, player, target, ref modifiers);
		}

		if (HasSanguinePrefix(item)) {
			VampiricPreyState preyState = SanguinePrefix.GetPreyState(target);
			player.GetModPlayer<VampiricPlayer>().QueuePendingDirectPreyState(preyState);
			TryApplyVampiricFrenzyCrit(player, preyState, ref modifiers, 1f);
		}

		if (HasDesperatePrefix(item)) {
			TryApplyDesperateSurge(player, ref modifiers, 1f);
		}

		if (!TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)) {
			return;
		}

		if (simulationId == ShiftingSimulationId.Breaching || simulationId == ShiftingSimulationId.Spinbound) {
			modifiers.ArmorPenetration += simulationId == ShiftingSimulationId.Breaching
				? ShiftingSimulationCatalog.GetShiftedArmorPenetrationBonus(simulationId)
				: ShiftingSimulationCatalog.GetShiftedSpinboundArmorPenetrationBonus();
		}

		if (simulationId == ShiftingSimulationId.Sanguine) {
			VampiricPreyState preyState = SanguinePrefix.GetPreyState(target);
			player.GetModPlayer<VampiricPlayer>().QueuePendingDirectPreyState(preyState);
			TryApplyVampiricFrenzyCrit(player, preyState, ref modifiers, ShiftingPrefix.ShiftingStrengthMultiplier);
		}

		if (simulationId == ShiftingSimulationId.Desperate) {
			TryApplyDesperateSurge(player, ref modifiers, ShiftingPrefix.ShiftingStrengthMultiplier, true);
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
		if (HasAwakenedPrefix(item) && damageDone > 0) {
			HandleAwakenedSwordHit(item, player, target);
		}

		if (HasShiftingPrefix(item) && damageDone > 0) {
			player.GetModPlayer<ShiftingPlayer>().RegisterSuccessfulHit(item);
		}

		if (HasAttunedPrefix(item) && damageDone > 0) {
			player.GetModPlayer<AttunedPlayer>().RegisterQualifyingHit();
		}

		if (HasSkirmishingPrefix(item) && damageDone > 0) {
			player.GetModPlayer<SkirmishingPlayer>().RegisterQualifyingHit();
		}

		if (HasTemporalPrefix(item) && damageDone > 0) {
			HandleTemporalHit(player, target, damageDone, 1f);
		}

		if (HasSanguinePrefix(item) && damageDone > 0) {
			ApplyVampiricFeed(player, item, target, damageDone, hit.Crit, 1f);
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

		if (player.GetModPlayer<DesperatePlayer>().TryConsumeDirectSurgeFeedback(out float directSurgeVisualMultiplier)) {
			SpawnDesperateSurgeImpactEffect(target.Center, new Vector2(hit.HitDirection, 0f), directSurgeVisualMultiplier);
		}

		if (!TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)) {
			return;
		}

		if (simulationId == ShiftingSimulationId.Sanguine && damageDone > 0) {
			ApplyVampiricFeed(player, item, target, damageDone, hit.Crit, ShiftingPrefix.ShiftingStrengthMultiplier, true);
		}

		if (simulationId == ShiftingSimulationId.Attuned && damageDone > 0) {
			player.GetModPlayer<AttunedPlayer>().RegisterQualifyingHit();
		}

		if (simulationId == ShiftingSimulationId.Temporal && damageDone > 0) {
			HandleTemporalHit(player, target, damageDone, ShiftingPrefix.ShiftingStrengthMultiplier, true);
		}

		if (simulationId == ShiftingSimulationId.Skirmishing && damageDone > 0) {
			player.GetModPlayer<SkirmishingPlayer>().RegisterQualifyingHit();
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

	private static bool HasDeadeyePrefix(Item item)
	{
		return item.prefix == ModContent.PrefixType<DeadeyePrefix>();
	}

	private static bool HasAwakenedPrefix(Item item)
	{
		return item.prefix == ModContent.PrefixType<AwakenedPrefix>();
	}

	private static bool HasRadiantPrefix(Item item)
	{
		return item.prefix == ModContent.PrefixType<RadiantPrefix>();
	}

	private static bool HasAttunedPrefix(Item item)
	{
		return item.prefix == ModContent.PrefixType<AttunedPrefix>();
	}

	private static bool HasSkirmishingPrefix(Item item)
	{
		return item.prefix == ModContent.PrefixType<SkirmishingPrefix>();
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

	internal static void ApplyVampiricFeed(Player player, Item item, NPC target, int damageDone, bool crit, float strengthMultiplier = 1f, bool shifted = false)
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
			? ShiftingSimulationCatalog.GetShiftedSanguineLifeStealMultiplier()
			: SanguinePrefix.LifeStealMultiplier;
		int healCapPerSecond = shifted
			? ShiftingSimulationCatalog.GetShiftedSanguineHealCapPerSecond()
			: SanguinePrefix.BaseHealCapPerSecond;
		int healAmount = TryApplyVampiricHeal(
			player,
			damageDone,
			lifeStealMultiplier * feedMultiplier,
			healCapPerSecond);

		int manaRestoreAmount = 0;
		bool magicWeapon = WeaponPrefix.IsMagicWeapon(item);
		if (magicWeapon) {
			float manaRestoreRatio = shifted
				? ShiftingSimulationCatalog.GetShiftedVampiricManaRestoreRatio()
				: SanguinePrefix.ManaRestoreFromManaCost;
			int maxManaRestorePerHit = shifted
				? ShiftingSimulationCatalog.GetShiftedVampiricMaxRestorePerHit()
				: SanguinePrefix.MaxManaRestorePerHit;
			int maxManaRestorePerSecond = shifted
				? ShiftingSimulationCatalog.GetShiftedVampiricMaxRestorePerSecond()
				: SanguinePrefix.BaseManaRestorePerSecond;
			int requestedManaRestore = System.Math.Max(
				1,
				(int)System.MathF.Round(GetVampiricManaRestoreAmount(item, manaRestoreRatio, maxManaRestorePerHit) * feedMultiplier));
			manaRestoreAmount = TryApplyVampiricManaRestore(player, requestedManaRestore, maxManaRestorePerSecond);
		}

		bool triggeredFrenzy = TryTriggerVampiricFrenzy(player, target, preyState);
		if (triggeredFrenzy) {
			vampiricPlayer.TriggerFrenzy(preyState, shifted ? strengthMultiplier : 1f);
		}

		if ((healAmount > 0 || manaRestoreAmount > 0 || triggeredFrenzy)
			&& player.whoAmI == Main.myPlayer) {
			SpawnVampiricFeedEffect(
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

	private static void EmitDeadeyeStateFeedback(Player player, Item item, float strengthMultiplier)
	{
		if (player == null || !player.active || player.dead || item == null || item.IsAir) {
			return;
		}

		DeadeyePlayer deadeyePlayer = player.GetModPlayer<DeadeyePlayer>();
		deadeyePlayer.RegisterHeldDeadeyeWeapon(player);

		Vector2 auraCenter = player.MountedCenter + new Vector2(player.direction * 10f, -8f);
		float pulse = 0.78f + 0.22f * (0.5f + 0.5f * System.MathF.Sin(Main.GlobalTimeWrappedHourly * 8.6f + player.whoAmI * 0.29f));
		float chargeProgress = deadeyePlayer.GetChargeProgress();
		Color auraColor = Color.Lerp(WeaponPrefixVisuals.DeadeyeReadyColor * 0.55f, WeaponPrefixVisuals.DeadeyeShotColor, chargeProgress);
		Lighting.AddLight(auraCenter, auraColor.ToVector3() * (0.09f + 0.05f * pulse) * strengthMultiplier);

		if (deadeyePlayer.WasReadiedThisTick()) {
			SpawnDeadeyeReadyEffect(auraCenter, strengthMultiplier);
			if (player.whoAmI == Main.myPlayer && deadeyePlayer.CanPlayReadySound()) {
				SoundEngine.PlaySound(SoundID.Item9 with { Pitch = -0.2f, Volume = 0.55f }, auraCenter);
			}

			return;
		}

		if (player.whoAmI != Main.myPlayer || !deadeyePlayer.CanEmitReadyVisual()) {
			return;
		}

		Dust dust = Dust.NewDustPerfect(
			auraCenter + Main.rand.NextVector2Circular(6f, 6f),
			WeaponPrefixVisuals.DeadeyeReadyDustType,
			new Vector2(0f, -0.18f) + Main.rand.NextVector2Circular(0.08f, 0.08f),
			0,
			auraColor,
			0.78f + 0.16f * chargeProgress * strengthMultiplier);
		dust.noGravity = true;
		dust.fadeIn = 0.92f;
	}

	private static void SpawnDeadeyeReadyEffect(Vector2 position, float strengthMultiplier)
	{
		Lighting.AddLight(position, WeaponPrefixVisuals.DeadeyeReadyColor.ToVector3() * (0.26f * strengthMultiplier));
		for (int i = 0; i < 8; i++) {
			Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(0.9f, 1.8f) * strengthMultiplier;
			Dust dust = Dust.NewDustPerfect(position, WeaponPrefixVisuals.DeadeyeReadyDustType, velocity, 0, WeaponPrefixVisuals.DeadeyeReadyColor, 0.92f * strengthMultiplier);
			dust.noGravity = true;
			dust.fadeIn = 0.95f;
		}
	}

	internal static void ApplyVampiricProjectileFeed(
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
			? ShiftingSimulationCatalog.GetShiftedSanguineLifeStealMultiplier()
			: SanguinePrefix.LifeStealMultiplier;
		int healCapPerSecond = shifted
			? ShiftingSimulationCatalog.GetShiftedSanguineHealCapPerSecond()
			: SanguinePrefix.BaseHealCapPerSecond;
		int healAmount = TryApplyVampiricHeal(
			player,
			damageDone,
			lifeStealMultiplier * feedMultiplier,
			healCapPerSecond);

		int manaRestoreAmount = 0;
		if (baseManaRestoreAmount > 0) {
			int maxManaRestorePerSecond = shifted
				? ShiftingSimulationCatalog.GetShiftedVampiricMaxRestorePerSecond()
				: SanguinePrefix.BaseManaRestorePerSecond;
			int requestedManaRestore = System.Math.Max(1, (int)System.MathF.Round(baseManaRestoreAmount * feedMultiplier));
			manaRestoreAmount = TryApplyVampiricManaRestore(player, requestedManaRestore, maxManaRestorePerSecond);
		}

		VampiricPlayer vampiricPlayer = player.GetModPlayer<VampiricPlayer>();
		bool triggeredFrenzy = TryTriggerVampiricFrenzy(player, target, preyState);
		if (triggeredFrenzy) {
			vampiricPlayer.TriggerFrenzy(preyState, shifted ? strengthMultiplier : 1f);
		}

		if ((healAmount > 0 || manaRestoreAmount > 0 || triggeredFrenzy)
			&& player.whoAmI == Main.myPlayer
			&& target != null
			&& target.active) {
			SpawnVampiricFeedEffect(
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

	internal static int TryApplyVampiricHeal(Player player, int damageDone, float lifeStealMultiplier, int healCapPerSecond)
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

	internal static int GetVampiricManaRestoreAmount(Item item)
	{
		return GetVampiricManaRestoreAmount(item, SanguinePrefix.ManaRestoreFromManaCost, SanguinePrefix.MaxManaRestorePerHit);
	}

	internal static int GetVampiricManaRestoreAmount(Item item, float manaRestoreRatio, int maxManaRestorePerHit)
	{
		if (item == null || item.IsAir || item.mana <= 0 || manaRestoreRatio <= 0f || maxManaRestorePerHit <= 0) {
			return 0;
		}

		return System.Math.Min(
			maxManaRestorePerHit,
			System.Math.Max(1, (int)System.MathF.Round(item.mana * manaRestoreRatio)));
	}

	internal static int TryApplyVampiricManaRestore(Player player, int requestedManaRestore, int maxManaRestorePerSecond)
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

	internal static void HandleTemporalHit(Player player, NPC target, int damageDone, float visualMultiplier = 1f, bool shifted = false)
	{
		if (player == null
			|| !player.active
			|| player.dead
			|| target == null
			|| !target.active
			|| damageDone <= 0) {
			return;
		}

		WeaponPrefixGlobalNPC targetGlobal = target.GetGlobalNPC<WeaponPrefixGlobalNPC>();
		int playerIndex = player.whoAmI;
		if (targetGlobal.IsTemporalFracturedForPlayer(playerIndex)) {
			RestoreTemporalImmediateDamage(target, damageDone);
			int storedDamage = targetGlobal.StoreTemporalDamage(playerIndex, damageDone);
			if (storedDamage >= System.Math.Max(1, target.life)) {
				targetGlobal.TryTriggerTemporalCollapse(target, playerIndex, visualMultiplier);
			}
			return;
		}

		WeaponPrefixGlobalNPC.TemporalPressureBuildResult buildResult = targetGlobal.TryBuildTemporalPressure(playerIndex, target, damageDone);
		if (buildResult.PressureAdded <= 0f || playerIndex != Main.myPlayer) {
			return;
		}

		TemporalFeedbackPlayer feedbackPlayer = player.GetModPlayer<TemporalFeedbackPlayer>();
		if (!feedbackPlayer.CanEmitPressureVisual()) {
			return;
		}

		SpawnTemporalPressureEffect(
			target.Center,
			buildResult.CurrentPressure / TemporalPrefix.PressureMax,
			buildResult.StartedFracture,
			shifted ? ShiftingPrefix.ShiftingStrengthMultiplier : visualMultiplier);
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

	private static void TryApplyVampiricFrenzyCrit(Player player, VampiricPreyState preyState, ref NPC.HitModifiers modifiers, float strengthMultiplier)
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

	private static bool TryTriggerVampiricFrenzy(Player player, NPC target, VampiricPreyState preyState)
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

	private static void TryApplyDesperateSurge(Player player, ref NPC.HitModifiers modifiers, float visualMultiplier, bool shifted = false)
	{
		if (player == null || !player.active || player.dead) {
			return;
		}

		DesperatePlayer desperatePlayer = player.GetModPlayer<DesperatePlayer>();
		if (!desperatePlayer.TryConsumeSurge()) {
			return;
		}

		float surgeDamageBonus = shifted
			? ShiftingSimulationCatalog.GetShiftedDesperateSurgeDamageBonus()
			: DesperatePrefix.SurgeDamageBonus;
		modifiers.SourceDamage *= 1f + surgeDamageBonus;
		desperatePlayer.QueueDirectSurgeFeedback(visualMultiplier * (shifted ? 1.12f : 1f));
	}

	private static void AddRadiantLight(Vector2 position, float multiplier = 1f)
	{
		Lighting.AddLight(position, 1f * multiplier, 0.74f * multiplier, 0.24f * multiplier);
	}

	private static void AddTemporalLight(Vector2 position, float multiplier = 1f)
	{
		Lighting.AddLight(position, 0.18f * multiplier, 0.26f * multiplier, 0.38f * multiplier);
	}

	private static void RestoreTemporalImmediateDamage(NPC target, int damageDone)
	{
		if (target == null
			|| !target.active
			|| damageDone <= 0
			|| Main.netMode == NetmodeID.MultiplayerClient) {
			return;
		}

		target.life = System.Math.Min(target.lifeMax, target.life + damageDone);
		target.netUpdate = true;
	}

	private static void AddVampiricLight(Vector2 position, Color color, float multiplier = 1f)
	{
		Lighting.AddLight(position, color.ToVector3() * multiplier);
	}

	private static void AddAwakenedLight(Vector2 position, Color color, float multiplier = 1f)
	{
		Lighting.AddLight(position, color.ToVector3() * multiplier);
	}

	private static void ApplyAwakenedSweetSpotBonus(Item item, Player player, NPC target, ref NPC.HitModifiers modifiers)
	{
		if (player == null || !player.active || player.dead || target == null || !target.active) {
			return;
		}

		AwakenedPlayer awakenedPlayer = player.GetModPlayer<AwakenedPlayer>();
		if (!awakenedPlayer.IsAwakened) {
			return;
		}

		if (!AwakenedCombatHelper.TryEvaluateSweetSpot(
			player,
			item,
			target,
			awakenedPlayer.GetCurrentScaleMultiplier(),
			out _,
			out _,
			out _)) {
			return;
		}

		int bonusDamage = AwakenedPrefix.GetAwakenedBonusDamage(target);
		if (target.boss) {
			bonusDamage = target.GetGlobalNPC<WeaponPrefixGlobalNPC>().ConsumeAwakenedBossBonusBudget(
				player.whoAmI,
				bonusDamage,
				AwakenedPrefix.GetBossBonusDamageCapPerSecond(target));
		}

		if (bonusDamage > 0) {
			modifiers.FlatBonusDamage += bonusDamage;
		}
	}

	private static void HandleAwakenedSwordHit(Item item, Player player, NPC target)
	{
		if (player == null || !player.active || player.dead || target == null || !target.active) {
			return;
		}

		AwakenedPlayer awakenedPlayer = player.GetModPlayer<AwakenedPlayer>();
		if (!AwakenedCombatHelper.TryEvaluateSweetSpot(
			player,
			item,
			target,
			awakenedPlayer.GetCurrentScaleMultiplier(),
			out float tipProgress,
			out Vector2 hitPosition,
			out Vector2 tipPosition)) {
			return;
		}

		if (awakenedPlayer.IsAwakened) {
			if (player.whoAmI == Main.myPlayer && awakenedPlayer.CanEmitAwakenedStrikeVisual()) {
				SpawnAwakenedStrikeEffect(target.Center, tipPosition, tipProgress);
			}
			return;
		}

		if (awakenedPlayer.IsInRecoveryLockout) {
			return;
		}

		bool awakenedStarted = awakenedPlayer.RegisterSweetSpotHit(item);
		if (player.whoAmI != Main.myPlayer || !awakenedPlayer.CanEmitBuildFeedback()) {
			return;
		}

		if (awakenedStarted) {
			SpawnAwakenedStartEffect(player, tipPosition);
			SoundEngine.PlaySound(SoundID.Item94, player.MountedCenter);
			return;
		}

		SpawnAwakenedBuildEffect(hitPosition, tipPosition, tipProgress, awakenedPlayer.MeterProgress);
	}

	private static void EmitAwakenedStateFeedback(Player player)
	{
		if (player == null || !player.active || player.dead) {
			return;
		}

		AwakenedPlayer awakenedPlayer = player.GetModPlayer<AwakenedPlayer>();
		if (awakenedPlayer.CurrentState == AwakenedState.RecoveryLockout) {
			return;
		}

		float stateProgress = awakenedPlayer.GetStateProgress();
		if (stateProgress <= 0f && !awakenedPlayer.IsAwakened) {
			return;
		}

		float pulse = WeaponPrefixVisuals.GetAwakenedPulse(player.whoAmI * 0.37f);
		Color stateColor = WeaponPrefixVisuals.GetAwakenedColor(awakenedPlayer.CurrentState, stateProgress);
		Vector2 position = player.MountedCenter + new Vector2(player.direction * 10f, -8f);
		float lightStrength = awakenedPlayer.IsAwakened ? 0.24f : 0.08f + stateProgress * 0.08f;
		AddAwakenedLight(position, stateColor, lightStrength * pulse);

		bool canEmitAura = awakenedPlayer.IsAwakened
			? awakenedPlayer.CanEmitAwakenedAuraVisual()
			: awakenedPlayer.CanEmitDormantAuraVisual();
		if (player.whoAmI != Main.myPlayer || !canEmitAura) {
			return;
		}

		int dustCount = awakenedPlayer.IsAwakened ? 3 : 1;
		for (int i = 0; i < dustCount; i++) {
			Vector2 offset = Main.rand.NextVector2Circular(12f, 14f);
			Dust dust = Dust.NewDustPerfect(position + offset, WeaponPrefixVisuals.AwakenedDustType);
			dust.noGravity = true;
			dust.velocity = offset.SafeNormalize(Vector2.UnitY) * (awakenedPlayer.IsAwakened ? 0.35f : 0.14f);
			dust.scale = (awakenedPlayer.IsAwakened ? 0.95f : 0.72f + stateProgress * 0.12f) * pulse;
			dust.fadeIn = 0.95f;
		}
	}

	private static void SpawnAwakenedBuildEffect(Vector2 hitPosition, Vector2 tipPosition, float tipProgress, float meterProgress)
	{
		Vector2 direction = (tipPosition - hitPosition).SafeNormalize(Vector2.UnitX);
		Color buildColor = WeaponPrefixVisuals.GetAwakenedColor(AwakenedState.Dormant, meterProgress);
		AddAwakenedLight(hitPosition, buildColor, 0.12f + meterProgress * 0.05f);

		for (int i = 0; i < 2; i++) {
			Dust dust = Dust.NewDustPerfect(
				hitPosition + Main.rand.NextVector2Circular(6f, 6f),
				WeaponPrefixVisuals.AwakenedDustType,
				direction.RotatedByRandom(0.35f) * Main.rand.NextFloat(0.45f, 0.95f));
			dust.noGravity = true;
			dust.scale = 0.72f + meterProgress * 0.1f + tipProgress * 0.08f;
			dust.fadeIn = 0.9f;
		}
	}

	private static void SpawnAwakenedStartEffect(Player player, Vector2 tipPosition)
	{
		Vector2 origin = player.MountedCenter + new Vector2(player.direction * 10f, -6f);
		AddAwakenedLight(origin, WeaponPrefixVisuals.AwakenedActiveColor, 0.34f);

		for (int i = 0; i < 8; i++) {
			Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(0.9f, 1.8f);
			Dust dust = Dust.NewDustPerfect(origin, WeaponPrefixVisuals.AwakenedDustType, velocity);
			dust.noGravity = true;
			dust.scale = 0.92f;
			dust.fadeIn = 1f;
		}

		for (int i = 0; i < 3; i++) {
			Vector2 velocity = (tipPosition - origin).SafeNormalize(Vector2.UnitX).RotatedByRandom(0.28f) * Main.rand.NextFloat(0.8f, 1.5f);
			Dust dust = Dust.NewDustPerfect(origin, WeaponPrefixVisuals.AwakenedDustType, velocity);
			dust.noGravity = true;
			dust.scale = 1f;
			dust.fadeIn = 0.95f;
		}
	}

	private static void SpawnAwakenedStrikeEffect(Vector2 targetCenter, Vector2 tipPosition, float tipProgress)
	{
		Vector2 direction = (targetCenter - tipPosition).SafeNormalize(Vector2.UnitX);
		AddAwakenedLight(targetCenter, WeaponPrefixVisuals.AwakenedActiveColor, 0.28f);

		for (int i = 0; i < 4; i++) {
			Vector2 velocity = direction.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.9f, 1.8f);
			Dust dust = Dust.NewDustPerfect(
				targetCenter + Main.rand.NextVector2Circular(10f, 10f),
				WeaponPrefixVisuals.AwakenedDustType,
				velocity);
			dust.noGravity = true;
			dust.scale = 0.86f + tipProgress * 0.1f;
			dust.fadeIn = 0.96f;
		}
	}

	private static void EmitVampiricStateFeedback(Player player, float visualMultiplier)
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
		AddVampiricLight(position, WeaponPrefixVisuals.VampiricFrenzyColor, (0.1f + frenzyProgress * 0.08f) * pulse * visualMultiplier * frenzyTierScale);

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

	private static void EmitAttunedStateFeedback(Player player, float visualMultiplier)
	{
		if (player == null || !player.active || player.dead) {
			return;
		}

		AttunedPlayer attunedPlayer = player.GetModPlayer<AttunedPlayer>();
		float resonanceProgress = attunedPlayer.GetResonanceProgress();
		if (resonanceProgress <= 0f) {
			return;
		}

		bool empowered = attunedPlayer.HasEmpoweredCastReady;
		float pulse = WeaponPrefixVisuals.GetAttunedPulse(player.whoAmI * 0.31f);
		Color attunedColor = WeaponPrefixVisuals.GetAttunedColor(resonanceProgress, empowered);
		Vector2 position = player.MountedCenter + new Vector2(player.direction * 6f, -8f);
		Lighting.AddLight(position, attunedColor.ToVector3() * ((0.08f + resonanceProgress * 0.08f) * pulse * visualMultiplier));

		if (player.whoAmI != Main.myPlayer || !attunedPlayer.CanEmitAuraVisual()) {
			return;
		}

		int dustCount = empowered ? 3 : System.Math.Max(1, (int)System.MathF.Ceiling(resonanceProgress * 2f));
		for (int i = 0; i < dustCount; i++) {
			Vector2 offset = Main.rand.NextVector2Circular(10f, 12f);
			Dust dust = Dust.NewDustPerfect(position + offset, WeaponPrefixVisuals.AttunedDustType);
			dust.noGravity = true;
			dust.velocity = new Vector2(0f, -0.12f) + offset.SafeNormalize(Vector2.UnitY) * (0.12f + resonanceProgress * 0.08f);
			dust.scale = (0.7f + resonanceProgress * 0.2f) * visualMultiplier;
			dust.fadeIn = 0.95f;
		}
	}

	private static void SpawnVampiricFeedEffect(
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
		AddVampiricLight(target.Center, feedColor, (0.12f + (crit ? 0.06f : 0f) + (manaRestoreAmount > 0 ? 0.05f : 0f)) * visualMultiplier);

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
			SpawnVampiricFrenzyTriggerEffect(player, target.Center, manaRestoreAmount > 0, visualMultiplier);
		}
	}

	private static void SpawnVampiricFrenzyTriggerEffect(Player player, Vector2 targetCenter, bool magicFeed, float visualMultiplier)
	{
		Vector2 origin = player.MountedCenter;
		Color frenzyColor = magicFeed ? WeaponPrefixVisuals.VampiricArcaneColor : WeaponPrefixVisuals.VampiricFrenzyColor;
		AddVampiricLight(origin, frenzyColor, 0.24f * visualMultiplier);

		for (int i = 0; i < 6; i++) {
			Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(0.9f, 1.8f);
			Dust dust = Dust.NewDustPerfect(origin + Main.rand.NextVector2Circular(12f, 12f), WeaponPrefixVisuals.VampiricDustType, velocity);
			dust.noGravity = true;
			dust.scale = 0.9f * visualMultiplier;
			dust.fadeIn = 1f;
		}

		if (magicFeed) {
			for (int i = 0; i < 2; i++) {
				Vector2 velocity = (targetCenter - origin).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.35f) * Main.rand.NextFloat(0.7f, 1.3f);
				Dust dust = Dust.NewDustPerfect(origin, WeaponPrefixVisuals.VampiricMagicDustType, velocity);
				dust.noGravity = true;
				dust.scale = 0.86f * visualMultiplier;
				dust.fadeIn = 0.94f;
			}
		}
	}

	private static void EmitAttunedCastReleaseFeedback(Player player, float visualMultiplier)
	{
		if (player == null || !player.active || player.dead || player.whoAmI != Main.myPlayer) {
			return;
		}

		Vector2 position = player.MountedCenter + new Vector2(player.direction * 10f, -6f);
		Color attunedColor = WeaponPrefixVisuals.AttunedReadyColor;
		Lighting.AddLight(position, attunedColor.ToVector3() * (0.28f * visualMultiplier));
		SoundEngine.PlaySound(SoundID.Item29, position);

		for (int i = 0; i < 8; i++) {
			Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(0.9f, 1.8f);
			Dust dust = Dust.NewDustPerfect(position, WeaponPrefixVisuals.AttunedDustType, velocity);
			dust.noGravity = true;
			dust.scale = 0.86f * visualMultiplier;
			dust.fadeIn = 1f;
		}
	}

	private static void EmitDesperateStateFeedback(Player player, float visualMultiplier)
	{
		if (player == null || !player.active || player.dead) {
			return;
		}

		DesperatePlayer desperatePlayer = player.GetModPlayer<DesperatePlayer>();
		DesperateThresholdState state = desperatePlayer.CurrentState;
		if (state == DesperateThresholdState.None) {
			return;
		}

		float pulse = WeaponPrefixVisuals.GetDesperatePulse(player.whoAmI * 0.39f);
		float surgeReadyStrength = state == DesperateThresholdState.Critical
			? 0.2f + 0.25f * desperatePlayer.GetSurgeCooldownProgress()
			: 0f;
		Color stateColor = WeaponPrefixVisuals.GetDesperateColor(state);
		Vector2 position = player.MountedCenter + new Vector2(player.direction * 8f, -4f);

		Lighting.AddLight(position, stateColor.ToVector3() * ((0.08f + surgeReadyStrength) * pulse * visualMultiplier));

		if (player.whoAmI != Main.myPlayer || !desperatePlayer.CanEmitStateVisual()) {
			return;
		}

		int dustCount = state switch {
			DesperateThresholdState.Minor => 1,
			DesperateThresholdState.Severe => 2,
			_ => 3
		};

		for (int i = 0; i < dustCount; i++) {
			Vector2 offset = Main.rand.NextVector2Circular(10f, 12f);
			Dust dust = Dust.NewDustPerfect(position + offset, WeaponPrefixVisuals.DesperateDustType);
			dust.noGravity = true;
			dust.velocity = new Vector2(0f, -0.16f) + offset.SafeNormalize(Vector2.UnitY) * (0.14f + surgeReadyStrength * 0.2f);
			dust.scale = (0.72f + 0.08f * (int)state + surgeReadyStrength * 0.25f) * visualMultiplier;
			dust.fadeIn = 0.95f;
		}
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

	internal static void SpawnTemporalCollapseEffect(Vector2 center, float visualMultiplier = 1f)
	{
		Color collapseColor = WeaponPrefixVisuals.TemporalCollapseColor;
		AddTemporalLight(center, (0.9f + 0.15f * visualMultiplier) * visualMultiplier);

		for (int i = 0; i < 10; i++) {
			Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.2f, 2.6f);
			Dust dust = Dust.NewDustPerfect(center, WeaponPrefixVisuals.TemporalDustType, velocity, 0, collapseColor, 0.95f * visualMultiplier);
			dust.noGravity = true;
			dust.fadeIn = 1f;
		}

		for (int i = 0; i < 6; i++) {
			float angle = MathHelper.TwoPi * i / 6f;
			Vector2 offset = angle.ToRotationVector2() * 10f;
			Dust dust = Dust.NewDustPerfect(center + offset, WeaponPrefixVisuals.TemporalDustType, offset.SafeNormalize(Vector2.UnitX) * 0.85f, 0, collapseColor, 0.86f * visualMultiplier);
			dust.noGravity = true;
			dust.fadeIn = 0.96f;
		}
	}

	private static void SpawnTemporalPressureEffect(Vector2 center, float pressureRatio, bool startedFracture, float visualMultiplier)
	{
		float clampedRatio = MathHelper.Clamp(pressureRatio, 0f, 1f);
		Color pressureColor = startedFracture
			? WeaponPrefixVisuals.TemporalFractureColor
			: WeaponPrefixVisuals.GetTemporalPressureColor(clampedRatio);
		float lightMultiplier = startedFracture ? 0.42f : 0.16f + clampedRatio * 0.12f;
		AddTemporalLight(center, lightMultiplier * visualMultiplier);

		int dustCount = startedFracture ? 7 : 3;
		float burstSpeed = startedFracture ? 1.5f : 0.7f + clampedRatio * 0.4f;
		for (int i = 0; i < dustCount; i++) {
			Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * burstSpeed;
			Dust dust = Dust.NewDustPerfect(center, WeaponPrefixVisuals.TemporalDustType, velocity, 0, pressureColor, (0.72f + clampedRatio * 0.18f) * visualMultiplier);
			dust.noGravity = true;
			dust.fadeIn = startedFracture ? 1f : 0.92f;
		}
	}

	private static void ApplySkirmishingMobilityAndFeedback(Player player, float visualMultiplier, bool shifted)
	{
		if (player == null || !player.active || player.dead) {
			return;
		}

		SkirmishingPlayer skirmishingPlayer = player.GetModPlayer<SkirmishingPlayer>();
		if (!skirmishingPlayer.HasActiveSkirmishWindow) {
			return;
		}

		float moveSpeedBonus = shifted
			? ShiftingSimulationCatalog.GetShiftedSkirmishingMoveSpeedBonus()
			: SkirmishingPrefix.MobilityMoveSpeedBonus;
		float runAccelerationMultiplier = shifted
			? ShiftingSimulationCatalog.GetShiftedSkirmishingRunAccelerationMultiplier()
			: SkirmishingPrefix.MobilityRunAccelerationMultiplier;
		float maxRunSpeedBonus = shifted
			? ShiftingSimulationCatalog.GetShiftedSkirmishingMaxRunSpeedBonus()
			: SkirmishingPrefix.MobilityMaxRunSpeedBonus;

		player.moveSpeed += moveSpeedBonus;
		player.runAcceleration *= runAccelerationMultiplier;
		player.maxRunSpeed += maxRunSpeedBonus;

		float progress = skirmishingPlayer.GetWindowProgress();
		float pulse = WeaponPrefixVisuals.GetSkirmishingPulse(player.whoAmI * 0.33f);
		Color skirmishColor = WeaponPrefixVisuals.GetSkirmishingColor(progress, false);
		Vector2 position = player.MountedCenter + new Vector2(-player.direction * 10f, 4f);
		Lighting.AddLight(position, skirmishColor.ToVector3() * ((0.07f + progress * 0.08f) * pulse * visualMultiplier));

		if (player.whoAmI != Main.myPlayer || !skirmishingPlayer.CanEmitWindowVisual()) {
			return;
		}

		for (int i = 0; i < 2; i++) {
			float side = i == 0 ? -1f : 1f;
			Vector2 offset = new Vector2(-player.direction * Main.rand.NextFloat(8f, 14f), Main.rand.NextFloat(-6f, 8f));
			Dust dust = Dust.NewDustPerfect(position + offset, WeaponPrefixVisuals.SkirmishingDustType);
			dust.noGravity = true;
			dust.velocity = new Vector2(player.direction * Main.rand.NextFloat(0.55f, 1.1f), Main.rand.NextFloat(-0.15f, 0.15f)) + new Vector2(0f, 0.08f * side);
			dust.scale = (0.7f + progress * 0.18f) * visualMultiplier;
			dust.fadeIn = 0.95f;
		}
	}

	private static void EmitSkirmishingFollowUpReleaseFeedback(Player player, float visualMultiplier)
	{
		if (player == null || !player.active || player.dead || player.whoAmI != Main.myPlayer) {
			return;
		}

		Vector2 position = player.MountedCenter + new Vector2(player.direction * 10f, -4f);
		Color skirmishColor = WeaponPrefixVisuals.GetSkirmishingColor(1f, true);
		Lighting.AddLight(position, skirmishColor.ToVector3() * (0.22f * visualMultiplier));

		for (int i = 0; i < 6; i++) {
			Vector2 velocity = new Vector2(player.direction * Main.rand.NextFloat(0.8f, 1.8f), Main.rand.NextFloat(-0.2f, 0.2f));
			Dust dust = Dust.NewDustPerfect(position, WeaponPrefixVisuals.SkirmishingDustType, velocity, 0, skirmishColor, 0.84f * visualMultiplier);
			dust.noGravity = true;
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

	internal static void SpawnDesperateSurgeImpactEffect(Vector2 position, Vector2 impactDirection, float visualMultiplier)
	{
		Vector2 direction = impactDirection.LengthSquared() > 0.0001f
			? impactDirection.SafeNormalize(Vector2.UnitX)
			: Vector2.UnitX;
		Vector2 tangent = direction.RotatedBy(MathHelper.PiOver2);
		Lighting.AddLight(position, WeaponPrefixVisuals.DesperateCriticalColor.ToVector3() * (0.3f * visualMultiplier));

		for (int i = 0; i < 6; i++) {
			float side = i % 2 == 0 ? -1f : 1f;
			Vector2 velocity = direction * Main.rand.NextFloat(1.1f, 2.2f) + tangent * (0.35f * side);
			Dust dust = Dust.NewDustPerfect(position + tangent * (4f * side), WeaponPrefixVisuals.DesperateDustType, velocity);
			dust.noGravity = true;
			dust.scale = 0.95f * visualMultiplier;
			dust.fadeIn = 1f;
		}

		for (int i = 0; i < 4; i++) {
			Vector2 offset = Main.rand.NextVector2Circular(8f, 8f);
			Dust dust = Dust.NewDustPerfect(position + offset, WeaponPrefixVisuals.DesperateDustType);
			dust.noGravity = true;
			dust.velocity = direction * Main.rand.NextFloat(0.45f, 0.9f) + Main.rand.NextVector2Circular(0.18f, 0.18f);
			dust.scale = 0.82f * visualMultiplier;
			dust.fadeIn = 0.95f;
		}
	}
}
