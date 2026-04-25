using System.Collections.Generic;
using Mozandifiers.Common.Config;
using Mozandifiers.Content.Prefixes.Common;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Mozandifiers.Content.Prefixes.Weapons;

public enum VampiricPreyState : byte
{
	None,
	Weakened,
	Bloodied,
	Critical
}

public sealed class SanguinePrefix : WeaponPrefix
{
	public static float LifeStealMultiplier => PrefixTuningConfig.Instance.VampiricLifeStealMultiplier;
	public static float ManaRestoreFromManaCost => PrefixTuningConfig.Instance.VampiricManaRestoreFromManaCost;
	public static int MaxManaRestorePerHit => PrefixTuningConfig.Instance.VampiricMaxManaRestorePerHit;
	public static int BaseHealCapPerSecond => PrefixTuningConfig.Instance.VampiricBaseHealCapPerSecond;
	public static int BaseManaRestorePerSecond => PrefixTuningConfig.Instance.VampiricBaseManaRestorePerSecond;
	public const int SustainWindowTicks = 60;
	public static float CriticalFeedMultiplier => PrefixTuningConfig.Instance.VampiricCriticalFeedMultiplier;
	public static float WeakenedPreyThreshold => PrefixTuningConfig.Instance.VampiricWeakenedPreyThreshold;
	public static float BloodiedPreyThreshold => PrefixTuningConfig.Instance.VampiricBloodiedPreyThreshold;
	public static float CriticalPreyThreshold => PrefixTuningConfig.Instance.VampiricCriticalPreyThreshold;
	public static float WeakenedFeedMultiplier => PrefixTuningConfig.Instance.VampiricWeakenedFeedMultiplier;
	public static float BloodiedFeedMultiplier => PrefixTuningConfig.Instance.VampiricBloodiedFeedMultiplier;
	public static float CriticalFeedStateMultiplier => PrefixTuningConfig.Instance.VampiricCriticalFeedStateMultiplier;
	public static int FrenzyDurationTicks => PrefixTuningConfig.Instance.VampiricFrenzyDurationTicks;
	public static int FrenzyRetriggerCooldownTicks => PrefixTuningConfig.Instance.VampiricFrenzyRetriggerCooldownTicks;
	public static float FrenzyAttackSpeedBonus => PrefixTuningConfig.Instance.VampiricFrenzyAttackSpeedBonus;
	public static float FrenzyCapMultiplier => PrefixTuningConfig.Instance.VampiricFrenzyCapMultiplier;
	public static int FrenzyCritBonusAgainstWeakened => PrefixTuningConfig.Instance.VampiricFrenzyCritBonusAgainstWeakened;
	public static int FrenzyCritBonusAgainstBloodied => PrefixTuningConfig.Instance.VampiricFrenzyCritBonusAgainstBloodied;
	public static int FrenzyCritBonusAgainstCritical => PrefixTuningConfig.Instance.VampiricFrenzyCritBonusAgainstCritical;

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
			$"{Name}VampiricLifeSteal",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.LifeStealTooltip",
				(int)(LifeStealMultiplier * 100f)));

		yield return new TooltipLine(
			Mod,
			$"{Name}VampiricMana",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.ManaSiphonTooltip",
				MaxManaRestorePerHit));

		yield return new TooltipLine(
			Mod,
			$"{Name}VampiricCriticalFeed",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.CriticalFeedTooltip",
				(int)((CriticalFeedMultiplier - 1f) * 100f)));

		yield return new TooltipLine(
			Mod,
			$"{Name}VampiricThresholds",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.ThresholdTooltip",
				(int)(WeakenedPreyThreshold * 100f),
				(int)(BloodiedPreyThreshold * 100f),
				(int)(CriticalPreyThreshold * 100f)));

		yield return new TooltipLine(
			Mod,
			$"{Name}VampiricFrenzy",
			Language.GetTextValue(
				$"Mods.{Mod.Name}.Prefixes.{Name}.FrenzyTooltip",
				FrenzyDurationTicks / 60f,
				(int)(FrenzyAttackSpeedBonus * 100f),
				FrenzyCritBonusAgainstWeakened,
				FrenzyCritBonusAgainstBloodied,
				FrenzyCritBonusAgainstCritical));
	}

	internal static VampiricPreyState GetPreyState(NPC target)
	{
		if (target == null || !target.active || target.lifeMax <= 0) {
			return VampiricPreyState.None;
		}

		return GetPreyState(target.life / (float)target.lifeMax);
	}

	internal static VampiricPreyState GetPreyState(float currentLifeRatio)
	{
		if (currentLifeRatio <= CriticalPreyThreshold) {
			return VampiricPreyState.Critical;
		}

		if (currentLifeRatio <= BloodiedPreyThreshold) {
			return VampiricPreyState.Bloodied;
		}

		if (currentLifeRatio <= WeakenedPreyThreshold) {
			return VampiricPreyState.Weakened;
		}

		return VampiricPreyState.None;
	}

	internal static float GetFeedMultiplier(VampiricPreyState preyState)
	{
		return preyState switch {
			VampiricPreyState.Weakened => WeakenedFeedMultiplier,
			VampiricPreyState.Bloodied => BloodiedFeedMultiplier,
			VampiricPreyState.Critical => CriticalFeedStateMultiplier,
			_ => 1f
		};
	}

	internal static int GetFrenzyCritBonus(VampiricPreyState preyState)
	{
		return preyState switch {
			VampiricPreyState.Weakened => FrenzyCritBonusAgainstWeakened,
			VampiricPreyState.Bloodied => FrenzyCritBonusAgainstBloodied,
			VampiricPreyState.Critical => FrenzyCritBonusAgainstCritical,
			_ => 0
		};
	}

	internal static float GetFrenzyTierScale(VampiricPreyState preyState)
	{
		return preyState switch {
			VampiricPreyState.Weakened => 0.6f,
			VampiricPreyState.Bloodied => 0.8f,
			VampiricPreyState.Critical => 1f,
			_ => 0f
		};
	}
}
