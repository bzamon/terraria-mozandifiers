using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mozandifiers.Content.Prefixes.Weapons;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;

namespace Mozandifiers.Content.Prefixes.Common;

internal static class FracturedRuntime
{
	internal static void ApplyDamageModifier(WeaponPrefixGlobalProjectile global, ref NPC.HitModifiers modifiers)
	{
		if (global.IsFracturedDamageProjectile) {
			modifiers.SourceDamage *= global.FracturedDamageMultiplier > 0f ? global.FracturedDamageMultiplier : EchoingPrefix.FractureIDamageMultiplier;
		}
	}

	internal static void ApplyPlayerDamageModifier(WeaponPrefixGlobalProjectile global, ref Player.HurtModifiers modifiers)
	{
		if (global.IsFracturedDamageProjectile) {
			modifiers.SourceDamage *= global.FracturedDamageMultiplier > 0f ? global.FracturedDamageMultiplier : EchoingPrefix.FractureIDamageMultiplier;
		}
	}

	internal static void ScheduleCascade(WeaponPrefixGlobalProjectile global, Projectile projectile, float fractureIChance, float fractureIIChance, float fractureIIIChance)
	{
		if (projectile == null
			|| !projectile.active
			|| global.IsSpawnedFracturedProjectile
			|| Main.rand.NextFloat() >= fractureIChance) {
			return;
		}

		InitializeSourceState(global, projectile);
		SpawnFracturedProjectile(global, projectile, 1);

		if (Main.rand.NextFloat() >= fractureIIChance) {
			return;
		}

		global.hasPendingFractureII = true;
		global.pendingFractureIIFrames = Main.rand.Next(EchoingPrefix.FractureIIDelayMinFrames, EchoingPrefix.FractureIIDelayMaxFrames + 1);
		if (Main.rand.NextFloat() >= fractureIIIChance) {
			return;
		}

		global.hasPendingFractureIII = true;
		int fractureIIIMinFrames = System.Math.Min(
			EchoingPrefix.FractureIIIDelayMaxFrames,
			System.Math.Max(EchoingPrefix.FractureIIIDelayMinFrames, global.pendingFractureIIFrames + 1));
		global.pendingFractureIIIFrames = Main.rand.Next(fractureIIIMinFrames, EchoingPrefix.FractureIIIDelayMaxFrames + 1);
	}

	internal static void ProcessPending(WeaponPrefixGlobalProjectile global, Projectile projectile)
	{
		if (projectile == null
			|| !projectile.active
			|| projectile.owner != Main.myPlayer
			|| projectile.numUpdates != 0) {
			return;
		}

		if (global.hasPendingFractureII && --global.pendingFractureIIFrames <= 0) {
			global.hasPendingFractureII = false;
			SpawnFracturedProjectile(global, projectile, 2);
		}

		if (global.hasPendingFractureIII && --global.pendingFractureIIIFrames <= 0) {
			global.hasPendingFractureIII = false;
			SpawnFracturedProjectile(global, projectile, 3);
		}
	}

	internal static void UpdateVisual(WeaponPrefixGlobalProjectile global, Projectile projectile)
	{
		if (!global.IsFracturedVisualProjectile) {
			return;
		}

		Lighting.AddLight(projectile.Center, 0.035f, 0.05f, 0.09f);
		Color fractureTint = WeaponPrefixVisuals.GetFracturedTint(global.FractureTier);
		int fractureTier = System.Math.Max(1, (int)global.FractureTier);
		float tierScale = 0.7f + 0.15f * fractureTier;
		Lighting.AddLight(projectile.Center, fractureTint.ToVector3() * (0.06f * tierScale));

		if (projectile.numUpdates == 0 && projectile.timeLeft % 6 == 0) {
			Dust dust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, WeaponPrefixVisuals.EchoDustType);
			dust.noGravity = true;
			dust.velocity = projectile.velocity * 0.08f;
			dust.color = fractureTint;
			dust.scale = 0.66f + 0.08f * fractureTier;
			dust.fadeIn = 0.88f + 0.02f * global.FractureTier;
		}

		if (projectile.numUpdates == 0 && projectile.timeLeft % 7 == 0) {
			Vector2 orbitOffset = Main.rand.NextVector2CircularEdge(projectile.width * 0.55f, projectile.height * 0.55f);
			Dust auraDust = Dust.NewDustPerfect(projectile.Center + orbitOffset, WeaponPrefixVisuals.EchoDustType);
			auraDust.noGravity = true;
			auraDust.velocity = projectile.velocity * 0.02f;
			auraDust.color = fractureTint;
			auraDust.scale = 0.7f + 0.08f * fractureTier;
			auraDust.fadeIn = 0.92f + 0.02f * global.FractureTier;
		}
	}

	internal static Color? GetAlpha(WeaponPrefixGlobalProjectile global)
	{
		return !global.IsFracturedVisualProjectile
			? null
			: WeaponPrefixVisuals.GetFracturedTint(global.FractureTier);
	}

	internal static bool PreDraw(WeaponPrefixGlobalProjectile global, Projectile projectile, ref Color lightColor)
	{
		if (!global.IsFracturedVisualProjectile) {
			return true;
		}

		Texture2D texture = TextureAssets.Projectile[projectile.type].Value;
		Rectangle frame = texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
		Vector2 origin = frame.Size() * 0.5f;
		Vector2 basePosition = GetProjectileDrawPosition(projectile);
		float pulse = 1f + 0.015f * System.MathF.Sin(Main.GlobalTimeWrappedHourly * 10f);
		Color fractureTint = WeaponPrefixVisuals.GetFracturedTint(global.FractureTier);
		Color preDrawTint = Color.Lerp(lightColor, fractureTint, 0.24f);
		Color underlayTint = WeaponPrefixVisuals.GetFracturedAfterimageTint(global.FractureTier) * (0.34f + 0.04f * System.Math.Max(1, (int)global.FractureTier));

		DrawSprite(projectile, texture, frame, origin, basePosition, underlayTint, projectile.rotation, projectile.scale * (pulse + 0.01f));
		DrawSprite(projectile, texture, frame, origin, basePosition - projectile.velocity * 0.1f, underlayTint * 0.6f, projectile.rotation, projectile.scale * pulse);

		lightColor = preDrawTint;
		return true;
	}

	internal static void DrawAfterimage(Projectile projectile, Texture2D texture, Rectangle frame, Vector2 origin, Vector2 drawPosition, Color color, float rotation, float scale)
	{
		DrawSprite(projectile, texture, frame, origin, drawPosition, color, rotation, scale);
	}

	internal static Vector2 GetProjectileDrawPosition(Projectile projectile)
	{
		return projectile.Center - Main.screenPosition + new Vector2(0f, projectile.gfxOffY);
	}

	private static void InitializeSourceState(WeaponPrefixGlobalProjectile global, Projectile projectile)
	{
		global.fractureSpawnPosition = projectile.Center;
		global.fractureBaseVelocity = projectile.velocity;
		global.fractureUsesLocalNPCImmunity = projectile.usesLocalNPCImmunity;
		global.fractureLocalNPCHitCooldown = projectile.localNPCHitCooldown;
		global.fractureUsesIDStaticNPCImmunity = projectile.usesIDStaticNPCImmunity;
		global.fractureIDStaticNPCHitCooldown = projectile.idStaticNPCHitCooldown;

		NormalizeHitTracking(
			projectile,
			global.fractureUsesLocalNPCImmunity,
			global.fractureLocalNPCHitCooldown,
			global.fractureUsesIDStaticNPCImmunity,
			global.fractureIDStaticNPCHitCooldown);
	}

	private static void SpawnFracturedProjectile(WeaponPrefixGlobalProjectile global, Projectile projectile, int fractureTier)
	{
		if (projectile == null || !projectile.active) {
			return;
		}

		Vector2 baseVelocity = global.fractureBaseVelocity.LengthSquared() > 0.0001f ? global.fractureBaseVelocity : projectile.velocity;
		Vector2 spawnPosition = global.fractureSpawnPosition == Vector2.Zero ? projectile.Center : global.fractureSpawnPosition;
		Vector2 fracturedVelocity = baseVelocity.RotatedByRandom(MathHelper.ToRadians(GetFractureAngleVarianceDegrees(fractureTier)));
		int fractureIndex = Projectile.NewProjectile(
			projectile.GetSource_FromThis("MozandifiersFracturedProjectile"),
			spawnPosition,
			fracturedVelocity,
			projectile.type,
			projectile.damage,
			projectile.knockBack,
			projectile.owner);

		if (fractureIndex < 0 || fractureIndex >= Main.maxProjectiles) {
			return;
		}

		Projectile fracturedProjectile = Main.projectile[fractureIndex];
		NormalizeHitTracking(
			fracturedProjectile,
			global.fractureUsesLocalNPCImmunity,
			global.fractureLocalNPCHitCooldown,
			global.fractureUsesIDStaticNPCImmunity,
			global.fractureIDStaticNPCHitCooldown);

		WeaponPrefixGlobalProjectile fracturedGlobal = fracturedProjectile.GetGlobalProjectile<WeaponPrefixGlobalProjectile>();
		fracturedGlobal.IsSpawnedFracturedProjectile = true;
		fracturedGlobal.IsFracturedDamageProjectile = true;
		fracturedGlobal.IsFracturedVisualProjectile = true;
		fracturedGlobal.FractureTier = (byte)fractureTier;
		fracturedGlobal.FracturedDamageMultiplier = GetFractureDamageMultiplier(fractureTier);
		SpawnSpawnEffect(fracturedProjectile, fractureTier);
		fracturedProjectile.netUpdate = true;
	}

	private static void NormalizeHitTracking(
		Projectile projectile,
		bool sourceUsesLocalNPCImmunity,
		int sourceLocalNPCHitCooldown,
		bool sourceUsesIDStaticNPCImmunity,
		int sourceIDStaticNPCHitCooldown)
	{
		int localNPCHitCooldown = sourceUsesLocalNPCImmunity
			? sourceLocalNPCHitCooldown
			: sourceUsesIDStaticNPCImmunity
				? sourceIDStaticNPCHitCooldown
				: -1;

		projectile.usesIDStaticNPCImmunity = false;
		projectile.idStaticNPCHitCooldown = -1;
		projectile.usesLocalNPCImmunity = true;
		projectile.localNPCHitCooldown = localNPCHitCooldown;
	}

	private static float GetFractureDamageMultiplier(int fractureTier)
	{
		return fractureTier switch {
			2 => EchoingPrefix.FractureIIDamageMultiplier,
			>= 3 => EchoingPrefix.FractureIIIDamageMultiplier,
			_ => EchoingPrefix.FractureIDamageMultiplier
		};
	}

	private static float GetFractureAngleVarianceDegrees(int fractureTier)
	{
		return fractureTier switch {
			2 => EchoingPrefix.FractureIIAngleVarianceDegrees,
			>= 3 => EchoingPrefix.FractureIIIAngleVarianceDegrees,
			_ => EchoingPrefix.FractureIAngleVarianceDegrees
		};
	}

	private static void SpawnSpawnEffect(Projectile projectile, int fractureTier)
	{
		Color fractureColor = WeaponPrefixVisuals.GetFracturedTint(fractureTier);
		int dustCount = fractureTier switch {
			2 => 7,
			>= 3 => 8,
			_ => 6
		};

		Lighting.AddLight(projectile.Center, fractureColor.ToVector3() * (0.14f + 0.03f * fractureTier));
		for (int i = 0; i < dustCount; i++) {
			Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(0.7f + fractureTier * 0.1f, 1.6f + fractureTier * 0.15f);
			Dust dust = Dust.NewDustPerfect(projectile.Center, WeaponPrefixVisuals.EchoDustType, velocity, 0, fractureColor, 0.72f + 0.08f * fractureTier);
			dust.noGravity = true;
			dust.fadeIn = 0.9f + 0.02f * fractureTier;
		}

		if (projectile.owner == Main.myPlayer && fractureTier == 1) {
			SoundEngine.PlaySound(SoundID.Item8, projectile.Center);
		}
	}

	private static void DrawSprite(Projectile projectile, Texture2D texture, Rectangle frame, Vector2 origin, Vector2 drawPosition, Color color, float rotation, float scale)
	{
		SpriteEffects spriteEffects = projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
		Main.EntitySpriteDraw(texture, drawPosition, frame, color, rotation, origin, scale, spriteEffects);
	}
}
