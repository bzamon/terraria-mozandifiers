using Microsoft.Xna.Framework;
using Mozandifiers.Common.Players;
using Mozandifiers.Content.Prefixes.Weapons;
using Terraria;
using Terraria.ID;

namespace Mozandifiers.Content.Prefixes.Common;

internal static class TemporalRuntime
{
	internal static void PrepareFracturedHit(Player player, NPC target, ref NPC.HitModifiers modifiers, float visualMultiplier = 1f)
	{
		if (player == null
			|| !player.active
			|| player.dead
			|| target == null
			|| !target.active) {
			return;
		}

		WeaponPrefixGlobalNPC targetGlobal = target.GetGlobalNPC<WeaponPrefixGlobalNPC>();
		int playerIndex = player.whoAmI;
		if (!targetGlobal.IsTemporalFracturedForPlayer(playerIndex)) {
			return;
		}

		modifiers.ModifyHitInfo += (ref NPC.HitInfo info) => {
			if (info.Damage <= 0) {
				return;
			}

			int storedDamage = targetGlobal.StoreTemporalDamage(playerIndex, info.Damage);
			info.Damage = 0;
			if (storedDamage >= System.Math.Max(1, target.life)) {
				targetGlobal.TryTriggerTemporalCollapse(target, playerIndex, visualMultiplier);
			}
		};

		modifiers.HideCombatText();
	}

	internal static void HandleHit(Player player, NPC target, int damageDone, float visualMultiplier = 1f, bool shifted = false)
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
		WeaponPrefixGlobalNPC.TemporalPressureBuildResult buildResult = targetGlobal.TryBuildTemporalPressure(playerIndex, target, damageDone);
		if (buildResult.PressureAdded <= 0f || playerIndex != Main.myPlayer) {
			return;
		}

		TemporalFeedbackPlayer feedbackPlayer = player.GetModPlayer<TemporalFeedbackPlayer>();
		if (!feedbackPlayer.CanEmitPressureVisual()) {
			return;
		}

		SpawnPressureEffect(
			target.Center,
			buildResult.CurrentPressure / TemporalPrefix.PressureMax,
			buildResult.StartedFracture,
			shifted ? ShiftingPrefix.ShiftingStrengthMultiplier : visualMultiplier);
	}

	internal static void EmitUseFeedback(Player player, float visualMultiplier)
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
		AddHeldLight(center, pulse);

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

	internal static void AddHeldLight(Vector2 position, float multiplier = 1f)
	{
		Lighting.AddLight(position, 0.18f * multiplier, 0.26f * multiplier, 0.38f * multiplier);
	}

	internal static void SpawnCollapseEffect(Vector2 center, float visualMultiplier = 1f)
	{
		Color collapseColor = WeaponPrefixVisuals.TemporalCollapseColor;
		AddHeldLight(center, (0.9f + 0.15f * visualMultiplier) * visualMultiplier);

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

	internal static void UpdateProjectileVisual(Projectile projectile, float intensityMultiplier)
	{
		float pulse = WeaponPrefixVisuals.GetTemporalPulse(projectile.identity * 0.19f) * intensityMultiplier;
		Lighting.AddLight(projectile.Center, 0.15f * pulse, 0.24f * pulse, 0.36f * pulse);

		if (projectile.numUpdates == 0 && projectile.timeLeft % 6 == 0) {
			Vector2 lateralOffset = projectile.velocity.SafeNormalize(Vector2.UnitX * projectile.direction)
				.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-6f, 6f);
			Dust dust = Dust.NewDustPerfect(projectile.Center + lateralOffset, WeaponPrefixVisuals.TemporalDustType);
			dust.noGravity = true;
			dust.velocity = projectile.velocity * -0.035f + Main.rand.NextVector2Circular(0.12f, 0.12f);
			dust.scale = 0.72f + 0.08f * intensityMultiplier;
			dust.fadeIn = 0.95f;
		}
	}

	private static void SpawnPressureEffect(Vector2 center, float pressureRatio, bool startedFracture, float visualMultiplier)
	{
		float clampedRatio = MathHelper.Clamp(pressureRatio, 0f, 1f);
		Color pressureColor = startedFracture
			? WeaponPrefixVisuals.TemporalFractureColor
			: WeaponPrefixVisuals.GetTemporalPressureColor(clampedRatio);
		float lightMultiplier = startedFracture ? 0.42f : 0.16f + clampedRatio * 0.12f;
		AddHeldLight(center, lightMultiplier * visualMultiplier);

		int dustCount = startedFracture ? 7 : 3;
		float burstSpeed = startedFracture ? 1.5f : 0.7f + clampedRatio * 0.4f;
		for (int i = 0; i < dustCount; i++) {
			Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * burstSpeed;
			Dust dust = Dust.NewDustPerfect(center, WeaponPrefixVisuals.TemporalDustType, velocity, 0, pressureColor, (0.72f + clampedRatio * 0.18f) * visualMultiplier);
			dust.noGravity = true;
			dust.fadeIn = startedFracture ? 1f : 0.92f;
		}
	}
}
