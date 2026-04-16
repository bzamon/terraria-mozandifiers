using System.IO;
using Microsoft.Xna.Framework;
using Mozandifiers.Content.Prefixes.Common;
using Mozandifiers.Content.Prefixes.Weapons;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Mozandifiers.Content.Projectiles.Weapons;

public sealed class EchoingMeleeSpearReplayProjectile : ModProjectile
{
	private const int ReplayDelayFrames = 5;

	private int sourceProjectileType = ProjectileID.None;
	private Vector2 initialVelocity;
	private bool spawnedClone;

	public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.WoodenArrowFriendly}";

	public override void SetDefaults()
	{
		Projectile.width = 2;
		Projectile.height = 2;
		Projectile.aiStyle = -1;
		Projectile.friendly = false;
		Projectile.tileCollide = false;
		Projectile.ignoreWater = true;
		Projectile.timeLeft = ReplayDelayFrames + 2;
		Projectile.hide = true;
	}

	public override bool? CanDamage()
	{
		return false;
	}

	public override void AI()
	{
		Player owner = Main.player[Projectile.owner];
		if (!owner.active || owner.dead || sourceProjectileType <= ProjectileID.None) {
			Projectile.Kill();
			return;
		}

		Projectile.Center = owner.MountedCenter;

		if (spawnedClone || Projectile.timeLeft > 2) {
			return;
		}

		spawnedClone = true;

		int spearIndex = Projectile.NewProjectile(
			Projectile.GetSource_FromThis("MozandifiersEchoSpearReplay"),
			owner.MountedCenter,
			initialVelocity,
			sourceProjectileType,
			Projectile.damage,
			Projectile.knockBack,
			Projectile.owner);

		if (spearIndex >= 0 && spearIndex < Main.maxProjectiles) {
			WeaponPrefixGlobalItem.ConfigureIndependentHitTracking(ContentSamples.ProjectilesByType[sourceProjectileType], Main.projectile[spearIndex]);
			Main.projectile[spearIndex].netUpdate = true;
		}

		Projectile.Kill();
	}

	public override void SendExtraAI(BinaryWriter writer)
	{
		writer.Write(sourceProjectileType);
		writer.WriteVector2(initialVelocity);
		writer.Write(spawnedClone);
	}

	public override void ReceiveExtraAI(BinaryReader reader)
	{
		sourceProjectileType = reader.ReadInt32();
		initialVelocity = reader.ReadVector2();
		spawnedClone = reader.ReadBoolean();
	}

	public void Initialize(int projectileType, Vector2 velocity)
	{
		sourceProjectileType = projectileType;
		initialVelocity = velocity;
	}
}
