using System.Collections.Generic;
using Mozandifiers.Content.Prefixes.Common;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Weapons;

public sealed class ShiftingPrefix : WeaponPrefix
{
	public const int ShiftDurationTicks = 600;
	public const int CombatWindowTicks = 300;
	public const float ShiftingStrengthMultiplier = 1.5f;

	public override PrefixCategory Category => PrefixCategory.AnyWeapon;

	public override bool CanRoll(Item item)
	{
		return ShiftingSimulationCatalog.TryGetWeaponMode(item, out _);
	}

	protected override IEnumerable<TooltipLine> GetExtraTooltipLines(Item item)
	{
		yield return new TooltipLine(
			Mod,
			$"{Name}Shifting",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.ShiftingTooltip",
				ShiftDurationTicks / 60,
				CombatWindowTicks / 60));

		yield return new TooltipLine(
			Mod,
			$"{Name}ShiftPower",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.ShiftPowerTooltip",
				(int)(ShiftingStrengthMultiplier * 100f)));
	}
}
