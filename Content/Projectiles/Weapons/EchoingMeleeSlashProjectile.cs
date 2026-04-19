using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Projectiles.Weapons;

public sealed class EchoingMeleeSlashProjectile : ModProjectile
{
	private const int EchoDelayFrames = 10;

	private static readonly Color SlashTint = new(190, 245, 255, 150);
	private static readonly Color SlashOutlineTint = new(235, 255, 255, 110);
	private static readonly Vector2[] SlashOffsets = [
		new Vector2(2f, 0f),
		new Vector2(-2f, 0f),
		new Vector2(0f, 2f),
		new Vector2(0f, -2f)
	];

	private readonly Vector2[] cachedCenters = new Vector2[EchoDelayFrames + 1];
	private readonly float[] cachedRotations = new float[EchoDelayFrames + 1];
	private readonly float[] cachedScales = new float[EchoDelayFrames + 1];
	private readonly int[] cachedDirections = new int[EchoDelayFrames + 1];

	private int cachedFrames;

	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.WoodenArrowFriendly}";

	public override void AI()
	{
		Player owner = Main.player[Projectile.owner];
		if (!owner.active || owner.dead) {
			Projectile.Kill();
			return;
		}

		int itemType = (int)Projectile.ai[0];
		if (itemType <= ItemID.None || !ContentSamples.ItemsByType.TryGetValue(itemType, out Item sourceItem)) {
			Projectile.Kill();
			return;
		}

		if (owner.itemAnimation <= 0 || owner.HeldItem.type != itemType) {
			Projectile.Kill();
			return;
		}

		float adjustedScale = owner.GetAdjustedItemScale(sourceItem);
		float baseReach = System.MathF.Max(sourceItem.width, sourceItem.height) * adjustedScale;
		Vector2 slashDirection = owner.itemRotation.ToRotationVector2();
		if (owner.direction == -1) {
			slashDirection *= -1f;
		}

		Vector2 currentCenter = owner.MountedCenter + slashDirection * (baseReach * 0.6f + 28f);
		float currentRotation = slashDirection.ToRotation() + (owner.direction == -1 ? MathHelper.PiOver2 : 0f);
		float currentScale = adjustedScale * 1.08f;

		RecordSwingState(currentCenter, currentRotation, currentScale, owner.direction);

		if (!HasDelayedSwingState) {
			Projectile.Center = owner.MountedCenter;
			Projectile.rotation = currentRotation;
			Projectile.scale = currentScale;
			Projectile.spriteDirection = owner.direction;
			Projectile.direction = owner.direction;
			return;
		}

		Projectile.Center = cachedCenters[EchoDelayFrames];
		Projectile.rotation = cachedRotations[EchoDelayFrames];
		Projectile.scale = cachedScales[EchoDelayFrames];
		Projectile.spriteDirection = cachedDirections[EchoDelayFrames];
		Projectile.direction = cachedDirections[EchoDelayFrames];

		Lighting.AddLight(Projectile.Center, 0.18f, 0.2f, 0.32f);

		if (Projectile.timeLeft % 2 == 0) {
			Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(20f, 20f), DustID.IceTorch);
			dust.noGravity = true;
			dust.velocity = slashDirection.RotatedByRandom(0.45f) * Main.rand.NextFloat(0.8f, 2.1f);
			dust.scale = 1.1f;
			dust.fadeIn = 1.1f;
		}
	}

	public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
	{
		if (!HasDelayedSwingState) {
			return false;
		}

		Player owner = Main.player[Projectile.owner];
		int itemType = (int)Projectile.ai[0];
		if (itemType <= ItemID.None || !ContentSamples.ItemsByType.TryGetValue(itemType, out Item sourceItem)) {
			return false;
		}

		float adjustedScale = owner.GetAdjustedItemScale(sourceItem);
		float bladeLength = System.MathF.Max(sourceItem.width, sourceItem.height) * adjustedScale + 36f;
		float bladeWidth = 38f * adjustedScale;
		Vector2 slashStart = owner.MountedCenter;
		Vector2 slashEnd = Projectile.Center;

		return Collision.CheckAABBvLineCollision(
			targetHitbox.TopLeft(),
			targetHitbox.Size(),
			slashStart,
			slashEnd,
			bladeWidth,
			ref bladeLength);
	}

	public override bool PreDraw(ref Color lightColor)
	{
		if (!HasDelayedSwingState) {
			return false;
		}

		int itemType = (int)Projectile.ai[0];
		if (itemType <= ItemID.None || !TextureAssets.Item[itemType].IsLoaded) {
			return false;
		}

		Texture2D itemTexture = TextureAssets.Item[itemType].Value;
		Vector2 drawPosition = Projectile.Center - Main.screenPosition;
		Vector2 origin = itemTexture.Size() * 0.5f;
		float pulse = 1f + 0.04f * System.MathF.Sin(Main.GlobalTimeWrappedHourly * 16f);
		SpriteEffects effects = SpriteEffects.None;

		foreach (Vector2 offset in SlashOffsets) {
			Main.EntitySpriteDraw(
				itemTexture,
				drawPosition + offset,
				null,
				SlashOutlineTint,
				Projectile.rotation,
				origin,
				Projectile.scale * (pulse + 0.04f),
				effects);
		}

		Main.EntitySpriteDraw(
			itemTexture,
			drawPosition,
			null,
			SlashTint,
			Projectile.rotation,
			origin,
			Projectile.scale * pulse,
			effects);

		Main.EntitySpriteDraw(
			itemTexture,
			drawPosition - Projectile.velocity * 0.15f,
			null,
			SlashTint * 0.55f,
			Projectile.rotation,
			origin,
			Projectile.scale * (pulse + 0.08f),
			effects);

		return false;
	}

	private bool HasDelayedSwingState => cachedFrames > EchoDelayFrames;

	private void RecordSwingState(Vector2 center, float rotation, float scale, int direction)
	{
		for (int i = EchoDelayFrames; i > 0; i--) {
			cachedCenters[i] = cachedCenters[i - 1];
			cachedRotations[i] = cachedRotations[i - 1];
			cachedScales[i] = cachedScales[i - 1];
			cachedDirections[i] = cachedDirections[i - 1];
		}

		cachedCenters[0] = center;
		cachedRotations[0] = rotation;
		cachedScales[0] = scale;
		cachedDirections[0] = direction;
		cachedFrames++;
	}
}
