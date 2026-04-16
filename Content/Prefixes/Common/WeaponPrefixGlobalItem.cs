using Microsoft.Xna.Framework;
using Mozandifiers.Common.Config;
using Mozandifiers.Content.Prefixes.Weapons;
using Mozandifiers.Content.Projectiles.Weapons;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Common;

public sealed class WeaponPrefixGlobalItem : GlobalItem
{
	public override bool Shoot(
		Item item,
		Player player,
		EntitySource_ItemUse_WithAmmo source,
		Vector2 position,
		Vector2 velocity,
		int type,
		int damage,
		float knockback)
	{
        if (IsBoomerang(item))
        {
            //Main.NewText($"This weapon is a boomerang! - {item.Name} ? {IsBoomerang(item)}"); // Debug message, can be removed later
        }
        
		if (!HasEchoingPrefix(item) || type <= ProjectileID.None) {
			return true;
		}

		if (!CanShoot(item, player))
		{
			return true;
		}

        //Main.NewText($"Passed echoing / can shoot check for Shoot! - {item.Name} ? {HasEchoingPrefix(item)} ? {CanShoot(item, player)}"); // Debug message, can be removed later


        EchoingWeaponMode mode = WeaponPrefix.GetEchoingWeaponMode(item);
        //if (mode is not EchoingWeaponMode.RangedProjectile and not EchoingWeaponMode.MagicProjectile)
        //{
        //	return true;
        //} 
        var random = new Random();
        float min = 0.0f;
        float max = 1.0f;

        // Range: [5.0, 10.0)
        float rnd = random.NextSingle() * (max - min) + min;

        if (rnd > PrefixTuningConfig.Instance.EchoChance) {
            //Main.NewText($"Random check : {rnd} ? {PrefixTuningConfig.Instance.EchoChance}"); // Debug message, can be removed later

            return true;
		}


       // Main.NewText($"Passed all checks for Shoot! - {item.Name} ? {CanShoot(item, player)}"); // Debug message, can be removed later

		Vector2 echoedVelocity = velocity.RotatedByRandom(MathHelper.ToRadians(10));
		Vector2 travelDirection = echoedVelocity.SafeNormalize(Vector2.UnitX);
		Vector2 echoedPosition = position - travelDirection * 10f;
		int projectileIndex = Projectile.NewProjectile(source, echoedPosition, echoedVelocity, type, damage, knockback, player.whoAmI);

		if (projectileIndex >= 0 && projectileIndex < Main.maxProjectiles) {
			Projectile echoedProjectile = Main.projectile[projectileIndex];
			ConfigureIndependentHitTracking(ContentSamples.ProjectilesByType[type], echoedProjectile);

			WeaponPrefixGlobalProjectile echoProjectile = Main.projectile[projectileIndex].GetGlobalProjectile<WeaponPrefixGlobalProjectile>();
			echoProjectile.IsEchoDamageProjectile = true;
			echoProjectile.IsEchoVisualProjectile = true;//mode == EchoingWeaponMode.RangedProjectile;
			Main.projectile[projectileIndex].netUpdate = true;
		}

		return true;
	}
    public static bool IsBoomerang(Item item)
    {
        if (item == null || item.IsAir)
            return false;

        if (item.shoot <= ProjectileID.None)
            return false;

        Projectile sampleProj = ContentSamples.ProjectilesByType[item.shoot];
        return sampleProj.aiStyle == ProjAIStyleID.Boomerang;
    }
    
	public override void UseAnimation(Item item, Player player)
	{
        
		if (!HasEchoingPrefix(item) || WeaponPrefix.GetEchoingWeaponMode(item) != EchoingWeaponMode.MeleeSwing) {
			return;
		}

		if (player.whoAmI != Main.myPlayer || Main.rand.NextFloat() >= PrefixTuningConfig.Instance.EchoChance) {
			return;
		}

        //Main.NewText($"Passed all checks for UseAnimation! - {item.Name} ? {CanShoot(item, player)}"); // Debug message, can be removed later


        int echoedDamage = (int)System.MathF.Max(
			1f,
			System.MathF.Round(player.GetTotalDamage(item.DamageType).ApplyTo(item.damage) * EchoingPrefix.EchoDamageMultiplier));

		Projectile.NewProjectile(
			player.GetSource_ItemUse(item),
			player.MountedCenter,
			Vector2.Zero,
			ModContent.ProjectileType<EchoingMeleeSlashProjectile>(),
			echoedDamage,
			item.knockBack,
			player.whoAmI,
			item.type);
	}

	private static bool HasEchoingPrefix(Item item)
	{
		return item.prefix == ModContent.PrefixType<EchoingPrefix>();
	}

	internal static void ConfigureIndependentHitTracking(Projectile sourceProjectile, Projectile echoProjectile)
	{
		//Main.NewText($"sourceProjectile - usesIDStaticNPCImmunity? {sourceProjectile.usesIDStaticNPCImmunity} - usesLocalNPCImmunity: {sourceProjectile.usesLocalNPCImmunity} - localNPCHitCooldown: {sourceProjectile.localNPCHitCooldown}"); // Debug message, can be removed later


		echoProjectile.usesIDStaticNPCImmunity = false;
		echoProjectile.idStaticNPCHitCooldown = -1;
		echoProjectile.usesLocalNPCImmunity = true;

		int cooldown = sourceProjectile.usesLocalNPCImmunity
			? sourceProjectile.localNPCHitCooldown
			: sourceProjectile.idStaticNPCHitCooldown;

		echoProjectile.localNPCHitCooldown = cooldown == -2 ? 1 : cooldown;
	}
}
