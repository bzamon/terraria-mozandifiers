using System.Collections.Generic;
using Mozandifiers.Content.Prefixes.Common;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Weapons;

public sealed class StormforgedPrefix : WeaponPrefix
{
	public const int MaxChainJumps = 3;
	public const float ChainProcChance = 0.25f;
	public const float ChainRangePixels = 360f;
	public const float ChainDamageDecay = 0.65f;

	protected override float PrefixRollChance => 0.7f;
	protected override float PrefixValueMultiplier => 1.5f;

	public override PrefixCategory Category => PrefixCategory.AnyWeapon;

	public override bool CanRoll(Item item)
	{
		return IsSwingingMeleeWeapon(item) || IsStandardProjectileCombatWeapon(item);
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
		useTimeMult *= 1.06f;
		critBonus += 4;
	}

	protected override IEnumerable<TooltipLine> GetExtraTooltipLines(Item item)
	{
		yield return new TooltipLine(
			Mod,
			$"{Name}Stormforged",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.StormforgedTooltip",
				(int)(ChainProcChance * 100f),
				MaxChainJumps));
	}
}
