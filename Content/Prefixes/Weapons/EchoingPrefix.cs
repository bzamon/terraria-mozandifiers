using System.Collections.Generic;
using Mozandifiers.Common.Config;
using Mozandifiers.Content.Prefixes.Common;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Weapons;

public sealed class EchoingPrefix : WeaponPrefix
{
	public static float FractureIDamageMultiplier => PrefixTuningConfig.Instance.FractureIDamageMultiplier;
	public static float FractureIIDamageMultiplier => PrefixTuningConfig.Instance.FractureIIDamageMultiplier;
	public static float FractureIIIDamageMultiplier => PrefixTuningConfig.Instance.FractureIIIDamageMultiplier;
	public static float FractureIAngleVarianceDegrees => PrefixTuningConfig.Instance.FractureIAngleVarianceDegrees;
	public static float FractureIIAngleVarianceDegrees => PrefixTuningConfig.Instance.FractureIIAngleVarianceDegrees;
	public static float FractureIIIAngleVarianceDegrees => PrefixTuningConfig.Instance.FractureIIIAngleVarianceDegrees;
	public static int FractureIIDelayMinFrames => PrefixTuningConfig.Instance.FractureIIDelayMinFrames;
	public static int FractureIIDelayMaxFrames => PrefixTuningConfig.Instance.FractureIIDelayMaxFrames;
	public static int FractureIIIDelayMinFrames => PrefixTuningConfig.Instance.FractureIIIDelayMinFrames;
	public static int FractureIIIDelayMaxFrames => PrefixTuningConfig.Instance.FractureIIIDelayMaxFrames;

	protected override float PrefixRollChance => 0.7f;
	protected override float PrefixValueMultiplier => 1.55f;

	public override PrefixCategory Category => PrefixCategory.AnyWeapon;

	public override bool CanRoll(Item item)
	{
		return IsImplementedEchoingMode(GetEchoingWeaponMode(item));
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
		damageMult *= 0.95f;
		useTimeMult *= 1.12f;
		critBonus -= 8;
	}

	protected override IEnumerable<TooltipLine> GetExtraTooltipLines(Item item)
	{
		yield return new TooltipLine(
			Mod,
			$"{Name}FractureChance",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.FractureTooltip",
				PrefixTuningConfig.Instance.FractureIChancePercent,
				PrefixTuningConfig.Instance.FractureIIChancePercent,
				PrefixTuningConfig.Instance.FractureIIIChancePercent));

		yield return new TooltipLine(
			Mod,
			$"{Name}FractureDamage",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.FractureDamageTooltip",
				(int)(FractureIDamageMultiplier * 100f),
				(int)(FractureIIDamageMultiplier * 100f),
				(int)(FractureIIIDamageMultiplier * 100f)));
	}
}
