using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Projectiles.Weapons;

public sealed class DeadeyeBurstProjectile : ModProjectile
{
	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.Bullet}";

	public override void SetDefaults()
	{
		Projectile.width = 24;
		Projectile.height = 24;
		Projectile.friendly = true;
		Projectile.hostile = false;
		Projectile.penetrate = 1;
		Projectile.timeLeft = 2;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.hide = true;
		Projectile.DamageType = DamageClass.Ranged;
		Projectile.usesLocalNPCImmunity = true;
		Projectile.localNPCHitCooldown = -1;
	}

	public override bool ShouldUpdatePosition()
	{
		return false;
	}

	public override void AI()
	{
		int targetIndex = (int)Projectile.ai[0];
		if (targetIndex < 0 || targetIndex >= Main.maxNPCs) {
			Projectile.Kill();
			return;
		}

		NPC target = Main.npc[targetIndex];
		if (!target.active || target.dontTakeDamage) {
			Projectile.Kill();
			return;
		}

		Projectile.Center = target.Center;
		Projectile.width = System.Math.Max(18, target.width + 12);
		Projectile.height = System.Math.Max(18, target.height + 12);
	}

	public override bool? CanHitNPC(NPC target)
	{
		return target.whoAmI == (int)Projectile.ai[0];
	}

	public override bool PreDraw(ref Color lightColor)
	{
		return false;
	}

	public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
	{
		Projectile.Kill();
	}
}
