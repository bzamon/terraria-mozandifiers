using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Mozandifiers.Content.Prefixes.Weapons;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace Mozandifiers.Content.Prefixes.Common;

public sealed class WeaponPrefixGlobalProjectile : GlobalProjectile
{
	public override bool InstancePerEntity => true;

	public bool IsFracturedDamageProjectile { get; set; }
	public bool IsFracturedVisualProjectile { get; set; }
	public bool IsSpawnedFracturedProjectile { get; set; }
	public byte FractureTier { get; set; }
	public bool IsVampiricProjectile { get; set; }
	public int VampiricManaRestoreAmount { get; set; }
	public bool IsRadiantProjectile { get; set; }
	public bool IsBreachingProjectile { get; set; }
	public bool IsDesperateProjectile { get; set; }
	public bool IsTemporalProjectile { get; set; }
	public bool IsSkirmishingProjectile { get; set; }
	public bool IsSkirmishFollowUpProjectile { get; set; }
	public bool IsAttunedProjectile { get; set; }
	public bool IsEmpoweredAttunedProjectile { get; set; }
	public bool IsStormforgedProjectile { get; set; }
	public bool IsCatalyticProjectile { get; set; }
	public bool IsDeadeyeProjectile { get; set; }
	public bool IsSpinboundProjectile { get; set; }
	public float BreachImpactScale { get; set; }
	public float SpinboundReferenceSpeed { get; set; }
	public int GoldenSpinTier { get; set; }
	public bool HasConsumedSpinboundHitBonus { get; set; }
	public float FracturedDamageMultiplier { get; set; }
	internal ShiftingSimulationId ShiftedSimulationId { get; set; }
	internal float ShiftedStrengthMultiplier { get; set; }
	internal int ShiftedVampiricManaRestoreAmount { get; set; }
	internal bool IsShiftedSpinboundProjectile { get; set; }
	internal float ShiftedSpinboundReferenceSpeed { get; set; }
	internal int ShiftedGoldenSpinTier { get; set; }
	internal bool HasConsumedShiftedSpinboundHitBonus { get; set; }
	internal bool IsShiftedSkirmishFollowUpProjectile { get; set; }
	internal bool IsEmpoweredShiftedAttunedProjectile { get; set; }
	internal bool IsShiftedDeadeyeProjectile { get; set; }
	internal bool HasPendingDesperateSurgeFeedback { get; set; }

	internal bool pendingCatalyticConsume;
	internal VampiricPreyState pendingVampiricPreyState;
	internal int pendingFractureIIFrames;
	internal int pendingFractureIIIFrames;
	internal bool hasPendingFractureII;
	internal bool hasPendingFractureIII;
	internal Vector2 fractureSpawnPosition;
	internal Vector2 fractureBaseVelocity;
	internal bool fractureUsesLocalNPCImmunity;
	internal int fractureLocalNPCHitCooldown;
	internal bool fractureUsesIDStaticNPCImmunity;
	internal int fractureIDStaticNPCHitCooldown;

	public override void OnSpawn(Projectile projectile, IEntitySource source)
	{
		WeaponPrefixProjectileSpawnDispatch.OnSpawn(this, projectile, source);
	}

	public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
	{
		WeaponPrefixProjectileHitDispatch.OnHitNPC(this, projectile, target, hit, damageDone);
	}

	public override void AI(Projectile projectile)
	{
		WeaponPrefixProjectileVisualDispatch.AI(this, projectile);
	}

	public override Color? GetAlpha(Projectile projectile, Color lightColor)
	{
		return WeaponPrefixProjectileVisualDispatch.GetAlpha(this);
	}

	public override bool PreDraw(Projectile projectile, ref Color lightColor)
	{
		return WeaponPrefixProjectileVisualDispatch.PreDraw(this, projectile, ref lightColor);
	}

	public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
	{
		WeaponPrefixProjectileHitDispatch.ModifyHitNPC(this, projectile, target, ref modifiers);
	}

	public override void ModifyHitPlayer(Projectile projectile, Player target, ref Player.HurtModifiers modifiers)
	{
		WeaponPrefixProjectileHitDispatch.ModifyHitPlayer(this, ref modifiers);
	}

	public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
	{
		WeaponPrefixProjectileSyncDispatch.SendExtraAI(this, bitWriter, binaryWriter);
	}

	public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
	{
		WeaponPrefixProjectileSyncDispatch.ReceiveExtraAI(this, bitReader, binaryReader);
	}
}
