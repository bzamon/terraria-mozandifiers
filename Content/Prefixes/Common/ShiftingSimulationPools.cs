using Mozandifiers.Content.Prefixes.Weapons;
using Terraria;
using Terraria.Utilities;

namespace Mozandifiers.Content.Prefixes.Common;

internal static class ShiftingSimulationPools
{
	private static readonly ShiftingSimulationId[] MeleePool = [
		ShiftingSimulationId.Breaching,
		ShiftingSimulationId.Desperate,
		ShiftingSimulationId.Radiant,
		ShiftingSimulationId.Vampiric,
		ShiftingSimulationId.Temporal,
		ShiftingSimulationId.Stormforged
	];

	private static readonly ShiftingSimulationId[] RangedPool = [
		ShiftingSimulationId.Breaching,
		ShiftingSimulationId.Skirmishing,
		ShiftingSimulationId.Deadeye,
		ShiftingSimulationId.Desperate,
		ShiftingSimulationId.Radiant,
		ShiftingSimulationId.Vampiric,
		ShiftingSimulationId.Temporal,
		ShiftingSimulationId.Fractured,
		ShiftingSimulationId.Stormforged,
		ShiftingSimulationId.Spinbound
	];

	private static readonly ShiftingSimulationId[] MagicPool = [
		ShiftingSimulationId.Attuned,
		ShiftingSimulationId.Desperate,
		ShiftingSimulationId.Radiant,
		ShiftingSimulationId.Vampiric,
		ShiftingSimulationId.Temporal,
		ShiftingSimulationId.Fractured,
		ShiftingSimulationId.Stormforged,
		ShiftingSimulationId.Catalytic
	];

	internal static bool TryGetWeaponMode(Item item, out ShiftingWeaponMode mode)
	{
		if (WeaponPrefix.IsSwingingMeleeWeapon(item)) {
			mode = ShiftingWeaponMode.MeleeSwing;
			return true;
		}

		if (WeaponPrefix.IsStandardProjectileCombatWeapon(item)) {
			if (WeaponPrefix.IsRangedWeapon(item)) {
				mode = ShiftingWeaponMode.RangedProjectile;
				return true;
			}

			if (WeaponPrefix.IsMagicWeapon(item)) {
				mode = ShiftingWeaponMode.MagicProjectile;
				return true;
			}
		}

		mode = ShiftingWeaponMode.None;
		return false;
	}

	internal static ShiftingSimulationId SelectNextSimulation(
		ShiftingWeaponMode mode,
		ShiftingSimulationId previousSimulationId,
		ulong currentTick,
		int playerId,
		int itemType)
	{
		ShiftingSimulationId[] pool = GetPool(mode);
		if (pool.Length == 0) {
			return ShiftingSimulationId.None;
		}

		ShiftingSimulationId[] selectionPool = pool;
		if (previousSimulationId != ShiftingSimulationId.None && pool.Length > 1) {
			selectionPool = System.Array.FindAll(pool, simulationId => simulationId != previousSimulationId);
		}

		int seed = System.HashCode.Combine(
			(int)(currentTick / (ulong)ShiftingPrefix.ShiftDurationTicks),
			playerId,
			itemType,
			(int)mode,
			(int)previousSimulationId);
		UnifiedRandom random = new(seed);
		return selectionPool[random.Next(selectionPool.Length)];
	}

	private static ShiftingSimulationId[] GetPool(ShiftingWeaponMode mode)
	{
		return mode switch {
			ShiftingWeaponMode.MeleeSwing => MeleePool,
			ShiftingWeaponMode.RangedProjectile => RangedPool,
			ShiftingWeaponMode.MagicProjectile => MagicPool,
			_ => []
		};
	}
}
