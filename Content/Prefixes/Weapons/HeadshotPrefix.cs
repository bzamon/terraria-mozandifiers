using System.Collections.Generic;
using Mozandifiers.Content.Prefixes.Common;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Weapons;

public sealed class HeadshotPrefix : WeaponPrefix
{
	public const float MaxDistanceDamageBonus = 0.3f;
	public const float FullDistanceBonusPixels = 600f;
	public const float LongRangeCritThresholdPixels = FullDistanceBonusPixels * 0.7f;
	public const float CritDamageBonus = 0.35f;
	public const float LongRangeCritDamageBonus = 0.6f;

	protected override float PrefixRollChance => 0.7f;
	protected override float PrefixValueMultiplier => 1.45f;

	public override PrefixCategory Category => PrefixCategory.Ranged;

	public override bool CanRoll(Item item)
	{
		return IsRangedWeapon(item) && IsStandardProjectileCombatWeapon(item);
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
		useTimeMult *= 1.12f;
		shootSpeedMult *= 1.1f;
		knockbackMult *= 0.9f;
		critBonus += 6;
	}

	protected override IEnumerable<TooltipLine> GetExtraTooltipLines(Item item)
	{
		yield return new TooltipLine(
			Mod,
			$"{Name}DistanceDamage",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.DistanceDamageTooltip",
				(int)(MaxDistanceDamageBonus * 100f),
				(int)FullDistanceBonusPixels));

		yield return new TooltipLine(
			Mod,
			$"{Name}CritDamage",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.CritDamageTooltip",
				(int)(CritDamageBonus * 100f),
				(int)(LongRangeCritDamageBonus * 100f),
				(int)LongRangeCritThresholdPixels));
	}
}
