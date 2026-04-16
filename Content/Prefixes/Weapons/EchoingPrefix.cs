using System.Collections.Generic;
using Mozandifiers.Common.Config;
using Mozandifiers.Content.Prefixes.Common;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Weapons;

public sealed class EchoingPrefix : WeaponPrefix
{
	public const float EchoDamageMultiplier = 0.5f;

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
				$"{Name}EchoChance",
				Language.GetTextValue(
					$"Mods.{Mod.Name}.Prefixes.{Name}.EchoTooltip",
					PrefixTuningConfig.Instance.EchoChancePercent,
					(int)(EchoDamageMultiplier * 100f)));
	}
}
