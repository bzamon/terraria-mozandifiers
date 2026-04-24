using Mozandifiers.Common.Config;
using Mozandifiers.Content.Prefixes.Weapons;
using Terraria;
using Terraria.Localization;
using Terraria.Utilities;

namespace Mozandifiers.Content.Prefixes.Common;

internal enum ShiftingWeaponMode : byte
{
	None,
	MeleeSwing,
	RangedProjectile,
	MagicProjectile
}

internal enum ShiftingSimulationId : byte
{
	None,
	Breaching,
	Attuned,
	Skirmishing,
	Desperate,
	Radiant,
	Sanguine,
	Deadeye,
	Temporal,
	Echoing,
	Stormforged,
	Catalytic,
	Spinbound
}

internal static class ShiftingSimulationCatalog
{
	private static readonly ShiftingSimulationId[] MeleePool = [
		ShiftingSimulationId.Breaching,
		ShiftingSimulationId.Desperate,
		ShiftingSimulationId.Radiant,
		ShiftingSimulationId.Sanguine,
		ShiftingSimulationId.Temporal,
		ShiftingSimulationId.Stormforged
	];

	private static readonly ShiftingSimulationId[] RangedPool = [
		ShiftingSimulationId.Breaching,
		ShiftingSimulationId.Skirmishing,
		ShiftingSimulationId.Deadeye,
		ShiftingSimulationId.Desperate,
		ShiftingSimulationId.Radiant,
		ShiftingSimulationId.Sanguine,
		ShiftingSimulationId.Temporal,
		ShiftingSimulationId.Echoing,
		ShiftingSimulationId.Stormforged,
		ShiftingSimulationId.Spinbound
	];

	private static readonly ShiftingSimulationId[] MagicPool = [
		ShiftingSimulationId.Attuned,
		ShiftingSimulationId.Desperate,
		ShiftingSimulationId.Radiant,
		ShiftingSimulationId.Sanguine,
		ShiftingSimulationId.Temporal,
		ShiftingSimulationId.Echoing,
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

	internal static string GetDisplayName(string modName, ShiftingSimulationId simulationId)
	{
		string prefixKey = simulationId switch {
			ShiftingSimulationId.Breaching => "BreachingPrefix",
			ShiftingSimulationId.Attuned => "AttunedPrefix",
			ShiftingSimulationId.Skirmishing => "SkirmishingPrefix",
			ShiftingSimulationId.Desperate => "DesperatePrefix",
			ShiftingSimulationId.Radiant => "RadiantPrefix",
			ShiftingSimulationId.Sanguine => "SanguinePrefix",
			ShiftingSimulationId.Deadeye => "DeadeyePrefix",
			ShiftingSimulationId.Temporal => "TemporalPrefix",
			ShiftingSimulationId.Echoing => "EchoingPrefix",
			ShiftingSimulationId.Stormforged => "StormforgedPrefix",
			ShiftingSimulationId.Catalytic => "CatalyticPrefix",
			ShiftingSimulationId.Spinbound => "SpinboundPrefix",
			_ => string.Empty
		};

		if (string.IsNullOrEmpty(prefixKey)) {
			return string.Empty;
		}

		return Language.GetTextValue($"Mods.{modName}.Prefixes.{prefixKey}.DisplayName");
	}

	internal static float ScaleMultiplierFromNeutral(float originalMultiplier)
	{
		return 1f + ((originalMultiplier - 1f) * ShiftingPrefix.ShiftingStrengthMultiplier);
	}

	internal static float ScaleValue(float originalValue)
	{
		return originalValue * ShiftingPrefix.ShiftingStrengthMultiplier;
	}

	internal static int ScaleRoundedInt(int originalValue)
	{
		return (int)System.MathF.Round(originalValue * ShiftingPrefix.ShiftingStrengthMultiplier);
	}

	internal static int ScalePositiveRoundedInt(int originalValue)
	{
		return System.Math.Max(1, ScaleRoundedInt(originalValue));
	}

	internal static int GetShiftedArmorPenetrationBonus(ShiftingSimulationId simulationId)
	{
		return simulationId switch {
			ShiftingSimulationId.Breaching => ScaleRoundedInt(BreachingPrefix.ArmorPenetrationValue),
			_ => 0
		};
	}

	internal static float GetShiftedCritBonus(ShiftingSimulationId simulationId)
	{
		return simulationId switch {
			ShiftingSimulationId.Breaching => ScaleValue(BreachingPrefix.CritBonus),
			ShiftingSimulationId.Skirmishing => ScaleValue(4f),
			ShiftingSimulationId.Desperate => ScaleValue(DesperatePrefix.CritBonus),
			ShiftingSimulationId.Radiant => ScaleValue(2f),
			ShiftingSimulationId.Deadeye => ScaleValue(6f),
			ShiftingSimulationId.Temporal => ScaleValue(3f),
			ShiftingSimulationId.Echoing => ScaleValue(-8f),
			ShiftingSimulationId.Stormforged => ScaleValue(4f),
			ShiftingSimulationId.Catalytic => ScaleValue(-2f),
			ShiftingSimulationId.Spinbound => ScaleValue(-4f),
			_ => 0f
		};
	}

	internal static float GetShiftedDamageMultiplier(ShiftingSimulationId simulationId)
	{
		return simulationId switch {
			ShiftingSimulationId.Breaching => ScaleMultiplierFromNeutral(BreachingPrefix.DamageMultiplier),
			ShiftingSimulationId.Attuned => ScaleMultiplierFromNeutral(0.84f),
			ShiftingSimulationId.Skirmishing => ScaleMultiplierFromNeutral(0.9f),
			ShiftingSimulationId.Desperate => ScaleMultiplierFromNeutral(1.05f),
			ShiftingSimulationId.Radiant => ScaleMultiplierFromNeutral(0.96f),
			ShiftingSimulationId.Temporal => ScaleMultiplierFromNeutral(0.5f),
			ShiftingSimulationId.Echoing => ScaleMultiplierFromNeutral(0.95f),
			ShiftingSimulationId.Stormforged => ScaleMultiplierFromNeutral(0.96f),
			ShiftingSimulationId.Catalytic => ScaleMultiplierFromNeutral(0.9f),
			ShiftingSimulationId.Spinbound => ScaleMultiplierFromNeutral(1.08f),
			_ => 1f
		};
	}

	internal static float GetShiftedKnockbackMultiplier(ShiftingSimulationId simulationId)
	{
		return simulationId switch {
			ShiftingSimulationId.Breaching => ScaleMultiplierFromNeutral(BreachingPrefix.KnockbackMultiplier),
			ShiftingSimulationId.Attuned => ScaleMultiplierFromNeutral(0.9f),
			ShiftingSimulationId.Skirmishing => ScaleMultiplierFromNeutral(0.85f),
			ShiftingSimulationId.Radiant => ScaleMultiplierFromNeutral(0.95f),
			ShiftingSimulationId.Deadeye => ScaleMultiplierFromNeutral(0.9f),
			ShiftingSimulationId.Temporal => ScaleMultiplierFromNeutral(0.9f),
			ShiftingSimulationId.Spinbound => ScaleMultiplierFromNeutral(0.92f),
			_ => 1f
		};
	}

	internal static float GetShiftedUseSpeedMultiplier(ShiftingSimulationId simulationId)
	{
		return simulationId switch {
			ShiftingSimulationId.Breaching => ScaleMultiplierFromNeutral(BreachingPrefix.UseTimeMultiplierValue),
			ShiftingSimulationId.Attuned => ScaleMultiplierFromNeutral(0.88f),
			ShiftingSimulationId.Skirmishing => ScaleMultiplierFromNeutral(0.8f),
			ShiftingSimulationId.Deadeye => ScaleMultiplierFromNeutral(1.12f),
			ShiftingSimulationId.Temporal => 1f / 2.5f,
			ShiftingSimulationId.Echoing => ScaleMultiplierFromNeutral(1.12f),
			ShiftingSimulationId.Stormforged => ScaleMultiplierFromNeutral(1.06f),
			ShiftingSimulationId.Spinbound => ScaleMultiplierFromNeutral(1.12f),
			_ => 1f
		};
	}

	internal static float GetShiftedShootSpeedMultiplier(ShiftingSimulationId simulationId)
	{
		return simulationId switch {
			ShiftingSimulationId.Attuned => ScaleMultiplierFromNeutral(1.1f),
			ShiftingSimulationId.Skirmishing => ScaleMultiplierFromNeutral(1.2f),
			ShiftingSimulationId.Deadeye => ScaleMultiplierFromNeutral(1.1f),
			ShiftingSimulationId.Temporal => 2.5f,
			ShiftingSimulationId.Spinbound => ScaleMultiplierFromNeutral(1.12f),
			_ => 1f
		};
	}

	internal static float GetShiftedManaCostMultiplier(ShiftingSimulationId simulationId)
	{
		return simulationId switch {
			ShiftingSimulationId.Attuned => ScaleMultiplierFromNeutral(0.75f),
			_ => 1f
		};
	}

	internal static float GetShiftedAttunedEmpoweredShootSpeedMultiplier()
	{
		return ScaleMultiplierFromNeutral(AttunedPrefix.EmpoweredCastShootSpeedMultiplier);
	}

	internal static float GetShiftedSkirmishingMoveSpeedBonus()
	{
		return ScaleValue(SkirmishingPrefix.MobilityMoveSpeedBonus);
	}

	internal static float GetShiftedSkirmishingRunAccelerationMultiplier()
	{
		return ScaleMultiplierFromNeutral(SkirmishingPrefix.MobilityRunAccelerationMultiplier);
	}

	internal static float GetShiftedSkirmishingMaxRunSpeedBonus()
	{
		return ScaleValue(SkirmishingPrefix.MobilityMaxRunSpeedBonus);
	}

	internal static float GetShiftedSkirmishingFollowUpShootSpeedMultiplier()
	{
		return ScaleMultiplierFromNeutral(SkirmishingPrefix.FollowUpShootSpeedMultiplier);
	}

	internal static float GetShiftedSkirmishingFollowUpCritDamageBonus()
	{
		return ScaleValue(SkirmishingPrefix.FollowUpCritDamageBonus);
	}

	internal static float GetShiftedScaleMultiplier(ShiftingSimulationId simulationId, float breachingScaleMultiplier)
	{
		return simulationId switch {
			ShiftingSimulationId.Breaching => ScaleMultiplierFromNeutral(BreachingPrefix.GetMergedScaleMultiplier(breachingScaleMultiplier)),
			_ => 1f
		};
	}

	internal static float GetShiftedDesperateMaxDamageBonus()
	{
		return ScaleValue(DesperatePrefix.MaxDamageBonus);
	}

	internal static float GetShiftedDesperateSurgeDamageBonus()
	{
		return ScaleValue(DesperatePrefix.SurgeDamageBonus);
	}

	internal static int GetShiftedRadiantDurationTicks()
	{
		return ScalePositiveRoundedInt(RadiantPrefix.OnFireDurationTicks);
	}

	internal static float GetShiftedRadiantLightMultiplier()
	{
		return ShiftingPrefix.ShiftingStrengthMultiplier;
	}

	internal static float GetShiftedSanguineLifeStealMultiplier()
	{
		return ScaleValue(SanguinePrefix.LifeStealMultiplier);
	}

	internal static int GetShiftedSanguineHealCapPerSecond()
	{
		return ScalePositiveRoundedInt(SanguinePrefix.BaseHealCapPerSecond);
	}

	internal static float GetShiftedVampiricManaRestoreRatio()
	{
		return ScaleValue(SanguinePrefix.ManaRestoreFromManaCost);
	}

	internal static int GetShiftedVampiricMaxRestorePerHit()
	{
		return ScalePositiveRoundedInt(SanguinePrefix.MaxManaRestorePerHit);
	}

	internal static int GetShiftedVampiricMaxRestorePerSecond()
	{
		return ScalePositiveRoundedInt(SanguinePrefix.BaseManaRestorePerSecond);
	}

	internal static float GetShiftedDeadeyeVelocityMultiplier()
	{
		return ScaleMultiplierFromNeutral(DeadeyePrefix.DeadeyeShotVelocityMultiplier);
	}

	internal static int GetShiftedDeadeyeCritChanceBonus()
	{
		return ScaleRoundedInt(DeadeyePrefix.DeadeyeShotCritChanceBonus);
	}

	internal static float GetShiftedDeadeyeCritDamageBonus()
	{
		return ScaleValue(DeadeyePrefix.DeadeyeShotCritDamageBonus);
	}

	internal static float GetShiftedDeadeyeBurstDamageRatio()
	{
		return ScaleValue(DeadeyePrefix.CriticalBurstDamageRatio);
	}

	internal static float GetShiftedFractureIChance()
	{
		return System.MathF.Min(1f, PrefixTuningConfig.Instance.FractureIChance * ShiftingPrefix.ShiftingStrengthMultiplier);
	}

	internal static float GetShiftedFractureIIChance()
	{
		return System.MathF.Min(1f, PrefixTuningConfig.Instance.FractureIIChance * ShiftingPrefix.ShiftingStrengthMultiplier);
	}

	internal static float GetShiftedFractureIIIChance()
	{
		return System.MathF.Min(1f, PrefixTuningConfig.Instance.FractureIIIChance * ShiftingPrefix.ShiftingStrengthMultiplier);
	}

	internal static float GetShiftedStormforgedProcChance()
	{
		return 0.375f;
	}

	internal static int GetShiftedStormforgedMaxChainJumps()
	{
		return 4;
	}

	internal static float GetShiftedStormforgedChainRangePixels()
	{
		return 540f;
	}

	internal static float GetShiftedStormforgedChainDamageDecay()
	{
		return 0.9f;
	}

	internal static int GetShiftedCatalyticMarkDurationTicks()
	{
		return 450;
	}

	internal static int GetShiftedCatalyticArmDelayTicks()
	{
		return 9;
	}

	internal static float GetShiftedCatalyticConsumeDamageBonus()
	{
		return 0.525f;
	}

	internal static int GetShiftedSpinboundArmorPenetrationBonus()
	{
		return 9;
	}

	internal static float GetShiftedSpinboundEmpoweredDamageBonus()
	{
		return 0.3f;
	}

	internal static float GetShiftedSpinboundEmpoweredCritDamageBonus()
	{
		return 0.15f;
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
