using System.Collections.Generic;
using Mozandifiers.Content.Prefixes.Common;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Weapons;

public sealed class RadiantPrefix : WeaponPrefix
{
	public const int OnFireDurationTicks = 180;

	protected override float PrefixRollChance => 0.9f;
	protected override float PrefixValueMultiplier => 1.35f;

	public override PrefixCategory Category => PrefixCategory.AnyWeapon;

	public override bool CanRoll(Item item)
	{
		return IsStandardDirectCombatWeapon(item);
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
		damageMult *= 0.96f;
		knockbackMult *= 0.95f;
		critBonus += 2;
	}

	protected override IEnumerable<TooltipLine> GetExtraTooltipLines(Item item)
	{
		yield return new TooltipLine(
			Mod,
			$"{Name}Radiant",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.RadiantTooltip",
				OnFireDurationTicks / 60));
	}
}
