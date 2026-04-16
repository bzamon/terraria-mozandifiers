using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mozandifiers.Content.Prefixes.Weapons;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Mozandifiers.Content.Prefixes.Common;

public sealed class WeaponPrefixGlobalProjectile : GlobalProjectile
{
	private static readonly Color EchoTint = new(70, 95, 120, 52);
	private static readonly Color EchoAfterimageTint = new(90, 105, 130, 28);
	private static readonly Color EchoOverlayTint = new(105, 120, 145, 42);
	private static readonly Vector2[] EchoOutlineOffsets = [
		new Vector2(20f, 0f),
		new Vector2(-20f, 0f),
		new Vector2(0f, 20f),
		new Vector2(0f, -20f)
	];

	public override bool InstancePerEntity => true;

	public bool IsEchoDamageProjectile { get; set; }
	public bool IsEchoVisualProjectile { get; set; }



    public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
    {

        if (!IsEchoDamageProjectile) {
			return;
        }

        Main.NewText($"OnHitNPC - IsEchoDamageProjectile: {IsEchoDamageProjectile} - Projectile Type: {projectile.type} - Target: {target.whoAmI} - Damage Done: {damageDone}"); // Debug message, can be removed later

    }


    public override void AI(Projectile projectile)
	{
		if (!IsEchoVisualProjectile) {
			return;
		}

		Lighting.AddLight(projectile.Center, 0.015f, 0.015f, 0.04f);

		if (projectile.numUpdates == 0 && projectile.timeLeft % 6 == 0) {
			Dust dust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, DustID.Shadowflame);
			dust.noGravity = true;
			dust.velocity = projectile.velocity * 0.05f;
			dust.scale = 0.55f;
			dust.fadeIn = 0.65f;
		}

		if (projectile.numUpdates == 0 && projectile.timeLeft % 7 == 0) {
			Vector2 orbitOffset = Main.rand.NextVector2CircularEdge(projectile.width * 0.55f, projectile.height * 0.55f);
			Dust auraDust = Dust.NewDustPerfect(projectile.Center + orbitOffset, DustID.Shadowflame);
			auraDust.noGravity = true;
			auraDust.velocity = projectile.velocity * 0.02f;
			auraDust.scale = 0.6f;
			auraDust.fadeIn = 0.7f;
		}
	}

	public override Color? GetAlpha(Projectile projectile, Color lightColor)
	{
		if (!IsEchoVisualProjectile) {
			return null;
		}

		return EchoTint;
	}

	public override bool PreDraw(Projectile projectile, ref Color lightColor)
	{
		if (!IsEchoVisualProjectile) {
			return true;
		}

		Texture2D texture = TextureAssets.Projectile[projectile.type].Value;
		Rectangle frame = texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
		Vector2 origin = frame.Size() * 0.5f;
		Vector2 basePosition = GetProjectileDrawPosition(projectile);
		float pulse = 1f + 0.015f * System.MathF.Sin(Main.GlobalTimeWrappedHourly * 10f);
		Color preDrawTint = Color.Lerp(lightColor, EchoTint, 0.2f);
		Color underlayTint = EchoAfterimageTint * 0.4f;

		DrawEchoSprite(projectile, texture, frame, origin, basePosition, underlayTint, projectile.rotation, projectile.scale * (pulse + 0.01f));
		DrawEchoSprite(projectile, texture, frame, origin, basePosition - projectile.velocity * 0.1f, underlayTint * 0.6f, projectile.rotation, projectile.scale * pulse);

		lightColor = preDrawTint;
		return true;
	}

	public override void PostDraw(Projectile projectile, Color lightColor)
	{
		if (!IsEchoVisualProjectile) {
			return;
		}

		Texture2D texture = TextureAssets.Projectile[projectile.type].Value;
		Rectangle frame = texture.Frame(1, Main.projFrames[projectile.type], 0, projectile.frame);
		Vector2 origin = frame.Size() * 0.5f;
		Vector2 basePosition = GetProjectileDrawPosition(projectile);
		float pulse = 1f + 0.02f * System.MathF.Sin(Main.GlobalTimeWrappedHourly * 12f);

		//DrawEchoTrail(projectile, texture, frame, origin);

		//foreach (Vector2 offset in EchoOutlineOffsets) {
		//	DrawEchoSprite(projectile, texture, frame, origin, basePosition + offset, EchoOverlayTint, projectile.rotation, projectile.scale * pulse);
		//}

		//DrawEchoSprite(projectile, texture, frame, origin, basePosition, EchoOverlayTint * 0.65f, projectile.rotation, projectile.scale * (pulse - 0.01f));
	}

	public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
	{
		if (IsEchoDamageProjectile) {
			modifiers.SourceDamage *= EchoingPrefix.EchoDamageMultiplier;
		}
	}

	public override void ModifyHitPlayer(Projectile projectile, Player target, ref Player.HurtModifiers modifiers)
	{
		if (IsEchoDamageProjectile) {
			modifiers.SourceDamage *= EchoingPrefix.EchoDamageMultiplier;
		}
	}

	public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
	{
		bitWriter.WriteBit(IsEchoDamageProjectile);
		bitWriter.WriteBit(IsEchoVisualProjectile);
	}

	public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
	{
		IsEchoDamageProjectile = bitReader.ReadBit();
		IsEchoVisualProjectile = bitReader.ReadBit();
	}

	private static void DrawEchoTrail(Projectile projectile, Texture2D texture, Rectangle frame, Vector2 origin)
	{
		for (int i = projectile.oldPos.Length - 1; i >= 1; i--) {
			Vector2 oldPosition = projectile.oldPos[i];
			if (oldPosition == Vector2.Zero) {
				continue;
			}

			float progress = 1f - i / (float)projectile.oldPos.Length;
			Color trailColor = EchoAfterimageTint * (0.1f + progress * 0.14f);
			Vector2 drawPosition = oldPosition + projectile.Size * 0.5f - Main.screenPosition + new Vector2(0f, projectile.gfxOffY);
			float rotation = projectile.oldRot[i] == 0f ? projectile.rotation : projectile.oldRot[i];
			DrawEchoSprite(projectile, texture, frame, origin, drawPosition, trailColor, rotation, projectile.scale * (0.94f + progress * 0.03f));
		}
	}

	private static Vector2 GetProjectileDrawPosition(Projectile projectile)
	{
		return projectile.Center - Main.screenPosition + new Vector2(0f, projectile.gfxOffY);
	}

	private static void DrawEchoSprite(Projectile projectile, Texture2D texture, Rectangle frame, Vector2 origin, Vector2 drawPosition, Color color, float rotation, float scale)
	{
		SpriteEffects spriteEffects = projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
		Main.EntitySpriteDraw(texture, drawPosition, frame, color, rotation, origin, scale, spriteEffects);
	}
}
