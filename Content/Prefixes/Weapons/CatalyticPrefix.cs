using System.Collections.Generic;
using Mozandifiers.Common.Config;
using Mozandifiers.Content.Prefixes.Common;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Weapons;

public sealed class CatalyticPrefix : WeaponPrefix
{
	public static int MarkDurationTicks => PrefixTuningConfig.Instance.CatalyticMarkDurationTicks;
	public static int MarkArmDelayTicks => PrefixTuningConfig.Instance.CatalyticMarkArmDelayTicks;
	public static float ConsumeDamageBonus => PrefixTuningConfig.Instance.CatalyticConsumeDamageBonus;

	protected override float PrefixRollChance => 0.65f;
	protected override float PrefixValueMultiplier => 1.5f;

	public override PrefixCategory Category => PrefixCategory.AnyWeapon;

	public override bool CanRoll(Item item)
	{
		return IsStandardMagicProjectileWeapon(item);
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
		damageMult *= 0.9f;
		critBonus -= 2;
	}

	protected override IEnumerable<TooltipLine> GetExtraTooltipLines(Item item)
	{
		yield return new TooltipLine(
			Mod,
			$"{Name}Catalytic",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.CatalyticTooltip",
				MarkDurationTicks / 60f,
				(int)(ConsumeDamageBonus * 100f)));
	}
}
