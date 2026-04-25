using System.Collections.Generic;
using Mozandifiers.Common.Config;
using Mozandifiers.Content.Prefixes.Common;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Weapons;

public sealed class DeadeyePrefix : WeaponPrefix
{
	public static int ReadyIntervalTicks => PrefixTuningConfig.Instance.DeadeyeReadyIntervalTicks;
	public static int StationaryReadyTicks => PrefixTuningConfig.Instance.DeadeyeStationaryReadyTicks;
	public static int ProjectileAssignmentLockoutTicks => PrefixTuningConfig.Instance.DeadeyeProjectileAssignmentLockoutTicks;
	public static float StationaryTolerancePixels => PrefixTuningConfig.Instance.DeadeyeStationaryTolerancePixels;
	public static float DeadeyeShotVelocityMultiplier => PrefixTuningConfig.Instance.DeadeyeShotVelocityMultiplier;
	public static int DeadeyeShotCritChanceBonus => PrefixTuningConfig.Instance.DeadeyeShotCritChanceBonus;
	public static float DeadeyeShotCritDamageBonus => PrefixTuningConfig.Instance.DeadeyeShotCritDamageBonus;
	public static float CriticalBurstDamageRatio => PrefixTuningConfig.Instance.DeadeyeCriticalBurstDamageRatio;
	public static float CriticalBurstRadiusPixels => PrefixTuningConfig.Instance.DeadeyeCriticalBurstRadiusPixels;
	public static float BossBurstDamageMultiplier => PrefixTuningConfig.Instance.DeadeyeBossBurstDamageMultiplier;

	protected override float PrefixRollChance => 0.7f;
	protected override float PrefixValueMultiplier => 1.45f;

	public override PrefixCategory Category => PrefixCategory.Ranged;

	public override bool CanRoll(Item item)
	{
		return IsRangedWeapon(item) && IsStandardProjectileCombatWeapon(item);
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
		useTimeMult *= 1.12f;
		shootSpeedMult *= 1.1f;
		knockbackMult *= 0.9f;
		critBonus += 6;
	}

	protected override IEnumerable<TooltipLine> GetExtraTooltipLines(Item item)
	{
		yield return new TooltipLine(
			Mod,
			$"{Name}Ready",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.ReadyTooltip",
				ReadyIntervalTicks / 60f,
				StationaryReadyTicks / 60f));

		yield return new TooltipLine(
			Mod,
			$"{Name}Shot",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.ShotTooltip",
				(int)((DeadeyeShotVelocityMultiplier - 1f) * 100f),
				DeadeyeShotCritChanceBonus,
				(int)(DeadeyeShotCritDamageBonus * 100f)));

		yield return new TooltipLine(
			Mod,
			$"{Name}Burst",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.BurstTooltip",
				(int)(CriticalBurstDamageRatio * 100f),
				(int)CriticalBurstRadiusPixels));
	}
}
