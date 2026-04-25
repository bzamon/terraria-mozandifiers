using Microsoft.Xna.Framework;
using Mozandifiers.Common.Combat;
using Mozandifiers.Common.Players;
using Mozandifiers.Content.Prefixes.Weapons;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace Mozandifiers.Content.Prefixes.Common;

internal static class WeaponPrefixItemHoldDispatch
{
	internal static void Dispatch(Item item, Player player)
	{
		if (WeaponPrefixIdentity.HasAwakenedPrefix(item)) {
			AwakenedRuntime.EmitStateFeedback(player);
			return;
		}

		WeaponPrefixIdentity.TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId);

		bool hasDeadeyePrefix = WeaponPrefixIdentity.HasDeadeyePrefix(item);
		bool hasShiftedDeadeye = simulationId == ShiftingSimulationId.Deadeye;
		if (hasDeadeyePrefix || hasShiftedDeadeye) {
			DeadeyeRuntime.EmitStateFeedback(
				player,
				item,
				hasShiftedDeadeye ? ShiftingPrefix.ShiftingStrengthMultiplier : 1f);
			return;
		}

		bool hasVampiricPrefix = WeaponPrefixIdentity.HasVampiricPrefix(item);
		bool hasShiftedVampiric = simulationId == ShiftingSimulationId.Vampiric;
		if (hasVampiricPrefix || hasShiftedVampiric) {
			VampiricRuntime.EmitStateFeedback(
				player,
				hasShiftedVampiric ? ShiftingPrefix.ShiftingStrengthMultiplier : 1f);
			return;
		}

		if (WeaponPrefixIdentity.HasRadiantPrefix(item)) {
			AddRadiantLight(player.MountedCenter, WeaponPrefixVisuals.GetRadiantPulse(player.whoAmI * 0.35f));
			return;
		}

		if (WeaponPrefixIdentity.HasTemporalPrefix(item)) {
			TemporalRuntime.AddHeldLight(player.MountedCenter, WeaponPrefixVisuals.GetTemporalPulse(player.whoAmI * 0.27f));
			return;
		}

		if (simulationId == ShiftingSimulationId.Radiant) {
			AddRadiantLight(
				player.MountedCenter,
					WeaponPrefixVisuals.GetRadiantPulse(player.whoAmI * 0.35f) * ShiftingSimulationEffectScaling.GetShiftedRadiantLightMultiplier());
			return;
		}

		if (simulationId == ShiftingSimulationId.Temporal) {
			TemporalRuntime.AddHeldLight(
				player.MountedCenter,
				WeaponPrefixVisuals.GetTemporalPulse(player.whoAmI * 0.27f) * ShiftingPrefix.ShiftingStrengthMultiplier);
			return;
		}

		bool hasAttunedPrefix = WeaponPrefixIdentity.HasAttunedPrefix(item);
		bool hasShiftedAttuned = simulationId == ShiftingSimulationId.Attuned;
		if (hasAttunedPrefix || hasShiftedAttuned) {
			EmitAttunedStateFeedback(
				player,
				hasShiftedAttuned ? ShiftingPrefix.ShiftingStrengthMultiplier : 1f);
			return;
		}

		bool hasSkirmishingPrefix = WeaponPrefixIdentity.HasSkirmishingPrefix(item);
		bool hasShiftedSkirmishing = simulationId == ShiftingSimulationId.Skirmishing;
		if (hasSkirmishingPrefix || hasShiftedSkirmishing) {
			ApplySkirmishingMobilityAndFeedback(
				player,
				hasShiftedSkirmishing ? ShiftingPrefix.ShiftingStrengthMultiplier : 1f,
				hasShiftedSkirmishing);
			return;
		}

		bool hasDesperatePrefix = WeaponPrefixIdentity.HasDesperatePrefix(item);
		bool hasShiftedDesperate = simulationId == ShiftingSimulationId.Desperate;
		if (hasDesperatePrefix || hasShiftedDesperate) {
			EmitDesperateStateFeedback(
				player,
				hasShiftedDesperate ? ShiftingPrefix.ShiftingStrengthMultiplier : 1f);
		}
	}

	internal static void EmitAttunedCastReleaseFeedback(Player player, float visualMultiplier)
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

	private static void AddRadiantLight(Vector2 position, float multiplier = 1f)
	{
		Lighting.AddLight(position, 1f * multiplier, 0.74f * multiplier, 0.24f * multiplier);
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
			? ShiftingSimulationEffectScaling.GetShiftedSkirmishingMoveSpeedBonus()
			: SkirmishingPrefix.MobilityMoveSpeedBonus;
		float runAccelerationMultiplier = shifted
			? ShiftingSimulationEffectScaling.GetShiftedSkirmishingRunAccelerationMultiplier()
			: SkirmishingPrefix.MobilityRunAccelerationMultiplier;
		float maxRunSpeedBonus = shifted
			? ShiftingSimulationEffectScaling.GetShiftedSkirmishingMaxRunSpeedBonus()
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
}

internal static class WeaponPrefixItemSwingDispatch
{
	internal static void UseItemHitbox(Item item, Player player, Rectangle hitbox, bool noHitbox)
	{
		if (!WeaponPrefixIdentity.HasAwakenedPrefix(item)) {
			return;
		}

		AwakenedRuntime.RegisterSwingHitboxAndEmitTelegraph(player, item, hitbox, noHitbox);
	}
}

internal static class WeaponPrefixItemShootDispatch
{
	internal static void Dispatch(Item item, Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
	{
		SpinboundPlayer spinboundPlayer = player.GetModPlayer<SpinboundPlayer>();
		AttunedPlayer attunedPlayer = player.GetModPlayer<AttunedPlayer>();
		SkirmishingPlayer skirmishingPlayer = player.GetModPlayer<SkirmishingPlayer>();
		bool hasSpinboundPrefix = WeaponPrefixIdentity.HasSpinboundPrefix(item);
		bool hasShiftedSpinbound = WeaponPrefixIdentity.TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)
			&& simulationId == ShiftingSimulationId.Spinbound;
		bool hasAttunedPrefix = WeaponPrefixIdentity.HasAttunedPrefix(item);
		bool hasShiftedAttuned = simulationId == ShiftingSimulationId.Attuned;
		bool hasSkirmishingPrefix = WeaponPrefixIdentity.HasSkirmishingPrefix(item);
		bool hasShiftedSkirmishing = simulationId == ShiftingSimulationId.Skirmishing;

		if ((hasAttunedPrefix || hasShiftedAttuned)
			&& attunedPlayer.IsEmpoweredProjectileWindowActive()) {
			float attunedShootSpeedMultiplier = hasShiftedAttuned
			? ShiftingSimulationEffectScaling.GetShiftedAttunedEmpoweredShootSpeedMultiplier()
				: AttunedPrefix.EmpoweredCastShootSpeedMultiplier;
			velocity *= attunedShootSpeedMultiplier;
		}

		if ((hasSkirmishingPrefix || hasShiftedSkirmishing)
			&& skirmishingPlayer.TryConsumeFollowUpShot()) {
			float followUpShootSpeedMultiplier = hasShiftedSkirmishing
			? ShiftingSimulationEffectScaling.GetShiftedSkirmishingFollowUpShootSpeedMultiplier()
				: SkirmishingPrefix.FollowUpShootSpeedMultiplier;
			velocity *= followUpShootSpeedMultiplier;
			EmitSkirmishingFollowUpReleaseFeedback(
				player,
				hasShiftedSkirmishing ? ShiftingPrefix.ShiftingStrengthMultiplier : 1f);
		}

		if (hasShiftedSpinbound || hasShiftedAttuned || hasShiftedSkirmishing) {
			velocity *= ShiftingSimulationStatScaling.GetShiftedShootSpeedMultiplier(simulationId);
		}

		if ((hasSpinboundPrefix || hasShiftedSpinbound) && player.active && !player.dead) {
			if (hasSpinboundPrefix) {
				spinboundPlayer.ResolveSpinboundShot(item, velocity, false);
			}

			if (hasShiftedSpinbound) {
				spinboundPlayer.ResolveSpinboundShot(item, velocity, true);
			}
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
}

internal static class WeaponPrefixItemHitDispatch
{
	internal static void ModifyHitNPC(Item item, Player player, NPC target, ref NPC.HitModifiers modifiers)
	{
		if (WeaponPrefixIdentity.HasTemporalPrefix(item)) {
			TemporalRuntime.PrepareFracturedHit(player, target, ref modifiers, 1f);
		}

		if (WeaponPrefixIdentity.HasAwakenedPrefix(item)) {
			AwakenedRuntime.ApplySweetSpotBonus(item, player, target, ref modifiers);
		}

		if (WeaponPrefixIdentity.HasVampiricPrefix(item)) {
			VampiricPreyState preyState = SanguinePrefix.GetPreyState(target);
			player.GetModPlayer<VampiricPlayer>().QueuePendingDirectPreyState(preyState);
			VampiricRuntime.TryApplyFrenzyCrit(player, preyState, ref modifiers, 1f);
		}

		if (WeaponPrefixIdentity.HasDesperatePrefix(item)) {
			TryApplyDesperateSurge(player, ref modifiers, 1f);
		}

		if (!WeaponPrefixIdentity.TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)) {
			return;
		}

		if (simulationId == ShiftingSimulationId.Breaching || simulationId == ShiftingSimulationId.Spinbound) {
			modifiers.ArmorPenetration += simulationId == ShiftingSimulationId.Breaching
			? ShiftingSimulationStatScaling.GetShiftedArmorPenetrationBonus(simulationId)
			: ShiftingSimulationEffectScaling.GetShiftedSpinboundArmorPenetrationBonus();
		}

		if (simulationId == ShiftingSimulationId.Temporal) {
			TemporalRuntime.PrepareFracturedHit(player, target, ref modifiers, ShiftingPrefix.ShiftingStrengthMultiplier);
		}

		if (simulationId == ShiftingSimulationId.Vampiric) {
			VampiricPreyState preyState = SanguinePrefix.GetPreyState(target);
			player.GetModPlayer<VampiricPlayer>().QueuePendingDirectPreyState(preyState);
			VampiricRuntime.TryApplyFrenzyCrit(player, preyState, ref modifiers, ShiftingPrefix.ShiftingStrengthMultiplier);
		}

		if (simulationId == ShiftingSimulationId.Desperate) {
			TryApplyDesperateSurge(player, ref modifiers, ShiftingPrefix.ShiftingStrengthMultiplier, true);
		}
	}

	internal static void OnHitNPC(Item item, Player player, NPC target, NPC.HitInfo hit, int damageDone)
	{
		if (WeaponPrefixIdentity.HasAwakenedPrefix(item) && damageDone > 0) {
			AwakenedRuntime.HandleSwordHit(item, player, target);
		}

		if (WeaponPrefixIdentity.HasShiftingPrefix(item) && damageDone > 0) {
			player.GetModPlayer<ShiftingPlayer>().RegisterSuccessfulHit(item);
		}

		if (WeaponPrefixIdentity.HasAttunedPrefix(item) && damageDone > 0) {
			player.GetModPlayer<AttunedPlayer>().RegisterQualifyingHit();
		}

		if (WeaponPrefixIdentity.HasSkirmishingPrefix(item) && damageDone > 0) {
			player.GetModPlayer<SkirmishingPlayer>().RegisterQualifyingHit();
		}

		if (WeaponPrefixIdentity.HasTemporalPrefix(item) && damageDone > 0) {
			TemporalRuntime.HandleHit(player, target, damageDone, 1f);
		}

		if (WeaponPrefixIdentity.HasVampiricPrefix(item) && damageDone > 0) {
			VampiricRuntime.ApplyItemFeed(player, item, target, damageDone, hit.Crit, 1f);
		}

		if (WeaponPrefixIdentity.HasRadiantPrefix(item) && target.active) {
			ApplyRadiantHitEffects(target);
		}

		if (WeaponPrefixIdentity.HasStormforgedPrefix(item)) {
			StormforgedChainHelper.TryTriggerChain(player, target, damageDone);
		}

		if (WeaponPrefixIdentity.HasBreachingPrefix(item)) {
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

		if (!WeaponPrefixIdentity.TryGetShiftingSimulation(item, player, out ShiftingSimulationId simulationId)) {
			return;
		}

		if (simulationId == ShiftingSimulationId.Vampiric && damageDone > 0) {
			VampiricRuntime.ApplyItemFeed(player, item, target, damageDone, hit.Crit, ShiftingPrefix.ShiftingStrengthMultiplier, true);
		}

		if (simulationId == ShiftingSimulationId.Attuned && damageDone > 0) {
			player.GetModPlayer<AttunedPlayer>().RegisterQualifyingHit();
		}

		if (simulationId == ShiftingSimulationId.Temporal && damageDone > 0) {
			TemporalRuntime.HandleHit(player, target, damageDone, ShiftingPrefix.ShiftingStrengthMultiplier, true);
		}

		if (simulationId == ShiftingSimulationId.Skirmishing && damageDone > 0) {
			player.GetModPlayer<SkirmishingPlayer>().RegisterQualifyingHit();
		}

		if (simulationId == ShiftingSimulationId.Radiant && target.active) {
		ApplyRadiantHitEffects(target, ShiftingSimulationEffectScaling.GetShiftedRadiantDurationTicks());
			return;
		}

		if (simulationId == ShiftingSimulationId.Stormforged) {
			StormforgedChainHelper.TryTriggerChain(
				player,
				target,
				damageDone,
					ShiftingSimulationEffectScaling.GetShiftedStormforgedProcChance(),
					ShiftingSimulationEffectScaling.GetShiftedStormforgedMaxChainJumps(),
					ShiftingSimulationEffectScaling.GetShiftedStormforgedChainRangePixels(),
					ShiftingSimulationEffectScaling.GetShiftedStormforgedChainDamageDecay());
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
			? ShiftingSimulationEffectScaling.GetShiftedDesperateSurgeDamageBonus()
			: DesperatePrefix.SurgeDamageBonus;
		modifiers.SourceDamage *= 1f + surgeDamageBonus;
		desperatePlayer.QueueDirectSurgeFeedback(visualMultiplier * (shifted ? 1.12f : 1f));
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
