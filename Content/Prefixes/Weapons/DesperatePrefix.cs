using System.Collections.Generic;
using Mozandifiers.Content.Prefixes.Common;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Weapons;

public sealed class DesperatePrefix : WeaponPrefix
{
	public const float MaxDamageBonus = 1f;

	protected override float PrefixRollChance => 0.75f;
	protected override float PrefixValueMultiplier => 1.4f;

	public override PrefixCategory Category => PrefixCategory.AnyWeapon;

	public override bool CanRoll(Item item)
	{
		return IsStandardCombatWeapon(item);
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
		damageMult *= 0.90f;
		critBonus += 5;
	}

	protected override IEnumerable<TooltipLine> GetExtraTooltipLines(Item item)
	{
		yield return new TooltipLine(
			Mod,
			$"{Name}Desperate",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.DesperateTooltip",
				(int)(MaxDamageBonus * 100f)));
	}
}
