using System.Collections.Generic;
using Mozandifiers.Common.Config;
using Mozandifiers.Content.Prefixes.Common;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Weapons;

public sealed class ShiftingPrefix : WeaponPrefix
{
	public static int ShiftDurationTicks => PrefixTuningConfig.Instance.ShiftingShiftDurationTicks;
	public static int CombatWindowTicks => PrefixTuningConfig.Instance.ShiftingCombatWindowTicks;
	public static float ShiftingStrengthMultiplier => PrefixTuningConfig.Instance.ShiftingStrengthMultiplier;

	public override PrefixCategory Category => PrefixCategory.AnyWeapon;

	public override bool CanRoll(Item item)
	{
		return ShiftingSimulationPools.TryGetWeaponMode(item, out _);
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
