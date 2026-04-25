using Mozandifiers.Common.Config;
using Mozandifiers.Content.Prefixes.Weapons;

namespace Mozandifiers.Content.Prefixes.Common;

internal static class ShiftingSimulationEffectScaling
{
	internal static float GetShiftedAttunedEmpoweredShootSpeedMultiplier()
	{
		return ShiftingSimulationStatScaling.ScaleMultiplierFromNeutral(AttunedPrefix.EmpoweredCastShootSpeedMultiplier);
	}

	internal static float GetShiftedSkirmishingMoveSpeedBonus()
	{
		return ShiftingSimulationStatScaling.ScaleValue(SkirmishingPrefix.MobilityMoveSpeedBonus);
	}

	internal static float GetShiftedSkirmishingRunAccelerationMultiplier()
	{
		return ShiftingSimulationStatScaling.ScaleMultiplierFromNeutral(SkirmishingPrefix.MobilityRunAccelerationMultiplier);
	}

	internal static float GetShiftedSkirmishingMaxRunSpeedBonus()
	{
		return ShiftingSimulationStatScaling.ScaleValue(SkirmishingPrefix.MobilityMaxRunSpeedBonus);
	}

	internal static float GetShiftedSkirmishingFollowUpShootSpeedMultiplier()
	{
		return ShiftingSimulationStatScaling.ScaleMultiplierFromNeutral(SkirmishingPrefix.FollowUpShootSpeedMultiplier);
	}

	internal static float GetShiftedSkirmishingFollowUpCritDamageBonus()
	{
		return ShiftingSimulationStatScaling.ScaleValue(SkirmishingPrefix.FollowUpCritDamageBonus);
	}

	internal static float GetShiftedDesperateMaxDamageBonus()
	{
		return ShiftingSimulationStatScaling.ScaleValue(DesperatePrefix.MaxDamageBonus);
	}

	internal static float GetShiftedDesperateSurgeDamageBonus()
	{
		return ShiftingSimulationStatScaling.ScaleValue(DesperatePrefix.SurgeDamageBonus);
	}

	internal static int GetShiftedRadiantDurationTicks()
	{
		return ShiftingSimulationStatScaling.ScalePositiveRoundedInt(RadiantPrefix.OnFireDurationTicks);
	}

	internal static float GetShiftedRadiantLightMultiplier()
	{
		return ShiftingPrefix.ShiftingStrengthMultiplier;
	}

	internal static float GetShiftedVampiricLifeStealMultiplier()
	{
		return ShiftingSimulationStatScaling.ScaleValue(SanguinePrefix.LifeStealMultiplier);
	}

	internal static int GetShiftedVampiricHealCapPerSecond()
	{
		return ShiftingSimulationStatScaling.ScalePositiveRoundedInt(SanguinePrefix.BaseHealCapPerSecond);
	}

	// Transitional forwarders while the runtime vocabulary moves off legacy names.
	internal static float GetShiftedSanguineLifeStealMultiplier() => GetShiftedVampiricLifeStealMultiplier();
	internal static int GetShiftedSanguineHealCapPerSecond() => GetShiftedVampiricHealCapPerSecond();

	internal static float GetShiftedVampiricManaRestoreRatio()
	{
		return ShiftingSimulationStatScaling.ScaleValue(SanguinePrefix.ManaRestoreFromManaCost);
	}

	internal static int GetShiftedVampiricMaxRestorePerHit()
	{
		return ShiftingSimulationStatScaling.ScalePositiveRoundedInt(SanguinePrefix.MaxManaRestorePerHit);
	}

	internal static int GetShiftedVampiricMaxRestorePerSecond()
	{
		return ShiftingSimulationStatScaling.ScalePositiveRoundedInt(SanguinePrefix.BaseManaRestorePerSecond);
	}

	internal static float GetShiftedDeadeyeVelocityMultiplier()
	{
		return ShiftingSimulationStatScaling.ScaleMultiplierFromNeutral(DeadeyePrefix.DeadeyeShotVelocityMultiplier);
	}

	internal static int GetShiftedDeadeyeCritChanceBonus()
	{
		return ShiftingSimulationStatScaling.ScaleRoundedInt(DeadeyePrefix.DeadeyeShotCritChanceBonus);
	}

	internal static float GetShiftedDeadeyeCritDamageBonus()
	{
		return ShiftingSimulationStatScaling.ScaleValue(DeadeyePrefix.DeadeyeShotCritDamageBonus);
	}

	internal static float GetShiftedDeadeyeBurstDamageRatio()
	{
		return ShiftingSimulationStatScaling.ScaleValue(DeadeyePrefix.CriticalBurstDamageRatio);
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
		return ShiftingSimulationStatScaling.ScalePositiveRoundedInt(CatalyticPrefix.MarkDurationTicks);
	}

	internal static int GetShiftedCatalyticArmDelayTicks()
	{
		return ShiftingSimulationStatScaling.ScalePositiveRoundedInt(CatalyticPrefix.MarkArmDelayTicks);
	}

	internal static float GetShiftedCatalyticConsumeDamageBonus()
	{
		return ShiftingSimulationStatScaling.ScaleValue(CatalyticPrefix.ConsumeDamageBonus);
	}

	internal static int GetShiftedSpinboundArmorPenetrationBonus()
	{
		return ShiftingSimulationStatScaling.ScaleRoundedInt(6);
	}

	internal static float GetShiftedSpinboundEmpoweredDamageBonus()
	{
		return ShiftingSimulationStatScaling.ScaleValue(SpinboundPrefix.EmpoweredDamageBonus);
	}

	internal static float GetShiftedSpinboundEmpoweredCritDamageBonus()
	{
		return ShiftingSimulationStatScaling.ScaleValue(SpinboundPrefix.EmpoweredCritDamageBonus);
	}
}
