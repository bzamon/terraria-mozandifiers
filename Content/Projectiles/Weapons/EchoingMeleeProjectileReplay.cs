using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mozandifiers.Content.Prefixes.Common;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Projectiles.Weapons;

internal enum EchoingMeleeReplayStyle
{
	None = 0,
	Thrust = 1,
	Boomerang = 2
}

public sealed class EchoingMeleeProjectileReplay : ModProjectile
{
	private const int EchoDelayFrames = 5;

	private static readonly Color ReplayTint = new(80, 105, 130, 72);
	private static readonly Color ReplayOutlineTint = new(105, 125, 150, 46);
	private static readonly Vector2[] ReplayOutlineOffsets = [
		new Vector2(0f, 0f),
		new Vector2(-0f, 0f),
		new Vector2(0f, 0f),
		new Vector2(0f, -0f)
	];

	private readonly Queue<EchoFrame> frameHistory = new(EchoDelayFrames + 1);

	private bool hasCurrentFrame;
	private bool hasStartedPlayback;
	private bool hasStartedDraining;
	private int sourceProjectileType = ProjectileID.None;
	private EchoFrame currentFrame;

	private readonly struct EchoFrame
	{
		public Vector2 Center { get; init; }
		public Vector2 OwnerCenter { get; init; }
		public Vector2 Velocity { get; init; }
		public float Rotation { get; init; }
		public float Scale { get; init; }
		public int Direction { get; init; }
		public int SpriteDirection { get; init; }
		public int Width { get; init; }
		public int Height { get; init; }
		public int Frame { get; init; }
	}

	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.WoodenArrowFriendly}";

	private EchoingMeleeReplayStyle ReplayStyle => (EchoingMeleeReplayStyle)(int)Projectile.ai[1];

    //public override void SetDefaults()
    //{
    //Projectile.width = 24;
    //Projectile.height = 24;
    //Projectile.aiStyle = -1;
    //Projectile.friendly = true;
    //Projectile.penetrate = -1;
    //Projectile.tileCollide = false;
    //Projectile.ignoreWater = true;
    //Projectile.usesLocalNPCImmunity = true;
    //Projectile.localNPCHitCooldown = -1;
    //Projectile.DamageType = DamageClass.Melee;
    //Projectile.timeLeft = 600;
    //}

    public override void OnSpawn(IEntitySource source)
    {
        Main.NewText($"EchoingMeleeProjectileReplay spawned - ai0: {Projectile.ai[0]} - ai1: {Projectile.ai[1]}"); // Debug message, can be removed later
    }
	
	public override bool? CanDamage()
	{
		return hasCurrentFrame;
	}

	public override void AI()
	{
		if (Projectile.owner < 0 || Projectile.owner >= Main.maxPlayers) {
			Projectile.Kill();
			return;
		}

		Player owner = Main.player[Projectile.owner];
		if (!owner.active || owner.dead) {
			Projectile.Kill();
			return;
		}

		Projectile.timeLeft = 2;

		bool sourceActive = TryRecordSourceFrame(owner);
		if (!TryAdvanceReplay(sourceActive)) {
			hasCurrentFrame = false;
			return;
		}

		ApplyCurrentFrame();
		Lighting.AddLight(Projectile.Center, 0.02f, 0.025f, 0.05f);

		if (Main.GameUpdateCount % 4ul == 0) {
			Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f), DustID.Shadowflame);
			dust.noGravity = true;
			dust.velocity = currentFrame.Velocity * 0.05f;
			dust.scale = 0.6f;
			dust.fadeIn = 0.7f;
		}
	}

	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{
		if (!hasCurrentFrame) {
			return false;
		}

		return ReplayStyle switch {
			EchoingMeleeReplayStyle.Thrust => CollideAsThrust(targetHitbox),
			EchoingMeleeReplayStyle.Boomerang => CollideAsBoomerang(targetHitbox),
			_ => false
		};
	}

	public override bool PreDraw(ref Color lightColor)
	{
		if (!hasCurrentFrame || sourceProjectileType <= ProjectileID.None) {
			return false;
		}

		Texture2D texture = TextureAssets.Projectile[sourceProjectileType].Value;
		int frameCount = Main.projFrames[sourceProjectileType];
		if (frameCount < 1) {
			frameCount = 1;
		}

		Rectangle frame = texture.Frame(1, frameCount, 0, currentFrame.Frame % frameCount);
		Vector2 origin = frame.Size() * 0.5f;
		Vector2 drawPosition = Projectile.Center - Main.screenPosition;
		SpriteEffects effects = currentFrame.SpriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

		foreach (Vector2 offset in ReplayOutlineOffsets) {
			Main.EntitySpriteDraw(
				texture,
				drawPosition + offset,
				frame,
				ReplayOutlineTint,
				Projectile.rotation,
				origin,
				Projectile.scale,
				effects);
		}

		Main.EntitySpriteDraw(
			texture,
			drawPosition - currentFrame.Velocity * 0.12f,
			frame,
			ReplayTint * 0.45f,
			Projectile.rotation,
			origin,
			Projectile.scale,
			effects);

		Main.EntitySpriteDraw(
			texture,
			drawPosition,
			frame,
			ReplayTint,
			Projectile.rotation,
			origin,
			Projectile.scale,
			effects);

		return false;
	}

	private bool TryRecordSourceFrame(Player owner)
	{
		int sourceProjectileIndex = (int)Projectile.ai[0];
		if (sourceProjectileIndex < 0 || sourceProjectileIndex >= Main.maxProjectiles) {
			return false;
		}

		Projectile sourceProjectile = Main.projectile[sourceProjectileIndex];
		if (!sourceProjectile.active || sourceProjectile.owner != Projectile.owner) {
			return false;
		}

		if (!MatchesReplayStyle(sourceProjectile)) {
			return false;
		}

		sourceProjectileType = sourceProjectile.type;

		if (frameHistory.Count == EchoDelayFrames + 1) {
			frameHistory.Dequeue();
		}

		frameHistory.Enqueue(new EchoFrame {
			Center = sourceProjectile.Center,
			OwnerCenter = owner.MountedCenter,
			Velocity = sourceProjectile.velocity,
			Rotation = sourceProjectile.rotation,
			Scale = sourceProjectile.scale <= 0f ? 1f : sourceProjectile.scale,
			Direction = sourceProjectile.direction == 0 ? owner.direction : sourceProjectile.direction,
			SpriteDirection = sourceProjectile.spriteDirection == 0 ? owner.direction : sourceProjectile.spriteDirection,
			Width = sourceProjectile.width,
			Height = sourceProjectile.height,
			Frame = sourceProjectile.frame
		});

		return true;
	}

	private bool TryAdvanceReplay(bool sourceActive)
	{
		if (sourceActive) {
			if (frameHistory.Count <= EchoDelayFrames) {
				return false;
			}

			currentFrame = frameHistory.Peek();
			hasCurrentFrame = true;
			hasStartedPlayback = true;
			hasStartedDraining = false;
			return true;
		}

		if (!hasStartedPlayback) {
			Projectile.Kill();
			return false;
		}

		if (!hasStartedDraining) {
			if (frameHistory.Count > 0) {
				frameHistory.Dequeue();
			}

			hasStartedDraining = true;
		}

		if (frameHistory.Count == 0) {
			Projectile.Kill();
			return false;
		}

		currentFrame = frameHistory.Peek();
		hasCurrentFrame = true;
		frameHistory.Dequeue();
		return true;
	}

	private void ApplyCurrentFrame()
	{
		Vector2 center = currentFrame.Center;
		Projectile.width = currentFrame.Width;
		Projectile.height = currentFrame.Height;
		Projectile.Center = center;
		Projectile.rotation = currentFrame.Rotation;
		Projectile.scale = currentFrame.Scale;
		Projectile.direction = currentFrame.Direction;
		Projectile.spriteDirection = currentFrame.SpriteDirection;
	}

	private bool MatchesReplayStyle(Projectile sourceProjectile)
	{
		return ReplayStyle switch {
			EchoingMeleeReplayStyle.Thrust => WeaponPrefix.GetProjectileMeleeEchoMode(sourceProjectile) == EchoingWeaponMode.MeleeThrust,
			EchoingMeleeReplayStyle.Boomerang => WeaponPrefix.GetProjectileMeleeEchoMode(sourceProjectile) == EchoingWeaponMode.MeleeBoomerang,
			_ => false
		};
	}

	private bool CollideAsThrust(Rectangle targetHitbox)
	{
		float collisionPoint = 0f;
		float lineWidth = MathHelper.Clamp(System.MathF.Min(currentFrame.Width, currentFrame.Height) * currentFrame.Scale * 0.8f, 10f, 28f);

		return Collision.CheckAABBvLineCollision(
			targetHitbox.TopLeft(),
			targetHitbox.Size(),
			currentFrame.OwnerCenter,
			currentFrame.Center,
			lineWidth,
			ref collisionPoint);
	}

	private bool CollideAsBoomerang(Rectangle targetHitbox)
	{
		Rectangle replayHitbox = Utils.CenteredRectangle(
			Projectile.Center,
			new Vector2(Projectile.width, Projectile.height) * Projectile.scale);

		return replayHitbox.Intersects(targetHitbox);
	}
}
