using System.Collections.Generic;
using Mozandifiers.Content.Prefixes.Common;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Weapons;

public sealed class SanguinePrefix : WeaponPrefix
{
	public const float LifeStealMultiplier = 0.1f;

	protected override float PrefixRollChance => 0.8f;
	protected override float PrefixValueMultiplier => 1.45f;

	public override PrefixCategory Category => PrefixCategory.AnyWeapon;

	public override bool CanRoll(Item item)
	{
		return IsStandardWeapon(item)
			&& (item.CountsAsClass(DamageClass.Melee)
				|| item.CountsAsClass(DamageClass.Ranged)
				|| item.CountsAsClass(DamageClass.Magic));
	}

	protected override IEnumerable<TooltipLine> GetExtraTooltipLines(Item item)
	{
		yield return new TooltipLine(
			Mod,
			$"{Name}LifeSteal",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.LifeStealTooltip",
				(int)(LifeStealMultiplier * 100f)));
	}
}
