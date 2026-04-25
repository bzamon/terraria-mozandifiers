using Mozandifiers.Content.Prefixes.Weapons;
using Terraria;

namespace Mozandifiers.Content.Prefixes.Common;

internal static class CatalyticRuntime
{
	internal static void PrepareHit(WeaponPrefixGlobalProjectile global, Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
	{
		bool shiftedCatalytic = global.ShiftedSimulationId == ShiftingSimulationId.Catalytic;
		if ((!global.IsCatalyticProjectile && !shiftedCatalytic) || !IsEligibleProjectile(projectile)) {
			global.pendingCatalyticConsume = false;
			return;
		}

		WeaponPrefixGlobalNPC targetGlobal = target.GetGlobalNPC<WeaponPrefixGlobalNPC>();
		if (!targetGlobal.ShouldConsumeCatalyticMark(projectile.owner, projectile, shiftedCatalytic)) {
			global.pendingCatalyticConsume = false;
			return;
		}

		modifiers.SourceDamage *= 1f + (shiftedCatalytic
			? ShiftingSimulationEffectScaling.GetShiftedCatalyticConsumeDamageBonus()
			: CatalyticPrefix.ConsumeDamageBonus);
		global.pendingCatalyticConsume = true;
	}

	internal static void HandleHit(WeaponPrefixGlobalProjectile global, Projectile projectile, NPC target, int damageDone)
	{
		bool shiftedCatalytic = global.ShiftedSimulationId == ShiftingSimulationId.Catalytic;
		if (damageDone > 0 && (global.IsCatalyticProjectile || shiftedCatalytic)) {
			HandleHit(
				global,
				projectile,
				target,
				shiftedCatalytic,
			shiftedCatalytic ? ShiftingSimulationEffectScaling.GetShiftedCatalyticMarkDurationTicks() : CatalyticPrefix.MarkDurationTicks,
			shiftedCatalytic ? ShiftingSimulationEffectScaling.GetShiftedCatalyticArmDelayTicks() : CatalyticPrefix.MarkArmDelayTicks);
			return;
		}

		global.pendingCatalyticConsume = false;
	}

	private static void HandleHit(WeaponPrefixGlobalProjectile global, Projectile projectile, NPC target, bool shifted, int durationTicks, int armDelayTicks)
	{
		if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers || target == null || !target.active) {
			global.pendingCatalyticConsume = false;
			return;
		}

		WeaponPrefixGlobalNPC targetGlobal = target.GetGlobalNPC<WeaponPrefixGlobalNPC>();
		if (global.pendingCatalyticConsume
			&& targetGlobal.ShouldConsumeCatalyticMark(projectile.owner, projectile, shifted)) {
			targetGlobal.ConsumeCatalyticMark(projectile.owner, shifted);
			targetGlobal.SpawnCatalyticConsumeEffect(target, shifted);
			global.pendingCatalyticConsume = false;
			return;
		}

		targetGlobal.ApplyCatalyticMark(projectile.owner, projectile, shifted, durationTicks, armDelayTicks);
		global.pendingCatalyticConsume = true;
	}

	private static bool IsEligibleProjectile(Projectile projectile)
	{
		return projectile != null && projectile.active;
	}
}
