using System.Text;
using Microsoft.Xna.Framework;
using Mozandifiers.Common.Players;
using Mozandifiers.Content.Prefixes.Weapons;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace Mozandifiers.Content.Prefixes.Common;

internal static class SpinboundRuntime
{
	private const float SpinboundSoundVolumeMultiplier = 0.85f;

	[System.Flags]
	private enum GoldenSpinMatchFlags : byte
	{
		None = 0,
		VelocityVector = 1 << 0,
		AngleGolden = 1 << 1,
		SpeedPrecision = 1 << 2,
		AnglePrecision = 1 << 3,
		PrecisionRelease = 1 << 4
	}

	private readonly struct GoldenSpinEvaluation
	{
		public GoldenSpinMatchFlags Matches { get; init; }
		public int Tier { get; init; }
		public float PlayerSpeed { get; init; }
		public Vector2 Velocity { get; init; }
		public float VelocityVectorRatio { get; init; }
		public float FoldedAngleDegrees { get; init; }
		public float Speed { get; init; }
		public float SpeedRatio { get; init; }
	}

	internal static void TryInitializeLiveProjectile(WeaponPrefixGlobalProjectile global, Projectile projectile, Item item)
	{
		Player owner = Main.player[projectile.owner];
		Vector2 snapshotVelocity = projectile.velocity;
		bool cadenceBonusTriggered;
		if (!owner.GetModPlayer<SpinboundPlayer>().TryConsumeSpinboundShotContext(item, false, out snapshotVelocity, out cadenceBonusTriggered)) {
			return;
		}

		GoldenSpinEvaluation goldenSpin = EvaluateGoldenSpin(owner, snapshotVelocity);
		int finalTier = GetFinalTier(goldenSpin.Tier, cadenceBonusTriggered);
		if (finalTier <= 0) {
			return;
		}

		global.IsSpinboundProjectile = true;
		global.SpinboundReferenceSpeed = snapshotVelocity.Length();
		global.GoldenSpinTier = finalTier;
		PlaySpawnSounds(projectile, global.GoldenSpinTier);
		SpawnReleaseEffect(projectile, global.GoldenSpinTier);
		ShowCombatText(projectile, goldenSpin, cadenceBonusTriggered, global.GoldenSpinTier);
	}

	internal static void TryInitializeShiftedProjectile(WeaponPrefixGlobalProjectile global, Projectile projectile, Item item)
	{
		Vector2 snapshotVelocity = projectile.velocity;
		bool cadenceBonusTriggered;
		Player owner = Main.player[projectile.owner];
		if (!owner.GetModPlayer<SpinboundPlayer>().TryConsumeSpinboundShotContext(item, true, out snapshotVelocity, out cadenceBonusTriggered)) {
			return;
		}

		int finalTier = GetFinalTier(EvaluateGoldenSpin(owner, snapshotVelocity).Tier, cadenceBonusTriggered);
		if (finalTier <= 0) {
			return;
		}

		global.IsShiftedSpinboundProjectile = true;
		global.ShiftedSpinboundReferenceSpeed = snapshotVelocity.Length();
		global.ShiftedGoldenSpinTier = finalTier;
		PlaySpawnSounds(projectile, global.ShiftedGoldenSpinTier);
		SpawnReleaseEffect(projectile, global.ShiftedGoldenSpinTier);
	}

	internal static void ApplyLiveHitBonus(WeaponPrefixGlobalProjectile global, Projectile projectile, ref NPC.HitModifiers modifiers)
	{
		if (!global.IsSpinboundProjectile || global.HasConsumedSpinboundHitBonus) {
			return;
		}

		float spinboundMultiplier = GetSpecialMultiplier(global.GoldenSpinTier);
		modifiers.SourceDamage *= 1f + SpinboundPrefix.EmpoweredDamageBonus * spinboundMultiplier;
		modifiers.CritDamage += SpinboundPrefix.EmpoweredCritDamageBonus * spinboundMultiplier;
		global.HasConsumedSpinboundHitBonus = true;
		projectile.netUpdate = true;
	}

	internal static void ApplyShiftedHitBonus(WeaponPrefixGlobalProjectile global, Projectile projectile, ref NPC.HitModifiers modifiers)
	{
		if (global.ShiftedSimulationId != ShiftingSimulationId.Spinbound
			|| !global.IsShiftedSpinboundProjectile
			|| global.HasConsumedShiftedSpinboundHitBonus) {
			return;
		}

		float shiftedSpinboundMultiplier = GetSpecialMultiplier(global.ShiftedGoldenSpinTier);
			modifiers.SourceDamage *= 1f + ShiftingSimulationEffectScaling.GetShiftedSpinboundEmpoweredDamageBonus() * shiftedSpinboundMultiplier;
			modifiers.CritDamage += ShiftingSimulationEffectScaling.GetShiftedSpinboundEmpoweredCritDamageBonus() * shiftedSpinboundMultiplier;
			modifiers.ArmorPenetration += ShiftingSimulationEffectScaling.GetShiftedSpinboundArmorPenetrationBonus();
		global.HasConsumedShiftedSpinboundHitBonus = true;
		projectile.netUpdate = true;
	}

	internal static void UpdateProjectileVisual(WeaponPrefixGlobalProjectile global, Projectile projectile, bool shifted)
	{
		if (shifted) {
			ApplyStabilization(projectile, global.ShiftedSpinboundReferenceSpeed);
			AddLight(projectile, global.ShiftedGoldenSpinTier);
			SpawnTrail(projectile, global.ShiftedGoldenSpinTier);
			return;
		}

		ApplyStabilization(projectile, global.SpinboundReferenceSpeed);
		AddLight(projectile, global.GoldenSpinTier);
		SpawnTrail(projectile, global.GoldenSpinTier);
	}

	private static void ApplyStabilization(Projectile projectile, float targetSpeed)
	{
		if (projectile == null || !projectile.active || projectile.velocity.LengthSquared() <= 0.0001f) {
			return;
		}

		float currentSpeed = projectile.velocity.Length();
		if (currentSpeed <= 0f || targetSpeed <= 0f) {
			return;
		}

		float stabilizedSpeed = MathHelper.Lerp(currentSpeed, targetSpeed, SpinboundPrefix.StabilizationStrength);
		Vector2 desiredVelocity = projectile.velocity.SafeNormalize(Vector2.UnitX * projectile.direction) * stabilizedSpeed;
		projectile.velocity = Vector2.Lerp(projectile.velocity, desiredVelocity, SpinboundPrefix.StabilizationStrength);
	}

	private static void SpawnReleaseEffect(Projectile projectile, int goldenSpinTier)
	{
		if (projectile == null || !projectile.active) {
			return;
		}

		float tierThreePlusMultiplier = goldenSpinTier >= 3 ? 1.5f : 1f;
		float glowStrength = goldenSpinTier switch {
			<= 1 => 0.55f,
			<= 3 => 0.78f,
			_ => 1.05f
		};
		float haloStrength = goldenSpinTier >= 4 ? 0.18f + (goldenSpinTier - 4) * 0.06f : 0f;
		Lighting.AddLight(
			projectile.Center,
			0.7f * glowStrength + haloStrength,
			0.56f * glowStrength + haloStrength * 0.75f,
			0.14f * glowStrength + haloStrength * 0.3f);

		int coreBurstCount = ScalePositiveRoundedInt(4 + goldenSpinTier, tierThreePlusMultiplier);
		int ringCount = ScalePositiveRoundedInt(5 + goldenSpinTier, tierThreePlusMultiplier);
		float ringRadius = 8f + goldenSpinTier * 1.7f;
		Vector2 forward = projectile.velocity.SafeNormalize(Vector2.UnitX * projectile.direction);
		Vector2 side = forward.RotatedBy(MathHelper.PiOver2);

		for (int i = 0; i < coreBurstCount; i++) {
			float sideDirection = i % 2 == 0 ? -1f : 1f;
			float layer = 1f + i / 2f;
			Vector2 offset = side * (7f + layer * 3f) * sideDirection;
			int dustType = goldenSpinTier >= 4 && i % 3 == 0 ? DustID.YellowTorch : DustID.GoldFlame;
			Dust dust = Dust.NewDustPerfect(projectile.Center + offset, dustType);
			dust.noGravity = true;
			dust.velocity = forward * (1.4f + goldenSpinTier * 0.08f) + side * (0.45f * sideDirection * layer);
			dust.scale = 0.9f + goldenSpinTier * 0.06f;
			dust.fadeIn = 1.1f;
			ApplyDustPersistence(dust, goldenSpinTier);
		}

		for (int i = 0; i < ringCount; i++) {
			float angle = MathHelper.TwoPi * i / ringCount;
			Vector2 offset = angle.ToRotationVector2() * ringRadius;
			int dustType = goldenSpinTier >= 4 && i % 4 == 0 ? DustID.YellowTorch : DustID.GoldFlame;
			Dust dust = Dust.NewDustPerfect(projectile.Center + offset, dustType);
			dust.noGravity = true;
			dust.velocity = offset.SafeNormalize(Vector2.UnitX) * (1.15f + goldenSpinTier * 0.12f);
			dust.scale = 0.72f + goldenSpinTier * 0.05f;
			dust.fadeIn = 0.9f;
			ApplyDustPersistence(dust, goldenSpinTier);
		}

		if (goldenSpinTier < 5) {
			return;
		}

		int outerRingCount = ScalePositiveRoundedInt(10, tierThreePlusMultiplier);
		float outerRingRadius = ringRadius + 8f;
		for (int i = 0; i < outerRingCount; i++) {
			float angle = MathHelper.TwoPi * i / outerRingCount;
			Vector2 offset = angle.ToRotationVector2() * outerRingRadius;
			Dust dust = Dust.NewDustPerfect(projectile.Center + offset, i % 2 == 0 ? DustID.YellowTorch : DustID.GoldFlame);
			dust.noGravity = true;
			dust.velocity = offset.SafeNormalize(Vector2.UnitX) * 1.9f;
			dust.scale = 1.02f;
			dust.fadeIn = 1f;
			ApplyDustPersistence(dust, goldenSpinTier);
		}
	}

	private static void SpawnTrail(Projectile projectile, int goldenSpinTier)
	{
		if (projectile == null
			|| !projectile.active
			|| projectile.numUpdates != 0
			|| projectile.timeLeft % 3 != 0) {
			return;
		}

		float tierThreePlusMultiplier = goldenSpinTier >= 3 ? 1.5f : 1f;
		int sideCount = goldenSpinTier >= 5 ? ScalePositiveRoundedInt(3, tierThreePlusMultiplier) : goldenSpinTier >= 3 ? ScalePositiveRoundedInt(2, tierThreePlusMultiplier) : 1;
		Vector2 tangent = projectile.velocity.SafeNormalize(Vector2.UnitX * projectile.direction).RotatedBy(MathHelper.PiOver2);
		for (int layer = 0; layer < sideCount; layer++) {
			float offsetDistance = 6f + layer * 3f;
			for (int i = -1; i <= 1; i += 2) {
				int dustType = goldenSpinTier >= 4 && (layer + (i > 0 ? 1 : 0)) % 2 == 0 ? DustID.YellowTorch : DustID.GoldFlame;
				Dust dust = Dust.NewDustPerfect(projectile.Center + tangent * (offsetDistance * i), dustType);
				dust.noGravity = true;
				dust.velocity = projectile.velocity * (0.08f + goldenSpinTier * 0.005f) + tangent * ((0.45f + layer * 0.08f) * i);
				dust.scale = 0.7f + goldenSpinTier * 0.04f;
				dust.fadeIn = goldenSpinTier >= 4 ? 1.02f : 0.9f;
				ApplyDustPersistence(dust, goldenSpinTier);
			}
		}

		if (goldenSpinTier < 3) {
			return;
		}

		float orbitAngle = projectile.timeLeft * 0.3f + projectile.identity * 0.17f;
		float orbitRadius = 7f + goldenSpinTier * 1.2f;
		int spiralCount = goldenSpinTier >= 5 ? ScalePositiveRoundedInt(2, tierThreePlusMultiplier) : ScalePositiveRoundedInt(1, tierThreePlusMultiplier);
		for (int i = 0; i < spiralCount; i++) {
			float angle = orbitAngle + MathHelper.Pi * i;
			Vector2 offset = angle.ToRotationVector2() * orbitRadius;
			Dust spiralDust = Dust.NewDustPerfect(
				projectile.Center + offset,
				goldenSpinTier >= 4 && i == 0 ? DustID.YellowTorch : DustID.GoldFlame);
			spiralDust.noGravity = true;
			spiralDust.velocity = projectile.velocity * 0.06f + offset.SafeNormalize(Vector2.UnitX) * 0.45f;
			spiralDust.scale = 0.68f + goldenSpinTier * 0.035f;
			spiralDust.fadeIn = 0.95f + goldenSpinTier * 0.03f;
			ApplyDustPersistence(spiralDust, goldenSpinTier);
		}
	}

	private static void AddLight(Projectile projectile, int goldenSpinTier)
	{
		float strength = 0.12f + goldenSpinTier * 0.025f;
		float halo = goldenSpinTier >= 4 ? 0.04f + (goldenSpinTier - 4) * 0.015f : 0f;
		Lighting.AddLight(
			projectile.Center,
			0.28f + strength + halo,
			0.21f + strength * 0.75f + halo * 0.7f,
			0.05f + goldenSpinTier * 0.007f + halo * 0.2f);
	}

	private static void PlaySpawnSounds(Projectile projectile, int goldenSpinTier)
	{
		if (projectile == null || !projectile.active || projectile.owner != Main.myPlayer) {
			return;
		}

		SoundEngine.PlaySound(SoundID.Item42 with { Volume = SoundID.Item42.Volume * SpinboundSoundVolumeMultiplier }, projectile.Center);
		if (goldenSpinTier >= 4) {
			SoundEngine.PlaySound(SoundID.Item20 with { Volume = SoundID.Item20.Volume * SpinboundSoundVolumeMultiplier }, projectile.Center);
		}

		if (goldenSpinTier >= 5) {
			SoundEngine.PlaySound(SoundID.Item113 with { Volume = SoundID.Item113.Volume * SpinboundSoundVolumeMultiplier }, projectile.Center);
		}
	}

	private static void ApplyDustPersistence(Dust dust, int goldenSpinTier)
	{
		if (goldenSpinTier < 3) {
			return;
		}

		dust.velocity *= 0.67f;
		dust.scale *= 1.12f;
		dust.fadeIn += 0.25f;
	}

	private static int ScalePositiveRoundedInt(int value, float multiplier)
	{
		return System.Math.Max(1, (int)System.MathF.Round(value * multiplier));
	}

	private static GoldenSpinEvaluation EvaluateGoldenSpin(Player owner, Vector2 shotVelocity)
	{
		Vector2 velocity = shotVelocity;
		float speed = velocity.Length();
		float playerSpeed = owner?.active == true ? owner.velocity.Length() : 0f;
		float velocityVectorRatio = System.MathF.Abs(velocity.X) / System.MathF.Max(System.MathF.Abs(velocity.Y), 0.001f);
		float foldedAngleDegrees = MathHelper.ToDegrees(System.MathF.Atan2(System.MathF.Abs(velocity.Y), System.MathF.Abs(velocity.X)));
		float speedRatio = playerSpeed / System.MathF.Max(speed, 0.001f);
		float angleDeltaDegrees = GetGoldenAngleDeltaDegrees(foldedAngleDegrees);

		GoldenSpinMatchFlags matches = GoldenSpinMatchFlags.None;
		bool velocityVectorMatch = MatchesGoldenRatio(velocityVectorRatio, SpinboundPrefix.VelocityRatioTolerance);
		if (velocityVectorMatch) {
			matches |= GoldenSpinMatchFlags.VelocityVector;
		}

		bool angleGoldenMatch = angleDeltaDegrees <= SpinboundPrefix.AngleToleranceDegrees;
		if (angleGoldenMatch) {
			matches |= GoldenSpinMatchFlags.AngleGolden;
		}

		if (MatchesGoldenRatio(speedRatio, SpinboundPrefix.SpeedRatioTolerance)) {
			matches |= GoldenSpinMatchFlags.SpeedPrecision;
		}

		if (angleDeltaDegrees <= SpinboundPrefix.AnglePrecisionToleranceDegrees) {
			matches |= GoldenSpinMatchFlags.AnglePrecision;
		}

		if (velocityVectorMatch && angleGoldenMatch) {
			matches |= GoldenSpinMatchFlags.PrecisionRelease;
		}

		return new GoldenSpinEvaluation {
			Matches = matches,
			Tier = CountMatches(matches),
			PlayerSpeed = playerSpeed,
			Velocity = velocity,
			VelocityVectorRatio = velocityVectorRatio,
			FoldedAngleDegrees = foldedAngleDegrees,
			Speed = speed,
			SpeedRatio = speedRatio
		};
	}

	private static bool MatchesGoldenRatio(float ratio, float tolerance)
	{
		return MatchesGoldenValue(ratio, SpinboundPrefix.GoldenRatio, tolerance)
			|| MatchesGoldenValue(ratio, SpinboundPrefix.InverseGoldenRatio, tolerance);
	}

	private static bool MatchesGoldenValue(float value, float target, float tolerance)
	{
		return System.MathF.Abs(value - target) <= tolerance;
	}

	private static float GetGoldenAngleDeltaDegrees(float foldedAngleDegrees)
	{
		float lowAngleDelta = System.MathF.Abs(foldedAngleDegrees - SpinboundPrefix.LowGoldenAngleDegrees);
		float highAngleDelta = System.MathF.Abs(foldedAngleDegrees - SpinboundPrefix.HighGoldenAngleDegrees);
		return System.MathF.Min(lowAngleDelta, highAngleDelta);
	}

	private static float GetSpecialMultiplier(int goldenSpinTier)
	{
		return System.Math.Min(SpinboundPrefix.MaxGoldenSpinMultiplier, 1 + goldenSpinTier);
	}

	private static int GetFinalTier(int baseTier, bool cadenceBonusTriggered)
	{
		int finalTier = baseTier + (cadenceBonusTriggered ? 1 : 0);
		return System.Math.Min(SpinboundPrefix.MaxGoldenSpinTier, finalTier);
	}

	private static int CountMatches(GoldenSpinMatchFlags matches)
	{
		int tier = 0;
		if ((matches & GoldenSpinMatchFlags.VelocityVector) != 0) {
			tier++;
		}

		if ((matches & GoldenSpinMatchFlags.AngleGolden) != 0) {
			tier++;
		}

		if ((matches & GoldenSpinMatchFlags.SpeedPrecision) != 0) {
			tier++;
		}

		if ((matches & GoldenSpinMatchFlags.AnglePrecision) != 0) {
			tier++;
		}

		if ((matches & GoldenSpinMatchFlags.PrecisionRelease) != 0) {
			tier++;
		}

		return System.Math.Min(SpinboundPrefix.MaxGoldenSpinTier, tier);
	}

	private static void ShowCombatText(Projectile projectile, GoldenSpinEvaluation goldenSpin, bool cadenceBonusTriggered, int finalTier)
	{
		if (projectile.owner != Main.myPlayer) {
			return;
		}

		StringBuilder text = new($"Spin G{finalTier}");
		AppendTag(text, goldenSpin.Matches, GoldenSpinMatchFlags.VelocityVector, "VV");
		AppendTag(text, goldenSpin.Matches, GoldenSpinMatchFlags.AngleGolden, "AG");
		AppendTag(text, goldenSpin.Matches, GoldenSpinMatchFlags.SpeedPrecision, "SP");
		AppendTag(text, goldenSpin.Matches, GoldenSpinMatchFlags.AnglePrecision, "AP");
		AppendTag(text, goldenSpin.Matches, GoldenSpinMatchFlags.PrecisionRelease, "PR");
		if (cadenceBonusTriggered) {
			text.Append(" FB");
		}
		// CombatText.NewText(projectile.Hitbox, Color.Gold, text.ToString());
		// Main.NewText(BuildDebugText(goldenSpin), Color.Gold);
	}

	private static void AppendTag(StringBuilder text, GoldenSpinMatchFlags matches, GoldenSpinMatchFlags flag, string tag)
	{
		if ((matches & flag) == 0) {
			return;
		}

		text.Append(' ');
		text.Append(tag);
	}

	private static string BuildDebugText(GoldenSpinEvaluation goldenSpin)
	{
		StringBuilder text = new();
		text.AppendLine($"Spin G{goldenSpin.Tier}");
		text.AppendLine($"VV={FormatValue(goldenSpin.VelocityVectorRatio)} need 0.618/1.618 tol {SpinboundPrefix.VelocityRatioTolerance:0.##}");
		text.AppendLine($"AG={FormatValue(goldenSpin.FoldedAngleDegrees)} need {SpinboundPrefix.LowGoldenAngleDegrees:0.##}/{SpinboundPrefix.HighGoldenAngleDegrees:0.##} tol {SpinboundPrefix.AngleToleranceDegrees:0.##}");
		text.AppendLine($"SP={FormatValue(goldenSpin.SpeedRatio)} need 0.618/1.618 tol {SpinboundPrefix.SpeedRatioTolerance:0.##} (shotSpeed={FormatValue(goldenSpin.Speed)} playerSpeed={FormatValue(goldenSpin.PlayerSpeed)})");
		text.AppendLine($"AP={FormatValue(goldenSpin.FoldedAngleDegrees)} need {SpinboundPrefix.LowGoldenAngleDegrees:0.##}/{SpinboundPrefix.HighGoldenAngleDegrees:0.##} tol {SpinboundPrefix.AnglePrecisionToleranceDegrees:0.##}");
		text.AppendLine($"PR={(((goldenSpin.Matches & GoldenSpinMatchFlags.PrecisionRelease) != 0) ? "hit" : "miss")} need VV+AG");
		text.Append($"velocity=({FormatValue(goldenSpin.Velocity.X)},{FormatValue(goldenSpin.Velocity.Y)})");
		return text.ToString();
	}

	private static string FormatValue(float value)
	{
		return float.IsFinite(value) ? value.ToString("0.##") : "n/a";
	}
}
