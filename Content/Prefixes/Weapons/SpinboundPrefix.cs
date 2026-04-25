using System.Collections.Generic;
using Mozandifiers.Common.Config;
using Mozandifiers.Content.Prefixes.Common;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Weapons;

public sealed class SpinboundPrefix : WeaponPrefix
{
	public static readonly int[] FibonacciShotGaps = [1, 1, 2, 3, 5, 8, 13];
	public static float EmpoweredDamageBonus => PrefixTuningConfig.Instance.SpinboundEmpoweredDamageBonus;
	public static float EmpoweredCritDamageBonus => PrefixTuningConfig.Instance.SpinboundEmpoweredCritDamageBonus;
	public static float StabilizationStrength => PrefixTuningConfig.Instance.SpinboundStabilizationStrength;
	public const int MaxGoldenSpinMultiplier = 5;
	public const int MaxGoldenSpinTier = 5;
	public const float GoldenRatio = 1.618034f;
	public const float InverseGoldenRatio = 0.618034f;
	public const float LowGoldenAngleDegrees = 31.72f;
	public const float HighGoldenAngleDegrees = 58.28f;
	public static float VelocityRatioTolerance => PrefixTuningConfig.Instance.SpinboundVelocityRatioTolerance;
	public static float AngleToleranceDegrees => PrefixTuningConfig.Instance.SpinboundAngleToleranceDegrees;
	public static float AnglePrecisionToleranceDegrees => PrefixTuningConfig.Instance.SpinboundAnglePrecisionToleranceDegrees;
	public static float SpeedRatioTolerance => PrefixTuningConfig.Instance.SpinboundSpeedRatioTolerance;

	protected override int ArmorPenetrationBonus => 6;
	protected override float PrefixRollChance => 0.65f;
	protected override float PrefixValueMultiplier => 1.45f;

	public override PrefixCategory Category => PrefixCategory.Ranged;

	public override bool CanRoll(Item item)
	{
		return IsRangedWeapon(item) && IsProjectileWeapon(item);
	}

	public override void SetStats(
		ref float damageMult,
		ref float knockbackMult,
		ref float useTimeMult,
		ref float scaleMult,
		ref float shootSpeedMult,
		ref float manaMult,
		ref int critBonus)
	{
		damageMult *= 1.08f;
		useTimeMult *= 1.12f;
		knockbackMult *= 0.92f;
		shootSpeedMult *= 1.12f;
		critBonus -= 4;
	}

	protected override IEnumerable<TooltipLine> GetExtraTooltipLines(Item item)
	{
		yield return new TooltipLine(
			Mod,
			$"{Name}Spinbound",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.SpinboundTooltip",
				(int)(EmpoweredDamageBonus * 100f),
				(int)(EmpoweredCritDamageBonus * 100f)));

		yield return new TooltipLine(
			Mod,
			$"{Name}GoldenSpin",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.GoldenSpinTooltip",
				MaxGoldenSpinMultiplier));
	}
}
