using System.Collections.Generic;
using Mozandifiers.Common.Config;
using Mozandifiers.Content.Prefixes.Common;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Weapons;

public sealed class EchoingPrefix : WeaponPrefix
{
	public const float FractureIDamageMultiplier = 0.45f;
	public const float FractureIIDamageMultiplier = 0.35f;
	public const float FractureIIIDamageMultiplier = 0.25f;
	public const float FractureIAngleVarianceDegrees = 2f;
	public const float FractureIIAngleVarianceDegrees = 4f;
	public const float FractureIIIAngleVarianceDegrees = 6f;
	public const int FractureIIDelayMinFrames = 1;
	public const int FractureIIDelayMaxFrames = 3;
	public const int FractureIIIDelayMinFrames = 2;
	public const int FractureIIIDelayMaxFrames = 5;

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
