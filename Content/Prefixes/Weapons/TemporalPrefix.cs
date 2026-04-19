using System.Collections.Generic;
using Mozandifiers.Content.Prefixes.Common;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Weapons;

public sealed class TemporalPrefix : WeaponPrefix
{
	public const float AttackSpeedMultiplier = 2f;

	protected override float PrefixRollChance => 0.6f;
	protected override float PrefixValueMultiplier => 1.5f;

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
		damageMult *= (1f / AttackSpeedMultiplier);
		useTimeMult *= (1f / AttackSpeedMultiplier);
        shootSpeedMult *= (AttackSpeedMultiplier/2);
	}

	protected override IEnumerable<TooltipLine> GetExtraTooltipLines(Item item)
	{
		yield return new TooltipLine(
			Mod,
			$"{Name}Temporal",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.TemporalTooltip",
				(int)((AttackSpeedMultiplier - 1f) * 100f)));
	}
}
