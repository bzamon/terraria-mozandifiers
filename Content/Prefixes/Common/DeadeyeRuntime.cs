using Microsoft.Xna.Framework;
using Mozandifiers.Common.Players;
using Mozandifiers.Content.Prefixes.Weapons;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Common;

internal static class DeadeyeRuntime
{
	internal static void EmitStateFeedback(Player player, Item item, float strengthMultiplier)
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
			SpawnReadyEffect(auraCenter, strengthMultiplier);
			if (player.whoAmI == Main.myPlayer && deadeyePlayer.CanPlayReadySound()) {
				SoundEngine.PlaySound(SoundID.Item149 with { Pitch = -0.2f, Volume = 0.45f }, auraCenter);
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

	internal static void TryApplyProjectileCrit(Projectile projectile, ref NPC.HitModifiers modifiers, float strengthMultiplier, bool shifted = false)
	{
		if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers) {
			return;
		}

		Player owner = Main.player[projectile.owner];
		if (!owner.active || owner.dead) {
			return;
		}

		int critBonus = shifted
			? ShiftingSimulationEffectScaling.GetShiftedDeadeyeCritChanceBonus()
			: DeadeyePrefix.DeadeyeShotCritChanceBonus;
		if (critBonus <= 0) {
			return;
		}

		float critChance = System.MathF.Min(0.95f, (critBonus * System.MathF.Max(1f, strengthMultiplier)) / 100f);
		if (Main.rand.NextFloat() < critChance) {
			modifiers.SetCrit();
		}
	}

	internal static void HandleProjectileHit(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone, float strengthMultiplier, bool shifted = false)
	{
		SpawnImpactEffect(target.Center, projectile.velocity, strengthMultiplier, hit.Crit);
		if (!hit.Crit) {
			return;
		}

		TryTriggerBurst(projectile, target, damageDone, strengthMultiplier, shifted);
	}

	internal static void UpdateProjectileVisual(Projectile projectile, float strengthMultiplier)
	{
		float pulse = 0.8f + 0.2f * (0.5f + 0.5f * System.MathF.Sin(Main.GlobalTimeWrappedHourly * 10.5f + projectile.identity * 0.23f));
		Lighting.AddLight(projectile.Center, WeaponPrefixVisuals.DeadeyeShotColor.ToVector3() * (0.14f * pulse * strengthMultiplier));

		if (projectile.owner != Main.myPlayer || projectile.numUpdates != 0) {
			return;
		}

		if (projectile.timeLeft % 5 == 0) {
			Dust shotDust = Dust.NewDustPerfect(
				projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
				WeaponPrefixVisuals.DeadeyeShotDustType,
				projectile.velocity * -0.06f + Main.rand.NextVector2Circular(0.1f, 0.1f),
				0,
				WeaponPrefixVisuals.DeadeyeShotColor,
				0.8f * strengthMultiplier);
			shotDust.noGravity = true;
			shotDust.fadeIn = 0.92f;
		}

		if (projectile.timeLeft % 9 == 0) {
			Dust leafDust = Dust.NewDustPerfect(
				projectile.Center + Main.rand.NextVector2Circular(3f, 3f),
				WeaponPrefixVisuals.DeadeyeLeafDustType,
				projectile.velocity * -0.03f + Main.rand.NextVector2Circular(0.08f, 0.08f),
				0,
				WeaponPrefixVisuals.DeadeyeLeafAccentColor,
				0.72f * strengthMultiplier);
			leafDust.noGravity = true;
			leafDust.fadeIn = 0.9f;
		}
	}

	private static void SpawnReadyEffect(Vector2 position, float strengthMultiplier)
	{
		Lighting.AddLight(position, WeaponPrefixVisuals.DeadeyeReadyColor.ToVector3() * (0.26f * strengthMultiplier));
		for (int i = 0; i < 8; i++) {
			Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(0.9f, 1.8f) * strengthMultiplier;
			Dust dust = Dust.NewDustPerfect(position, WeaponPrefixVisuals.DeadeyeReadyDustType, velocity, 0, WeaponPrefixVisuals.DeadeyeReadyColor, 0.92f * strengthMultiplier);
			dust.noGravity = true;
			dust.fadeIn = 0.95f;
		}
	}

	private static void SpawnImpactEffect(Vector2 position, Vector2 projectileVelocity, float strengthMultiplier, bool critical)
	{
		Vector2 direction = projectileVelocity.SafeNormalize(Vector2.UnitX);
		Vector2 tangent = direction.RotatedBy(MathHelper.PiOver2);
		Color impactColor = critical ? WeaponPrefixVisuals.DeadeyeLeafAccentColor : WeaponPrefixVisuals.DeadeyeShotColor;
		Lighting.AddLight(position, impactColor.ToVector3() * ((critical ? 0.22f : 0.14f) * strengthMultiplier));

		int dustCount = critical ? 7 : 4;
		for (int i = 0; i < dustCount; i++) {
			float side = i % 2 == 0 ? -1f : 1f;
			float lane = 1f + i / 2f;
			Vector2 velocity = direction * (critical ? 1.2f : 0.8f) + tangent * (0.24f * lane * side);
			Dust dust = Dust.NewDustPerfect(
				position + tangent * (2.5f * lane * side),
				critical && i % 2 == 0 ? WeaponPrefixVisuals.DeadeyeLeafDustType : WeaponPrefixVisuals.DeadeyeShotDustType,
				velocity,
				0,
				critical && i % 2 == 0 ? WeaponPrefixVisuals.DeadeyeLeafAccentColor : impactColor,
				(critical ? 0.95f : 0.78f) * strengthMultiplier);
			dust.noGravity = true;
			dust.fadeIn = critical ? 0.95f : 0.88f;
		}
	}

	private static void TryTriggerBurst(Projectile projectile, NPC primaryTarget, int damageDone, float strengthMultiplier, bool shifted)
	{
		if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers || damageDone <= 0 || primaryTarget == null || !primaryTarget.active) {
			return;
		}

		Player owner = Main.player[projectile.owner];
		if (!owner.active || owner.dead) {
			return;
		}

		int baseBurstDamage = (int)System.MathF.Max(
			1f,
			System.MathF.Round(damageDone * (shifted
			? ShiftingSimulationEffectScaling.GetShiftedDeadeyeBurstDamageRatio()
				: DeadeyePrefix.CriticalBurstDamageRatio)));

		SpawnBurstEffect(primaryTarget.Center, strengthMultiplier);
		if (owner.whoAmI == Main.myPlayer && owner.GetModPlayer<DeadeyePlayer>().CanPlayCritBurstSound()) {
			SoundEngine.PlaySound(SoundID.Item60 with { Pitch = 0.20f, Volume = 0.70f }, primaryTarget.Center);
		}

		if (owner.whoAmI != Main.myPlayer) {
			return;
		}

		float maxDistanceSquared = DeadeyePrefix.CriticalBurstRadiusPixels * DeadeyePrefix.CriticalBurstRadiusPixels;
		foreach (NPC candidate in Main.npc) {
			if (!candidate.active
				|| candidate.whoAmI == primaryTarget.whoAmI
				|| candidate.friendly
				|| candidate.dontTakeDamage
				|| (!candidate.CanBeChasedBy() && !candidate.immortal)
				|| Vector2.DistanceSquared(primaryTarget.Center, candidate.Center) > maxDistanceSquared) {
				continue;
			}

			int burstDamage = candidate.boss
				? (int)System.MathF.Max(1f, System.MathF.Round(baseBurstDamage * DeadeyePrefix.BossBurstDamageMultiplier))
				: baseBurstDamage;
			Projectile.NewProjectile(
				projectile.GetSource_FromThis("DeadeyeBurst"),
				candidate.Center,
				Vector2.Zero,
				ModContent.ProjectileType<Content.Projectiles.Weapons.DeadeyeBurstProjectile>(),
				burstDamage,
				0f,
				owner.whoAmI,
				candidate.whoAmI);
		}
	}

	private static void SpawnBurstEffect(Vector2 position, float strengthMultiplier)
	{
		float burstRadius = DeadeyePrefix.CriticalBurstRadiusPixels;
		Color leafColor = WeaponPrefixVisuals.DeadeyeLeafBurstColor;
		Color accentColor = WeaponPrefixVisuals.DeadeyeLeafAccentColor;
		Lighting.AddLight(position, leafColor.ToVector3() * (0.32f * strengthMultiplier));

		for (int i = 0; i < 14; i++) {
			float angle = MathHelper.TwoPi * i / 14f;
			Vector2 direction = angle.ToRotationVector2();
			Vector2 velocity = direction * Main.rand.NextFloat(1f, 2.2f) * strengthMultiplier;

			Dust coreLeaf = Dust.NewDustPerfect(position, WeaponPrefixVisuals.DeadeyeLeafDustType, velocity, 0, leafColor, 0.96f * strengthMultiplier);
			coreLeaf.noGravity = true;
			coreLeaf.fadeIn = 0.98f;

			if (i % 2 == 0) {
				Dust accentLeaf = Dust.NewDustPerfect(position, WeaponPrefixVisuals.DeadeyeLeafDustType, velocity * 0.7f, 0, accentColor, 0.84f * strengthMultiplier);
				accentLeaf.noGravity = true;
				accentLeaf.fadeIn = 0.94f;
			}
		}

		for (int i = 0; i < 18; i++) {
			float angle = MathHelper.TwoPi * i / 18f;
			Vector2 direction = angle.ToRotationVector2();
			Vector2 ringPosition = position + direction * burstRadius;
			Vector2 inwardVelocity = direction * Main.rand.NextFloat(-0.35f, -0.1f) * strengthMultiplier;

			Dust ringLeaf = Dust.NewDustPerfect(ringPosition, WeaponPrefixVisuals.DeadeyeLeafDustType, inwardVelocity, 0, accentColor, 0.88f * strengthMultiplier);
			ringLeaf.noGravity = true;
			ringLeaf.fadeIn = 1f;
		}

		int vineCount = 6;
		for (int vineIndex = 0; vineIndex < vineCount; vineIndex++) {
			float baseAngle = MathHelper.TwoPi * vineIndex / vineCount + Main.rand.NextFloat(-0.18f, 0.18f);
			Vector2 direction = baseAngle.ToRotationVector2();
			float vineLength = burstRadius * Main.rand.NextFloat(0.75f, 1f);
			int vineSegments = 7;

			for (int segment = 1; segment <= vineSegments; segment++) {
				float progress = segment / (float)vineSegments;
				Vector2 curveOffset = direction.RotatedBy(System.MathF.Sin(progress * MathHelper.Pi) * 0.35f) * (vineLength * progress);
				Vector2 segmentPosition = position + curveOffset;
				Vector2 segmentVelocity = direction.RotatedByRandom(0.25f) * Main.rand.NextFloat(0.12f, 0.35f) * strengthMultiplier;

				Dust vineLeaf = Dust.NewDustPerfect(
					segmentPosition,
					WeaponPrefixVisuals.DeadeyeLeafDustType,
					segmentVelocity,
					0,
					Color.Lerp(leafColor, accentColor, progress),
					(0.62f + 0.12f * progress) * strengthMultiplier);
				vineLeaf.noGravity = true;
				vineLeaf.fadeIn = 0.92f;

				if (segment % 2 == 0) {
					Dust vineAccent = Dust.NewDustPerfect(
						segmentPosition + Main.rand.NextVector2Circular(3f, 3f),
						WeaponPrefixVisuals.DeadeyeLeafDustType,
						segmentVelocity * 0.65f,
						0,
						accentColor,
						(0.56f + 0.08f * progress) * strengthMultiplier);
					vineAccent.noGravity = true;
					vineAccent.fadeIn = 0.9f;
				}
			}
		}
	}
}
