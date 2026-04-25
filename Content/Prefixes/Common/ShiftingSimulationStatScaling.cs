using Mozandifiers.Content.Prefixes.Weapons;

namespace Mozandifiers.Content.Prefixes.Common;

internal static class ShiftingSimulationStatScaling
{
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
			ShiftingSimulationId.Fractured => ScaleValue(-8f),
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
			ShiftingSimulationId.Fractured => ScaleMultiplierFromNeutral(0.95f),
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
			ShiftingSimulationId.Fractured => ScaleMultiplierFromNeutral(1.12f),
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

	internal static float GetShiftedScaleMultiplier(ShiftingSimulationId simulationId, float breachingScaleMultiplier)
	{
		return simulationId switch {
			ShiftingSimulationId.Breaching => ScaleMultiplierFromNeutral(BreachingPrefix.GetMergedScaleMultiplier(breachingScaleMultiplier)),
			_ => 1f
		};
	}
}
