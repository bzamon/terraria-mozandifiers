using System.Collections.Generic;
using Mozandifiers.Content.Prefixes.Common;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Weapons;

public sealed class SpinboundPrefix : WeaponPrefix
{
	public static readonly int[] FibonacciShotGaps = [1, 1, 2, 3, 5, 8, 13];
	public const float EmpoweredDamageBonus = 0.2f;
	public const float EmpoweredCritDamageBonus = 0.1f;
	public const float StabilizationStrength = 0.035f;
	public const int MaxGoldenSpinMultiplier = 5;
	public const int MaxGoldenSpinTier = 5;
	public const float GoldenRatio = 1.618034f;
	public const float InverseGoldenRatio = 0.618034f;
	public const float DistanceToSpeedTolerance = 0.24f;
	public const float ComponentRatioTolerance = 0.42f;
	public const float AlignmentTolerance = 0.16f;
	public const float MinimumGoldenRatioDenominator = 1f;

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
