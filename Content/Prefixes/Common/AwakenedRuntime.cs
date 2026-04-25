using Microsoft.Xna.Framework;
using Mozandifiers.Common.Combat;
using Mozandifiers.Common.Players;
using Mozandifiers.Content.Prefixes.Weapons;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace Mozandifiers.Content.Prefixes.Common;

internal static class AwakenedRuntime
{
	internal static void ApplySweetSpotBonus(Item item, Player player, NPC target, ref NPC.HitModifiers modifiers)
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
			modifiers.Knockback *= AwakenedPrefix.AwakenedSweetSpotKnockbackMultiplier;
		}
	}

	internal static void HandleSwordHit(Item item, Player player, NPC target)
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

		if (player.whoAmI == Main.myPlayer) {
			SpawnSweetSpotContactEffect(hitPosition, tipPosition, tipProgress, awakenedPlayer.IsAwakened);
		}

		if (awakenedPlayer.IsAwakened) {
			if (player.whoAmI == Main.myPlayer && awakenedPlayer.CanEmitAwakenedStrikeVisual()) {
				SpawnStrikeEffect(target.Center, tipPosition, tipProgress);
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
			SpawnStartEffect(player, tipPosition);
			SoundEngine.PlaySound(SoundID.Item94, player.MountedCenter);
			return;
		}

		SpawnBuildEffect(hitPosition, tipPosition, tipProgress, awakenedPlayer.MeterProgress);
	}

	internal static void EmitStateFeedback(Player player)
	{
		if (player == null || !player.active || player.dead) {
			return;
		}

		AwakenedPlayer awakenedPlayer = player.GetModPlayer<AwakenedPlayer>();
		float stateProgress = awakenedPlayer.GetStateProgress();
		if (stateProgress <= 0f && awakenedPlayer.CurrentState == AwakenedState.Dormant) {
			return;
		}

		float pulse = WeaponPrefixVisuals.GetAwakenedPulse(player.whoAmI * 0.37f);
		Color stateColor = WeaponPrefixVisuals.GetAwakenedColor(awakenedPlayer.CurrentState, stateProgress);
		Vector2 position = player.MountedCenter + new Vector2(player.direction * 10f, -8f);
		float lightStrength = awakenedPlayer.CurrentState switch {
			AwakenedState.Awakened => 0.26f,
			AwakenedState.RecoveryLockout => 0.06f + (1f - stateProgress) * 0.05f,
			_ => 0.08f + stateProgress * 0.08f
		};
		AddLight(position, stateColor, lightStrength * pulse);

		bool canEmitAura = awakenedPlayer.CurrentState switch {
			AwakenedState.Awakened => awakenedPlayer.CanEmitAwakenedAuraVisual(),
			AwakenedState.RecoveryLockout => awakenedPlayer.CanEmitDormantAuraVisual(),
			_ => awakenedPlayer.CanEmitDormantAuraVisual()
		};
		if (player.whoAmI != Main.myPlayer || !canEmitAura) {
			return;
		}

		int dustCount = awakenedPlayer.CurrentState switch {
			AwakenedState.Awakened => 3,
			AwakenedState.RecoveryLockout => 2,
			_ => 1
		};
		for (int i = 0; i < dustCount; i++) {
			Vector2 offset = Main.rand.NextVector2Circular(12f, 14f);
			Dust dust = Dust.NewDustPerfect(position + offset, WeaponPrefixVisuals.AwakenedDustType);
			dust.noGravity = true;
			dust.color = awakenedPlayer.CurrentState == AwakenedState.RecoveryLockout
				? Color.Lerp(WeaponPrefixVisuals.AwakenedSealedColor, WeaponPrefixVisuals.AwakenedActiveColor, 0.18f)
				: stateColor;
			dust.velocity = offset.SafeNormalize(Vector2.UnitY) * (awakenedPlayer.IsAwakened ? 0.35f : awakenedPlayer.IsInRecoveryLockout ? 0.2f : 0.14f);
			dust.scale = (awakenedPlayer.IsAwakened ? 0.95f : awakenedPlayer.IsInRecoveryLockout ? 0.78f : 0.72f + stateProgress * 0.12f) * pulse;
			dust.fadeIn = 0.95f;
		}

		if (awakenedPlayer.IsAwakened) {
			SpawnAwakenedLightningAura(position, pulse);
		}
	}

	internal static void RegisterSwingHitboxAndEmitTelegraph(Player player, Item item, Rectangle hitbox, bool noHitbox)
	{
		if (player == null
			|| !player.active
			|| player.dead
			|| item == null
			|| item.IsAir) {
			return;
		}

		AwakenedPlayer awakenedPlayer = player.GetModPlayer<AwakenedPlayer>();
		awakenedPlayer.UpdateCurrentSwingHitbox(hitbox, noHitbox);
		if (noHitbox
			|| player.whoAmI != Main.myPlayer
			|| player.itemAnimation <= 0) {
			return;
		}

		if (!awakenedPlayer.CanEmitSweetSpotTelegraph()) {
			return;
		}

		if (!AwakenedCombatHelper.TryGetSweetSpotSegment(
			player,
			item,
			awakenedPlayer.GetCurrentScaleMultiplier(),
			out _,
			out Vector2 sweetSpotStart,
			out Vector2 tipPosition)) {
			return;
		}

		//SpawnSwingTelegraphEffect(sweetSpotStart, tipPosition, awakenedPlayer.IsAwakened);
	}

	private static void SpawnBuildEffect(Vector2 hitPosition, Vector2 tipPosition, float tipProgress, float meterProgress)
	{
		Vector2 direction = (tipPosition - hitPosition).SafeNormalize(Vector2.UnitX);
		Color buildColor = WeaponPrefixVisuals.GetAwakenedColor(AwakenedState.Dormant, meterProgress);
		AddLight(hitPosition, buildColor, 0.12f + meterProgress * 0.05f);
		SpawnSweetSpotLineEffect(hitPosition, tipPosition, buildColor, 0.65f + meterProgress * 0.15f, false);

		for (int i = 0; i < 2; i++) {
			Dust dust = Dust.NewDustPerfect(
				hitPosition + Main.rand.NextVector2Circular(6f, 6f),
				WeaponPrefixVisuals.AwakenedDustType,
				direction.RotatedByRandom(0.35f) * Main.rand.NextFloat(0.45f, 0.95f));
			dust.noGravity = true;
			dust.color = Color.Lerp(WeaponPrefixVisuals.AwakenedDormantColor, Color.Black, 0.2f);
			dust.scale = 0.72f + meterProgress * 0.1f + tipProgress * 0.08f;
			dust.fadeIn = 0.9f;
		}
	}

	private static void SpawnStartEffect(Player player, Vector2 tipPosition)
	{
		Vector2 origin = player.MountedCenter + new Vector2(player.direction * 10f, -6f);
		AddLight(origin, WeaponPrefixVisuals.AwakenedActiveColor, 0.34f);

		for (int i = 0; i < 8; i++) {
			Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(0.9f, 1.8f);
			Dust dust = Dust.NewDustPerfect(origin, WeaponPrefixVisuals.AwakenedDustType, velocity);
			dust.noGravity = true;
			dust.color = WeaponPrefixVisuals.AwakenedActiveColor;
			dust.scale = 0.92f;
			dust.fadeIn = 1f;
		}

		for (int i = 0; i < 3; i++) {
			Vector2 velocity = (tipPosition - origin).SafeNormalize(Vector2.UnitX).RotatedByRandom(0.28f) * Main.rand.NextFloat(0.8f, 1.5f);
			Dust dust = Dust.NewDustPerfect(origin, WeaponPrefixVisuals.AwakenedDustType, velocity);
			dust.noGravity = true;
			dust.color = Color.Lerp(WeaponPrefixVisuals.AwakenedActiveColor, Color.White, 0.15f);
			dust.scale = 1f;
			dust.fadeIn = 0.95f;
		}
	}

	private static void SpawnStrikeEffect(Vector2 targetCenter, Vector2 tipPosition, float tipProgress)
	{
		Vector2 direction = (targetCenter - tipPosition).SafeNormalize(Vector2.UnitX);
		AddLight(targetCenter, WeaponPrefixVisuals.AwakenedActiveColor, 0.28f);
		SpawnSweetSpotLineEffect(tipPosition, targetCenter, WeaponPrefixVisuals.AwakenedActiveColor, 0.92f + tipProgress * 0.12f, true);

		for (int i = 0; i < 4; i++) {
			Vector2 velocity = direction.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.9f, 1.8f);
			Dust dust = Dust.NewDustPerfect(
				targetCenter + Main.rand.NextVector2Circular(10f, 10f),
				WeaponPrefixVisuals.AwakenedDustType,
				velocity);
			dust.noGravity = true;
			dust.color = WeaponPrefixVisuals.AwakenedActiveColor;
			dust.scale = 0.86f + tipProgress * 0.1f;
			dust.fadeIn = 0.96f;
		}
	}

	private static void SpawnSweetSpotLineEffect(Vector2 start, Vector2 end, Color color, float scaleMultiplier, bool awakened)
	{
		Vector2 segment = end - start;
		float length = segment.Length();
		if (length <= 4f) {
			return;
		}

		Vector2 direction = segment / length;
		Vector2 tangent = direction.RotatedBy(MathHelper.PiOver2);
		int steps = System.Math.Max(3, (int)System.MathF.Round(length / 10f));
		for (int step = 0; step <= steps; step++) {
			float progress = step / (float)steps;
			Vector2 position = Vector2.Lerp(start, end, progress);
			Vector2 velocity = tangent * Main.rand.NextFloat(-0.12f, 0.12f) + direction * Main.rand.NextFloat(0.08f, 0.22f);
			Dust lineDust = Dust.NewDustPerfect(
				position + tangent * Main.rand.NextFloat(-2f, 2f),
				WeaponPrefixVisuals.AwakenedDustType,
				velocity,
				0,
				color,
				(scaleMultiplier + progress * 0.08f) * (awakened ? 1f : 0.86f));
			lineDust.noGravity = true;
			lineDust.color = color;
			lineDust.fadeIn = awakened ? 1f : 0.92f;
		}
	}

	private static void SpawnAwakenedLightningAura(Vector2 center, float pulse)
	{
		Vector2 baseDirection = Main.rand.NextVector2CircularEdge(1f, 1f);
		float arcLength = 12f + Main.rand.NextFloat(4f, 9f) * pulse;
		Vector2 start = center + baseDirection * Main.rand.NextFloat(6f, 12f);
		Vector2 mid = start + baseDirection.RotatedBy(Main.rand.NextFloat(-0.65f, 0.65f)) * (arcLength * 0.55f);
		Vector2 end = mid + baseDirection.RotatedBy(Main.rand.NextFloat(-0.45f, 0.45f)) * (arcLength * 0.45f);

		SpawnLightningSegment(start, mid, 0.94f * pulse);
		SpawnLightningSegment(mid, end, 0.82f * pulse);
		AddLight(end, WeaponPrefixVisuals.AwakenedActiveColor, 0.1f * pulse);
	}

	private static void SpawnLightningSegment(Vector2 start, Vector2 end, float scaleMultiplier)
	{
		Vector2 segment = end - start;
		float length = segment.Length();
		if (length <= 2f) {
			return;
		}

		Vector2 direction = segment / length;
		Vector2 tangent = direction.RotatedBy(MathHelper.PiOver2);
		int steps = System.Math.Max(2, (int)System.MathF.Round(length / 5f));
		for (int step = 0; step <= steps; step++) {
			float progress = step / (float)steps;
			Vector2 position = Vector2.Lerp(start, end, progress) + tangent * Main.rand.NextFloat(-1.2f, 1.2f);
			Dust dust = Dust.NewDustPerfect(
				position,
				WeaponPrefixVisuals.AwakenedDustType,
				direction * Main.rand.NextFloat(0.04f, 0.12f),
				0,
				Color.Lerp(WeaponPrefixVisuals.AwakenedActiveColor, Color.White, 0.18f),
				0.8f * scaleMultiplier);
			dust.noGravity = true;
			dust.color = Color.Lerp(WeaponPrefixVisuals.AwakenedActiveColor, Color.Black, 0.12f);
			dust.fadeIn = 1f;
		}
	}

	private static void SpawnSweetSpotContactEffect(Vector2 hitPosition, Vector2 tipPosition, float tipProgress, bool awakened)
	{
		Vector2 direction = (tipPosition - hitPosition).SafeNormalize(Vector2.UnitX);
		Vector2 diagonalA = direction.RotatedBy(MathHelper.PiOver4);
		Vector2 diagonalB = direction.RotatedBy(-MathHelper.PiOver4);
		float scale = 0.86f + tipProgress * 0.08f + (awakened ? 0.1f : 0f);
		float halfLength = 7f + tipProgress * 2f + (awakened ? 1.5f : 0f);

		SpawnSweetSpotCrossStroke(hitPosition, diagonalA, Color.White, scale, halfLength);
		SpawnSweetSpotCrossStroke(hitPosition, diagonalB, new Color(255, 222, 120), scale * 0.96f, halfLength);
		AddLight(hitPosition, Color.Lerp(Color.White, new Color(255, 214, 96), 0.45f), awakened ? 0.16f : 0.1f);
	}

	private static void SpawnSweetSpotCrossStroke(Vector2 center, Vector2 diagonal, Color color, float scale, float halfLength)
	{
		for (int step = -2; step <= 2; step++) {
			float progress = step / 2f;
			Vector2 position = center + diagonal * (halfLength * progress);
			Vector2 velocity = diagonal * (0.22f * progress);
			Dust dust = Dust.NewDustPerfect(position, WeaponPrefixVisuals.AwakenedDustType, velocity, 0, color, scale);
			dust.noGravity = true;
			dust.color = color;
			dust.fadeIn = 1f;
		}
	}

	private static void SpawnSwingTelegraphEffect(Vector2 sweetSpotStart, Vector2 tipPosition, bool awakened)
	{
		Vector2 direction = (tipPosition - sweetSpotStart).SafeNormalize(Vector2.UnitX);
		Vector2 tangent = direction.RotatedBy(MathHelper.PiOver2);
		Color telegraphColor = Color.Lerp(Color.White, WeaponPrefixVisuals.AwakenedActiveColor, awakened ? 0.18f : 0.08f);
		AddLight(tipPosition, telegraphColor, awakened ? 0.16f : 0.1f);

		for (int i = 0; i < 3; i++) {
			float progress = i / 2f;
			Vector2 position = Vector2.Lerp(sweetSpotStart, tipPosition, progress);
			Vector2 velocity = tangent * Main.rand.NextFloat(-0.08f, 0.08f) + direction * Main.rand.NextFloat(0.06f, 0.14f);
			Dust dust = Dust.NewDustPerfect(
				position + Main.rand.NextVector2Circular(2f, 2f),
				WeaponPrefixVisuals.AwakenedDustType,
				velocity,
				0,
				telegraphColor,
				awakened ? 0.84f : 0.74f);
			dust.noGravity = true;
			dust.color = telegraphColor;
			dust.fadeIn = 0.95f;
		}
	}

	private static void AddLight(Vector2 position, Color color, float multiplier = 1f)
	{
		Lighting.AddLight(position, color.ToVector3() * multiplier);
	}
}
