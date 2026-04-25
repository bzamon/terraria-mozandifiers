using System.Collections.Generic;
using Mozandifiers.Common.Config;
using Mozandifiers.Content.Prefixes.Common;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Weapons;

public sealed class SkirmishingPrefix : WeaponPrefix
{
	public static int SkirmishWindowTicks => PrefixTuningConfig.Instance.SkirmishWindowTicks;
	public static float MobilityMoveSpeedBonus => PrefixTuningConfig.Instance.SkirmishingMobilityMoveSpeedBonus;
	public static float MobilityRunAccelerationMultiplier => PrefixTuningConfig.Instance.SkirmishingRunAccelerationMultiplier;
	public static float MobilityMaxRunSpeedBonus => PrefixTuningConfig.Instance.SkirmishingMaxRunSpeedBonus;
	public static float FollowUpShootSpeedMultiplier => PrefixTuningConfig.Instance.SkirmishingFollowUpShootSpeedMultiplier;
	public static float FollowUpCritDamageBonus => PrefixTuningConfig.Instance.SkirmishingFollowUpCritDamageBonus;

	protected override float PrefixRollChance => 0.95f;
	protected override float PrefixValueMultiplier => 1.3f;

	public override PrefixCategory Category => PrefixCategory.Ranged;

	public override bool CanRoll(Item item)
	{
		return IsRangedWeapon(item) && IsProjectileWeapon(item);
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
		knockbackMult *= 0.85f;
		useTimeMult *= 0.80f;
		shootSpeedMult *= 1.20f;
		critBonus += 4;
	}

	protected override IEnumerable<TooltipLine> GetExtraTooltipLines(Item item)
	{
		yield return new TooltipLine(
			Mod,
			$"{Name}SkirmishWindow",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.SkirmishWindowTooltip",
				SkirmishWindowTicks / 60f));

		yield return new TooltipLine(
			Mod,
			$"{Name}Mobility",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.MobilityTooltip",
				(int)(MobilityMoveSpeedBonus * 100f)));

		yield return new TooltipLine(
			Mod,
			$"{Name}FollowUp",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.FollowUpTooltip",
				(int)((FollowUpShootSpeedMultiplier - 1f) * 100f),
				(int)(FollowUpCritDamageBonus * 100f)));
	}
}
