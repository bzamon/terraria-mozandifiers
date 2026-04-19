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
	Colossal,
	Breaching,
	Attuned,
	Skirmishing,
	Desperate,
	Radiant,
	Sanguine,
	Siphoning,
	Headshot,
	Temporal,
	Echoing,
	Stormforged,
	Catalytic,
	Spinbound
}

internal static class ShiftingSimulationCatalog
{
	private static readonly ShiftingSimulationId[] MeleePool = [
		ShiftingSimulationId.Colossal,
		ShiftingSimulationId.Breaching,
		ShiftingSimulationId.Desperate,
		ShiftingSimulationId.Radiant,
		ShiftingSimulationId.Sanguine,
		ShiftingSimulationId.Temporal,
		ShiftingSimulationId.Stormforged
	];

	private static readonly ShiftingSimulationId[] RangedPool = [
		ShiftingSimulationId.Skirmishing,
		ShiftingSimulationId.Headshot,
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
		ShiftingSimulationId.Siphoning,
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
			ShiftingSimulationId.Colossal => "ColossalPrefix",
			ShiftingSimulationId.Breaching => "BreachingPrefix",
			ShiftingSimulationId.Attuned => "AttunedPrefix",
			ShiftingSimulationId.Skirmishing => "SkirmishingPrefix",
			ShiftingSimulationId.Desperate => "DesperatePrefix",
			ShiftingSimulationId.Radiant => "RadiantPrefix",
			ShiftingSimulationId.Sanguine => "SanguinePrefix",
			ShiftingSimulationId.Siphoning => "SiphoningPrefix",
			ShiftingSimulationId.Headshot => "HeadshotPrefix",
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
			ShiftingSimulationId.Breaching => ScaleRoundedInt(10),
			_ => 0
		};
	}

	internal static float GetShiftedCritBonus(ShiftingSimulationId simulationId)
	{
		return simulationId switch {
			ShiftingSimulationId.Colossal => ScaleValue(5f),
			ShiftingSimulationId.Breaching => ScaleValue(5f),
			ShiftingSimulationId.Skirmishing => ScaleValue(4f),
			ShiftingSimulationId.Desperate => ScaleValue(5f),
			ShiftingSimulationId.Radiant => ScaleValue(2f),
			ShiftingSimulationId.Siphoning => ScaleValue(2f),
			ShiftingSimulationId.Headshot => ScaleValue(6f),
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
			ShiftingSimulationId.Colossal => ScaleMultiplierFromNeutral(1.25f),
			ShiftingSimulationId.Breaching => ScaleMultiplierFromNeutral(1.14f),
			ShiftingSimulationId.Attuned => ScaleMultiplierFromNeutral(0.84f),
			ShiftingSimulationId.Skirmishing => ScaleMultiplierFromNeutral(0.9f),
			ShiftingSimulationId.Desperate => ScaleMultiplierFromNeutral(1.05f),
			ShiftingSimulationId.Radiant => ScaleMultiplierFromNeutral(0.96f),
			ShiftingSimulationId.Siphoning => ScaleMultiplierFromNeutral(0.9f),
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
			ShiftingSimulationId.Colossal => ScaleMultiplierFromNeutral(1.2f),
			ShiftingSimulationId.Breaching => ScaleMultiplierFromNeutral(1.12f),
			ShiftingSimulationId.Attuned => ScaleMultiplierFromNeutral(0.9f),
			ShiftingSimulationId.Skirmishing => ScaleMultiplierFromNeutral(0.85f),
			ShiftingSimulationId.Radiant => ScaleMultiplierFromNeutral(0.95f),
			ShiftingSimulationId.Headshot => ScaleMultiplierFromNeutral(0.9f),
			ShiftingSimulationId.Temporal => ScaleMultiplierFromNeutral(0.9f),
			ShiftingSimulationId.Spinbound => ScaleMultiplierFromNeutral(0.92f),
			_ => 1f
		};
	}

	internal static float GetShiftedUseSpeedMultiplier(ShiftingSimulationId simulationId)
	{
		return simulationId switch {
			ShiftingSimulationId.Colossal => ScaleMultiplierFromNeutral(1.25f),
			ShiftingSimulationId.Breaching => ScaleMultiplierFromNeutral(1.18f),
			ShiftingSimulationId.Attuned => ScaleMultiplierFromNeutral(0.88f),
			ShiftingSimulationId.Skirmishing => ScaleMultiplierFromNeutral(0.8f),
			ShiftingSimulationId.Headshot => ScaleMultiplierFromNeutral(1.12f),
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
			ShiftingSimulationId.Headshot => ScaleMultiplierFromNeutral(1.1f),
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

	internal static float GetShiftedScaleMultiplier(ShiftingSimulationId simulationId, float colossalScaleMultiplier)
	{
		return simulationId switch {
			ShiftingSimulationId.Colossal => ScaleMultiplierFromNeutral(colossalScaleMultiplier),
			_ => 1f
		};
	}

	internal static float GetShiftedDesperateMaxDamageBonus()
	{
		return ScaleValue(DesperatePrefix.MaxDamageBonus);
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
		return ScalePositiveRoundedInt(5);
	}

	internal static float GetShiftedSiphoningManaRestoreRatio()
	{
		return ScaleValue(SiphoningPrefix.ManaRestoreFromManaCost);
	}

	internal static int GetShiftedSiphoningMaxRestorePerHit()
	{
		return ScalePositiveRoundedInt(SiphoningPrefix.MaxManaRestorePerHit);
	}

	internal static int GetShiftedSiphoningMaxRestorePerSecond()
	{
		return ScalePositiveRoundedInt(SiphoningPrefix.MaxManaRestorePerSecond);
	}

	internal static float GetShiftedHeadshotMaxDistanceDamageBonus()
	{
		return ScaleValue(HeadshotPrefix.MaxDistanceDamageBonus);
	}

	internal static float GetShiftedHeadshotCritDamageBonus()
	{
		return ScaleValue(HeadshotPrefix.CritDamageBonus);
	}

	internal static float GetShiftedHeadshotLongRangeCritDamageBonus()
	{
		return ScaleValue(HeadshotPrefix.LongRangeCritDamageBonus);
	}

	internal static float GetShiftedEchoChance()
	{
		return System.MathF.Min(1f, PrefixTuningConfig.Instance.EchoChance * ShiftingPrefix.ShiftingStrengthMultiplier);
	}

	internal static float GetShiftedEchoDamageMultiplier()
	{
		return 0.75f;
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
